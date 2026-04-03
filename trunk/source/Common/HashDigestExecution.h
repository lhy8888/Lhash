#ifndef _HASH_DIGEST_EXECUTION_H_
#define _HASH_DIGEST_EXECUTION_H_

#include "Common/HashExecutionContext.h"
#include "Common/HashDigestExecutionMode.h"
#include "Common/HashDigestQueuePlan.h"
#include "Common/HashDigestRuntimePlan.h"
#include "Common/HashDigestUpdater.h"

class ThreadPool;

namespace HashEngineInternal
{
	struct FileExecutionState;

	bool ExecuteOpenedFileDigestUpdate(HashExecutionContext *executionContext, const HashDigestRuntimePlan& digestRuntimePlan, uint64_t fsize, bool isSizeCaled,
		FileExecutionState *executionState, ThreadPool *threadPool);
}

#endif
