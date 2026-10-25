#include "stdafx.h"

#include "WindowsUtils.h"

#include <stdio.h>
#include <string>
#include <vector>
#include <limits>

#include "Common/HashInputPolicy.h"

#include <atlbase.h>

#include "Common/strhelper.h"
#include "Common/Utils.h"
#include "WinCommon/WinHandleGuard.h"
#include "WinCommon/WindowsStrings.h"
#include "ShellExtComm.h"

using namespace std;
using namespace sunjwbase;

typedef HRESULT(__stdcall *LPFN_DllRegisterServer)(void);
typedef HRESULT(__stdcall *LPFN_DllUnregisterServer)(void);

namespace
{
	typedef BOOL(WINAPI *LPFN_SetDefaultDllDirectories)(DWORD directoryFlags);
	bool IsAcceptableContextMenuDeleteResult(LONG deleteResult)
	{
		return deleteResult == ERROR_SUCCESS || deleteResult == ERROR_FILE_NOT_FOUND;
	}
	LPFN_SetDefaultDllDirectories ResolveSetDefaultDllDirectories()
	{
		HMODULE kernel32Module = GetModuleHandle(_T("kernel32.dll"));
		if (kernel32Module == NULL)
			return NULL;
		return reinterpret_cast<LPFN_SetDefaultDllDirectories>(
			GetProcAddress(kernel32Module, "SetDefaultDllDirectories"));
	}
	HMODULE LoadLibraryWithSecureSearchPath(const TCHAR *pszDllPath)
	{
		if (pszDllPath == NULL || pszDllPath[0] == _T('\0'))
			return NULL;
		if (ResolveSetDefaultDllDirectories() != NULL)
		{
			HMODULE hModule = LoadLibraryEx(
				pszDllPath,
				NULL,
				LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
			if (hModule != NULL || GetLastError() != ERROR_INVALID_PARAMETER)
				return hModule;
		}
		return LoadLibraryEx(pszDllPath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
	}
}
namespace WindowsUtils
{
	bool InitializeProcessDllSearchPolicy()
	{
		LPFN_SetDefaultDllDirectories setDefaultDllDirectories = ResolveSetDefaultDllDirectories();
		if (setDefaultDllDirectories != NULL)
			return setDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS) == TRUE;
		return SetDllDirectory(_T("")) == TRUE;
	}

	BOOL IsWindows64()
	{
		SYSTEM_INFO systemInfo = { 0 };
		GetNativeSystemInfo(&systemInfo);

		switch(systemInfo.wProcessorArchitecture)
		{
		case PROCESSOR_ARCHITECTURE_AMD64:
		case PROCESSOR_ARCHITECTURE_IA64:
		case PROCESSOR_ARCHITECTURE_ARM64:
			return TRUE;
		default:
			return FALSE;
		}
	}

	BOOL IsLimitedProc()
	{
		HRESULT hResult = E_FAIL; // assume an error occurred
		HANDLE hToken = NULL;
		TOKEN_ELEVATION_TYPE tet;

		if (!OpenProcessToken(GetCurrentProcess(),
							TOKEN_QUERY,
							&hToken))
		{
			return TRUE;
		}

		WinHandleGuard::UniqueWinHandle tokenHandle(hToken);

		DWORD dwReturnLength = 0;

		if (GetTokenInformation(
					hToken,
					TokenElevationType,
					&tet,
					sizeof(tet),
					&dwReturnLength))
		{
				if(dwReturnLength != sizeof(tet))
					return TRUE;
				hResult = S_OK;
		}


		if(hResult != S_OK)
			return TRUE;

		if(tet == TokenElevationTypeLimited)
		{
			return TRUE;
		}
		else if(tet == TokenElevationTypeDefault &&
				!IsUserAnAdmin())
		{
			return TRUE;
		}

		return FALSE;
	}

	bool GetExecutablePath(tstring& path)
	{
		path.clear();
		for (size_t capacity = 512; capacity <= 32768; capacity *= 2)
		{
			std::vector<TCHAR> buffer(capacity, 0);
			DWORD length = GetModuleFileName(NULL, buffer.data(), static_cast<DWORD>(buffer.size()));
			if (length == 0) return false;
			if (length < buffer.size())
			{
				path.assign(buffer.data(), length);
				return true;
			}
		}
		SetLastError(ERROR_FILENAME_EXCED_RANGE);
		return false;
	}

	bool CanAcceptUntrustedFileInput()
	{
		HANDLE rawToken = NULL;
		if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &rawToken)) return false;
		WinHandleGuard::UniqueWinHandle token(rawToken);
		TOKEN_ELEVATION elevation = {};
		DWORD bytes = 0;
		if (!GetTokenInformation(token.get(), TokenElevation, &elevation, sizeof(elevation), &bytes) ||
			bytes != sizeof(elevation) || elevation.TokenIsElevated != 0) return false;
		GetTokenInformation(token.get(), TokenIntegrityLevel, NULL, 0, &bytes);
		if (bytes < sizeof(TOKEN_MANDATORY_LABEL) || bytes > 65536) return false;
		std::vector<unsigned char> buffer(bytes);
		if (!GetTokenInformation(token.get(), TokenIntegrityLevel, buffer.data(), bytes, &bytes)) return false;
		const TOKEN_MANDATORY_LABEL* label = reinterpret_cast<const TOKEN_MANDATORY_LABEL*>(buffer.data());
		if (!IsValidSid(label->Label.Sid)) return false;
		const UCHAR count = *GetSidSubAuthorityCount(label->Label.Sid);
		return count != 0 && *GetSidSubAuthority(label->Label.Sid, count - 1) < SECURITY_MANDATORY_HIGH_RID;
	}

	BOOL ElevateProcess()
	{
		tstring executable;
		if (!GetExecutablePath(executable)) return FALSE;
		SHELLEXECUTEINFO sei = { sizeof(SHELLEXECUTEINFO) };
		sei.lpVerb = TEXT("runas");
		sei.lpFile = executable.c_str();
		sei.nShow = SW_SHOWNORMAL;
		return ShellExecuteEx(&sei);
	}

	bool CopyCString(const CString& cstrToCopy, HWND owner)
	{
		if (owner == NULL || !::IsWindow(owner)) return false;
		const size_t chars = static_cast<size_t>(cstrToCopy.GetLength()) + 1;
		if (chars > (std::numeric_limits<size_t>::max)() / sizeof(TCHAR)) return false;
		const size_t bytes = chars * sizeof(TCHAR);
		HGLOBAL block = GlobalAlloc(GMEM_MOVEABLE, bytes);
		if (block == NULL) return false;
		LPTSTR destination = static_cast<LPTSTR>(GlobalLock(block));
		if (destination == NULL)
		{
			GlobalFree(block);
			return false;
		}
		memcpy(destination, cstrToCopy.GetString(), bytes);
		GlobalUnlock(block);
		// Prepare the data before opening/emptying the clipboard so allocation
		// errors leave the previous clipboard contents intact.
		if (!::OpenClipboard(owner))
		{
			GlobalFree(block);
			return false;
		}
		bool copied = false;
		if (::EmptyClipboard())
		{
#if defined(UNICODE) || defined(_UNICODE)
			copied = ::SetClipboardData(CF_UNICODETEXT, block) != NULL;
#else
			copied = ::SetClipboardData(CF_TEXT, block) != NULL;
#endif
		}
		if (!copied) GlobalFree(block);
		::CloseClipboard();
		return copied;
	}

	HINSTANCE OpenURL(const TCHAR *pszURL)
	{
		return ShellExecute(NULL, _T("open"), pszURL,
			NULL, NULL, SW_SHOW);
	}

	bool FindShlExtDll(const tstring& exePath, tstring& dllPath)
	{
		dllPath.clear();
		const tstring::size_type separator = exePath.find_last_of(_T("\\/"));
		if (separator == tstring::npos) return false;
		const tstring directory = exePath.substr(0, separator + 1);
		const TCHAR* names[] = { IsWindows64() ? _T("LHashShlExt64.dll") : _T("LHashShlExt.dll"), _T("LHashShlExt.dll") };
		for (const TCHAR* name : names)
		{
			const tstring candidate = directory + name;
			DWORD attributes = GetFileAttributes(candidate.c_str());
			if (attributes != INVALID_FILE_ATTRIBUTES && (attributes & FILE_ATTRIBUTE_DIRECTORY) == 0)
			{
				dllPath = candidate;
				return true;
			}
		}
		return false;
	}

	bool RegShellExt(const TCHAR *pszShlDllPath)
	{
		WinHandleGuard::UniqueModuleHandle hModule(LoadLibraryWithSecureSearchPath(pszShlDllPath));
		if(hModule.isValid())
		{
			LPFN_DllRegisterServer fnDllRegSvr = NULL;
			fnDllRegSvr = (LPFN_DllRegisterServer) GetProcAddress(hModule.get(), "DllRegisterServer");
			if(fnDllRegSvr != NULL)
			{
				bool bRet = (fnDllRegSvr() == S_OK);
				return bRet;
			}

		}

		return false;
	}

	bool UnregShellExt(const TCHAR *pszShlDllPath)
	{
		WinHandleGuard::UniqueModuleHandle hModule(LoadLibraryWithSecureSearchPath(pszShlDllPath));
		if(hModule.isValid())
		{
			LPFN_DllUnregisterServer fnDllUnregSvr = NULL;
			fnDllUnregSvr = (LPFN_DllUnregisterServer) GetProcAddress(hModule.get(), "DllUnregisterServer");
			if(fnDllUnregSvr != NULL)
			{
				bool bRet = (fnDllUnregSvr() == S_OK);
				return bRet;
			}

		}

		return false;
	}


	bool AddContextMenu(const TCHAR *pszExeFullPath)
	{
		CRegKey key;
		LONG lResult;
		LPCTSTR lpszKeyName = NULL;
		if (WindowsStrings::GetCurrentUILang() == 2052)
			lpszKeyName = CONTEXT_MENU_REGESTRY_ZH_CN;
		else
			lpszKeyName = CONTEXT_MENU_REGESTRY_EN_US;

		// 创建目录
		lResult = key.Create(HKEY_CLASSES_ROOT, lpszKeyName);
		if(lResult != ERROR_SUCCESS)
			return false; // 失败

		// 打开
		lResult = key.Open(HKEY_CLASSES_ROOT, lpszKeyName, KEY_ALL_ACCESS);
		if(lResult != ERROR_SUCCESS)
			return false;

		tstring command;
		if (pszExeFullPath == NULL || !TryBuildHashShellCommand(tstring(pszExeFullPath), command)) return false;
		lResult = key.SetStringValue(NULL, command.c_str());
		key.Close();

		if(lResult == ERROR_SUCCESS)
			return true;
		else
			return false;
	}

	bool AddShellExt(const TCHAR *pszExeFullPath)
	{
		CRegKey key;
		LONG lResult;
		LPCTSTR lpszKeyName = SHELL_EXT_REGESTRY;

		// 创建目录
		lResult = key.Create(HKEY_CLASSES_ROOT, lpszKeyName);
		if(lResult != ERROR_SUCCESS)
			return false; // 失败

		// 打开
		lResult = key.Open(HKEY_CLASSES_ROOT, lpszKeyName, KEY_ALL_ACCESS);
		if(lResult != ERROR_SUCCESS)
			return false;

		// 成功打开
		LPCTSTR pszUuid = SHELL_EXT_UUID;
		LPCTSTR pszExePath = pszExeFullPath;

		lResult = key.SetStringValue(NULL, pszUuid);
		lResult |= key.SetStringValue(SHELL_EXT_EXEPATH, pszExePath);
		key.Close();

		if(lResult == ERROR_SUCCESS)
			return true;
		else
			return false;
	}

	bool AddContextMenu()
	{
		tstring executable, dllPath;
		if (!GetExecutablePath(executable)) return false;
		if (FindShlExtDll(executable, dllPath) && RegShellExt(dllPath.c_str()) && AddShellExt(executable.c_str()))
			return true;
		return AddContextMenu(executable.c_str());
	}

	bool RemoveContextMenu()
	{
		tstring executable, dllPath;
		if (!GetExecutablePath(executable)) return false;
		if (FindShlExtDll(executable, dllPath) && !UnregShellExt(dllPath.c_str())) return false;

		CRegKey keyShell, keyShellEx;
		LPCTSTR lpszKeyShellName = _T("*\\shell\\");
		LPCTSTR lpszKeyShellExName = _T("*\\shellex\\ContextMenuHandlers\\");
		LONG lResShell, lResShellEx;

		// 打开
		lResShell = keyShell.Open(HKEY_CLASSES_ROOT, lpszKeyShellName, KEY_ALL_ACCESS);
		lResShellEx = keyShellEx.Open(HKEY_CLASSES_ROOT, lpszKeyShellExName, KEY_ALL_ACCESS);
		if(lResShell != ERROR_SUCCESS &&
			lResShellEx != ERROR_SUCCESS)
			return false;

		bool deleteSucceeded = true;

		// Try to delete context menu
		if(lResShell == ERROR_SUCCESS)
		{
			deleteSucceeded = deleteSucceeded && IsAcceptableContextMenuDeleteResult(keyShell.RecurseDeleteKey(CONTEXT_MENU_ITEM_EN_US));
			deleteSucceeded = deleteSucceeded && IsAcceptableContextMenuDeleteResult(keyShell.RecurseDeleteKey(CONTEXT_MENU_ITEM_ZH_CN));
			keyShell.Close();
		}

		// Try to delete shell extension
		if(lResShellEx == ERROR_SUCCESS)
		{
			deleteSucceeded = deleteSucceeded && IsAcceptableContextMenuDeleteResult(keyShellEx.RecurseDeleteKey(_T("LHashShellExt")));
			keyShellEx.Close();
		}

		if(!deleteSucceeded)
			return false;
		else
			return true;
	}

	bool ContextMenuExisted()
	{
		CRegKey keyCtxMenuBase, keyCtxMenuZh, keyShlExt;

		LPCTSTR lpszCtxMenuKeyNameBase = CONTEXT_MENU_REGESTRY_EN_US;
		LPCTSTR lpszCtxMenuKeyNameZh = CONTEXT_MENU_REGESTRY_ZH_CN;
		LPCTSTR lpszShlExtKeyName = SHELL_EXT_REGESTRY;

		LONG lResCtxMenuBase;
		LONG lResCtxMenuZh;
		LONG lResShlExt;

		// 打开
		lResCtxMenuBase = keyCtxMenuBase.Open(HKEY_CLASSES_ROOT, lpszCtxMenuKeyNameBase, KEY_READ);
		lResCtxMenuZh = keyCtxMenuZh.Open(HKEY_CLASSES_ROOT, lpszCtxMenuKeyNameZh, KEY_READ);
		lResShlExt = keyShlExt.Open(HKEY_CLASSES_ROOT, lpszShlExtKeyName, KEY_READ);

		if(lResCtxMenuBase != ERROR_SUCCESS &&
			lResCtxMenuZh != ERROR_SUCCESS &&
			lResShlExt != ERROR_SUCCESS)
			return false;

		if(lResCtxMenuBase == ERROR_SUCCESS)
			keyCtxMenuBase.Close();
		if(lResCtxMenuZh == ERROR_SUCCESS)
			keyCtxMenuZh.Close();
		if(lResShlExt == ERROR_SUCCESS)
			keyShlExt.Close();

		return true;
	}

}



