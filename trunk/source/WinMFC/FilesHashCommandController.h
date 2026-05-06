#pragma once

#include "afxwin.h"

#include "Common/HashTypes.h"
struct ThreadData;

class FilesHashInputController;
class FilesHashSearchController;
class FilesHashSessionController;
class FilesHashLifecycleController;
class FilesHashMessageController;
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
		FilesHashMessageController* hashMessageController,
		FilesHashProgressController* hashProgressController,
		FilesHashResultViewController* hashResultViewController);

	void HandleOpenButtonClick(
		LPCTSTR fileFilter,
		LPCTSTR clearButtonText,
		LPCTSTR secondText,
		LPCTSTR noSelectionMessage,
		LPCTSTR overLimitMessage,
		LPCTSTR overLimitWithCountMessage,
		LPCTSTR truncatedMessage,
		LPCTSTR maybeTruncatedMessage,
		LPCTSTR errorMessage);
	void HandleOpenFolderButtonClick(
		LPCTSTR folderDialogTitle,
		LPCTSTR emptyFolderMessage,
		LPCTSTR clearButtonText,
		LPCTSTR secondText,
		LPCTSTR noSelectionMessage,
		LPCTSTR overLimitMessage,
		LPCTSTR overLimitWithCountMessage,
		LPCTSTR truncatedMessage,
		LPCTSTR maybeTruncatedMessage,
		LPCTSTR errorMessage);
	void HandleExitButtonClick() const;
	void HandleAboutButtonClick() const;
	void HandleCleanButtonClick(LPCTSTR clearButtonText, LPCTSTR clearVerifyButtonText);
	void HandleCopyButtonClick() const;
	void HandleExportButtonClick(LPCTSTR exportFilter, LPCTSTR defaultFileName) const;
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
	FilesHashMessageController* m_hashMessageController;
	FilesHashProgressController* m_hashProgressController;
	FilesHashResultViewController* m_hashResultViewController;
};
