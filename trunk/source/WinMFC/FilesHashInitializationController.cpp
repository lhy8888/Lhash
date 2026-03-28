#include "stdafx.h"

#include "FilesHashInitializationController.h"

#include "resource.h"

#include "Common/ThreadDataAccess.h"
#include "FilesHashAlgorithmSelectionController.h"
#include "FilesHashContextMenuController.h"
#include "FilesHashInputController.h"
#include "FilesHashProgressController.h"
#include "FilesHashResultViewController.h"
#include "FilesHashSearchController.h"
#include "FilesHashSessionController.h"
#include "UIBridgeMFC.h"

namespace
{
static void SetDialogItemText(CDialog* parentWnd, int controlId, LPCTSTR text)
{
	if (parentWnd == NULL)
	{
		return;
	}

	CWnd* pWnd = parentWnd->GetDlgItem(controlId);
	if (pWnd != NULL)
	{
		pWnd->SetWindowText(text);
	}
}
}

FilesHashInitializationController::FilesHashInitializationController()
{
}

void FilesHashInitializationController::InitializeDialog(
	ThreadData* threadData,
	sunjwbase::OsMutex* mainMutex,
	CDialog* parentWnd,
	CProgressCtrl* progressCtrl,
	CHyperEditHash* mainEdit,
	CButton* btnOpen,
	CButton* btnExit,
	CButton* btnClr,
	CButton* btnFind,
	CButton* chkUppercase,
	CButton* btnContext,
	UIBridgeMFC** uiBridgeMFC,
	FilesHashAlgorithmSelectionController* hashAlgorithmSelectionController,
	FilesHashInputController* hashInputController,
	FilesHashSearchController* hashSearchController,
	FilesHashSessionController* hashSessionController,
	FilesHashContextMenuController* hashContextMenuController,
	FilesHashProgressController* hashProgressController,
	FilesHashResultViewController* hashResultViewController,
	BOOL limited,
	LPTSTR filesCmdLine,
	LPCTSTR clearButtonText,
	LPCTSTR upperHashText,
	LPCTSTR timeTitleText,
	LPCTSTR openButtonText,
	LPCTSTR verifyButtonText,
	LPCTSTR exitButtonText,
	LPCTSTR aboutButtonText,
	LPCTSTR addContextText,
	LPCTSTR removeContextText,
	LPCTSTR stopButtonText,
	LPCTSTR initInfoText)
{
	if (threadData == NULL ||
		mainMutex == NULL ||
		parentWnd == NULL ||
		progressCtrl == NULL ||
		mainEdit == NULL ||
		btnOpen == NULL ||
		btnExit == NULL ||
		btnClr == NULL ||
		btnFind == NULL ||
		chkUppercase == NULL ||
		btnContext == NULL ||
		uiBridgeMFC == NULL ||
		hashAlgorithmSelectionController == NULL ||
		hashInputController == NULL ||
		hashSearchController == NULL ||
		hashSessionController == NULL ||
		hashContextMenuController == NULL ||
		hashProgressController == NULL ||
		hashResultViewController == NULL)
	{
		return;
	}

	hashProgressController->PrepareAdvTaskbar();

	btnClr->SetWindowText(clearButtonText);
	SetDialogItemText(parentWnd, IDC_STATIC_SPEED, _T(""));
	SetDialogItemText(parentWnd, IDC_STATIC_TIME, _T(""));
	SetDialogItemText(parentWnd, IDC_STATIC_UPPER, upperHashText);
	SetDialogItemText(parentWnd, IDC_STATIC_TIMETITLE, timeTitleText);

	btnOpen->SetWindowText(openButtonText);
	btnFind->SetWindowText(verifyButtonText);
	btnFind->ShowWindow(SW_HIDE);
	btnExit->SetWindowText(exitButtonText);
	SetDialogItemText(parentWnd, IDC_ABOUT, aboutButtonText);

	*uiBridgeMFC = new UIBridgeMFC(parentWnd->GetSafeHwnd(), mainMutex, mainEdit);
	hashAlgorithmSelectionController->Initialize(threadData, parentWnd);
	hashInputController->Initialize(threadData, parentWnd);
	hashSearchController->Initialize(threadData, mainEdit, btnClr, btnFind, btnOpen, chkUppercase);
	hashSessionController->Initialize(threadData, parentWnd, mainEdit, btnOpen, btnClr, btnFind, btnContext, chkUppercase, hashAlgorithmSelectionController);
	hashContextMenuController->Initialize(btnContext, parentWnd->GetDlgItem(IDC_STATIC_ADDRESULT));
	hashProgressController->Initialize(parentWnd, progressCtrl);
	hashResultViewController->Initialize(mainMutex, mainEdit);

	mainMutex->lock();
	{
		SetThreadDataObserver(*threadData, *uiBridgeMFC);
		ResetThreadDataForNewSession(*threadData);
		mainEdit->SetLimitText(UINT_MAX);
	}
	mainMutex->unlock();

	hashResultViewController->ShowInitialInfo(initInfoText);
	hashContextMenuController->RefreshButtonText(addContextText, removeContextText);
	hashContextMenuController->ResetStatus();
	hashSessionController->SetControls(FALSE, limited, openButtonText, stopButtonText);

	hashInputController->LoadCommandLineFiles(filesCmdLine);

	SetThreadDataWorking(*threadData, false);
	progressCtrl->SetRange(0, 99);
	chkUppercase->SetCheck(0);
	hashAlgorithmSelectionController->ResetChecks();
	hashAlgorithmSelectionController->SyncSelections();

	if (HasThreadDataInputFiles(*threadData))
	{
		parentWnd->SetTimer(4, 50, NULL);
	}
}
