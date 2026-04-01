#include "stdafx.h"

#include "Common/HashEngineInternal.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
#include "Common/ThreadPool.h"
#endif

#include <condition_variable>
#include <future>
#include <memory>
#include <mutex>
#include <queue>

using namespace std;

namespace HashEngineInternal
{
	DigestDataBuffer::DigestDataBuffer():datalen(0), data(NULL)
	{
		data = new unsigned char[DigestDataBuffer::preflen];
	}

	DigestDataBuffer::~DigestDataBuffer()
	{
		delete[] data;
		datalen = 0;
	}

	unsigned int DigestDataBuffer::preflen = 1048576; // 2^20

	unsigned int GetDigestDataBufferPreferredLength()
	{
		return DigestDataBuffer::preflen;
	}

	uint64_t CalculateFileChunkIterations(uint64_t fileSize)
	{
		return fileSize / DigestDataBuffer::preflen + 1;
	}

	bool ReadDigestDataBuffer(FileExecutionState *executionState, DigestDataBuffer& dataBuffer)
	{
		int64_t readRet = executionState->fileAttemptState.osFile->read(dataBuffer.data, DigestDataBuffer::preflen);
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

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
	bool ProcessOpenedFileHashingParallel(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fileSize, bool isSizeCaled,
		FileExecutionState *executionState, ThreadPool *threadPool)
	{
		bool isFileFinished = false;

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
						return (!queueDataBuffer.empty() || isFileFinished || ShouldStopHashExecution(*executionContext));
					});

					if (queueDataBuffer.empty() && isFileFinished)
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

			unique_ptr<DigestDataBuffer> ptrDataBufFile = make_unique<DigestDataBuffer>();
			if (ReadDigestDataBuffer(executionState, *ptrDataBufFile))
			{
				isFileFinished = (ptrDataBufFile->datalen < DigestDataBuffer::preflen);

				unique_lock<mutex> lock(mtxQueue);
				cvFile.wait(lock, [&]
				{
					return (queueDataBuffer.size() < 4 || ShouldStopHashExecution(*executionContext));
				});
				queueDataBuffer.push(std::move(ptrDataBufFile));
			}
			cvCalc.notify_all();
		}
		while (!isFileFinished && !executionState->fileAttemptState.readFailed);

		isFileFinished = true;
		cvCalc.notify_all();
		taskHash.wait();

		return ShouldStopHashExecution(*executionContext);
	}
#endif
}
