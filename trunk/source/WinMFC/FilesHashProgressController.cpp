#include "stdafx.h"

#include "FilesHashProgressController.h"
#include "FilesHashTaskUpdate.h"
#include "resource.h"
#include "WinCommon/WindowsStrings.h"

using namespace WindowsStrings;

FilesHashProgressController::FilesHashProgressController()
	: m_parentWnd(NULL),
	m_progressCtrl(NULL),
	m_taskListCtrl(NULL),
	m_statusOverviewCtrl(NULL),
	m_calculateTime(0.0),
	m_workerTiming(NULL),
	m_timerId(0),
	m_bAdvTaskbar(FALSE),
	m_taskbarList(NULL)
{
}

FilesHashProgressController::~FilesHashProgressController()
{
	if (m_parentWnd != NULL && m_timerId != 0)
	{
		m_parentWnd->KillTimer(m_timerId);
		m_timerId = 0;
	}
	CloseTaskbarList();
}

void FilesHashProgressController::Initialize(CDialog* parentWnd, CProgressCtrl* progressCtrl)
{
	m_parentWnd = parentWnd;
	m_progressCtrl = progressCtrl;
	m_calculateTime = 0.0;
	m_workerTiming = NULL;
	m_timerId = 0;
	m_secondText.Empty();
	m_speedText.Empty();
	m_summaryText.Empty();
	m_taskModel.Clear();
	CloseTaskbarList();
}

void FilesHashProgressController::AttachTaskControls(CListCtrl* taskListCtrl, CStatic* statusOverviewCtrl)
{
	m_taskListCtrl = taskListCtrl;
	m_statusOverviewCtrl = statusOverviewCtrl;
	m_summaryText.Empty();
}

void FilesHashProgressController::InitializeTaskList(LPCTSTR fileColumnText, LPCTSTR algorithmColumnText, LPCTSTR statusColumnText, LPCTSTR progressColumnText)
{
	if (m_taskListCtrl == NULL || !::IsWindow(m_taskListCtrl->GetSafeHwnd()))
	{
		return;
	}

	m_taskListCtrl->ModifyStyle(0, LVS_REPORT | LVS_OWNERDATA);
	m_taskListCtrl->SetExtendedStyle(LVS_EX_FULLROWSELECT | LVS_EX_DOUBLEBUFFER | LVS_EX_GRIDLINES);
	m_taskListCtrl->SetItemCountEx(0, LVSICF_NOINVALIDATEALL | LVSICF_NOSCROLL);
	while (m_taskListCtrl->DeleteColumn(0))
	{
	}

	CRect clientRect;
	m_taskListCtrl->GetClientRect(&clientRect);
	int availableWidth = max(420, clientRect.Width() - 2);
	int fileColumnWidth = max(190, (availableWidth * 35) / 100);
	int algorithmColumnWidth = max(120, (availableWidth * 20) / 100);
	int statusColumnWidth = max(130, (availableWidth * 15) / 100);
	int progressColumnWidth = max(220, availableWidth - fileColumnWidth - algorithmColumnWidth - statusColumnWidth);

	m_taskListCtrl->InsertColumn(0, fileColumnText, LVCFMT_LEFT, fileColumnWidth);
	m_taskListCtrl->InsertColumn(1, algorithmColumnText, LVCFMT_LEFT, algorithmColumnWidth);
	m_taskListCtrl->InsertColumn(2, statusColumnText, LVCFMT_LEFT, statusColumnWidth);
	m_taskListCtrl->InsertColumn(3, progressColumnText, LVCFMT_LEFT, progressColumnWidth);
	m_taskListCtrl->SetItemCountEx(0, LVSICF_NOINVALIDATEALL | LVSICF_NOSCROLL);
	UpdateSummaryText();
}

void FilesHashProgressController::BeginTaskSession(uint64_t sessionId, const TStrVector& inputFiles, const sunjwbase::tstring& algorithmSummary)
{
	m_taskModel.BeginSession(sessionId, inputFiles, algorithmSummary, GetStringByKey(MAINDLG_TASK_STATUS_PENDING));
	RefreshAllTaskRows();
	UpdateSummaryText();
}

