#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	uint64_t TrackHashResolvedFileSize(HashExecutionContext *executionContext, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, HashResult& result, uint64_t fsize)
	{
		result.meta.size = fsize;

		if (!isSizeCaled || fileIndex >= fSizes.size())
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

	uint64_t ResolveHashFileSizeAndTrack(HashExecutionContext *executionContext, sunjwbase::OsFile& osFile, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, HashResult& result)
	{
		return TrackHashResolvedFileSize(executionContext, isSizeCaled, fSizes, fileIndex, result, osFile.getLength());
	}
}
