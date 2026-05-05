#ifndef _LEGACY_HASH_THREAD_ENTRY_RUNTIME_H_
#define _LEGACY_HASH_THREAD_ENTRY_RUNTIME_H_

#include "Common/HashEngine.h"
#include "Adapters/ThreadDataBridge/HashThreadEntryProjection.h"

static inline int RunLegacyHashThread(void *param)
{
	ThreadData *thrdData = (ThreadData *)param;
	HashExecutionContext executionContext = CreateThreadDataHashExecutionContext(*thrdData);
	HashRequest request = CreateThreadDataHashRequest(*thrdData);
	return RunHashRequest(&executionContext, request);
}

#endif
