#include "stdafx.h"

#include "Common/HashEngineInternal.h"

using namespace sunjwbase;

namespace HashEngineInternal
{
	void RunHashPreScanVisitWorkflow(HashExecutionContext *executionContext, const HashRequest& request, ULLongVector& fSizes, bool *wasCancelled)
	{
		VisitHashRequestFiles(request, [&](uint32_t fileIndex, const tstring& fullPath)
		{
			(void)fullPath;
			if (ShouldStopHashExecution(*executionContext))
			{
				*wasCancelled = true;
				return false;
			}

			AccumulatePreScannedFileSize(executionContext, request, fSizes, fileIndex);
			return true;
		});
	}
}
