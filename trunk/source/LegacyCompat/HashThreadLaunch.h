#ifndef _LEGACY_HASH_THREAD_LAUNCH_H_
#define _LEGACY_HASH_THREAD_LAUNCH_H_

#include <process.h>
#include <utility>

#include "LegacyCompat/HashThreadEntry.h"
#include "LegacyCompat/LegacyThreadData.h"
#include "WinCommon/WinHandleGuard.h"

#if defined (_WIN32)
#include <WinBase.h>
#endif

static inline WinHandleGuard::UniqueWinHandle StartHashWorkerThread(ThreadData *threadData, unsigned int *threadId)
{
	return WinHandleGuard::UniqueWinHandle(reinterpret_cast<HANDLE>(_beginthreadex(
		NULL,
		0,
		reinterpret_cast<unsigned int (WINAPI*)(void*)>(HashThreadFunc),
		threadData,
		0,
		threadId)));
}

static inline void CloseHashWorkerThreadHandle(WinHandleGuard::UniqueWinHandle *threadHandle)
{
	if (threadHandle == NULL)
	{
		return;
	}

	threadHandle->reset();
}

static inline HANDLE RestartHashWorkerThread(WinHandleGuard::UniqueWinHandle *existingThreadHandle, ThreadData *threadData, unsigned int *threadId)
{
	CloseHashWorkerThreadHandle(existingThreadHandle);
	WinHandleGuard::UniqueWinHandle workThreadHandle = StartHashWorkerThread(threadData, threadId);
	if (existingThreadHandle != NULL)
	{
		*existingThreadHandle = std::move(workThreadHandle);
	}
	return (existingThreadHandle != NULL) ? existingThreadHandle->get() : NULL;
}

#endif
