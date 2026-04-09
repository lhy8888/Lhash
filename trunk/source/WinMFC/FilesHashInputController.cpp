#include "stdafx.h"

#include <vector>
#include <shlobj.h>

#include "FilesHashInputController.h"

#include "LegacyCompat/ThreadDataInputAccess.h"
#include "Common/strhelper.h"

using namespace sunjwbase;

FilesHashInputController::FilesHashInputController()
	: m_threadData(NULL),
	m_parentWnd(NULL)
{
}

void FilesHashInputController::Initialize(ThreadData* threadData, CWnd* parentWnd)
{
	m_threadData = threadData;
	m_parentWnd = parentWnd;
}

void FilesHashInputController::LoadCommandLineFiles(LPTSTR filesCmdLine)
{
	if (m_threadData == NULL)
	{
		return;
	}

	TStrVector parameters = ParseFilesCmdLine(filesCmdLine);
	ClearFilePaths();
	ReplaceThreadDataInputFiles(*m_threadData, parameters);
}

BOOL FilesHashInputController::LoadOpenFileDialogSelection(LPCTSTR fileFilter)
{
	if (m_threadData == NULL || m_parentWnd == NULL)
	{
		return FALSE;
	}

	std::vector<TCHAR> nameBuffer(MAX_FILES_NUM * MAX_PATH + 1, 0);
	CFileDialog dlgOpen(TRUE, NULL, NULL, OFN_HIDEREADONLY | OFN_ALLOWMULTISELECT, fileFilter, m_parentWnd, 0);
	dlgOpen.GetOFN().lpstrFile = nameBuffer.data();
	dlgOpen.GetOFN().nMaxFile = MAX_FILES_NUM;
	if (IDOK != dlgOpen.DoModal())
	{
		return FALSE;
	}

	ClearFilePaths();
	for (POSITION pos = dlgOpen.GetStartPosition(); pos != NULL;)
	{
		AppendThreadDataInputFile(*m_threadData, dlgOpen.GetNextPathName(pos).GetString());
	}

	return HasThreadDataInputFiles(*m_threadData) ? TRUE : FALSE;
}

BOOL FilesHashInputController::LoadFolderDialogSelection(LPCTSTR folderDialogTitle, LPCTSTR emptyFolderMessage)
{
	if (m_threadData == NULL || m_parentWnd == NULL)
	{
		return FALSE;
	}

	BROWSEINFO browseInfo = {};
	browseInfo.hwndOwner = m_parentWnd->GetSafeHwnd();
	browseInfo.lpszTitle = folderDialogTitle;
	browseInfo.ulFlags = BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE | BIF_USENEWUI;

	LPITEMIDLIST itemIdList = SHBrowseForFolder(&browseInfo);
	if (itemIdList == NULL)
	{
		return FALSE;
	}

	TCHAR selectedPath[MAX_PATH] = { 0 };
	BOOL hasPath = SHGetPathFromIDList(itemIdList, selectedPath);
	CoTaskMemFree(itemIdList);
	if (!hasPath)
	{
		return FALSE;
	}

	TStrVector files;
	AppendFolderFilesRecursive(selectedPath, files);
	if (files.empty())
	{
		AfxMessageBox(emptyFolderMessage, MB_OK | MB_ICONINFORMATION);
		return FALSE;
	}

	ClearFilePaths();
	ReplaceThreadDataInputFiles(*m_threadData, files);
	return TRUE;
}

BOOL FilesHashInputController::LoadDroppedFiles(HDROP hDropInfo)
{
	if (m_threadData == NULL)
	{
		DragFinish(hDropInfo);
		return FALSE;
	}

	ClearFilePaths();
	uint32_t droppedFileCount = DragQueryFile(hDropInfo, static_cast<UINT>(-1), NULL, 0);
	for (uint32_t index = 0; index < droppedFileCount; ++index)
	{
		tstring tstrDragFilename;
		if (CopyDraggedPath(hDropInfo, index, tstrDragFilename))
		{
			AppendThreadDataInputFile(*m_threadData, tstrDragFilename);
		}
	}

	DragFinish(hDropInfo);
	return HasThreadDataInputFiles(*m_threadData) ? TRUE : FALSE;
}

BOOL FilesHashInputController::LoadCopyDataFiles(const COPYDATASTRUCT* pCopyDataStruct)
{
	if (m_threadData == NULL || !IsValidCopyDataString(pCopyDataStruct))
	{
		return FALSE;
	}

	const TCHAR* szFiles = static_cast<const TCHAR*>(pCopyDataStruct->lpData);
	TStrVector parameters = ParseFilesCmdLine(const_cast<TCHAR*>(szFiles));
	ClearFilePaths();
	ReplaceTrimmedThreadDataInputFiles(*m_threadData, parameters);

	return HasThreadDataInputFiles(*m_threadData) ? TRUE : FALSE;
}

