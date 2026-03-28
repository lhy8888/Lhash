#pragma once

#include "afxwin.h"

#include "Common/Global.h"

class UIBridgeMFC;
class FilesHashSearchController;
class FilesHashSessionController;
class FilesHashProgressController;
class FilesHashResultViewController;

class FilesHashLifecycleController
{
public:
	FilesHashLifecycleController();

	void Initialize(
		ThreadData* threadData,
		CDialog* parentWnd,
		CButton* btnClr,
		UIBridgeMFC** uiBridgeMFC,
		FilesHashSearchController* hashSearchController,
		FilesHashSessionController* hashSessionController,
		FilesHashProgressController* hashProgressController,
		FilesHashResultViewController* hashResultViewController);

	void StartHashing(LPCTSTR clearButtonText, LPCTSTR secondText, LPCTSTR noSelectionMessage);
	void HandleTimer(UINT_PTR nIDEvent, LPCTSTR secondText, LPCTSTR clearButtonText, LPCTSTR noSelectionMessage);
	LRESULT HandleThreadMessage(WPARAM wParam, LPARAM lParam, BOOL limited, LPCTSTR openButtonText, LPCTSTR stopButtonText);
	BOOL HandleClose();

private:
	void ReleaseBridge();

	ThreadData* m_threadData;
	CDialog* m_parentWnd;
	CButton* m_btnClr;
	UIBridgeMFC** m_uiBridgeMFC;
	FilesHashSearchController* m_hashSearchController;
	FilesHashSessionController* m_hashSessionController;
	FilesHashProgressController* m_hashProgressController;
	FilesHashResultViewController* m_hashResultViewController;
	BOOL m_waitingExit;
};
