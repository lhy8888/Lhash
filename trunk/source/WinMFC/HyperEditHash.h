#pragma once

#include "HyperEdit.h"
#include "ResultTextBuffer.h"

// CHyperEditHash
#define CHyperEditHash_BASE_CLASS CHyperEdit

class CHyperEditHash : public CHyperEditHash_BASE_CLASS
{
	DECLARE_DYNAMIC(CHyperEditHash)

public:
	CHyperEditHash();
	virtual ~CHyperEditHash();

	void ClearTextBuffer();
	CString GetTextBuffer() const;

	using TextUpdate = BasicResultTextBuffer<TCHAR>::Update;
	TextUpdate TakeTextUpdate();
	void ShowTextUpdate(const TextUpdate& update, BOOL scrollToEnd);

	void AppendTextToBuffer(LPCTSTR pszText);
	void AppendLinkToBuffer(LPCTSTR pszText);

	void CopyLinkOffsets(OFFSETS& linkOffsets);
	void SetLinkOffsets(const OFFSETS& linkOffsets);

	inline CString GetLastHyperlink() const
	{ return m_cstrLastHyperlink; }

	inline CPoint GetLastScreenPoint() const
	{ return m_cpLastScreenPoint; }

	void LineScrollEnd();

protected:
	DECLARE_MESSAGE_MAP()

	BOOL PreTranslateMessage(MSG* pMsg);
	afx_msg void OnDropFiles(HDROP hDropInfo);

	virtual inline void EnableRedraw(BOOL bEnable)
	{ SendMessage(WM_SETREDRAW, bEnable, 0); }

	virtual CString GetDisplayTextRange(int start, int length) const;

	virtual void BuildOffsetList(int iCharStart, int iCharFinish);

	virtual HINSTANCE OpenHyperlink(const CString& hyperlink, CPoint point);

private:
	BasicResultTextBuffer<TCHAR> m_textBuffer; // Guarded by the shared main mutex.
	BasicResultTextView<TCHAR> m_displayText; // UI thread only, also used when painting links.

	CString m_cstrLastHyperlink;
	CPoint m_cpLastScreenPoint;

};


