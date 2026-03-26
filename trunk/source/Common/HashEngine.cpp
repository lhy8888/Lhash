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

#include "Common/strhelper.h"
#include "Common/HashEngineObserver.h"
#include "Common/ThreadDataAccess.h"
#include "Common/ResultDataAccess.h"
#include "Common/ResultDigestAccess.h"

#if defined (_WIN32)
#include "WinCommon/WindowsComm.h"
#if (defined (FHASH_UWP_LIB) || defined(FHASH_WUI_LIB))
#include "WinCommon/FileVersionHelper.h"
#endif
#endif

#include "OsUtils/OsFile.h"
#include "OsUtils/OsThread.h"

#include "Algorithms/MD5.h"
#include "Algorithms/SHA1.h"
#include "Algorithms/sha256.h"
#include "Algorithms/sha512.h"

using namespace std;
using namespace sunjwbase;

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
	MD5Update(mdContext, inBuf, inLen); // MD5 update
}

static void SHA1UpdateWrapper(CSHA1 *sha1, unsigned char *data, unsigned int len)
{
	sha1->Update(data, len); // SHA1 update
}

static void SHA256UpdateWrapper(struct sha256_ctx *ctx, const unsigned char *buffer, uint32_t length)
{
	sha256_update(ctx, buffer, length); // SHA256 update
}

static void SHA512UpdateWrapper(SHA512_CTX *context, void *datain, size_t len)
{
	SHA512_Update(context, datain, len); // SHA512 update
}

struct FileProgressState
{
	uint64_t finishedSize;
	uint64_t finishedSizeWhole;
	int position;
	int positionWhole;
};

struct FileAttemptState
{
	const TCHAR *path;
	OsFile *osFile;
	tstring fileVersion;
	bool readFailed;
	bool isFileOpened;
	const TCHAR *openErrorText;
};

static void UpdateProgressWrapper(uint64_t fsize, uint64_t totalSize, bool isSizeCaled, unsigned int dataBufLen,
	HashEngineObserver *observer, FileProgressState *progressState)
{
	// update progress
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
		observer->onFileProgress(positionNew);
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
		observer->onTotalProgress(progressState->positionWhole);
	}
}

static void AccumulatePreScannedFileSize(ThreadData *thrdData, ULLongVector& fSizes, uint32_t fileIndex)
{
	uint64_t fSize = 0;

	const TCHAR *path;
	path = GetThreadDataFullPath(*thrdData, fileIndex).c_str();
	OsFile osFile(path);
	if (osFile.openRead())
	{
		fSize = osFile.getLength();//fsize=status.m_size; // Fix 4GB file
		osFile.close();
	}

	fSizes[fileIndex] = fSize;
	AddThreadDataTotalSize(*thrdData, fSize);
}

static bool TryPreScanSmallBatchFileSizes(ThreadData *thrdData, ULLongVector& fSizes, bool *wasCancelled)
{
	if (GetThreadDataFileCount(*thrdData) < 200) // not too many
	{
		VisitThreadDataInputFiles(*thrdData, [&](uint32_t fileIndex, const tstring& fullPath)
		{
			(void)fullPath;
			if (ShouldStopThreadData(*thrdData))
			{
				*wasCancelled = true;
				return false;
			}

			AccumulatePreScannedFileSize(thrdData, fSizes, fileIndex);
			return true;
		});

		return true;
	}

	return false;
}

static bool PrepareHashingWork(ThreadData *thrdData, HashEngineObserver *observer, ULLongVector& fSizes, bool *wasCancelled)
{
	observer->onPreparing();
	bool isSizeCaled = TryPreScanSmallBatchFileSizes(thrdData, fSizes, wasCancelled);
	if (*wasCancelled)
	{
		return isSizeCaled;
	}

	observer->onPreparationFinished();
	return isSizeCaled;
}

static void InitializeFileAttemptState(const TCHAR *path, OsFile *osFile, FileAttemptState *fileAttemptState)
{
	fileAttemptState->path = path;
	fileAttemptState->osFile = osFile;
	fileAttemptState->fileVersion.clear();
	fileAttemptState->readFailed = false;
	fileAttemptState->isFileOpened = false;
	fileAttemptState->openErrorText = NULL;
}

static bool OpenFileForHashing(FileAttemptState *fileAttemptState, void *openErrorBuffer)
{
	fileAttemptState->readFailed = false;
	fileAttemptState->openErrorText = (const TCHAR *)openErrorBuffer;
	fileAttemptState->isFileOpened = fileAttemptState->osFile->openReadScan(openErrorBuffer);
	return fileAttemptState->isFileOpened;
}

