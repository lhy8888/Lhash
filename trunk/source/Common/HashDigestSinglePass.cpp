#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	bool ProcessOpenedFileHashingSinglePass(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fsize, bool isSizeCaled, unsigned int preferredBufferLength,
		FileExecutionState *executionState)
	{
		bool isFileFinished = false;
		DigestDataBuffer databuf(preferredBufferLength);
		do
		{
			if (ShouldStopHashExecution(*executionContext))
			{
				break;
			}

			if (ReadDigestDataBuffer(executionState, databuf))
			{
				UpdateDigestContextsSequential(digestUpdateRequest, executionState->hashContexts, databuf.data, databuf.datalen);
				UpdateHashExecutionProgress(executionContext, fsize, isSizeCaled, databuf.datalen, &executionState->progressState);
			}

			isFileFinished = (databuf.datalen < databuf.capacity);
		}
		while (!isFileFinished && !executionState->fileAttemptState.readFailed);

		return ShouldStopHashExecution(*executionContext);
	}
}
