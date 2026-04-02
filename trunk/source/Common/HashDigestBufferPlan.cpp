#include "stdafx.h"

#include "Common/HashDigestBufferPlan.h"

namespace HashEngineInternal
{
	HashDigestBufferPlan CreateHashDigestBufferPlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode)
	{
		(void)request;
		(void)digestExecutionMode;

		HashDigestBufferPlan digestBufferPlan = { 0 };
		digestBufferPlan.preferredBufferLength = 1048576; // 2^20
		return digestBufferPlan;
	}

	unsigned int GetHashDigestBufferPreferredLength(const HashDigestBufferPlan& digestBufferPlan)
	{
		if (digestBufferPlan.preferredBufferLength == 0)
		{
			return 1048576; // 2^20
		}

		return digestBufferPlan.preferredBufferLength;
	}
}
