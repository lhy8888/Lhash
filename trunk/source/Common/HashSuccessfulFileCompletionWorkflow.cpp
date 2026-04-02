#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void PublishWholeProgressAfterFile(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, uint32_t fileIndex)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		if (!isSizeCaled)
		{
			if (GetHashRequestFileCount(request) == 0)
			{
				observer->onProgressEvent(CreateTotalProgressEvent(0));
			}
			else
			{
				int progressMax = observer->progressMax();
				observer->onProgressEvent(CreateTotalProgressEvent((fileIndex + 1) * progressMax / static_cast<int>(GetHashRequestFileCount(request))));
			}
		}
	}

	void ExecuteSuccessfulFileHashingWorkflow(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		observer->onProgressEvent(CreateFileCalculatedProgressEvent());

		FinalizeDigestStrings(request, executionState.hashContexts, executionState.digestBundle);
		UpdateWholeProgressAfterFile(executionContext, request, isSizeCaled, fileIndex);

		executionState.fileAttemptState.osFile->close();

		PopulateDigestResult(request, result, executionState.digestBundle);
		if (!result.digests.empty())
		{
			EmitHashResult(executionContext, result, GetHashRequestUppercaseDigest(request));
		}
	}
}
