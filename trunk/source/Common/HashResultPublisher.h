#ifndef _HASH_RESULT_PUBLISHER_H_
#define _HASH_RESULT_PUBLISHER_H_

#include "Runtime/HashExecutionContext.h"
#include "Domain/HashRequest.h"
#include "Common/HashResult.h"

namespace HashEngineInternal
{
	struct FileExecutionState;

	void UpdateWholeProgressAfterFile(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, uint32_t fileIndex);
	void CompleteSuccessfulFileHashing(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState);
	void CompleteOpenedFileAttempt(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState);
	void EmitMetaResult(HashExecutionContext *executionContext, HashResult& result);
	void EmitHashResult(HashExecutionContext *executionContext, HashResult& result, bool uppercase);
	void EmitErrorResult(HashExecutionContext *executionContext, HashResult& result);
	void EmitErrorMessageResult(HashExecutionContext *executionContext, HashResult& result, const sunjwbase::tstring& errorText);
	void EmitOpenFileError(HashExecutionContext *executionContext, HashResult& result, const TCHAR *errorText);
	void EmitReadFileError(HashExecutionContext *executionContext, HashResult& result);
	void FinishFileProcessing(HashExecutionContext *executionContext);
	void CompleteFileAttempt(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState);
}

#endif
