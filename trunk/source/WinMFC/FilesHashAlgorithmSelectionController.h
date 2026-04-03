#pragma once

#include <vector>

#include "afxwin.h"

#include "Common/Global.h"
#include "Common/HashAlgorithmRegistry.h"
#include "LegacyCompat/LegacyThreadData.h"

class FilesHashAlgorithmSelectionController
{
public:
	FilesHashAlgorithmSelectionController();
	~FilesHashAlgorithmSelectionController();

	void Initialize(ThreadData* threadData, CWnd* parentWnd);

	void ResetChecks();
	void SyncSelections();
	BOOL ValidateSelection(LPCTSTR noSelectionMessage) const;
	void SetEnabled(BOOL enabled);

private:
	struct HashAlgorithmCheckBox
	{
		HashAlgorithmCheckBox();

		ResultDigestType digestType;
		UINT controlId;
		CButton* checkBox;
	};

	void CreateDynamicCheckBoxes();
	void DestroyDynamicCheckBoxes();
	CRect GetCheckBoxLayoutRect() const;
	CButton* GetCheckBox(ResultDigestType digestType) const;

	ThreadData* m_threadData;
	CWnd* m_parentWnd;
	std::vector<HashAlgorithmCheckBox> m_checkBoxes;
};
