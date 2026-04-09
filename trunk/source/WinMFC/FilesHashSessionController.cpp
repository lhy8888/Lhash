#include "stdafx.h"

#include <CommCtrl.h>

#include "FilesHashSessionController.h"

#include "LegacyCompat/HashThreadLaunch.h"
#include "LegacyCompat/ThreadDataExecutionAccess.h"
#include "FilesHashAlgorithmSelectionController.h"

FilesHashSessionController::FilesHashSessionController()
	: m_threadData(NULL),
	m_parentWnd(NULL),
	m_mainEditDropTarget(NULL),
	m_btnOpen(NULL),
	m_btnClr(NULL),
	m_btnFind(NULL),
	m_btnContext(NULL),
	m_chkUppercase(NULL),
	m_btnOpenFolder(NULL),
	m_btnSettings(NULL),
	m_hashAlgorithmSelectionController(NULL),
	m_hWorkThread()
{
}

FilesHashSessionController::~FilesHashSessionController()
{
	CloseWorkThreadHandle();
}

void FilesHashSessionController::Initialize(ThreadData* threadData,
	CDialog* parentWnd,
	CWnd* mainEditDropTarget,
	CButton* btnOpen,
	CButton* btnClr,
	CButton* btnFind,
	CButton* btnContext,
	CButton* chkUppercase,
	FilesHashAlgorithmSelectionController* hashAlgorithmSelectionController)
{
	m_threadData = threadData;
	m_parentWnd = parentWnd;
	m_mainEditDropTarget = mainEditDropTarget;
	m_btnOpen = btnOpen;
	m_btnClr = btnClr;
	m_btnFind = btnFind;
	m_btnContext = btnContext;
	m_chkUppercase = chkUppercase;
	m_hashAlgorithmSelectionController = hashAlgorithmSelectionController;
}

void FilesHashSessionController::AttachSupplementalControls(CButton* btnOpenFolder, CButton* btnSettings)
{
	m_btnOpenFolder = btnOpenFolder;
	m_btnSettings = btnSettings;
}

BOOL FilesHashSessionController::PrepareHashStart(LPCTSTR noSelectionMessage)
{
	if (m_threadData == NULL)
	{
		return FALSE;
	}

	SetThreadDataUppercase(*m_threadData, (m_chkUppercase != NULL && m_chkUppercase->GetCheck() != FALSE));
	if (m_hashAlgorithmSelectionController != NULL)
	{
		m_hashAlgorithmSelectionController->SyncSelections();
		return m_hashAlgorithmSelectionController->ValidateSelection(noSelectionMessage);
	}

	return TRUE;
}

void FilesHashSessionController::StartHashThread()
{
	if (m_threadData == NULL)
	{
		return;
	}

	SetThreadDataStop(*m_threadData, false);

	unsigned int thredID = 0;
	RestartHashWorkerThread(&m_hWorkThread, m_threadData, &thredID);
}

void FilesHashSessionController::StopWorkingThread()
{
	if (m_threadData != NULL && IsThreadDataWorking(*m_threadData))
	{
		SetThreadDataStop(*m_threadData, true);
	}
}

