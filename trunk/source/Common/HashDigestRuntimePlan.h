#ifndef _HASH_DIGEST_RUNTIME_PLAN_H_
#define _HASH_DIGEST_RUNTIME_PLAN_H_

#include "Common/HashDigestExecutionMode.h"
#include "Common/HashDigestQueuePlan.h"
#include "Common/HashDigestUpdater.h"

namespace HashEngineInternal
{
	struct HashJobExecutionPlan;

	struct HashDigestRuntimePlan
	{
		HashDigestRuntimePlan(const DigestUpdateRequest& updateRequest, HashDigestExecutionMode executionMode, unsigned int bufferLength, const HashDigestQueuePlan& queuePlan)
			: digestUpdateRequest(updateRequest),
			digestExecutionMode(executionMode),
			preferredBufferLength(bufferLength),
			digestQueuePlan(queuePlan)
		{
		}

		const DigestUpdateRequest& digestUpdateRequest;
		HashDigestExecutionMode digestExecutionMode;
		unsigned int preferredBufferLength;
		const HashDigestQueuePlan& digestQueuePlan;
	};

	HashDigestRuntimePlan CreateHashDigestRuntimePlan(const HashJobExecutionPlan& executionPlan);
	const DigestUpdateRequest& GetHashDigestRuntimeUpdateRequest(const HashDigestRuntimePlan& digestRuntimePlan);
	HashDigestExecutionMode GetHashDigestRuntimeExecutionMode(const HashDigestRuntimePlan& digestRuntimePlan);
	unsigned int GetHashDigestRuntimePreferredBufferLength(const HashDigestRuntimePlan& digestRuntimePlan);
	const HashDigestQueuePlan& GetHashDigestRuntimeQueuePlan(const HashDigestRuntimePlan& digestRuntimePlan);
}

#endif
