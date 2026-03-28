#include "stdafx.h"

#include "FilesHashMessageController.h"

#include "Common/ThreadDataExecutionAccess.h"
#include "FilesHashInputController.h"
#include "FilesHashLifecycleController.h"
#include "FilesHashResultViewController.h"

FilesHashMessageController::FilesHashMessageController()
	: m_threadData(NULL),
	m_parentWnd(NULL),
	m_hashInputController(NULL),
	m_hashLifecycleController(NULL),
	m_hashResultViewController(NULL)
{
}

void FilesHashMessageController::Initialize(
	ThreadData* threadData,
	CDialog* parentWnd,
	FilesHashInputController* hashInputController,
	FilesHashLifecycleController* hashLifecycleController,
	FilesHashResultViewController* hashResultViewController)
{
	m_threadData = threadData;
	m_parentWnd = parentWnd;
	m_hashInputController = hashInputController;
	m_hashLifecycleController = hashLifecycleController;
	m_hashResultViewController = hashResultViewController;
}

BOOL FilesHashMessageController::HandlePaint(HICON icon) const
{
	if (m_parentWnd == NULL || !m_parentWnd->IsIconic())
	{
		return FALSE;
	}

	CPaintDC dc(m_parentWnd);
	m_parentWnd->SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

	int cxIcon = GetSystemMetrics(SM_CXICON);
	int cyIcon = GetSystemMetrics(SM_CYICON);
	CRect rect;
	m_parentWnd->GetClientRect(&rect);
	int x = (rect.Width() - cxIcon + 1) / 2;
	int y = (rect.Height() - cyIcon + 1) / 2;

	dc.DrawIcon(x, y, icon);
	return TRUE;
}

HCURSOR FilesHashMessageController::GetDragCursor(HICON icon) const
{
	return static_cast<HCURSOR>(icon);
}

void FilesHashMessageController::HandleDropFiles(HDROP hDropInfo, LPCTSTR clearButtonText, LPCTSTR secondText, LPCTSTR noSelectionMessage) const
{
	if (m_threadData == NULL || m_parentWnd == NULL || m_hashInputController == NULL || m_hashLifecycleController == NULL)
	{
		return;
	}

	if (!IsThreadDataWorking(*m_threadData))
	{
		m_parentWnd->DragAcceptFiles(FALSE);
		BOOL hasPendingFiles = m_hashInputController->LoadDroppedFiles(hDropInfo);
		m_parentWnd->DragAcceptFiles(TRUE);
		if (hasPendingFiles)
		{
			m_hashLifecycleController->StartHashing(clearButtonText, secondText, noSelectionMessage);
		}
	}
}

BOOL FilesHashMessageController::HandleCopyData(const COPYDATASTRUCT* pCopyDataStruct, LPCTSTR clearButtonText, LPCTSTR secondText, LPCTSTR noSelectionMessage) const
{
	if (pCopyDataStruct == NULL || m_threadData == NULL || m_parentWnd == NULL || m_hashInputController == NULL || m_hashLifecycleController == NULL)
	{
		return FALSE;
	}

	if (pCopyDataStruct->dwData == 0)
	{
		m_parentWnd->SetForegroundWindow();
	}

	if (pCopyDataStruct->dwData == 0 &&
		!IsThreadDataWorking(*m_threadData) &&
		m_hashInputController->LoadCopyDataFiles(pCopyDataStruct))
	{
		m_hashLifecycleController->StartHashing(clearButtonText, secondText, noSelectionMessage);
		return TRUE;
	}

	return FALSE;
}

LRESULT FilesHashMessageController::HandleCustomMessage(WPARAM wParam) const
{
	if (m_parentWnd == NULL || m_hashResultViewController == NULL)
	{
		return 0;
	}

	switch (wParam)
	{
	case WM_HYPEREDIT_MENU:
		m_hashResultViewController->ShowHyperEditMenu(m_parentWnd);
		break;
	}

	return 0;
}

void FilesHashMessageController::HandleInitMenuPopup(CMenu* pPopupMenu) const
{
	if (m_parentWnd == NULL || m_hashResultViewController == NULL)
	{
		return;
	}

	m_hashResultViewController->UpdatePopupMenu(m_parentWnd, pPopupMenu);
}

void FilesHashMessageController::HandleCopyHash() const
{
	if (m_hashResultViewController == NULL)
	{
		return;
	}

	m_hashResultViewController->CopyLastHyperlink();
}

void FilesHashMessageController::UpdateCopyHashMenuText(CCmdUI* pCmdUI, LPCTSTR copyText) const
{
	if (m_hashResultViewController == NULL)
	{
		return;
	}

	m_hashResultViewController->UpdateCopyHashMenuText(pCmdUI, copyText);
}
