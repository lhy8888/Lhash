#include "stdafx.h"

#include "Common/HashDigestExecutionMode.h"

namespace HashEngineInternal
{
	HashDigestExecutionMode ResolveHashDigestExecutionMode(const HashRequest& request)
	{
		(void)request;
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		return HASH_DIGEST_EXECUTION_MODE_PARALLEL;
#else
		return HASH_DIGEST_EXECUTION_MODE_SINGLE_PASS;
#endif
	}

	bool IsParallelHashDigestExecutionMode(HashDigestExecutionMode executionMode)
	{
		return executionMode == HASH_DIGEST_EXECUTION_MODE_PARALLEL;
	}
}
