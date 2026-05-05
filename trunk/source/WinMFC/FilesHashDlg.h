// MD5SUM03Dlg.h : dialog declaration

#pragma once
#include "afxwin.h"
#include "afxcmn.h"
#include <vector>

#include "HyperEditHash.h"
#include "FilesHashTaskUpdate.h"

#include "Common/strhelper.h"
#include "OsUtils/OsThread.h"

#include "Common/HashTypes.h"
#include "Adapters/MfcBridge/MfcHashState.h"
#include "UIBridgeMFC.h"
#include "FilesHashAlgorithmSelectionController.h"
#include "FilesHashCommandController.h"
#include "FilesHashMessageController.h"
#include "FilesHashInputController.h"
#include "FilesHashSearchController.h"
#include "FilesHashSessionController.h"

#include "FilesHashContextMenuController.h"
#include "FilesHashInitializationController.h"
#include "FilesHashLifecycleController.h"
#include "FilesHashProgressController.h"
#include "FilesHashResultViewController.h"

// CFilesHashDlg dialog
class CFilesHashDlg : public CDialog
{
public:
	CFilesHashDlg(CWnd* pParent = NULL);
	enum { IDD = IDD_MAIN_DIALOG };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);

	virtual BOOL OnInitDialog();
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()
	afx_msg void OnBnClickedOpen();
	afx_msg void OnBnClickedExit();
	afx_msg void OnBnClickedAbout();
	afx_msg void OnBnClickedClean();
	afx_msg void OnBnClickedOpenFolder();
	afx_msg void OnBnClickedFind();
	afx_msg void OnBnClickedCopy();
	afx_msg void OnBnClickedExport();
	afx_msg void OnBnClickedContext();
	afx_msg void OnBnClickedSettings();
	afx_msg void OnBnClickedCheckup();
	afx_msg void OnBnClickedUpperHash();
	afx_msg void OnDropFiles(HDROP hDropInfo);
	afx_msg BOOL OnCopyData(CWnd* pWnd, COPYDATASTRUCT* pCopyDataStruct);
	afx_msg void OnTimer(UINT_PTR nIDEvent);
	afx_msg void OnClose();
	afx_msg HBRUSH OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor);
	afx_msg LRESULT OnThreadMsg(WPARAM, LPARAM);
	afx_msg LRESULT OnCustomMsg(WPARAM, LPARAM);
	afx_msg void OnInitMenuPopup(CMenu *pPopupMenu, UINT nIndex, BOOL bSysMenu);
	afx_msg void OnTaskListGetDispInfo(NMHDR* pNMHDR, LRESULT* pResult);
	afx_msg void OnHypereditmenuCopyhash();
	afx_msg void OnUpdateHypereditmenuCopyhash(CCmdUI *pCmdUI);

protected:
	HICON m_hIcon;

	CProgressCtrl m_progWhole;
	CHyperEditHash m_editMain;
	CButton m_btnOpen;
	CButton m_btnExit;
	CButton m_btnClr;
	CButton m_btnFind;
	CButton m_btnOpenFolder;
	CButton m_btnCopy;
	CButton m_btnExport;
	CButton m_btnSettings;
	CButton m_chkUppercase;
	CButton m_btnContext;
	CListCtrl m_taskList;
	CStatic m_statusOverview;

	sunjwbase::OsMutex m_mainMtx;

	UIBridgeMFC *m_uiBridgeMFC;
	FilesHashAlgorithmSelectionController m_hashAlgorithmSelectionController;
	FilesHashCommandController m_hashCommandController;
	FilesHashMessageController m_hashMessageController;
	FilesHashInputController m_hashInputController;
	FilesHashSearchController m_hashSearchController;
	FilesHashSessionController m_hashSessionController;
	FilesHashLifecycleController m_hashLifecycleController;

	FilesHashContextMenuController m_hashContextMenuController;
	FilesHashInitializationController m_hashInitializationController;
	FilesHashProgressController m_hashProgressController;
	FilesHashResultViewController m_hashResultViewController;
	ThreadData m_thrdData;
	BOOL m_bLimited;

	void ShowSettingsMenu();
	void ShowAlgorithmSelectionDialog();
	void HandleSettingsCommand(UINT commandId);
};
