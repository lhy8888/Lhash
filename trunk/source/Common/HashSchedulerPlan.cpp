#include "stdafx.h"

#include "Common/HashSchedulerPlan.h"

namespace HashEngineInternal
{
	HashSchedulerPlan CreateHashSchedulerPlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode)
	{
		(void)request;

		HashSchedulerPlan schedulerPlan = { 0 };
		if (IsParallelHashDigestExecutionMode(digestExecutionMode))
		{
			schedulerPlan.workerThreadCount = 5;
		}
		else
		{
			schedulerPlan.workerThreadCount = 1;
		}

		return schedulerPlan;
	}

	size_t GetHashSchedulerWorkerThreadCount(const HashSchedulerPlan& schedulerPlan)
	{
		if (schedulerPlan.workerThreadCount == 0)
		{
			return 1;
		}

		return schedulerPlan.workerThreadCount;
	}
}