bool FilesHashProgressController::FinishTaskSession(uint64_t sessionId, FilesHashSessionOutcome outcome)
{
	const FilesHashTaskFinishText text = {
		GetStringByKey(MAINDLG_TASK_STATUS_CANCELLED), GetStringByKey(MAINDLG_TASK_STATUS_ABORTED),
		GetStringByKey(MAINDLG_TASK_STATUS_NOT_RUN_CANCELLED), GetStringByKey(MAINDLG_TASK_STATUS_NOT_RUN_ABORTED)};
	const bool consistent = m_taskModel.FinishSession(sessionId, outcome, text);
	RefreshAllTaskRows();
	UpdateSummaryText();
	return consistent;
}

void FilesHashProgressController::ResetTaskSession()
{
	m_taskModel.Clear();
	m_speedText.Empty();
	RefreshAllTaskRows();
	UpdateSummaryText();
}

void FilesHashProgressController::ApplyTaskUpdate(const FilesHashTaskUpdate& taskUpdate)
{
	bool ensureVisible = false;
	int rowIndex = ApplyTaskUpdateToState(taskUpdate, &ensureVisible);
	if (rowIndex >= 0)
	{
		RefreshTaskRow(rowIndex, ensureVisible);
		UpdateSummaryText();
	}
}

void FilesHashProgressController::ApplyTaskUpdates(const std::vector<FilesHashTaskUpdate>& taskUpdates)
{
	if (taskUpdates.empty())
	{
		return;
	}

	int firstChangedRow = -1;
	int lastChangedRow = -1;
	int ensureVisibleRowIndex = -1;
	for (const auto& taskUpdate : taskUpdates)
	{
		bool ensureVisible = false;
		const int rowIndex = ApplyTaskUpdateToState(taskUpdate, &ensureVisible);
		if (rowIndex < 0) continue;
		firstChangedRow = firstChangedRow < 0 ? rowIndex : min(firstChangedRow, rowIndex);
		lastChangedRow = max(lastChangedRow, rowIndex);
		if (ensureVisible) ensureVisibleRowIndex = rowIndex;
	}

	if (firstChangedRow < 0) return;
	if (m_taskListCtrl != NULL && ::IsWindow(m_taskListCtrl->GetSafeHwnd()))
	{
		// Owner-data count changes only at Begin/Clear. Invalidate the changed
		// range once rather than the entire retained history for every batch.
		m_taskListCtrl->RedrawItems(firstChangedRow, lastChangedRow);
		if (ensureVisibleRowIndex >= 0) m_taskListCtrl->EnsureVisible(ensureVisibleRowIndex, FALSE);
	}

	UpdateSummaryText();
}

int FilesHashProgressController::ApplyTaskUpdateToState(const FilesHashTaskUpdate& taskUpdate, bool *ensureVisible)
{
	const int rowIndex = m_taskModel.Apply(taskUpdate);
	if (ensureVisible != NULL) *ensureVisible = rowIndex >= 0 && IsFilesHashTaskTerminal(taskUpdate.state);
	return rowIndex;
}

void FilesHashProgressController::PrepareAdvTaskbar()
{
	CloseTaskbarList();
	m_bAdvTaskbar = FALSE;
#ifndef _DEBUG
	ITaskbarList3* taskbarList = NULL;
	const HRESULT created = CoCreateInstance(
		CLSID_TaskbarList, NULL, CLSCTX_ALL,
		IID_ITaskbarList3, reinterpret_cast<void**>(&taskbarList));
	if (SUCCEEDED(created) && taskbarList != NULL)
	{
		if (SUCCEEDED(taskbarList->HrInit()))
		{
			m_taskbarList = taskbarList;
			m_bAdvTaskbar = TRUE;
		}
		else
		{
			taskbarList->Release();
		}
	}
#endif
}

