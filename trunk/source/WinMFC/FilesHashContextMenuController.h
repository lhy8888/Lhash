#pragma once

#include "afxwin.h"

class FilesHashContextMenuController
{
public:
	FilesHashContextMenuController();

	void Initialize(CButton* contextButton, CWnd* statusLabel);
	void ResetStatus();
	void RefreshButtonText(LPCTSTR addText, LPCTSTR removeText);
	BOOL HandleButtonClick(BOOL limited,
		LPCTSTR addText,
		LPCTSTR removeText,
		LPCTSTR addSucceededText,
		LPCTSTR addFailedText,
		LPCTSTR removeSucceededText,
		LPCTSTR removeFailedText);

private:
	BOOL TryElevateLimitedProcess() const;
	void SetStatusText(LPCTSTR statusText);

	CButton* m_contextButton;
	CWnd* m_statusLabel;
};
