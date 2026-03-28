#include "stdafx.h"

#include "FilesHashLifecycleController.h"

#include "Common/ThreadDataExecutionAccess.h"
#include "FilesHashProgressController.h"
#include "FilesHashResultViewController.h"
#include "FilesHashSearchController.h"
#include "FilesHashSessionController.h"
#include "UIBridgeMFC.h"

FilesHashLifecycleController::FilesHashLifecycleController()
	: m_threadData(NULL),
	m_parentWnd(NULL),
	m_btnClr(NULL),
	m_uiBridgeMFC(NULL),
	m_hashSearchController(NULL),
	m_hashSessionController(NULL),
	m_hashProgressController(NULL),
	m_hashResultViewController(NULL),
	m_waitingExit(FALSE)
{
}

void FilesHashLifecycleController::Initialize(
	ThreadData* threadData,
	CDialog* parentWnd,
	CButton* btnClr,
	UIBridgeMFC** uiBridgeMFC,
	FilesHashSearchController* hashSearchController,
	FilesHashSessionController* hashSessionController,
	FilesHashProgressController* hashProgressController,
	FilesHashResultViewController* hashResultViewController)
{
	m_threadData = threadData;
	m_parentWnd = parentWnd;
	m_btnClr = btnClr;
	m_uiBridgeMFC = uiBridgeMFC;
	m_hashSearchController = hashSearchController;
	m_hashSessionController = hashSessionController;
	m_hashProgressController = hashProgressController;
	m_hashResultViewController = hashResultViewController;
	m_waitingExit = FALSE;
}

void FilesHashLifecycleController::StartHashing(LPCTSTR clearButtonText, LPCTSTR secondText, LPCTSTR noSelectionMessage)
{
	if (m_threadData == NULL ||
		m_btnClr == NULL ||
		m_hashSessionController == NULL ||
		m_hashProgressController == NULL ||
		m_hashResultViewController == NULL)
	{
		return;
	}

	if (m_hashSearchController != NULL && m_hashSearchController->IsActive())
	{
		m_hashSearchController->ClearSearch(clearButtonText);
		m_hashResultViewController->RefreshMainText();
	}

	m_btnClr->SetWindowText(clearButtonText);

	m_hashProgressController->PrepareAdvTaskbar();
	m_hashProgressController->SetWholeProgress(0);

	if (!m_hashSessionController->PrepareHashStart(noSelectionMessage))
	{
		return;
	}

	m_hashProgressController->StartTiming(secondText);
	m_hashSessionController->StartHashThread();
}

void FilesHashLifecycleController::HandleTimer(UINT_PTR nIDEvent, LPCTSTR secondText, LPCTSTR clearButtonText, LPCTSTR noSelectionMessage)
{
	if (nIDEvent == 1)
	{
		if (m_hashProgressController != NULL)
		{
			m_hashProgressController->AdvanceTimeTick(secondText);
		}
	}
	else if (nIDEvent == 4)
	{
		StartHashing(clearButtonText, secondText, noSelectionMessage);
		if (m_parentWnd != NULL)
		{
			m_parentWnd->KillTimer(4);
		}
	}
}

LRESULT FilesHashLifecycleController::HandleThreadMessage(WPARAM wParam, LPARAM lParam, BOOL limited, LPCTSTR openButtonText, LPCTSTR stopButtonText)
{
	switch (wParam)
	{
	case WP_WORKING:
		if (m_hashSessionController != NULL)
		{
			m_hashSessionController->SetControls(TRUE, limited, openButtonText, stopButtonText);
		}
		break;
	case WP_REFRESH_TEXT:
		if (m_hashResultViewController != NULL)
		{
			m_hashResultViewController->RefreshMainText();
		}
		break;
	case WP_PROG_WHOLE:
		if (m_hashProgressController != NULL)
		{
			m_hashProgressController->SetWholeProgress((int)lParam);
		}
		break;
	case WP_FINISHED:
		if (m_hashProgressController != NULL && m_threadData != NULL)
		{
			m_hashProgressController->FinishTiming(GetThreadDataTotalSize(*m_threadData));
		}
		if (m_hashSessionController != NULL)
		{
			m_hashSessionController->SetControls(FALSE, limited, openButtonText, stopButtonText);
		}
		if (m_hashProgressController != NULL)
		{
			m_hashProgressController->SetWholeProgress(99);
		}
		break;
	case WP_STOPPED:
		if (m_hashProgressController != NULL)
		{
			m_hashProgressController->ResetAfterStop();
			m_hashProgressController->SetWholeProgress(0);
		}
		if (m_hashSessionController != NULL)
		{
			m_hashSessionController->SetControls(FALSE, limited, openButtonText, stopButtonText);
		}
		if (m_hashResultViewController != NULL)
		{
			m_hashResultViewController->AppendLineBreakAndScrollEnd();
		}
		if (m_waitingExit && m_parentWnd != NULL)
		{
			m_parentWnd->PostMessage(WM_CLOSE);
		}
		break;
	}

	return 0;
}

BOOL FilesHashLifecycleController::HandleClose()
{
	if (m_threadData != NULL && IsThreadDataWorking(*m_threadData))
	{
		m_waitingExit = TRUE;
		if (m_hashSessionController != NULL)
		{
			m_hashSessionController->StopWorkingThread();
		}
		return TRUE;
	}

	ReleaseBridge();
	return FALSE;
}

void FilesHashLifecycleController::ReleaseBridge()
{
	if (m_uiBridgeMFC != NULL && *m_uiBridgeMFC != NULL)
	{
		delete *m_uiBridgeMFC;
		*m_uiBridgeMFC = NULL;
	}

	m_waitingExit = FALSE;
}
