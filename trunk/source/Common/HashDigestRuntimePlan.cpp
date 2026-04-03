#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	HashDigestRuntimePlan CreateHashDigestRuntimePlan(const HashJobExecutionPlan& executionPlan)
	{
		return HashDigestRuntimePlan(
			GetHashJobDigestUpdateRequest(executionPlan),
			GetHashJobDigestExecutionMode(executionPlan),
			GetHashDigestBufferPreferredLength(GetHashJobDigestBufferPlan(executionPlan)),
			GetHashJobDigestQueuePlan(executionPlan));
	}

	const DigestUpdateRequest& GetHashDigestRuntimeUpdateRequest(const HashDigestRuntimePlan& digestRuntimePlan)
	{
		return digestRuntimePlan.digestUpdateRequest;
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
		return digestRuntimePlan.digestQueuePlan;
	}
}
