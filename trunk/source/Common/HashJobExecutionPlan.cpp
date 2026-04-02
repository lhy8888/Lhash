#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void InitializeHashJobExecutionPlan(const HashRequest& request, HashJobExecutionPlan *executionPlan)
	{
		executionPlan->digestUpdateRequest = CreateDigestUpdateRequest(request);
		executionPlan->digestExecutionMode = ResolveHashDigestExecutionMode(request);
		executionPlan->digestBufferPlan = CreateHashDigestBufferPlan(request, executionPlan->digestExecutionMode);
		executionPlan->digestQueuePlan = CreateHashDigestQueuePlan(request, executionPlan->digestExecutionMode);
		executionPlan->preparationPlan = CreateHashPreparationPlan(request);
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

	const HashDigestBufferPlan& GetHashJobDigestBufferPlan(const HashJobExecutionPlan& executionPlan)
	{
		return executionPlan.digestBufferPlan;
	}

	const HashDigestQueuePlan& GetHashJobDigestQueuePlan(const HashJobExecutionPlan& executionPlan)
	{
		return executionPlan.digestQueuePlan;
	}

	const HashPreparationPlan& GetHashJobPreparationPlan(const HashJobExecutionPlan& executionPlan)
	{
		return executionPlan.preparationPlan;
	}

	const HashSchedulerPlan& GetHashJobSchedulerPlan(const HashJobExecutionPlan& executionPlan)
	{
		return executionPlan.schedulerPlan;
	}
}
