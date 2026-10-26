#ifndef _MFC_HASH_STATE_H_
#define _MFC_HASH_STATE_H_

#include "Common/HashTypes.h"
#include "Adapters/MfcBridge/HashWorkerSessionState.h"
#include "Adapters/MfcBridge/HashWorkerTiming.h"
class HashProgressSink;

struct ThreadDataInputState
{
	uint32_t fileCount;
	TStrVector inputFiles;
};

struct ThreadDataExecutionState
{
	HashExecutionPreferenceState preferences;
	HashCancellationState cancellation;
	HashJobState jobState;
	HashWorkerSessionState workerSession;
	HashWorkerTiming timing;
};

struct ThreadData
{
	ThreadData() : observer(NULL) { inputState.fileCount = 0; }
	HashProgressSink *observer;
	ThreadDataInputState inputState;
	ThreadDataExecutionState executionState;
};

#endif
