#ifndef _HASH_FILE_ATTEMPT_WORKFLOW_H_
#define _HASH_FILE_ATTEMPT_WORKFLOW_H_

#include "Common/HashExecutionContext.h"
#include "Common/HashRequest.h"

class ThreadPool;

namespace HashEngineInternal
{
	struct FileExecutionState;

	bool ExecuteFileHashAttemptWorkflow(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const sunjwbase::tstring& fullPath,
		bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState, ThreadPool *threadPool);
}

#endif
