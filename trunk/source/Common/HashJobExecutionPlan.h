#ifndef _HASH_JOB_EXECUTION_PLAN_H_
#define _HASH_JOB_EXECUTION_PLAN_H_

#include "Common/HashDigestUpdater.h"
#include "Common/HashRequest.h"

namespace HashEngineInternal
{
	struct HashJobExecutionPlan
	{
		DigestUpdateRequest digestUpdateRequest;
	};

	void InitializeHashJobExecutionPlan(const HashRequest& request, HashJobExecutionPlan *executionPlan);
	const DigestUpdateRequest& GetHashJobDigestUpdateRequest(const HashJobExecutionPlan& executionPlan);
}

#endif
