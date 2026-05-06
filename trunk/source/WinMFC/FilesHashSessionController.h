#pragma once

#include "afxwin.h"

#include "Common/HashTypes.h"
#include "WinCommon/WinHandleGuard.h"
struct ThreadData;

class CHyperEditHash;
class FilesHashAlgorithmSelectionController;

class FilesHashSessionController
{
public:
	FilesHashSessionController();
	~FilesHashSessionController();

	void Initialize(ThreadData* threadData,
		CDialog* parentWnd,
		CWnd* mainEditDropTarget,
		CButton* btnOpen,
		CButton* btnClr,
		CButton* btnFind,
		CButton* btnContext,
		CButton* chkUppercase,
		FilesHashAlgorithmSelectionController* hashAlgorithmSelectionController);
	void AttachSupplementalControls(CButton* btnOpenFolder, CButton* btnSettings);

	BOOL PrepareHashStart(LPCTSTR noSelectionMessage);
	void StartHashThread();
	void StopWorkingThread();
	void SetControls(BOOL working, BOOL limited, LPCTSTR openButtonText, LPCTSTR stopButtonText);

private:
	struct WindowMessageFilterStatus
	{
		DWORD cbSize;
		DWORD extStatus;
	};

	typedef BOOL (WINAPI *LPFN_CHANGEWINDOWMESSAGEFILTEREX)(HWND, UINT, DWORD, void*);
	typedef BOOL (WINAPI *LPFN_CHANGEWINDOWMESSAGEFILTER)(UINT, DWORD);

	static const DWORD WINDOW_MESSAGE_FILTER_ACTION_ALLOW = 1;

	static void AllowMessageForWindow(HWND hWnd, UINT message);
	static void PrepareDropTarget(CWnd* pWnd, BOOL bAccept);

	void CloseWorkThreadHandle();

	ThreadData* m_threadData;
	CDialog* m_parentWnd;
	CWnd* m_mainEditDropTarget;
	CButton* m_btnOpen;
	CButton* m_btnClr;
	CButton* m_btnFind;
	CButton* m_btnContext;
	CButton* m_chkUppercase;
	CButton* m_btnOpenFolder;
	CButton* m_btnSettings;
	FilesHashAlgorithmSelectionController* m_hashAlgorithmSelectionController;
	WinHandleGuard::UniqueWinHandle m_hWorkThread;
};
