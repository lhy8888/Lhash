#ifndef _RUNTIME_HASH_EXECUTION_CONTEXT_H_
#define _RUNTIME_HASH_EXECUTION_CONTEXT_H_

#include "Common/CheckedArithmetic.h"
#include "Common/Global.h"
#include "Runtime/HashProgressSink.h"

class NullHashProgressSink : public HashProgressSink
{
public:
	virtual int progressMax()
	{
		return 100;
	}

	virtual void onProgressEvent(const ProgressEvent& progressEvent)
	{
		(void)progressEvent;
	}
};

static inline HashProgressSink& GetNullHashProgressSink()
{
	static NullHashProgressSink sink;
	return sink;
}

struct HashExecutionContext
{
	HashExecutionContext(HashProgressSink *sink, HashJobState& state, HashCancellationState& cancellation)
		: progressSinkObserver(sink != NULL ? sink : &GetNullHashProgressSink()),
		jobState(state),
		cancellationState(cancellation)
	{
	}
	HashProgressSink *progressSinkObserver; // Non-owning; the caller keeps the sink alive until RunHashRequest returns.
	HashJobState& jobState;
	HashCancellationState& cancellationState;
};

static inline HashExecutionContext CreateHashExecutionContext(HashProgressSink *progressSink, HashJobState& jobState, HashCancellationState& cancellationState)
{
	return HashExecutionContext(progressSink, jobState, cancellationState);
}

static inline HashProgressSink *GetHashExecutionProgressSink(const HashExecutionContext& executionContext)
{
	return executionContext.progressSinkObserver;
}

static inline void SetHashExecutionWorking(HashExecutionContext& executionContext, bool working)
{
	executionContext.jobState.working.store(working);
}

static inline bool IsHashExecutionWorking(const HashExecutionContext& executionContext)
{
	return executionContext.jobState.working.load();
}

static inline bool ShouldStopHashExecution(const HashExecutionContext& executionContext)
{
	return executionContext.cancellationState.stopRequested.load();
}

static inline uint64_t GetHashExecutionTotalSize(const HashExecutionContext& executionContext)
{
	return executionContext.jobState.countedSize.load(std::memory_order_relaxed);
}

static inline void ResetHashExecutionTotalSize(HashExecutionContext& executionContext)
{
	executionContext.jobState.countedSize.store(0, std::memory_order_relaxed);
}

static inline void AddHashExecutionTotalSize(HashExecutionContext& executionContext, uint64_t sizeDelta)
{
	uint64_t current = executionContext.jobState.countedSize.load(std::memory_order_relaxed);
	for (;;)
	{
		uint64_t desired = SaturatingAddUInt64(current, sizeDelta);
		if (executionContext.jobState.countedSize.compare_exchange_weak(
			current,
			desired,
			std::memory_order_relaxed,
			std::memory_order_relaxed))
		{
			return;
		}
	}
}

static inline void ReplaceHashExecutionCountedFileSize(HashExecutionContext& executionContext, uint64_t previousSize, uint64_t currentSize)
{
	uint64_t current = executionContext.jobState.countedSize.load(std::memory_order_relaxed);
	for (;;)
	{
		uint64_t desired = ReplaceSizedValueUInt64(current, previousSize, currentSize);
		if (executionContext.jobState.countedSize.compare_exchange_weak(
			current,
			desired,
			std::memory_order_relaxed,
			std::memory_order_relaxed))
		{
			return;
		}
	}
}

static inline HashResult& AppendHashExecutionResult(HashExecutionContext& executionContext)
{
	HashResult resultNew;
	executionContext.jobState.results.push_back(resultNew);
	return executionContext.jobState.results.back();
}

#endif
