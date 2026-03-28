// MD5SUM03Dlg.h : 头文件
//

#pragma once
#include "afxwin.h"
#include "afxcmn.h"

#include "HyperEditHash.h"

#include "Common/strhelper.h"
#include "OsUtils/OsThread.h"

#include "Common/Global.h"
#include "UIBridgeMFC.h"
#include "FilesHashAlgorithmSelectionController.h"
#include "FilesHashInputController.h"
#include "FilesHashSearchController.h"
#include "FilesHashSessionController.h"

#include "FilesHashContextMenuController.h"
#include "FilesHashInitializationController.h"
#include "FilesHashProgressController.h"
#include "FilesHashResultViewController.h"

// CMD5SUM03Dlg 对话框
class CFilesHashDlg : public CDialog
{
// 构造
public:
	CFilesHashDlg(CWnd* pParent = NULL);	// 标准构造函数

// 对话框数据
	enum { IDD = IDD_MAIN_DIALOG };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV 支持

	virtual BOOL OnInitDialog();
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()
	afx_msg void OnBnClickedOpen();
	afx_msg void OnBnClickedExit();
	afx_msg void OnBnClickedAbout();
	afx_msg void OnBnClickedClean();
	afx_msg void OnBnClickedFind();
	afx_msg void OnBnClickedContext();
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
	afx_msg void OnHypereditmenuCopyhash();
	afx_msg void OnUpdateHypereditmenuCopyhash(CCmdUI *pCmdUI);

// 实现
protected:
	HICON m_hIcon;

	CProgressCtrl m_progWhole;
	CHyperEditHash m_editMain;
	CButton m_btnOpen;
	CButton m_btnExit;
	CButton m_btnClr;
	CButton m_btnFind;
	CButton m_chkUppercase;
	CButton m_btnContext;

	sunjwbase::OsMutex m_mainMtx;

	UIBridgeMFC *m_uiBridgeMFC;
	FilesHashAlgorithmSelectionController m_hashAlgorithmSelectionController;
	FilesHashInputController m_hashInputController;
	FilesHashSearchController m_hashSearchController;
	FilesHashSessionController m_hashSessionController;

	FilesHashContextMenuController m_hashContextMenuController;
	FilesHashInitializationController m_hashInitializationController;
	FilesHashProgressController m_hashProgressController;
	FilesHashResultViewController m_hashResultViewController;
	ThreadData m_thrdData;
	BOOL m_waitingExit; // 等待线程退出后，退出程序

	BOOL m_bLimited;

	void DoMD5();

};
