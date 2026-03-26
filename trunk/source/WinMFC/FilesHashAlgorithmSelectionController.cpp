#include "stdafx.h"

#include "FilesHashAlgorithmSelectionController.h"

#include "Common/ThreadDataAccess.h"

FilesHashAlgorithmSelectionController::FilesHashAlgorithmSelectionController()
	: m_threadData(NULL),
	m_chkMd5(NULL),
	m_chkSha1(NULL),
	m_chkSha256(NULL),
	m_chkSha512(NULL)
{
}

void FilesHashAlgorithmSelectionController::Initialize(ThreadData* threadData,
	CButton* chkMd5,
	CButton* chkSha1,
	CButton* chkSha256,
	CButton* chkSha512)
{
	m_threadData = threadData;
	m_chkMd5 = chkMd5;
	m_chkSha1 = chkSha1;
	m_chkSha256 = chkSha256;
	m_chkSha512 = chkSha512;
}

void FilesHashAlgorithmSelectionController::ResetChecks()
{
	VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)
	{
		UNREFERENCED_PARAMETER(index);
		CButton* checkBox = GetCheckBox(GetHashAlgorithmDescriptorType(algorithmDescriptor));
		if (checkBox != NULL)
		{
			checkBox->SetCheck(BST_CHECKED);
		}

		return true;
	});
}

void FilesHashAlgorithmSelectionController::SyncSelections()
{
	if (m_threadData == NULL)
	{
		return;
	}

	VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)
	{
		UNREFERENCED_PARAMETER(index);
		ResultDigestType digestType = GetHashAlgorithmDescriptorType(algorithmDescriptor);
		CButton* checkBox = GetCheckBox(digestType);
		if (checkBox != NULL)
		{
			SetThreadDataHashAlgorithmEnabled(*m_threadData, digestType, (checkBox->GetCheck() != FALSE));
		}

		return true;
	});
}

BOOL FilesHashAlgorithmSelectionController::ValidateSelection(LPCTSTR noSelectionMessage) const
{
	if (m_threadData != NULL && HasEnabledThreadDataHashAlgorithms(*m_threadData))
	{
		return TRUE;
	}

	AfxMessageBox(noSelectionMessage, MB_OK | MB_ICONWARNING);
	return FALSE;
}

void FilesHashAlgorithmSelectionController::SetEnabled(BOOL enabled)
{
	VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)
	{
		UNREFERENCED_PARAMETER(index);
		CButton* checkBox = GetCheckBox(GetHashAlgorithmDescriptorType(algorithmDescriptor));
		if (checkBox != NULL)
		{
			checkBox->EnableWindow(enabled);
		}

		return true;
	});
}

CButton* FilesHashAlgorithmSelectionController::GetCheckBox(ResultDigestType digestType) const
{
	switch (digestType)
	{
	case RESULT_DIGEST_MD5:
		return m_chkMd5;
	case RESULT_DIGEST_SHA1:
		return m_chkSha1;
	case RESULT_DIGEST_SHA256:
		return m_chkSha256;
	case RESULT_DIGEST_SHA512:
		return m_chkSha512;
	default:
		return NULL;
	}
}
