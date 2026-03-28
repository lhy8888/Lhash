// MD5SUM03Dlg.cpp : 实现文件
//
#include "stdafx.h"

#include <string>
#include <vector>

#include <shellapi.h>

#include "Common/strhelper.h"

#include "FilesHash.h"
#include "FilesHashDlg.h"
#include "FindDlg.h"
#include "AboutDlg.h"
#include "Common/Global.h"
#include "Common/ThreadDataAccess.h"
#include "Common/Utils.h"
#include "WindowsUtils.h"
#include "UIBridgeMFC.h"
#include "WinCommon/WindowsStrings.h"

using namespace std;
using namespace sunjwbase;
using namespace WindowsStrings;

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

// CFilesHashDlg 对话框
CFilesHashDlg::CFilesHashDlg(CWnd* pParent /*=NULL*/)
	: CDialog(CFilesHashDlg::IDD, pParent),
	m_uiBridgeMFC(NULL)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDI_ICON1);
}

void CFilesHashDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_PROG_WHOLE, m_progWhole);
	DDX_Control(pDX, IDE_TXTMAIN, m_editMain);
	DDX_Control(pDX, IDC_OPEN, m_btnOpen);
	DDX_Control(pDX, IDC_EXIT, m_btnExit);
	DDX_Control(pDX, IDC_CLEAN, m_btnClr);
	DDX_Control(pDX, IDC_CHECKUP, m_chkUppercase);
	DDX_Control(pDX, IDC_FIND, m_btnFind);
	DDX_Control(pDX, IDC_CONTEXT, m_btnContext);
}

BEGIN_MESSAGE_MAP(CFilesHashDlg, CDialog)
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	//}}AFX_MSG_MAP
	ON_BN_CLICKED(IDC_OPEN, OnBnClickedOpen)
	ON_BN_CLICKED(IDC_EXIT, OnBnClickedExit)
	ON_BN_CLICKED(IDC_ABOUT, OnBnClickedAbout)
	ON_BN_CLICKED(IDC_CLEAN, OnBnClickedClean)
	ON_BN_CLICKED(IDC_FIND, &CFilesHashDlg::OnBnClickedFind)
	ON_BN_CLICKED(IDC_CONTEXT, &CFilesHashDlg::OnBnClickedContext)
	ON_BN_CLICKED(IDC_CHECKUP, &CFilesHashDlg::OnBnClickedCheckup)
	ON_BN_CLICKED(IDC_STATIC_UPPER, &CFilesHashDlg::OnBnClickedUpperHash)
	ON_WM_DROPFILES()
	ON_WM_TIMER()
	ON_WM_CTLCOLOR()
	ON_WM_CLOSE()
	ON_WM_INITMENUPOPUP()
	ON_MESSAGE(WM_THREAD_INFO, OnThreadMsg)
	ON_MESSAGE(WM_CUSTOM_MSG, OnCustomMsg)
	ON_COMMAND(ID_HYPEREDITMENU_COPYHASH, &CFilesHashDlg::OnHypereditmenuCopyhash)
	ON_UPDATE_COMMAND_UI(ID_HYPEREDITMENU_COPYHASH, &CFilesHashDlg::OnUpdateHypereditmenuCopyhash)
	ON_WM_COPYDATA()
END_MESSAGE_MAP()


// CFilesHashDlg 消息处理程序