static void ResetFileProgressState(FileProgressState *progressState)
{
	progressState->finishedSize = 0;
	progressState->position = 0;
}

static void EmitPathResult(HashEngineObserver *observer, ResultData& result)
{
	SetResultState(result, RESULT_PATH);
	observer->onFileStarted(result);
}

static ResultData& BeginFileResult(ThreadData *thrdData, HashEngineObserver *observer, const tstring& path)
{
	ResultData& result = AppendThreadDataResult(*thrdData);

	ResetResultData(result);
	SetResultState(result, RESULT_NONE);
	SetResultPath(result, path);

	EmitPathResult(observer, result);
	return result;
}

static uint64_t PrepareFileMetaResult(ThreadData *thrdData, HashEngineObserver *observer, ResultData& result,
	OsFile& osFile, const TCHAR *path, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, tstring& tstrFileVersion)
{
	SetResultModifiedDate(result, osFile.getModifiedTimeFormat());

	uint64_t fsize = osFile.getLength(); // fix 4GB file
	SetResultSize(result, fsize);

	if (!isSizeCaled) // not calculated size
	{
		AddThreadDataTotalSize(*thrdData, fsize);
	}
	else
	{
		ReplaceThreadDataCountedFileSize(*thrdData, fSizes[fileIndex], fsize); // fix total size
		fSizes[fileIndex] = fsize; // fix file size
	}

#if defined (_WIN32)
	// get file version //
#if (defined (FHASH_UWP_LIB) || defined(FHASH_WUI_LIB))
	WindowsComm::FileVersionHelper fvHelper(osFile);
	tstrFileVersion = fvHelper.Find();
		SetResultVersion(result, tstrFileVersion);
		osFile.seek(0, OsFile::OsFileSeekFrom::OF_SEEK_BEGIN); // reset offset
#else
		tstrFileVersion = WindowsComm::GetExeFileVersion((TCHAR *)path);
		SetResultVersion(result, tstrFileVersion);
#endif
#endif

	EmitMetaResult(observer, result);
	return fsize;
}

static void InitializeFileHashing(HashEngineObserver *observer, FileHashContexts *hashContexts)
{
	MD5Init(&hashContexts->mdContext, 0); // MD5 init
	hashContexts->sha1.Reset(); // SHA1 init
	sha256_init(&hashContexts->sha256Ctx); // SHA256 init
	SHA512_Init(&hashContexts->sha512Ctx); // SHA512 init

	observer->onFileProgress(0);
}

static void UpdateWholeProgressAfterFile(HashEngineObserver *observer, ThreadData *thrdData, bool isSizeCaled, uint32_t fileIndex)
{
	if (!isSizeCaled)
	{
		if (GetThreadDataFileCount(*thrdData) == 0)
		{
			observer->onTotalProgress(0);
		}
		else
		{
			int progressMax = observer->progressMax();
			observer->onTotalProgress((fileIndex + 1) * progressMax / (GetThreadDataFileCount(*thrdData)));
		}
	}
}

typedef ResultDigestStorage FinalizedDigestBundle;

struct FileHashContexts
{
	MD5_CTX mdContext; // MD5 context
	CSHA1 sha1; // SHA1 object
	SHA256_CTX sha256Ctx; // SHA256 context
	SHA512_CTX sha512Ctx; // SHA512 context
	uint8_t digestSHA512[SHA512_DIGEST_LENGTH];
};

struct FileExecutionState
{
	FileProgressState progressState;
	FileAttemptState fileAttemptState;
	FileHashContexts hashContexts;
	FinalizedDigestBundle digestBundle;
};

static const tstring& GetFinalizedDigestValue(const FinalizedDigestBundle& digestBundle, ResultDigestType digestType)
{
	return GetDigestStorageValue(digestBundle, digestType);
}

static void SetFinalizedDigestValue(FinalizedDigestBundle& digestBundle, ResultDigestType digestType, const tstring& digestValue)
{
	SetDigestStorageValue(digestBundle, digestType, digestValue);
}

static void PopulateDigestResult(ResultData& result, const FinalizedDigestBundle& digestBundle)
{
	for (int index = 0; index < GetResultDigestCount(); index++)
	{
		ResultDigestType digestType = GetResultDigestTypeAt(index);
		SetResultDigest(result, digestType, GetFinalizedDigestValue(digestBundle, digestType));
	}
}

