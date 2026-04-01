#ifndef _HASH_DIGEST_QUEUE_PLAN_H_
#define _HASH_DIGEST_QUEUE_PLAN_H_

#include <stddef.h>

#include "Common/HashDigestExecutionMode.h"
#include "Common/HashRequest.h"

namespace HashEngineInternal
{
	struct HashDigestQueuePlan
	{
		size_t maxBufferedChunkCount;
	};

	HashDigestQueuePlan CreateHashDigestQueuePlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode);
	size_t GetHashDigestQueueMaxBufferedChunkCount(const HashDigestQueuePlan& digestQueuePlan);
}

#endif