void FilesHashProgressController::StartTiming(LPCTSTR secondText, const HashWorkerTiming* timing)
{
	m_workerTiming = timing;
	m_calculateTime = timing != NULL ? timing->ElapsedSeconds() : 0.0;
	m_secondText = secondText;
	m_speedText.Empty();
	if (m_parentWnd != NULL)
	{
		if (m_timerId != 0)
		{
			m_parentWnd->KillTimer(m_timerId);
		}
		m_timerId = m_parentWnd->SetTimer(1, 100, NULL);
	}

	UpdateSummaryText();
}

void FilesHashProgressController::AdvanceTimeTick(LPCTSTR secondText)
{
	if (m_workerTiming == NULL) return;
	m_calculateTime = m_workerTiming->ElapsedSeconds();
	m_secondText = secondText;
	UpdateSummaryText();
}

void FilesHashProgressController::FinishTiming(ULONGLONG totalSize)
{
	if (m_parentWnd != NULL && m_timerId != 0)
	{
		m_parentWnd->KillTimer(m_timerId);
		m_timerId = 0;
	}

	if (m_workerTiming != NULL) m_calculateTime = m_workerTiming->ElapsedSeconds();
	m_workerTiming = NULL;
	if (m_calculateTime > 0.0)
	{
		double speed = totalSize / m_calculateTime;
		CString speedStr;
		CString measure = _T("B/s");
		if ((speed / 1024) > 1)
		{
			speed /= 1024;
			measure = _T("KB/s");
			if ((speed / 1024) > 1)
			{
				speed /= 1024;
				measure = _T("MB/s");
			}
		}
		speedStr.Format(_T("%4.2f "), speed);
		speedStr.Append(measure);
		m_speedText = speedStr;
	}
	else
	{
		m_speedText.Empty();
	}

	UpdateSummaryText();
}

void FilesHashProgressController::ResetAfterStop()
{
	if (m_parentWnd != NULL && m_timerId != 0)
	{
		m_parentWnd->KillTimer(m_timerId);
		m_timerId = 0;
	}

	m_calculateTime = 0.0;
	m_workerTiming = NULL;
	m_speedText.Empty();
	UpdateSummaryText();
}

void FilesHashProgressController::SetWholeProgress(UINT pos)
{
	if (m_progressCtrl != NULL)
	{
		m_progressCtrl->SetPos(pos);
	}

	if (m_bAdvTaskbar && m_taskbarList != NULL && m_parentWnd != NULL)
	{
		m_taskbarList->SetProgressValue(m_parentWnd->GetSafeHwnd(), pos, 99);
	}
}

int FilesHashProgressController::GetTaskRowCount() const
{
	return static_cast<int>(m_taskModel.Rows().size());
}

bool FilesHashProgressController::TryGetTaskRowDisplayText(int rowIndex, int subItem, CString *displayText) const
{
	if (displayText == NULL)
	{
		return false;
	}

	const TaskRowState* taskRowState = TryGetTaskRowState(rowIndex);
	if (taskRowState == NULL)
	{
		return false;
	}

	switch (subItem)
	{
	case 0:
		*displayText = taskRowState->displayName.c_str();
		return true;
	case 1:
		*displayText = taskRowState->algorithms.c_str();
		return true;
	case 2:
		*displayText = taskRowState->status.c_str();
		return true;
	case 3:
		*displayText = BuildProgressText(taskRowState->progress);
		return true;
	default:
		displayText->Empty();
		return false;
	}
}

const FilesHashProgressController::TaskRowState* FilesHashProgressController::TryGetTaskRowState(int rowIndex) const
{
	if (rowIndex < 0 || rowIndex >= static_cast<int>(m_taskModel.Rows().size()))
	{
		return NULL;
	}

	return &m_taskModel.Rows()[static_cast<size_t>(rowIndex)];
}

