#include "stdafx.h"

#include "FilesHashLifecycleController.h"

#include "Adapters/MfcBridge/ThreadDataExecutionAccess.h"
#include "FilesHashProgressController.h"
#include "FilesHashResultViewController.h"
#include "FilesHashSearchController.h"
#include "FilesHashSessionController.h"
#include "UIBridgeMFC.h"
#include "Domain/HashAlgorithmRegistryCore.h"
#include "Adapters/MfcBridge/ThreadDataInputAccess.h"
#include "WinCommon/WindowsStrings.h"

namespace
{
static sunjwbase::tstring BuildEnabledAlgorithmSummary(const ThreadData& threadData)
{
	sunjwbase::tstring algorithmSummary;
	VisitEnabledThreadDataHashAlgorithmIds(threadData, [&](const HashAlgorithmId& algorithmId)
	{
		int algorithmIndex = -1;
		if (!TryGetHashAlgorithmIndexById(algorithmId, &algorithmIndex))
		{
			return true;
		}

		const HashAlgorithmDescriptor& descriptor = GetHashAlgorithmDescriptorAt(algorithmIndex);

		if (!algorithmSummary.empty())
		{
			algorithmSummary += _T(", ");
		}
		algorithmSummary += GetHashAlgorithmDescriptorDisplayLabel(descriptor);
		return true;
	});
	return algorithmSummary;
}
}

FilesHashLifecycleController::FilesHashLifecycleController()
	: m_threadData(NULL),
	m_parentWnd(NULL),
	m_btnClr(NULL),
	m_uiBridgeMFC(NULL),
	m_hashSearchController(NULL),
	m_hashSessionController(NULL),
	m_hashProgressController(NULL),
	m_hashResultViewController(NULL),
	m_waitingExit(FALSE),
	m_workerPollTimer(0),
	m_terminalMessage(0),
	m_sessionSetupFailed(false)
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
		m_hashResultViewController == NULL ||
		m_uiBridgeMFC == NULL || *m_uiBridgeMFC == NULL ||
		m_parentWnd == NULL || m_waitingExit || IsThreadDataWorking(*m_threadData))
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

	// Establish the nonblocking exit monitor before launching. If timer creation
	// fails, no worker is allowed to outlive an unmonitored session.
	m_workerPollTimer = m_parentWnd->SetTimer(5, 30, NULL);
	if (m_workerPollTimer == 0)
	{
		AfxMessageBox(GetStringByKey(MAINDLG_WORKER_START_FAILED), MB_OK | MB_ICONERROR);
		return;
	}
	m_terminalMessage = 0;
	m_sessionSetupFailed = false;
	++m_sessionId;
	(*m_uiBridgeMFC)->BeginTaskSession(m_sessionId);
	if (!m_hashSessionController->StartHashThread())
	{
		StopWorkerPoll();
		m_hashProgressController->ResetAfterStop();
		m_hashProgressController->SetWholeProgress(0);
		AfxMessageBox(GetStringByKey(MAINDLG_WORKER_START_FAILED), MB_OK | MB_ICONERROR);
		return;
	}
	// Worker notifications are posted and cannot be handled until this UI
	// handler returns. Failed launches leave the existing task history intact.
	try
	{
		m_hashProgressController->BeginTaskSession(m_sessionId, GetThreadDataInputFiles(*m_threadData), BuildEnabledAlgorithmSummary(*m_threadData));
		m_hashProgressController->StartTiming(secondText);
	}
	catch (...)
	{
		// Keep the timer and ownership lease intact even if UI allocation fails.
		m_sessionSetupFailed = true;
		m_hashSessionController->StopWorkingThread();
	}
}

void FilesHashLifecycleController::HandleTimer(UINT_PTR nIDEvent, LPCTSTR secondText, LPCTSTR clearButtonText, LPCTSTR noSelectionMessage)
{
	if (m_workerPollTimer != 0 && nIDEvent == m_workerPollTimer)
	{
		PollWorkerExit();
	}
	else if (nIDEvent == 1)
	{
		if (m_hashProgressController != NULL)
		{
			m_hashProgressController->AdvanceTimeTick(secondText);
		}
	}
	else if (nIDEvent == 4)
	{
		if (m_parentWnd != NULL)
		{
			m_parentWnd->KillTimer(4);
		}
		StartHashing(clearButtonText, secondText, noSelectionMessage);
	}
}

LRESULT FilesHashLifecycleController::HandleThreadMessage(WPARAM wParam, LPARAM lParam, BOOL limited, LPCTSTR openButtonText, LPCTSTR stopButtonText)
{
	switch (wParam)
	{
	case WP_WORKER_JOINED:
		FinalizeWorkerExit();
		break;
	case WP_WORKING:
		if (m_hashSessionController != NULL && m_threadData != NULL && IsThreadDataWorking(*m_threadData))
		{
			m_hashSessionController->SetControls(TRUE, limited, openButtonText, stopButtonText);
		}
		break;
	case WP_REFRESH_TEXT:
		if (m_uiBridgeMFC != NULL && *m_uiBridgeMFC != NULL)
		{
			(*m_uiBridgeMFC)->MarkMainTextRefreshHandled();
		}
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
	case WP_STOPPED:
		// A posted terminal event does not mean the worker has returned. Keep
		// controls and object ownership busy until the real handle is signalled.
		m_terminalMessage = wParam;
		break;
	}

	return 0;
}

