#include "stdafx.h"

#include "Common/HashPreScanSizeProbe.h"
#include "OsUtils/OsFile.h"

namespace HashEngineInternal
{
	uint64_t ResolveHashPreScannedFileSize(const TCHAR *path)
	{
		if (path == NULL || path[0] == _T('\0'))
		{
			return 0;
		}

		sunjwbase::OsFile osFile(path);
		if (!osFile.openReadScan())
		{
			return 0;
		}

		int64_t fileLength = osFile.getLength();
		osFile.close();

		if (fileLength <= 0)
		{
			return 0;
		}

		return static_cast<uint64_t>(fileLength);
	}
}
