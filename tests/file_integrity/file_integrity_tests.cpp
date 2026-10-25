#include "stdafx.h"
#include "Common/HashEngine.h"
#include "Domain/HashRequest.h"
#include "Runtime/HashExecutionContext.h"
#include "OsUtils/OsFile.h"

#include <atomic>
#include <cerrno>
#include <chrono>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <stdexcept>
#include <string>
#include <thread>
#include <vector>
#include <fcntl.h>
#include <signal.h>
#include <sys/stat.h>
#include <sys/wait.h>
#include <unistd.h>

namespace fs = std::filesystem;
static constexpr size_t kFileSize = 8 * 1024 * 1024 + 137;
static std::string injectionMode, target;
static dev_t targetDev = 0;
static ino_t targetIno = 0;
static size_t readCalls = 0, readBytes = 0;
static bool injected = false;

static void Require(bool condition, const char* message)
{
    if (!condition) throw std::runtime_error(message);
}

static void WriteFile(const fs::path& path, const std::string& contents)
{
    std::ofstream file(path, std::ios::binary | std::ios::trunc);
    file.write(contents.data(), static_cast<std::streamsize>(contents.size()));
    Require(bool(file), "fixture write failed");
}

#if defined(__linux__)
// Linker interposition is confined to this test executable and our fixture inode.
extern "C" ssize_t __real_read(int, void*, size_t);
extern "C" int __real_lstat(const char*, struct stat*);
extern "C" ssize_t __wrap_read(int fd, void* bytes, size_t count)
{
    struct stat status = {};
    if (injectionMode.empty() || fstat(fd, &status) != 0 ||
        status.st_dev != targetDev || status.st_ino != targetIno)
        return __real_read(fd, bytes, count);
    ++readCalls;
    if (injectionMode == "short" && !injected) { injected = true; count = 137; }
    if ((injectionMode == "eio" && readCalls == 2) || (injectionMode == "eintr" && readCalls == 1))
    {
        injected = true;
        errno = injectionMode == "eio" ? EIO : EINTR;
        return -1;
    }
    const ssize_t result = __real_read(fd, bytes, count);
    if (result > 0) readBytes += static_cast<size_t>(result);
    if (result > 0 && !injected && (injectionMode == "overwrite" || injectionMode == "shrink" || injectionMode == "grow"))
    {
        injected = true;
        const int writer = open(target.c_str(), O_WRONLY);
        Require(writer >= 0, "fixture writer open failed");
        bool changed = false;
        if (injectionMode == "overwrite")
        {
            const std::string contents(kFileSize, 'B');
            changed = pwrite(writer, contents.data(), contents.size(), 0) == static_cast<ssize_t>(contents.size());
        }
        else if (injectionMode == "shrink") changed = ftruncate(writer, 1) == 0;
        else
        {
            const std::string contents(131073, 'B');
            changed = pwrite(writer, contents.data(), contents.size(), kFileSize) == static_cast<ssize_t>(contents.size());
        }
        close(writer);
        Require(changed, "fixture concurrent write failed");
    }
    return result;
}

extern "C" int __wrap_lstat(const char* path, struct stat* status)
{
    const int result = __real_lstat(path, status);
    if (injectionMode == "fifo-race" && !injected && target == path && result == 0 && S_ISREG(status->st_mode))
    {
        injected = true;
        if (unlink(path) != 0 || mkfifo(path, 0600) != 0) _exit(111);
    }
    return result;
}
#endif

static void TestEngine(const fs::path& root, const std::string& mode, bool parallel)
{
    target = (root / "engine.bin").string();
    WriteFile(target, std::string(kFileSize, 'A'));
    struct stat status = {};
    Require(stat(target.c_str(), &status) == 0, "fixture stat failed");
    targetDev = status.st_dev; targetIno = status.st_ino;
    readCalls = readBytes = 0; injected = false; injectionMode = mode;
    HashRequest request;
    request.files.push_back(sunjwbase::strtotstrutf8(target));
    AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr("md5"));
    AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr("sha1"));
    size_t expectedDigestCount = 2;
#if defined(LHASH_WITH_OPENSSL3_VENDOR)
    AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr("openssl-sha-256"));
    ++expectedDigestCount;
