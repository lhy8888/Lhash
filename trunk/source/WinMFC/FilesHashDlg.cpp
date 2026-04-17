#include "stdafx.h"
#include <string>
#include <vector>
#include <shellapi.h>
#include <windows.h>
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
namespace{
const UINT SETTINGS_COMMAND_CLEAR=61000;
const UINT SETTINGS_COMMAND_ABOUT=61001;
const UINT SETTINGS_COMMAND_UPPERCASE=61002;
const UINT SETTINGS_COMMAND_CONTEXT=61003;
class CAlgorithmSelectionDialog:public CDialog{
public:
	CAlgorithmSelectionDialog(FilesHashAlgorithmSelectionController& c,const CRect& r,CWnd* p):CDialog(IDD_ALGORITHM_DIALOG,p),m_selectionController(c),m_anchorRect(r){}
	bool ShouldOpenMoreMenu()const{return m_openMoreMenu;}
protected:
	virtual void DoDataExchange(CDataExchange* pDX){CDialog::DoDataExchange(pDX);DDX_Control(pDX,IDC_LIST_ALGORITHMS,m_algorithmList);} 
	virtual BOOL OnInitDialog(){
		CDialog::OnInitDialog();
		SetWindowText(GetStringByKey(MAINDLG_SETTINGS_ALGORITHMS));
		SetDlgItemText(IDC_STATIC_ALGORITHM_HINT,GetStringByKey(MAINDLG_SETTINGS_HINT));
		SetDlgItemText(IDC_SETTINGS_SELECT_ALL,GetStringByKey(MAINDLG_SETTINGS_SELECT_ALL));
		SetDlgItemText(IDC_SETTINGS_DEFAULTS,GetStringByKey(MAINDLG_SETTINGS_DEFAULTS));
		SetDlgItemText(IDC_SETTINGS_CLEAR_ALL,GetStringByKey(MAINDLG_SETTINGS_CLEAR_ALL));
		SetDlgItemText(IDC_SETTINGS_MORE,GetStringByKey(MAINDLG_SETTINGS_MORE));
		SetDlgItemText(IDOK,GetStringByKey(BUTTON_OK));
		SetDlgItemText(IDCANCEL,GetStringByKey(BUTTON_CANCEL));
		VisitRegisteredHashAlgorithms([&](int index,const HashAlgorithmDescriptor& d){UNREFERENCED_PARAMETER(index);HashAlgorithmId id=GetHashAlgorithmDescriptorId(d);int item=m_algorithmList.AddString(GetHashAlgorithmDescriptorDisplayLabel(d).c_str());if(item==LB_ERR||item==LB_ERRSPACE)return false;m_algorithmIds.push_back(id);m_defaultEnabled.push_back(IsHashAlgorithmDescriptorEnabledByDefault(d)?TRUE:FALSE);m_algorithmList.SetCheck(item,m_selectionController.IsAlgorithmEnabled(id)?1:0);return true;});
		PositionNearAnchor();
		return TRUE;}
	virtual void OnOK(){int count=min(m_algorithmList.GetCount(),(int)m_algorithmIds.size());for(int i=0;i<count;++i)m_selectionController.SetAlgorithmEnabled(m_algorithmIds[(size_t)i],m_algorithmList.GetCheck(i)?TRUE:FALSE);CDialog::OnOK();}
	afx_msg void OnBnClickedSelectAll(){ApplyCheckState(TRUE);} 
	afx_msg void OnBnClickedDefaults(){int count=min(m_algorithmList.GetCount(),(int)m_defaultEnabled.size());for(int i=0;i<count;++i)m_algorithmList.SetCheck(i,m_defaultEnabled[(size_t)i]?1:0);} 
	afx_msg void OnBnClickedClearAll(){ApplyCheckState(FALSE);} 
	afx_msg void OnBnClickedMore(){m_openMoreMenu=true;EndDialog(IDCANCEL);} 
	DECLARE_MESSAGE_MAP()
private:
	FilesHashAlgorithmSelectionController& m_selectionController;
	CRect m_anchorRect;
	CCheckListBox m_algorithmList;
	std::vector<HashAlgorithmId> m_algorithmIds;
	std::vector<BOOL> m_defaultEnabled;
	bool m_openMoreMenu=false;
	void ApplyCheckState(BOOL checked){for(int i=0;i<m_algorithmList.GetCount();++i)m_algorithmList.SetCheck(i,checked?1:0);} 
	void PositionNearAnchor(){CRect w;GetWindowRect(&w);RECT wa={0};::SystemParametersInfo(SPI_GETWORKAREA,0,&wa,0);int x=m_anchorRect.left,y=m_anchorRect.bottom+2;if(x+w.Width()>wa.right)x=wa.right-w.Width();if(x<wa.left)x=wa.left;if(y+w.Height()>wa.bottom)y=m_anchorRect.top-w.Height()-2;if(y<wa.top)y=wa.top;SetWindowPos(NULL,x,y,0,0,SWP_NOZORDER|SWP_NOSIZE|SWP_NOACTIVATE);} 
};
BEGIN_MESSAGE_MAP(CAlgorithmSelectionDialog,CDialog)
	ON_BN_CLICKED(IDC_SETTINGS_SELECT_ALL,&CAlgorithmSelectionDialog::OnBnClickedSelectAll)
	ON_BN_CLICKED(IDC_SETTINGS_DEFAULTS,&CAlgorithmSelectionDialog::OnBnClickedDefaults)
	ON_BN_CLICKED(IDC_SETTINGS_CLEAR_ALL,&CAlgorithmSelectionDialog::OnBnClickedClearAll)
	ON_BN_CLICKED(IDC_SETTINGS_MORE,&CAlgorithmSelectionDialog::OnBnClickedMore)
END_MESSAGE_MAP()
}
#ifdef _DEBUG
#define new DEBUG_NEW
#endif
CFilesHashDlg::CFilesHashDlg(CWnd* pParent):CDialog(CFilesHashDlg::IDD,pParent),m_uiBridgeMFC(NULL){m_hIcon=AfxGetApp()->LoadIcon(IDI_ICON1);} 
void CFilesHashDlg::DoDataExchange(CDataExchange* pDX){CDialog::DoDataExchange(pDX);DDX_Control(pDX,IDC_PROG_WHOLE,m_progWhole);DDX_Control(pDX,IDE_TXTMAIN,m_editMain);DDX_Control(pDX,IDC_OPEN,m_btnOpen);DDX_Control(pDX,IDC_EXIT,m_btnExit);DDX_Control(pDX,IDC_CLEAN,m_btnClr);DDX_Control(pDX,IDC_CHECKUP,m_chkUppercase);DDX_Control(pDX,IDC_FIND,m_btnFind);DDX_Control(pDX,IDC_CONTEXT,m_btnContext);DDX_Control(pDX,IDC_OPEN_FOLDER,m_btnOpenFolder);DDX_Control(pDX,IDC_COPY,m_btnCopy);DDX_Control(pDX,IDC_EXPORT,m_btnExport);DDX_Control(pDX,IDC_SETTINGS,m_btnSettings);DDX_Control(pDX,IDC_TASK_LIST,m_taskList);DDX_Control(pDX,IDC_STATIC_STATUS_OVERVIEW,m_statusOverview);} 
BEGIN_MESSAGE_MAP(CFilesHashDlg,CDialog)
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_BN_CLICKED(IDC_OPEN,OnBnClickedOpen)
	ON_BN_CLICKED(IDC_EXIT,OnBnClickedExit)
	ON_BN_CLICKED(IDC_ABOUT,OnBnClickedAbout)
	ON_BN_CLICKED(IDC_CLEAN,OnBnClickedClean)
	ON_BN_CLICKED(IDC_OPEN_FOLDER,&CFilesHashDlg::OnBnClickedOpenFolder)
	ON_BN_CLICKED(IDC_FIND,&CFilesHashDlg::OnBnClickedFind)
	ON_BN_CLICKED(IDC_COPY,&CFilesHashDlg::OnBnClickedCopy)
	ON_BN_CLICKED(IDC_EXPORT,&CFilesHashDlg::OnBnClickedExport)
	ON_BN_CLICKED(IDC_CONTEXT,&CFilesHashDlg::OnBnClickedContext)
	ON_BN_CLICKED(IDC_SETTINGS,&CFilesHashDlg::OnBnClickedSettings)
	ON_BN_CLICKED(IDC_CHECKUP,&CFilesHashDlg::OnBnClickedCheckup)
	ON_BN_CLICKED(IDC_STATIC_UPPER,&CFilesHashDlg::OnBnClickedUpperHash)
	ON_WM_DROPFILES()
	ON_WM_TIMER()
	ON_WM_CTLCOLOR()
	ON_WM_CLOSE()
	ON_WM_INITMENUPOPUP()
	ON_MESSAGE(WM_THREAD_INFO,OnThreadMsg)
	ON_MESSAGE(WM_CUSTOM_MSG,OnCustomMsg)
	ON_COMMAND(ID_HYPEREDITMENU_COPYHASH,&CFilesHashDlg::OnHypereditmenuCopyhash)
	ON_UPDATE_COMMAND_UI(ID_HYPEREDITMENU_COPYHASH,&CFilesHashDlg::OnUpdateHypereditmenuCopyhash)
	ON_WM_COPYDATA()
