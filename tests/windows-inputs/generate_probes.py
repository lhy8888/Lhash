#!/usr/bin/env python3
"""Extract selected current product function bodies into isolated Linux probes.

Windows/MFC APIs are explicitly test doubles. This is NOT native Windows testing.
No registry writes, processes, messages, or clipboard operations are performed.
"""
from pathlib import Path
import hashlib
import json
import sys

repo = Path(__file__).resolve().parents[2]
out = Path(sys.argv[1])
out.mkdir(parents=True, exist_ok=True)
manifest = []

def extract(relative, signature):
    text = (repo / relative).read_text(encoding='utf-8-sig')
    start = text.index(signature)
    opening = text.index('{', start)
    depth = 0
    for i in range(opening, len(text)):
        if text[i] == '{': depth += 1
        if text[i] == '}': depth -= 1
        if depth == 0:
            result = text[start:i+1]
            manifest.append(dict(path=relative, signature=signature,
                                 first_line=text[:start].count('\n')+1,
                                 sha256=hashlib.sha256(result.encode()).hexdigest()))
            return result
    raise ValueError(signature)

inputs = 'trunk/source/WinMFC/FilesHashInputController.cpp'
access = 'trunk/source/Adapters/MfcBridge/ThreadDataInputAccess.h'
common = r'''
#include <algorithm>
#include <cassert>
#include <cstdint>
#include <cstring>
#include <cwchar>
#include <iostream>
#include <random>
#include <string>
#include <vector>
#include <stdexcept>
#include <functional>
#ifdef NDEBUG
#undef assert
#define assert(value) do { if (!(value)) throw std::runtime_error(#value); } while (false)
#endif
using DWORD = uint32_t;
'''
copy_probe = common + r'''
using TCHAR = char16_t; // Windows Unicode code unit, independent of Linux wchar_t.
#define _T(s) u##s
struct COPYDATASTRUCT { uintptr_t dwData; DWORD cbData; void* lpData; };
constexpr size_t COPYDATA_COMMAND_CHAR_LIMIT = 32768;
struct FilesHashInputController {
    static size_t GetCopyDataCommandCharLimit();
    static bool IsValidCopyDataString(const COPYDATASTRUCT*);
};
'''
copy_probe += extract(inputs, 'size_t FilesHashInputController::GetCopyDataCommandCharLimit()') + '\n'
copy_probe += extract(inputs, 'bool FilesHashInputController::IsValidCopyDataString(') + '\n'
copy_probe += r'''
bool oracle(const COPYDATASTRUCT* p) {
    if (!p || !p->lpData || p->cbData < 2 || p->cbData % 2 || p->cbData > 65536) return false;
    auto b = static_cast<const char16_t*>(p->lpData);
    const auto end = b + p->cbData / 2;
    const auto nul = std::find(b, end, char16_t(0));
    return nul != end && std::all_of(nul, end, [](char16_t x){ return x == 0; });
}
int main() {
    size_t cases = 0;
    auto check = [&](COPYDATASTRUCT* p) {
        assert(FilesHashInputController::IsValidCopyDataString(p) == oracle(p));
        ++cases;
    };
    check(nullptr);
    COPYDATASTRUCT p{}; check(&p);
    std::vector<char16_t> b(32769, u'x'); p.lpData = b.data();
    for (uint32_t bytes : {0u, 1u, 2u, 3u, 4u, 65534u, 65535u, 65536u, 65538u, 0xFFFFFFFFu}) {
        p.cbData = bytes; check(&p);
        b[0] = 0; check(&p); b[0] = u'x';
    }
    b[32767] = 0; p.cbData = 65536; check(&p);
    assert(FilesHashInputController::IsValidCopyDataString(&p));
    b[200] = 0; check(&p); assert(!FilesHashInputController::IsValidCopyDataString(&p));
    std::mt19937_64 rng(0x109035889591ULL);
    for (size_t k = 0; k < 250000; ++k) {
        const size_t n = 1 + rng() % 96;
        b.resize(n);
        for (auto &c : b) c = (rng() % 11 == 0) ? 0 : char16_t(1 + rng() % 65535);
        p.lpData = b.data(); p.cbData = static_cast<DWORD>(n * 2 - rng() % 3);
        check(&p);
    }
    std::cout << "COPYDATA actual validator: " << cases << " boundary/random cases PASS; UTF-16 code units; ASan/UBSan\n";
}
'''
(out/'copydata_probe.cpp').write_text(copy_probe, encoding='utf-8')

