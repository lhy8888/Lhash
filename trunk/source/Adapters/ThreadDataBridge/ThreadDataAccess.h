#ifndef _LEGACY_THREAD_DATA_ACCESS_H_
#define _LEGACY_THREAD_DATA_ACCESS_H_

#include "Adapters/ThreadDataBridge/ThreadDataExecutionAccess.h"
#include "Adapters/ThreadDataBridge/ThreadDataInputAccess.h"
#include "Adapters/ThreadDataBridge/ThreadDataResultAccess.h"

static inline void ResetThreadDataForNewSession(ThreadData& threadData)
{
	SetThreadDataWorking(threadData, false);
	SetThreadDataStop(threadData, false);
	SetThreadDataUppercase(threadData, false);
	ResetThreadDataHashAlgorithms(threadData);
	ResetThreadDataTotalSize(threadData);

	ResetThreadDataInputFiles(threadData);
	ClearThreadDataResults(threadData);
}

#endif
