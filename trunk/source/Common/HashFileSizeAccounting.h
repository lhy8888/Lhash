#ifndef _HASH_FILE_SIZE_ACCOUNTING_H_
#define _HASH_FILE_SIZE_ACCOUNTING_H_

#include "Runtime/HashExecutionContext.h"
#include "Domain/HashResult.h"

namespace sunjwbase
{
	class OsFile;
}

namespace HashEngineInternal
{
	uint64_t TrackHashResolvedFileSize(HashExecutionContext *executionContext, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, HashResult& result, uint64_t fsize);
	uint64_t ResolveHashFileSizeAndTrack(HashExecutionContext *executionContext, sunjwbase::OsFile& osFile, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, HashResult& result);
}

#endif
