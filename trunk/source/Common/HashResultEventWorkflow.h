#ifndef _HASH_RESULT_EVENT_WORKFLOW_H_
#define _HASH_RESULT_EVENT_WORKFLOW_H_

#include "Common/HashExecutionContext.h"
#include "Common/HashResult.h"

namespace HashEngineInternal
{
	void PublishMetaResultEvent(HashExecutionContext *executionContext, HashResult& result);
	void PublishHashResultEvent(HashExecutionContext *executionContext, HashResult& result, bool uppercase);
	void PublishErrorResultEvent(HashExecutionContext *executionContext, HashResult& result);
	void PublishFileFinishedEvent(HashExecutionContext *executionContext);
}

#endif
