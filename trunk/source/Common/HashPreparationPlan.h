#ifndef _HASH_PREPARATION_PLAN_H_
#define _HASH_PREPARATION_PLAN_H_

#include <stddef.h>

#include "Common/HashRequest.h"

namespace HashEngineInternal
{
	struct HashPreparationPlan
	{
		size_t preScanFileCountThreshold;
	};

	HashPreparationPlan CreateHashPreparationPlan(const HashRequest& request);
	bool ShouldPreScanHashRequestFileSizes(const HashPreparationPlan& preparationPlan, const HashRequest& request);
}

#endif
