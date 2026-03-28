#pragma once

#include "afxwin.h"
#include "afxcmn.h"

#include "OsUtils/OsThread.h"
#include "Common/Global.h"

class CHyperEditHash;
class UIBridgeMFC;
class FilesHashAlgorithmSelectionController;
class FilesHashCommandController;
class FilesHashMessageController;
class FilesHashInputController;
class FilesHashSearchController;
class FilesHashSessionController;
class FilesHashLifecycleController;
class FilesHashContextMenuController;
class FilesHashProgressController;
class FilesHashResultViewController;

class FilesHashInitializationController
{
public:
	FilesHashInitializationController();

	void InitializeDialog(
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
		FilesHashCommandController* hashCommandController,
		FilesHashMessageController* hashMessageController,
		FilesHashInputController* hashInputController,
		FilesHashSearchController* hashSearchController,
		FilesHashSessionController* hashSessionController,
		FilesHashLifecycleController* hashLifecycleController,
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
		LPCTSTR initInfoText);
};
