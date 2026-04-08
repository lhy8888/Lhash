#ifndef _HASH_FILE_ATTEMPT_COMPLETION_WORKFLOW_H_
#define _HASH_FILE_ATTEMPT_COMPLETION_WORKFLOW_H_

#include "Runtime/HashExecutionContext.h"
#include "Domain/HashRequest.h"
#include "Common/HashResult.h"

namespace HashEngineInternal
{
	struct FileExecutionState;

	void ExecuteOpenedFileAttemptCompletionWorkflow(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState);
	void ExecuteFileAttemptCompletionWorkflow(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState);
}

#endif
