#ifndef _HASH_FILE_RESULT_WORKFLOW_H_
#define _HASH_FILE_RESULT_WORKFLOW_H_

#include "Common/HashExecutionContext.h"
#include "Common/HashResult.h"

namespace HashEngineInternal
{
	struct FileExecutionState;

	void PublishFilePathResult(HashExecutionContext *executionContext, HashResult& result);
	HashResult& ExecuteFileResultBeginWorkflow(HashExecutionContext *executionContext, const sunjwbase::tstring& path);
	HashResult& ExecuteFileHashAttemptBeginWorkflow(HashExecutionContext *executionContext, const sunjwbase::tstring& path, FileExecutionState *executionState, const TCHAR **resultPath);
}

#endif
