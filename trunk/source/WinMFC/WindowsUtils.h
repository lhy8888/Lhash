#ifndef _WINDOWS_UTILS_H_
#define _WINDOWS_UTILS_H_

#include <string>
#include <Windows.h>
#include <atlbase.h>
#include <stdint.h>

#include "Common/strhelper.h"
#include "Common/HashTypes.h"

namespace WindowsUtils
{
	inline sunjwbase::tstring cstrtotstr(const CString& cstring)
	{
		return sunjwbase::tstring(cstring.GetString());
	}

	inline CString tstrtocstr(const sunjwbase::tstring& tstr)
	{
		CString temp;
		temp.SetString(tstr.c_str());
		return temp;
	}

	BOOL IsWindows64();
	BOOL IsLimitedProc();
	BOOL ElevateProcess();
	bool InitializeProcessDllSearchPolicy();
	bool CanAcceptUntrustedFileInput();
	bool GetExecutablePath(sunjwbase::tstring& path);

	bool CopyCString(const CString& cstrToCopy, HWND owner);

	HINSTANCE OpenURL(const TCHAR *pszURL);

	bool FindShlExtDll(const sunjwbase::tstring& exePath, sunjwbase::tstring& dllPath);

	bool RegShellExt(const TCHAR *pszShlDllPath);
	bool UnregShellExt(const TCHAR *pszShlDllPath);

	bool AddContextMenu(const TCHAR *pszExeFullPath);
	bool AddShellExt(const TCHAR *pszExeFullPath);

	bool AddContextMenu();
	bool RemoveContextMenu();
	bool ContextMenuExisted();

}

#endif
