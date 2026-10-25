#include "stdafx.h"

#include "WindowsStringsInternal.h"

#include <memory>

using namespace sunjwbase;

namespace WindowsStrings
{
	const TCHAR *WindowsStringsMap::getStringByKey(const TCHAR *tzhKey)
	{
		StringMap::iterator itr = m_stringsMap.find(tstring(tzhKey));
		if (itr == m_stringsMap.end())
			return NULL;

		return itr->second.c_str();
	}


	WindowsStringsMgr::WindowsStringsMgr()
	{
	}

	WindowsStringsMgr::~WindowsStringsMgr()
	{
		LangStringsMap::iterator itr = m_langStrsMap.begin();
		for (; itr != m_langStrsMap.end(); ++itr)
		{
			WindowsStringsMap *ptr = itr->second;
			delete ptr;
			itr->second = NULL;
		}
	}

	WindowsStringsMgr *WindowsStringsMgr::getInstance(void)
	{
		static WindowsStringsMgr instance;
		return &instance;
	}

	int WindowsStringsMgr::registerStringsForLang(LANGID langId, WindowsStringsMap *stringsMap)
	{
		if (stringsMap == NULL) return -1;
		std::unique_ptr<WindowsStringsMap> incoming(stringsMap);
		std::lock_guard<std::mutex> lock(m_mutex);
		const auto found = m_langStrsMap.find(langId);
		if (found != m_langStrsMap.end())
		{
			// Registered maps remain immutable for the process lifetime, so
			// previously returned string pointers stay valid across COM objects.
			if (found->second == stringsMap) incoming.release();
			return 0;
		}
		m_langStrsMap.emplace(langId, stringsMap);
		incoming.release();
		return 0;
	}

	const TCHAR *WindowsStringsMgr::getStringForLang(LANGID langId, const TCHAR *tzhKey)
	{
		std::lock_guard<std::mutex> lock(m_mutex);
		const TCHAR *tzhRet = NULL;

		// Find string in lang id.
		LangStringsMap::iterator itrLang = m_langStrsMap.find(langId);
		if (itrLang != m_langStrsMap.end())
		{
			WindowsStringsMap *langStringsMap = itrLang->second;
			tzhRet = langStringsMap->getStringByKey(tzhKey);
		}

		// Find string in base.
		itrLang = m_langStrsMap.find(-1);
		if (tzhRet == NULL && itrLang != m_langStrsMap.end())
		{
			WindowsStringsMap *baseStringsMap = itrLang->second;
			tzhRet = baseStringsMap->getStringByKey(tzhKey);
		}

		if (tzhRet == NULL)
		{
			tzhRet = tzhKey;
		}

		return tzhRet;
	}

}
