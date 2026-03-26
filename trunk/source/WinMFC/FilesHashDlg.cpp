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
#include "Common/ResultDataSearch.h"
#include "Common/ThreadDataAccess.h"
#include "Common/Utils.h"
#include "Common/HashEngine.h"
#include "WindowsUtils.h"
#include "UIBridgeMFC.h"
#include "WinCommon/WindowsComm.h"
#include "WinCommon/WindowsStrings.h"

using namespace std;
using namespace sunjwbase;
using namespace WindowsStrings;

namespace
{
	bool CopyDraggedPath(HDROP hDropInfo, UINT index, sunjwbase::tstring& tstrPath)
	{
		UINT cchPath = DragQueryFile(hDropInfo, index, NULL, 0);
		if (cchPath == 0)
		{
			return false;
		}

		std::vector<TCHAR> pathBuffer(cchPath + 1, 0);
		if (DragQueryFile(hDropInfo, index, pathBuffer.data(), cchPath + 1) == 0)
		{
			return false;
		}

		tstrPath.assign(pathBuffer.data());
		return true;
	}

	bool IsValidCopyDataString(const COPYDATASTRUCT* pCopyDataStruct)
	{
		if (pCopyDataStruct == NULL || pCopyDataStruct->lpData == NULL)
		{
			return false;
		}

		if (pCopyDataStruct->cbData < sizeof(TCHAR) ||
			(pCopyDataStruct->cbData % sizeof(TCHAR)) != 0)
		{
			return false;
		}

		size_t charCount = pCopyDataStruct->cbData / sizeof(TCHAR);
		const TCHAR* szData = static_cast<const TCHAR*>(pCopyDataStruct->lpData);
		for (size_t i = 0; i < charCount; ++i)
		{
			if (szData[i] == _T('\0'))
			{
				return true;
			}
		}

		return false;
	}

	struct WindowMessageFilterStatus
	{
		DWORD cbSize;
		DWORD extStatus;
	};

	typedef BOOL (WINAPI *LPFN_CHANGEWINDOWMESSAGEFILTEREX)(HWND, UINT, DWORD, void*);
	typedef BOOL (WINAPI *LPFN_CHANGEWINDOWMESSAGEFILTER)(UINT, DWORD);

	const DWORD WINDOW_MESSAGE_FILTER_ACTION_ALLOW = 1;

	void AllowMessageForWindow(HWND hWnd, UINT message)
	{
		if (hWnd == NULL)
		{
			return;
		}

		HMODULE hUser32 = GetModuleHandle(_T("user32.dll"));
		if (hUser32 == NULL)
		{
			return;
		}

		LPFN_CHANGEWINDOWMESSAGEFILTEREX pChangeWindowMessageFilterEx =
			reinterpret_cast<LPFN_CHANGEWINDOWMESSAGEFILTEREX>(GetProcAddress(hUser32, "ChangeWindowMessageFilterEx"));
		if (pChangeWindowMessageFilterEx != NULL)
		{
			WindowMessageFilterStatus cfs = { sizeof(WindowMessageFilterStatus), 0 };
			pChangeWindowMessageFilterEx(hWnd, message, WINDOW_MESSAGE_FILTER_ACTION_ALLOW, &cfs);
			return;
		}

		LPFN_CHANGEWINDOWMESSAGEFILTER pChangeWindowMessageFilter =
			reinterpret_cast<LPFN_CHANGEWINDOWMESSAGEFILTER>(GetProcAddress(hUser32, "ChangeWindowMessageFilter"));
		if (pChangeWindowMessageFilter != NULL)
		{
			pChangeWindowMessageFilter(message, WINDOW_MESSAGE_FILTER_ACTION_ALLOW);
		}
	}

