#include "stdafx.h"

#include "Common/HashEngineInternal.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
#include "Common/ThreadPool.h"
#endif

using namespace sunjwbase;

namespace HashEngineInternal
{
	bool RunHashScheduler(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, ULLongVector& fSizes)
	{
		FileExecutionState executionState = { 0 };
		InitializeHashJobExecutionPlan(request, &executionState.executionPlan);

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		ThreadPool threadPool(5);
		return ExecuteScheduledHashRequestFiles(executionContext, request, isSizeCaled, fSizes, &executionState, &threadPool);
#else
		return ExecuteScheduledHashRequestFiles(executionContext, request, isSizeCaled, fSizes, &executionState);
#endif
	}
}
