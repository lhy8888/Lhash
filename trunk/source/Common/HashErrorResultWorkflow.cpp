#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void PublishErrorMessageResult(HashExecutionContext *executionContext, HashResult& result, const sunjwbase::tstring& errorText)
	{
		result.error = errorText;
		EmitErrorResult(executionContext, result);
	}
}
