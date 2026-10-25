#ifndef _MFC_HASH_THREAD_ENTRY_RUNTIME_H_
#define _MFC_HASH_THREAD_ENTRY_RUNTIME_H_

#include "Common/HashEngine.h"
#include "Adapters/MfcBridge/HashThreadEntryBridge.h"
#include "Adapters/MfcBridge/HashThreadLaunchContext.h"
#include <memory>

static inline int RunMfcHashThread(void *param)
{
	if (param == NULL)
	{
		return 1;
	}
	try
	{
		std::unique_ptr<HashThreadLaunchContext> launchContext(static_cast<HashThreadLaunchContext*>(param));
		ThreadData *thrdData = launchContext->threadData;
		HashExecutionContext executionContext = CreateThreadDataHashExecutionContext(*thrdData);
		const HashRequest& request = launchContext->request;
		return RunHashRequest(&executionContext, request);
	}
	catch (...)
	{
		// No C++ exception may cross _beginthreadex. The UI observes the nonzero
		// exit code only after the handle is signalled and then reports failure.
		return 1;
	}
}

#endif