dialog_probe = common + r'''
using TCHAR = char16_t;
using LPCTSTR = const TCHAR*;
using POSITION = uintptr_t;
namespace sunjwbase { using tstring = std::u16string; }
using TStrVector = std::vector<sunjwbase::tstring>;
constexpr size_t kMaxHashFilesPerSession = 8192;
constexpr int MAX_PATH = 260, TRUE = 1, OFN_HIDEREADONLY = 1, OFN_ALLOWMULTISELECT = 2, IDOK = 1;
enum class FileLoadResult { Success, SuccessPossiblyTruncated, SuccessWithTruncation, RejectedOverLimit, EnumerationLimit, Cancelled, Empty, Error };
struct FileLoadOutcome { FileLoadResult result; size_t loadedCount, limit; bool hasRequestedCount; size_t requestedCount; };
struct ThreadDataInputState { TStrVector inputFiles; uint32_t fileCount{}; };
struct ThreadData { ThreadDataInputState inputState; };
constexpr int WM_CLOSE=16;
struct CWnd { int closes=0; void PostMessage(int message) { assert(message==WM_CLOSE); ++closes; } };
static std::function<void()> onDialog;
struct FakeCString { sunjwbase::tstring value; LPCTSTR GetString() const { return value.c_str(); } };
struct FakeOFN { TCHAR* lpstrFile{}; DWORD nMaxFile{}; };
static TStrVector selected;
struct CFileDialog {
    FakeOFN ofn;
    CFileDialog(int, void*, void*, int, LPCTSTR, CWnd*, int) {}
    FakeOFN& GetOFN() { return ofn; }
    int DoModal() { if(onDialog)onDialog(); return IDOK; }
    POSITION GetStartPosition() { return selected.empty() ? 0 : 1; }
    FakeCString GetNextPathName(POSITION& p) { auto s = selected.at(p-1); p = p == selected.size() ? 0 : p+1; return {s}; }
};
struct FilesHashInputController {
    ThreadData* m_threadData;
    CWnd* m_parentWnd;
    bool m_loadingFiles=false, m_cancelLoad=false, m_closeAfterLoading=false;
    struct PendingLoadGuard { FilesHashInputController& m_owner; explicit PendingLoadGuard(FilesHashInputController&); ~PendingLoadGuard(); };
    void CancelPendingLoad(bool closeAfterLoading=false);
    void ClearFilePaths();
    FileLoadOutcome LoadOpenFileDialogSelection(LPCTSTR);
};
'''
for signature in (
    'static inline const ThreadDataInputState& GetThreadDataInputState(',
    'static inline ThreadDataInputState& GetMutableThreadDataInputState(',
    'static inline TStrVector& GetMutableThreadDataInputFiles(',
    'static inline void SetThreadDataFileCount(',
    'static inline uint32_t GetThreadDataFileCount(',
    'static inline void ResetThreadDataInputFiles(',
    'static inline void AddThreadDataFullPath(',
    'static inline void AppendThreadDataInputFile(',
    'static inline bool HasThreadDataInputFiles(',
    'static inline void AppendThreadDataInputFiles(',
    'static inline void ReplaceThreadDataInputFiles('):
    dialog_probe += extract(access, signature) + '\n'
dialog_probe += extract(inputs, 'FileLoadOutcome MakeFileLoadOutcome(') + '\n'
dialog_probe += extract(inputs, 'void FilesHashInputController::ClearFilePaths()') + '\n'
for signature in ['FilesHashInputController::PendingLoadGuard::PendingLoadGuard(', 'FilesHashInputController::PendingLoadGuard::~PendingLoadGuard()', 'void FilesHashInputController::CancelPendingLoad(']:
    dialog_probe += extract(inputs, signature)+'\n'
