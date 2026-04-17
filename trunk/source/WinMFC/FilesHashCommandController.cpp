#include "stdafx.h"

#include "FilesHashCommandController.h"

#include "resource.h"

#include "AboutDlg.h"
#include "LegacyCompat/ThreadDataExecutionAccess.h"
#include "FilesHashInputController.h"
#include "FilesHashLifecycleController.h"
#include "FilesHashMessageController.h"
#include "FilesHashProgressController.h"
#include "FilesHashResultViewController.h"
#include "FilesHashSearchController.h"
#include "FilesHashSessionController.h"
#include "FindDlg.h"
#include "Common/strhelper.h"
#include <afxdlgs.h>

FilesHashCommandController::FilesHashCommandController()
	: m_threadData(NULL),
	m_parentWnd(NULL),
	m_btnClr(NULL),
	m_hashInputController(NULL),
	m_hashSearchController(NULL),
	m_hashSessionController(NULL),
	m_hashLifecycleController(NULL),
	m_hashMessageController(NULL),
	m_hashProgressController(NULL),
	m_hashResultViewController(NULL)
{
}

void FilesHashCommandController::HandleOpenFolderButtonClick(
	LPCTSTR folderDialogTitle,
	LPCTSTR emptyFolderMessage,
	LPCTSTR clearButtonText,
	LPCTSTR secondText,
	LPCTSTR noSelectionMessage,
	LPCTSTR overLimitMessage,
	LPCTSTR overLimitWithCountMessage,
	LPCTSTR truncatedMessage,
	LPCTSTR maybeTruncatedMessage,
	LPCTSTR errorMessage)
{
	if (m_threadData == NULL || m_hashLifecycleController == NULL || m_hashMessageController == NULL || IsThreadDataWorking(*m_threadData))
	{
		return;
	}

	if (m_hashInputController != NULL)
	{
		FileLoadOutcome outcome = m_hashInputController->LoadFolderDialogSelection(folderDialogTitle, emptyFolderMessage);
		m_hashMessageController->DispatchFileLoadOutcome(
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
}

void FilesHashCommandController::Initialize(
	ThreadData* threadData,
	CDialog* parentWnd,
	CButton* btnClr,
	FilesHashInputController* hashInputController,
	FilesHashSearchController* hashSearchController,
	FilesHashSessionController* hashSessionController,
	FilesHashLifecycleController* hashLifecycleController,
	FilesHashMessageController* hashMessageController,
	FilesHashProgressController* hashProgressController,
	FilesHashResultViewController* hashResultViewController)
{
	m_threadData = threadData;
	m_parentWnd = parentWnd;
	m_btnClr = btnClr;
	m_hashInputController = hashInputController;
	m_hashSearchController = hashSearchController;
	m_hashSessionController = hashSessionController;
	m_hashLifecycleController = hashLifecycleController;
	m_hashMessageController = hashMessageController;
	m_hashProgressController = hashProgressController;
	m_hashResultViewController = hashResultViewController;
}

void FilesHashCommandController::HandleOpenButtonClick(
	LPCTSTR fileFilter,
	LPCTSTR clearButtonText,
	LPCTSTR secondText,
	LPCTSTR noSelectionMessage,
	LPCTSTR overLimitMessage,
	LPCTSTR overLimitWithCountMessage,
	LPCTSTR truncatedMessage,
	LPCTSTR maybeTruncatedMessage,
	LPCTSTR errorMessage)
{
	if (m_threadData == NULL || m_hashLifecycleController == NULL || m_hashMessageController == NULL)
	{
		return;
	}

	if (!IsThreadDataWorking(*m_threadData))
	{
		if (m_hashInputController != NULL)
		{
			FileLoadOutcome outcome = m_hashInputController->LoadOpenFileDialogSelection(fileFilter);
			m_hashMessageController->DispatchFileLoadOutcome(
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
	}
	else if (m_hashSessionController != NULL)
	{
		m_hashSessionController->StopWorkingThread();
	}
}

void FilesHashCommandController::HandleExitButtonClick() const
{
	if (m_parentWnd != NULL)
	{
		m_parentWnd->PostMessage(WM_CLOSE);
	}
}

void FilesHashCommandController::HandleAboutButtonClick() const
{
	CAboutDlg about;
	about.DoModal();
}

void FilesHashCommandController::HandleCleanButtonClick(LPCTSTR clearButtonText, LPCTSTR clearVerifyButtonText)
{
	if (m_threadData == NULL ||
		m_btnClr == NULL ||
		m_hashResultViewController == NULL ||
		m_hashProgressController == NULL ||
		IsThreadDataWorking(*m_threadData))
	{
		return;
	}

	CString buttonText;
	m_btnClr->GetWindowText(buttonText);
	if (buttonText.Compare(clearButtonText) == 0)
	{
		m_hashResultViewController->ClearResults(*m_threadData);
		ClearProgressLabels();
		m_hashProgressController->SetWholeProgress(0);
		m_hashProgressController->ResetTaskSession();
	}
	else if (buttonText.Compare(clearVerifyButtonText) == 0 &&
		m_hashSearchController != NULL)
	{
		m_hashSearchController->ClearSearch(clearButtonText);
		m_hashResultViewController->RefreshMainText();
	}
}

void FilesHashCommandController::HandleCopyButtonClick() const
{
	if (m_hashResultViewController != NULL)
	{
		m_hashResultViewController->CopyAllResults();
	}
}

void FilesHashCommandController::HandleExportButtonClick(LPCTSTR exportFilter, LPCTSTR defaultFileName) const
{
	if (m_parentWnd == NULL || m_hashResultViewController == NULL)
	{
		return;
	}

	CFileDialog saveDialog(FALSE, _T("txt"), defaultFileName, OFN_HIDEREADONLY | OFN_OVERWRITEPROMPT, exportFilter, m_parentWnd, 0);
	if (IDOK != saveDialog.DoModal())
	{
		return;
	}

	CString currentText = m_hashResultViewController->GetCurrentText();
	CFile outputFile;
	if (!outputFile.Open(saveDialog.GetPathName(), CFile::modeCreate | CFile::modeWrite | CFile::typeBinary))
	{
		return;
	}

	static const BYTE utf8Bom[] = { 0xEF, 0xBB, 0xBF };
	outputFile.Write(utf8Bom, sizeof(utf8Bom));

	sunjwbase::tstring exportText = currentText.GetString();
	std::string utf8Text = sunjwbase::tstrtostrutf8(exportText);
	if (!utf8Text.empty())
	{
		outputFile.Write(utf8Text.data(), static_cast<UINT>(utf8Text.length()));
	}
	outputFile.Close();
}

void FilesHashCommandController::HandleFindButtonClick(LPCTSTR clearVerifyButtonText) const
{
	if (m_hashSearchController == NULL || m_hashResultViewController == NULL)
	{
		return;
	}

	CFindDlg findDialog;
	findDialog.SetFindHash(_T(""));
	if (IDOK == findDialog.DoModal())
	{
		if (m_hashSearchController->BeginSearch(CString(), findDialog.GetFindHash(), clearVerifyButtonText))
		{
			m_hashResultViewController->RefreshMainText(FALSE);
		}
	}
}

void FilesHashCommandController::ClearProgressLabels() const
{
	if (m_parentWnd == NULL)
	{
		return;
	}

	CWnd* pWnd = m_parentWnd->GetDlgItem(IDC_STATIC_TIME);
	if (pWnd != NULL)
	{
		pWnd->SetWindowText(_T(""));
	}

	pWnd = m_parentWnd->GetDlgItem(IDC_STATIC_SPEED);
	if (pWnd != NULL)
	{
		pWnd->SetWindowText(_T(""));
	}
}
