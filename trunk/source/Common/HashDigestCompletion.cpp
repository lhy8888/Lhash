#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	bool CompleteOpenedFileDigestExecution(HashExecutionContext *executionContext, FileExecutionState *executionState, bool wasStopped)
	{
		if (wasStopped)
		{
			executionState->fileAttemptState.osFile->close();
			return true;
		}

		if (ShouldStopHashExecution(*executionContext))
		{
			executionState->fileAttemptState.osFile->close();
			return true;
		}

		return false;
	}
}
