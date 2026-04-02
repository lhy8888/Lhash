#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void ExecuteOpenedFileAttemptCompletionWorkflow(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState)
	{
		if (executionState.fileAttemptState.readFailed)
		{
			executionState.fileAttemptState.osFile->close();
			EmitReadFileError(executionContext, result);
		}
		else
		{
			CompleteSuccessfulFileHashing(executionContext, request, result, fileIndex, isSizeCaled, executionState);
		}

		FinishFileProcessing(executionContext);
	}

	void ExecuteFileAttemptCompletionWorkflow(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState)
	{
		if (executionState.fileAttemptState.isFileOpened)
		{
			CompleteOpenedFileAttempt(executionContext, request, result, fileIndex, isSizeCaled, executionState);
		}
		else
		{
			EmitOpenFileError(executionContext, result, executionState.fileAttemptState.openErrorText);
			FinishFileProcessing(executionContext);
		}
	}
}
