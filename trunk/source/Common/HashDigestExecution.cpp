#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	bool ExecuteOpenedFileDigestUpdate(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, HashDigestExecutionMode digestExecutionMode, unsigned int preferredBufferLength, const HashDigestQueuePlan& digestQueuePlan, uint64_t fsize, bool isSizeCaled,
		FileExecutionState *executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		, ThreadPool *threadPool
#endif
	)
	{
#if defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		(void)digestQueuePlan;
#endif
		if (IsParallelHashDigestExecutionMode(digestExecutionMode))
		{
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
			return ProcessOpenedFileHashingParallel(executionContext, digestUpdateRequest, fsize, isSizeCaled, preferredBufferLength, digestQueuePlan, executionState, threadPool);
#else
			return ProcessOpenedFileHashingSinglePass(executionContext, digestUpdateRequest, fsize, isSizeCaled, preferredBufferLength, executionState);
#endif
		}

		return ProcessOpenedFileHashingSinglePass(executionContext, digestUpdateRequest, fsize, isSizeCaled, preferredBufferLength, executionState);
	}
}
