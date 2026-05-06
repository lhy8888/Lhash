#pragma once

#include "afxwin.h"

#include "Common/HashTypes.h"
#include "Domain/HashResult.h"
#include "HyperEditHash.h"
struct ThreadData;

class FilesHashSearchController
{
public:
	FilesHashSearchController();

	void Initialize(ThreadData* threadData,
		CHyperEditHash* mainEdit,
		CButton* btnClr,
		CButton* btnFind,
		CButton* btnOpen,
		CButton* chkUppercase);

	BOOL IsActive() const;
	BOOL BeginSearch(const CString& findFile,
		const CString& findHash,
		LPCTSTR clearVerifyText);
	void ClearSearch(LPCTSTR clearText);
	void RebuildCurrentView();

private:
	void RebuildResultList();
	void RebuildSearchResults();
	void AppendResult(const HashResult& result);

	ThreadData* m_threadData;
	CHyperEditHash* m_mainEdit;
	CButton* m_btnClr;
	CButton* m_btnFind;
	CButton* m_btnOpen;
	CButton* m_chkUppercase;
	BOOL m_searchActive;
	CString m_strFindFile;
	CString m_strFindHash;
};
