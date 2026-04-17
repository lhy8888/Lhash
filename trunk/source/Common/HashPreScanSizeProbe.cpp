#include "stdafx.h"

#include "Common/HashPreScanSizeProbe.h"

#if defined (_WIN32)
#include <Windows.h>
#else
#include "OsUtils/OsFile.h"
#endif

namespace HashEngineInternal
{
	uint64_t ResolveHashPreScannedFileSize(const TCHAR *path)
	{
		if (path == NULL || path[0] == _T('\0'))
		{
			return 0;
		}

#if defined (_WIN32)
		WIN32_FILE_ATTRIBUTE_DATA fileAttributes = {};
		if (!GetFileAttributesEx(path, GetFileExInfoStandard, &fileAttributes))
		{
			return 0;
		}

		return (static_cast<uint64_t>(fileAttributes.nFileSizeHigh) << 32) |
			static_cast<uint64_t>(fileAttributes.nFileSizeLow);
#else
		uint64_t fSize = 0;
		sunjwbase::OsFile osFile(path);
		if (osFile.openRead())
		{
			fSize = osFile.getLength();
			osFile.close();
		}

		return fSize;
#endif
	}
}
