#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	bool ExecuteScheduledHashRequestFiles(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState,
		ThreadPool *threadPool)
	{
		return VisitHashRequestFiles(request, [&](uint32_t fileIndex, const sunjwbase::tstring& fullPath)
		{
			return RunFileHashAttempt(executionContext, request, fileIndex, fullPath, isSizeCaled, fSizes, executionState, threadPool);
		});
	}
}