dialog_probe += extract(inputs, 'FileLoadOutcome FilesHashInputController::LoadOpenFileDialogSelection(') + '\n'
dialog_probe += r'''
int main() {
    CWnd parent; ThreadData state; FilesHashInputController ctl{&state,&parent};
    for (size_t n : {0u, 1u, 8191u, 8192u, 8193u, 10000u}) {
        selected.clear(); size_t multiStringChars = 7; // C:\a + terminators, conservative.
        for (size_t i=0; i<n; ++i) {
            auto digits=std::to_string(i);
            std::u16string name=u"f";
            for(char c:digits) name.push_back(c);
            multiStringChars += name.size()+1;
            selected.push_back(u"C:\\a\\"+name);
        }
        assert(multiStringChars < 8192*260+1);
        state.inputState.inputFiles={u"previous"}; state.inputState.fileCount=1;
        auto r=ctl.LoadOpenFileDialogSelection(nullptr);
        const size_t expected=(n==0 || n>8192)?1:n;
        assert(state.inputState.fileCount == expected);
        assert(state.inputState.inputFiles.size() == expected);
        if(n>8192) { assert(r.result==FileLoadResult::RejectedOverLimit); assert(state.inputState.inputFiles.front()==u"previous"); }
        else if(n==0) assert(r.result==FileLoadResult::Empty);
        else assert(r.result==FileLoadResult::Success);
        std::cout << "dialog supplied=" << n << " loaded=" << r.loadedCount << " stored=" << state.inputState.fileCount
                  << " declaredLimit=" << r.limit << " result=" << int(r.result)
                  << " namesMultiStringChars=" << multiStringChars << " capacity=" << 8192*260+1 << '\n';
    }
    selected={u"new selection"}; state.inputState.inputFiles={u"previous"};state.inputState.fileCount=1;
    onDialog=[&]{assert(ctl.m_loadingFiles);ctl.CancelPendingLoad(true);};
    auto cancelled=ctl.LoadOpenFileDialogSelection(nullptr);
    assert(cancelled.result==FileLoadResult::Cancelled && !ctl.m_loadingFiles && parent.closes==1);
    assert(state.inputState.inputFiles.front()==u"previous"); onDialog={};
    std::cout << "Selection boundaries preserve previous task on rejection/cancel; Windows dialog API is a test double, production selection function is unchanged.\n";
}
'''
(out/'dialog_limit_probe.cpp').write_text(dialog_probe, encoding='utf-8')

registry_probe = common + r'''
#include "Common/HashInputPolicy.h"
using tstring = std::wstring;
#define UNICODE 1
#define _T(s) L##s
using TCHAR = wchar_t; using LPCTSTR = const wchar_t*; using LONG = long;
constexpr int MAX_PATH=260, HKEY_CLASSES_ROOT=1, KEY_ALL_ACCESS=1, ERROR_SUCCESS=0;
constexpr auto CONTEXT_MENU_REGESTRY_ZH_CN=L"mock-zh", CONTEXT_MENU_REGESTRY_EN_US=L"mock-en";
namespace WindowsStrings { int GetCurrentUILang() { return 2052; } }
static std::wstring recorded;
struct CRegKey {
    int Create(int, LPCTSTR) { return 0; } int Open(int, LPCTSTR, int) { return 0; }
    int SetStringValue(void*, LPCTSTR s) { recorded=s; return 0; } void Close() {}
};
int wcscpy_s(wchar_t* d, size_t n, const wchar_t* s) { auto len=std::wcslen(s); assert(len<n); std::wmemcpy(d,s,len+1); return 0; }
int wcscat_s(wchar_t* d, size_t n, const wchar_t* s) { auto len=std::wcslen(d); return wcscpy_s(d+len,n-len,s); }
'''
registry_probe += extract('trunk/source/WinMFC/WindowsUtils.cpp', 'bool AddContextMenu(const TCHAR *pszExeFullPath)')+'\n'
registry_probe += r'''
int main() {
    std::vector<std::wstring> paths={L"C:\\Tools\\LHash.exe",L"C:\\Program Files\\LHash\\LHash.exe",L"C:\\Users\\Alice\\Downloads\\hash tools\\LHash.exe",L"C:\\工具 目录\\LHash.exe"};
    for(size_t i=0;i<paths.size();++i) {
        auto p=paths[i]; assert(AddContextMenu(p.data()));
        assert(recorded == L"\"" + p + L"\" \"%1\"");
        std::cout << "registry case=" << i << " unicode=" << (i==3) << " executableContainsSpaces=" << (p.find(L' ')!=p.npos)
                  << " executableQuoted=" << (recorded.front()==L'\"') << " commandChars=" << recorded.size() << '\n';
        if(i!=3) { for(wchar_t c:recorded) std::cout << char(c); std::cout << '\n'; }
    }
    std::cout << "All registry calls mocked. No Windows Shell resolution or process launch attempted.\n";
}
'''
(out/'registry_command_probe.cpp').write_text(registry_probe, encoding='utf-8')

