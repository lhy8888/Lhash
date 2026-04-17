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
	m_calculateTime(0.0f),
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
	m_calculateTime = 0.0f;
	m_timerId = 0;
	m_secondText.Empty();
	m_speedText.Empty();
	m_taskRows.clear();
	m_algorithmSummary.clear();
	CloseTaskbarList();
}

void FilesHashProgressController::AttachTaskControls(CListCtrl* taskListCtrl, CStatic* statusOverviewCtrl)
{
	m_taskListCtrl = taskListCtrl;
	m_statusOverviewCtrl = statusOverviewCtrl;
}

void FilesHashProgressController::InitializeTaskList(LPCTSTR fileColumnText, LPCTSTR algorithmColumnText, LPCTSTR statusColumnText, LPCTSTR progressColumnText)
{
	if (m_taskListCtrl == NULL || !::IsWindow(m_taskListCtrl->GetSafeHwnd()))
	{
		return;
	}

	m_taskListCtrl->SetExtendedStyle(LVS_EX_FULLROWSELECT | LVS_EX_DOUBLEBUFFER | LVS_EX_GRIDLINES);
	m_taskListCtrl->DeleteAllItems();
	while (m_taskListCtrl->DeleteColumn(0))
	{
	}

	CRect clientRect;
	m_taskListCtrl->GetClientRect(&clientRect);
	int availableWidth = max(420, clientRect.Width() - 2);
	int fileColumnWidth = max(190, (availableWidth * 35) / 100);
	int algorithmColumnWidth = max(120, (availableWidth * 20) / 100);
	int statusColumnWidth = max(56, (availableWidth * 9) / 100);
	int progressColumnWidth = max(220, availableWidth - fileColumnWidth - algorithmColumnWidth - statusColumnWidth);

	m_taskListCtrl->InsertColumn(0, fileColumnText, LVCFMT_LEFT, fileColumnWidth);
	m_taskListCtrl->InsertColumn(1, algorithmColumnText, LVCFMT_LEFT, algorithmColumnWidth);
	m_taskListCtrl->InsertColumn(2, statusColumnText, LVCFMT_LEFT, statusColumnWidth);
	m_taskListCtrl->InsertColumn(3, progressColumnText, LVCFMT_LEFT, progressColumnWidth);
	UpdateSummaryText();
}

void FilesHashProgressController::BeginTaskSession(const TStrVector& inputFiles, const sunjwbase::tstring& algorithmSummary)
{
	m_algorithmSummary = algorithmSummary;

	for (TStrVector::const_iterator itr = inputFiles.begin(); itr != inputFiles.end(); ++itr)
	{
		TaskRowState taskRowState;
		taskRowState.fullPath = *itr;
		taskRowState.displayName = BuildDisplayName(*itr);
		taskRowState.algorithms = algorithmSummary;
		taskRowState.status = GetStringByKey(MAINDLG_TASK_STATUS_PENDING);
		taskRowState.state = FILES_HASH_TASK_PENDING;
		taskRowState.progress = 0;
		m_taskRows.push_back(taskRowState);
	}

	RefreshAllTaskRows();
	UpdateSummaryText();
}

void FilesHashProgressController::ResetTaskSession()
{
	m_taskRows.clear();
	m_algorithmSummary.clear();
	m_speedText.Empty();
	RefreshAllTaskRows();
	UpdateSummaryText();
}

void FilesHashProgressController::ApplyTaskUpdate(const FilesHashTaskUpdate& taskUpdate)
{
	if (taskUpdate.path.empty())
	{
		return;
	}

	bool ensureVisible = false;
	int rowIndex = ApplyTaskUpdateToState(taskUpdate, &ensureVisible);
	RefreshTaskRow(rowIndex, ensureVisible);
	UpdateSummaryText();
}

