//********************************************************************************************
//*	HYPEREDIT.CPP - Custom window message handler(s) implementation                          *
//*                                                                                          *
//* This module contains implementation code for object initialization and default windows   *
//* messages.                                                                                *
//*                                                                                          *
//* Aug.31.04                                                                                *
//*                                                                                          *
//* Copyright PCSpectra 2004 (Free for any purpose, except to sell indivually)               *
//* Website: www.pcspectra.com                                                               *
//*                                                                                          *
//* Notes:                                                                                   *
//* ======																					 *
//* Search module for 'PROGRAMMERS NOTE'                                                     *
//*                                                                                          *
//* History:						                                                         *
//*	========																				 *
//* 7.8.2016 - Sun Junwen                                                                    *
//* Change for LHash usage.                                                                  *
//********************************************************************************************

#include "stdafx.h"
#include "HyperEdit.h"
#include "winuser.h"
#include "ResultTextBuffer.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

IMPLEMENT_DYNAMIC(CHyperEdit, CEdit)

// Object construction

CHyperEdit::CHyperEdit()
{
	m_nLineHeight = 0;

	m_bMouseOnHyperlink = FALSE;

	// Set default colors
	m_crEditBk = RGB(255, 255, 255);
	m_crHyperlinkNormal = RGB(92, 92, 154);
	m_crHyperlinkHover = RGB(168, 168, 230);

	m_csLocation.Empty();
}

// Object destruction

CHyperEdit::~CHyperEdit()
{
	m_oFont.DeleteObject();	// Delete hyperlink font object
	m_brEditBk.DeleteObject();
}
		  
BEGIN_MESSAGE_MAP(CHyperEdit, CEdit)
	//{{AFX_MSG_MAP(CHyperEdit)
	ON_WM_MOUSEMOVE()
	ON_WM_SETCURSOR()
	ON_WM_CTLCOLOR_REFLECT()
	ON_WM_HSCROLL()
	ON_WM_VSCROLL()
	ON_WM_DESTROY()
	ON_WM_LBUTTONUP()
	ON_WM_LBUTTONDOWN()
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

// Control initialization

void CHyperEdit::PreSubclassWindow() 
{
	CEdit::PreSubclassWindow();
	
	ASSERT(GetStyle() & ES_MULTILINE); // MUST be in multiline state
	ASSERT(GetStyle() & WS_VSCROLL); // We need vertical scrollbar
	//ASSERT(GetStyle() & ES_AUTOVSCROLL);

	// Initialize font object(s)
	CDC* pDC = GetDC();
	ASSERT(pDC);

	CFont* pTemp = GetFont(); // Get the font dialog controls will be using
	ASSERT(pTemp);

	LOGFONT lf;
	pTemp->GetLogFont(&lf);

	// TODO:
	// Check if there is a system setting that we can check to see if hyperlinks should be 
	// underlined by default or not.
	lf.lfUnderline = TRUE; // Our font needs to be undelrined to distinguish itself as hyper

	// Create our hyperlink font :)
	m_oFont.CreateFontIndirect(&lf);

	// Calculate single line height
	m_nLineHeight = pDC->DrawText(_T("Test Line"), CRect(0,0,0,0), DT_SINGLELINE|DT_CALCRECT);

}

int CHyperEdit::GetFirstInvisibleLine()
{
	CRect rcRect;
	GetRect(rcRect);
	int iLastVisibleLine = GetFirstVisibleLine();
	int iLineCount = GetLineCount();
	for (; iLastVisibleLine < iLineCount; ++iLastVisibleLine)
	{
		int iChar = LineIndex(iLastVisibleLine);
		CPoint ptChar = PosFromChar(iChar);
		if (ptChar.y > rcRect.bottom)
			break;
	}

	if (iLastVisibleLine >= iLineCount)
		iLastVisibleLine = iLineCount - 1;

	return iLastVisibleLine;
}

// Override mouse movements

void CHyperEdit::OnMouseMove(UINT nFlags, CPoint point) 
{
	m_bMouseOnHyperlink = FALSE;

	CString csURL = GetHyperlinkFromPoint(point);
	if (!csURL.IsEmpty())
		m_bMouseOnHyperlink = TRUE;

	CEdit::OnMouseMove(nFlags, point);
}

BOOL CHyperEdit::OnSetCursor(CWnd* pWnd, UINT nHitTest, UINT message)
{
	if (nHitTest == HTCAPTION)
	{
		::SetCursor(AfxGetApp()->LoadStandardCursor(IDC_ARROW));
		return TRUE;
	}

	CPoint ptMouse(GetMessagePos()); // Current mouse location
	ScreenToClient(&ptMouse);

	CRect rectEdit;
	GetRect(rectEdit); // Get the formatting rectangle for the edit control
	if (ptMouse.x < rectEdit.left ||
		ptMouse.x > rectEdit.right ||
		ptMouse.y < rectEdit.top ||
		ptMouse.y > rectEdit.bottom)
	{
		::SetCursor(AfxGetApp()->LoadStandardCursor(IDC_ARROW));
		return TRUE;
	}

	if (m_bMouseOnHyperlink)
		::SetCursor(AfxGetApp()->LoadStandardCursor(IDC_HAND));
	else
		::SetCursor(AfxGetApp()->LoadStandardCursor(IDC_IBEAM));

	return TRUE;
}

