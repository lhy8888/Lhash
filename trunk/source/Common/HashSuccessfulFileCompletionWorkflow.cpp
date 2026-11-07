#include "stdafx.h"

#include "Common/HashEngineInternal.h"
#include "Common/CheckedArithmetic.h"

namespace HashEngineInternal
{
	void PublishWholeProgressAfterFile(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, uint32_t fileIndex)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		const uint64_t fileCount = GetHashRequestFileCount(request);
		if (!isSizeCaled || !executionContext->totalSizeKnown || GetHashExecutionTotalSize(*executionContext) == 0)
		{
			if (fileCount == 0)
			{
				observer->onProgressEvent(CreateTotalProgressEvent(0));
			}
			else
			{
				int progressMax = observer->progressMax();
				int position = CalculateIndexedProgressValue(
					static_cast<uint64_t>(fileIndex) + 1,
					fileCount,
					progressMax);
				if (position > executionContext->lastTotalProgress)
				{
					executionContext->lastTotalProgress = position;
					observer->onProgressEvent(CreateTotalProgressEvent(position));
				}
			}
		}
		else if (fileCount > 0 && static_cast<uint64_t>(fileIndex) + 1 == fileCount)
		{
			int progressMax = observer->progressMax();
			if (progressMax > executionContext->lastTotalProgress)
			{
				executionContext->lastTotalProgress = progressMax;
				observer->onProgressEvent(CreateTotalProgressEvent(progressMax));
			}
		}
	}

	void ExecuteSuccessfulFileHashingWorkflow(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState)
	{
		sunjwbase::tstring consistencyErrorText;
		const bool hasDigestOperations = !GetHashJobDigestUpdateRequest(executionState.executionPlan).operationDescriptors.empty();
		if (hasDigestOperations && !executionState.fileAttemptState.osFile->validateReadConsistency(&consistencyErrorText))
		{
			executionState.fileAttemptState.osFile->close();
			EmitErrorMessageResult(executionContext, result, consistencyErrorText);
			return;
		}

		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		observer->onProgressEvent(CreateFileCalculatedProgressEvent(executionContext->currentTaskIndex));

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
