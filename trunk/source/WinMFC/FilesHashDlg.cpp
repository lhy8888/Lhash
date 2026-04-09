// MD5SUM03Dlg.cpp : dialog implementation
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

namespace
{
const UINT SETTINGS_COMMAND_CLEAR = 61000;
const UINT SETTINGS_COMMAND_ABOUT = 61001;
const UINT SETTINGS_COMMAND_UPPERCASE = 61002;
const UINT SETTINGS_COMMAND_CONTEXT = 61003;
const UINT SETTINGS_COMMAND_ALGORITHM_BASE = 61100;
}

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

// CFilesHashDlg message handlers
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
	DDX_Control(pDX, IDC_OPEN_FOLDER, m_btnOpenFolder);
	DDX_Control(pDX, IDC_COPY, m_btnCopy);
	DDX_Control(pDX, IDC_EXPORT, m_btnExport);
	DDX_Control(pDX, IDC_SETTINGS, m_btnSettings);
	DDX_Control(pDX, IDC_TASK_LIST, m_taskList);
	DDX_Control(pDX, IDC_STATIC_STATUS_OVERVIEW, m_statusOverview);
}

BEGIN_MESSAGE_MAP(CFilesHashDlg, CDialog)
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	//}}AFX_MSG_MAP
	ON_BN_CLICKED(IDC_OPEN, OnBnClickedOpen)
	ON_BN_CLICKED(IDC_EXIT, OnBnClickedExit)
	ON_BN_CLICKED(IDC_ABOUT, OnBnClickedAbout)
	ON_BN_CLICKED(IDC_CLEAN, OnBnClickedClean)
	ON_BN_CLICKED(IDC_OPEN_FOLDER, &CFilesHashDlg::OnBnClickedOpenFolder)
	ON_BN_CLICKED(IDC_FIND, &CFilesHashDlg::OnBnClickedFind)
	ON_BN_CLICKED(IDC_COPY, &CFilesHashDlg::OnBnClickedCopy)
	ON_BN_CLICKED(IDC_EXPORT, &CFilesHashDlg::OnBnClickedExport)
	ON_BN_CLICKED(IDC_CONTEXT, &CFilesHashDlg::OnBnClickedContext)
	ON_BN_CLICKED(IDC_SETTINGS, &CFilesHashDlg::OnBnClickedSettings)
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


// CFilesHashDlg message handlers

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

	m_hashSessionController.AttachSupplementalControls(&m_btnOpenFolder, &m_btnSettings);
	m_hashProgressController.AttachTaskControls(&m_taskList, &m_statusOverview);
	m_hashProgressController.InitializeTaskList(
		GetStringByKey(MAINDLG_TASK_FILE),
		GetStringByKey(MAINDLG_TASK_ALGORITHM),
		GetStringByKey(MAINDLG_TASK_STATUS),
		GetStringByKey(MAINDLG_TASK_PROGRESS));
	m_hashProgressController.ResetTaskSession();

	m_btnOpenFolder.SetWindowText(GetStringByKey(MAINDLG_OPEN_FOLDER));
	m_btnCopy.SetWindowText(GetStringByKey(MAINDLG_COPY));
	m_btnExport.SetWindowText(GetStringByKey(MAINDLG_EXPORT));
	m_btnSettings.SetWindowText(GetStringByKey(MAINDLG_SETTINGS));
	m_btnFind.ShowWindow(SW_SHOW);
	m_btnFind.EnableWindow(TRUE);
	m_btnClr.ShowWindow(SW_HIDE);
	m_btnExit.ShowWindow(SW_HIDE);
	m_btnContext.ShowWindow(SW_HIDE);
	m_chkUppercase.ShowWindow(SW_HIDE);

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

// Return the drag cursor while the app is minimized.
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

void CFilesHashDlg::OnBnClickedOpenFolder()
{
	m_hashCommandController.HandleOpenFolderButtonClick(
		GetStringByKey(MAINDLG_SELECT_FOLDER),
		GetStringByKey(MAINDLG_EMPTY_FOLDER),
		GetStringByKey(MAINDLG_CLEAR),
		GetStringByKey(SECOND_STRING),
		GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM));
}

void CFilesHashDlg::OnBnClickedFind()
{
	m_hashCommandController.HandleFindButtonClick(GetStringByKey(MAINDLG_CLEAR_VERIFY));
}

void CFilesHashDlg::OnBnClickedCopy()
{
	m_hashCommandController.HandleCopyButtonClick();
}