	void PrepareDropTarget(CWnd* pWnd, BOOL bAccept)
	{
		if (pWnd == NULL || !::IsWindow(pWnd->GetSafeHwnd()))
		{
			return;
		}

		if (bAccept)
		{
			pWnd->ModifyStyleEx(0, WS_EX_ACCEPTFILES, 0);
		}
		else
		{
			pWnd->ModifyStyleEx(WS_EX_ACCEPTFILES, 0, 0);
		}

		pWnd->DragAcceptFiles(bAccept);
		if (bAccept)
		{
			AllowMessageForWindow(pWnd->GetSafeHwnd(), WM_DROPFILES);
			AllowMessageForWindow(pWnd->GetSafeHwnd(), WM_COPYDATA);
			AllowMessageForWindow(pWnd->GetSafeHwnd(), 0x0049);
		}
	}
}

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
	DDX_Control(pDX, IDC_CHECK_MD5, m_chkMd5);
	DDX_Control(pDX, IDC_CHECK_SHA1, m_chkSha1);
	DDX_Control(pDX, IDC_CHECK_SHA256, m_chkSha256);
	DDX_Control(pDX, IDC_CHECK_SHA512, m_chkSha512);
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

	PrepareAdvTaskbar();

	m_bFind = FALSE;
	m_btnClr.SetWindowText(GetStringByKey(MAINDLG_CLEAR));

	m_hWorkThread = NULL;
	m_waitingExit = FALSE;

	m_calculateTime = 0.0;
	m_timer = -1;

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
	PrepareDropTarget(this, TRUE);
	PrepareDropTarget(&m_editMain, TRUE);

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

	pTl = NULL;

	if(WindowsUtils::ContextMenuExisted())
	{
		// 已经添加右键菜单
		m_btnContext.SetWindowText(GetStringByKey(MAINDLG_REMOVE_CONTEXT_MENU));
	}
	else
	{
		m_btnContext.SetWindowText(GetStringByKey(MAINDLG_ADD_CONTEXT_MENU));
	}
	pWnd = (CStatic*)GetDlgItem(IDC_STATIC_ADDRESULT);
	pWnd->SetWindowText(_T(""));

	SetCtrls(FALSE);

	// 从命令行获取文件路径
	TStrVector Paras = ParseFilesCmdLine(theApp.m_lpCmdLine);
	ClearFilePaths();
	ReplaceThreadDataInputFiles(m_thrdData, Paras);
	// 从命令行获取文件路径结束

	SetThreadDataWorking(m_thrdData, false);
	m_progWhole.SetRange(0, 99);
	m_chkUppercase.SetCheck(0);
	ResetHashAlgorithmChecks();
	SyncHashAlgorithmSelections();

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
		unsigned int i;
		DragAcceptFiles(FALSE);

		ClearFilePaths();
		uint32_t droppedFileCount = DragQueryFile(hDropInfo, -1, NULL, 0);

		for(i = 0; i < droppedFileCount; i++)
		{
			tstring tstrDragFilename;
			if (CopyDraggedPath(hDropInfo, i, tstrDragFilename))
			{
				AppendThreadDataInputFile(m_thrdData, tstrDragFilename);
			}
		}

		DragFinish(hDropInfo);
		DragAcceptFiles(TRUE);

		if (HasThreadDataInputFiles(m_thrdData))
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
		IsValidCopyDataString(pCopyDataStruct))
	{
		const TCHAR *szFiles = static_cast<const TCHAR *>(pCopyDataStruct->lpData);
		TStrVector Paras = ParseFilesCmdLine(const_cast<TCHAR *>(szFiles));
		ClearFilePaths();
		ReplaceTrimmedThreadDataInputFiles(m_thrdData, Paras);

		if (HasThreadDataInputFiles(m_thrdData))
		{
			DoMD5();
		}

		return TRUE;
	}

	return CDialog::OnCopyData(pWnd, pCopyDataStruct);
}

