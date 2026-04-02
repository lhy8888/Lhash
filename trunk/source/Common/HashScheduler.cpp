#include "stdafx.h"

#include "Common/HashEngineInternal.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
#include "Common/ThreadPool.h"
#endif

using namespace sunjwbase;

namespace HashEngineInternal
{
	bool RunHashScheduler(HashExecutionContext *executionContext, const HashRequest& request, const HashJobExecutionPlan& executionPlan, bool isSizeCaled, ULLongVector& fSizes)
	{
		FileExecutionState executionState = { 0 };
		executionState.executionPlan = executionPlan;

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		const HashSchedulerPlan& schedulerPlan = GetHashJobSchedulerPlan(executionState.executionPlan);
		ThreadPool threadPool(GetHashSchedulerWorkerThreadCount(schedulerPlan));
		return ExecuteScheduledHashRequestFiles(executionContext, request, isSizeCaled, fSizes, &executionState, &threadPool);
#else
		return ExecuteScheduledHashRequestFiles(executionContext, request, isSizeCaled, fSizes, &executionState);
#endif
	}
}
