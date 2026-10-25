// fileshash.cpp : 定义应用程序的类行为。
//

#include "stdafx.h"
#include "fileshash.h"
#include "fileshashdlg.h"
#include "WindowsUtils.h"
#include "WinCommon/WindowsStrings.h"
#include "WinCommon/ComApartmentScope.h"
#include "UIStringsBase.h"
#include "UIStringsZHCN.h"

//#define MEMORY_LEAK_TEST
#undef MEMORY_LEAK_TEST

// Memory leak test
#if defined (MEMORY_LEAK_TEST)
#define _CRTDBG_MAP_ALLOC
#include <stdlib.h>
#include <crtdbg.h>
#endif
// Memory leak test


using namespace WindowsStrings;

namespace
{
    struct UiComApartmentApi
    {
        static HRESULT Initialize()
        {
            return CoInitializeEx(NULL, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);
        }
        static void Uninitialize() { CoUninitialize(); }
    };
}

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

// CFILESHASHApp

BEGIN_MESSAGE_MAP(CFILESHASHApp, CWinApp)
	ON_COMMAND(ID_HELP, CWinApp::OnHelp)
END_MESSAGE_MAP()


// CFILESHASHApp 构造

CFILESHASHApp::CFILESHASHApp() : m_exitCode(1)
{
	// TODO: 在此处添加构造代码，
	// 将所有重要的初始化放置在 InitInstance 中
}


// 唯一的一个 CFILESHASHApp 对象

CFILESHASHApp theApp;


// CFILESHASHApp 初始化

BOOL CFILESHASHApp::InitInstance()
{
	// Memory leak test
#if defined (MEMORY_LEAK_TEST)
	_CrtSetDbgFlag ( _CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF );
#endif
	// Memory leak test

	WindowsUtils::InitializeProcessDllSearchPolicy();

	// 如果一个运行在 Windows XP 上的应用程序清单指定要
	// 使用 ComCtl32.dll 版本 6 或更高版本来启用可视化方式，
	//则需要 InitCommonControls()。否则，将无法创建窗口。

	INITCOMMONCONTROLSEX initCommonControls = { sizeof(INITCOMMONCONTROLSEX), ICC_WIN95_CLASSES | ICC_LISTVIEW_CLASSES | ICC_PROGRESS_CLASS };
	InitCommonControlsEx(&initCommonControls);

	if (!CWinApp::InitInstance()) return FALSE;

	// Register multi-lang.
	RegisterStringsForLang(-1, new UIStringsBase());
	RegisterStringsForLang(2052, new UIStringsZHCN());

	// COM must outlive the dialog itself, including its taskbar controller's
	// destructor after DoModal returns. The callback scope enforces that order.
	if (!RunInComApartment<UiComApartmentApi>([this] {
		CFilesHashDlg dlg;
		struct MainWindowReset {
			CWnd*& window;
			~MainWindowReset() { window = NULL; }
		} resetMainWindow{m_pMainWnd};
		m_pMainWnd = &dlg;
		m_exitCode = dlg.DoModal() == -1 ? 1 : 0;
	}))
	{
		AfxMessageBox(_T("Cannot initialize the Windows COM apartment."), MB_OK | MB_ICONERROR);
	}
	m_pMainWnd = NULL;

	// The modal dialog owns the message loop; the application should now exit.
	return FALSE;
}

int CFILESHASHApp::ExitInstance()
{
	// Preserve MFC cleanup, but a modal application must not use the last
	// dispatched message wParam (for example WP_WORKER_JOINED) as its exit code.
	CWinApp::ExitInstance();
	return m_exitCode;
}