#endif
    request.digestExecutionPolicy = parallel ? HASH_REQUEST_DIGEST_EXECUTION_POLICY_PARALLEL : HASH_REQUEST_DIGEST_EXECUTION_POLICY_SINGLE_THREADED;
    HashJobState state; HashCancellationState cancel; HashExecutionContext context(nullptr, state, cancel);
    Require(RunHashRequest(&context, request) == 0, "engine rejected fixture request");
    Require(state.results.size() == 1, "wrong result count");
    const auto& result = state.results.front();
    const bool expectSuccess = mode == "normal" || mode == "short" || mode == "eintr";
    Require(result.state == (expectSuccess ? RESULT_ALL : RESULT_ERROR), "incorrect file result state");
    if (expectSuccess)
    {
        if (!result.error.empty() || result.meta.size != kFileSize || result.digests.size() != expectedDigestCount)
        {
            std::string detail = "incomplete success result: mode=" + mode +
                " policy=" + (parallel ? "parallel" : "single") +
                " state=" + std::to_string(result.state) +
                " meta=" + std::to_string(result.meta.size) +
                " expected_size=" + std::to_string(kFileSize) +
                " digests=" + std::to_string(result.digests.size()) +
                " expected_digests=" + std::to_string(expectedDigestCount) +
                " read_calls=" + std::to_string(readCalls) +
                " read_bytes=" + std::to_string(readBytes) +
                " error=" + sunjwbase::tstrtostr(result.error);
            for (const auto& digest : result.digests) detail += " id=" + sunjwbase::tstrtostr(digest.algorithmId);
            throw std::runtime_error(detail);
        }
        for (const auto& digest : result.digests)
        {
            const auto id = sunjwbase::tstrtostr(digest.algorithmId);
            const auto value = sunjwbase::str_lower(sunjwbase::tstrtostr(digest.value));
            const char* expected = id == "md5" ? "0dfee6720119110e06a886de07416ca5" :
                id == "sha1" ? "6bfb18774391a05b3861fc4753dad1914eed3a01" :
                id == "openssl-sha-256" ? "33a9cb5984cfd9047d24d5fe403698a0dd71e7636215f5cd01a861573b09d9a3" : "";
            Require(value == expected, "full-file digest mismatch");
        }
#if defined(__linux__)
        Require(readBytes == kFileSize, "successful result did not read the full file");
#endif
    }
    else Require(!result.error.empty() && result.digests.empty(), "unstable/error file published digests");
#if defined(__linux__)
    Require(mode == "normal" || injected, "fault was not injected");
#endif
    std::cout << "engine " << mode << ' ' << (parallel ? "parallel" : "single") << " PASS reads=" << readCalls << " bytes=" << readBytes << " digests=" << result.digests.size() << '\n';
    injectionMode.clear();
}

static void Drain(sunjwbase::OsFile& file)
{
    char buffer[137];
    int64_t count;
    do { count = file.read(buffer, sizeof(buffer)); Require(count >= 0, "direct read failed"); } while (count != 0);
}

static void TestMetadataOnly(const fs::path& root)
{
    target = (root / "metadata.bin").string(); WriteFile(target, "abc");
    struct stat status = {}; Require(stat(target.c_str(), &status) == 0, "metadata fixture stat failed");
    targetDev = status.st_dev; targetIno = status.st_ino;
    injectionMode = "eintr"; injected = false; readCalls = readBytes = 0;
    HashRequest request; request.files.push_back(sunjwbase::strtotstrutf8(target));
    HashJobState state; HashCancellationState cancel; HashExecutionContext context(nullptr, state, cancel);
    Require(RunHashRequest(&context, request) == 0 && state.results.size() == 1, "metadata request failed");
    const auto& result = state.results.front();
    Require(result.state == RESULT_META && result.meta.size == 3 && result.digests.empty() && result.error.empty(), "metadata-only result changed");
#if defined(__linux__)
    Require(readCalls == 0, "metadata-only request read file contents");
#endif
    injectionMode.clear();
    std::cout << "metadata-only request PASS\n";
}

static void TestDirectConsistency(const fs::path& root)
{
    for (const std::string mode : {"complete", "empty", "partial", "overwrite", "restore-mtime", "shrink", "grow", "seek", "closed"})
    {
        const auto path = root / "direct.bin";
        WriteFile(path, mode == "empty" ? "" : std::string(4096, 'A'));
        // A known old mtime makes same-size overwrite detection deterministic.
        timespec oldTimes[2] = {{1000000000, 0}, {1000000000, 0}};
        Require(utimensat(AT_FDCWD, path.c_str(), oldTimes, 0) == 0, "fixture timestamp setup failed");
        sunjwbase::OsFile file(sunjwbase::strtotstrutf8(path.string()));
        Require(file.openReadScan(), "direct open failed");
        char first[8];
        if (mode != "empty") Require(file.read(first, sizeof(first)) == sizeof(first), "initial read failed");
        if (mode == "overwrite" || mode == "restore-mtime" || mode == "shrink" || mode == "grow")
        {
            // Ensure ctime-only changes are distinguishable on supported filesystems.
            if (mode == "restore-mtime") std::this_thread::sleep_for(std::chrono::milliseconds(20));
            const int writer = open(path.c_str(), O_WRONLY);
            Require(writer >= 0, "direct writer open failed");
            bool changed;
            if (mode == "shrink") changed = ftruncate(writer, 1) == 0;
            else changed = pwrite(writer, "B", 1, mode == "grow" ? 4096 : 0) == 1;
            close(writer);
            Require(changed, "direct mutation failed");
            if (mode == "restore-mtime") Require(utimensat(AT_FDCWD, path.c_str(), oldTimes, 0) == 0, "restore mtime failed");
        }
        if (mode == "seek") file.seek(0, sunjwbase::OsFile::OF_SEEK_BEGIN);
        if (mode != "partial") Drain(file);
        if (mode == "closed") file.close();
        sunjwbase::tstring error;
        const bool valid = file.validateReadConsistency(&error);
        const bool expected = mode == "complete" || mode == "empty";
        Require(valid == expected, "direct consistency check wrong");
        Require(valid || !error.empty(), "rejected read did not provide a reason");
        std::cout << "consistency " << mode << " PASS\n";
    }
}

