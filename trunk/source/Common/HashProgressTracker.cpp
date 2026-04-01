#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void UpdateHashExecutionProgress(HashExecutionContext *executionContext, uint64_t fileSize, bool isSizeCaled, unsigned int dataLen,
		FileProgressState *progressState)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		progressState->finishedSize += dataLen;

		int progressMax = observer->progressMax();

		int positionNew;
		if (fileSize == 0)
		{
			positionNew = progressMax;
		}
		else
		{
			positionNew = (int)(progressMax * progressState->finishedSize / fileSize);
		}

		if (positionNew > progressState->position)
		{
			observer->onProgressEvent(CreateFileProgressEvent(positionNew));
			progressState->position = positionNew;
		}

		progressState->finishedSizeWhole += dataLen;
		int positionWholeNew;
		uint64_t totalSize = GetHashExecutionTotalSize(*executionContext);
		if (totalSize == 0)
		{
			positionWholeNew = progressMax;
		}
		else
		{
			positionWholeNew = (int)(progressMax * progressState->finishedSizeWhole / totalSize);
		}

		if (isSizeCaled && positionWholeNew > progressState->positionWhole)
		{
			progressState->positionWhole = positionWholeNew;
			observer->onProgressEvent(CreateTotalProgressEvent(progressState->positionWhole));
		}
	}
}