folder_probe = common + r'''
#include <deque>
#include <utility>
#include "Common/HashInputPolicy.h"
#define _T(s) s
#define _tcscmp strcmp
namespace sunjwbase { using tstring = std::string; }
using sunjwbase::tstring;
using TCHAR=char; using TStrVector=std::vector<std::string>;
constexpr size_t kMaxHashFilesPerSession=8192;
constexpr DWORD FILE_ATTRIBUTE_DIRECTORY=16, FILE_ATTRIBUTE_REPARSE_POINT=1024;
constexpr int FALSE=0, ERROR_FILE_NOT_FOUND=2, ERROR_NO_MORE_FILES=18, ERROR_ACCESS_DENIED=5;
struct FolderScanOutcome { TStrVector files; bool overFileLimit=false, budgetExceeded=false, cancelled=false, failed=false; };
struct WIN32_FIND_DATA { DWORD dwFileAttributes; char cFileName[260]; };
struct FindState { size_t i; };
using HANDLE=FindState*;
static const HANDLE INVALID_HANDLE_VALUE=reinterpret_cast<HANDLE>(intptr_t(-1));
static size_t directoryOpens, entryCount, closed;
static DWORD entryAttributes, lastError;
static bool failOpen=false;
struct IProgressDialog { size_t calls=0, cancelAt=~size_t(0); bool HasUserCancelled() { return ++calls >= cancelAt; } };
DWORD GetLastError() { return lastError; }
void setEntry(WIN32_FIND_DATA* d,size_t i) {
    auto name=std::string("item")+std::to_string(i);
    std::strcpy(d->cFileName,name.c_str()); d->dwFileAttributes=entryAttributes;
}
HANDLE FindFirstFile(const char* path, WIN32_FIND_DATA* d) {
    ++directoryOpens;
    if (failOpen) { lastError=ERROR_ACCESS_DENIED; return INVALID_HANDLE_VALUE; }
    if(std::string(path)!="root\\*" || entryCount==0) { lastError=ERROR_FILE_NOT_FOUND; return INVALID_HANDLE_VALUE; }
    setEntry(d,0); return new FindState{0};
}
int FindNextFile(HANDLE h,WIN32_FIND_DATA* d) {
    if(++h->i>=entryCount) { lastError=ERROR_NO_MORE_FILES; return FALSE; }
    setEntry(d,h->i); return 1;
}
namespace WinHandleGuard { struct UniqueFindHandle {
    HANDLE h; explicit UniqueFindHandle(HANDLE v):h(v){} bool isValid()const{return h!=INVALID_HANDLE_VALUE;}
    HANDLE get()const{return h;} ~UniqueFindHandle(){if(isValid()){delete h;++closed;}}
}; }
struct FilesHashInputController { static FolderScanOutcome AppendFolderFilesRecursive(const sunjwbase::tstring&,IProgressDialog*,const bool* = nullptr); };
'''
folder_probe += extract(inputs, 'FolderScanOutcome FilesHashInputController::AppendFolderFilesRecursive(')+'\n'
folder_probe += r'''
int main() {
    entryCount=50000; directoryOpens=closed=0; entryAttributes=FILE_ATTRIBUTE_DIRECTORY;
    auto r=FilesHashInputController::AppendFolderFilesRecursive("root",nullptr);
    assert(directoryOpens==1 && r.files.empty() && r.budgetExceeded && closed==1);
    entryAttributes=FILE_ATTRIBUTE_DIRECTORY|FILE_ATTRIBUTE_REPARSE_POINT;
    r=FilesHashInputController::AppendFolderFilesRecursive("root",nullptr);
    assert(r.files.empty() && !r.budgetExceeded && !r.failed);
    entryCount=65537;
    r=FilesHashInputController::AppendFolderFilesRecursive("root",nullptr);
    assert(r.budgetExceeded);
    entryAttributes=0;
    for(size_t n : {0u,1u,8191u,8192u,8193u,10000u}) {
        entryCount=n; r=FilesHashInputController::AppendFolderFilesRecursive("root",nullptr);
        assert(r.files.size()==std::min(n,size_t(8192))); assert(r.overFileLimit==(n>8192));
        assert(!r.failed && !r.cancelled && !r.budgetExceeded);
    }
    IProgressDialog progress; progress.cancelAt=8; entryCount=10000;
    size_t before=closed;
    r=FilesHashInputController::AppendFolderFilesRecursive("root",&progress);
    assert(r.cancelled && r.files.size()<8 && closed==before+1);
    progress.calls=0; progress.cancelAt=1;
    r=FilesHashInputController::AppendFolderFilesRecursive("root",&progress);
    assert(r.cancelled && r.files.empty());
    bool requested=true; r=FilesHashInputController::AppendFolderFilesRecursive("root",nullptr,&requested);
    assert(r.cancelled && r.files.empty());
    failOpen=true; r=FilesHashInputController::AppendFolderFilesRecursive("root",nullptr);
    assert(r.failed && r.files.empty());
    HashFolderScanBudget budget;
    assert(budget.QueueDirectory(128)); assert(!budget.QueueDirectory(129));
    std::cout<<"Folder budgets, 8192 boundary, cancellation, error and handle cleanup PASS; Win32 enumeration API test doubles\n";
}
'''
(out/'folder_walk_probe.cpp').write_text(folder_probe, encoding='utf-8')


