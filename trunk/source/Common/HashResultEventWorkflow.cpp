#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void PublishMetaResultEvent(HashExecutionContext *executionContext, HashResult& result)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		result.state = RESULT_META;
		observer->onProgressEvent(CreateFileMetaReadyProgressEvent(result));
	}

	void PublishHashResultEvent(HashExecutionContext *executionContext, HashResult& result, bool uppercase)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		result.state = RESULT_ALL;
		observer->onProgressEvent(CreateFileHashReadyProgressEvent(result, uppercase));
	}

	void PublishErrorResultEvent(HashExecutionContext *executionContext, HashResult& result)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		result.state = RESULT_ERROR;
		observer->onProgressEvent(CreateFileFailedProgressEvent(result));
	}

	void PublishFileFinishedEvent(HashExecutionContext *executionContext)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		observer->onProgressEvent(CreateFileFinishedProgressEvent());
	}
}