static void FinalizeDigestStrings(FileHashContexts& hashContexts, FinalizedDigestBundle& digestBundle)
{
	MD5Final(&hashContexts.mdContext); // MD5 final
	hashContexts.sha1.Final(); // SHA1 final
	sha256_final(&hashContexts.sha256Ctx); // SHA256 final
	SHA512_Final(hashContexts.digestSHA512, &hashContexts.sha512Ctx); // SHA256 final

	char chHashBuff[1024] = {0};
	char strSHA1[256] = {0};
	string strSHA256;
	string strSHA512;

	// MD5
#if defined (_WIN32)
	sprintf_s(chHashBuff, 1024,
					"%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X",
					hashContexts.mdContext.digest[0],
					hashContexts.mdContext.digest[1],
					hashContexts.mdContext.digest[2],
					hashContexts.mdContext.digest[3],
					hashContexts.mdContext.digest[4],
					hashContexts.mdContext.digest[5],
					hashContexts.mdContext.digest[6],
					hashContexts.mdContext.digest[7],
					hashContexts.mdContext.digest[8],
					hashContexts.mdContext.digest[9],
					hashContexts.mdContext.digest[10],
					hashContexts.mdContext.digest[11],
					hashContexts.mdContext.digest[12],
					hashContexts.mdContext.digest[13],
					hashContexts.mdContext.digest[14],
					hashContexts.mdContext.digest[15]);
#else
	snprintf(chHashBuff, 1024,
		  "%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X",
		  hashContexts.mdContext.digest[0],
		  hashContexts.mdContext.digest[1],
		  hashContexts.mdContext.digest[2],
		  hashContexts.mdContext.digest[3],
		  hashContexts.mdContext.digest[4],
		  hashContexts.mdContext.digest[5],
		  hashContexts.mdContext.digest[6],
		  hashContexts.mdContext.digest[7],
		  hashContexts.mdContext.digest[8],
		  hashContexts.mdContext.digest[9],
		  hashContexts.mdContext.digest[10],
		  hashContexts.mdContext.digest[11],
		  hashContexts.mdContext.digest[12],
		  hashContexts.mdContext.digest[13],
		  hashContexts.mdContext.digest[14],
		  hashContexts.mdContext.digest[15]);
#endif
	SetFinalizedDigestValue(digestBundle, RESULT_DIGEST_MD5, strtotstr(string(chHashBuff)));

	// SHA1
	hashContexts.sha1.ReportHash(strSHA1, CSHA1::REPORT_HEX);
	SetFinalizedDigestValue(digestBundle, RESULT_DIGEST_SHA1, strtotstr(string(strSHA1)));

	// SHA256
	sha256_digest(&hashContexts.sha256Ctx, &strSHA256);
	SetFinalizedDigestValue(digestBundle, RESULT_DIGEST_SHA256, strtotstr(strSHA256));

	// SHA512
	for (int p = 0; p < SHA512_DIGEST_LENGTH; p++)
	{
		char buf[8] = { 0 };
#if defined (_WIN32)
		sprintf_s(buf, 8, "%02X", hashContexts.digestSHA512[p]);
#else
		snprintf(buf, 8, "%02X", hashContexts.digestSHA512[p]);
#endif
		strSHA512.append(std::string(buf));
	}
	SetFinalizedDigestValue(digestBundle, RESULT_DIGEST_SHA512, strtotstr(strSHA512));
}

static void FinishFileProcessing(HashEngineObserver *observer);

static void CompleteSuccessfulFileHashing(HashEngineObserver *observer, ThreadData *thrdData, ResultData& result, uint32_t fileIndex, bool isSizeCaled, bool uppercase,
	FileExecutionState& executionState)
{
	observer->onFileCalculated();

	FinalizeDigestStrings(executionState.hashContexts, executionState.digestBundle);
	UpdateWholeProgressAfterFile(observer, thrdData, isSizeCaled, fileIndex);

	executionState.fileAttemptState.osFile->close();
	//Calculating ends

	PopulateDigestResult(result, executionState.digestBundle);
	EmitHashResult(observer, result, uppercase);
}

static void CompleteOpenedFileAttempt(HashEngineObserver *observer, ThreadData *thrdData, ResultData& result, uint32_t fileIndex, bool isSizeCaled,
	FileExecutionState& executionState)
{
	if (executionState.fileAttemptState.readFailed)
	{
		executionState.fileAttemptState.osFile->close();
		EmitReadFileError(observer, result);
	}
	else
	{
		CompleteSuccessfulFileHashing(observer, thrdData, result, fileIndex, isSizeCaled, GetThreadDataUppercase(*thrdData), executionState);
	}

	FinishFileProcessing(observer);
}

