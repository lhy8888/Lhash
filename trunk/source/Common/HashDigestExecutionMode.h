#ifndef _HASH_DIGEST_EXECUTION_MODE_H_
#define _HASH_DIGEST_EXECUTION_MODE_H_

#include "Domain/HashRequest.h"

namespace HashEngineInternal
{
	enum HashDigestExecutionMode
	{
		HASH_DIGEST_EXECUTION_MODE_SINGLE_PASS = 0,
		HASH_DIGEST_EXECUTION_MODE_PARALLEL
	};

	HashDigestExecutionMode ResolveHashDigestExecutionMode(const HashRequest& request);
	bool IsParallelHashDigestExecutionMode(HashDigestExecutionMode executionMode);
}

#endif
