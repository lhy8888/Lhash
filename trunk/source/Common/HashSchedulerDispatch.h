#ifndef _HASH_SCHEDULER_DISPATCH_H_
#define _HASH_SCHEDULER_DISPATCH_H_

#include "Common/HashExecutionContext.h"
#include "Common/HashRequest.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
class ThreadPool;
#endif

namespace HashEngineInternal
{
	struct FileExecutionState;

	bool ExecuteScheduledHashRequestFiles(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		, ThreadPool *threadPool
#endif
	);
}

#endif