static void EmitMetaResult(HashEngineObserver *observer, ResultData& result)
{
	SetResultState(result, RESULT_META);
	observer->onFileMetaReady(result);
}

static void EmitHashResult(HashEngineObserver *observer, ResultData& result, bool uppercase)
{
	SetResultState(result, RESULT_ALL);
	observer->onFileHashReady(result, uppercase);
}

static void EmitErrorResult(HashEngineObserver *observer, ResultData& result)
{
	SetResultState(result, RESULT_ERROR);
	observer->onFileFailed(result);
}

static void EmitErrorMessageResult(HashEngineObserver *observer, ResultData& result, const tstring& errorText)
{
	SetResultError(result, errorText);
	EmitErrorResult(observer, result);
}

static void EmitOpenFileError(HashEngineObserver *observer, ResultData& result, const TCHAR *errorText)
{
	EmitErrorMessageResult(observer, result, tstring(errorText));
}

static void EmitReadFileError(HashEngineObserver *observer, ResultData& result)
{
	EmitErrorMessageResult(observer, result, strtotstr(string("Failed to read file while hashing.")));
}

static void FinishFileProcessing(HashEngineObserver *observer)
{
	observer->onFileFinished();
}

static void CompleteFileAttempt(HashEngineObserver *observer, ThreadData *thrdData, ResultData& result, uint32_t fileIndex, bool isSizeCaled,
	FileExecutionState& executionState)
{
	if (executionState.fileAttemptState.isFileOpened)
	{
		CompleteOpenedFileAttempt(observer, thrdData, result, fileIndex, isSizeCaled, executionState);
	}
	else
	{
		EmitOpenFileError(observer, result, executionState.fileAttemptState.openErrorText);
		FinishFileProcessing(observer);
	}
}

static int CancelHashing(ThreadData *thrdData, HashEngineObserver *observer)
{
	SetThreadDataWorking(*thrdData, false);
	observer->onCancelled();
	return 0;
}

