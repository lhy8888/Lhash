#ifndef _LEGACY_HASH_THREAD_LAUNCH_H_
#define _LEGACY_HASH_THREAD_LAUNCH_H_

#include <process.h>

#include "Common/HashThreadEntry.h"
#include "LegacyCompat/LegacyThreadData.h"

#if defined (_WIN32)
#include <WinBase.h>
#endif

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

static inline void CloseHashWorkerThreadHandle(HANDLE *threadHandle)
{
	if (threadHandle == NULL || *threadHandle == NULL)
	{
		return;
	}

	CloseHandle(*threadHandle);
	*threadHandle = NULL;
}

static inline HANDLE RestartHashWorkerThread(HANDLE *existingThreadHandle, ThreadData *threadData, unsigned int *threadId)
{
	CloseHashWorkerThreadHandle(existingThreadHandle);
	HANDLE workThreadHandle = StartHashWorkerThread(threadData, threadId);
	if (existingThreadHandle != NULL)
	{
		*existingThreadHandle = workThreadHandle;
	}
	return workThreadHandle;
}

#endif
