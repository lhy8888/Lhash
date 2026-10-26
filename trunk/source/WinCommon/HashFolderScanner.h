#pragma once

#include <atomic>
#include <deque>
#include <memory>
#include <process.h>
#include <tchar.h>
#include <utility>

#include "Common/HashBackgroundResult.h"
#include "Common/HashInputPolicy.h"
#include "Common/HashTypes.h"
#include "WinCommon/WinHandleGuard.h"

struct FolderScanOutcome
{
	TStrVector files;
	bool overFileLimit = false;
	bool budgetExceeded = false;
	bool cancelled = false;
	bool failed = false;
};

inline FolderScanOutcome ScanHashFolder(const sunjwbase::tstring& folderPath, const std::atomic<bool>& cancelRequested)
{
	FolderScanOutcome outcome;
	if (folderPath.empty()) return outcome;
	HashFolderScanBudget budget;
	std::deque<std::pair<sunjwbase::tstring, size_t>> pendingFolders;
	pendingFolders.emplace_back(folderPath, 0);
	while (!pendingFolders.empty())
	{
		if (cancelRequested.load(std::memory_order_relaxed)) { outcome.cancelled = true; break; }
		const auto current = pendingFolders.back();
		pendingFolders.pop_back();
		const sunjwbase::tstring separator = current.first.back() == _T('\\') || current.first.back() == _T('/') ? _T("") : _T("\\");
		const sunjwbase::tstring prefix = current.first + separator;
		WIN32_FIND_DATA findData = {};
		WinHandleGuard::UniqueFindHandle find(FindFirstFile((prefix + _T("*")).c_str(), &findData));
		if (!find.isValid())
		{
			// Only a genuinely empty folder is a successful empty enumeration.
			if (GetLastError() != ERROR_FILE_NOT_FOUND) outcome.failed = true;
			if (outcome.failed) break;
			continue;
		}
		for (;;)
		{
			if (cancelRequested.load(std::memory_order_relaxed)) { outcome.cancelled = true; break; }
			if (_tcscmp(findData.cFileName, _T(".")) != 0 && _tcscmp(findData.cFileName, _T("..")) != 0)
			{
				if (!budget.VisitEntry()) { outcome.budgetExceeded = true; break; }
				if ((findData.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) == 0)
				{
					if ((findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0)
					{
						if (!budget.QueueDirectory(current.second + 1)) { outcome.budgetExceeded = true; break; }
						pendingFolders.emplace_back(prefix + findData.cFileName, current.second + 1);
					}
					else
					{
						if (outcome.files.size() == kMaxHashFilesPerSession) { outcome.overFileLimit = true; break; }
						outcome.files.push_back(prefix + findData.cFileName);
					}
				}
			}
			if (!FindNextFile(find.get(), &findData))
			{
				if (GetLastError() != ERROR_NO_MORE_FILES) outcome.failed = true;
				break;
			}
		}
		if (outcome.cancelled || outcome.failed || outcome.budgetExceeded || outcome.overFileLimit) break;
	}
	return outcome;
}

// Owns only values and a shared result. No HWND, controller, MFC object or COM
// pointer may enter the worker. A stuck filesystem call cannot retain the UI.
class HashFolderScanJob
{
public:
	using ScanFunction = FolderScanOutcome (*)(const sunjwbase::tstring&, const std::atomic<bool>&);
	static std::shared_ptr<HashFolderScanJob> Start(const sunjwbase::tstring& path, ScanFunction scan = &ScanHashFolder)
	{
		auto job = std::shared_ptr<HashFolderScanJob>(new HashFolderScanJob(path, scan));
		auto argument = std::make_unique<std::shared_ptr<HashFolderScanJob>>(job);
		const uintptr_t thread = _beginthreadex(NULL, 0, &Run, argument.get(), 0, NULL);
		if (thread == 0) return {};
		argument.release(); // Run owns this reference until the worker returns.
		job->m_thread.reset(reinterpret_cast<HANDLE>(thread));
		return job;
	}

	void Cancel() noexcept
	{
		m_result.Cancel();
		// Best effort only: filesystem drivers need not abort immediately. The UI
		// never waits for this handle after cancellation and never kills the thread.
		if (m_thread.isValid()) CancelSynchronousIo(m_thread.get());
	}
	bool IsFinished() const noexcept { return m_result.IsFinished(); }
	bool HasExited() const noexcept { return WaitForSingleObject(m_thread.get(), 0) == WAIT_OBJECT_0; }
	bool TryTake(FolderScanOutcome& result) { return m_result.TryTake(result); }
	HANDLE ThreadHandle() const noexcept { return m_thread.get(); }

private:
	HashFolderScanJob(const sunjwbase::tstring& path, ScanFunction scan) : m_path(path), m_scan(scan) {}
	static unsigned int __stdcall Run(void* parameter)
	{
		std::unique_ptr<std::shared_ptr<HashFolderScanJob>> reference(static_cast<std::shared_ptr<HashFolderScanJob>*>(parameter));
		auto& job = **reference;
		FolderScanOutcome outcome;
		try { outcome = job.m_scan(job.m_path, job.m_result.Cancellation()); }
		catch (...) { outcome.failed = true; }
		job.m_result.Complete(std::move(outcome));
		return 0;
	}
	const sunjwbase::tstring m_path;
	const ScanFunction m_scan;
	WinHandleGuard::UniqueWinHandle m_thread;
	HashBackgroundResult<FolderScanOutcome> m_result;
};
