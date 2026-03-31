#ifndef _HASH_EXECUTION_CONTEXT_H_
#define _HASH_EXECUTION_CONTEXT_H_
#include "Common/Global.h"
#include "Common/HashProgressSink.h"
struct HashExecutionContext
{
	HashExecutionContext()
		: progressSink(NULL),
		jobState(NULL),
		cancellationState(NULL)
	{
	}
	HashProgressSink *progressSink;
	HashJobState *jobState;
	HashCancellationState *cancellationState;
};
static inline HashExecutionContext CreateHashExecutionContext(ThreadData& threadData)
{
	HashExecutionContext executionContext;
	executionContext.progressSink = threadData.observer;
	executionContext.jobState = &threadData.executionState.jobState;
	executionContext.cancellationState = &threadData.executionState.cancellation;
	return executionContext;
}
static inline HashProgressSink *GetHashExecutionProgressSink(const HashExecutionContext& executionContext)
{
	return executionContext.progressSink;
}
static inline void SetHashExecutionWorking(HashExecutionContext& executionContext, bool working)
{
	executionContext.jobState->working.store(working);
}
static inline bool IsHashExecutionWorking(const HashExecutionContext& executionContext)
{
	return executionContext.jobState->working.load();
}
static inline bool ShouldStopHashExecution(const HashExecutionContext& executionContext)
{
	return executionContext.cancellationState->stopRequested.load();
}
static inline uint64_t GetHashExecutionTotalSize(const HashExecutionContext& executionContext)
{
	return executionContext.jobState->countedSize;
}
static inline void ResetHashExecutionTotalSize(HashExecutionContext& executionContext)
{
	executionContext.jobState->countedSize = 0;
}
static inline void AddHashExecutionTotalSize(HashExecutionContext& executionContext, uint64_t sizeDelta)
{
	executionContext.jobState->countedSize += sizeDelta;
}
static inline void ReplaceHashExecutionCountedFileSize(HashExecutionContext& executionContext, uint64_t previousSize, uint64_t currentSize)
{
	executionContext.jobState->countedSize = GetHashExecutionTotalSize(executionContext) + currentSize - previousSize;
}
static inline HashResult& AppendHashExecutionResult(HashExecutionContext& executionContext)
{
	HashResult resultNew;
	executionContext.jobState->results.push_back(resultNew);
	return executionContext.jobState->results.back();
}
#endif
