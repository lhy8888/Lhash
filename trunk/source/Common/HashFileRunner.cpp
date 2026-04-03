#include "stdafx.h"

#include "Common/HashEngineInternal.h"

using namespace sunjwbase;

namespace HashEngineInternal
{
	bool RunFileHashAttempt(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const tstring& fullPath, bool isSizeCaled, ULLongVector& fSizes,
		FileExecutionState *executionState, ThreadPool *threadPool)
	{
		return ExecuteFileHashAttemptWorkflow(executionContext, request, fileIndex, fullPath, isSizeCaled, fSizes, executionState, threadPool);
	}
}
