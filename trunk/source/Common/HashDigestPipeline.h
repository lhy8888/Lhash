#ifndef _HASH_DIGEST_PIPELINE_H_
#define _HASH_DIGEST_PIPELINE_H_

#include "Runtime/HashExecutionContext.h"
#include "Domain/HashRequest.h"
#include "Common/HashResult.h"

class ThreadPool;

namespace HashEngineInternal
{
	struct FileExecutionState;

	uint64_t CalculateFileChunkIterations(uint64_t fsize, unsigned int preferredBufferLength);

	bool ProcessOpenedFileHashing(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex,
		bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState, ThreadPool *threadPool);
}

#endif