void FilesHashProgressController::ApplyTaskUpdates(const std::vector<FilesHashTaskUpdate>& taskUpdates)
{
	if (taskUpdates.empty())
	{
		return;
	}

	bool canBatchRedraw = m_taskListCtrl != NULL && ::IsWindow(m_taskListCtrl->GetSafeHwnd());
	if (canBatchRedraw)
	{
		m_taskListCtrl->SetRedraw(FALSE);
	}

	int ensureVisibleRowIndex = -1;
	for (size_t updateIndex = 0; updateIndex < taskUpdates.size(); ++updateIndex)
	{
		const FilesHashTaskUpdate& taskUpdate = taskUpdates[updateIndex];
		if (taskUpdate.path.empty())
		{
			continue;
		}

		bool ensureVisible = false;
		int rowIndex = ApplyTaskUpdateToState(taskUpdate, &ensureVisible);
		RefreshTaskRow(rowIndex, false);
		if (ensureVisible)
		{
			ensureVisibleRowIndex = rowIndex;
		}
	}

	if (canBatchRedraw)
	{
		m_taskListCtrl->SetRedraw(TRUE);
		if (ensureVisibleRowIndex >= 0)
		{
			m_taskListCtrl->EnsureVisible(ensureVisibleRowIndex, FALSE);
		}
		m_taskListCtrl->Invalidate(FALSE);
	}

	UpdateSummaryText();
}

int FilesHashProgressController::ApplyTaskUpdateToState(const FilesHashTaskUpdate& taskUpdate, bool *ensureVisible)
{
	if (ensureVisible != NULL)
	{
		*ensureVisible = false;
	}

	int rowIndex = FindTaskRowIndex(taskUpdate.path);
	if (rowIndex < 0)
	{
		TaskRowState taskRowState;
		taskRowState.fullPath = taskUpdate.path;
		taskRowState.displayName = BuildDisplayName(taskUpdate.path);
		taskRowState.algorithms = taskUpdate.algorithms.empty() ? m_algorithmSummary : taskUpdate.algorithms;
		taskRowState.status = taskUpdate.status;
		taskRowState.state = taskUpdate.state;
		taskRowState.progress = taskUpdate.progress;
		m_taskRows.push_back(taskRowState);
		rowIndex = static_cast<int>(m_taskRows.size() - 1);
		if (ensureVisible != NULL)
		{
			*ensureVisible = true;
		}
	}
	else
	{
		TaskRowState& taskRowState = m_taskRows[static_cast<size_t>(rowIndex)];
		if (!taskUpdate.algorithms.empty())
		{
			taskRowState.algorithms = taskUpdate.algorithms;
		}
		taskRowState.status = taskUpdate.status;
		taskRowState.state = taskUpdate.state;
		taskRowState.progress = taskUpdate.progress;
	}

	if (taskUpdate.state == FILES_HASH_TASK_COMPLETED || taskUpdate.state == FILES_HASH_TASK_FAILED)
	{
		if (ensureVisible != NULL)
		{
			*ensureVisible = true;
		}
	}

	return rowIndex;
}

void FilesHashProgressController::PrepareAdvTaskbar()
{
	CloseTaskbarList();
	m_bAdvTaskbar = FALSE;
#ifndef _DEBUG
	VERIFY(CoCreateInstance(
		CLSID_TaskbarList, NULL, CLSCTX_ALL,
		IID_ITaskbarList3, (void**)&m_taskbarList));
	if (m_taskbarList != NULL)
	{
		m_bAdvTaskbar = TRUE;
	}
#endif
}