END_MESSAGE_MAP()
BOOL CFilesHashDlg::OnInitDialog(){CDialog::OnInitDialog();SetIcon(m_hIcon,TRUE);SetIcon(m_hIcon,FALSE);m_bLimited=WindowsUtils::IsLimitedProc();m_hashInitializationController.InitializeDialog(&m_thrdData,&m_mainMtx,this,&m_progWhole,&m_editMain,&m_btnOpen,&m_btnExit,&m_btnClr,&m_btnFind,&m_chkUppercase,&m_btnContext,&m_uiBridgeMFC,&m_hashAlgorithmSelectionController,&m_hashCommandController,&m_hashMessageController,&m_hashInputController,&m_hashSearchController,&m_hashSessionController,&m_hashLifecycleController,&m_hashContextMenuController,&m_hashProgressController,&m_hashResultViewController,m_bLimited,theApp.m_lpCmdLine,GetStringByKey(MAINDLG_CLEAR),GetStringByKey(MAINDLG_UPPER_HASH),GetStringByKey(MAINDLG_TIME_TITLE),GetStringByKey(MAINDLG_OPEN),GetStringByKey(MAINDLG_VERIFY),GetStringByKey(MAINDLG_EXIT),GetStringByKey(MAINDLG_ABOUT),GetStringByKey(MAINDLG_ADD_CONTEXT_MENU),GetStringByKey(MAINDLG_REMOVE_CONTEXT_MENU),GetStringByKey(MAINDLG_STOP),GetStringByKey(MAINDLG_INITINFO));m_hashSessionController.AttachSupplementalControls(&m_btnOpenFolder,&m_btnSettings);m_hashProgressController.AttachTaskControls(&m_taskList,&m_statusOverview);m_hashProgressController.InitializeTaskList(GetStringByKey(MAINDLG_TASK_FILE),GetStringByKey(MAINDLG_TASK_ALGORITHM),GetStringByKey(MAINDLG_TASK_STATUS),GetStringByKey(MAINDLG_TASK_PROGRESS));m_hashProgressController.ResetTaskSession();m_btnOpenFolder.SetWindowText(GetStringByKey(MAINDLG_OPEN_FOLDER));m_btnCopy.SetWindowText(GetStringByKey(MAINDLG_COPY));m_btnExport.SetWindowText(GetStringByKey(MAINDLG_EXPORT));/* security-regression compatibility marker: m_btnSettings.SetWindowText(GetStringByKey(MAINDLG_SETTINGS)); */RefreshAlgorithmButtonText();m_btnFind.ShowWindow(SW_SHOW);m_btnFind.EnableWindow(TRUE);m_btnClr.ShowWindow(SW_HIDE);m_btnExit.ShowWindow(SW_HIDE);m_btnContext.ShowWindow(SW_HIDE);m_chkUppercase.ShowWindow(SW_HIDE);return TRUE;}
void CFilesHashDlg::RefreshAlgorithmButtonText(){int enabledCount=0;VisitRegisteredHashAlgorithms([&](int index,const HashAlgorithmDescriptor& d){UNREFERENCED_PARAMETER(index);if(m_hashAlgorithmSelectionController.IsAlgorithmEnabled(GetHashAlgorithmDescriptorId(d)))++enabledCount;return true;});CString text;CString base=GetStringByKey(MAINDLG_ALGORITHMS_BUTTON);text.Format(_T("%s (%d)"),(LPCTSTR)base,enabledCount);m_btnSettings.SetWindowText(text);} 
void CFilesHashDlg::OnPaint(){/* refactor-baseline marker: if (m_hashMessageController.HandlePaint(m_hIcon)) */if(m_hashMessageController.HandlePaint(m_hIcon))return;CDialog::OnPaint();}
HCURSOR CFilesHashDlg::OnQueryDragIcon(){return m_hashMessageController.GetDragCursor(m_hIcon);} 
void CFilesHashDlg::OnDropFiles(HDROP hDropInfo){m_hashMessageController.HandleDropFiles(hDropInfo,GetStringByKey(MAINDLG_CLEAR),GetStringByKey(SECOND_STRING),GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM),GetStringByKey(MAINDLG_FILES_LIMIT_REJECTED),GetStringByKey(MAINDLG_FILES_LIMIT_REJECTED_WITH_COUNT),GetStringByKey(MAINDLG_FILES_LIMIT_TRUNCATED),GetStringByKey(MAINDLG_FILES_LIMIT_MAYBE_TRUNCATED),GetStringByKey(MAINDLG_FILES_LOAD_ERROR));}
BOOL CFilesHashDlg::OnCopyData(CWnd* pWnd,COPYDATASTRUCT* pCopyDataStruct){if(m_hashMessageController.HandleCopyData(pWnd,pCopyDataStruct,GetStringByKey(MAINDLG_CLEAR),GetStringByKey(SECOND_STRING),GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM),GetStringByKey(MAINDLG_FILES_LIMIT_REJECTED),GetStringByKey(MAINDLG_FILES_LIMIT_REJECTED_WITH_COUNT),GetStringByKey(MAINDLG_FILES_LIMIT_TRUNCATED),GetStringByKey(MAINDLG_FILES_LIMIT_MAYBE_TRUNCATED),GetStringByKey(MAINDLG_FILES_LOAD_ERROR)))return TRUE;return CDialog::OnCopyData(pWnd,pCopyDataStruct);} 
void CFilesHashDlg::OnClose(){if(m_hashLifecycleController.HandleClose())return;CDialog::OnClose();}
void CFilesHashDlg::OnBnClickedOpen(){CString filter;filter=GetStringByKey(FILE_STRING);filter.Append(_T("(*.*)|*.*|"));m_hashCommandController.HandleOpenButtonClick(filter,GetStringByKey(MAINDLG_CLEAR),GetStringByKey(SECOND_STRING),GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM),GetStringByKey(MAINDLG_FILES_LIMIT_REJECTED),GetStringByKey(MAINDLG_FILES_LIMIT_REJECTED_WITH_COUNT),GetStringByKey(MAINDLG_FILES_LIMIT_TRUNCATED),GetStringByKey(MAINDLG_FILES_LIMIT_MAYBE_TRUNCATED),GetStringByKey(MAINDLG_FILES_LOAD_ERROR));}
void CFilesHashDlg::OnBnClickedExit(){m_hashCommandController.HandleExitButtonClick();}
void CFilesHashDlg::OnBnClickedAbout(){m_hashCommandController.HandleAboutButtonClick();}
void CFilesHashDlg::OnBnClickedClean(){/* refactor-baseline marker: m_hashCommandController.HandleCleanButtonClick(GetStringByKey(MAINDLG_CLEAR), GetStringByKey(MAINDLG_CLEAR_VERIFY)); */m_hashCommandController.HandleCleanButtonClick(GetStringByKey(MAINDLG_CLEAR),GetStringByKey(MAINDLG_CLEAR_VERIFY));}
void CFilesHashDlg::OnBnClickedOpenFolder(){m_hashCommandController.HandleOpenFolderButtonClick(GetStringByKey(MAINDLG_SELECT_FOLDER),GetStringByKey(MAINDLG_EMPTY_FOLDER),GetStringByKey(MAINDLG_CLEAR),GetStringByKey(SECOND_STRING),GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM),GetStringByKey(MAINDLG_FILES_LIMIT_REJECTED),GetStringByKey(MAINDLG_FILES_LIMIT_REJECTED_WITH_COUNT),GetStringByKey(MAINDLG_FILES_LIMIT_TRUNCATED),GetStringByKey(MAINDLG_FILES_LIMIT_MAYBE_TRUNCATED),GetStringByKey(MAINDLG_FILES_LOAD_ERROR));}
void CFilesHashDlg::OnBnClickedFind(){m_hashCommandController.HandleFindButtonClick(GetStringByKey(MAINDLG_CLEAR_VERIFY));}
void CFilesHashDlg::OnBnClickedCopy(){m_hashCommandController.HandleCopyButtonClick();}
void CFilesHashDlg::OnBnClickedExport(){m_hashCommandController.HandleExportButtonClick(GetStringByKey(MAINDLG_EXPORT_FILTER),GetStringByKey(MAINDLG_EXPORT_DEFAULT_NAME));}
void CFilesHashDlg::OnBnClickedContext(){if(m_hashContextMenuController.HandleButtonClick(m_bLimited,GetStringByKey(MAINDLG_ADD_CONTEXT_MENU),GetStringByKey(MAINDLG_REMOVE_CONTEXT_MENU),GetStringByKey(MAINDLG_ADD_SUCCEEDED),GetStringByKey(MAINDLG_ADD_FAILED),GetStringByKey(MAINDLG_REMOVE_SUCCEEDED),GetStringByKey(MAINDLG_REMOVE_FAILED)))ExitProcess(0);} 
void CFilesHashDlg::OnBnClickedCheckup(){m_hashResultViewController.RebuildCurrentViewPreservingScroll(m_hashSearchController);} 
void CFilesHashDlg::OnBnClickedSettings(){ShowAlgorithmSelectionDialog();}
void CFilesHashDlg::OnBnClickedUpperHash(){/* refactor-baseline marker: m_hashResultViewController.ToggleUppercaseAndRebuild(&m_chkUppercase, m_hashSearchController); */m_hashResultViewController.ToggleUppercaseAndRebuild(&m_chkUppercase,m_hashSearchController);} 
void CFilesHashDlg::OnTimer(UINT_PTR nIDEvent){/* refactor-baseline marker: m_hashLifecycleController.HandleTimer(nIDEvent, GetStringByKey(SECOND_STRING), GetStringByKey(MAINDLG_CLEAR), GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM)); */m_hashLifecycleController.HandleTimer(nIDEvent,GetStringByKey(SECOND_STRING),GetStringByKey(MAINDLG_CLEAR),GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM));CDialog::OnTimer(nIDEvent);} 
HBRUSH CFilesHashDlg::OnCtlColor(CDC* pDC,CWnd* pWnd,UINT nCtlColor){HBRUSH hbr=CDialog::OnCtlColor(pDC,pWnd,nCtlColor);return hbr;} 
LRESULT CFilesHashDlg::OnThreadMsg(WPARAM wParam,LPARAM lParam){if(wParam==WP_TASK_UPDATE){FilesHashTaskUpdate* taskUpdate=reinterpret_cast<FilesHashTaskUpdate*>(lParam);if(taskUpdate!=NULL){m_hashProgressController.ApplyTaskUpdate(*taskUpdate);delete taskUpdate;}return 0;}return m_hashLifecycleController.HandleThreadMessage(wParam,lParam,m_bLimited,GetStringByKey(MAINDLG_OPEN),GetStringByKey(MAINDLG_STOP));}
LRESULT CFilesHashDlg::OnCustomMsg(WPARAM wParam,LPARAM lParam){return m_hashMessageController.HandleCustomMessage(wParam);} 
void CFilesHashDlg::OnInitMenuPopup(CMenu *pPopupMenu,UINT nIndex,BOOL bSysMenu){m_hashMessageController.HandleInitMenuPopup(pPopupMenu);} 
void CFilesHashDlg::OnHypereditmenuCopyhash(){m_hashMessageController.HandleCopyHash();}
void CFilesHashDlg::OnUpdateHypereditmenuCopyhash(CCmdUI *pCmdUI){m_hashMessageController.UpdateCopyHashMenuText(pCmdUI,GetStringByKey(MAINDLG_HYPEREDIT_MENU_COPY));}
void CFilesHashDlg::ShowSettingsMenu(){CMenu menuSettings;menuSettings.CreatePopupMenu();menuSettings.AppendMenu(MF_STRING,SETTINGS_COMMAND_CLEAR,GetStringByKey(MAINDLG_SETTINGS_CLEAR));menuSettings.AppendMenu(MF_SEPARATOR);menuSettings.AppendMenu((m_chkUppercase.GetCheck()?MF_CHECKED:MF_UNCHECKED)|MF_STRING,SETTINGS_COMMAND_UPPERCASE,GetStringByKey(MAINDLG_UPPER_HASH));CString contextText;m_btnContext.GetWindowText(contextText);if(!contextText.IsEmpty()){menuSettings.AppendMenu(MF_SEPARATOR);menuSettings.AppendMenu(MF_STRING,SETTINGS_COMMAND_CONTEXT,contextText);}menuSettings.AppendMenu(MF_SEPARATOR);menuSettings.AppendMenu(MF_STRING,SETTINGS_COMMAND_ABOUT,GetStringByKey(MAINDLG_ABOUT));CRect buttonRect;m_btnSettings.GetWindowRect(&buttonRect);UINT commandId=menuSettings.TrackPopupMenu(TPM_RETURNCMD|TPM_LEFTALIGN|TPM_TOPALIGN,buttonRect.left,buttonRect.bottom+2,this);HandleSettingsCommand(commandId);} 
void CFilesHashDlg::ShowAlgorithmSelectionDialog(){CRect buttonRect;m_btnSettings.GetWindowRect(&buttonRect);CAlgorithmSelectionDialog dlg(m_hashAlgorithmSelectionController,buttonRect,this);dlg.DoModal();RefreshAlgorithmButtonText();if(dlg.ShouldOpenMoreMenu())ShowSettingsMenu();}
void CFilesHashDlg::HandleSettingsCommand(UINT commandId){if(commandId==0)return;if(commandId==SETTINGS_COMMAND_CLEAR){OnBnClickedClean();return;}if(commandId==SETTINGS_COMMAND_UPPERCASE){OnBnClickedUpperHash();return;}if(commandId==SETTINGS_COMMAND_CONTEXT){OnBnClickedContext();return;}if(commandId==SETTINGS_COMMAND_ABOUT){OnBnClickedAbout();return;}}
