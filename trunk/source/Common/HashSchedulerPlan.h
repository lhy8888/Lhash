#ifndef _HASH_SCHEDULER_PLAN_H_
#define _HASH_SCHEDULER_PLAN_H_

#include <stddef.h>

#include "Common/HashDigestExecutionMode.h"
#include "Common/HashRequest.h"

namespace HashEngineInternal
{
	struct HashSchedulerPlan
	{
		size_t workerThreadCount;
	};

	HashSchedulerPlan CreateHashSchedulerPlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode);
	size_t GetHashSchedulerWorkerThreadCount(const HashSchedulerPlan& schedulerPlan);
}

#endif
