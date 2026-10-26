// src\HyperEditHash.cpp : 实现文件
//
#include "stdafx.h"
#include "HyperEditHash.h"

#include "WinMFC/WinMFCMessages.h"
#include "Common/HashTypes.h"

// CHyperEditHash

IMPLEMENT_DYNAMIC(CHyperEditHash, CHyperEditHash_BASE_CLASS)

CHyperEditHash::CHyperEditHash()
{
	ClearTextBuffer();

	COLORREF crBlack = RGB(0, 0, 0);
	SetHyperlinkColors(crBlack, crBlack);
}

CHyperEditHash::~CHyperEditHash()
{
}


BEGIN_MESSAGE_MAP(CHyperEditHash, CHyperEditHash_BASE_CLASS)
	ON_WM_DROPFILES()
END_MESSAGE_MAP()



// CHyperEditHash 消息处理程序
BOOL CHyperEditHash::PreTranslateMessage(MSG* pMsg)
{
    // 编辑框快捷键操作
    if(WM_KEYDOWN == pMsg->message)
    {
		if(::GetFocus() == GetSafeHwnd() && (GetKeyState( VK_CONTROL) & 0xFF00 ) == 0xFF00)   
        {
            // 全选
            if( pMsg->wParam == 'A' || pMsg->wParam == 'a')
            {
                this->SetSel(0, -1);
                return true;
            }

            // 拷贝
            if( pMsg->wParam == 'C' || pMsg->wParam == 'c')
            {
                this->Copy();
                return true;
			}

            // 剪切
            if( pMsg->wParam == 'X' || pMsg->wParam == 'x')
            {
                this->Cut();
                return true;
			}

            // 粘贴
            if( pMsg->wParam == 'V' || pMsg->wParam == 'v')
            {
                this->Paste();
                return true;
            }

            // 撤销
            if( pMsg->wParam == 'Z' || pMsg->wParam == 'z')
            {
                this->Undo();
                return true;
            }

        }  
    }  

    return CHyperEditHash_BASE_CLASS::PreTranslateMessage(pMsg);  
}

void CHyperEditHash::ClearTextBuffer()
{
	m_textBuffer.Clear();
}

CString CHyperEditHash::GetTextBuffer() const
{
	return CString(m_textBuffer.Text().c_str());
}

CHyperEditHash::TextUpdate CHyperEditHash::TakeTextUpdate()
{
	return m_textBuffer.Drain();
}

void CHyperEditHash::ShowTextUpdate(const TextUpdate& update, BOOL scrollToEnd)
{
	if (update.Empty() || !m_displayText.CanApply(update)) return;
	// All allocations and producer data access are complete before window calls.
	// Link offsets and the display cache belong exclusively to the UI thread.
	if (update.reset) m_linkOffsets.clear();
	const size_t neededLinks = m_linkOffsets.size() + update.links.size();
	if (neededLinks > m_linkOffsets.capacity())
		m_linkOffsets.reserve((std::max)(neededLinks, m_linkOffsets.capacity() * 2));
	for (const auto& link : update.links) m_linkOffsets.push_back({link.start, link.length});
	m_displayText.Apply(update);

	int selectionStart = 0, selectionEnd = 0;
	GetSel(selectionStart, selectionEnd);
	const int firstVisible = GetFirstVisibleLine();
	EnableRedraw(FALSE);
	if (update.reset)
	{
		SetWindowText(update.text.c_str());
	}
	else if (!update.text.empty())
	{
		// EM_SETSEL with start == -1 only collapses the current selection;
		// it does not move the caret to the end of the control. Each refresh
		// restores the user's selection below, so append at an explicit offset.
		// Otherwise the native text diverges from the display cache/link offsets.
		const int appendPosition = GetWindowTextLength();
		SetSel(appendPosition, appendPosition, TRUE);
		ReplaceSel(update.text.c_str(), FALSE);
	}
	// Appending must not destroy a user's selection while results are arriving.
	const int textLength = static_cast<int>(m_displayText.Size());
	SetSel((std::min)(selectionStart, textLength), (std::min)(selectionEnd, textLength), TRUE);
	if (scrollToEnd) LineScroll(GetLineCount());
	else LineScroll(firstVisible - GetFirstVisibleLine());
	EnableRedraw(TRUE);
	Invalidate(FALSE);
}

CString CHyperEditHash::GetDisplayTextRange(int start, int length) const
{
	if (start < 0 || length <= 0) return CString();
	return CString(m_displayText.Range(static_cast<size_t>(start), static_cast<size_t>(length)).c_str());
}

void CHyperEditHash::AppendTextToBuffer(LPCTSTR pszText)
{
	m_textBuffer.Append(pszText);
}

void CHyperEditHash::AppendLinkToBuffer(LPCTSTR pszText)
{
	m_textBuffer.Append(pszText, true);
}

void CHyperEditHash::CopyLinkOffsets(OFFSETS& linkOffsets)
{
	linkOffsets.clear();
	linkOffsets.reserve(m_textBuffer.Links().size());
	for (const auto& link : m_textBuffer.Links()) linkOffsets.push_back({link.start, link.length});
}

void CHyperEditHash::SetLinkOffsets(const OFFSETS& linkOffsets)
{
	std::vector<ResultTextLink> links;
	links.reserve(linkOffsets.size());
	for (const auto& link : linkOffsets) links.push_back({link.iStart, link.iLength});
	m_textBuffer.ReplaceLinks(links);
}

void CHyperEditHash::LineScrollEnd()
{
	EnableRedraw(FALSE);
	LineScroll(GetLineCount());
	EnableRedraw(TRUE);
}

void CHyperEditHash::BuildOffsetList(int iCharStart, int iCharFinish)
{
	// We do nothing in CHyperEditHash::BuildOffsetList.
	// m_linkOffsets is already built in AppendLinkToBuffer.
}

HINSTANCE CHyperEditHash::OpenHyperlink(const CString& hyperlink, CPoint point)
{
	if (hyperlink.IsEmpty())
		return 0;

	ClientToScreen(&point);

	m_cstrLastHyperlink = hyperlink;
	m_cpLastScreenPoint = point;
	
	CWnd *parentWnd = GetParent();
	if (parentWnd != NULL)
	{
		::PostMessage(parentWnd->GetSafeHwnd(), WM_CUSTOM_MSG, WM_HYPEREDIT_MENU, 0);
	}

	return 0;
}

void CHyperEditHash::OnDropFiles(HDROP hDropInfo)
{
	CWnd* parentWnd = GetParent();
	if (parentWnd != NULL)
	{
		parentWnd->SendMessage(WM_DROPFILES, reinterpret_cast<WPARAM>(hDropInfo), 0);
		return;
	}

	DragFinish(hDropInfo);
}

