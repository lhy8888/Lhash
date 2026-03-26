#ifndef _SHELL_EXPLORER_COMMAND_CORE_H_
#define _SHELL_EXPLORER_COMMAND_CORE_H_

#include <vector>

#include <shlobj.h>
#include <strsafe.h>

#include "Common/strhelper.h"

static inline HRESULT ResolveWindowsAppExePath(PCWSTR pszExecName, LPWSTR pszPath, size_t cchPath)
{
    if (pszExecName == NULL || pszPath == NULL || cchPath == 0)
    {
        return E_INVALIDARG;
    }

    WCHAR szUserPath[MAX_PATH + 20] = { 0 };
    HRESULT hr = SHGetFolderPath(NULL, CSIDL_PROFILE, NULL, 0, szUserPath);
    if (FAILED(hr))
    {
        return hr;
    }

    return StringCchPrintf(pszPath, cchPath, L"%s\\AppData\\Local\\Microsoft\\WindowsApps\\%s",
        szUserPath, pszExecName);
}

static inline HRESULT BuildShellItemCommandLine(IShellItemArray *psia, const sunjwbase::tstring& tstrExecPath, PCWSTR pszAdditionalArgs, sunjwbase::tstring* ptstrExecCmd)
{
    if (psia == NULL || ptstrExecCmd == NULL || tstrExecPath.empty())
    {
        return E_INVALIDARG;
    }

    sunjwbase::tstring tstrExecCmd = L"\"" + tstrExecPath + L"\"";
    if (pszAdditionalArgs != NULL && pszAdditionalArgs[0] != L'\0')
    {
        tstrExecCmd.append(L" ");
        tstrExecCmd.append(pszAdditionalArgs);
    }

    DWORD shellItemCount = 0;
    HRESULT hr = psia->GetCount(&shellItemCount);
    if (FAILED(hr))
    {
        return hr;
    }

    for (DWORD i = 0; i < shellItemCount; i++)
    {
        IShellItem2* psi = NULL;
        hr = GetItemAt(psia, i, IID_PPV_ARGS(&psi));
        if (FAILED(hr))
        {
            continue;
        }

        PWSTR pszPath = NULL;
        hr = psi->GetDisplayName(SIGDN_FILESYSPATH, &pszPath);
        if (SUCCEEDED(hr))
        {
            sunjwbase::tstring tstrItemPath(pszPath);
            CoTaskMemFree(pszPath);

            tstrExecCmd.append(L" \"");
            tstrExecCmd.append(tstrItemPath);
            tstrExecCmd.append(L"\"");
        }

        psi->Release();
    }

    *ptstrExecCmd = tstrExecCmd;
    return S_OK;
}

static inline bool LaunchShellCommandLine(const sunjwbase::tstring& tstrExecPath, const sunjwbase::tstring& tstrExecCmd)
{
    if (tstrExecPath.empty() || tstrExecCmd.empty())
    {
        return false;
    }

    std::vector<WCHAR> cmdLineBuffer(tstrExecCmd.length() + 1, 0);
    wcscpy_s(cmdLineBuffer.data(), cmdLineBuffer.size(), tstrExecCmd.c_str());

    STARTUPINFO sInfo = { 0 };
    sInfo.cb = sizeof(sInfo);
    PROCESS_INFORMATION pInfo = { 0 };

    BOOL bCreated = CreateProcess(tstrExecPath.c_str(), cmdLineBuffer.data(),
        0, 0, TRUE,
        NORMAL_PRIORITY_CLASS,
        0, 0, &sInfo, &pInfo);

    if (bCreated)
    {
        CloseHandle(pInfo.hThread);
        CloseHandle(pInfo.hProcess);
    }

    return bCreated != FALSE;
}

#endif