HBRUSH CHyperEdit::CtlColor(CDC* pDC, UINT nCtlColor)
{
	pDC->SetBkMode(TRANSPARENT);
	m_brEditBk.DeleteObject();
	m_brEditBk.CreateSolidBrush(m_crEditBk);
	return (HBRUSH)m_brEditBk.GetSafeHandle();
}

// Override left mouse button down (Clicking)

void CHyperEdit::OnLButtonDown(UINT nFlags, CPoint point) 
{
	m_csLocation = GetHyperlinkFromPoint(point);

	CEdit::OnLButtonDown(nFlags, point);
}

// Override left mouse button up (Clicking)

void CHyperEdit::OnLButtonUp(UINT nFlags, CPoint point) 
{
	CEdit::OnLButtonUp(nFlags, point);

	int iSelStart=0, iSelFinish=0; 
	GetSel(iSelStart, iSelFinish);

	// If there is a selection, just exit now, we don't open URL's
	if(IsSelection(iSelStart, iSelFinish)) return;

	CString csURL = GetHyperlinkFromPoint(point);

	// If not empty, then open browser and show web site
	// only if the URL is the same as one clicked on in OnLButtonDown()
	if(!csURL.IsEmpty() && (m_csLocation==csURL))
	{
		OpenHyperlink(csURL, point);
		m_bMouseOnHyperlink = FALSE;
	}
}

// Override low level message handling

LRESULT CHyperEdit::WindowProc(UINT message, WPARAM wParam, LPARAM lParam) 
{
	// handling OnPaint() didn't work
	if (message == WM_PAINT)
	{
		CEdit::WindowProc(message, wParam, lParam);

		DrawHyperlinks();

		return TRUE;
	}
	
	return CEdit::WindowProc(message, wParam, lParam);
}

//
// DrawHyperlinks() draws a hyperlink colored character(individually) over existing
// CEdit text entered by user. It does this by first building a internal offset list of all
// valid hyperlinks and then iterating over each of them and their individual characters
// painting them the color specified by the client programmer or using default colors.
//

void CHyperEdit::DrawHyperlinks()
{
	const int textLength = GetWindowTextLength();
	const int firstCharacter = LineIndex(GetFirstVisibleLine());
	if (textLength == 0 || firstCharacter < 0) return;
	int lastCharacter = LineIndex(GetFirstInvisibleLine() + 1);
	if (lastCharacter < 0) lastCharacter = textLength;
	BuildOffsetList(firstCharacter, lastCharacter);
	const auto visible = VisibleResultLinkRange(m_linkOffsets, firstCharacter, lastCharacter,
		[](const _TOKEN_OFFSET& link) { return ResultTextLink{link.iStart, link.iLength}; });
	if (visible.first == visible.second) return;
	const CString visibleText = GetDisplayTextRange(firstCharacter, lastCharacter - firstCharacter);
	lastCharacter = firstCharacter + visibleText.GetLength();

	CRect rect;
	GetRect(rect);
	CDC* dc = GetDC();
	if (dc == NULL) return;
	CFont* previousFont = dc->SelectObject(&m_oFont);
	dc->SetBkMode(TRANSPARENT);
	dc->IntersectClipRect(rect);
	dc->SetTextColor(m_crHyperlinkNormal);
	int selectionStart = 0, selectionEnd = 0;
	GetSel(selectionStart, selectionEnd);
	for (size_t index = visible.first; index < visible.second; ++index)
	{
		const auto& link = m_linkOffsets[index];
		const int begin = (std::max)(link.iStart, firstCharacter);
		const int end = (std::min)(link.iStart + link.iLength, lastCharacter);
		for (int character = begin; character < end; ++character)
		{
			if (IsSelection(selectionStart, selectionEnd) && character >= selectionStart && character < selectionEnd) continue;
			const CPoint point = PosFromCharEx(character);
			const CString token(visibleText.GetAt(character - firstCharacter));
			const CSize size = dc->GetTextExtent(token);
			if (character == link.iStart) dc->FillSolidRect(point.x - 1, point.y, size.cx + 1, size.cy, m_crEditBk);
			else if (character == link.iStart + link.iLength - 1) dc->FillSolidRect(point.x, point.y, size.cx + 1, size.cy, m_crEditBk);
			else dc->FillSolidRect(point.x, point.y, size.cx, size.cy, m_crEditBk);
			dc->TextOut(point.x, point.y, token);
		}
	}
	dc->SelectObject(previousFont);
	ReleaseDC(dc);
}

CString CHyperEdit::GetDisplayTextRange(int start, int length) const
{
	CString text;
	GetWindowText(text);
	return start < 0 || length <= 0 ? CString() : text.Mid(start, length);
}

