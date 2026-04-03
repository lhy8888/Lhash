#ifndef _HASH_THREAD_ENTRY_PROJECTION_H_
#define _HASH_THREAD_ENTRY_PROJECTION_H_

#include "Common/HashExecutionContext.h"
#include "Common/HashRequestProjection.h"
#include "Common/ThreadDataExecutionAccess.h"

static inline HashExecutionContext CreateThreadDataHashExecutionContext(ThreadData& threadData)
{
	return CreateHashExecutionContext(
		GetThreadDataObserver(threadData),
		GetMutableThreadDataHashJobState(threadData),
		GetMutableThreadDataHashCancellationState(threadData));
}

static inline HashRequest CreateThreadDataHashRequest(const ThreadData& threadData)
{
	return CreateHashRequest(threadData);
}

#endif
