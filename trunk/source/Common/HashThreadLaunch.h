#ifndef _HASH_THREAD_LAUNCH_H_
#define _HASH_THREAD_LAUNCH_H_

#include <process.h>

#include "Common/HashThreadEntry.h"

static inline HANDLE StartHashWorkerThread(ThreadData *threadData, unsigned int *threadId)
{
	return reinterpret_cast<HANDLE>(_beginthreadex(
		NULL,
		0,
		reinterpret_cast<unsigned int (WINAPI*)(void*)>(HashThreadFunc),
		threadData,
		0,
		threadId));
}

#endif
