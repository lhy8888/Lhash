#pragma once

#include <vector>

#include "afxwin.h"

#include "Common/Global.h"
#include "Domain/HashAlgorithmRegistryCore.h"
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
	BOOL IsAlgorithmEnabled(const HashAlgorithmId& algorithmId) const;
	void SetAlgorithmEnabled(const HashAlgorithmId& algorithmId, BOOL enabled);

private:
	struct HashAlgorithmCheckBox
	{
		HashAlgorithmCheckBox();

		HashAlgorithmId algorithmId;
		UINT controlId;
		CButton* checkBox;
	};

	void CreateDynamicCheckBoxes();
	void DestroyDynamicCheckBoxes();
	CRect GetCheckBoxLayoutRect() const;
	CButton* GetCheckBox(const HashAlgorithmId& algorithmId) const;

	ThreadData* m_threadData;
	CWnd* m_parentWnd;
	std::vector<HashAlgorithmCheckBox> m_checkBoxes;
};
