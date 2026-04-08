#ifndef _HASH_SUCCESSFUL_FILE_COMPLETION_WORKFLOW_H_
#define _HASH_SUCCESSFUL_FILE_COMPLETION_WORKFLOW_H_

#include "Runtime/HashExecutionContext.h"
#include "Domain/HashRequest.h"
#include "Common/HashResult.h"

namespace HashEngineInternal
{
	struct FileExecutionState;

	void PublishWholeProgressAfterFile(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, uint32_t fileIndex);
	void ExecuteSuccessfulFileHashingWorkflow(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState);
}

#endif