BOOL FilesHashLifecycleController::HandleClose()
{
	m_waitingExit = TRUE;
	if (m_parentWnd != NULL)
	{
		m_parentWnd->KillTimer(4);
	}
	if (m_threadData != NULL && IsThreadDataWorking(*m_threadData))
	{
		m_waitingExit = TRUE;
		if (m_hashSessionController != NULL)
		{
			m_hashSessionController->StopWorkingThread();
		}
		return TRUE;
	}

	StopWorkerPoll();
	ReleaseBridge();
	return FALSE;
}

void FilesHashLifecycleController::StopWorkerPoll()
{
	if (m_workerPollTimer != 0 && m_parentWnd != NULL)
	{
		m_parentWnd->KillTimer(m_workerPollTimer);
	}
	m_workerPollTimer = 0;
}

void FilesHashLifecycleController::PollWorkerExit()
{
	if (m_threadData == NULL || m_hashSessionController == NULL || m_parentWnd == NULL)
	{
		return;
	}
	m_threadData->executionState.workerSession.QueueFinalization(m_hashSessionController->HasWorkerExited() != FALSE, [&]()
	{
		return m_parentWnd->PostMessage(WM_THREAD_INFO, WP_WORKER_JOINED) != FALSE;
	});
}

void FilesHashLifecycleController::FinalizeWorkerExit()
{
	if (m_threadData == NULL) return;
	m_threadData->executionState.workerSession.FinalizationHandled();
	DWORD exitCode = 1;
	if (m_hashSessionController == NULL || !m_hashSessionController->TryCollectExitedThread(&exitCode))
	{
		return;
	}
	StopWorkerPoll();
	const bool completedNormally = exitCode == 0 && m_terminalMessage == WP_FINISHED && !m_sessionSetupFailed;
	const bool cancelledNormally = exitCode == 0 && m_terminalMessage == WP_STOPPED && !m_sessionSetupFailed;
	bool sessionConsistent = true;
	if (m_uiBridgeMFC != NULL && *m_uiBridgeMFC != NULL && m_hashProgressController != NULL)
	{
		std::vector<FilesHashTaskUpdate> updates;
		(*m_uiBridgeMFC)->MarkTaskUpdatesHandled();
		(*m_uiBridgeMFC)->DrainPendingTaskUpdates(updates);
		m_hashProgressController->ApplyTaskUpdates(updates);
	}
	if (m_hashProgressController != NULL && m_threadData != NULL)
	{
		const FilesHashSessionOutcome outcome = completedNormally ? FilesHashSessionOutcome::Completed :
			(cancelledNormally ? FilesHashSessionOutcome::Cancelled : FilesHashSessionOutcome::Aborted);
		sessionConsistent = m_hashProgressController->FinishTaskSession(m_sessionId, outcome);
		if (completedNormally && sessionConsistent)
		{
			m_hashProgressController->FinishTiming(GetThreadDataTotalSize(*m_threadData));
			m_hashProgressController->SetWholeProgress(99);
		}
		else
		{
			m_hashProgressController->ResetAfterStop();
			m_hashProgressController->SetWholeProgress(0);
		}
	}
	if (m_hashResultViewController != NULL)
	{
		m_hashResultViewController->RefreshMainText();
	}
	if (m_waitingExit && m_parentWnd != NULL)
	{
		// Already joined, and this runs on the UI thread. Finish the pending
		// close now so a queued click cannot open another modal input dialog.
		m_parentWnd->SendMessage(WM_CLOSE);
	}
	else if (exitCode != 0 || m_terminalMessage == 0 || m_sessionSetupFailed || !sessionConsistent)
	{
		AfxMessageBox(GetStringByKey(MAINDLG_WORKER_FAILED), MB_OK | MB_ICONERROR);
	}
}

void FilesHashLifecycleController::Shutdown()
{
	StopWorkerPoll();
	if (m_hashSessionController != NULL)
	{
		m_hashSessionController->WaitForWorkerExit();
	}
	ReleaseBridge();
}

void FilesHashLifecycleController::ReleaseBridge()
{
	if (m_uiBridgeMFC != NULL && *m_uiBridgeMFC != NULL)
	{
		delete *m_uiBridgeMFC;
		*m_uiBridgeMFC = NULL;
	}
	if (m_threadData != NULL)
	{
		SetThreadDataObserver(*m_threadData, NULL);
	}

	m_waitingExit = FALSE;
}
