#ifndef _HASH_JOB_EXECUTION_PLAN_H_
#define _HASH_JOB_EXECUTION_PLAN_H_

#include "Common/HashDigestExecutionMode.h"
#include "Common/HashDigestUpdater.h"
#include "Common/HashRequest.h"

namespace HashEngineInternal
{
	struct HashJobExecutionPlan
	{
		DigestUpdateRequest digestUpdateRequest;
		HashDigestExecutionMode digestExecutionMode;
	};

	void InitializeHashJobExecutionPlan(const HashRequest& request, HashJobExecutionPlan *executionPlan);
	const DigestUpdateRequest& GetHashJobDigestUpdateRequest(const HashJobExecutionPlan& executionPlan);
	HashDigestExecutionMode GetHashJobDigestExecutionMode(const HashJobExecutionPlan& executionPlan);
}

#endif
