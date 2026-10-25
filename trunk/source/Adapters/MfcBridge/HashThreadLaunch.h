#ifndef _MFC_HASH_THREAD_LAUNCH_H_
#define _MFC_HASH_THREAD_LAUNCH_H_

#include <process.h>
#include <utility>
#include <memory>

#include "Adapters/MfcBridge/HashThreadEntry.h"
#include "Adapters/MfcBridge/MfcHashState.h"
#include "Adapters/MfcBridge/HashThreadLaunchContext.h"
#include "WinCommon/WinHandleGuard.h"

#if defined (_WIN32)
#include <WinBase.h>
#endif

static inline WinHandleGuard::UniqueWinHandle StartHashWorkerThread(ThreadData *threadData, unsigned int *threadId)
{
	if (threadData == NULL) return WinHandleGuard::UniqueWinHandle();
	std::unique_ptr<HashThreadLaunchContext> context(new HashThreadLaunchContext(*threadData));
	WinHandleGuard::UniqueWinHandle worker(reinterpret_cast<HANDLE>(_beginthreadex(
		NULL,
		0,
		reinterpret_cast<unsigned int (WINAPI*)(void*)>(HashThreadFunc),
		context.get(),
		0,
		threadId)));
	if (worker.isValid()) context.release(); // The worker entry now owns the snapshot.
	return worker;
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
	// Never discard ownership of a live worker. Closing a thread handle is not a join.
	if (existingThreadHandle == NULL ||
		(existingThreadHandle->isValid() && WaitForSingleObject(existingThreadHandle->get(), 0) != WAIT_OBJECT_0))
	{
		return NULL;
	}
	CloseHashWorkerThreadHandle(existingThreadHandle);
	WinHandleGuard::UniqueWinHandle workThreadHandle = StartHashWorkerThread(threadData, threadId);
	if (existingThreadHandle != NULL)
	{
		*existingThreadHandle = std::move(workThreadHandle);
	}
	return (existingThreadHandle != NULL) ? existingThreadHandle->get() : NULL;
}

#endif
