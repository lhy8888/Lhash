#ifndef _HASH_DIGEST_COMPLETION_H_
#define _HASH_DIGEST_COMPLETION_H_

#include "Runtime/HashExecutionContext.h"

namespace HashEngineInternal
{
	struct FileExecutionState;

	bool CompleteOpenedFileDigestExecution(HashExecutionContext *executionContext, FileExecutionState *executionState, bool wasStopped);
}

#endif
