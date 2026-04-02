#include "stdafx.h"

#include "Common/HashPreparationPlan.h"

namespace HashEngineInternal
{
	HashPreparationPlan CreateHashPreparationPlan(const HashRequest& request)
	{
		(void)request;

		HashPreparationPlan preparationPlan = { 0 };
		preparationPlan.preScanFileCountThreshold = 200;
		return preparationPlan;
	}

	bool ShouldPreScanHashRequestFileSizes(const HashPreparationPlan& preparationPlan, const HashRequest& request)
	{
		if (preparationPlan.preScanFileCountThreshold == 0)
		{
			return false;
		}

		return GetHashRequestFileCount(request) < preparationPlan.preScanFileCountThreshold;
	}
}
