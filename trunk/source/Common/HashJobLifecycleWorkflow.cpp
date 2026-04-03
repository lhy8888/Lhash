#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void ExecuteCancelledHashingWorkflow(HashExecutionContext *executionContext, HashProgressSink *observer)
	{
		SetHashExecutionWorking(*executionContext, false);
		observer->onProgressEvent(CreateCancelledProgressEvent());
	}

	void ExecuteCompletedHashingWorkflow(HashExecutionContext *executionContext, HashProgressSink *observer)
	{
		observer->onProgressEvent(CreateCompletedProgressEvent());
		SetHashExecutionWorking(*executionContext, false);
	}
}
