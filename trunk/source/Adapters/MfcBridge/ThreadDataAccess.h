#ifndef _MFC_THREAD_DATA_ACCESS_H_
#define _MFC_THREAD_DATA_ACCESS_H_

#include "Adapters/MfcBridge/ThreadDataExecutionAccess.h"
#include "Adapters/MfcBridge/ThreadDataInputAccess.h"
#include "Adapters/MfcBridge/ThreadDataResultAccess.h"

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
