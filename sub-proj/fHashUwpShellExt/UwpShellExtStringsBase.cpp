#include "UwpShellExtStringsBase.h"

#include <tchar.h>

UwpShlExtStringsBase::UwpShlExtStringsBase()
{
	// Shell ext
#if defined (_DEBUG)
	m_stringsMap[_T("SHELL_EXT_DISPLAY_NAME")] = _T("Hash with LHash UWP DEV");
#else
	m_stringsMap[_T("SHELL_EXT_DISPLAY_NAME")] = _T("Hash with LHash UWP");
#endif
}

