#ifndef _HASH_PRE_SCAN_SIZE_PROBE_H_
#define _HASH_PRE_SCAN_SIZE_PROBE_H_

#include "Common/Global.h"

namespace HashEngineInternal
{
	uint64_t ResolveHashPreScannedFileSize(const TCHAR *path);
}

#endif
