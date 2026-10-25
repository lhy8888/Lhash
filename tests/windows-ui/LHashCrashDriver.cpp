#include <Windows.h>
#include <DbgHelp.h>

#include <algorithm>
#include <array>
#include <chrono>
#include <filesystem>
#include <fstream>
#include <iomanip>
#include <iostream>
#include <map>
#include <sstream>
#include <stdexcept>
#include <string>
#include <vector>

#if !defined(_M_X64) && !defined(__x86_64__)
#error "Build this crash diagnostic driver for x64."
#endif

namespace fs = std::filesystem;

static std::wstring EnvironmentValue(const wchar_t* name)
{
    const DWORD required = GetEnvironmentVariableW(name, nullptr, 0);
    if (required == 0) return {};
    std::wstring value(required, L'\0');
    const DWORD count = GetEnvironmentVariableW(name, value.data(), required);
    if (count == 0 || count >= required) return {};
    value.resize(count);
    return value;
}

static std::string Hex(DWORD64 value)
{
    std::ostringstream text;
    text << "0x" << std::hex << value;
    return text.str();
}

static std::string AnnotationData(const std::string& value)
{
    std::string result;
    for (const char byte : value.substr(0, 6000)) {
        if (byte == '%') result += "%25";
        else if (byte == '\r') result += "%0D";
        else if (byte == '\n') result += "%0A";
        else result += byte;
    }
    return result;
}

class Log
{
public:
    explicit Log(const fs::path& file) : file_(file), github_(EnvironmentValue(L"GITHUB_ACTIONS") == L"true")
    {
        if (!file_) throw std::runtime_error("Cannot create diagnostic log.");
    }
    void Write(const std::string& text, bool annotate = false)
    {
        std::cout << text << std::endl;
        file_ << text << '\n';
        file_.flush();
        if (annotate && github_) std::cout << "::error::" << AnnotationData(text) << std::endl;
    }
private:
    std::ofstream file_;
    bool github_;
};

class Handle
{
public:
    explicit Handle(HANDLE value = nullptr) : value_(value) {}
    ~Handle() { if (value_ != nullptr && value_ != INVALID_HANDLE_VALUE) CloseHandle(value_); }
    Handle(const Handle&) = delete;
    Handle& operator=(const Handle&) = delete;
    HANDLE get() const { return value_; }
    bool valid() const { return value_ != nullptr && value_ != INVALID_HANDLE_VALUE; }
private:
    HANDLE value_;
};

struct OwnedChild
{
    PROCESS_INFORMATION process = {};
    bool exited = false;
    ~OwnedChild()
    {
        if (process.hProcess != nullptr) {
            if (!exited) {
                TerminateProcess(process.hProcess, 238);
                WaitForSingleObject(process.hProcess, 2000);
            }
            CloseHandle(process.hProcess);
        }
        if (process.hThread != nullptr) CloseHandle(process.hThread);
    }
};

struct InputCleanup
{
    fs::path path;
    ~InputCleanup() { if (!path.empty()) DeleteFileW(path.c_str()); }
};

static std::vector<wchar_t> ChildEnvironment()
{
    // The dump belongs only to this diagnostic child. Never give it the CI
    // process's signing variables, tokens, full PATH, or other ambient secrets.
    std::map<std::wstring, std::wstring> allowed;
    // Uppercase keys make the map's ordering also Windows' case-insensitive ordering.
    for (const auto name : {L"SYSTEMROOT", L"WINDIR", L"TEMP", L"TMP", L"USERPROFILE", L"APPDATA", L"LOCALAPPDATA"}) {
        const auto value = EnvironmentValue(name);
        if (!value.empty()) allowed.emplace(name, value);
    }
    std::array<wchar_t, 32768> systemDirectory = {};
    const UINT length = GetSystemDirectoryW(systemDirectory.data(), static_cast<UINT>(systemDirectory.size()));
    if (length == 0 || length >= systemDirectory.size()) throw std::runtime_error("GetSystemDirectory failed.");
    allowed[L"PATH"] = std::wstring(systemDirectory.data(), length);
    std::vector<wchar_t> block;
    for (const auto& item : allowed) {
        const auto entry = item.first + L"=" + item.second;
        block.insert(block.end(), entry.begin(), entry.end());
        block.push_back(L'\0');
    }
    block.push_back(L'\0');
    return block;
}

