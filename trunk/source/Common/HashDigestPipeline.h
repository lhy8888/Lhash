#ifndef _HASH_DIGEST_PIPELINE_H_
#define _HASH_DIGEST_PIPELINE_H_

#include "Common/HashExecutionContext.h"
#include "Common/HashRequest.h"
#include "Common/HashResult.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
class ThreadPool;
#endif

namespace HashEngineInternal
{
	struct FileExecutionState;

	uint64_t CalculateFileChunkIterations(uint64_t fsize, unsigned int preferredBufferLength);

	bool ProcessOpenedFileHashing(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex,
		bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		, ThreadPool *threadPool
#endif
	);
}

#endif
