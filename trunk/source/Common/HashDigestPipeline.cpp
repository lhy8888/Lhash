#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	bool ProcessOpenedFileHashing(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex,
		bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		, ThreadPool *threadPool
#endif
	)
	{
		InitializeFileHashing(request, executionContext, &executionState->hashContexts);
		const DigestUpdateRequest& digestUpdateRequest = GetHashJobDigestUpdateRequest(executionState->executionPlan);
		HashDigestExecutionMode digestExecutionMode = GetHashJobDigestExecutionMode(executionState->executionPlan);
		const HashDigestBufferPlan& digestBufferPlan = GetHashJobDigestBufferPlan(executionState->executionPlan);
		const HashDigestQueuePlan& digestQueuePlan = GetHashJobDigestQueuePlan(executionState->executionPlan);
		SetDigestDataBufferPreferredLength(GetHashDigestBufferPreferredLength(digestBufferPlan));

		uint64_t fsize = PrepareFileMetaResult(executionContext, result, *executionState->fileAttemptState.osFile, executionState->fileAttemptState.path,
			isSizeCaled, fSizes, fileIndex, executionState->fileAttemptState.fileVersion);
		uint64_t times = CalculateFileChunkIterations(fsize);
		(void)times;

		bool wasStopped = ExecuteOpenedFileDigestUpdate(executionContext, digestUpdateRequest, digestExecutionMode, digestQueuePlan, fsize, isSizeCaled, executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
			, threadPool
#endif
		);
		if (wasStopped)
		{
			executionState->fileAttemptState.osFile->close();
			return true;
		}

		if (ShouldStopHashExecution(*executionContext))
		{
			executionState->fileAttemptState.osFile->close();
			return true;
		}

		return false;
	}
}
