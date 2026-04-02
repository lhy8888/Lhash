#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	HashDigestRuntimePlan CreateHashDigestRuntimePlan(const HashJobExecutionPlan& executionPlan)
	{
		HashDigestRuntimePlan digestRuntimePlan = { 0 };
		digestRuntimePlan.digestUpdateRequest = &GetHashJobDigestUpdateRequest(executionPlan);
		digestRuntimePlan.digestExecutionMode = GetHashJobDigestExecutionMode(executionPlan);
		digestRuntimePlan.preferredBufferLength = GetHashDigestBufferPreferredLength(GetHashJobDigestBufferPlan(executionPlan));
		digestRuntimePlan.digestQueuePlan = &GetHashJobDigestQueuePlan(executionPlan);
		return digestRuntimePlan;
	}

	const DigestUpdateRequest& GetHashDigestRuntimeUpdateRequest(const HashDigestRuntimePlan& digestRuntimePlan)
	{
		static const DigestUpdateRequest emptyDigestUpdateRequest = { 0 };
		if (digestRuntimePlan.digestUpdateRequest == NULL)
		{
			return emptyDigestUpdateRequest;
		}

		return *digestRuntimePlan.digestUpdateRequest;
	}

	HashDigestExecutionMode GetHashDigestRuntimeExecutionMode(const HashDigestRuntimePlan& digestRuntimePlan)
	{
		return digestRuntimePlan.digestExecutionMode;
	}

	unsigned int GetHashDigestRuntimePreferredBufferLength(const HashDigestRuntimePlan& digestRuntimePlan)
	{
		return digestRuntimePlan.preferredBufferLength;
	}

	const HashDigestQueuePlan& GetHashDigestRuntimeQueuePlan(const HashDigestRuntimePlan& digestRuntimePlan)
	{
		static const HashDigestQueuePlan fallbackQueuePlan = { 1 };
		if (digestRuntimePlan.digestQueuePlan == NULL)
		{
			return fallbackQueuePlan;
		}

		return *digestRuntimePlan.digestQueuePlan;
	}
}