BOOL CFilesHashDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

	// 设置此对话框的图标。当应用程序主窗口不是对话框时，框架将自动
	//  执行此操作
	SetIcon(m_hIcon, TRUE);			// 设置大图标
	SetIcon(m_hIcon, FALSE);		// 设置小图标

	// TODO：在此添加额外的初始化代码

	m_hashProgressController.PrepareAdvTaskbar();

	m_btnClr.SetWindowText(GetStringByKey(MAINDLG_CLEAR));

	m_waitingExit = FALSE;

	m_bLimited = WindowsUtils::IsLimitedProc();

	CWnd* pWnd;
	pWnd = GetDlgItem(IDC_STATIC_SPEED);
	pWnd->SetWindowText(_T(""));
	pWnd = GetDlgItem(IDC_STATIC_TIME);
	pWnd->SetWindowText(_T(""));
	pWnd = GetDlgItem(IDC_STATIC_UPPER);
	pWnd->SetWindowText(GetStringByKey(MAINDLG_UPPER_HASH));
	pWnd = GetDlgItem(IDC_STATIC_TIMETITLE);
	pWnd->SetWindowText(GetStringByKey(MAINDLG_TIME_TITLE));

	m_btnOpen.SetWindowText(GetStringByKey(MAINDLG_OPEN));
	m_btnFind.SetWindowText(GetStringByKey(MAINDLG_VERIFY));
	m_btnFind.ShowWindow(SW_HIDE);
	m_btnExit.SetWindowText(GetStringByKey(MAINDLG_EXIT));
	pWnd = GetDlgItem(IDC_ABOUT);
	pWnd->SetWindowText(GetStringByKey(MAINDLG_ABOUT));

	m_uiBridgeMFC = new UIBridgeMFC(GetSafeHwnd(), &m_mainMtx, &m_editMain);
	m_hashAlgorithmSelectionController.Initialize(&m_thrdData, this);
	m_hashInputController.Initialize(&m_thrdData, this);
	m_hashSearchController.Initialize(&m_thrdData, &m_editMain, &m_btnClr, &m_btnFind, &m_btnOpen, &m_chkUppercase);
	m_hashSessionController.Initialize(&m_thrdData, this, &m_editMain, &m_btnOpen, &m_btnClr, &m_btnFind, &m_btnContext, &m_chkUppercase, &m_hashAlgorithmSelectionController);
	m_hashContextMenuController.Initialize(&m_btnContext, GetDlgItem(IDC_STATIC_ADDRESULT));
	m_hashProgressController.Initialize(this, &m_progWhole);

	m_mainMtx.lock();
	{
		SetThreadDataObserver(m_thrdData, m_uiBridgeMFC);
		ResetThreadDataForNewSession(m_thrdData);


		m_editMain.SetLimitText(UINT_MAX);
		m_editMain.ClearTextBuffer();
		m_editMain.AppendTextToBuffer(GetStringByKey(MAINDLG_INITINFO));
		m_editMain.ShowTextBuffer();
	}
	m_mainMtx.unlock();

	m_hashContextMenuController.RefreshButtonText(GetStringByKey(MAINDLG_ADD_CONTEXT_MENU), GetStringByKey(MAINDLG_REMOVE_CONTEXT_MENU));
	m_hashContextMenuController.ResetStatus();

	m_hashSessionController.SetControls(FALSE, m_bLimited, GetStringByKey(MAINDLG_OPEN), GetStringByKey(MAINDLG_STOP));

	// 从命令行获取文件路径
	m_hashInputController.LoadCommandLineFiles(theApp.m_lpCmdLine);
	// 从命令行获取文件路径结束

	SetThreadDataWorking(m_thrdData, false);
	m_progWhole.SetRange(0, 99);
	m_chkUppercase.SetCheck(0);
	m_hashAlgorithmSelectionController.ResetChecks();
	m_hashAlgorithmSelectionController.SyncSelections();

	if(HasThreadDataInputFiles(m_thrdData))
		SetTimer(4, 50, NULL); // 使 DoMD5() 在 OnInitDialog() 之后执行

	return TRUE;  // 除非设置了控件的焦点，否则返回 TRUE
}

// 如果向对话框添加最小化按钮，则需要下面的代码
//  来绘制该图标。对于使用文档/视图模型的 MFC 应用程序，
//  这将由框架自动完成。

void CFilesHashDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // 用于绘制的设备上下文

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// 使图标在工作矩形中居中
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// 绘制图标
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialog::OnPaint();
	}
}

//当用户拖动最小化窗口时系统调用此函数取得光标显示。
HCURSOR CFilesHashDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}

