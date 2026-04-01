#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void InitializeHashJobExecutionPlan(const HashRequest& request, HashJobExecutionPlan *executionPlan)
	{
		executionPlan->digestUpdateRequest = CreateDigestUpdateRequest(request);
	}

	const DigestUpdateRequest& GetHashJobDigestUpdateRequest(const HashJobExecutionPlan& executionPlan)
	{
		return executionPlan.digestUpdateRequest;
	}
}
