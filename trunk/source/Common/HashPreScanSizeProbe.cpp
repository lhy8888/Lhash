#include "stdafx.h"

#include "Common/HashPreScanSizeProbe.h"
#include "OsUtils/OsFile.h"

namespace HashEngineInternal
{
	int64_t ResolveHashPreScannedFileSize(const TCHAR *path)
	{
		if (path == NULL || path[0] == _T('\0'))
		{
			return -1;
		}

		sunjwbase::OsFile osFile(path);
		if (!osFile.openReadScan())
		{
			return -1;
		}

		int64_t fileLength = osFile.getLength();
		osFile.close();

		return fileLength;
	}
}
