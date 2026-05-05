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

		uint64_t size = static_cast<uint64_t>(osFile.getLength());
		osFile.close();
		return size;
	}
}