void CFilesHashDlg::OnDropFiles(HDROP hDropInfo)
{
	if(!IsThreadDataWorking(m_thrdData))
	{
		DragAcceptFiles(FALSE);
		BOOL hasPendingFiles = m_hashInputController.LoadDroppedFiles(hDropInfo);
		DragAcceptFiles(TRUE);
		if (hasPendingFiles)
		{
			DoMD5();
		}
	}
}
BOOL CFilesHashDlg::OnCopyData(CWnd* pWnd, COPYDATASTRUCT* pCopyDataStruct)
{
	if (pCopyDataStruct->dwData == 0)
		SetForegroundWindow();
	if (pCopyDataStruct->dwData == 0 &&
		!IsThreadDataWorking(m_thrdData) &&
		m_hashInputController.LoadCopyDataFiles(pCopyDataStruct))
	{
		DoMD5();
		return TRUE;
	}
	return CDialog::OnCopyData(pWnd, pCopyDataStruct);
}
void CFilesHashDlg::OnClose()
{
	// TODO: 在此添加消息处理程序代码和/或调用默认值
	if(IsThreadDataWorking(m_thrdData))
	{
		m_waitingExit = TRUE;
		m_hashSessionController.StopWorkingThread();

		return;
	}

	if(m_uiBridgeMFC != NULL)
	{
		delete m_uiBridgeMFC;
		m_uiBridgeMFC = NULL;
	}

	CDialog::OnClose();
}

void CFilesHashDlg::OnBnClickedOpen()
{
	if(!IsThreadDataWorking(m_thrdData))
	{
		CString filter;
		filter = GetStringByKey(FILE_STRING);
		filter.Append(_T("(*.*)|*.*|"));
		if (m_hashInputController.LoadOpenFileDialogSelection(filter))
		{
			DoMD5();
		}
	}
	else
	{
		//??????
		m_hashSessionController.StopWorkingThread();
	}
}
void CFilesHashDlg::OnBnClickedExit()
{
	PostMessage(WM_CLOSE);//OnCancel();
}

void CFilesHashDlg::OnBnClickedAbout()
{
	CAboutDlg About;
	About.DoModal();
}

void CFilesHashDlg::OnBnClickedClean()
{
	if (!IsThreadDataWorking(m_thrdData))
	{
		CString strBtnText;
		m_btnClr.GetWindowText(strBtnText);
		if (strBtnText.Compare(GetStringByKey(MAINDLG_CLEAR)) == 0)
		{
			m_mainMtx.lock();
			{
				m_editMain.ClearTextBuffer();
				ClearThreadDataResults(m_thrdData);
				m_editMain.ShowTextBuffer();
			}
			m_mainMtx.unlock();

			CStatic* pWnd =(CStatic *)GetDlgItem(IDC_STATIC_TIME);
			pWnd->SetWindowText(_T(""));
			pWnd = (CStatic*)GetDlgItem(IDC_STATIC_SPEED);
			pWnd->SetWindowText(_T(""));

			m_hashProgressController.SetWholeProgress(0);
		}
		else if (strBtnText.Compare(GetStringByKey(MAINDLG_CLEAR_VERIFY)) == 0)
		{
			m_hashSearchController.ClearSearch(GetStringByKey(MAINDLG_CLEAR));
			RefreshMainText();
		}
	}
}

void CFilesHashDlg::OnBnClickedFind()
{
	// TODO: 在此添加控件通知处理程序代码
	CFindDlg Find;
	Find.SetFindHash(_T(""));
	if (IDOK == Find.DoModal())
	{
		if (m_hashSearchController.BeginSearch(CString(), Find.GetFindHash(), GetStringByKey(MAINDLG_CLEAR_VERIFY)))
		{
			m_editMain.ShowTextBuffer();
		}
	}
}

void CFilesHashDlg::OnBnClickedContext()
{
	if (m_hashContextMenuController.HandleButtonClick(
		m_bLimited,
		GetStringByKey(MAINDLG_ADD_CONTEXT_MENU),
		GetStringByKey(MAINDLG_REMOVE_CONTEXT_MENU),
		GetStringByKey(MAINDLG_ADD_SUCCEEDED),
		GetStringByKey(MAINDLG_ADD_FAILED),
		GetStringByKey(MAINDLG_REMOVE_SUCCEEDED),
		GetStringByKey(MAINDLG_REMOVE_FAILED)))
	{
		ExitProcess(0);
	}
}