TStrVector CFilesHashDlg::ParseFilesCmdLine(LPTSTR filesCmdLine)
{
	TStrVector parameters;
	if (filesCmdLine == NULL || filesCmdLine[0] == _T('\0'))
	{
		return parameters;
	}

#if defined(UNICODE) || defined(_UNICODE)
	int argc = 0;
	LPWSTR* argv = CommandLineToArgvW(filesCmdLine, &argc);
	if (argv == NULL)
	{
		return parameters;
	}

	for (int i = 0; i < argc; ++i)
	{
		if (argv[i] != NULL && argv[i][0] != L'\0')
		{
			parameters.push_back(argv[i]);
		}
	}
	LocalFree(argv);
#else
	std::wstring wstrCmdLine = strtowstr(std::string(filesCmdLine));
	int argc = 0;
	LPWSTR* argv = CommandLineToArgvW(wstrCmdLine.c_str(), &argc);
	if (argv == NULL)
	{
		return parameters;
	}

	for (int i = 0; i < argc; ++i)
	{
		if (argv[i] != NULL && argv[i][0] != L'\0')
		{
			parameters.push_back(wstrtostr(argv[i]));
		}
	}
	LocalFree(argv);
#endif

	return parameters;
}

void CFilesHashDlg::PrepareAdvTaskbar()
{
	m_bAdvTaskbar = FALSE;
#ifndef _DEBUG
	VERIFY(CoCreateInstance(
			CLSID_TaskbarList, NULL, CLSCTX_ALL,
			IID_ITaskbarList3, (void**)&pTl));
	if(pTl)
		m_bAdvTaskbar = TRUE;
	else
		m_bAdvTaskbar = FALSE;
#endif
}

void CFilesHashDlg::OnClose()
{
	// TODO: 在此添加消息处理程序代码和/或调用默认值
	if(IsThreadDataWorking(m_thrdData))
	{
		m_waitingExit = TRUE;
		StopWorkingThread();

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
		TCHAR* nameBuffer;
		POSITION pos;
		nameBuffer = new TCHAR[MAX_FILES_NUM * MAX_PATH + 1];
		nameBuffer[0] = 0;
		filter = GetStringByKey(FILE_STRING);
		filter.Append(_T("(*.*)|*.*|"));
		CFileDialog dlgOpen(TRUE, NULL, NULL, OFN_HIDEREADONLY|OFN_ALLOWMULTISELECT, filter, NULL, 0);
		dlgOpen.GetOFN().lpstrFile = nameBuffer;
		dlgOpen.GetOFN().nMaxFile = MAX_FILES_NUM;
		if(IDOK == dlgOpen.DoModal())
		{
			pos = dlgOpen.GetStartPosition();
			ClearFilePaths();
			for(; pos != NULL;)
				AppendThreadDataInputFile(m_thrdData, dlgOpen.GetNextPathName(pos).GetString());

			DoMD5();
		}
		delete[] nameBuffer;
	}
	else
	{
		//停止工作线程
		StopWorkingThread();
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

			SetWholeProgPos(0);
		}
		else if (strBtnText.Compare(GetStringByKey(MAINDLG_CLEAR_VERIFY)) == 0)
		{
			ClearFind();
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
		m_strFindHash = Find.GetFindHash().Trim();
		if (m_strFindHash.Compare(_T("")) != 0)
		{
			m_bFind = TRUE; // 进入搜索模式
			m_btnClr.SetWindowText(GetStringByKey(MAINDLG_CLEAR_VERIFY));

			m_editMain.ClearTextBuffer();
			ResultFind(m_strFindFile, m_strFindHash);
			m_editMain.ShowTextBuffer();
			//m_editMain.LineScrollEnd(); // 将文本框滚动到结尾

			m_btnFind.EnableWindow(FALSE);
			m_btnOpen.EnableWindow(FALSE);
		}
	}
}

