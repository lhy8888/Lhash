#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	bool ExecuteOpenedFileDigestUpdate(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fsize, bool isSizeCaled,
		FileExecutionState *executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		, ThreadPool *threadPool
#endif
	)
	{
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		return ProcessOpenedFileHashingParallel(executionContext, digestUpdateRequest, fsize, isSizeCaled, executionState, threadPool);
#else
		return ProcessOpenedFileHashingSinglePass(executionContext, digestUpdateRequest, fsize, isSizeCaled, executionState);
#endif
	}
}
