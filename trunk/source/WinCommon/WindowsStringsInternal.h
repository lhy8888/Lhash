#ifndef _WINDOWS_STRINGS_INTERNAL_H_
#define _WINDOWS_STRINGS_INTERNAL_H_

#include <map>
#include <mutex>
#include <Windows.h>
#include <tchar.h>
#include "OsUtils/OsThread.h"
#include "WindowsStrings.h"

namespace WindowsStrings
{
	typedef std::map<LANGID, WindowsStringsMap *> LangStringsMap;

	class WindowsStringsMgr
	{
	public:
		virtual ~WindowsStringsMgr();

		static WindowsStringsMgr *getInstance(void);

		int registerStringsForLang(LANGID langId, WindowsStringsMap *stringsMap);

		const TCHAR *getStringForLang(LANGID langId, const TCHAR *tzhKey);

	private:
		WindowsStringsMgr();
		WindowsStringsMgr(const WindowsStringsMgr&) = delete;
		WindowsStringsMgr& operator=(const WindowsStringsMgr&) = delete;

		std::mutex m_mutex;

		LangStringsMap m_langStrsMap;
	};
}

#endif