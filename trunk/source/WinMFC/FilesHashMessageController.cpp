#include "stdafx.h"

#include "FilesHashMessageController.h"

#include "WinMFC/WinMFCMessages.h"

#include <vector>

#include "Adapters/MfcBridge/ThreadDataExecutionAccess.h"
#include "FilesHashInputController.h"
#include "FilesHashLifecycleController.h"
#include "FilesHashResultViewController.h"
#include "WindowsUtils.h"
#include "WinCommon/WindowsStrings.h"

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

bool FilesHashMessageController::DispatchFileLoadOutcome(
	const FileLoadOutcome& outcome,
	LPCTSTR clearButtonText,
	LPCTSTR secondText,
	LPCTSTR noSelectionMessage,
	LPCTSTR overLimitMessage,
	LPCTSTR overLimitWithCountMessage,
	LPCTSTR truncatedMessage,
	LPCTSTR maybeTruncatedMessage,
	LPCTSTR errorMessage) const
{
	if (m_hashLifecycleController == NULL || m_parentWnd == NULL)
	{
		return false;
	}

	CString message;
	switch (outcome.result)
	{
	case FileLoadResult::Success:
		m_hashLifecycleController->StartHashing(clearButtonText, secondText, noSelectionMessage);
		return true;

	case FileLoadResult::SuccessPossiblyTruncated:
		message.Format(maybeTruncatedMessage, outcome.limit);
		AfxMessageBox(message, MB_OK | MB_ICONWARNING);
		m_hashLifecycleController->StartHashing(clearButtonText, secondText, noSelectionMessage);
		return true;

	case FileLoadResult::SuccessWithTruncation:
		message.Format(truncatedMessage, outcome.loadedCount, outcome.limit);
		AfxMessageBox(message, MB_OK | MB_ICONWARNING);
		m_hashLifecycleController->StartHashing(clearButtonText, secondText, noSelectionMessage);
		return true;

	case FileLoadResult::RejectedOverLimit:
		if (outcome.hasRequestedCount)
		{
			message.Format(overLimitWithCountMessage, outcome.requestedCount, outcome.limit);
		}
		else
		{
			message.Format(overLimitMessage, outcome.limit);
		}
		AfxMessageBox(message, MB_OK | MB_ICONWARNING);
		return false;

	case FileLoadResult::EnumerationLimit:
		AfxMessageBox(WindowsStrings::GetCurrentUILang() == 2052
			? _T("文件夹包含过多内容或目录层级过深，请选择较小的文件夹。")
			: _T("The folder contains too many entries or nested folders. Select a smaller folder."), MB_OK | MB_ICONWARNING);
		return false;

	case FileLoadResult::Cancelled:
		return false;

	case FileLoadResult::Error:
		AfxMessageBox(errorMessage, MB_OK | MB_ICONERROR);
		return false;

	case FileLoadResult::Empty:
	default:
		return false;
	}
}

void FilesHashMessageController::HandleDropFiles(
	HDROP hDropInfo,
	LPCTSTR clearButtonText,
	LPCTSTR secondText,
	LPCTSTR noSelectionMessage,
	LPCTSTR overLimitMessage,
	LPCTSTR overLimitWithCountMessage,
	LPCTSTR truncatedMessage,
	LPCTSTR maybeTruncatedMessage,
	LPCTSTR errorMessage) const
{
	if (m_threadData == NULL || m_parentWnd == NULL || m_hashInputController == NULL || m_hashLifecycleController == NULL)
	{
		DragFinish(hDropInfo);
		return;
	}

	if (WindowsUtils::CanAcceptUntrustedFileInput() && !m_hashInputController->IsLoadingFiles() && !IsThreadDataWorking(*m_threadData))
	{
		m_parentWnd->DragAcceptFiles(FALSE);
		FileLoadOutcome outcome = m_hashInputController->LoadDroppedFiles(hDropInfo);
		m_parentWnd->DragAcceptFiles(TRUE);
		DispatchFileLoadOutcome(
			outcome,
			clearButtonText,
			secondText,
			noSelectionMessage,
			overLimitMessage,
			overLimitWithCountMessage,
			truncatedMessage,
			maybeTruncatedMessage,
			errorMessage);
	}
	else
	{
		DragFinish(hDropInfo);
	}
}

BOOL FilesHashMessageController::HandleCopyData(
	const CWnd* pSenderWnd,
	const COPYDATASTRUCT* pCopyDataStruct,
	LPCTSTR clearButtonText,
	LPCTSTR secondText,
	LPCTSTR noSelectionMessage,
	LPCTSTR overLimitMessage,
	LPCTSTR overLimitWithCountMessage,
	LPCTSTR truncatedMessage,
	LPCTSTR maybeTruncatedMessage,
	LPCTSTR errorMessage) const
{
	if (pCopyDataStruct == NULL || m_threadData == NULL || m_parentWnd == NULL || m_hashInputController == NULL || m_hashLifecycleController == NULL)
	{
		return FALSE;
	}

	// WM_COPYDATA's HWND is supplied by the caller, not an authenticated
	// sender identity. Ordinary instances accept only validated file input;
	// elevated/high-integrity instances do not accept this IPC at all.
	(void)pSenderWnd;
	if (pCopyDataStruct->dwData == 0 &&
		WindowsUtils::CanAcceptUntrustedFileInput() &&
		!m_hashInputController->IsLoadingFiles() &&
		!IsThreadDataWorking(*m_threadData))
	{
		FileLoadOutcome outcome = m_hashInputController->LoadCopyDataFiles(pCopyDataStruct);
		if (outcome.result == FileLoadResult::Success ||
			outcome.result == FileLoadResult::SuccessPossiblyTruncated ||
			outcome.result == FileLoadResult::SuccessWithTruncation)
		{
			m_parentWnd->SetForegroundWindow();
		}

		DispatchFileLoadOutcome(
			outcome,
			clearButtonText,
			secondText,
			noSelectionMessage,
			overLimitMessage,
			overLimitWithCountMessage,
			truncatedMessage,
			maybeTruncatedMessage,
			errorMessage);
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
