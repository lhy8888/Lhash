// LHashShellExt.cpp : CLHashShellExt 的实现

#include "stdafx.h"
#include "LHashShellExt.h"

#include <string>
#include <vector>
#include <atlconv.h>

#include "Psapi.h"

#include "Common/strhelper.h"
#include "WinMFC/ShellExtComm.h"
#include "WinCommon/WinHandleGuard.h"
#include "WinCommon/WindowsStrings.h"
#include "LHashShlExtStringsBase.h"
#include "LHashShlExtStringsZHCN.h"

using namespace std;
using namespace sunjwbase;
using namespace WindowsStrings;

#define MAX_FILE_CMD 32768

namespace
{
	bool CopyDraggedPath(HDROP hDrop, UINT index, sunjwbase::tstring& tstrPath)
	{
		UINT cchPath = DragQueryFile(hDrop, index, NULL, 0);
		if (cchPath == 0)
		{
			return false;
		}

		std::vector<TCHAR> pathBuffer(cchPath + 1, 0);
		if (DragQueryFile(hDrop, index, pathBuffer.data(), cchPath + 1) == 0)
		{
			return false;
		}

		tstrPath.assign(pathBuffer.data());
		return true;
	}
}

// CLHashShellExt
CLHashShellExt::CLHashShellExt()
{
	RegisterStringsForLang(-1, new LHashShlExtStringsBase());
	RegisterStringsForLang(2052, new LHashShlExtStringsZHCN());
}

// CLHashShellExt
HRESULT CLHashShellExt::Initialize(LPCITEMIDLIST pidlFolder,
								  LPDATAOBJECT pDataObj,
								  HKEY hProgID)
{
	FORMATETC fmt = { CF_HDROP, NULL, DVASPECT_CONTENT,
					  -1, TYMED_HGLOBAL };
	STGMEDIUM stg = { TYMED_HGLOBAL };
	HDROP     hDrop;

	// Look for CF_HDROP data in the data object. If there
	// is no such data, return an error back to Explorer.
	if(FAILED(pDataObj->GetData(&fmt, &stg)))
		return E_INVALIDARG;

	// Get a pointer to the actual data.
	hDrop = (HDROP)GlobalLock(stg.hGlobal);

	// Make sure it worked.
	if(NULL == hDrop)
	{
		ReleaseStgMedium(&stg);
		return E_INVALIDARG;
	}

	// Sanity check – make sure there is at least one filename.
	UINT uNumFiles = DragQueryFile(hDrop, 0xFFFFFFFF, NULL, 0);
	HRESULT hr = S_OK;

	if(0 == uNumFiles)
    {
		GlobalUnlock(stg.hGlobal);
		ReleaseStgMedium(&stg);
		return E_INVALIDARG;
    }

	// Get the name of files
	for(UINT i = 0; i < uNumFiles; i++)
	{
		tstring tstrDragFilename;
		if(!CopyDraggedPath(hDrop, i, tstrDragFilename))
		{
			hr = E_INVALIDARG;
			continue;
		}

		m_pathList.push_back(tstrDragFilename);
	}

	GlobalUnlock(stg.hGlobal);
	ReleaseStgMedium(&stg);

	// Try to find LHash
	CRegKey key;
	LPCTSTR lpszKeyName = SHELL_EXT_REGESTRY;
	LONG lResult;
	lResult = key.Open(HKEY_CLASSES_ROOT, lpszKeyName, KEY_READ);
	if(lResult != ERROR_SUCCESS)
		return E_INVALIDARG;

	TCHAR szPath[MAX_PATH + 1] = {};
	ULONG nChars = static_cast<ULONG>(_countof(szPath));
	if (key.QueryStringValue(SHELL_EXT_EXEPATH, szPath, &nChars) != ERROR_SUCCESS)
	{
		key.Close();
		return E_INVALIDARG;
	}
	key.Close();

	m_LHashPath = szPath;
	if(m_LHashPath == _T(""))
		hr = E_INVALIDARG;

	return hr;
}

HRESULT CLHashShellExt::QueryContextMenu(
						HMENU hmenu, UINT uMenuIndex, UINT uidFirstCmd,
						UINT uidLastCmd, UINT uFlags)
{
	// If the flags include CMF_DEFAULTONLY then we shouldn't do anything.
	if (uFlags & CMF_DEFAULTONLY)
		return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);

	LPCTSTR pszMenuItem = GetStringByKey(SHELL_EXT_ITEM);

	InsertMenu(hmenu, uMenuIndex, MF_BYPOSITION,
               uidFirstCmd, pszMenuItem);

	return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 1);
}

HRESULT CLHashShellExt::GetCommandString(
						  UINT_PTR idCmd, UINT uFlags, UINT* pwReserved,
						  LPSTR pszName, UINT cchMax)
{
	USES_CONVERSION;

	// Check idCmd, it must be 0 since we have only one menu item.
	if(0 != idCmd)
		return E_INVALIDARG;

	// If Explorer is asking for a help string, copy our string into the
	// supplied buffer.
	if(uFlags & GCS_HELPTEXT)
    {
		LPCTSTR szText = _T("Using LHash to hash selected file(s).");

		if(uFlags & GCS_UNICODE)
		{
			// We need to cast pszName to a Unicode string, and then use the
			// Unicode string copy API.
			lstrcpynW((LPWSTR)pszName, T2CW(szText), cchMax);
		}
		else
		{
			// Use the ANSI string copy API to return the help string.
			lstrcpynA(pszName, T2CA(szText), cchMax);
		}

		return S_OK;
    }

  return E_INVALIDARG;
}

