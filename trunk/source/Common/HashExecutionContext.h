#ifndef _HASH_EXECUTION_CONTEXT_H_
#define _HASH_EXECUTION_CONTEXT_H_

#include "Common/Global.h"
#include "Common/HashProgressSink.h"

struct HashExecutionContext
{
	HashExecutionContext()
		: progressSink(NULL),
		workingFlag(NULL),
		stopRequestedFlag(NULL),
		countedSize(NULL),
		results(NULL)
	{
	}

	HashProgressSink *progressSink;
	bool *workingFlag;
	bool *stopRequestedFlag;
	uint64_t *countedSize;
	ResultList *results;
};

static inline HashExecutionContext CreateHashExecutionContext(ThreadData& threadData)
{
	HashExecutionContext executionContext;
	executionContext.progressSink = threadData.observer;
	executionContext.workingFlag = &threadData.executionState.working;
	executionContext.stopRequestedFlag = &threadData.executionState.stopRequested;
	executionContext.countedSize = &threadData.executionState.countedSize;
	executionContext.results = &threadData.executionState.results;
	return executionContext;
}

static inline HashProgressSink *GetHashExecutionProgressSink(const HashExecutionContext& executionContext)
{
	return executionContext.progressSink;
}

static inline void SetHashExecutionWorking(HashExecutionContext& executionContext, bool working)
{
	*executionContext.workingFlag = working;
}

static inline bool IsHashExecutionWorking(const HashExecutionContext& executionContext)
{
	return *executionContext.workingFlag;
}

static inline bool ShouldStopHashExecution(const HashExecutionContext& executionContext)
{
	return *executionContext.stopRequestedFlag;
}

static inline uint64_t GetHashExecutionTotalSize(const HashExecutionContext& executionContext)
{
	return *executionContext.countedSize;
}

static inline void ResetHashExecutionTotalSize(HashExecutionContext& executionContext)
{
	*executionContext.countedSize = 0;
}

static inline void AddHashExecutionTotalSize(HashExecutionContext& executionContext, uint64_t sizeDelta)
{
	*executionContext.countedSize += sizeDelta;
}

static inline void ReplaceHashExecutionCountedFileSize(HashExecutionContext& executionContext, uint64_t previousSize, uint64_t currentSize)
{
	*executionContext.countedSize = GetHashExecutionTotalSize(executionContext) + currentSize - previousSize;
}

static inline ResultData& AppendHashExecutionResult(HashExecutionContext& executionContext)
{
	ResultData resultNew;
	executionContext.results->push_back(resultNew);
	return executionContext.results->back();
}

#endif
