#include "stdafx.h"

#include "Common/HashDigestQueuePlan.h"

namespace HashEngineInternal
{
	HashDigestQueuePlan CreateHashDigestQueuePlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode)
	{
		(void)request;

		HashDigestQueuePlan digestQueuePlan = { 0 };
		if (IsParallelHashDigestExecutionMode(digestExecutionMode))
		{
			digestQueuePlan.maxBufferedChunkCount = 4;
		}
		else
		{
			digestQueuePlan.maxBufferedChunkCount = 1;
		}

		return digestQueuePlan;
	}

	size_t GetHashDigestQueueMaxBufferedChunkCount(const HashDigestQueuePlan& digestQueuePlan)
	{
		if (digestQueuePlan.maxBufferedChunkCount == 0)
		{
			return 1;
		}

		return digestQueuePlan.maxBufferedChunkCount;
	}
}
