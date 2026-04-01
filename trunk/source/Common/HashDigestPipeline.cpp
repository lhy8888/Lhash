#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	bool ProcessOpenedFileHashing(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex,
		bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		, ThreadPool *threadPool
#endif
	)
	{
		InitializeFileHashing(request, executionContext, &executionState->hashContexts);
		DigestUpdateRequest digestUpdateRequest = CreateDigestUpdateRequest(request);

		uint64_t fsize = PrepareFileMetaResult(executionContext, result, *executionState->fileAttemptState.osFile, executionState->fileAttemptState.path,
			isSizeCaled, fSizes, fileIndex, executionState->fileAttemptState.fileVersion);
		uint64_t times = CalculateFileChunkIterations(fsize);
		(void)times;

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		bool wasStopped = ProcessOpenedFileHashingParallel(executionContext, digestUpdateRequest, fsize, isSizeCaled, executionState, threadPool);
		if (wasStopped)
		{
			executionState->fileAttemptState.osFile->close();
			return true;
		}
#else
		bool isFileFinished = false;
		DigestDataBuffer databuf;
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

			isFileFinished = (databuf.datalen < GetDigestDataBufferPreferredLength());
		}
		while (!isFileFinished && !executionState->fileAttemptState.readFailed);
#endif

		if (ShouldStopHashExecution(*executionContext))
		{
			executionState->fileAttemptState.osFile->close();
			return true;
		}

		return false;
	}
}
