#ifndef _MFC_HASH_THREAD_ENTRY_BRIDGE_H_
#define _MFC_HASH_THREAD_ENTRY_BRIDGE_H_

#include "Runtime/HashExecutionContext.h"
#include "Adapters/MfcBridge/HashRequestBridge.h"
#include "Adapters/MfcBridge/ThreadDataExecutionAccess.h"

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
