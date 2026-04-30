#include "stdafx.h"

#include "Common/HashFileVersionResolver.h"

#if defined (_WIN32)
#include "WinCommon/WindowsComm.h"
#if (defined (LHASH_UWP_LIB) || defined(LHASH_WUI_LIB))
#include "WinCommon/FileVersionHelper.h"
#endif
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
#if (defined (LHASH_UWP_LIB) || defined(LHASH_WUI_LIB))
		WindowsComm::FileVersionHelper fvHelper(osFile);
		sunjwbase::tstring fileVersion = fvHelper.Find();
		osFile.seek(0, sunjwbase::OsFile::OsFileSeekFrom::OF_SEEK_BEGIN);
		return fileVersion;
#else
		if (!ShouldResolveWindowsFileVersion(path))
		{
			return sunjwbase::tstring();
		}

		return WindowsComm::GetExeFileVersion((TCHAR *)path);
#endif
#else
		(void)osFile;
		(void)path;
		return sunjwbase::tstring();
#endif
	}
}