void CFilesHashDlg::OnBnClickedExport()
{
	m_hashCommandController.HandleExportButtonClick(GetStringByKey(MAINDLG_EXPORT_FILTER), GetStringByKey(MAINDLG_EXPORT_DEFAULT_NAME));
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

void CFilesHashDlg::OnBnClickedSettings()
{
	ShowSettingsMenu();
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

	// TODO: change any DC attributes here if needed.

	// TODO: return a different brush here if the default one is not suitable.
	return hbr;
}


LRESULT CFilesHashDlg::OnThreadMsg(WPARAM wParam, LPARAM lParam)
{
	if (wParam == WP_TASK_UPDATE)
	{
		FilesHashTaskUpdate* taskUpdate = reinterpret_cast<FilesHashTaskUpdate*>(lParam);
		if (taskUpdate != NULL)
		{
			m_hashProgressController.ApplyTaskUpdate(*taskUpdate);
			delete taskUpdate;
		}
		return 0;
	}

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

void CFilesHashDlg::ShowSettingsMenu()
{
	CMenu menuSettings;
	menuSettings.CreatePopupMenu();
	menuSettings.AppendMenu(MF_STRING, SETTINGS_COMMAND_CLEAR, GetStringByKey(MAINDLG_SETTINGS_CLEAR));
	menuSettings.AppendMenu(MF_SEPARATOR);
	menuSettings.AppendMenu((m_chkUppercase.GetCheck() ? MF_CHECKED : MF_UNCHECKED) | MF_STRING, SETTINGS_COMMAND_UPPERCASE, GetStringByKey(MAINDLG_UPPER_HASH));

	CMenu menuAlgorithms;
	menuAlgorithms.CreatePopupMenu();
	VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)
	{
		UINT commandId = SETTINGS_COMMAND_ALGORITHM_BASE + static_cast<UINT>(index);
		UINT commandFlags = MF_STRING | (m_hashAlgorithmSelectionController.IsAlgorithmEnabled(GetHashAlgorithmDescriptorId(algorithmDescriptor)) ? MF_CHECKED : MF_UNCHECKED);
		menuAlgorithms.AppendMenu(commandFlags, commandId, GetHashAlgorithmDescriptorDisplayLabel(algorithmDescriptor).c_str());
		return true;
	});
	menuSettings.AppendMenu(MF_POPUP, reinterpret_cast<UINT_PTR>(menuAlgorithms.GetSafeHmenu()), GetStringByKey(MAINDLG_SETTINGS_ALGORITHMS));
	menuAlgorithms.Detach();

	CString contextText;
	m_btnContext.GetWindowText(contextText);
	if (!contextText.IsEmpty())
	{
		menuSettings.AppendMenu(MF_SEPARATOR);
		menuSettings.AppendMenu(MF_STRING, SETTINGS_COMMAND_CONTEXT, contextText);
	}

	menuSettings.AppendMenu(MF_SEPARATOR);
	menuSettings.AppendMenu(MF_STRING, SETTINGS_COMMAND_ABOUT, GetStringByKey(MAINDLG_ABOUT));

	CRect buttonRect;
	m_btnSettings.GetWindowRect(&buttonRect);
	UINT commandId = menuSettings.TrackPopupMenu(TPM_RETURNCMD | TPM_LEFTALIGN | TPM_TOPALIGN, buttonRect.left, buttonRect.bottom + 2, this);
	HandleSettingsCommand(commandId);
}

void CFilesHashDlg::HandleSettingsCommand(UINT commandId)
{
	if (commandId == 0)
	{
		return;
	}

	if (commandId == SETTINGS_COMMAND_CLEAR)
	{
		OnBnClickedClean();
		return;
	}
	if (commandId == SETTINGS_COMMAND_UPPERCASE)
	{
		OnBnClickedUpperHash();
		return;
	}
	if (commandId == SETTINGS_COMMAND_CONTEXT)
	{
		OnBnClickedContext();
		return;
	}
	if (commandId == SETTINGS_COMMAND_ABOUT)
	{
		OnBnClickedAbout();
		return;
	}
	if (commandId >= SETTINGS_COMMAND_ALGORITHM_BASE)
	{
		int algorithmIndex = static_cast<int>(commandId - SETTINGS_COMMAND_ALGORITHM_BASE);
		if (algorithmIndex >= 0 && algorithmIndex < GetRegisteredHashAlgorithmCount())
		{
			const HashAlgorithmDescriptor& algorithmDescriptor = GetHashAlgorithmDescriptorAt(algorithmIndex);
			HashAlgorithmId algorithmId = GetHashAlgorithmDescriptorId(algorithmDescriptor);
			BOOL enabled = m_hashAlgorithmSelectionController.IsAlgorithmEnabled(algorithmId);
			m_hashAlgorithmSelectionController.SetAlgorithmEnabled(algorithmId, !enabled);
		}
	}
}


