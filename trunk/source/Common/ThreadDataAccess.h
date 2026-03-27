#ifndef _THREAD_DATA_ACCESS_H_
#define _THREAD_DATA_ACCESS_H_

#include "Common/ThreadDataExecutionAccess.h"
#include "Common/ThreadDataInputAccess.h"
#include "Common/ThreadDataResultAccess.h"

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
