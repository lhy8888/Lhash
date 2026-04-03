// MD5SUM03Dlg.cpp : 实现文件
//
#include "stdafx.h"

#include <string>
#include <vector>

#include <shellapi.h>

#include "Common/strhelper.h"

#include "FilesHash.h"
#include "FilesHashDlg.h"
#include "Common/Global.h"
#include "LegacyCompat/ThreadDataAccess.h"
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

	// Initialize dialog icons and startup state.
	SetIcon(m_hIcon, TRUE);
	SetIcon(m_hIcon, FALSE);

	m_bLimited = WindowsUtils::IsLimitedProc();

	m_hashInitializationController.InitializeDialog(
		&m_thrdData,
		&m_mainMtx,
		this,
		&m_progWhole,
		&m_editMain,
		&m_btnOpen,
		&m_btnExit,
		&m_btnClr,
		&m_btnFind,
		&m_chkUppercase,
		&m_btnContext,
		&m_uiBridgeMFC,
		&m_hashAlgorithmSelectionController,
		&m_hashCommandController,
		&m_hashMessageController,
		&m_hashInputController,
		&m_hashSearchController,
		&m_hashSessionController,
		&m_hashLifecycleController,
		&m_hashContextMenuController,
		&m_hashProgressController,
		&m_hashResultViewController,
		m_bLimited,
		theApp.m_lpCmdLine,
		GetStringByKey(MAINDLG_CLEAR),
		GetStringByKey(MAINDLG_UPPER_HASH),
		GetStringByKey(MAINDLG_TIME_TITLE),
		GetStringByKey(MAINDLG_OPEN),
		GetStringByKey(MAINDLG_VERIFY),
		GetStringByKey(MAINDLG_EXIT),
		GetStringByKey(MAINDLG_ABOUT),
		GetStringByKey(MAINDLG_ADD_CONTEXT_MENU),
		GetStringByKey(MAINDLG_REMOVE_CONTEXT_MENU),
		GetStringByKey(MAINDLG_STOP),
		GetStringByKey(MAINDLG_INITINFO));

	return TRUE;
}

void CFilesHashDlg::OnPaint()
{
	if (m_hashMessageController.HandlePaint(m_hIcon))
	{
		return;
	}

	CDialog::OnPaint();
}

//?????????????????????????
HCURSOR CFilesHashDlg::OnQueryDragIcon()
{
	return m_hashMessageController.GetDragCursor(m_hIcon);
}

void CFilesHashDlg::OnDropFiles(HDROP hDropInfo)
{
	m_hashMessageController.HandleDropFiles(hDropInfo, GetStringByKey(MAINDLG_CLEAR), GetStringByKey(SECOND_STRING), GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM));
}
BOOL CFilesHashDlg::OnCopyData(CWnd* pWnd, COPYDATASTRUCT* pCopyDataStruct)
{
	if (m_hashMessageController.HandleCopyData(pCopyDataStruct, GetStringByKey(MAINDLG_CLEAR), GetStringByKey(SECOND_STRING), GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM)))
	{
		return TRUE;
	}
	return CDialog::OnCopyData(pWnd, pCopyDataStruct);
}
void CFilesHashDlg::OnClose()
{
	if (m_hashLifecycleController.HandleClose())
	{
		return;
	}

	CDialog::OnClose();
}

void CFilesHashDlg::OnBnClickedOpen()
{
	CString filter;
	filter = GetStringByKey(FILE_STRING);
	filter.Append(_T("(*.*)|*.*|"));
	m_hashCommandController.HandleOpenButtonClick(filter, GetStringByKey(MAINDLG_CLEAR), GetStringByKey(SECOND_STRING), GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM));
}
void CFilesHashDlg::OnBnClickedExit()
{
	m_hashCommandController.HandleExitButtonClick();
}

void CFilesHashDlg::OnBnClickedAbout()
{
	m_hashCommandController.HandleAboutButtonClick();
}

void CFilesHashDlg::OnBnClickedClean()
{
	m_hashCommandController.HandleCleanButtonClick(GetStringByKey(MAINDLG_CLEAR), GetStringByKey(MAINDLG_CLEAR_VERIFY));
}

void CFilesHashDlg::OnBnClickedFind()
{
	m_hashCommandController.HandleFindButtonClick(GetStringByKey(MAINDLG_CLEAR_VERIFY));
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
	m_hashResultViewController.RebuildCurrentViewPreservingScroll(m_hashSearchController);
}

void CFilesHashDlg::OnBnClickedUpperHash()
{
	m_hashResultViewController.ToggleUppercaseAndRebuild(&m_chkUppercase, m_hashSearchController);
}

void CFilesHashDlg::OnTimer(UINT_PTR nIDEvent)
{
	m_hashLifecycleController.HandleTimer(nIDEvent, GetStringByKey(SECOND_STRING), GetStringByKey(MAINDLG_CLEAR), GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM));

	CDialog::OnTimer(nIDEvent);
}
HBRUSH CFilesHashDlg::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor)
{
	HBRUSH hbr = CDialog::OnCtlColor(pDC, pWnd, nCtlColor);

	// TODO:  在此更改 DC 的任何属性

	// TODO:  如果默认的不是所需画笔，则返回另一个画笔
	return hbr;
}


LRESULT CFilesHashDlg::OnThreadMsg(WPARAM wParam, LPARAM lParam)
{
	return m_hashLifecycleController.HandleThreadMessage(wParam, lParam, m_bLimited, GetStringByKey(MAINDLG_OPEN), GetStringByKey(MAINDLG_STOP));
}

LRESULT CFilesHashDlg::OnCustomMsg(WPARAM wParam, LPARAM lParam)
{
	return m_hashMessageController.HandleCustomMessage(wParam);
}

void CFilesHashDlg::OnInitMenuPopup(CMenu *pPopupMenu, UINT nIndex, BOOL bSysMenu)
{
	m_hashMessageController.HandleInitMenuPopup(pPopupMenu);
}

void CFilesHashDlg::OnHypereditmenuCopyhash()
{
	m_hashMessageController.HandleCopyHash();
}

void CFilesHashDlg::OnUpdateHypereditmenuCopyhash(CCmdUI *pCmdUI)
{
	m_hashMessageController.UpdateCopyHashMenuText(pCmdUI, GetStringByKey(MAINDLG_HYPEREDIT_MENU_COPY));
}
