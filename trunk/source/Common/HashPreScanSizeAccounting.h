#ifndef _HASH_PRE_SCAN_SIZE_ACCOUNTING_H_
#define _HASH_PRE_SCAN_SIZE_ACCOUNTING_H_

#include "Runtime/HashExecutionContext.h"

namespace HashEngineInternal
{
	uint64_t TrackHashPreScannedFileSize(HashExecutionContext *executionContext, ULLongVector& fSizes, uint32_t fileIndex, uint64_t fSize);
}

#endif
