#include "stdafx.h"

#include "FilesHashSearchController.h"

#include "Common/HashResultSearch.h"
#include "Adapters/MfcBridge/ThreadDataExecutionAccess.h"
#include "Adapters/MfcBridge/ThreadDataResultAccess.h"
#include "UIBridgeMFC.h"
#include "WinCommon/WindowsStrings.h"

using namespace sunjwbase;
using namespace WindowsStrings;

FilesHashSearchController::FilesHashSearchController()
	: m_threadData(NULL),
	m_mainEdit(NULL),
	m_btnClr(NULL),
	m_btnFind(NULL),
	m_btnOpen(NULL),
	m_chkUppercase(NULL),
	m_searchActive(FALSE)
{
}

void FilesHashSearchController::Initialize(ThreadData* threadData,
	CHyperEditHash* mainEdit,
	CButton* btnClr,
	CButton* btnFind,
	CButton* btnOpen,
	CButton* chkUppercase)
{
	m_threadData = threadData;
	m_mainEdit = mainEdit;
	m_btnClr = btnClr;
	m_btnFind = btnFind;
	m_btnOpen = btnOpen;
	m_chkUppercase = chkUppercase;
}

BOOL FilesHashSearchController::IsActive() const
{
	return m_searchActive;
}

BOOL FilesHashSearchController::BeginSearch(const CString& findFile,
	const CString& findHash,
	LPCTSTR clearVerifyText)
{
	m_strFindFile = findFile;
	m_strFindHash = findHash;
	m_strFindFile.Trim();
	m_strFindHash.Trim();

	if (m_strFindHash.IsEmpty())
	{
		return FALSE;
	}

	m_searchActive = TRUE;

	if (m_btnClr != NULL)
	{
		m_btnClr->SetWindowText(clearVerifyText);
	}
	if (m_btnFind != NULL)
	{
		m_btnFind->EnableWindow(FALSE);
	}
	if (m_btnOpen != NULL)
	{
		m_btnOpen->EnableWindow(FALSE);
	}

	RebuildCurrentView();
	return TRUE;
}

void FilesHashSearchController::ClearSearch(LPCTSTR clearText)
{
	m_searchActive = FALSE;

	if (m_btnClr != NULL)
	{
		m_btnClr->SetWindowText(clearText);
	}
	if (m_btnFind != NULL)
	{
		m_btnFind->EnableWindow(TRUE);
	}
	if (m_btnOpen != NULL)
	{
		m_btnOpen->EnableWindow(TRUE);
	}

	RebuildCurrentView();
}

void FilesHashSearchController::RebuildCurrentView()
{
	if (m_mainEdit == NULL)
	{
		return;
	}

	m_mainEdit->ClearTextBuffer();

	if (m_searchActive)
	{
		RebuildSearchResults();
	}
	else
	{
		RebuildResultList();
	}
}

void FilesHashSearchController::RebuildResultList()
{
	if (m_threadData == NULL)
	{
		return;
	}

	VisitThreadDataHashResults(*m_threadData, [&](const HashResult& result)
	{
		AppendResult(result);
	});
}

void FilesHashSearchController::RebuildSearchResults()
{
	if (m_threadData == NULL || m_mainEdit == NULL)
	{
		return;
	}

	m_mainEdit->AppendTextToBuffer(GetStringByKey(MAINDLG_FIND_IN_RESULT));
	m_mainEdit->AppendTextToBuffer(_T("\r\n"));
	m_mainEdit->AppendTextToBuffer(GetStringByKey(HASHVALUE_STRING));
	m_mainEdit->AppendTextToBuffer(_T(" "));
	m_mainEdit->AppendTextToBuffer(m_strFindHash);
	m_mainEdit->AppendTextToBuffer(_T("\r\n\r\n"));
	m_mainEdit->AppendTextToBuffer(GetStringByKey(MAINDLG_RESULT));
	m_mainEdit->AppendTextToBuffer(_T("\r\n\r\n"));

	tstring tstrFileToFind = NormalizeHashResultPathSearchText(m_strFindFile.GetString());
	tstring tstrHashToFind = NormalizeHashResultDigestSearchText(m_strFindHash.GetString());

	size_t count = VisitThreadDataPathAndDigestMatchingHashResults(*m_threadData, tstrFileToFind, tstrHashToFind, [&](const HashResult& result)
	{
		AppendResult(result);
	});

	if (count == 0)
	{
		m_mainEdit->AppendTextToBuffer(GetStringByKey(MAINDLG_NORESULT));
	}
}

void FilesHashSearchController::AppendResult(const HashResult& result)
{
	if (m_threadData == NULL || m_mainEdit == NULL)
	{
		return;
	}

	SetThreadDataUppercase(*m_threadData, (m_chkUppercase != NULL && m_chkUppercase->GetCheck() != FALSE));
	UIBridgeMFC::AppendResultToHyperEdit(result, GetThreadDataUppercase(*m_threadData), m_mainEdit);
}
