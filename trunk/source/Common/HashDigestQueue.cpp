#include "stdafx.h"

#include "Common/HashEngineInternal.h"
#include "Common/ThreadPool.h"
#include "Common/CheckedArithmetic.h"

#include <exception>
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
		bool isFileFinished = false;
		exception_ptr pipelineError;

		// Both wait predicates observe terminal failures under the queue mutex.
		// In particular, a failed consumer must wake a producer waiting for a buffer.
		auto publishError = [&](exception_ptr error)
		{
			{
				lock_guard<mutex> lock(mtxQueue);
				if (!pipelineError)
				{
					pipelineError = error;
				}
			}
			cvFile.notify_all();
			cvCalc.notify_all();
		};

		future<void> taskHash = threadPool->enqueue([&]
		{
			try
			{
				while (true)
				{
					size_t bufferIndex;
					{
						unique_lock<mutex> lock(mtxQueue);
						cvCalc.wait(lock, [&]
						{
							return pipelineError || !queuedBufferIndices.empty() || isFileFinished || ShouldStopHashExecution(*executionContext);
						});
						if (pipelineError || ShouldStopHashExecution(*executionContext) || queuedBufferIndices.empty())
						{
							break;
						}
						bufferIndex = queuedBufferIndices.front();
						queuedBufferIndices.pop();
					}

					DigestDataBuffer& digestDataBuffer = *digestBufferPool[bufferIndex];
					UpdateDigestContextsParallel(digestUpdateRequest, executionState->hashContexts, digestDataBuffer.data, digestDataBuffer.datalen, threadPool);
					UpdateHashExecutionProgress(executionContext, fileSize, isSizeCaled, digestDataBuffer.datalen, &executionState->progressState);

					{
						lock_guard<mutex> lock(mtxQueue);
						availableBufferIndices.push(bufferIndex);
					}
					cvFile.notify_all();
				}
			}
			catch (...)
			{
				publishError(current_exception());
			}
			cvFile.notify_all();
		});

		try
		{
			while (!ShouldStopHashExecution(*executionContext))
			{
				size_t bufferIndex;
				{
					unique_lock<mutex> lock(mtxQueue);
					cvFile.wait(lock, [&]
					{
						return pipelineError || !availableBufferIndices.empty() || ShouldStopHashExecution(*executionContext);
					});
					if (pipelineError || ShouldStopHashExecution(*executionContext))
					{
						break;
					}
					bufferIndex = availableBufferIndices.front();
					availableBufferIndices.pop();
				}

				DigestDataBuffer& digestDataBuffer = *digestBufferPool[bufferIndex];
				if (!ReadDigestDataBuffer(executionState, digestDataBuffer) || digestDataBuffer.datalen == 0)
				{
					// A short positive read is not EOF. Only a zero-byte read ends
					// the stream; already queued chunks are still consumed below.
					break;
				}
				{
					lock_guard<mutex> lock(mtxQueue);
					if (pipelineError || ShouldStopHashExecution(*executionContext))
					{
						break;
					}
					queuedBufferIndices.push(bufferIndex);
				}
				cvCalc.notify_all();
			}
		}
		catch (...)
		{
			publishError(current_exception());
		}

		{
			lock_guard<mutex> lock(mtxQueue);
			isFileFinished = true;
		}
		cvCalc.notify_all();
		// Always wait before destroying the queues, buffers and referenced state,
		// including when reading or enqueueing a chunk threw on the producer.
		taskHash.get();
		if (pipelineError)
		{
			rethrow_exception(pipelineError);
		}

		return ShouldStopHashExecution(*executionContext);
	}
}
