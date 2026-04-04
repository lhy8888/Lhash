#include "stdafx.h"

#include "Common/HashEngineInternal.h"
#include "Common/ThreadPool.h"

#include <atomic>
#include <condition_variable>
#include <future>
#include <memory>
#include <mutex>
#include <queue>

using namespace std;

namespace HashEngineInternal
{
	DigestDataBuffer::DigestDataBuffer(unsigned int preferredLength):datalen(0), capacity(NormalizeDigestDataBufferPreferredLength(preferredLength)), data(NULL)
	{
		data = new unsigned char[capacity];
	}

	DigestDataBuffer::~DigestDataBuffer()
	{
		delete[] data;
		datalen = 0;
	}

	unsigned int NormalizeDigestDataBufferPreferredLength(unsigned int preferredLength)
	{
		if (preferredLength == 0)
		{
			return 1;
		}

		return preferredLength;
	}

	uint64_t CalculateFileChunkIterations(uint64_t fileSize, unsigned int preferredLength)
	{
		unsigned int bufferLength = NormalizeDigestDataBufferPreferredLength(preferredLength);
		return fileSize / bufferLength + 1;
	}

	bool ReadDigestDataBuffer(FileExecutionState *executionState, DigestDataBuffer& dataBuffer)
	{
		int64_t readRet = executionState->fileAttemptState.osFile->read(dataBuffer.data, dataBuffer.capacity);
		if (readRet >= 0)
		{
			dataBuffer.datalen = (unsigned int)readRet;
		}
		else
		{
			dataBuffer.datalen = 0;
			executionState->fileAttemptState.readFailed = true;
		}

		return !executionState->fileAttemptState.readFailed;
	}

	bool ProcessOpenedFileHashingParallel(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fileSize, bool isSizeCaled,
		unsigned int preferredBufferLength, const HashDigestQueuePlan& digestQueuePlan, FileExecutionState *executionState, ThreadPool *threadPool)
	{
		std::atomic<bool> isFileFinished(false);

		queue<unique_ptr<DigestDataBuffer>> queueDataBuffer;
		mutex mtxQueue;
		condition_variable cvFile;
		condition_variable cvCalc;

		future<void> taskHash = threadPool->enqueue([&]
		{
			while (true)
			{
				unique_ptr<DigestDataBuffer> ptrDataBufCalc;

				{
					unique_lock<mutex> lock(mtxQueue);
					cvCalc.wait(lock, [&]
					{
						return (!queueDataBuffer.empty() || isFileFinished.load() || ShouldStopHashExecution(*executionContext));
					});

					if (queueDataBuffer.empty() && isFileFinished.load())
					{
						break;
					}

					if (!queueDataBuffer.empty())
					{
						ptrDataBufCalc = std::move(queueDataBuffer.front());
						queueDataBuffer.pop();
					}
				}
				cvFile.notify_all();

				if (ShouldStopHashExecution(*executionContext))
				{
					break;
				}

				if (!ptrDataBufCalc)
				{
					continue;
				}

				UpdateDigestContextsParallel(digestUpdateRequest, executionState->hashContexts, ptrDataBufCalc->data, ptrDataBufCalc->datalen, threadPool);
				UpdateHashExecutionProgress(executionContext, fileSize, isSizeCaled, ptrDataBufCalc->datalen, &executionState->progressState);
			}
			cvFile.notify_all();
		});

		do
		{
			if (ShouldStopHashExecution(*executionContext))
			{
				break;
			}

			unique_ptr<DigestDataBuffer> ptrDataBufFile = make_unique<DigestDataBuffer>(preferredBufferLength);
			if (ReadDigestDataBuffer(executionState, *ptrDataBufFile))
			{
				isFileFinished.store(ptrDataBufFile->datalen < ptrDataBufFile->capacity);

				unique_lock<mutex> lock(mtxQueue);
				cvFile.wait(lock, [&]
				{
					return (queueDataBuffer.size() < GetHashDigestQueueMaxBufferedChunkCount(digestQueuePlan) || ShouldStopHashExecution(*executionContext));
				});
				queueDataBuffer.push(std::move(ptrDataBufFile));
			}
			cvCalc.notify_all();
		}
		while (!isFileFinished.load() && !executionState->fileAttemptState.readFailed);

		isFileFinished.store(true);
		cvCalc.notify_all();
		taskHash.wait();

		return ShouldStopHashExecution(*executionContext);
	}
}
