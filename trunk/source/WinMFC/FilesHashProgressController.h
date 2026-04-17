#pragma once

#include <vector>

#include "afxwin.h"
#include "afxcmn.h"

#include "Common/Global.h"
#include "FilesHashTaskUpdate.h"

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
	void BeginTaskSession(const TStrVector& inputFiles, const sunjwbase::tstring& algorithmSummary);
	void ResetTaskSession();
	void ApplyTaskUpdate(const FilesHashTaskUpdate& taskUpdate);
	void PrepareAdvTaskbar();
	void StartTiming(LPCTSTR secondText);
	void AdvanceTimeTick(LPCTSTR secondText);
	void FinishTiming(ULONGLONG totalSize);
	void ResetAfterStop();
	void SetWholeProgress(UINT pos);

private:
	struct TaskRowState
	{
		sunjwbase::tstring fullPath;
		sunjwbase::tstring displayName;
		sunjwbase::tstring algorithms;
		sunjwbase::tstring status;
		FilesHashTaskState state;
		int progress;
	};

	static sunjwbase::tstring BuildDisplayName(const sunjwbase::tstring& fullPath);
	static CString BuildProgressText(int progress);
	int FindTaskRowIndex(const sunjwbase::tstring& fullPath) const;
	void RefreshTaskRow(int rowIndex, bool ensureVisible = false);
	void RefreshAllTaskRows();
	void UpdateSummaryText();
	void CloseTaskbarList();

	CDialog* m_parentWnd;
	CProgressCtrl* m_progressCtrl;
	CListCtrl* m_taskListCtrl;
	CStatic* m_statusOverviewCtrl;
	float m_calculateTime;
	UINT_PTR m_timerId;
	BOOL m_bAdvTaskbar;
	ITaskbarList3* m_taskbarList;
	CString m_secondText;
	CString m_speedText;
	std::vector<TaskRowState> m_taskRows;
	sunjwbase::tstring m_algorithmSummary;
};
