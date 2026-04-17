#pragma once

#include <shellapi.h>

#include "afxwin.h"

#include "Common/Global.h"
struct ThreadData;

enum class FileLoadResult
{
	Success,
	SuccessPossiblyTruncated,
	SuccessWithTruncation,
	RejectedOverLimit,
	Empty,
	Error
};

struct FileLoadOutcome
{
	FileLoadOutcome()
		: result(FileLoadResult::Error),
		loadedCount(0),
		limit(kMaxHashFilesPerSession),
		hasRequestedCount(false),
		requestedCount(0)
	{
	}

	FileLoadResult result;
	size_t loadedCount;
	size_t limit;
	bool hasRequestedCount;
	size_t requestedCount;
};

struct FolderScanOutcome
{
	FolderScanOutcome()
		: truncated(false),
		limit(kMaxHashFilesPerSession)
	{
	}

	TStrVector files;
	bool truncated;
	size_t limit;
};

class FilesHashInputController
{
public:
	FilesHashInputController();

	void Initialize(ThreadData* threadData, CWnd* parentWnd);

	void LoadCommandLineFiles(LPTSTR filesCmdLine);
	FileLoadOutcome LoadOpenFileDialogSelection(LPCTSTR fileFilter);
	FileLoadOutcome LoadFolderDialogSelection(LPCTSTR folderDialogTitle, LPCTSTR emptyFolderMessage);
	FileLoadOutcome LoadDroppedFiles(HDROP hDropInfo);
	FileLoadOutcome LoadCopyDataFiles(const COPYDATASTRUCT* pCopyDataStruct);

private:
	static size_t GetCopyDataCommandCharLimit();
	static bool CopyDraggedPath(HDROP hDropInfo, UINT index, sunjwbase::tstring& tstrPath);
	static bool IsValidCopyDataString(const COPYDATASTRUCT* pCopyDataStruct);
	static TStrVector ParseFilesCmdLine(LPTSTR filesCmdLine);
	static FolderScanOutcome AppendFolderFilesRecursive(const sunjwbase::tstring& folderPath);

	void ClearFilePaths();

	ThreadData* m_threadData;
	CWnd* m_parentWnd;
};