void CFilesHashDlg::OnBnClickedCheckup()
{
	// Remember current scroll position
	int iFirstVisible = m_editMain.GetFirstVisibleLine();

	m_hashSearchController.RebuildCurrentView();
	RefreshMainText(FALSE);

	// Reset scroll position
	m_editMain.LineScroll(iFirstVisible);
}

void CFilesHashDlg::OnBnClickedUpperHash()
{
	if (m_chkUppercase.IsWindowEnabled())
	{
		m_chkUppercase.SetCheck(!m_chkUppercase.GetCheck());
		OnBnClickedCheckup();
	}
}

void CFilesHashDlg::OnTimer(UINT_PTR nIDEvent)
{
	if(nIDEvent == 1)
	{
		// 计算花费时间
		m_hashProgressController.AdvanceTimeTick(GetStringByKey(SECOND_STRING));
	}
	else if(nIDEvent == 4)
	{
		// 通过命令行启动的
		DoMD5();
		KillTimer(4);
	}

	CDialog::OnTimer(nIDEvent);
}

void CFilesHashDlg::DoMD5()
{
	if (m_hashSearchController.IsActive())
	{
		m_hashSearchController.ClearSearch(GetStringByKey(MAINDLG_CLEAR));
		RefreshMainText();
	}

	m_btnClr.SetWindowText(GetStringByKey(MAINDLG_CLEAR));

	m_hashProgressController.PrepareAdvTaskbar();
	m_hashProgressController.SetWholeProgress(0);

	if (!m_hashSessionController.PrepareHashStart(GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM)))
	{
		return;
	}

	m_hashProgressController.StartTiming(GetStringByKey(SECOND_STRING));

	m_hashSessionController.StartHashThread();
}
HBRUSH CFilesHashDlg::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor)
{
	HBRUSH hbr = CDialog::OnCtlColor(pDC, pWnd, nCtlColor);

	// TODO:  在此更改 DC 的任何属性

	// TODO:  如果默认的不是所需画笔，则返回另一个画笔
	return hbr;
}


void CFilesHashDlg::RefreshMainText(BOOL bScrollToEnd /*= TRUE*/)
{
	m_mainMtx.lock();

	if (bScrollToEnd)
	{
		// 将文本框滚动到结尾
		m_editMain.ShowTextBufferScrollEnd();
	}
	else
	{
		m_editMain.ShowTextBuffer();
	}

	m_mainMtx.unlock();
}

LRESULT CFilesHashDlg::OnThreadMsg(WPARAM wParam, LPARAM lParam)
{
	switch(wParam)
	{
	case WP_WORKING:
		m_hashSessionController.SetControls(TRUE, m_bLimited, GetStringByKey(MAINDLG_OPEN), GetStringByKey(MAINDLG_STOP));
		break;
	case WP_REFRESH_TEXT:
		RefreshMainText();
		break;
	case WP_PROG_WHOLE:
		m_hashProgressController.SetWholeProgress((int)lParam);
		break;
	case WP_FINISHED:
		// 停止主界面计时器 计算读取速度
		m_hashProgressController.FinishTiming(GetThreadDataTotalSize(m_thrdData));
		// 停止主界面计时器 计算读取速度

		// 界面设置 - 开始
		m_hashSessionController.SetControls(FALSE, m_bLimited, GetStringByKey(MAINDLG_OPEN), GetStringByKey(MAINDLG_STOP));
		// 界面设置 - 结束

		m_hashProgressController.SetWholeProgress(99);
		break;
	case WP_STOPPED:
		m_hashProgressController.ResetAfterStop();

		//界面设置 - 开始
		m_hashSessionController.SetControls(FALSE, m_bLimited, GetStringByKey(MAINDLG_OPEN), GetStringByKey(MAINDLG_STOP));
		//界面设置 - 结束

		m_mainMtx.lock();
		{
			m_editMain.AppendTextToBuffer(_T("\r\n"));
			//m_editMain.AppendTextToBuffer(MAINDLG_CALCU_TERMINAL);
			//m_editMain.AppendTextToBuffer(_T("\r\n\r\n"));

			// 将文本框滚动到结尾
			m_editMain.ShowTextBufferScrollEnd();
		}
		m_mainMtx.unlock();

		m_hashProgressController.SetWholeProgress(0);

		if(m_waitingExit)
		{
			PostMessage(WM_CLOSE);
		}
		break;
	}

	return 0;
}