//
// Builds an offset list of all valid hyperlinks. This function is optimized by only
// searching through characters which are visible, ignoring those that would require 
// scrolling to view. When this function encounters an token and it determines it's
// hyperlink-able (Using virtual IsWordHyperlink) the starting offset and length of where
// the hyperlink exists within the actual CEdit buffer is recorded and pushed onto a vector
//

void CHyperEdit::BuildOffsetList(int iCharStart, int iCharFinish)
{
	// Entire control buffer and individual token buffer
	CString csBuff, csToken; 
	GetWindowText(csBuff);

	// Clear previous hyperlink offsets from vector and rebuild list
	m_linkOffsets.clear(); 

	// Rebuild list of hyperlink character offsets starting at iCharStart and ending at iCharFinish
	for(int i=iCharStart, iCurr=iCharStart; i<=iCharFinish; i++){
		
		if(IsWhiteSpaceOrEOF(csBuff, i)){	// Also checks for EOB (End of buffer)

			_TOKEN_OFFSET off = { iCurr /* Start offset */, i-iCurr /* Length */ };

			// Let client programmer define what tokens can be hyperlinked or not
			// if one desires he/she could easily implement an easy check using a
			// regex library on email addresses without using the mailto: suffix
			if(IsWordHyperlink(csToken))
				m_linkOffsets.push_back(off); // Save the starting offset for current token

			csToken.Empty(); // Flush previous token 		
			iCurr = i+1; // Initialize the start offset for next token
		}
		else 
			csToken += csBuff.GetAt(i); // Concatenate another char onto token
	}
}

CPoint CHyperEdit::PosFromCharEx(UINT nChar)
{
	const int length = GetWindowTextLength();
	if (length <= 0) return CPoint(0, 0);
	if (nChar < static_cast<UINT>(length)) return PosFromChar(nChar);
	// CEdit::PosFromChar cannot address the position after the last character.
	CPoint point = PosFromChar(length - 1);
	CDC* dc = GetDC();
	if (dc != NULL)
	{
		point.x += dc->GetTextExtent(GetDisplayTextRange(length - 1, 1)).cx;
		ReleaseDC(dc);
	}
	return point;
}

//
// Returns a hyperlinks URL if mouse cursor is actually over a URL
// and if mouse isn't over any hyperlink it returns a empty CString
//

CString CHyperEdit::GetHyperlinkFromPoint(CPoint& pt)
{
	const int firstCharacter = LineIndex(GetFirstVisibleLine());
	if (firstCharacter < 0) return CString();
	int lastCharacter = LineIndex(GetFirstInvisibleLine() + 1);
	if (lastCharacter < 0) lastCharacter = GetWindowTextLength();
	const auto visible = VisibleResultLinkRange(m_linkOffsets, firstCharacter, lastCharacter,
		[](const _TOKEN_OFFSET& link) { return ResultTextLink{link.iStart, link.iLength}; });
	for (size_t index = visible.second; index > visible.first; --index)
	{
		const auto& link = m_linkOffsets[index - 1];
		const CPoint left = PosFromCharEx(link.iStart);
		const CPoint right = PosFromCharEx(link.iStart + link.iLength);
		if (pt.y > left.y && pt.y < left.y + m_nLineHeight && pt.x >= left.x && pt.x <= right.x)
			return GetDisplayTextRange(link.iStart, link.iLength);
	}
	return CString();
}

//
// Returns TRUE if indexed character within buffer is whitespace
//

BOOL CHyperEdit::IsWhiteSpaceOrEOF(const CString& csBuff, int iIndex) const 
{ 
	// Check for End of buffer 
	if(csBuff.GetLength() == iIndex) return TRUE;
		
	// Check for whitespace
	if(csBuff.GetAt(iIndex) == WHITE_SPACE1) return TRUE;
	if(csBuff.GetAt(iIndex) == WHITE_SPACE2) return TRUE;
	if(csBuff.GetAt(iIndex) == WHITE_SPACE3) return TRUE;
	if(csBuff.GetAt(iIndex) == WHITE_SPACE4) return TRUE;

	return FALSE;
}

//
// Virtual function which can be overridden by client programmer to allow advanced
// hyperlinking. For example in a derived class we could use regex to validate
// email addresses without using the 'mailto:' suffix.
//

BOOL CHyperEdit::IsWordHyperlink(const CString& csToken) const
{
	CString csTemp(csToken); // Make a temp copy so we can convert it's case
	csTemp.MakeLower();

	// A trivial approach to hyperlinking web sites or email addresses
	// In a derived class we can use regex if we like to only hyperlink
	// fully qualified URL's etc...
	if(csTemp.Left(7) == _T("http://")) return TRUE;
	if(csTemp.Left(7) == _T("mailto:")) return TRUE;
	//if(csTemp.Left(5) == "file:") return TRUE;

	return FALSE; // Not a valid token by default
}	  

HINSTANCE CHyperEdit::OpenHyperlink(const CString& hyperlink, CPoint point)
{
	return GotoURL(hyperlink);
}

HINSTANCE CHyperEdit::GotoURL(const CString& url) const
{
	HINSTANCE result = ShellExecute(NULL, _T("open"), url.GetString(), NULL, NULL, SW_SHOW);

	return result;
}
