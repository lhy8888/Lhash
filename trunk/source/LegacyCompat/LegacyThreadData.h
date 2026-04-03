#ifndef _LEGACY_THREAD_DATA_H_
#define _LEGACY_THREAD_DATA_H_

#include "Common/Global.h"
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
};

struct ThreadData
{
	HashProgressSink *observer;
	ThreadDataInputState inputState;
	ThreadDataExecutionState executionState;
};

#endif
