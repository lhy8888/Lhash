#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	bool ExecuteHashPreparationWorkflow(HashExecutionContext *executionContext, const HashRequest& request, const HashPreparationPlan& preparationPlan, ULLongVector& fSizes, bool *wasCancelled)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		observer->onProgressEvent(CreatePreparingProgressEvent());
		bool isSizeCaled = TryPreScanSmallBatchFileSizes(executionContext, request, preparationPlan, fSizes, wasCancelled);
		if (*wasCancelled)
		{
			return isSizeCaled;
		}

		observer->onProgressEvent(CreatePreparationFinishedProgressEvent());
		return isSizeCaled;
	}
}
