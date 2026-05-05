#ifndef _MFC_HASH_STATE_H_
#define _MFC_HASH_STATE_H_

#include "Common/HashTypes.h"
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
