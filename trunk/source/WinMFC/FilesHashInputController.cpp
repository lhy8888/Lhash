#include "stdafx.h"

#include <vector>
#include <deque>
#include <shlobj.h>

#include "FilesHashInputController.h"

#include "Adapters/ThreadDataBridge/ThreadDataInputAccess.h"
#include "Common/strhelper.h"

using namespace sunjwbase;

namespace
{
	const size_t COPYDATA_COMMAND_CHAR_LIMIT = 32768;

	FileLoadOutcome MakeFileLoadOutcome(
		FileLoadResult result,
		size_t loadedCount,
		size_t limit,
		bool hasRequestedCount = false,
		size_t requestedCount = 0)
	{
		FileLoadOutcome outcome;
		outcome.result = result;
		outcome.loadedCount = loadedCount;
		outcome.limit = limit;
		outcome.hasRequestedCount = hasRequestedCount;
		outcome.requestedCount = requestedCount;
		return outcome;
	}
}

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

FileLoadOutcome FilesHashInputController::LoadOpenFileDialogSelection(LPCTSTR fileFilter)
{
	if (m_threadData == NULL || m_parentWnd == NULL)
	{
		return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
	}

	std::vector<TCHAR> nameBuffer((kMaxHashFilesPerSession * MAX_PATH) + 1, 0);
	CFileDialog dlgOpen(TRUE, NULL, NULL, OFN_HIDEREADONLY | OFN_ALLOWMULTISELECT, fileFilter, m_parentWnd, 0);
	dlgOpen.GetOFN().lpstrFile = nameBuffer.data();
	dlgOpen.GetOFN().nMaxFile = static_cast<DWORD>(nameBuffer.size());
	if (IDOK != dlgOpen.DoModal())
	{
		return MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession);
	}

	ClearFilePaths();
	size_t loadedCount = 0;
	for (POSITION pos = dlgOpen.GetStartPosition(); pos != NULL;)
	{
		AppendThreadDataInputFile(*m_threadData, dlgOpen.GetNextPathName(pos).GetString());
		++loadedCount;
	}

	if (!HasThreadDataInputFiles(*m_threadData))
	{
		return MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession);
	}

	if (loadedCount >= kMaxHashFilesPerSession)
	{
		return MakeFileLoadOutcome(FileLoadResult::SuccessPossiblyTruncated, loadedCount, kMaxHashFilesPerSession);
	}

	return MakeFileLoadOutcome(FileLoadResult::Success, loadedCount, kMaxHashFilesPerSession);
}

FileLoadOutcome FilesHashInputController::LoadFolderDialogSelection(LPCTSTR folderDialogTitle, LPCTSTR emptyFolderMessage)
{
	if (m_threadData == NULL || m_parentWnd == NULL)
	{
		return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
	}

	BROWSEINFO browseInfo = {};
	browseInfo.hwndOwner = m_parentWnd->GetSafeHwnd();
	browseInfo.lpszTitle = folderDialogTitle;
	browseInfo.ulFlags = BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE | BIF_USENEWUI;

	LPITEMIDLIST itemIdList = SHBrowseForFolder(&browseInfo);
	if (itemIdList == NULL)
	{
		return MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession);
	}

	TCHAR selectedPath[MAX_PATH] = { 0 };
	BOOL hasPath = SHGetPathFromIDList(itemIdList, selectedPath);
	CoTaskMemFree(itemIdList);
	if (!hasPath)
	{
		return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
	}

	FolderScanOutcome scanOutcome = AppendFolderFilesRecursive(selectedPath);
	if (scanOutcome.files.empty())
	{
		AfxMessageBox(emptyFolderMessage, MB_OK | MB_ICONINFORMATION);
		return MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession);
	}

	ClearFilePaths();
	ReplaceThreadDataInputFiles(*m_threadData, scanOutcome.files);
	return MakeFileLoadOutcome(
		scanOutcome.truncated ? FileLoadResult::SuccessWithTruncation : FileLoadResult::Success,
		scanOutcome.files.size(),
		scanOutcome.limit);
}

