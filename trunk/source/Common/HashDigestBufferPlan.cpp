#include "stdafx.h"

#include "Common/HashDigestBufferPlan.h"

namespace HashEngineInternal
{
	HashDigestBufferPlan CreateDefaultHashDigestBufferPlan()
	{
		HashDigestBufferPlan digestBufferPlan = { 0 };
		digestBufferPlan.preferredBufferLength = kDefaultHashBufferLength;
		return digestBufferPlan;
	}

	unsigned int GetHashDigestBufferPreferredLength(const HashDigestBufferPlan& digestBufferPlan)
	{
		if (digestBufferPlan.preferredBufferLength == 0)
		{
			return kDefaultHashBufferLength;
		}

		return digestBufferPlan.preferredBufferLength;
	}
}