static void PathCase(const fs::path& path, bool expected)
{
    sunjwbase::OsFile file(sunjwbase::strtotstrutf8(path.string()));
    Require(file.isHashTargetAllowed() == expected, "path policy mismatch");
    Require(file.openRead() == expected, "opened path policy mismatch");
}

static void TestPaths(const fs::path& root)
{
    fs::create_directory(root / "real"); WriteFile(root / "real/file", "abc");
    fs::create_directory_symlink(root / "real", root / "alias");
    fs::create_symlink(root / "real/file", root / "leaf-link");
    fs::create_symlink(root / "missing", root / "broken-link");
    fs::create_hard_link(root / "real/file", root / "hard-link");
    Require(mkfifo((root / "fifo").c_str(), 0600) == 0, "mkfifo fixture failed");
    PathCase(root / "real/file", true); PathCase(root / "alias/file", true); PathCase(root / "hard-link", true);
    PathCase(root / "leaf-link", false); PathCase(root / "broken-link", false); PathCase(root / "missing", false);
    PathCase(root / "real", false); PathCase(root / "fifo", false);
    std::cout << "8 path-policy cases PASS\n";
#if defined(__linux__)
    target = (root / "race").string(); WriteFile(target, "abc"); injectionMode = "fifo-race"; injected = false;
    const pid_t child = fork(); Require(child >= 0, "fork failed");
    if (child == 0) { sunjwbase::OsFile file(target); _exit(file.openRead() ? 2 : 0); }
    std::this_thread::sleep_for(std::chrono::milliseconds(500));
    int status = 0; const pid_t finished = waitpid(child, &status, WNOHANG);
    if (finished == 0) { kill(child, SIGTERM); waitpid(child, &status, 0); }
    injectionMode.clear();
    Require(finished == child && WIFEXITED(status) && WEXITSTATUS(status) == 0, "FIFO replacement blocked or was accepted");
    std::cout << "FIFO replacement rejected before blocking PASS\n";
#endif
}

static void TestConcurrentTimeFormatting(const fs::path& root)
{
    const auto path = root / "time.bin"; WriteFile(path, "abc");
    sunjwbase::OsFile baseline(sunjwbase::strtotstrutf8(path.string()));
    const auto expected = baseline.getModifiedTimeFormat(); Require(!expected.empty(), "timestamp baseline empty");
    std::atomic<bool> valid(true); std::vector<std::thread> threads;
    for (int i = 0; i < 8; ++i) threads.emplace_back([&] {
        for (int j = 0; j < 200; ++j)
        {
            sunjwbase::OsFile file(sunjwbase::strtotstrutf8(path.string()));
            if (file.getModifiedTimeFormat() != expected) valid.store(false);
        }
    });
    for (auto& thread : threads) thread.join();
    Require(valid.load(), "concurrent timestamp mismatch");
    std::cout << "8 x 200 concurrent timestamp formats PASS\n";
}

int main()
{
    char directory[] = "/tmp/lhash-file-integrity-XXXXXX";
    if (mkdtemp(directory) == nullptr) return 1;
    const fs::path root(directory);
    try
    {
        for (bool parallel : {false, true})
        {
            TestEngine(root, "normal", parallel);
#if defined(__linux__)
            for (const char* mode : {"short", "overwrite", "shrink", "grow", "eio", "eintr"}) TestEngine(root, mode, parallel);
#endif
        }
        TestMetadataOnly(root); TestDirectConsistency(root); TestPaths(root); TestConcurrentTimeFormatting(root);
        fs::remove_all(root);
        return 0;
    }
    catch (const std::exception& error)
    {
        std::cerr << error.what() << '\n'; fs::remove_all(root); return 1;
    }
}
