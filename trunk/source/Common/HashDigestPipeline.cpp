#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	bool ProcessOpenedFileHashing(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex,
		bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState, ThreadPool *threadPool)
	{
		InitializeFileHashing(request, executionContext, &executionState->hashContexts);
		HashDigestRuntimePlan digestRuntimePlan = CreateHashDigestRuntimePlan(executionState->executionPlan);
		uint64_t fsize = PrepareFileMetaResult(executionContext, result, *executionState->fileAttemptState.osFile, executionState->fileAttemptState.path,
			isSizeCaled, fSizes, fileIndex);
		executionState->progressState.fileSizeKnown = result.meta.sizeKnown;

		bool wasStopped = ExecuteOpenedFileDigestUpdate(executionContext, digestRuntimePlan, fsize, isSizeCaled, executionState, threadPool);
		if (CompleteOpenedFileDigestExecution(executionContext, executionState, wasStopped))
		{
			return true;
		}

		return false;
	}
}
