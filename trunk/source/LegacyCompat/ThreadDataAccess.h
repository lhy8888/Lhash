#ifndef _LEGACY_THREAD_DATA_ACCESS_H_
#define _LEGACY_THREAD_DATA_ACCESS_H_

#include "LegacyCompat/ThreadDataExecutionAccess.h"
#include "LegacyCompat/ThreadDataInputAccess.h"
#include "LegacyCompat/ThreadDataResultAccess.h"

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
