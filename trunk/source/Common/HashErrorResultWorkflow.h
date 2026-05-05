#ifndef _HASH_ERROR_RESULT_WORKFLOW_H_
#define _HASH_ERROR_RESULT_WORKFLOW_H_

#include "Runtime/HashExecutionContext.h"
#include "Domain/HashResult.h"

namespace HashEngineInternal
{
	void PublishErrorMessageResult(HashExecutionContext *executionContext, HashResult& result, const sunjwbase::tstring& errorText);
}

#endif
