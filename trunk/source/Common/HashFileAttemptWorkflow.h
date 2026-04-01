#ifndef _HASH_FILE_ATTEMPT_WORKFLOW_H_
#define _HASH_FILE_ATTEMPT_WORKFLOW_H_

#include "Common/HashExecutionContext.h"
#include "Common/HashRequest.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
class ThreadPool;
#endif

namespace HashEngineInternal
{
	struct FileExecutionState;

	bool ExecuteFileHashAttemptWorkflow(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const sunjwbase::tstring& fullPath,
		bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		, ThreadPool *threadPool
#endif
	);
}

#endif
