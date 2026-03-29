#include "stdafx.h"

#include "HashEngine.h"

#include <stdlib.h>

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
#include "Common/ThreadPool.h"
#endif

#if defined (__APPLE__) || defined (__unix)
#include <string.h>
#include <unistd.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <sched.h>
#endif

#include "Common/HashEngineObserver.h"
#include "Common/ThreadDataAccess.h"
#include "Common/ResultDataAccess.h"
#include "Common/ResultDigestAccess.h"
#include "Common/HashEngineInternal.h"

#include "OsUtils/OsThread.h"

using namespace std;
using namespace sunjwbase;
using namespace HashEngineInternal;

class DataBuffer
{
public:
	DataBuffer():datalen(0), data(NULL)
	{ data = new unsigned char[DataBuffer::preflen]; }

	~DataBuffer()
	{ delete[] data; datalen = 0; }

	static unsigned int preflen;

	unsigned int datalen;
	unsigned char *data;
};

unsigned int DataBuffer::preflen = 1048576; // 2^20

static void MD5UpdateWrapper(MD5_CTX *mdContext, unsigned char *inBuf, unsigned int inLen)
{
	MD5Update(mdContext, inBuf, inLen);
}

static void SHA1UpdateWrapper(CSHA1 *sha1, unsigned char *data, unsigned int len)
{
	sha1->Update(data, len);
}

static void SHA256UpdateWrapper(struct sha256_ctx *ctx, const unsigned char *buffer, uint32_t length)
{
	sha256_update(ctx, buffer, length);
}

static void SHA512UpdateWrapper(SHA512_CTX *context, void *datain, size_t len)
{
	SHA512_Update(context, datain, len);
}

static void UpdateProgressWrapper(uint64_t fsize, uint64_t totalSize, bool isSizeCaled, unsigned int dataBufLen,
	HashEngineObserver *observer, FileProgressState *progressState)
{
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

static int CancelHashing(ThreadData *thrdData, HashEngineObserver *observer)
{
	SetThreadDataWorking(*thrdData, false);
	observer->onProgressEvent(CreateCancelledProgressEvent());
	return 0;
}

static int CompleteHashing(ThreadData *thrdData, HashEngineObserver *observer)
{
	observer->onProgressEvent(CreateCompletedProgressEvent());
	SetThreadDataWorking(*thrdData, false);
	return 0;
}

static void YieldHashThread()
{
#if defined (_WIN32)
	Sleep(3);
#else
	sched_yield();
#endif
}

static uint64_t CalculateFileChunkIterations(uint64_t fsize)
{
	return fsize / DataBuffer::preflen + 1;
}

static bool ProcessOpenedFileHashing(ThreadData *thrdData, const HashRequest& request, HashEngineObserver *observer, ResultData& result, uint32_t fileIndex,
	bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
	, ThreadPool *threadPool
#endif
)
{
	InitializeFileHashing(request, observer, &executionState->hashContexts);

	uint64_t fsize = PrepareFileMetaResult(thrdData, observer, result, *executionState->fileAttemptState.osFile, executionState->fileAttemptState.path, isSizeCaled, fSizes, fileIndex, executionState->fileAttemptState.fileVersion);
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
					return (!queueDataBuffer.empty() || isFileFinished || ShouldStopThreadData(*thrdData));
				});

				if (queueDataBuffer.empty() && isFileFinished)
					break;

				if (!queueDataBuffer.empty())
				{
					ptrDataBufCalc = std::move(queueDataBuffer.front());
					queueDataBuffer.pop();
				}
			}
			cvFile.notify_all();

			if (ShouldStopThreadData(*thrdData))
				break;

			if (!ptrDataBufCalc)
				continue;

			bool isSha512Enabled = HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA512);
			bool isSha256Enabled = HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA256);
			bool isSha1Enabled = HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA1);
			bool isMd5Enabled = HasHashRequestAlgorithm(request, RESULT_DIGEST_MD5);
			future<void> taskSHA512Update;
			future<void> taskSHA256Update;
			future<void> taskSHA1Update;
			future<void> taskMD5Update;

			if (isSha512Enabled)
			{
				taskSHA512Update = threadPool->enqueue(SHA512UpdateWrapper, &executionState->hashContexts.sha512Ctx, ptrDataBufCalc->data, ptrDataBufCalc->datalen);
			}
			if (isSha256Enabled)
			{
				taskSHA256Update = threadPool->enqueue(SHA256UpdateWrapper, &executionState->hashContexts.sha256Ctx, ptrDataBufCalc->data, ptrDataBufCalc->datalen);
			}
			if (isSha1Enabled)
			{
				taskSHA1Update = threadPool->enqueue(SHA1UpdateWrapper, &executionState->hashContexts.sha1, ptrDataBufCalc->data, ptrDataBufCalc->datalen);
			}
			if (isMd5Enabled)
			{
				taskMD5Update = threadPool->enqueue(MD5UpdateWrapper, &executionState->hashContexts.mdContext, ptrDataBufCalc->data, ptrDataBufCalc->datalen);
			}

			if (isSha512Enabled)
			{
				taskSHA512Update.wait();
			}
			if (isSha256Enabled)
			{
				taskSHA256Update.wait();
			}
			if (isSha1Enabled)
			{
				taskSHA1Update.wait();
			}
			if (isMd5Enabled)
			{
				taskMD5Update.wait();
			}

			UpdateProgressWrapper(fsize, GetThreadDataTotalSize(*thrdData), isSizeCaled, ptrDataBufCalc->datalen,
				observer, &executionState->progressState);
		}
		cvFile.notify_all();
	});