void CFilesHashDlg::OnBnClickedContext()
{
	if (m_bLimited)
	{
		OSVERSIONINFOEX osvi;
		BOOL bOsVersionInfoEx;
		if (WindowsComm::GetWindowsVersion(osvi, bOsVersionInfoEx) &&
			osvi.dwMajorVersion >= 6)
		{
			if (WindowsUtils::ElevateProcess())
				ExitProcess(0);
		}
	}

	// May not a limited process.
	CStatic* pWnd = (CStatic *)GetDlgItem(IDC_STATIC_ADDRESULT);
	CString buttonText = _T("");

	m_btnContext.GetWindowText(buttonText);

	if (buttonText.Compare(GetStringByKey(MAINDLG_ADD_CONTEXT_MENU)) == 0)
	{
		WindowsUtils::RemoveContextMenu(); // Try to delete all items related to fHash
		if (WindowsUtils::AddContextMenu())
		{
			pWnd->SetWindowText(GetStringByKey(MAINDLG_ADD_SUCCEEDED));
			m_btnContext.SetWindowText(GetStringByKey(MAINDLG_REMOVE_CONTEXT_MENU));
		}
		else
		{
			pWnd->SetWindowText(GetStringByKey(MAINDLG_ADD_FAILED));
		}
	}
	else if (buttonText.Compare(GetStringByKey(MAINDLG_REMOVE_CONTEXT_MENU)) == 0)
	{
		if (WindowsUtils::RemoveContextMenu())
		{
			pWnd->SetWindowText(GetStringByKey(MAINDLG_REMOVE_SUCCEEDED));
			m_btnContext.SetWindowText(GetStringByKey(MAINDLG_ADD_CONTEXT_MENU));
		}
		else
		{
			pWnd->SetWindowText(GetStringByKey(MAINDLG_REMOVE_FAILED));
		}
	}
}

void CFilesHashDlg::OnBnClickedCheckup()
{
	// Remember current scroll position
	int iFirstVisible = m_editMain.GetFirstVisibleLine();

	if(!m_bFind)
	{
		// List mode
		RefreshResult();
		RefreshMainText(FALSE);
	}
	else
	{
		// Search mode
		m_editMain.ClearTextBuffer();
		ResultFind(m_strFindFile, m_strFindHash);
		m_editMain.ShowTextBuffer();
	}

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
		m_calculateTime += 0.1f;
		CStatic* pWnd = (CStatic*)GetDlgItem(IDC_STATIC_TIME);
		CString cstrTime;
		int i_calculateTime = (int)m_calculateTime;
		CString cstrFormat("%d ");
		cstrFormat.Append(GetStringByKey(SECOND_STRING));
		cstrTime.Format(cstrFormat, i_calculateTime);
		pWnd->SetWindowText(cstrTime);
	}
	else if(nIDEvent == 4)
	{
		// 通过命令行启动的
		DoMD5();
		KillTimer(4);
	}

	CDialog::OnTimer(nIDEvent);
}

void CFilesHashDlg::ResetHashAlgorithmChecks()
{
	m_chkMd5.SetCheck(BST_CHECKED);
	m_chkSha1.SetCheck(BST_CHECKED);
	m_chkSha256.SetCheck(BST_CHECKED);
	m_chkSha512.SetCheck(BST_CHECKED);
}

void CFilesHashDlg::SyncHashAlgorithmSelections()
{
	SetThreadDataHashAlgorithmEnabled(m_thrdData, RESULT_DIGEST_MD5, (m_chkMd5.GetCheck() != FALSE));
	SetThreadDataHashAlgorithmEnabled(m_thrdData, RESULT_DIGEST_SHA1, (m_chkSha1.GetCheck() != FALSE));
	SetThreadDataHashAlgorithmEnabled(m_thrdData, RESULT_DIGEST_SHA256, (m_chkSha256.GetCheck() != FALSE));
	SetThreadDataHashAlgorithmEnabled(m_thrdData, RESULT_DIGEST_SHA512, (m_chkSha512.GetCheck() != FALSE));
}

BOOL CFilesHashDlg::ValidateHashAlgorithmSelection()
{
	if (HasEnabledThreadDataHashAlgorithms(m_thrdData))
	{
		return TRUE;
	}

	AfxMessageBox(GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM), MB_OK | MB_ICONWARNING);
	return FALSE;
}
void CFilesHashDlg::SetWholeProgPos(UINT pos)
{
	m_progWhole.SetPos(pos);
	if (m_bAdvTaskbar)
	{
		pTl->SetProgressValue(GetSafeHwnd(), pos, 99);
	}
}

