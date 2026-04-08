#ifndef _HASH_JOB_LIFECYCLE_WORKFLOW_H_
#define _HASH_JOB_LIFECYCLE_WORKFLOW_H_

#include "Runtime/HashExecutionContext.h"
#include "Runtime/HashProgressSink.h"

namespace HashEngineInternal
{
	void ExecuteCancelledHashingWorkflow(HashExecutionContext *executionContext, HashProgressSink *observer);
	void ExecuteCompletedHashingWorkflow(HashExecutionContext *executionContext, HashProgressSink *observer);
}

#endif
