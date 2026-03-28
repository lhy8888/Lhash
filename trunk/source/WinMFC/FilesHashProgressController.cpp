#include "stdafx.h"

#include "FilesHashProgressController.h"
#include "resource.h"

FilesHashProgressController::FilesHashProgressController()
	: m_parentWnd(NULL),
	m_progressCtrl(NULL),
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
	CloseTaskbarList();
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
	if (m_parentWnd != NULL)
	{
		if (m_timerId != 0)
		{
			m_parentWnd->KillTimer(m_timerId);
		}
		m_timerId = m_parentWnd->SetTimer(1, 100, NULL);
	}

	CString cstrZero(_T("0 "));
	cstrZero.Append(secondText);
	SetTimeText(cstrZero);
	SetSpeedText(_T(""));
}

void FilesHashProgressController::AdvanceTimeTick(LPCTSTR secondText)
{
	m_calculateTime += 0.1f;
	CString cstrTime;
	CString cstrFormat(_T("%d "));
	cstrFormat.Append(secondText);
	cstrTime.Format(cstrFormat, static_cast<int>(m_calculateTime));
	SetTimeText(cstrTime);
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
		SetSpeedText(speedStr);
	}
	else
	{
		SetSpeedText(_T(""));
	}
}

void FilesHashProgressController::ResetAfterStop()
{
	if (m_parentWnd != NULL && m_timerId != 0)
	{
		m_parentWnd->KillTimer(m_timerId);
		m_timerId = 0;
	}

	m_calculateTime = 0.0f;
	SetTimeText(_T(""));
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

void FilesHashProgressController::CloseTaskbarList()
{
	if (m_taskbarList != NULL)
	{
		m_taskbarList->Release();
		m_taskbarList = NULL;
	}
	m_bAdvTaskbar = FALSE;
}

void FilesHashProgressController::SetTimeText(LPCTSTR text)
{
	if (m_parentWnd == NULL)
	{
		return;
	}

	CWnd* pWnd = m_parentWnd->GetDlgItem(IDC_STATIC_TIME);
	if (pWnd != NULL)
	{
		pWnd->SetWindowText(text);
	}
}

void FilesHashProgressController::SetSpeedText(LPCTSTR text)
{
	if (m_parentWnd == NULL)
	{
		return;
	}

	CWnd* pWnd = m_parentWnd->GetDlgItem(IDC_STATIC_SPEED);
	if (pWnd != NULL)
	{
		pWnd->SetWindowText(text);
	}
}
