#include "stdafx.h"

#include "Common/HashEngineInternal.h"
#include "Common/ThreadPool.h"

using namespace sunjwbase;

namespace HashEngineInternal
{
	bool RunHashScheduler(HashExecutionContext *executionContext, const HashRequest& request, const HashJobExecutionPlan& executionPlan, bool isSizeCaled, ULLongVector& fSizes)
	{
		FileExecutionState executionState;
		executionState.executionPlan = executionPlan;

		const HashSchedulerPlan& schedulerPlan = GetHashJobSchedulerPlan(executionState.executionPlan);
		ThreadPool threadPool(GetHashSchedulerWorkerThreadCount(schedulerPlan));
		return ExecuteScheduledHashRequestFiles(executionContext, request, isSizeCaled, fSizes, &executionState, &threadPool);
	}
}
