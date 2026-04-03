#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	bool ExecuteOpenedFileDigestUpdate(HashExecutionContext *executionContext, const HashDigestRuntimePlan& digestRuntimePlan, uint64_t fsize, bool isSizeCaled,
		FileExecutionState *executionState, ThreadPool *threadPool)
	{
		const DigestUpdateRequest& digestUpdateRequest = GetHashDigestRuntimeUpdateRequest(digestRuntimePlan);
		HashDigestExecutionMode digestExecutionMode = GetHashDigestRuntimeExecutionMode(digestRuntimePlan);
		unsigned int preferredBufferLength = GetHashDigestRuntimePreferredBufferLength(digestRuntimePlan);
		const HashDigestQueuePlan& digestQueuePlan = GetHashDigestRuntimeQueuePlan(digestRuntimePlan);

		if (IsParallelHashDigestExecutionMode(digestExecutionMode))
		{
			return ProcessOpenedFileHashingParallel(executionContext, digestUpdateRequest, fsize, isSizeCaled, preferredBufferLength, digestQueuePlan, executionState, threadPool);
		}

		return ProcessOpenedFileHashingSinglePass(executionContext, digestUpdateRequest, fsize, isSizeCaled, preferredBufferLength, executionState);
	}
}