HRESULT CLHashShellExt::InvokeCommand(LPCMINVOKECOMMANDINFO pCmdInfo)
{
	// If lpVerb really points to a string, ignore this function call and bail out.
	if(0 != HIWORD(pCmdInfo->lpVerb))
		return E_INVALIDARG;

	// Get the command index - the only valid one is 0.
	switch (LOWORD(pCmdInfo->lpVerb))
    {
    case 0:
		{
			HWND hWndLHash = FindLHashWindow();
			if (hWndLHash == NULL) // Launch and calculate...
				return LaunchLHashByCommandLine(pCmdInfo, TRUE);

			// Found LHash, send message.
			SendFilesToLHash(pCmdInfo, hWndLHash);
			return S_OK;
		}
		break;

    default:
		return E_INVALIDARG;
		break;
    }

	return S_OK;
}

HRESULT CLHashShellExt::LaunchLHashByCommandLine(LPCMINVOKECOMMANDINFO pCmdInfo, BOOL bWithFiles)
{
	tstring tstrLHashPath = m_LHashPath;
	// LHash.exe
	tstring tstrCmd = _T("\"") + tstrLHashPath + _T("\"");

	if (bWithFiles == TRUE)
	{
		// Files...
		for(TstrList::const_iterator itr = m_pathList.begin();
			itr != m_pathList.end();
			++itr)
		{
			tstrCmd.append(_T(" "));
			tstrCmd.append(_T("\""));
			tstrCmd.append(*itr);
			tstrCmd.append(_T("\""));
		}
	}

	size_t cmdLen = tstrCmd.length() + 1;
	if(cmdLen > MAX_FILE_CMD)
	{
		MessageBox(pCmdInfo->hwnd,
			GetStringByKey(SHELL_EXT_TOO_MANY_FILES),
			GetStringByKey(SHELL_EXT_TOO_MANY_FILES),
			MB_OK | MB_ICONWARNING);
		return S_OK;
	}

	std::vector<TCHAR> cmdBuffer(cmdLen, static_cast<TCHAR>(0));
#if defined(UNICODE) || defined(_UNICODE)
	wcscpy_s(cmdBuffer.data(), cmdBuffer.size(), tstrCmd.c_str());
#else
	strcpy_s(cmdBuffer.data(), cmdBuffer.size(), tstrCmd.c_str());
#endif

	STARTUPINFO sInfo = {0};
	sInfo.cb = sizeof(sInfo);
	PROCESS_INFORMATION pInfo = {0};

	BOOL bCreated = CreateProcess(tstrLHashPath.c_str(), cmdBuffer.data(),
		0, 0, FALSE,
		NORMAL_PRIORITY_CLASS,
		0, 0, &sInfo, &pInfo);

	if (!bCreated)
	{
		return HRESULT_FROM_WIN32(GetLastError());
	}

	WinHandleGuard::UniqueWinHandle threadHandle(pInfo.hThread);
	WinHandleGuard::UniqueWinHandle processHandle(pInfo.hProcess);

	return S_OK;
}

HWND CLHashShellExt::FindLHashWindow()
{
	HWND hWndLHash = NULL;
	hWndLHash = FindWindow(_T("#32770"), _T("LHash"));
	if (hWndLHash == NULL)
		return NULL;

	DWORD dwPidLHash = 0;
	GetWindowThreadProcessId(hWndLHash, &dwPidLHash);
	WinHandleGuard::UniqueWinHandle hProcLHash(OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, dwPidLHash));
	if (!hProcLHash.isValid())
		return NULL;

	std::vector<TCHAR> exePath(32768, 0);
	DWORD cchExecutable = (DWORD)exePath.size();
	if (!QueryFullProcessImageName(hProcLHash.get(), 0, exePath.data(), &cchExecutable))
	{
		return NULL;
	}


	tstring tstrProcLHashPath(exePath.data());
	if (_tcsicmp(tstrProcLHashPath.c_str(), m_LHashPath.c_str()) == 0)
		return hWndLHash;

	return NULL;
}

void CLHashShellExt::SendFilesToLHash(LPCMINVOKECOMMANDINFO pCmdInfo, HWND hWndLHash)
{
	if (hWndLHash == NULL)
		return;

	tstring tstrFiles;
	for(TstrList::const_iterator itr = m_pathList.begin();
		itr != m_pathList.end();
		++itr)
	{
		tstrFiles.append(_T(" "));
		tstrFiles.append(_T("\""));
		tstrFiles.append(*itr);
		tstrFiles.append(_T("\""));
	}

	size_t cmdLen = tstrFiles.length() + 1;
	if(cmdLen > MAX_FILE_CMD)
	{
		MessageBox(pCmdInfo->hwnd,
			GetStringByKey(SHELL_EXT_TOO_MANY_FILES),
			GetStringByKey(SHELL_EXT_TOO_MANY_FILES),
			MB_OK | MB_ICONWARNING);
		return;
	}

	COPYDATASTRUCT cdFiles;
	cdFiles.dwData = 0;
	cdFiles.cbData = (DWORD)(cmdLen * sizeof(TCHAR));
	cdFiles.lpData = (PVOID)(tstrFiles.c_str());

	SendMessage(hWndLHash,
				WM_COPYDATA,
				(WPARAM)pCmdInfo->hwnd,
				(LPARAM)&cdFiles);
}


