#ifndef _HASH_JOB_EXECUTION_PLAN_H_
#define _HASH_JOB_EXECUTION_PLAN_H_

#include "Common/HashDigestExecutionMode.h"
#include "Common/HashDigestBufferPlan.h"
#include "Common/HashDigestQueuePlan.h"
#include "Common/HashPreparationPlan.h"
#include "Common/HashDigestUpdater.h"
#include "Domain/HashRequest.h"
#include "Common/HashSchedulerPlan.h"

namespace HashEngineInternal
{
	struct HashJobExecutionPlan
	{
		DigestUpdateRequest digestUpdateRequest;
		HashDigestExecutionMode digestExecutionMode;
		HashDigestBufferPlan digestBufferPlan;
		HashDigestQueuePlan digestQueuePlan;
		HashPreparationPlan preparationPlan;
		HashSchedulerPlan schedulerPlan;
	};

	void InitializeHashJobExecutionPlan(const HashRequest& request, HashJobExecutionPlan *executionPlan);
	const DigestUpdateRequest& GetHashJobDigestUpdateRequest(const HashJobExecutionPlan& executionPlan);
	HashDigestExecutionMode GetHashJobDigestExecutionMode(const HashJobExecutionPlan& executionPlan);
	const HashDigestBufferPlan& GetHashJobDigestBufferPlan(const HashJobExecutionPlan& executionPlan);
	const HashDigestQueuePlan& GetHashJobDigestQueuePlan(const HashJobExecutionPlan& executionPlan);
	const HashPreparationPlan& GetHashJobPreparationPlan(const HashJobExecutionPlan& executionPlan);
	const HashSchedulerPlan& GetHashJobSchedulerPlan(const HashJobExecutionPlan& executionPlan);
}

#endif
