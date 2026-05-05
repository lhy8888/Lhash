#include "stdafx.h"

#include "FilesHashMessageController.h"

#include <vector>

#include "Adapters/ThreadDataBridge/ThreadDataExecutionAccess.h"
#include "FilesHashInputController.h"
#include "FilesHashLifecycleController.h"
#include "FilesHashResultViewController.h"
#include "WinCommon/WinHandleGuard.h"

namespace
{
	bool QueryWindowProcessImagePath(const CWnd* pSenderWnd, sunjwbase::tstring& processImagePath)
	{
		if (pSenderWnd == NULL || !::IsWindow(pSenderWnd->GetSafeHwnd()))
		{
			return false;
		}

		DWORD dwSenderProcessId = 0;
		GetWindowThreadProcessId(pSenderWnd->GetSafeHwnd(), &dwSenderProcessId);
		if (dwSenderProcessId == 0)
		{
			return false;
		}

		WinHandleGuard::UniqueWinHandle senderProcess(OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, dwSenderProcessId));
		if (!senderProcess.isValid())
		{
			return false;
		}

		std::vector<TCHAR> processPath(32768, 0);
		DWORD cchExecutable = static_cast<DWORD>(processPath.size());
		if (!QueryFullProcessImageName(senderProcess.get(), 0, processPath.data(), &cchExecutable) ||
			cchExecutable == 0)
		{
			return false;
		}

		processImagePath.assign(processPath.data(), cchExecutable);
		return !processImagePath.empty();
	}

	sunjwbase::tstring GetTrustedExplorerImagePath()
	{
		std::vector<TCHAR> windowsDirectory(32768, 0);
		UINT cchWindowsDirectory = GetWindowsDirectory(windowsDirectory.data(), static_cast<UINT>(windowsDirectory.size()));
		if (cchWindowsDirectory == 0 || cchWindowsDirectory >= windowsDirectory.size())
		{
			return _T("");
		}

		sunjwbase::tstring trustedPath(windowsDirectory.data(), cchWindowsDirectory);
		if (trustedPath[trustedPath.length() - 1] != _T('\\') &&
			trustedPath[trustedPath.length() - 1] != _T('/'))
		{
			trustedPath += _T("\\");
		}
		trustedPath += _T("explorer.exe");
		return trustedPath;
	}

	sunjwbase::tstring GetCurrentExecutableImagePath()
	{
		std::vector<TCHAR> executablePath(32768, 0);
		DWORD cchExecutable = GetModuleFileName(NULL, executablePath.data(), static_cast<DWORD>(executablePath.size()));
		if (cchExecutable == 0 || cchExecutable >= executablePath.size())
		{
			return _T("");
		}

		return sunjwbase::tstring(executablePath.data(), cchExecutable);
	}
}

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

	if (!IsThreadDataWorking(*m_threadData))
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

bool FilesHashMessageController::IsTrustedCopyDataSender(const CWnd* pSenderWnd)
{
	sunjwbase::tstring senderImagePath;
	if (!QueryWindowProcessImagePath(pSenderWnd, senderImagePath))
	{
		return false;
	}

	const TCHAR* pszFileName = _tcsrchr(senderImagePath.c_str(), _T('\\'));
	pszFileName = (pszFileName != NULL) ? (pszFileName + 1) : senderImagePath.c_str();
	if (_tcsicmp(pszFileName, _T("explorer.exe")) == 0)
	{
		sunjwbase::tstring trustedExplorerPath = GetTrustedExplorerImagePath();
		return !trustedExplorerPath.empty() && _tcsicmp(senderImagePath.c_str(), trustedExplorerPath.c_str()) == 0;
	}

	if (_tcsicmp(pszFileName, _T("LHash.exe")) == 0)
	{
		sunjwbase::tstring currentExecutablePath = GetCurrentExecutableImagePath();
		return !currentExecutablePath.empty() && _tcsicmp(senderImagePath.c_str(), currentExecutablePath.c_str()) == 0;
	}

	return false;
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

	if (pCopyDataStruct->dwData == 0 &&
		IsTrustedCopyDataSender(pSenderWnd) &&
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