utility_probe = common + r'''
#include <limits>
#include "Common/HashInputPolicy.h"
#define _T(s) s
using TCHAR=char; using LPTSTR=char*; using tstring=std::string; using HWND=void*;
constexpr int GMEM_MOVEABLE=2, CF_TEXT=1, ERROR_FILENAME_EXCED_RANGE=206;
struct CString { std::string s; int GetLength()const{return int(s.size());} const char* GetString()const{return s.c_str();} };
struct Block { std::vector<char> data; explicit Block(size_t n):data(n){} };
using HGLOBAL=Block*;
static bool allocationFails=false, lockFails=false, openFails=false, emptyFails=false, setFails=false;
static size_t allocated=0, freed=0, openCalls=0, emptyCalls=0, closeCalls=0;
static HWND clipboardOwner=nullptr;
static Block* systemOwned=nullptr;
bool IsWindow(HWND h){return h==reinterpret_cast<HWND>(uintptr_t(1));}
HGLOBAL GlobalAlloc(int,size_t n){if(allocationFails)return nullptr;++allocated;return new Block(n);}
void* GlobalLock(HGLOBAL h){return lockFails?nullptr:h->data.data();}
void GlobalUnlock(HGLOBAL){}
void GlobalFree(HGLOBAL h){++freed;delete h;}
bool OpenClipboard(HWND h){++openCalls;clipboardOwner=h;return !openFails;}
bool EmptyClipboard(){++emptyCalls;return !emptyFails;}
void* SetClipboardData(int,HGLOBAL b){if(setFails)return nullptr;systemOwned=b;return b;}
void CloseClipboard(){++closeCalls;}
static std::string modulePath;
static bool pathFails=false;
static int lastError=0;
DWORD GetModuleFileName(void*,char* b,DWORD n){if(pathFails)return 0;size_t count=std::min(modulePath.size(),size_t(n));std::memcpy(b,modulePath.data(),count);if(count<n)b[count]=0;return DWORD(count);}
void SetLastError(int e){lastError=e;}
'''
utility_probe += extract('trunk/source/WinMFC/WindowsUtils.cpp','bool GetExecutablePath(')+'\n'
utility_probe += extract('trunk/source/WinMFC/WindowsUtils.cpp','bool CopyCString(')+'\n'
utility_probe += r'''
int main(){
    for(size_t n:{1u,511u,512u,1023u,2048u,32767u}){modulePath.assign(n,'a');std::string path;assert(GetExecutablePath(path));assert(path==modulePath);}
    modulePath.assign(32768,'a');std::string path="old";assert(!GetExecutablePath(path)&&path.empty()&&lastError==ERROR_FILENAME_EXCED_RANGE);
    pathFails=true;assert(!GetExecutablePath(path));
    HWND owner=reinterpret_cast<HWND>(uintptr_t(1)); CString text{"abc checksum"};
    assert(!CopyCString(text,nullptr)&&openCalls==0);
    allocationFails=true;assert(!CopyCString(text,owner)&&openCalls==0);allocationFails=false;
    lockFails=true;assert(!CopyCString(text,owner)&&openCalls==0&&allocated==freed);lockFails=false;
    openFails=true;assert(!CopyCString(text,owner)&&emptyCalls==0&&allocated==freed);openFails=false;
    emptyFails=true;assert(!CopyCString(text,owner)&&allocated==freed&&closeCalls==1);emptyFails=false;
    setFails=true;assert(!CopyCString(text,owner)&&allocated==freed&&closeCalls==2);setFails=false;
    assert(CopyCString(text,owner)&&clipboardOwner==owner&&systemOwned&&closeCalls==3);
    assert(std::string(systemOwned->data.data())==text.s);GlobalFree(systemOwned);assert(allocated==freed);
    std::cout<<"Executable path truncation and clipboard ownership/failure cleanup PASS; Win32 APIs are test doubles\n";
}
'''
(out/'windows_utility_probe.cpp').write_text(utility_probe, encoding='utf-8')

