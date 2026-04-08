#ifndef _HASH_PRE_SCAN_WORKFLOW_H_
#define _HASH_PRE_SCAN_WORKFLOW_H_

#include "Runtime/HashExecutionContext.h"
#include "Domain/HashRequest.h"

namespace HashEngineInternal
{
	void RunHashPreScanVisitWorkflow(HashExecutionContext *executionContext, const HashRequest& request, ULLongVector& fSizes, bool *wasCancelled);
}

#endif
