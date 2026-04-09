#pragma once

#include <shellapi.h>

#include "afxwin.h"

#include "Common/Global.h"
struct ThreadData;

class FilesHashInputController
{
public:
	FilesHashInputController();

	void Initialize(ThreadData* threadData, CWnd* parentWnd);

	void LoadCommandLineFiles(LPTSTR filesCmdLine);
	BOOL LoadOpenFileDialogSelection(LPCTSTR fileFilter);
	BOOL LoadFolderDialogSelection(LPCTSTR folderDialogTitle, LPCTSTR emptyFolderMessage);
	BOOL LoadDroppedFiles(HDROP hDropInfo);
	BOOL LoadCopyDataFiles(const COPYDATASTRUCT* pCopyDataStruct);

private:
	static bool CopyDraggedPath(HDROP hDropInfo, UINT index, sunjwbase::tstring& tstrPath);
	static bool IsValidCopyDataString(const COPYDATASTRUCT* pCopyDataStruct);
	static TStrVector ParseFilesCmdLine(LPTSTR filesCmdLine);
	static void AppendFolderFilesRecursive(const sunjwbase::tstring& folderPath, TStrVector& files);

	void ClearFilePaths();

	ThreadData* m_threadData;
	CWnd* m_parentWnd;
};
