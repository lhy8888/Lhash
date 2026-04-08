#ifndef _LEGACY_HASH_THREAD_ENTRY_PROJECTION_H_
#define _LEGACY_HASH_THREAD_ENTRY_PROJECTION_H_

#include "Runtime/HashExecutionContext.h"
#include "LegacyCompat/HashRequestProjection.h"
#include "LegacyCompat/ThreadDataExecutionAccess.h"

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
