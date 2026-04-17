#include "stdafx.h"

#include "Common/HashEngineInternal.h"
#include "Common/CheckedArithmetic.h"

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
				observer->onProgressEvent(CreateTotalProgressEvent(CalculateIndexedProgressValue(
					static_cast<uint64_t>(fileIndex) + 1,
					GetHashRequestFileCount(request),
					progressMax)));
			}
		}
	}

	void ExecuteSuccessfulFileHashingWorkflow(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		observer->onProgressEvent(CreateFileCalculatedProgressEvent());

		executionState.fileAttemptState.osFile->close();
		sunjwbase::tstring finalizeErrorText;
		if (!FinalizeDigestStrings(request, executionState.hashContexts, executionState.digestBundle, &finalizeErrorText))
		{
			if (finalizeErrorText.empty())
			{
				finalizeErrorText = sunjwbase::strtotstr(std::string("Failed to finalize a digest while hashing."));
			}

			EmitErrorMessageResult(executionContext, result, finalizeErrorText);
			return;
		}

		UpdateWholeProgressAfterFile(executionContext, request, isSizeCaled, fileIndex);

		PopulateDigestResult(request, result, executionState.digestBundle);
		if (!result.digests.empty())
		{
			EmitHashResult(executionContext, result, GetHashRequestUppercaseDigest(request));
		}
	}
}
