#include "stdafx.h"

#include "Common/HashFileVersionResolver.h"

#if defined (_WIN32)
#include "WinCommon/WindowsComm.h"
#if (defined (FHASH_UWP_LIB) || defined(FHASH_WUI_LIB))
#include "WinCommon/FileVersionHelper.h"
#endif
#endif

namespace HashEngineInternal
{
	sunjwbase::tstring ResolveHashFileVersion(sunjwbase::OsFile& osFile, const TCHAR *path)
	{
#if defined (_WIN32)
#if (defined (FHASH_UWP_LIB) || defined(FHASH_WUI_LIB))
		WindowsComm::FileVersionHelper fvHelper(osFile);
		sunjwbase::tstring fileVersion = fvHelper.Find();
		osFile.seek(0, sunjwbase::OsFile::OsFileSeekFrom::OF_SEEK_BEGIN);
		return fileVersion;
#else
		return WindowsComm::GetExeFileVersion((TCHAR *)path);
#endif
#else
		(void)osFile;
		(void)path;
		return sunjwbase::tstring();
#endif
	}
}
