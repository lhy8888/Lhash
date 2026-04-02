#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	uint64_t ResolveHashFileSizeAndTrack(HashExecutionContext *executionContext, sunjwbase::OsFile& osFile, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, HashResult& result)
	{
		uint64_t fsize = osFile.getLength();
		result.meta.size = fsize;

		if (!isSizeCaled)
		{
			AddHashExecutionTotalSize(*executionContext, fsize);
		}
		else
		{
			ReplaceHashExecutionCountedFileSize(*executionContext, fSizes[fileIndex], fsize);
			fSizes[fileIndex] = fsize;
		}

		return fsize;
	}
}