void FilesHashProgressController::RefreshTaskRow(int rowIndex, bool ensureVisible /*= false*/)
{
	if (m_taskListCtrl == NULL || !::IsWindow(m_taskListCtrl->GetSafeHwnd()) || rowIndex < 0 || rowIndex >= static_cast<int>(m_taskModel.Rows().size()))
	{
		return;
	}

	m_taskListCtrl->RedrawItems(rowIndex, rowIndex);
	if (ensureVisible)
	{
		m_taskListCtrl->EnsureVisible(rowIndex, FALSE);
	}
}

void FilesHashProgressController::RefreshAllTaskRows()
{
	if (m_taskListCtrl == NULL || !::IsWindow(m_taskListCtrl->GetSafeHwnd()))
	{
		return;
	}

	m_taskListCtrl->SetItemCountEx(static_cast<int>(m_taskModel.Rows().size()), LVSICF_NOINVALIDATEALL | LVSICF_NOSCROLL);
	m_taskListCtrl->Invalidate(FALSE);
}

void FilesHashProgressController::UpdateSummaryText()
{
	if (m_statusOverviewCtrl == NULL || !::IsWindow(m_statusOverviewCtrl->GetSafeHwnd()))
	{
		return;
	}


	CString elapsedText;
	elapsedText.Format(_T("%d %s"), static_cast<int>(m_calculateTime), static_cast<LPCTSTR>(m_secondText.IsEmpty() ? CString(GetStringByKey(SECOND_STRING)) : m_secondText));

	CString summary;
	summary.Format(_T("%s %d    %s %d    %s %d    %s %d    %s %d    %s %d    %s %d    %s %s    %s %s"),
		GetStringByKey(MAINDLG_STATUS_TOTAL),
		static_cast<int>(m_taskModel.Rows().size()),
		GetStringByKey(MAINDLG_STATUS_DONE),
		static_cast<int>(m_taskModel.Count(FILES_HASH_TASK_COMPLETED)),
		GetStringByKey(MAINDLG_STATUS_FAILED),
		static_cast<int>(m_taskModel.Count(FILES_HASH_TASK_FAILED)),
		GetStringByKey(MAINDLG_STATUS_RUNNING),
		static_cast<int>(m_taskModel.Count(FILES_HASH_TASK_RUNNING)),
		GetStringByKey(MAINDLG_STATUS_CANCELLED),
		static_cast<int>(m_taskModel.Count(FILES_HASH_TASK_CANCELLED)),
		GetStringByKey(MAINDLG_STATUS_ABORTED),
		static_cast<int>(m_taskModel.Count(FILES_HASH_TASK_ABORTED)),
		GetStringByKey(MAINDLG_STATUS_NOT_RUN),
		static_cast<int>(m_taskModel.Count(FILES_HASH_TASK_NOT_RUN)),
		GetStringByKey(MAINDLG_STATUS_TIME),
		static_cast<LPCTSTR>(elapsedText),
		GetStringByKey(MAINDLG_STATUS_SPEED),
		static_cast<LPCTSTR>(m_speedText.IsEmpty() ? CString(_T("-")) : m_speedText));
	if (summary != m_summaryText)
	{
		m_statusOverviewCtrl->SetWindowText(summary);
		m_summaryText = summary;
	}
}

CString FilesHashProgressController::BuildProgressText(int progress)
{
	int clampedProgress = max(0, min(100, progress));
	const int barWidth = 24;
	int filledWidth = (clampedProgress * barWidth) / 100;
	CString progressText(_T("["));
	for (int index = 0; index < barWidth; ++index)
	{
		progressText.Append(index < filledWidth ? _T("=") : _T(" "));
	}
	progressText.Append(_T("] "));
	CString percentText;
	percentText.Format(_T("%d%%"), clampedProgress);
	progressText.Append(percentText);
	return progressText;
}

void FilesHashProgressController::CloseTaskbarList()
{
	ITaskbarList3* taskbarList = m_taskbarList;
	m_taskbarList = NULL;
	m_bAdvTaskbar = FALSE;
	if (taskbarList != NULL) taskbarList->Release();
}
