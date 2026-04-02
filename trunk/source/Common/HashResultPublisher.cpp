#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void UpdateWholeProgressAfterFile(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, uint32_t fileIndex)
	{
		PublishWholeProgressAfterFile(executionContext, request, isSizeCaled, fileIndex);
	}

	void CompleteSuccessfulFileHashing(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState)
	{
		ExecuteSuccessfulFileHashingWorkflow(executionContext, request, result, fileIndex, isSizeCaled, executionState);
	}

	void CompleteOpenedFileAttempt(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState)
	{
		ExecuteOpenedFileAttemptCompletionWorkflow(executionContext, request, result, fileIndex, isSizeCaled, executionState);
	}

	void EmitMetaResult(HashExecutionContext *executionContext, HashResult& result)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		result.state = RESULT_META;
		observer->onProgressEvent(CreateFileMetaReadyProgressEvent(result));
	}

	void EmitHashResult(HashExecutionContext *executionContext, HashResult& result, bool uppercase)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		result.state = RESULT_ALL;
		observer->onProgressEvent(CreateFileHashReadyProgressEvent(result, uppercase));
	}

	void EmitErrorResult(HashExecutionContext *executionContext, HashResult& result)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		result.state = RESULT_ERROR;
		observer->onProgressEvent(CreateFileFailedProgressEvent(result));
	}

	void EmitErrorMessageResult(HashExecutionContext *executionContext, HashResult& result, const sunjwbase::tstring& errorText)
	{
		result.error = errorText;
		EmitErrorResult(executionContext, result);
	}

	void EmitOpenFileError(HashExecutionContext *executionContext, HashResult& result, const TCHAR *errorText)
	{
		EmitErrorMessageResult(executionContext, result, sunjwbase::tstring(errorText));
	}

	void EmitReadFileError(HashExecutionContext *executionContext, HashResult& result)
	{
		EmitErrorMessageResult(executionContext, result, sunjwbase::strtotstr(std::string("Failed to read file while hashing.")));
	}

	void FinishFileProcessing(HashExecutionContext *executionContext)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		observer->onProgressEvent(CreateFileFinishedProgressEvent());
	}

	void CompleteFileAttempt(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState)
	{
		ExecuteFileAttemptCompletionWorkflow(executionContext, request, result, fileIndex, isSizeCaled, executionState);
	}
}
