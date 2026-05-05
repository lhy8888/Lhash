#include "stdafx.h"

#include "FilesHashAlgorithmSelectionController.h"

#include "Adapters/ThreadDataBridge/ThreadDataExecutionAccess.h"
#include "resource.h"

namespace
{
	const UINT HASH_ALGORITHM_CHECK_BOX_ID_BASE = 50000;
	const UINT HASH_ALGORITHM_LAYOUT_SOURCE_IDS[] =
	{
		IDC_CHECK_MD5,
		IDC_CHECK_SHA1
	};

	const int HASH_ALGORITHM_CHECK_BOX_SPACING_X = 18;
	const int HASH_ALGORITHM_CHECK_BOX_SPACING_Y = 8;
	const int HASH_ALGORITHM_CHECK_BOX_PADDING_X = 26;
	const int HASH_ALGORITHM_CHECK_BOX_MIN_WIDTH = 84;
	const int HASH_ALGORITHM_CHECK_BOX_MIN_HEIGHT = 18;
	const int HASH_ALGORITHM_LAYOUT_MAX_COLUMNS = 2;
}

FilesHashAlgorithmSelectionController::HashAlgorithmCheckBox::HashAlgorithmCheckBox()
	: algorithmId(),
	controlId(0),
	checkBox(NULL)
{
}

FilesHashAlgorithmSelectionController::FilesHashAlgorithmSelectionController()
	: m_threadData(NULL),
	m_parentWnd(NULL)
{
}

FilesHashAlgorithmSelectionController::~FilesHashAlgorithmSelectionController()
{
	DestroyDynamicCheckBoxes();
}

void FilesHashAlgorithmSelectionController::Initialize(ThreadData* threadData, CWnd* parentWnd)
{
	m_threadData = threadData;
	m_parentWnd = parentWnd;
	CreateDynamicCheckBoxes();
}

void FilesHashAlgorithmSelectionController::ResetChecks()
{
	if (m_threadData != NULL)
	{
		ResetThreadDataHashAlgorithms(*m_threadData);
	}

	VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)
	{
		UNREFERENCED_PARAMETER(index);
		CButton* checkBox = GetCheckBox(GetHashAlgorithmDescriptorId(algorithmDescriptor));
		if (checkBox != NULL)
		{
			checkBox->SetCheck(
				IsHashAlgorithmDescriptorEnabledByDefault(algorithmDescriptor) ? BST_CHECKED : BST_UNCHECKED);
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
		HashAlgorithmId algorithmId = GetHashAlgorithmDescriptorId(algorithmDescriptor);
		CButton* checkBox = GetCheckBox(algorithmId);
		if (checkBox != NULL)
		{
			SetThreadDataHashAlgorithmEnabledById(*m_threadData, algorithmId, (checkBox->GetCheck() != FALSE));
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
		CButton* checkBox = GetCheckBox(GetHashAlgorithmDescriptorId(algorithmDescriptor));
		if (checkBox != NULL)
		{
			checkBox->EnableWindow(enabled);
		}

		return true;
	});
}

BOOL FilesHashAlgorithmSelectionController::IsAlgorithmEnabled(const HashAlgorithmId& algorithmId) const
{
	if (m_threadData == NULL)
	{
		return FALSE;
	}

	return IsThreadDataHashAlgorithmEnabledById(*m_threadData, algorithmId) ? TRUE : FALSE;
}

void FilesHashAlgorithmSelectionController::SetAlgorithmEnabled(const HashAlgorithmId& algorithmId, BOOL enabled)
{
	if (m_threadData == NULL)
	{
		return;
	}

	SetThreadDataHashAlgorithmEnabledById(*m_threadData, algorithmId, (enabled != FALSE));

	CButton* checkBox = GetCheckBox(algorithmId);
	if (checkBox != NULL)
	{
		checkBox->SetCheck(enabled ? BST_CHECKED : BST_UNCHECKED);
	}
}

