#pragma once

#include "afxwin.h"

#include "Common/Global.h"
struct ThreadData;

class FilesHashInputController;
class FilesHashLifecycleController;
class FilesHashResultViewController;

class FilesHashMessageController
{
public:
	FilesHashMessageController();

	void Initialize(
		ThreadData* threadData,
		CDialog* parentWnd,
		FilesHashInputController* hashInputController,
		FilesHashLifecycleController* hashLifecycleController,
		FilesHashResultViewController* hashResultViewController);

	BOOL HandlePaint(HICON icon) const;
	HCURSOR GetDragCursor(HICON icon) const;
	void HandleDropFiles(HDROP hDropInfo, LPCTSTR clearButtonText, LPCTSTR secondText, LPCTSTR noSelectionMessage) const;
	BOOL HandleCopyData(const CWnd* pSenderWnd, const COPYDATASTRUCT* pCopyDataStruct, LPCTSTR clearButtonText, LPCTSTR secondText, LPCTSTR noSelectionMessage) const;
	LRESULT HandleCustomMessage(WPARAM wParam) const;
	void HandleInitMenuPopup(CMenu* pPopupMenu) const;
	void HandleCopyHash() const;
	void UpdateCopyHashMenuText(CCmdUI* pCmdUI, LPCTSTR copyText) const;

private:
	static bool IsTrustedCopyDataSender(const CWnd* pSenderWnd);
	ThreadData* m_threadData;
	CDialog* m_parentWnd;
	FilesHashInputController* m_hashInputController;
	FilesHashLifecycleController* m_hashLifecycleController;
	FilesHashResultViewController* m_hashResultViewController;
};