version_probe = common + r'''
#include <cstdio>
#define TEXT(s) s
#define _T(s) s
using TCHAR=char; using BYTE=unsigned char; using UINT=uint32_t; using tstring=std::string;
using std::string;
struct VS_FIXEDFILEINFO { DWORD dwSignature, dwStrucVersion, dwFileVersionMS, dwFileVersionLS, reserved[9]; };
static int scenario=0; static size_t getCalls=0;
DWORD GetFileVersionInfoSize(const char*,DWORD*) { return scenario==1 ? 0 : scenario==2 ? 0xFFFFFFFFu : 128; }
bool GetFileVersionInfo(const char*,DWORD,DWORD size,void* p) {
    ++getCalls; if(scenario==3)return false;
    VS_FIXEDFILEINFO v{};v.dwSignature=scenario==7?0:0xFEEF04BD;v.dwFileVersionMS=1u<<16|2;v.dwFileVersionLS=3u<<16|4;
    assert(size==128); std::memcpy(static_cast<BYTE*>(p)+8,&v,sizeof(v));return true;
}
bool VerQueryValue(void* p,const char*,void** out,UINT* length) {
    if(scenario==4)return false;
    *length=scenario==5 ? 1 : sizeof(VS_FIXEDFILEINFO);
    *out=scenario==6 ? static_cast<BYTE*>(p)+120 : static_cast<BYTE*>(p)+8;
    return true;
}
std::string strappendformat(std::string s,const char* f,unsigned a,unsigned b,unsigned c,unsigned d){char v[100];std::snprintf(v,sizeof(v),f,a,b,c,d);return s+v;}
std::string strtotstr(const std::string&s){return s;}
'''
version_probe += extract('trunk/source/WinCommon/WindowsComm.cpp','tstring GetExeFileVersion(')+'\n'
version_probe += r'''
int main(){
    assert(GetExeFileVersion(nullptr).empty());assert(GetExeFileVersion("").empty());
    assert(GetExeFileVersion("fixture.exe")=="1.2.3.4");
    for(scenario=1;scenario<=7;++scenario){size_t before=getCalls;assert(GetExeFileVersion("fixture.exe").empty());if(scenario<3)assert(getCalls==before);}
    std::cout<<"Version API size, return, range, signature validation PASS; Windows version APIs are test doubles\n";
}
'''
(out/'version_api_probe.cpp').write_text(version_probe, encoding='utf-8')

