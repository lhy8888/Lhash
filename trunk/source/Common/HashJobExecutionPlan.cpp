#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void InitializeHashJobExecutionPlan(const HashRequest& request, HashJobExecutionPlan *executionPlan)
	{
		executionPlan->digestUpdateRequest = CreateDigestUpdateRequest(request);
		executionPlan->digestExecutionMode = ResolveHashDigestExecutionMode(request);
		executionPlan->digestQueuePlan = CreateHashDigestQueuePlan(request, executionPlan->digestExecutionMode);
		executionPlan->schedulerPlan = CreateHashSchedulerPlan(request, executionPlan->digestExecutionMode);
	}

	const DigestUpdateRequest& GetHashJobDigestUpdateRequest(const HashJobExecutionPlan& executionPlan)
	{
		return executionPlan.digestUpdateRequest;
	}

	HashDigestExecutionMode GetHashJobDigestExecutionMode(const HashJobExecutionPlan& executionPlan)
	{
		return executionPlan.digestExecutionMode;
	}

	const HashDigestQueuePlan& GetHashJobDigestQueuePlan(const HashJobExecutionPlan& executionPlan)
	{
		return executionPlan.digestQueuePlan;
	}

	const HashSchedulerPlan& GetHashJobSchedulerPlan(const HashJobExecutionPlan& executionPlan)
	{
		return executionPlan.schedulerPlan;
	}
}