void FilesHashProgressController::StartTiming(LPCTSTR secondText)
{
	m_calculateTime = 0.0f;
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
	m_calculateTime += 0.1f;
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

	if (m_calculateTime > 0.1f)
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

	m_calculateTime = 0.0f;
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

int FilesHashProgressController::FindTaskRowIndex(const sunjwbase::tstring& fullPath) const
{
	for (int index = static_cast<int>(m_taskRows.size()) - 1; index >= 0; --index)
	{
		const TaskRowState& taskRowState = m_taskRows[static_cast<size_t>(index)];
		if (taskRowState.fullPath == fullPath &&
			(taskRowState.state == FILES_HASH_TASK_PENDING ||
			 taskRowState.state == FILES_HASH_TASK_RUNNING))
		{
			return index;
		}
	}

	return -1;
}

void FilesHashProgressController::RefreshTaskRow(int rowIndex, bool ensureVisible /*= false*/)
{
	if (m_taskListCtrl == NULL || !::IsWindow(m_taskListCtrl->GetSafeHwnd()) || rowIndex < 0 || rowIndex >= static_cast<int>(m_taskRows.size()))
	{
		return;
	}

	TaskRowState& taskRowState = m_taskRows[static_cast<size_t>(rowIndex)];
	if (m_taskListCtrl->GetItemCount() <= rowIndex)
	{
		m_taskListCtrl->InsertItem(rowIndex, taskRowState.displayName.c_str());
	}
	else
	{
		m_taskListCtrl->SetItemText(rowIndex, 0, taskRowState.displayName.c_str());
	}

	m_taskListCtrl->SetItemText(rowIndex, 1, taskRowState.algorithms.c_str());
	m_taskListCtrl->SetItemText(rowIndex, 2, taskRowState.status.c_str());
	m_taskListCtrl->SetItemText(rowIndex, 3, BuildProgressText(taskRowState.progress));
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

	m_taskListCtrl->DeleteAllItems();
	for (size_t index = 0; index < m_taskRows.size(); ++index)
	{
		RefreshTaskRow(static_cast<int>(index));
	}
}

void FilesHashProgressController::UpdateSummaryText()
{
	if (m_statusOverviewCtrl == NULL || !::IsWindow(m_statusOverviewCtrl->GetSafeHwnd()))
	{
		return;
	}

	int completedCount = 0;
	int failedCount = 0;
	int runningCount = 0;
	for (size_t index = 0; index < m_taskRows.size(); ++index)
	{
		switch (m_taskRows[index].state)
		{
		case FILES_HASH_TASK_COMPLETED:
			++completedCount;
			break;
		case FILES_HASH_TASK_FAILED:
			++failedCount;
			break;
		case FILES_HASH_TASK_RUNNING:
			++runningCount;
			break;
		default:
			break;
		}
	}

	CString elapsedText;
	elapsedText.Format(_T("%d %s"), static_cast<int>(m_calculateTime), static_cast<LPCTSTR>(m_secondText.IsEmpty() ? CString(GetStringByKey(SECOND_STRING)) : m_secondText));

	CString summary;
	summary.Format(_T("%s %d    %s %d    %s %d    %s %d    %s %s    %s %s"),
		GetStringByKey(MAINDLG_STATUS_TOTAL),
		static_cast<int>(m_taskRows.size()),
		GetStringByKey(MAINDLG_STATUS_DONE),
		completedCount,
		GetStringByKey(MAINDLG_STATUS_FAILED),
		failedCount,
		GetStringByKey(MAINDLG_STATUS_RUNNING),
		runningCount,
		GetStringByKey(MAINDLG_STATUS_TIME),
		static_cast<LPCTSTR>(elapsedText),
		GetStringByKey(MAINDLG_STATUS_SPEED),
		static_cast<LPCTSTR>(m_speedText.IsEmpty() ? CString(_T("-")) : m_speedText));
	m_statusOverviewCtrl->SetWindowText(summary);
}

sunjwbase::tstring FilesHashProgressController::BuildDisplayName(const sunjwbase::tstring& fullPath)
{
	sunjwbase::tstring::size_type separatorPosition = fullPath.find_last_of(_T("\\/"));
	if (separatorPosition == sunjwbase::tstring::npos)
	{
		return fullPath;
	}

	return fullPath.substr(separatorPosition + 1);
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
	if (m_taskbarList != NULL)
	{
		m_taskbarList->Release();
		m_taskbarList = NULL;
	}
	m_bAdvTaskbar = FALSE;
}
