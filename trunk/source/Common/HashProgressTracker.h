#ifndef _HASH_PROGRESS_TRACKER_H_
#define _HASH_PROGRESS_TRACKER_H_

#include "Runtime/HashExecutionContext.h"

namespace HashEngineInternal
{
	struct FileProgressState;

	void UpdateHashExecutionProgress(HashExecutionContext *executionContext, uint64_t fileSize, bool isSizeCaled, unsigned int dataLen,
		FileProgressState *progressState);
}

#endif
