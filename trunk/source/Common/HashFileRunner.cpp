#include "stdafx.h"

#include "Common/HashEngineInternal.h"

using namespace sunjwbase;

namespace HashEngineInternal
{
	bool RunFileHashAttempt(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const tstring& fullPath, bool isSizeCaled, ULLongVector& fSizes,
		FileExecutionState *executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		, ThreadPool *threadPool
#endif
	)
	{
		return ExecuteFileHashAttemptWorkflow(executionContext, request, fileIndex, fullPath, isSizeCaled, fSizes, executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
			, threadPool
#endif
		);
	}
}