struct WindowSearch { DWORD processId; HWND window = nullptr; };
static bool BelongsTo(HWND window, DWORD processId)
{
    DWORD actual = 0;
    return window != nullptr && GetWindowThreadProcessId(window, &actual) != 0 && actual == processId;
}

static BOOL CALLBACK FindDialog(HWND window, LPARAM parameter)
{
    auto& search = *reinterpret_cast<WindowSearch*>(parameter);
    if (!BelongsTo(window, search.processId) || !IsWindowVisible(window) || GetWindow(window, GW_OWNER) != nullptr) return TRUE;
    std::array<wchar_t, 256> name = {};
    GetClassNameW(window, name.data(), static_cast<int>(name.size()));
    if (std::wstring(name.data()) != L"#32770") return TRUE;
    search.window = window;
    return FALSE;
}

static bool Busy(HWND window)
{
    HWND open = GetDlgItem(window, 1003);
    HWND verify = GetDlgItem(window, 1025);
    HWND settings = GetDlgItem(window, 1034);
    return open != nullptr && verify != nullptr && settings != nullptr &&
        IsWindowEnabled(open) && !IsWindowEnabled(verify) && !IsWindowEnabled(settings);
}

static bool TryCloseActiveChild(DWORD processId, Log& log)
{
    WindowSearch search{processId};
    EnumWindows(FindDialog, reinterpret_cast<LPARAM>(&search));
    if (search.window == nullptr || !Busy(search.window)) return false;
    HWND progress = GetDlgItem(search.window, 1014);
    if (!BelongsTo(progress, processId)) return false;
    DWORD_PTR position = 0;
    if (SendMessageTimeoutW(progress, WM_USER + 8, 0, 0,
            SMTO_ABORTIFHUNG | SMTO_BLOCK | SMTO_ERRORONEXIT, 100, &position) == 0 ||
        position == 0 || position > 100 || !Busy(search.window)) return false;
    if (!BelongsTo(search.window, processId) || !PostMessageW(search.window, WM_CLOSE, 0, 0)) return false;
    log.Write("[GUI] owned PID " + std::to_string(processId) + " busy=true progress=" + std::to_string(position) + " WM_CLOSE posted");
    return true;
}

static std::wstring FilePath(HANDLE file)
{
    if (file == nullptr || file == INVALID_HANDLE_VALUE) return {};
    const DWORD needed = GetFinalPathNameByHandleW(file, nullptr, 0, FILE_NAME_NORMALIZED);
    if (needed == 0) return {};
    std::wstring path(needed + 1, L'\0');
    const DWORD count = GetFinalPathNameByHandleW(file, path.data(), static_cast<DWORD>(path.size()), FILE_NAME_NORMALIZED);
    if (count == 0 || count >= path.size()) return {};
    path.resize(count);
    return path;
}

static void Frame(HANDLE process, DWORD64 address, size_t index, Log& log, bool raw = false)
{
    std::ostringstream text;
    text << (raw ? "[raw stack slot " : "[frame ") << index << "] " << Hex(address);
    if (raw) text << " (candidate only; not an unwound frame)";
    IMAGEHLP_MODULE64 module = {};
    module.SizeOfStruct = sizeof(module);
    if (SymGetModuleInfo64(process, address, &module)) text << " " << module.ModuleName << "+" << Hex(address - module.BaseOfImage);
    alignas(SYMBOL_INFO) std::array<unsigned char, sizeof(SYMBOL_INFO) + MAX_SYM_NAME> storage = {};
    auto* symbol = reinterpret_cast<SYMBOL_INFO*>(storage.data());
    symbol->SizeOfStruct = sizeof(SYMBOL_INFO);
    symbol->MaxNameLen = MAX_SYM_NAME;
    DWORD64 displacement = 0;
    if (SymFromAddr(process, address, &displacement, symbol)) text << " " << symbol->Name << "+" << Hex(displacement);
    else text << " <symbol unavailable error=" << GetLastError() << ">";
    IMAGEHLP_LINE64 line = {};
    line.SizeOfStruct = sizeof(line);
    DWORD lineDisplacement = 0;
    if (SymGetLineFromAddr64(process, address, &lineDisplacement, &line)) text << " " << line.FileName << ":" << line.LineNumber;
    log.Write(text.str(), true);
}

