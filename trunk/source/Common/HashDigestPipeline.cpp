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
using namespace sunjwbase;

namespace HashEngineInternal
{
	class DataBuffer
	{
	public:
		DataBuffer():datalen(0), data(NULL)
		{
			data = new unsigned char[DataBuffer::preflen];
		}

		~DataBuffer()
		{
			delete[] data;
			datalen = 0;
		}

		static unsigned int preflen;

		unsigned int datalen;
		unsigned char *data;
	};

	unsigned int DataBuffer::preflen = 1048576; // 2^20

	static void UpdateProgressWrapper(HashExecutionContext *executionContext, uint64_t fsize, bool isSizeCaled, unsigned int dataBufLen,
		FileProgressState *progressState)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		progressState->finishedSize += dataBufLen;

		int progressMax = observer->progressMax();

		int positionNew;
		if (fsize == 0)
		{
			positionNew = progressMax;
		}
		else
		{
			positionNew = (int)(progressMax * progressState->finishedSize / fsize);
		}

		if (positionNew > progressState->position)
		{
			observer->onProgressEvent(CreateFileProgressEvent(positionNew));
			progressState->position = positionNew;
		}

		progressState->finishedSizeWhole += dataBufLen;
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

	uint64_t CalculateFileChunkIterations(uint64_t fsize)
	{
		return fsize / DataBuffer::preflen + 1;
	}

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

		bool isFileFinished = false;

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		queue<unique_ptr<DataBuffer>> queueDataBuffer;
		mutex mtxQueue;
		condition_variable cvFile;
		condition_variable cvCalc;

		future<void> taskHash = threadPool->enqueue([&]
		{
			while (true)
			{
				unique_ptr<DataBuffer> ptrDataBufCalc;

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

				UpdateProgressWrapper(executionContext, fsize, isSizeCaled, ptrDataBufCalc->datalen, &executionState->progressState);
			}
			cvFile.notify_all();
		});
#else
		DataBuffer databuf;
#endif

		do
		{
			if (ShouldStopHashExecution(*executionContext))
			{
				break;
			}

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
			unique_ptr<DataBuffer> ptrDataBufFile = make_unique<DataBuffer>();
			int64_t readRet = executionState->fileAttemptState.osFile->read(ptrDataBufFile->data, DataBuffer::preflen);
			if (readRet >= 0)
			{
				ptrDataBufFile->datalen = (unsigned int)readRet;
			}
			else
			{
				ptrDataBufFile->datalen = 0;
				executionState->fileAttemptState.readFailed = true;
			}

			if (!executionState->fileAttemptState.readFailed)
			{
				unique_lock<mutex> lock(mtxQueue);
				cvFile.wait(lock, [&]
				{
					return (queueDataBuffer.size() < 4 || ShouldStopHashExecution(*executionContext));
				});
				queueDataBuffer.push(std::move(ptrDataBufFile));
			}
			cvCalc.notify_all();
#else
			int64_t readRet = executionState->fileAttemptState.osFile->read(databuf.data, DataBuffer::preflen);
			if (readRet >= 0)
			{
				databuf.datalen = (unsigned int)readRet;
			}
			else
			{
				databuf.datalen = 0;
				executionState->fileAttemptState.readFailed = true;
			}

			if (!executionState->fileAttemptState.readFailed)
			{
				UpdateDigestContextsSequential(digestUpdateRequest, executionState->hashContexts, databuf.data, databuf.datalen);

				UpdateProgressWrapper(executionContext, fsize, isSizeCaled, databuf.datalen, &executionState->progressState);
			}

			isFileFinished = (databuf.datalen < DataBuffer::preflen);
#endif
		}
		while (!isFileFinished && !executionState->fileAttemptState.readFailed);

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		isFileFinished = true;
		cvCalc.notify_all();
		taskHash.wait();
#endif

		if (ShouldStopHashExecution(*executionContext))
		{
			executionState->fileAttemptState.osFile->close();
			return true;
		}

		return false;
	}
}
