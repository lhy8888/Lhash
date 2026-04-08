#ifndef _HASH_PREPARATION_WORKFLOW_H_
#define _HASH_PREPARATION_WORKFLOW_H_

#include "Runtime/HashExecutionContext.h"
#include "Common/HashPreparationPlan.h"
#include "Domain/HashRequest.h"

namespace HashEngineInternal
{
	bool ExecuteHashPreparationWorkflow(HashExecutionContext *executionContext, const HashRequest& request, const HashPreparationPlan& preparationPlan, ULLongVector& fSizes, bool *wasCancelled);
}

#endif