static void CaptureCrash(HANDLE process, DWORD processId, DWORD threadId,
                         const EXCEPTION_RECORD& exception, const fs::path& dumpPath, Log& log)
{
    log.Write("[CRASH] second-chance exception=" + Hex(exception.ExceptionCode) +
        " address=" + Hex(reinterpret_cast<DWORD64>(exception.ExceptionAddress)) +
        " PID=" + std::to_string(processId) + " TID=" + std::to_string(threadId), true);
    if (exception.NumberParameters >= 2) log.Write("[CRASH] operation=" + std::to_string(exception.ExceptionInformation[0]) +
        " target=" + Hex(exception.ExceptionInformation[1]), true);
    Handle thread(OpenThread(THREAD_GET_CONTEXT | THREAD_QUERY_INFORMATION, FALSE, threadId));
    CONTEXT context = {};
    context.ContextFlags = CONTEXT_FULL;
    if (!thread.valid() || !GetThreadContext(thread.get(), &context)) {
        log.Write("[CRASH] GetThreadContext failed, error=" + std::to_string(GetLastError()), true);
        Frame(process, reinterpret_cast<DWORD64>(exception.ExceptionAddress), 0, log);
        return;
    }
    EXCEPTION_RECORD exceptionCopy = exception;
    EXCEPTION_POINTERS pointers{&exceptionCopy, &context};
    MINIDUMP_EXCEPTION_INFORMATION information{threadId, &pointers, FALSE};
    Handle dump(CreateFileW(dumpPath.c_str(), GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr));
    const MINIDUMP_TYPE kind = static_cast<MINIDUMP_TYPE>(MiniDumpNormal | MiniDumpWithThreadInfo | MiniDumpWithUnloadedModules);
    if (!dump.valid() || !MiniDumpWriteDump(process, processId, dump.get(), kind, &information, nullptr, nullptr))
        log.Write("[CRASH] minidump failed, error=" + std::to_string(GetLastError()), true);
    else log.Write("[CRASH] wrote normal/thread-info minidump (no full-memory dump)");

    STACKFRAME64 frame = {};
    frame.AddrPC.Offset = context.Rip;
    frame.AddrStack.Offset = context.Rsp;
    frame.AddrFrame.Offset = context.Rbp;
    frame.AddrPC.Mode = frame.AddrStack.Mode = frame.AddrFrame.Mode = AddrModeFlat;
    const DWORD64 initialStack = context.Rsp;
    Frame(process, context.Rip, 0, log);
    DWORD64 previousPc = context.Rip;
    DWORD64 previousStack = context.Rsp;
    for (size_t index = 1; index < 20; ++index) {
        if (!StackWalk64(IMAGE_FILE_MACHINE_AMD64, process, thread.get(), &frame, &context, nullptr,
                        SymFunctionTableAccess64, SymGetModuleBase64, nullptr) || frame.AddrPC.Offset == 0) break;
        if (frame.AddrPC.Offset == previousPc && frame.AddrStack.Offset == previousStack) continue;
        Frame(process, frame.AddrPC.Offset, index, log);
        previousPc = frame.AddrPC.Offset;
        previousStack = frame.AddrStack.Offset;
    }
    if (exception.ExceptionAddress == nullptr) {
        // A null indirect call can leave no unwind metadata at RIP. Preserve a
        // few raw stack words as explicitly unverified return-address candidates.
        std::array<DWORD64, 8> candidates = {};
        SIZE_T bytes = 0;
        if (ReadProcessMemory(process, reinterpret_cast<const void*>(initialStack),
                candidates.data(), sizeof(candidates), &bytes)) {
            for (size_t index = 0; index < bytes / sizeof(DWORD64); ++index)
                Frame(process, candidates[index], index, log, true);
        }
    }
}