#else
	DataBuffer databuf;
#endif

	do
	{
		if (ShouldStopThreadData(*thrdData))
			break;

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

		isFileFinished = (ptrDataBufFile->datalen < DataBuffer::preflen);

		if (!executionState->fileAttemptState.readFailed)
		{
			unique_lock<mutex> lock(mtxQueue);
			cvFile.wait(lock, [&]
			{
				return (queueDataBuffer.size() < 4 || ShouldStopThreadData(*thrdData));
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
			if (HasHashRequestAlgorithm(request, RESULT_DIGEST_MD5))
			{
				MD5UpdateWrapper(&executionState->hashContexts.mdContext, databuf.data, databuf.datalen);
			}
			if (HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA1))
			{
				SHA1UpdateWrapper(&executionState->hashContexts.sha1, databuf.data, databuf.datalen);
			}
			if (HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA256))
			{
				SHA256UpdateWrapper(&executionState->hashContexts.sha256Ctx, databuf.data, databuf.datalen);
			}
			if (HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA512))
			{
				SHA512UpdateWrapper(&executionState->hashContexts.sha512Ctx, databuf.data, databuf.datalen);
			}

			UpdateProgressWrapper(fsize, GetThreadDataTotalSize(*thrdData), isSizeCaled, databuf.datalen,
				observer, &executionState->progressState);
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

	if (ShouldStopThreadData(*thrdData))
	{
		executionState->fileAttemptState.osFile->close();
		return true;
	}

	return false;
}

int RunHashRequest(ThreadData *thrdData, const HashRequest& request, HashEngineObserver *observer)
{
	SetThreadDataWorking(*thrdData, true);

	ResetThreadDataTotalSize(*thrdData);
	bool isSizeCaled = false;
	ULLongVector fSizes(GetHashRequestFileCount(request));

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
	ThreadPool threadPool(5);
#endif

	bool wasCancelled = false;
	isSizeCaled = PrepareHashingWork(thrdData, request, observer, fSizes, &wasCancelled);
	if (wasCancelled)
	{
		return CancelHashing(thrdData, observer);
	}

	FileExecutionState executionState = { 0 };

	bool completedAllFiles = VisitHashRequestFiles(request, [&](uint32_t fileIndex, const tstring& fullPath)
	{
		if (ShouldStopThreadData(*thrdData))
		{
			return false;
		}

		YieldHashThread();

		const TCHAR *path = fullPath.c_str();

		ResultData& result = BeginFileHashAttempt(thrdData, observer, fullPath, &executionState, &path);

#if defined (_WIN32)
		TCHAR fExc[OsFile::ERR_MSG_BUFFER_LEN] = { 0 };
#else
		char fExc[OsFile::ERR_MSG_BUFFER_LEN] = { 0 };
#endif
		OsFile osFile(path);
		InitializeFileAttemptState(path, &osFile, &executionState.fileAttemptState);
		OpenFileForHashing(&executionState.fileAttemptState, (void *)&fExc);
		if (executionState.fileAttemptState.isFileOpened)
		{
			bool wasStopped = ProcessOpenedFileHashing(thrdData, request, observer, result, fileIndex, isSizeCaled, fSizes, &executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
				, &threadPool
#endif
			);
			if (wasStopped)
			{
				return false;
			}
		}

		CompleteFileAttempt(observer, thrdData, request, result, fileIndex, isSizeCaled, executionState);
		return true;
	});

	if (!completedAllFiles)
	{
		return CancelHashing(thrdData, observer);
	}

	return CompleteHashing(thrdData, observer);
}

int WINAPI HashThreadFunc(void *param)
{
	ThreadData *thrdData = (ThreadData *)param;
	HashEngineObserver *observer = GetThreadDataObserver(*thrdData);
	HashRequest request = CreateHashRequest(*thrdData);

	return RunHashRequest(thrdData, request, observer);
}
