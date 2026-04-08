#ifndef _HASH_SCHEDULER_DISPATCH_H_
#define _HASH_SCHEDULER_DISPATCH_H_

#include "Runtime/HashExecutionContext.h"
#include "Domain/HashRequest.h"

class ThreadPool;

namespace HashEngineInternal
{
	struct FileExecutionState;

	bool ExecuteScheduledHashRequestFiles(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState,
		ThreadPool *threadPool);
}

#endif