bool FilesHashInputController::CopyDraggedPath(HDROP hDropInfo, UINT index, sunjwbase::tstring& tstrPath)
{
	UINT cchPath = DragQueryFile(hDropInfo, index, NULL, 0);
	if (cchPath == 0)
	{
		return false;
	}

	std::vector<TCHAR> pathBuffer(cchPath + 1, 0);
	if (DragQueryFile(hDropInfo, index, pathBuffer.data(), cchPath + 1) == 0)
	{
		return false;
	}

	tstrPath.assign(pathBuffer.data());
	return true;
}

bool FilesHashInputController::IsValidCopyDataString(const COPYDATASTRUCT* pCopyDataStruct)
{
	if (pCopyDataStruct == NULL || pCopyDataStruct->lpData == NULL)
	{
		return false;
	}

	if (pCopyDataStruct->cbData < sizeof(TCHAR) ||
		(pCopyDataStruct->cbData % sizeof(TCHAR)) != 0)
	{
		return false;
	}

	size_t charCount = pCopyDataStruct->cbData / sizeof(TCHAR);
	const TCHAR* szData = static_cast<const TCHAR*>(pCopyDataStruct->lpData);
	for (size_t i = 0; i < charCount; ++i)
	{
		if (szData[i] == _T('\0'))
		{
			return true;
		}
	}

	return false;
}

TStrVector FilesHashInputController::ParseFilesCmdLine(LPTSTR filesCmdLine)
{
	TStrVector parameters;
	if (filesCmdLine == NULL || filesCmdLine[0] == _T('\0'))
	{
		return parameters;
	}

#if defined(UNICODE) || defined(_UNICODE)
	int argc = 0;
	LPWSTR* argv = CommandLineToArgvW(filesCmdLine, &argc);
	if (argv == NULL)
	{
		return parameters;
	}

	for (int i = 0; i < argc; ++i)
	{
		if (argv[i] != NULL && argv[i][0] != L'\0')
		{
			parameters.push_back(argv[i]);
		}
	}
	LocalFree(argv);
#else
	std::wstring wstrCmdLine = strtowstr(std::string(filesCmdLine));
	int argc = 0;
	LPWSTR* argv = CommandLineToArgvW(wstrCmdLine.c_str(), &argc);
	if (argv == NULL)
	{
		return parameters;
	}

	for (int i = 0; i < argc; ++i)
	{
		if (argv[i] != NULL && argv[i][0] != L'\0')
		{
			parameters.push_back(wstrtostr(argv[i]));
		}
	}
	LocalFree(argv);
#endif

	return parameters;
}

void FilesHashInputController::AppendFolderFilesRecursive(const sunjwbase::tstring& folderPath, TStrVector& files)
{
	if (folderPath.empty() || files.size() >= MAX_FILES_NUM)
	{
		return;
	}

	sunjwbase::tstring searchPath = folderPath;
	if (searchPath[searchPath.length() - 1] != _T('\\') &&
		searchPath[searchPath.length() - 1] != _T('/'))
	{
		searchPath += _T("\\");
	}
	searchPath += _T("*");

	WIN32_FIND_DATA findData = {};
	HANDLE hFind = FindFirstFile(searchPath.c_str(), &findData);
	if (hFind == INVALID_HANDLE_VALUE)
	{
		return;
	}

	do
	{
		if (_tcscmp(findData.cFileName, _T(".")) == 0 ||
			_tcscmp(findData.cFileName, _T("..")) == 0)
		{
			continue;
		}

		sunjwbase::tstring fullPath = folderPath;
		if (fullPath[fullPath.length() - 1] != _T('\\') &&
			fullPath[fullPath.length() - 1] != _T('/'))
		{
			fullPath += _T("\\");
		}
		fullPath += findData.cFileName;

		if ((findData.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0)
		{
			continue;
		}

		if ((findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0)
		{
			AppendFolderFilesRecursive(fullPath, files);
		}
		else
		{
			files.push_back(fullPath);
			if (files.size() >= MAX_FILES_NUM)
			{
				break;
			}
		}
	} while (FindNextFile(hFind, &findData) != FALSE);

	FindClose(hFind);
}

void FilesHashInputController::ClearFilePaths()
{
	if (m_threadData == NULL)
	{
		return;
	}

	ResetThreadDataInputFiles(*m_threadData);
}