input_token_probe = common + r'''
using HANDLE=void*; using UCHAR=unsigned char;
constexpr int TOKEN_QUERY=8, TokenElevation=1, TokenIntegrityLevel=2;
constexpr DWORD SECURITY_MANDATORY_HIGH_RID=0x3000;
struct TOKEN_ELEVATION { DWORD TokenIsElevated; };
struct MockSid { UCHAR count; DWORD rid; };
struct TOKEN_MANDATORY_LABEL { struct { MockSid* Sid; DWORD Attributes; } Label; };
static int scenario=0;
static MockSid sid{1,0x2000};
static size_t closed=0;
HANDLE GetCurrentProcess(){return reinterpret_cast<HANDLE>(uintptr_t(1));}
bool OpenProcessToken(HANDLE,int,HANDLE* out){if(scenario==1)return false;*out=reinterpret_cast<HANDLE>(uintptr_t(2));return true;}
namespace WinHandleGuard { struct UniqueWinHandle { HANDLE h; explicit UniqueWinHandle(HANDLE v):h(v){} HANDLE get()const{return h;} ~UniqueWinHandle(){++closed;} }; }
bool GetTokenInformation(HANDLE,int type,void* p,DWORD size,DWORD* bytes){
    if(type==TokenElevation){if(scenario==2)return false;*bytes=scenario==3?0:sizeof(TOKEN_ELEVATION);if(p)static_cast<TOKEN_ELEVATION*>(p)->TokenIsElevated=scenario==4;return true;}
    *bytes=scenario==5?0:sizeof(TOKEN_MANDATORY_LABEL);if(!p)return false;if(scenario==6)return false;
    assert(size>=sizeof(TOKEN_MANDATORY_LABEL));auto label=static_cast<TOKEN_MANDATORY_LABEL*>(p);label->Label.Sid=&sid;return true;
}
bool IsValidSid(MockSid*){return scenario!=7;}
UCHAR* GetSidSubAuthorityCount(MockSid* p){return &p->count;}
DWORD* GetSidSubAuthority(MockSid* p,int){return &p->rid;}
'''
input_token_probe += extract('trunk/source/WinMFC/WindowsUtils.cpp','bool CanAcceptUntrustedFileInput(')+'\n'
input_token_probe += r'''
int main(){
    assert(CanAcceptUntrustedFileInput());
    for(scenario=1;scenario<=7;++scenario){size_t before=closed;assert(!CanAcceptUntrustedFileInput());assert(closed==before+(scenario!=1));}
    scenario=0;sid.count=0;assert(!CanAcceptUntrustedFileInput());sid.count=1;
    for(DWORD rid:{0x3000u,0x4000u,0x5000u}){sid.rid=rid;assert(!CanAcceptUntrustedFileInput());}
    for(DWORD rid:{0x1000u,0x2000u}){sid.rid=rid;assert(CanAcceptUntrustedFileInput());}
    std::cout<<"External input token policy fails closed on elevation, high integrity, invalid/query failure; API test doubles PASS\n";
}
'''
(out/'input_token_probe.cpp').write_text(input_token_probe, encoding='utf-8')
(out/'extracted_functions.json').write_text(json.dumps(manifest,indent=2)+'\n', encoding='utf-8')
