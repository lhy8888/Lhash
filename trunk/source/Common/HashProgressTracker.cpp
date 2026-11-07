#include "stdafx.h"

#include "Common/HashEngineInternal.h"
#include "Common/CheckedArithmetic.h"

namespace HashEngineInternal
{
	void UpdateHashExecutionProgress(HashExecutionContext *executionContext, uint64_t fileSize, bool isSizeCaled, unsigned int dataLen,
		FileProgressState *progressState)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		progressState->finishedSize = SaturatingAddUInt64(progressState->finishedSize, dataLen);

		int progressMax = observer->progressMax();

		if (progressState->fileSizeKnown)
		{
			int positionNew = CalculateBoundedProgressValue(progressState->finishedSize, fileSize, progressMax);
			if (positionNew > progressState->position)
			{
				observer->onProgressEvent(CreateFileProgressEvent(positionNew, executionContext->currentTaskIndex));
				progressState->position = positionNew;
			}
		}

		progressState->finishedSizeWhole = SaturatingAddUInt64(progressState->finishedSizeWhole, dataLen);
		uint64_t totalSize = GetHashExecutionTotalSize(*executionContext);
		if (isSizeCaled && executionContext->totalSizeKnown && totalSize != 0 && progressMax > 0)
		{
			int positionWholeNew = CalculateBoundedProgressValue(progressState->finishedSizeWhole, totalSize, progressMax);
			// File sizes may change or a later metadata query may fail. Completion
			// is only certain after the last file has finished successfully.
			if (positionWholeNew >= progressMax) positionWholeNew = progressMax - 1;
			if (positionWholeNew > progressState->positionWhole && positionWholeNew > executionContext->lastTotalProgress)
			{
				progressState->positionWhole = positionWholeNew;
				executionContext->lastTotalProgress = positionWholeNew;
				observer->onProgressEvent(CreateTotalProgressEvent(positionWholeNew));
			}
		}
	}
}