LRESULT CFilesHashDlg::OnCustomMsg(WPARAM wParam, LPARAM lParam)
{
	switch(wParam)
	{
	case WM_HYPEREDIT_MENU:
		{
			CPoint cpPoint = m_editMain.GetLastScreenPoint();

			CMenu menuHyperEdit;
			menuHyperEdit.LoadMenu(IDR_MENU_HYPEREDIT);
			CMenu *pmSubMenu = menuHyperEdit.GetSubMenu(0);
			ASSERT(pmSubMenu);
			pmSubMenu->TrackPopupMenu(TPM_LEFTALIGN | TPM_RIGHTBUTTON,
				cpPoint.x, cpPoint.y, this);
		}
		break;
	}

	return 0;
}

void CFilesHashDlg::OnInitMenuPopup(CMenu *pPopupMenu, UINT nIndex, BOOL bSysMenu)
{
    ASSERT(pPopupMenu != NULL);
    // Check the enabled state of various menu items.

    CCmdUI state;
    state.m_pMenu = pPopupMenu;
    ASSERT(state.m_pOther == NULL);
    ASSERT(state.m_pParentMenu == NULL);

    // Determine if menu is popup in top-level menu and set m_pOther to
    // it if so (m_pParentMenu == NULL indicates that it is secondary popup).
    HMENU hParentMenu;
    if (AfxGetThreadState()->m_hTrackingMenu == pPopupMenu->m_hMenu)
        state.m_pParentMenu = pPopupMenu;    // Parent == child for tracking popup.
    else if ((hParentMenu = ::GetMenu(m_hWnd)) != NULL)
    {
        CWnd* pParent = this;
           // Child windows don't have menus--need to go to the top!
        if (pParent != NULL &&
           (hParentMenu = ::GetMenu(pParent->m_hWnd)) != NULL)
        {
           int nIndexMax = ::GetMenuItemCount(hParentMenu);
           for (int nIndex = 0; nIndex < nIndexMax; nIndex++)
           {
            if (::GetSubMenu(hParentMenu, nIndex) == pPopupMenu->m_hMenu)
            {
                // When popup is found, m_pParentMenu is containing menu.
                state.m_pParentMenu = CMenu::FromHandle(hParentMenu);
                break;
            }
           }
        }
    }

    state.m_nIndexMax = pPopupMenu->GetMenuItemCount();
    for (state.m_nIndex = 0; state.m_nIndex < state.m_nIndexMax;
      state.m_nIndex++)
    {
        state.m_nID = pPopupMenu->GetMenuItemID(state.m_nIndex);
        if (state.m_nID == 0)
           continue; // Menu separator or invalid cmd - ignore it.

        ASSERT(state.m_pOther == NULL);
        ASSERT(state.m_pMenu != NULL);
        if (state.m_nID == (UINT)-1)
        {
           // Possibly a popup menu, route to first item of that popup.
           state.m_pSubMenu = pPopupMenu->GetSubMenu(state.m_nIndex);
           if (state.m_pSubMenu == NULL ||
            (state.m_nID = state.m_pSubMenu->GetMenuItemID(0)) == 0 ||
            state.m_nID == (UINT)-1)
           {
            continue;       // First item of popup can't be routed to.
           }
           state.DoUpdate(this, TRUE);   // Popups are never auto disabled.
        }
        else
        {
           // Normal menu item.
           // Auto enable/disable if frame window has m_bAutoMenuEnable
           // set and command is _not_ a system command.
           state.m_pSubMenu = NULL;
           state.DoUpdate(this, FALSE);
        }

        // Adjust for menu deletions and additions.
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

void CFilesHashDlg::OnHypereditmenuCopyhash()
{
	CString cstrHyperlink = m_editMain.GetLastHyperlink();
	WindowsUtils::CopyCString(cstrHyperlink);
}

void CFilesHashDlg::OnUpdateHypereditmenuCopyhash(CCmdUI *pCmdUI)
{
	pCmdUI->SetText(GetStringByKey(MAINDLG_HYPEREDIT_MENU_COPY));
}
