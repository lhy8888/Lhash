#include "stdafx.h"

#include "FilesHashContextMenuController.h"

#include "WindowsUtils.h"
#include "WinCommon/WindowsComm.h"

FilesHashContextMenuController::FilesHashContextMenuController()
	: m_contextButton(NULL),
	m_statusLabel(NULL)
{
}

void FilesHashContextMenuController::Initialize(CButton* contextButton, CWnd* statusLabel)
{
	m_contextButton = contextButton;
	m_statusLabel = statusLabel;
}

void FilesHashContextMenuController::ResetStatus()
{
	SetStatusText(_T(""));
}

void FilesHashContextMenuController::RefreshButtonText(LPCTSTR addText, LPCTSTR removeText)
{
	if (m_contextButton == NULL)
	{
		return;
	}

	if (WindowsUtils::ContextMenuExisted())
	{
		m_contextButton->SetWindowText(removeText);
	}
	else
	{
		m_contextButton->SetWindowText(addText);
	}
}

BOOL FilesHashContextMenuController::HandleButtonClick(BOOL limited,
	LPCTSTR addText,
	LPCTSTR removeText,
	LPCTSTR addSucceededText,
	LPCTSTR addFailedText,
	LPCTSTR removeSucceededText,
	LPCTSTR removeFailedText)
{
	if (limited && TryElevateLimitedProcess())
	{
		return TRUE;
	}

	if (m_contextButton == NULL)
	{
		return FALSE;
	}

	CString buttonText;
	m_contextButton->GetWindowText(buttonText);

	if (buttonText.Compare(addText) == 0)
	{
		WindowsUtils::RemoveContextMenu(); // Try to delete all items related to LHash
		if (WindowsUtils::AddContextMenu())
		{
			SetStatusText(addSucceededText);
			m_contextButton->SetWindowText(removeText);
		}
		else
		{
			SetStatusText(addFailedText);
		}
	}
	else if (buttonText.Compare(removeText) == 0)
	{
		if (WindowsUtils::RemoveContextMenu())
		{
			SetStatusText(removeSucceededText);
			m_contextButton->SetWindowText(addText);
		}
		else
		{
			SetStatusText(removeFailedText);
		}
	}

	return FALSE;
}

BOOL FilesHashContextMenuController::TryElevateLimitedProcess() const
{
	if (WindowsComm::IsWindowsVistaOrGreater())
	{
		if (WindowsUtils::ElevateProcess())
		{
			return TRUE;
		}
	}

	return FALSE;
}

void FilesHashContextMenuController::SetStatusText(LPCTSTR statusText)
{
	if (m_statusLabel != NULL)
	{
		m_statusLabel->SetWindowText(statusText);
	}
}