void FilesHashSessionController::SetControls(BOOL working, BOOL limited, LPCTSTR openButtonText, LPCTSTR stopButtonText)
{
	if (working)
	{
		PrepareDropTarget(m_parentWnd, FALSE);
		PrepareDropTarget(m_mainEditDropTarget, FALSE);
		if (m_btnOpen != NULL)
		{
			m_btnOpen->EnableWindow(TRUE);
			m_btnOpen->SetWindowText(stopButtonText);
		}
		if (m_btnClr != NULL)
		{
			m_btnClr->EnableWindow(FALSE);
		}
		if (m_btnFind != NULL)
		{
			m_btnFind->EnableWindow(FALSE);
		}
		if (m_btnContext != NULL)
		{
			m_btnContext->EnableWindow(FALSE);
		}
		if (m_btnOpenFolder != NULL)
		{
			m_btnOpenFolder->EnableWindow(FALSE);
		}
		if (m_btnSettings != NULL)
		{
			m_btnSettings->EnableWindow(FALSE);
		}
		if (m_chkUppercase != NULL)
		{
			m_chkUppercase->EnableWindow(FALSE);
		}
		if (m_hashAlgorithmSelectionController != NULL)
		{
			m_hashAlgorithmSelectionController->SetEnabled(FALSE);
		}
	}
	else
	{
		if (m_btnOpen != NULL)
		{
			m_btnOpen->EnableWindow(TRUE);
			m_btnOpen->SetWindowText(openButtonText);
		}
		if (m_btnClr != NULL)
		{
			m_btnClr->EnableWindow(TRUE);
		}
		if (m_btnFind != NULL)
		{
			m_btnFind->EnableWindow(TRUE);
		}
		if (m_btnContext != NULL)
		{
			Button_SetElevationRequiredState(m_btnContext->GetSafeHwnd(), limited ? TRUE : FALSE);
			m_btnContext->EnableWindow(TRUE);
		}
		if (m_btnOpenFolder != NULL)
		{
			m_btnOpenFolder->EnableWindow(TRUE);
		}
		if (m_btnSettings != NULL)
		{
			m_btnSettings->EnableWindow(TRUE);
		}
		if (m_chkUppercase != NULL)
		{
			m_chkUppercase->EnableWindow(TRUE);
		}
		if (m_hashAlgorithmSelectionController != NULL)
		{
			m_hashAlgorithmSelectionController->SetEnabled(TRUE);
		}
		PrepareDropTarget(m_parentWnd, TRUE);
		PrepareDropTarget(m_mainEditDropTarget, TRUE);
	}

	if (m_parentWnd != NULL && m_btnOpen != NULL)
	{
		m_parentWnd->GotoDlgCtrl(m_btnOpen);
	}
}

void FilesHashSessionController::AllowMessageForWindow(HWND hWnd, UINT message)
{
	if (hWnd == NULL)
	{
		return;
	}

	HMODULE hUser32 = GetModuleHandle(_T("user32.dll"));
	if (hUser32 == NULL)
	{
		return;
	}

	LPFN_CHANGEWINDOWMESSAGEFILTEREX pChangeWindowMessageFilterEx =
		reinterpret_cast<LPFN_CHANGEWINDOWMESSAGEFILTEREX>(GetProcAddress(hUser32, "ChangeWindowMessageFilterEx"));
	if (pChangeWindowMessageFilterEx != NULL)
	{
		WindowMessageFilterStatus cfs = { sizeof(WindowMessageFilterStatus), 0 };
		pChangeWindowMessageFilterEx(hWnd, message, WINDOW_MESSAGE_FILTER_ACTION_ALLOW, &cfs);
		return;
	}

	LPFN_CHANGEWINDOWMESSAGEFILTER pChangeWindowMessageFilter =
		reinterpret_cast<LPFN_CHANGEWINDOWMESSAGEFILTER>(GetProcAddress(hUser32, "ChangeWindowMessageFilter"));
	if (pChangeWindowMessageFilter != NULL)
	{
		pChangeWindowMessageFilter(message, WINDOW_MESSAGE_FILTER_ACTION_ALLOW);
	}
}

void FilesHashSessionController::PrepareDropTarget(CWnd* pWnd, BOOL bAccept)
{
	if (pWnd == NULL || !::IsWindow(pWnd->GetSafeHwnd()))
	{
		return;
	}

	if (bAccept)
	{
		pWnd->ModifyStyleEx(0, WS_EX_ACCEPTFILES, 0);
	}
	else
	{
		pWnd->ModifyStyleEx(WS_EX_ACCEPTFILES, 0, 0);
	}

	pWnd->DragAcceptFiles(bAccept);
	if (bAccept)
	{
		AllowMessageForWindow(pWnd->GetSafeHwnd(), WM_DROPFILES);
		AllowMessageForWindow(pWnd->GetSafeHwnd(), WM_COPYDATA);
		AllowMessageForWindow(pWnd->GetSafeHwnd(), 0x0049);
	}
}

void FilesHashSessionController::CloseWorkThreadHandle()
{
	CloseHashWorkerThreadHandle(&m_hWorkThread);
}