static int CompleteHashing(ThreadData *thrdData, HashEngineObserver *observer)
{
	observer->onCompleted();
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

static bool ProcessOpenedFileHashing(ThreadData *thrdData, HashEngineObserver *observer, ResultData& result, uint32_t fileIndex,
	bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
	, ThreadPool *threadPool
#endif
)
{
	InitializeFileHashing(observer, &executionState->hashContexts);

	uint64_t fsize = PrepareFileMetaResult(thrdData, observer, result, *executionState->fileAttemptState.osFile, executionState->fileAttemptState.path, isSizeCaled, fSizes, fileIndex, executionState->fileAttemptState.fileVersion);
	uint64_t times = CalculateFileChunkIterations(fsize);
	(void)times;

	bool isFileFinished = false;

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
	queue<unique_ptr<DataBuffer>> queueDataBuffer;
	mutex mtxQueue;
	condition_variable cvFile;
	condition_variable cvCalc;

	// create hashWorker thread
	future<void> taskHash = threadPool->enqueue([&] // capture by ref
	{
		while (true)
		{
			unique_ptr<DataBuffer> ptrDataBufCalc;

			{
				unique_lock<mutex> lock(mtxQueue);
				cvCalc.wait(lock, [&]
				{
					// not to wait
					return (!queueDataBuffer.empty() || isFileFinished || thrdData->stop);
				});

				if (queueDataBuffer.empty() && isFileFinished)
					break;

				if (!queueDataBuffer.empty())
				{
					// pop one
					ptrDataBufCalc = std::move(queueDataBuffer.front());
					queueDataBuffer.pop();
				}
			}
			cvFile.notify_all();

			if (thrdData->stop)
				break;

			if (!ptrDataBufCalc)
				continue; // no data

			// multi threads
			future<void> taskSHA512Update = threadPool->enqueue(SHA512UpdateWrapper, &executionState->hashContexts.sha512Ctx, ptrDataBufCalc->data, ptrDataBufCalc->datalen);
			future<void> taskSHA256Update = threadPool->enqueue(SHA256UpdateWrapper, &executionState->hashContexts.sha256Ctx, ptrDataBufCalc->data, ptrDataBufCalc->datalen);
			future<void> taskSHA1Update = threadPool->enqueue(SHA1UpdateWrapper, &executionState->hashContexts.sha1, ptrDataBufCalc->data, ptrDataBufCalc->datalen);
			future<void> taskMD5Update = threadPool->enqueue(MD5UpdateWrapper, &executionState->hashContexts.mdContext, ptrDataBufCalc->data, ptrDataBufCalc->datalen);

			taskSHA512Update.wait();
			taskSHA256Update.wait();
			taskSHA1Update.wait();
			taskMD5Update.wait();

			// update progress
			UpdateProgressWrapper(fsize, GetThreadDataTotalSize(*thrdData), isSizeCaled, ptrDataBufCalc->datalen,
				observer, &executionState->progressState);
		}
		// calc exit
		cvFile.notify_all();
	});
#else
	DataBuffer databuf;
#endif

	do
	{
		if (thrdData->stop)
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
				// limit to 4 DataBuffer
				return (queueDataBuffer.size() < 4 || thrdData->stop);
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
			// single thread
			MD5UpdateWrapper(&executionState->hashContexts.mdContext, databuf.data, databuf.datalen); // MD5 update
			SHA1UpdateWrapper(&executionState->hashContexts.sha1, databuf.data, databuf.datalen); // SHA1 update
			SHA256UpdateWrapper(&executionState->hashContexts.sha256Ctx, databuf.data, databuf.datalen); // SHA256 update
			SHA512UpdateWrapper(&executionState->hashContexts.sha512Ctx, databuf.data, databuf.datalen); // SHA512 update

			// update progress
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

	if (thrdData->stop)
	{
		executionState->fileAttemptState.osFile->close();
		return true;
	}

	return false;
}

static ResultData& BeginFileHashAttempt(ThreadData *thrdData, HashEngineObserver *observer, const tstring& path, FileExecutionState *executionState, const TCHAR **resultPath)
{
	YieldHashThread();
	ResetFileProgressState(&executionState->progressState);

	ResultData& result = BeginFileResult(thrdData, observer, path);
	*resultPath = GetResultPath(result).c_str();
	return result;
}

// working thread
int WINAPI HashThreadFunc(void *param)
{
	ThreadData *thrdData = (ThreadData *)param;
	HashEngineObserver *observer = GetThreadDataObserver(*thrdData);

	SetThreadDataWorking(*thrdData, true);

	ResetThreadDataTotalSize(*thrdData);
	bool isSizeCaled = false;
	ULLongVector fSizes(GetThreadDataFileCount(*thrdData));

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
	// Create thread pool with 5 threads
	ThreadPool threadPool(5);
#endif

	bool wasCancelled = false;
	isSizeCaled = PrepareHashingWork(thrdData, observer, fSizes, &wasCancelled);
	if (wasCancelled)
	{
		return CancelHashing(thrdData, observer);
	}

	FileExecutionState executionState = { 0 };

	// loop all files
	bool completedAllFiles = VisitThreadDataInputFiles(*thrdData, [&](uint32_t fileIndex, const tstring& fullPath)
	{
		if (ShouldStopThreadData(*thrdData))
		{
			return false;
		}

		// Declaration for calculator
		const TCHAR *path = fullPath.c_str();
		// Declaration for calculator

		ResultData& result = BeginFileHashAttempt(thrdData, observer, fullPath, &executionState, &path);

		//Calculating begins
#if defined (_WIN32)
		// CFileException fExc;
		TCHAR fExc[OsFile::ERR_MSG_BUFFER_LEN] = { 0 };
#else
		char fExc[OsFile::ERR_MSG_BUFFER_LEN] = { 0 };
#endif
		OsFile osFile(path);
		InitializeFileAttemptState(path, &osFile, &executionState.fileAttemptState);
		OpenFileForHashing(&executionState.fileAttemptState, (void *)&fExc);
		if (executionState.fileAttemptState.isFileOpened)
		{
			bool wasStopped = ProcessOpenedFileHashing(thrdData, observer, result, fileIndex, isSizeCaled, fSizes, &executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
				, &threadPool
#endif
			);
			if (wasStopped)
			{
				return false;
			}
		} // end if(File.Open(path, CFile::modeRead|CFile::shareDenyWrite, &ex))

		CompleteFileAttempt(observer, thrdData, result, fileIndex, isSizeCaled, executionState);
		return true;
	});

	if (!completedAllFiles)
	{
		return CancelHashing(thrdData, observer);
	}

	return CompleteHashing(thrdData, observer);
}
