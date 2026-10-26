#ifndef _MFC_HASH_WORKER_SESSION_LIFECYCLE_H_
#define _MFC_HASH_WORKER_SESSION_LIFECYCLE_H_

#include "Adapters/MfcBridge/ThreadDataExecutionAccess.h"

template<typename TLaunch>
static inline bool LaunchThreadDataWorker(ThreadData& threadData, TLaunch launch)
{
	if (!threadData.executionState.workerSession.TryBeginLaunch())
	{
		return false;
	}
	threadData.executionState.timing.Reset();
	SetThreadDataStop(threadData, false);
	try
	{
		if (launch())
		{
			return true;
		}
	}
	catch (...)
	{
		// A failed launch never transfers ownership to a worker.
	}
	SetThreadDataWorking(threadData, false);
	threadData.executionState.workerSession.LaunchFailed();
	return false;
}

template<typename TTryJoin>
static inline bool CollectThreadDataWorker(ThreadData& threadData, TTryJoin tryJoin)
{
	if (!threadData.executionState.workerSession.IsActive() || !tryJoin())
	{
		return false;
	}
	SetThreadDataWorking(threadData, false);
	threadData.executionState.workerSession.WorkerJoined();
	return true;
}

#endif
