#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	bool ExecuteScheduledHashRequestFiles(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		, ThreadPool *threadPool
#endif
	)
	{
		return VisitHashRequestFiles(request, [&](uint32_t fileIndex, const sunjwbase::tstring& fullPath)
		{
			return RunFileHashAttempt(executionContext, request, fileIndex, fullPath, isSizeCaled, fSizes,
				executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
				, threadPool
#endif
			);
		});
	}
}