FileLoadOutcome FilesHashInputController::LoadDroppedFiles(HDROP hDropInfo)
{
	if (m_threadData == NULL)
	{
		DragFinish(hDropInfo);
		return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
	}

	UINT droppedFileCount = DragQueryFile(hDropInfo, 0xFFFFFFFF, NULL, 0);
	if (droppedFileCount == 0)
	{
		DragFinish(hDropInfo);
		return MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession);
	}

	if (static_cast<size_t>(droppedFileCount) > kMaxHashFilesPerSession)
	{
		DragFinish(hDropInfo);
		return MakeFileLoadOutcome(FileLoadResult::RejectedOverLimit, 0, kMaxHashFilesPerSession, true, droppedFileCount);
	}

	ClearFilePaths();
	size_t loadedCount = 0;
	for (UINT index = 0; index < droppedFileCount; ++index)
	{
		tstring tstrDragFilename;
		if (CopyDraggedPath(hDropInfo, index, tstrDragFilename))
		{
			AppendThreadDataInputFile(*m_threadData, tstrDragFilename);
			++loadedCount;
		}
	}

	DragFinish(hDropInfo);
	return HasThreadDataInputFiles(*m_threadData)
		? MakeFileLoadOutcome(FileLoadResult::Success, loadedCount, kMaxHashFilesPerSession, true, droppedFileCount)
		: MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession, true, droppedFileCount);
}

FileLoadOutcome FilesHashInputController::LoadCopyDataFiles(const COPYDATASTRUCT* pCopyDataStruct)
{
	if (m_threadData == NULL || !IsValidCopyDataString(pCopyDataStruct))
	{
		return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
	}

	const TCHAR* szFiles = static_cast<const TCHAR*>(pCopyDataStruct->lpData);
	TStrVector parameters = ParseFilesCmdLine(const_cast<TCHAR*>(szFiles));
	if (parameters.empty())
	{
		return MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession, true, parameters.size());
	}

	if (parameters.size() > kMaxHashFilesPerSession)
	{
		return MakeFileLoadOutcome(FileLoadResult::RejectedOverLimit, 0, kMaxHashFilesPerSession, true, parameters.size());
	}

	ClearFilePaths();
	ReplaceTrimmedThreadDataInputFiles(*m_threadData, parameters);

	return HasThreadDataInputFiles(*m_threadData)
		? MakeFileLoadOutcome(FileLoadResult::Success, GetThreadDataFileCount(*m_threadData), kMaxHashFilesPerSession, true, parameters.size())
		: MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession, true, parameters.size());
}

size_t FilesHashInputController::GetCopyDataCommandCharLimit()
{
	return COPYDATA_COMMAND_CHAR_LIMIT;
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
	if (charCount == 0 || charCount > GetCopyDataCommandCharLimit())
	{
		return false;
	}

	const TCHAR* szData = static_cast<const TCHAR*>(pCopyDataStruct->lpData);
	if (szData[charCount - 1] != _T('\0'))
	{
		return false;
	}

	bool sawTerminator = false;
	for (size_t i = 0; i < charCount; ++i)
	{
		if (szData[i] == _T('\0'))
		{
			sawTerminator = true;
			continue;
		}

		if (sawTerminator)
		{
			return false;
		}
	}

	return sawTerminator;
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

FolderScanOutcome FilesHashInputController::AppendFolderFilesRecursive(const sunjwbase::tstring& folderPath)
{
	FolderScanOutcome scanOutcome;
	if (folderPath.empty())
	{
		return scanOutcome;
	}

	std::deque<sunjwbase::tstring> pendingFolders;
	pendingFolders.push_back(folderPath);
	bool truncated = false;

	while (!pendingFolders.empty() && !truncated)
	{
		sunjwbase::tstring currentFolder = pendingFolders.back();
		pendingFolders.pop_back();
		if (currentFolder.empty())
		{
			continue;
		}

		sunjwbase::tstring searchPath = currentFolder;
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
			continue;
		}

		do
		{
			if (_tcscmp(findData.cFileName, _T(".")) == 0 ||
				_tcscmp(findData.cFileName, _T("..")) == 0)
			{
				continue;
			}

			sunjwbase::tstring fullPath = currentFolder;
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
				pendingFolders.push_back(fullPath);
			}
			else
			{
				if (scanOutcome.files.size() >= kMaxHashFilesPerSession)
				{
					truncated = true;
					break;
				}

				scanOutcome.files.push_back(fullPath);
			}
		} while (FindNextFile(hFind, &findData) != FALSE);

		FindClose(hFind);
	}

	scanOutcome.truncated = truncated;
	return scanOutcome;
}

void FilesHashInputController::ClearFilePaths()
{
	if (m_threadData == NULL)
	{
		return;
	}

	ResetThreadDataInputFiles(*m_threadData);
}
