#include "stdafx.h"

#include "Common/HashDigestExecutionMode.h"

namespace HashEngineInternal
{
	HashDigestExecutionMode ResolveHashDigestExecutionMode(const HashRequest& request)
	{
		HashRequestDigestExecutionPolicy executionPolicy = GetHashRequestDigestExecutionPolicy(request);
		if (executionPolicy == HASH_REQUEST_DIGEST_EXECUTION_POLICY_SINGLE_THREADED)
		{
			return HASH_DIGEST_EXECUTION_MODE_SINGLE_PASS;
		}

		if (executionPolicy == HASH_REQUEST_DIGEST_EXECUTION_POLICY_PARALLEL)
		{
			return HASH_DIGEST_EXECUTION_MODE_PARALLEL;
		}

		return HASH_DIGEST_EXECUTION_MODE_PARALLEL;
	}

	bool IsParallelHashDigestExecutionMode(HashDigestExecutionMode executionMode)
	{
		return executionMode == HASH_DIGEST_EXECUTION_MODE_PARALLEL;
	}
}
