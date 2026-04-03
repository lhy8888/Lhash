#pragma once

#include "afxwin.h"

#include "Common/Global.h"
struct ThreadData;

class FilesHashInputController;
class FilesHashSearchController;
class FilesHashSessionController;
class FilesHashLifecycleController;
class FilesHashProgressController;
class FilesHashResultViewController;

class FilesHashCommandController
{
public:
	FilesHashCommandController();

	void Initialize(
		ThreadData* threadData,
		CDialog* parentWnd,
		CButton* btnClr,
		FilesHashInputController* hashInputController,
		FilesHashSearchController* hashSearchController,
		FilesHashSessionController* hashSessionController,
		FilesHashLifecycleController* hashLifecycleController,
		FilesHashProgressController* hashProgressController,
		FilesHashResultViewController* hashResultViewController);

	void HandleOpenButtonClick(LPCTSTR fileFilter, LPCTSTR clearButtonText, LPCTSTR secondText, LPCTSTR noSelectionMessage);
	void HandleExitButtonClick() const;
	void HandleAboutButtonClick() const;
	void HandleCleanButtonClick(LPCTSTR clearButtonText, LPCTSTR clearVerifyButtonText);
	void HandleFindButtonClick(LPCTSTR clearVerifyButtonText) const;

private:
	void ClearProgressLabels() const;

	ThreadData* m_threadData;
	CDialog* m_parentWnd;
	CButton* m_btnClr;
	FilesHashInputController* m_hashInputController;
	FilesHashSearchController* m_hashSearchController;
	FilesHashSessionController* m_hashSessionController;
	FilesHashLifecycleController* m_hashLifecycleController;
	FilesHashProgressController* m_hashProgressController;
	FilesHashResultViewController* m_hashResultViewController;
};
