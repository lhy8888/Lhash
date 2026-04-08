#ifndef _HASH_DIGEST_BUFFER_PLAN_H_
#define _HASH_DIGEST_BUFFER_PLAN_H_

#include "Common/HashDigestExecutionMode.h"
#include "Domain/HashRequest.h"

namespace HashEngineInternal
{
	struct HashDigestBufferPlan
	{
		unsigned int preferredBufferLength;
	};

	HashDigestBufferPlan CreateHashDigestBufferPlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode);
	unsigned int GetHashDigestBufferPreferredLength(const HashDigestBufferPlan& digestBufferPlan);
}

#endif
