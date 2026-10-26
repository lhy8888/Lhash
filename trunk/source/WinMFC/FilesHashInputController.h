#pragma once

#include <shellapi.h>
#include <memory>

#include "afxwin.h"

#include "Common/HashTypes.h"
struct ThreadData;

enum class FileLoadResult
{
	Success,
	SuccessPossiblyTruncated,
	SuccessWithTruncation,
	RejectedOverLimit,
	EnumerationLimit,
	Cancelled,
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

class HashFolderScanJob;

class FilesHashInputController
{
public:
	FilesHashInputController();
	~FilesHashInputController();

	void Initialize(ThreadData* threadData, CWnd* parentWnd);
	bool IsLoadingFiles() const { return m_loadingFiles; }
	void CancelPendingLoad(bool closeAfterLoading = false);

	void LoadCommandLineFiles(LPTSTR filesCmdLine);
	FileLoadOutcome LoadOpenFileDialogSelection(LPCTSTR fileFilter);
	FileLoadOutcome LoadFolderDialogSelection(LPCTSTR folderDialogTitle, LPCTSTR emptyFolderMessage);
	FileLoadOutcome LoadDroppedFiles(HDROP hDropInfo);
	FileLoadOutcome LoadCopyDataFiles(const COPYDATASTRUCT* pCopyDataStruct);

private:
	class PendingLoadGuard
	{
	public:
		explicit PendingLoadGuard(FilesHashInputController& owner);
		~PendingLoadGuard();
	private:
		FilesHashInputController& m_owner;
	};

	static size_t GetCopyDataCommandCharLimit();
	static bool CopyDraggedPath(HDROP hDropInfo, UINT index, sunjwbase::tstring& tstrPath);
	static bool IsValidCopyDataString(const COPYDATASTRUCT* pCopyDataStruct);
	static TStrVector ParseFilesCmdLine(LPTSTR filesCmdLine);

	void ClearFilePaths();

	ThreadData* m_threadData;
	CWnd* m_parentWnd;
	bool m_loadingFiles;
	bool m_cancelLoad;
	bool m_closeAfterLoading;
	// Retain a cancelled but still blocked job to prevent unbounded workers.
	std::shared_ptr<HashFolderScanJob> m_folderScan;
};
