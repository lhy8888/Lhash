#pragma once

#include "afxwin.h"

#include "Common/Global.h"
#include "Common/HashAlgorithmRegistry.h"

class FilesHashAlgorithmSelectionController
{
public:
	FilesHashAlgorithmSelectionController();

	void Initialize(ThreadData* threadData,
		CButton* chkMd5,
		CButton* chkSha1,
		CButton* chkSha256,
		CButton* chkSha512);

	void ResetChecks();
	void SyncSelections();
	BOOL ValidateSelection(LPCTSTR noSelectionMessage) const;
	void SetEnabled(BOOL enabled);

private:
	CButton* GetCheckBox(ResultDigestType digestType) const;

	ThreadData* m_threadData;
	CButton* m_chkMd5;
	CButton* m_chkSha1;
	CButton* m_chkSha256;
	CButton* m_chkSha512;
};