int wmain(int argc, wchar_t** argv)
{
    if (argc != 3) { std::cerr << "Usage: lhash_windows_crash_driver <LHash.exe> <diagnostic-directory>\n"; return 2; }
    fs::path input;
    try {
        const fs::path executable = fs::absolute(argv[1]);
        const fs::path output = fs::absolute(argv[2]);
        fs::create_directories(output);
        Log log(output / L"crash-stack.txt");
        input = output / (L"diagnostic-input-" + std::to_wstring(GetCurrentProcessId()) + L".bin");
        InputCleanup removeInput{input}; // Declared before the child: cleanup runs after it exits.
        {
            Handle file(CreateFileW(input.c_str(), GENERIC_WRITE, FILE_SHARE_READ, nullptr, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, nullptr));
            if (!file.valid()) throw std::runtime_error("Cannot create private diagnostic input.");
            const unsigned char marker = 0xA5;
            DWORD written = 0;
            LARGE_INTEGER size = {};
            size.QuadPart = 512LL * 1024 * 1024;
            if (!WriteFile(file.get(), &marker, 1, &written, nullptr) || written != 1 ||
                !SetFilePointerEx(file.get(), size, nullptr, FILE_BEGIN) || !SetEndOfFile(file.get()))
                throw std::runtime_error("Cannot initialize diagnostic input.");
        }
        auto environment = ChildEnvironment();
        std::wstring command = L"\"" + executable.wstring() + L"\" \"" + input.wstring() + L"\"";
        STARTUPINFOW startup = {};
        startup.cb = sizeof(startup);
        OwnedChild child;
        if (!CreateProcessW(executable.c_str(), command.data(), nullptr, nullptr, FALSE,
                DEBUG_ONLY_THIS_PROCESS | CREATE_UNICODE_ENVIRONMENT, environment.data(),
                executable.parent_path().c_str(), &startup, &child.process))
            throw std::runtime_error("CreateProcess(DEBUG_ONLY_THIS_PROCESS) failed: " + std::to_string(GetLastError()));
        DebugSetProcessKillOnExit(TRUE);
        log.Write("[debugger] launched owned PID " + std::to_string(child.process.dwProcessId) + " with a minimal environment; no inherited CI secrets or handles");
        SymSetOptions(SYMOPT_UNDNAME | SYMOPT_LOAD_LINES | SYMOPT_DEFERRED_LOADS | SYMOPT_FAIL_CRITICAL_ERRORS | SYMOPT_NO_PROMPTS);
        const bool symbols = SymInitializeW(child.process.hProcess, executable.parent_path().c_str(), FALSE) != FALSE;
        if (!symbols) log.Write("[symbols] SymInitialize failed: " + std::to_string(GetLastError()), true);
        bool closePosted = false;
        bool crashed = false;
        bool terminated = false;
        DWORD exitCode = 1;
        const auto start = std::chrono::steady_clock::now();
        while (!child.exited) {
            if (!closePosted && !terminated) closePosted = TryCloseActiveChild(child.process.dwProcessId, log);
            const auto elapsed = std::chrono::steady_clock::now() - start;
            if (!terminated && elapsed > std::chrono::seconds(20)) {
                log.Write("[debugger] timeout; terminating only the owned child", true);
                TerminateProcess(child.process.hProcess, 238);
                terminated = true;
            }
            if (terminated && elapsed > std::chrono::seconds(25)) break;
            DEBUG_EVENT event = {};
            if (!WaitForDebugEvent(&event, 25)) continue;
            DWORD continuation = DBG_CONTINUE;
            if (event.dwDebugEventCode == CREATE_PROCESS_DEBUG_EVENT) {
                const auto& info = event.u.CreateProcessInfo;
                if (symbols && SymLoadModuleExW(child.process.hProcess, nullptr, executable.c_str(), nullptr,
                        reinterpret_cast<DWORD64>(info.lpBaseOfImage), 0, nullptr, 0) == 0)
                    log.Write("[symbols] executable load returned 0, error=" + std::to_string(GetLastError()), true);
                if (info.hFile != nullptr) CloseHandle(info.hFile);
                if (info.hThread != child.process.hThread) CloseHandle(info.hThread);
                if (info.hProcess != child.process.hProcess) CloseHandle(info.hProcess);
            }
            else if (event.dwDebugEventCode == CREATE_THREAD_DEBUG_EVENT) {
                if (event.u.CreateThread.hThread != nullptr) CloseHandle(event.u.CreateThread.hThread);
            }
            else if (event.dwDebugEventCode == LOAD_DLL_DEBUG_EVENT) {
                const auto& info = event.u.LoadDll;
                const auto path = FilePath(info.hFile);
                if (symbols && !path.empty()) SymLoadModuleExW(child.process.hProcess, nullptr, path.c_str(), nullptr,
                    reinterpret_cast<DWORD64>(info.lpBaseOfDll), 0, nullptr, 0);
                if (info.hFile != nullptr) CloseHandle(info.hFile);
            }
            else if (event.dwDebugEventCode == UNLOAD_DLL_DEBUG_EVENT) {
                if (symbols) SymUnloadModule64(child.process.hProcess, reinterpret_cast<DWORD64>(event.u.UnloadDll.lpBaseOfDll));
            }
            else if (event.dwDebugEventCode == EXCEPTION_DEBUG_EVENT) {
                const auto& info = event.u.Exception;
                continuation = DBG_EXCEPTION_NOT_HANDLED;
                if (info.dwFirstChance != 0 && (info.ExceptionRecord.ExceptionCode == EXCEPTION_BREAKPOINT ||
                    info.ExceptionRecord.ExceptionCode == 0x406D1388)) continuation = DBG_CONTINUE;
                if (info.dwFirstChance == 0) {
                    crashed = true;
                    CaptureCrash(child.process.hProcess, child.process.dwProcessId, event.dwThreadId,
                                 info.ExceptionRecord, output / L"crash.dmp", log);
                    std::error_code copyError;
                    fs::copy_file(executable, output / executable.filename(), fs::copy_options::overwrite_existing, copyError);
                    auto pdb = executable;
                    pdb.replace_extension(L".pdb");
                    if (fs::exists(pdb, copyError)) fs::copy_file(pdb, output / pdb.filename(), fs::copy_options::overwrite_existing, copyError);
                    // Do not swallow the crash. Let this child exit normally
                    // through Windows' unhandled-exception processing.
                }
            }
            else if (event.dwDebugEventCode == EXIT_PROCESS_DEBUG_EVENT) {
                exitCode = event.u.ExitProcess.dwExitCode;
                child.exited = true;
                log.Write("[debugger] owned process exit=" + Hex(exitCode) + " closePosted=" + (closePosted ? "true" : "false"));
            }
            if (!ContinueDebugEvent(event.dwProcessId, event.dwThreadId, continuation))
                throw std::runtime_error("ContinueDebugEvent failed: " + std::to_string(GetLastError()));
        }
        if (symbols) SymCleanup(child.process.hProcess);
        if (child.exited) DeleteFileW(input.c_str());
        if (!crashed && closePosted && child.exited && !terminated && exitCode == 0) {
            log.Write("[debugger] this diagnostic run closed cleanly; the original GUI failure remains a failure");
            return 0;
        }
        return 1;
    }
    catch (const std::exception& error) {
        const std::string message = std::string("LHash crash driver: ") + error.what();
        std::cerr << message << '\n';
        if (EnvironmentValue(L"GITHUB_ACTIONS") == L"true") std::cout << "::error::" << AnnotationData(message) << '\n';
        if (!input.empty()) DeleteFileW(input.c_str());
        return 2;
    }
}
