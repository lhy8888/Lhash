#include "stdafx.h"

#include "WindowsStrings.h"

#include <Windows.h>
#include <tchar.h>
#include <mutex>
#include "OsUtils/OsThread.h"
#include "Common/strhelper.h"
#include "WindowsStringsInternal.h"

using namespace std;
using namespace sunjwbase;

namespace WindowsStrings
{
	static std::once_flag s_init;
	static WindowsStringsMgr* s_winStrsMgr = NULL;
	static LANGID s_uiLang = -1;

	static void InitStringsManager()
	{
		std::call_once(s_init, [] {
			s_winStrsMgr = WindowsStringsMgr::getInstance();
			s_uiLang = GetUserDefaultUILanguage();
		});
	}

	int RegisterStringsForLang(LANGID langId, WindowsStringsMap *stringsMap)
	{
		InitStringsManager();

		return s_winStrsMgr->registerStringsForLang(langId, stringsMap);
	}

	LANGID GetCurrentUILang()
	{
		InitStringsManager();

		return s_uiLang;
	}

	const TCHAR *GetStringByStringKey(const TCHAR *tzhKey)
	{
		InitStringsManager();

		return s_winStrsMgr->getStringForLang(s_uiLang, tzhKey);
	}
}
