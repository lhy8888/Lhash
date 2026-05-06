#include "stdafx.h"

#include "Common/HashFileVersionResolver.h"

#if defined (_WIN32)
#include "WinCommon/FileVersionHelper.h"
#endif

namespace HashEngineInternal
{
	static bool ShouldResolveWindowsFileVersion(const TCHAR *path)
	{
		if (path == NULL || path[0] == _T('\0'))
		{
			return false;
		}

		sunjwbase::tstring fullPath(path);
		size_t extensionPos = fullPath.find_last_of(_T('.'));
		if (extensionPos == sunjwbase::tstring::npos)
		{
			return false;
		}

		sunjwbase::tstring extension = fullPath.substr(extensionPos);
		std::string normalizedExtension = sunjwbase::str_lower(sunjwbase::tstrtostr(extension));
		return normalizedExtension == ".exe" ||
			normalizedExtension == ".dll" ||
			normalizedExtension == ".sys" ||
			normalizedExtension == ".ocx";
	}

	sunjwbase::tstring ResolveHashFileVersion(sunjwbase::OsFile& osFile, const TCHAR *path)
	{
#if defined (_WIN32)
		if (!ShouldResolveWindowsFileVersion(path))
		{
			return sunjwbase::tstring();
		}

		WindowsComm::FileVersionHelper fileVersionHelper(osFile);
		return fileVersionHelper.Find();
#else
		(void)osFile;
		(void)path;
		return sunjwbase::tstring();
#endif
	}
}

