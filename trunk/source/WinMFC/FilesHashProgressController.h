#pragma once

#include <vector>

#include "afxwin.h"
#include "afxcmn.h"

#include "Common/HashTypes.h"
#include "FilesHashTaskModel.h"
#include "Adapters/MfcBridge/HashWorkerTiming.h"

struct ITaskbarList3;
class CListCtrl;
class CStatic;

class FilesHashProgressController
{
public:
	FilesHashProgressController();
	~FilesHashProgressController();

	void Initialize(CDialog* parentWnd, CProgressCtrl* progressCtrl);
	void AttachTaskControls(CListCtrl* taskListCtrl, CStatic* statusOverviewCtrl);
	void InitializeTaskList(LPCTSTR fileColumnText, LPCTSTR algorithmColumnText, LPCTSTR statusColumnText, LPCTSTR progressColumnText);
	void BeginTaskSession(uint64_t sessionId, const TStrVector& inputFiles, const sunjwbase::tstring& algorithmSummary);
	bool FinishTaskSession(uint64_t sessionId, FilesHashSessionOutcome outcome);
	void ResetTaskSession();
	void ApplyTaskUpdate(const FilesHashTaskUpdate& taskUpdate);
	void ApplyTaskUpdates(const std::vector<FilesHashTaskUpdate>& taskUpdates);
	void PrepareAdvTaskbar();
	void StartTiming(LPCTSTR secondText, const HashWorkerTiming* timing);
	void AdvanceTimeTick(LPCTSTR secondText);
	void FinishTiming(ULONGLONG totalSize);
	void ResetAfterStop();
	void SetWholeProgress(UINT pos);
	int GetTaskRowCount() const;
	bool TryGetTaskRowDisplayText(int rowIndex, int subItem, CString *displayText) const;

private:
	using TaskRowState = FilesHashTaskRow;

	static CString BuildProgressText(int progress);
	int ApplyTaskUpdateToState(const FilesHashTaskUpdate& taskUpdate, bool *ensureVisible);
	const TaskRowState* TryGetTaskRowState(int rowIndex) const;
	void RefreshTaskRow(int rowIndex, bool ensureVisible = false);
	void RefreshAllTaskRows();
	void UpdateSummaryText();
	void CloseTaskbarList();

	CDialog* m_parentWnd;
	CProgressCtrl* m_progressCtrl;
	CListCtrl* m_taskListCtrl;
	CStatic* m_statusOverviewCtrl;
	double m_calculateTime;
	const HashWorkerTiming* m_workerTiming;
	UINT_PTR m_timerId;
	BOOL m_bAdvTaskbar;
	ITaskbarList3* m_taskbarList;
	CString m_secondText;
	CString m_speedText;
	CString m_summaryText;
	FilesHashTaskModel m_taskModel;
};