void CFilesHashDlg::DoMD5()
{
	if (m_bFind)
	{
		ClearFind();
	}

	if (m_hWorkThread)
	{
		CloseHandle(m_hWorkThread);
	}

	m_bFind = FALSE;
	m_btnClr.SetWindowText(GetStringByKey(MAINDLG_CLEAR));

	PrepareAdvTaskbar();

	SetWholeProgPos(0);

	SetThreadDataUppercase(m_thrdData, (m_chkUppercase.GetCheck() != FALSE));
	SyncHashAlgorithmSelections();
	if (!ValidateHashAlgorithmSelection())
	{
		return;
	}

	m_calculateTime = 0.0;
	m_timer = SetTimer(1, 100, NULL);
	CStatic* pWnd = (CStatic*)GetDlgItem(IDC_STATIC_TIME);
	CString cstrZero(_T("0 "));
	cstrZero.Append(GetStringByKey(SECOND_STRING));
	pWnd->SetWindowText(cstrZero);
	pWnd = (CStatic*)GetDlgItem(IDC_STATIC_SPEED);
	pWnd->SetWindowText(_T(""));

	DWORD thredID;

	SetThreadDataStop(m_thrdData, false);
	m_hWorkThread = (HANDLE)_beginthreadex(NULL,
											0,
											(unsigned int (WINAPI *)(void *))HashThreadFunc,
											&m_thrdData,
											0,
											(unsigned int *)&thredID);

}

void CFilesHashDlg::StopWorkingThread()
{
	if(IsThreadDataWorking(m_thrdData))
	{
		SetThreadDataStop(m_thrdData, true);
	}
}

HBRUSH CFilesHashDlg::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor)
{
	HBRUSH hbr = CDialog::OnCtlColor(pDC, pWnd, nCtlColor);

	// TODO:  在此更改 DC 的任何属性

	// TODO:  如果默认的不是所需画笔，则返回另一个画笔
	return hbr;
}

void CFilesHashDlg::ClearFilePaths()
{
	ResetThreadDataInputFiles(m_thrdData);
}

void CFilesHashDlg::SetCtrls(BOOL working)
{
	if(working)
	{
		PrepareDropTarget(this, FALSE);
		PrepareDropTarget(&m_editMain, FALSE);
		// Make open button to be stop button
		m_btnOpen.EnableWindow(TRUE);
		m_btnOpen.SetWindowText(GetStringByKey(MAINDLG_STOP));

		m_btnClr.EnableWindow(FALSE);
		m_btnFind.EnableWindow(FALSE);
		m_btnContext.EnableWindow(FALSE);
		m_chkUppercase.EnableWindow(FALSE);
		m_chkMd5.EnableWindow(FALSE);
		m_chkSha1.EnableWindow(FALSE);
		m_chkSha256.EnableWindow(FALSE);
		m_chkSha512.EnableWindow(FALSE);
		GotoDlgCtrl(&m_btnOpen);
	}
	else
	{
		// Make open button to be open button.
		m_btnOpen.EnableWindow(TRUE);
		m_btnOpen.SetWindowText(GetStringByKey(MAINDLG_OPEN));

		m_btnClr.EnableWindow(TRUE);
		m_btnFind.EnableWindow(TRUE);
		if(m_bLimited)
		{
			Button_SetElevationRequiredState(m_btnContext.GetSafeHwnd(), TRUE);
			//m_btnContext.SetWindowText(MAINDLG_CONTEXT_INIT);
			//m_btnContext.EnableWindow(FALSE);
		}
		else
		{
			Button_SetElevationRequiredState(m_btnContext.GetSafeHwnd(), FALSE);
			//m_btnContext.EnableWindow(TRUE);
		}
		m_btnContext.EnableWindow(TRUE);
		m_chkUppercase.EnableWindow(TRUE);
		m_chkMd5.EnableWindow(TRUE);
		m_chkSha1.EnableWindow(TRUE);
		m_chkSha256.EnableWindow(TRUE);
		m_chkSha512.EnableWindow(TRUE);
		GotoDlgCtrl(&m_btnOpen);
		PrepareDropTarget(this, TRUE);
		PrepareDropTarget(&m_editMain, TRUE);
	}
}

