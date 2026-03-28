#pragma once

#include "afxwin.h"

#include "HyperEditHash.h"

#include "OsUtils/OsThread.h"
#include "Common/Global.h"

class FilesHashSearchController;

class FilesHashResultViewController
{
public:
	FilesHashResultViewController();

	void Initialize(sunjwbase::OsMutex* mainMutex, CHyperEditHash* mainEdit);

	void ShowInitialInfo(LPCTSTR initInfo);
	void ClearResults(ThreadData& threadData);
	void RefreshMainText(BOOL scrollToEnd = TRUE);
	void RebuildCurrentViewPreservingScroll(FilesHashSearchController& searchController);
	void ToggleUppercaseAndRebuild(CButton* chkUppercase, FilesHashSearchController& searchController);
	void AppendLineBreakAndScrollEnd();
	void ShowHyperEditMenu(CWnd* ownerWnd);
	void UpdatePopupMenu(CWnd* ownerWnd, CMenu* pPopupMenu);
	void CopyLastHyperlink() const;
	void UpdateCopyHashMenuText(CCmdUI* pCmdUI, LPCTSTR copyText) const;

private:
	sunjwbase::OsMutex* m_mainMutex;
	CHyperEditHash* m_mainEdit;
};
