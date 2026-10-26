#pragma once

#include "afxwin.h"

#include "Common/HashTypes.h"
#include <cstdint>
struct ThreadData;

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
	void Shutdown();

private:
	void ReleaseBridge();
	void PollWorkerExit();
	void FinalizeWorkerExit();
	void StopWorkerPoll();

	ThreadData* m_threadData;
	CDialog* m_parentWnd;
	CButton* m_btnClr;
	UIBridgeMFC** m_uiBridgeMFC;
	FilesHashSearchController* m_hashSearchController;
	FilesHashSessionController* m_hashSessionController;
	FilesHashProgressController* m_hashProgressController;
	FilesHashResultViewController* m_hashResultViewController;
	BOOL m_waitingExit;
	UINT_PTR m_workerPollTimer;
	WPARAM m_terminalMessage;
	bool m_sessionSetupFailed;
	uint64_t m_sessionId = 0;
};
