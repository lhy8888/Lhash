#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	uint64_t TrackHashPreScannedFileSize(HashExecutionContext *executionContext, ULLongVector& fSizes, uint32_t fileIndex, uint64_t fSize)
	{
		fSizes[fileIndex] = fSize;
		AddHashExecutionTotalSize(*executionContext, fSize);
		return fSize;
	}
}
