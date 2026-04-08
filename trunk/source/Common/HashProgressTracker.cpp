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

		int positionNew = CalculateBoundedProgressValue(progressState->finishedSize, fileSize, progressMax);

		if (positionNew > progressState->position)
		{
			observer->onProgressEvent(CreateFileProgressEvent(positionNew));
			progressState->position = positionNew;
		}

		progressState->finishedSizeWhole = SaturatingAddUInt64(progressState->finishedSizeWhole, dataLen);
		uint64_t totalSize = GetHashExecutionTotalSize(*executionContext);
		int positionWholeNew = CalculateBoundedProgressValue(progressState->finishedSizeWhole, totalSize, progressMax);

		if (isSizeCaled && positionWholeNew > progressState->positionWhole)
		{
			progressState->positionWhole = positionWholeNew;
			observer->onProgressEvent(CreateTotalProgressEvent(progressState->positionWhole));
		}
	}
}
