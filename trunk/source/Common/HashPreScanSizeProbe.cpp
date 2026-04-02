#include "stdafx.h"

#include "Common/HashPreScanSizeProbe.h"

#include "OsUtils/OsFile.h"

namespace HashEngineInternal
{
	uint64_t ResolveHashPreScannedFileSize(const TCHAR *path)
	{
		uint64_t fSize = 0;

		sunjwbase::OsFile osFile(path);
		if (osFile.openRead())
		{
			fSize = osFile.getLength();
			osFile.close();
		}

		return fSize;
	}
}
