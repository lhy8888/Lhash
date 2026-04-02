#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void PublishFilePathResult(HashExecutionContext *executionContext, HashResult& result)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		result.state = RESULT_PATH;
		observer->onProgressEvent(CreateFileStartedProgressEvent(result));
	}

	HashResult& ExecuteFileResultBeginWorkflow(HashExecutionContext *executionContext, const sunjwbase::tstring& path)
	{
		HashResult& result = AppendHashExecutionResult(*executionContext);
		result = HashResult();
		result.path = path;

		EmitPathResult(executionContext, result);
		return result;
	}

	HashResult& ExecuteFileHashAttemptBeginWorkflow(HashExecutionContext *executionContext, const sunjwbase::tstring& path, FileExecutionState *executionState, const TCHAR **resultPath)
	{
		ResetFileProgressState(&executionState->progressState);

		HashResult& result = BeginFileResult(executionContext, path);
		*resultPath = result.path.c_str();
		return result;
	}
}
