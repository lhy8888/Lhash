#ifndef _MFC_HASH_THREAD_LAUNCH_CONTEXT_H_
#define _MFC_HASH_THREAD_LAUNCH_CONTEXT_H_

#include "Adapters/MfcBridge/HashThreadEntryBridge.h"

struct HashThreadLaunchContext
{
	explicit HashThreadLaunchContext(ThreadData& state)
		: threadData(&state), request(CreateThreadDataHashRequest(state))
	{
	}
	ThreadData* threadData; // Owner retains the session lease until thread exit.
	const HashRequest request; // Copied on the UI thread, before _beginthreadex.
};

#endif
