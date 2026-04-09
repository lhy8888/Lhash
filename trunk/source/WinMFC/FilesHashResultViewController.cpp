#include "stdafx.h"

#include "FilesHashResultViewController.h"

#include "resource.h"

#include "LegacyCompat/ThreadDataResultAccess.h"
#include "FilesHashSearchController.h"
#include "WindowsUtils.h"

using namespace sunjwbase;

FilesHashResultViewController::FilesHashResultViewController()
	: m_mainMutex(NULL),
	m_mainEdit(NULL)
{
}

void FilesHashResultViewController::Initialize(OsMutex* mainMutex, CHyperEditHash* mainEdit)
{
	m_mainMutex = mainMutex;
	m_mainEdit = mainEdit;
}

void FilesHashResultViewController::ShowInitialInfo(LPCTSTR initInfo)
{
	if (m_mainMutex == NULL || m_mainEdit == NULL)
	{
		return;
	}

	m_mainMutex->lock();
	{
		m_mainEdit->ClearTextBuffer();
		m_mainEdit->AppendTextToBuffer(initInfo);
		m_mainEdit->ShowTextBuffer();
	}
	m_mainMutex->unlock();
}

void FilesHashResultViewController::ClearResults(ThreadData& threadData)
{
	if (m_mainMutex == NULL || m_mainEdit == NULL)
	{
		return;
	}

	m_mainMutex->lock();
	{
		m_mainEdit->ClearTextBuffer();
		ClearThreadDataResults(threadData);
		m_mainEdit->ShowTextBuffer();
	}
	m_mainMutex->unlock();
}

void FilesHashResultViewController::RefreshMainText(BOOL scrollToEnd /*= TRUE*/)
{
	if (m_mainMutex == NULL || m_mainEdit == NULL)
	{
		return;
	}

	m_mainMutex->lock();
	{
		if (scrollToEnd)
		{
			m_mainEdit->ShowTextBufferScrollEnd();
		}
		else
		{
			m_mainEdit->ShowTextBuffer();
		}
	}
	m_mainMutex->unlock();
}

void FilesHashResultViewController::RebuildCurrentViewPreservingScroll(FilesHashSearchController& searchController)
{
	if (m_mainEdit == NULL)
	{
		return;
	}

	int firstVisible = m_mainEdit->GetFirstVisibleLine();
	searchController.RebuildCurrentView();
	RefreshMainText(FALSE);
	m_mainEdit->LineScroll(firstVisible);
}

void FilesHashResultViewController::ToggleUppercaseAndRebuild(CButton* chkUppercase, FilesHashSearchController& searchController)
{
	if (chkUppercase == NULL || !chkUppercase->IsWindowEnabled())
	{
		return;
	}

	chkUppercase->SetCheck(!chkUppercase->GetCheck());
	RebuildCurrentViewPreservingScroll(searchController);
}

void FilesHashResultViewController::AppendLineBreakAndScrollEnd()
{
	if (m_mainMutex == NULL || m_mainEdit == NULL)
	{
		return;
	}

	m_mainMutex->lock();
	{
		m_mainEdit->AppendTextToBuffer(_T("\r\n"));
		m_mainEdit->ShowTextBufferScrollEnd();
	}
	m_mainMutex->unlock();
}

void FilesHashResultViewController::ShowHyperEditMenu(CWnd* ownerWnd)
{
	if (m_mainEdit == NULL || ownerWnd == NULL)
	{
		return;
	}

	CPoint cpPoint = m_mainEdit->GetLastScreenPoint();

	CMenu menuHyperEdit;
	menuHyperEdit.LoadMenu(IDR_MENU_HYPEREDIT);
	CMenu* subMenu = menuHyperEdit.GetSubMenu(0);
	ASSERT(subMenu);
	subMenu->TrackPopupMenu(TPM_LEFTALIGN | TPM_RIGHTBUTTON, cpPoint.x, cpPoint.y, ownerWnd);
}

void FilesHashResultViewController::UpdatePopupMenu(CWnd* ownerWnd, CMenu* pPopupMenu)
{
	ASSERT(pPopupMenu != NULL);

	CCmdUI state;
	state.m_pMenu = pPopupMenu;
	ASSERT(state.m_pOther == NULL);
	ASSERT(state.m_pParentMenu == NULL);

	HMENU hParentMenu;
	if (AfxGetThreadState()->m_hTrackingMenu == pPopupMenu->m_hMenu)
	{
		state.m_pParentMenu = pPopupMenu;
	}
	else if (ownerWnd != NULL && (hParentMenu = ::GetMenu(ownerWnd->m_hWnd)) != NULL)
	{
		int nIndexMax = ::GetMenuItemCount(hParentMenu);
		for (int nIndex = 0; nIndex < nIndexMax; nIndex++)
		{
			if (::GetSubMenu(hParentMenu, nIndex) == pPopupMenu->m_hMenu)
			{
				state.m_pParentMenu = CMenu::FromHandle(hParentMenu);
				break;
			}
		}
	}

	state.m_nIndexMax = pPopupMenu->GetMenuItemCount();
	for (state.m_nIndex = 0; state.m_nIndex < state.m_nIndexMax; state.m_nIndex++)
	{
		state.m_nID = pPopupMenu->GetMenuItemID(state.m_nIndex);
		if (state.m_nID == 0)
		{
			continue;
		}

		ASSERT(state.m_pOther == NULL);
		ASSERT(state.m_pMenu != NULL);
		if (state.m_nID == (UINT)-1)
		{
			state.m_pSubMenu = pPopupMenu->GetSubMenu(state.m_nIndex);
			if (state.m_pSubMenu == NULL ||
				(state.m_nID = state.m_pSubMenu->GetMenuItemID(0)) == 0 ||
				state.m_nID == (UINT)-1)
			{
				continue;
			}
			state.DoUpdate(ownerWnd, TRUE);
		}
		else
		{
			state.m_pSubMenu = NULL;
			state.DoUpdate(ownerWnd, FALSE);
		}

		UINT nCount = pPopupMenu->GetMenuItemCount();
		if (nCount < state.m_nIndexMax)
		{
			state.m_nIndex -= (state.m_nIndexMax - nCount);
			while (state.m_nIndex < nCount &&
				pPopupMenu->GetMenuItemID(state.m_nIndex) == state.m_nID)
			{
				state.m_nIndex++;
			}
		}
		state.m_nIndexMax = nCount;
	}
}

void FilesHashResultViewController::CopyLastHyperlink() const
{
	if (m_mainEdit == NULL)
	{
		return;
	}

	WindowsUtils::CopyCString(m_mainEdit->GetLastHyperlink());
}

void FilesHashResultViewController::CopyAllResults() const
{
	if (m_mainEdit == NULL)
	{
		return;
	}

	WindowsUtils::CopyCString(m_mainEdit->GetTextBuffer());
}

CString FilesHashResultViewController::GetCurrentText() const
{
	if (m_mainEdit == NULL)
	{
		return CString();
	}

	return m_mainEdit->GetTextBuffer();
}

void FilesHashResultViewController::UpdateCopyHashMenuText(CCmdUI* pCmdUI, LPCTSTR copyText) const
{
	if (pCmdUI != NULL)
	{
		pCmdUI->SetText(copyText);
	}
}
