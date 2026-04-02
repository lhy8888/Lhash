#ifndef _HASH_FILE_SIZE_ACCOUNTING_H_
#define _HASH_FILE_SIZE_ACCOUNTING_H_

#include "Common/HashExecutionContext.h"
#include "Common/HashResult.h"

namespace sunjwbase
{
	class OsFile;
}

namespace HashEngineInternal
{
	uint64_t ResolveHashFileSizeAndTrack(HashExecutionContext *executionContext, sunjwbase::OsFile& osFile, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, HashResult& result);
}

#endif
