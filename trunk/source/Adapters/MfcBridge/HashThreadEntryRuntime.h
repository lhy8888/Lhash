#ifndef _MFC_HASH_THREAD_ENTRY_RUNTIME_H_
#define _MFC_HASH_THREAD_ENTRY_RUNTIME_H_

#include "Common/HashEngine.h"
#include "Adapters/MfcBridge/HashThreadEntryProjection.h"

static inline int RunMfcHashThread(void *param)
{
	ThreadData *thrdData = (ThreadData *)param;
	HashExecutionContext executionContext = CreateThreadDataHashExecutionContext(*thrdData);
	HashRequest request = CreateThreadDataHashRequest(*thrdData);
	return RunHashRequest(&executionContext, request);
}

#endif
