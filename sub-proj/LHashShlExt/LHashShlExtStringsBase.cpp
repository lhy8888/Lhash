#include "stdafx.h"

#include "LHashShlExtStringsBase.h"

#include <tchar.h>

LHashShlExtStringsBase::LHashShlExtStringsBase()
{
	// Shell ext
	m_stringsMap[_T("SHELL_EXT_ITEM")] = _T("Hash with LHash");
	m_stringsMap[_T("SHELL_EXT_TOO_MANY_FILES")] = _T("Selected too many files");
}


