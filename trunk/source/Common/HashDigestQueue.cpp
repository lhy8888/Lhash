#include "stdafx.h"

#include "Common/HashEngineInternal.h"
#include "Common/ThreadPool.h"
#include "Common/CheckedArithmetic.h"

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
		if (fileSize == 0)
		{
			return 1;
		}

		uint64_t adjustedFileSize = SaturatingAddUInt64(fileSize, static_cast<uint64_t>(bufferLength) - 1);
		return adjustedFileSize / bufferLength;
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

		const size_t maxBufferedChunkCount = GetHashDigestQueueMaxBufferedChunkCount(digestQueuePlan);
		vector<unique_ptr<DigestDataBuffer>> digestBufferPool;
		queue<size_t> availableBufferIndices;
		queue<size_t> queuedBufferIndices;
		for (size_t bufferIndex = 0; bufferIndex < maxBufferedChunkCount; ++bufferIndex)
		{
			digestBufferPool.push_back(unique_ptr<DigestDataBuffer>(new DigestDataBuffer(preferredBufferLength)));
			availableBufferIndices.push(bufferIndex);
		}

		mutex mtxQueue;
		condition_variable cvFile;
		condition_variable cvCalc;

		future<void> taskHash = threadPool->enqueue([&]
		{
			while (true)
			{
				size_t bufferIndex = maxBufferedChunkCount;

				{
					unique_lock<mutex> lock(mtxQueue);
					cvCalc.wait(lock, [&]
					{
						return (!queuedBufferIndices.empty() || isFileFinished.load() || ShouldStopHashExecution(*executionContext));
					});

					if (queuedBufferIndices.empty() && isFileFinished.load())
					{
						break;
					}

					if (!queuedBufferIndices.empty())
					{
						bufferIndex = queuedBufferIndices.front();
						queuedBufferIndices.pop();
					}
				}

				if (ShouldStopHashExecution(*executionContext))
				{
					if (bufferIndex < maxBufferedChunkCount)
					{
						unique_lock<mutex> lock(mtxQueue);
						availableBufferIndices.push(bufferIndex);
					}
					cvFile.notify_all();
					break;
				}

				if (bufferIndex >= maxBufferedChunkCount)
				{
					continue;
				}

				DigestDataBuffer& digestDataBuffer = *digestBufferPool[bufferIndex];
				UpdateDigestContextsParallel(digestUpdateRequest, executionState->hashContexts, digestDataBuffer.data, digestDataBuffer.datalen, threadPool);
				UpdateHashExecutionProgress(executionContext, fileSize, isSizeCaled, digestDataBuffer.datalen, &executionState->progressState);

				{
					unique_lock<mutex> lock(mtxQueue);
					availableBufferIndices.push(bufferIndex);
				}
				cvFile.notify_all();
			}
			cvFile.notify_all();
		});

		do
		{
			if (ShouldStopHashExecution(*executionContext))
			{
				break;
			}

			size_t bufferIndex = maxBufferedChunkCount;
			{
				unique_lock<mutex> lock(mtxQueue);
				cvFile.wait(lock, [&]
				{
					return (!availableBufferIndices.empty() || ShouldStopHashExecution(*executionContext));
				});
				if (ShouldStopHashExecution(*executionContext))
				{
					break;
				}

				bufferIndex = availableBufferIndices.front();
				availableBufferIndices.pop();
			}

			DigestDataBuffer& digestDataBuffer = *digestBufferPool[bufferIndex];
			if (ReadDigestDataBuffer(executionState, digestDataBuffer))
			{
				isFileFinished.store(digestDataBuffer.datalen < digestDataBuffer.capacity);

				{
					unique_lock<mutex> lock(mtxQueue);
					queuedBufferIndices.push(bufferIndex);
				}
			}
			else
			{
				unique_lock<mutex> lock(mtxQueue);
				availableBufferIndices.push(bufferIndex);
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