void FilesHashAlgorithmSelectionController::CreateDynamicCheckBoxes()
{
	DestroyDynamicCheckBoxes();

	if (m_parentWnd == NULL || !::IsWindow(m_parentWnd->GetSafeHwnd()))
	{
		return;
	}

	CRect layoutRect = GetCheckBoxLayoutRect();
	if (layoutRect.IsRectEmpty())
	{
		return;
	}

	CWnd* firstLayoutControl = m_parentWnd->GetDlgItem(HASH_ALGORITHM_LAYOUT_SOURCE_IDS[0]);
	CFont* font = (firstLayoutControl != NULL) ? firstLayoutControl->GetFont() : m_parentWnd->GetFont();
	CDC* pDC = m_parentWnd->GetDC();
	CFont* oldFont = (pDC != NULL && font != NULL) ? pDC->SelectObject(font) : NULL;

	int availableWidth = layoutRect.Width();
	int checkBoxHeight = max(layoutRect.Height(), HASH_ALGORITHM_CHECK_BOX_MIN_HEIGHT);
	int algorithmCount = GetRegisteredHashAlgorithmCount();
	int columnCount = min(HASH_ALGORITHM_LAYOUT_MAX_COLUMNS, max(1, algorithmCount));
	while (columnCount > 1)
	{
		int proposedColumnWidth = (availableWidth - ((columnCount - 1) * HASH_ALGORITHM_CHECK_BOX_SPACING_X)) / columnCount;
		if (proposedColumnWidth >= HASH_ALGORITHM_CHECK_BOX_MIN_WIDTH)
		{
			break;
		}

		--columnCount;
	}

	int columnWidth = (availableWidth - ((columnCount - 1) * HASH_ALGORITHM_CHECK_BOX_SPACING_X)) / columnCount;
	if (columnCount == 1)
	{
		columnWidth = max(columnWidth, availableWidth);
	}
	else
	{
		columnWidth = max(columnWidth, HASH_ALGORITHM_CHECK_BOX_MIN_WIDTH);
	}

	VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)
	{
		HashAlgorithmCheckBox checkBoxEntry;
		checkBoxEntry.algorithmId = GetHashAlgorithmDescriptorId(algorithmDescriptor);
		checkBoxEntry.controlId = HASH_ALGORITHM_CHECK_BOX_ID_BASE + index;
		checkBoxEntry.checkBox = new CButton();

		sunjwbase::tstring label = GetHashAlgorithmDescriptorDisplayLabel(algorithmDescriptor);
		int checkBoxWidth = static_cast<int>(label.length()) * 8 + HASH_ALGORITHM_CHECK_BOX_PADDING_X;
		if (pDC != NULL)
		{
			checkBoxWidth = pDC->GetTextExtent(label.c_str()).cx + HASH_ALGORITHM_CHECK_BOX_PADDING_X;
		}
		checkBoxWidth = min(max(checkBoxWidth, HASH_ALGORITHM_CHECK_BOX_MIN_WIDTH), columnWidth);

		int columnIndex = index % columnCount;
		int rowIndex = index / columnCount;
		int currentX = layoutRect.left + (columnIndex * (columnWidth + HASH_ALGORITHM_CHECK_BOX_SPACING_X));
		int currentY = layoutRect.top + (rowIndex * (checkBoxHeight + HASH_ALGORITHM_CHECK_BOX_SPACING_Y));

		CRect checkBoxRect(currentX, currentY, currentX + checkBoxWidth, currentY + checkBoxHeight);
		if (!checkBoxEntry.checkBox->Create(
			label.c_str(),
			WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_AUTOCHECKBOX,
			checkBoxRect,
			m_parentWnd,
			checkBoxEntry.controlId))
		{
			delete checkBoxEntry.checkBox;
			checkBoxEntry.checkBox = NULL;
			return false;
		}
		if (font != NULL)
		{
			checkBoxEntry.checkBox->SetFont(font);
		}

		m_checkBoxes.push_back(checkBoxEntry);
		return true;
	});

	if (pDC != NULL)
	{
		if (oldFont != NULL)
		{
			pDC->SelectObject(oldFont);
		}
		m_parentWnd->ReleaseDC(pDC);
	}
}

void FilesHashAlgorithmSelectionController::DestroyDynamicCheckBoxes()
{
	for (size_t index = 0; index < m_checkBoxes.size(); ++index)
	{
		CButton* checkBox = m_checkBoxes[index].checkBox;
		if (checkBox != NULL)
		{
			if (::IsWindow(checkBox->GetSafeHwnd()))
			{
				checkBox->DestroyWindow();
			}

			delete checkBox;
		}
	}

	m_checkBoxes.clear();
}

CRect FilesHashAlgorithmSelectionController::GetCheckBoxLayoutRect() const
{
	CRect layoutRect(0, 0, 0, 0);
	bool hasLayoutRect = false;

	for (int index = 0; index < _countof(HASH_ALGORITHM_LAYOUT_SOURCE_IDS); ++index)
	{
		CWnd* layoutControl = (m_parentWnd != NULL) ? m_parentWnd->GetDlgItem(HASH_ALGORITHM_LAYOUT_SOURCE_IDS[index]) : NULL;
		if (layoutControl == NULL || !::IsWindow(layoutControl->GetSafeHwnd()))
		{
			continue;
		}

		CRect controlRect;
		layoutControl->GetWindowRect(&controlRect);
		m_parentWnd->ScreenToClient(&controlRect);
		layoutControl->ShowWindow(SW_HIDE);

		if (!hasLayoutRect)
		{
			layoutRect = controlRect;
			hasLayoutRect = true;
		}
		else
		{
			layoutRect.UnionRect(layoutRect, controlRect);
		}
	}

	CWnd* openButton = (m_parentWnd != NULL) ? m_parentWnd->GetDlgItem(IDC_OPEN) : NULL;
	if (openButton != NULL && ::IsWindow(openButton->GetSafeHwnd()))
	{
		CRect openButtonRect;
		openButton->GetWindowRect(&openButtonRect);
		m_parentWnd->ScreenToClient(&openButtonRect);
		layoutRect.right = openButtonRect.left - HASH_ALGORITHM_CHECK_BOX_SPACING_X;
	}

	if (layoutRect.Width() <= 0 || layoutRect.Height() <= 0)
	{
		return CRect(0, 0, 0, 0);
	}

	return layoutRect;
}

CButton* FilesHashAlgorithmSelectionController::GetCheckBox(const HashAlgorithmId& algorithmId) const
{
	for (size_t index = 0; index < m_checkBoxes.size(); ++index)
	{
		if (NormalizeHashAlgorithmId(m_checkBoxes[index].algorithmId) == NormalizeHashAlgorithmId(algorithmId))
		{
			return m_checkBoxes[index].checkBox;
		}
	}

	return NULL;
}