void CFilesHashDlg::RefreshResult()
{
	m_editMain.ClearTextBuffer();
	VisitThreadDataResults(m_thrdData, [&](const ResultData& result)
	{
		AppendResult(result);
	});
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

void CFilesHashDlg::CalcSpeed(ULONGLONG tsize)
{
	KillTimer(m_timer);
	double speed;
	if(m_calculateTime > 0.1)
	{
		speed = tsize / m_calculateTime;
		CString speedStr, measure = _T("B/s");
		if((speed / 1024) > 1)
		{
			speed /= 1024;
			measure = _T("KB/s");
			if((speed / 1024) > 1)
			{
				speed /= 1024;
				measure = _T("MB/s");
			}
		}
		speedStr.Format(_T("%4.2f "), speed);
		speedStr.Append(measure);
		CStatic* pWnd = (CStatic*)GetDlgItem(IDC_STATIC_SPEED);
		pWnd->SetWindowText(speedStr);
	}
	else
	{
		CStatic* pWnd = (CStatic*)GetDlgItem(IDC_STATIC_SPEED);
		pWnd->SetWindowText(_T(""));
	}
}

LRESULT CFilesHashDlg::OnThreadMsg(WPARAM wParam, LPARAM lParam)
{
	switch(wParam)
	{
	case WP_WORKING:
		SetCtrls(TRUE);
		break;
	case WP_REFRESH_TEXT:
		RefreshMainText();
		break;
	case WP_PROG_WHOLE:
		SetWholeProgPos((int)lParam);
		break;
	case WP_FINISHED:
		// 停止主界面计时器 计算读取速度
		CalcSpeed(GetThreadDataTotalSize(m_thrdData));
		// 停止主界面计时器 计算读取速度

		// 界面设置 - 开始
		SetCtrls(FALSE);
		// 界面设置 - 结束

		SetWholeProgPos(99);
		break;
	case WP_STOPPED:
		KillTimer(1);

		m_calculateTime = 0.0;
		CStatic* pWnd = (CStatic*)GetDlgItem(IDC_STATIC_TIME);
		pWnd->SetWindowText(_T(""));

		//界面设置 - 开始
		SetCtrls(FALSE);
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

		SetWholeProgPos(0);

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

void CFilesHashDlg::ResultFind(CString strFile, CString strHash)
{
	m_editMain.AppendTextToBuffer(GetStringByKey(MAINDLG_FIND_IN_RESULT));
	m_editMain.AppendTextToBuffer(_T("\r\n"));
	m_editMain.AppendTextToBuffer(GetStringByKey(HASHVALUE_STRING));
	m_editMain.AppendTextToBuffer(_T(" "));
	m_editMain.AppendTextToBuffer(strHash);
	m_editMain.AppendTextToBuffer(_T("\r\n\r\n"));
	m_editMain.AppendTextToBuffer(GetStringByKey(MAINDLG_RESULT));
	m_editMain.AppendTextToBuffer(_T("\r\n\r\n"));

		tstring tstrFileToFind = NormalizeResultPathSearchText(strFile.GetString());
	tstring tstrHashToFind = NormalizeDigestSearchText(strHash.GetString());

	size_t count = VisitPathAndDigestMatchingResults(GetThreadDataResults(m_thrdData), tstrFileToFind, tstrHashToFind, [&](const ResultData& result)
	{
		AppendResult(result);
	});

	if(count == 0)
		m_editMain.AppendTextToBuffer(GetStringByKey(MAINDLG_NORESULT));
}

void CFilesHashDlg::AppendResult(const ResultData& result)
{
	SetThreadDataUppercase(m_thrdData, (m_chkUppercase.GetCheck() != FALSE));
	UIBridgeMFC::AppendResultToHyperEdit(result, GetThreadDataUppercase(m_thrdData), &m_editMain);
}

void CFilesHashDlg::ClearFind()
{
	// 退出搜索模式
	m_bFind = FALSE;
	m_btnClr.SetWindowText(GetStringByKey(MAINDLG_CLEAR));

	m_btnFind.EnableWindow(TRUE);
	m_btnOpen.EnableWindow(TRUE);

	RefreshResult();
	RefreshMainText();
}