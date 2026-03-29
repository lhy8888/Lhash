#include "stdafx.h"

#include "Common/HashEngineInternal.h"

using namespace sunjwbase;

namespace HashEngineInternal
{
	void AccumulatePreScannedFileSize(ThreadData *thrdData, const HashRequest& request, ULLongVector& fSizes, uint32_t fileIndex)
	{
		uint64_t fSize = 0;

		const TCHAR *path = GetHashRequestFileAt(request, fileIndex).c_str();
		OsFile osFile(path);
		if (osFile.openRead())
		{
			fSize = osFile.getLength();
			osFile.close();
		}

		fSizes[fileIndex] = fSize;
		AddThreadDataTotalSize(*thrdData, fSize);
	}

	bool TryPreScanSmallBatchFileSizes(ThreadData *thrdData, const HashRequest& request, ULLongVector& fSizes, bool *wasCancelled)
	{
		if (GetHashRequestFileCount(request) < 200)
		{
			VisitHashRequestFiles(request, [&](uint32_t fileIndex, const tstring& fullPath)
			{
				(void)fullPath;
				if (ShouldStopThreadData(*thrdData))
				{
					*wasCancelled = true;
					return false;
				}

				AccumulatePreScannedFileSize(thrdData, request, fSizes, fileIndex);
				return true;
			});

			return true;
		}

		return false;
	}

	bool PrepareHashingWork(ThreadData *thrdData, const HashRequest& request, HashProgressSink *observer, ULLongVector& fSizes, bool *wasCancelled)
	{
		observer->onProgressEvent(CreatePreparingProgressEvent());
		bool isSizeCaled = TryPreScanSmallBatchFileSizes(thrdData, request, fSizes, wasCancelled);
		if (*wasCancelled)
		{
			return isSizeCaled;
		}

		observer->onProgressEvent(CreatePreparationFinishedProgressEvent());
		return isSizeCaled;
	}

	void InitializeFileAttemptState(const TCHAR *path, OsFile *osFile, FileAttemptState *fileAttemptState)
	{
		fileAttemptState->path = path;
		fileAttemptState->osFile = osFile;
		fileAttemptState->fileVersion.clear();
		fileAttemptState->readFailed = false;
		fileAttemptState->isFileOpened = false;
		fileAttemptState->openErrorText = NULL;
	}

	bool OpenFileForHashing(FileAttemptState *fileAttemptState, void *openErrorBuffer)
	{
		fileAttemptState->readFailed = false;
		fileAttemptState->openErrorText = (const TCHAR *)openErrorBuffer;
		fileAttemptState->isFileOpened = fileAttemptState->osFile->openReadScan(openErrorBuffer);
		return fileAttemptState->isFileOpened;
	}

	void ResetFileProgressState(FileProgressState *progressState)
	{
		progressState->finishedSize = 0;
		progressState->position = 0;
	}

	void EmitPathResult(HashProgressSink *observer, ResultData& result)
	{
		SetResultState(result, RESULT_PATH);
		observer->onProgressEvent(CreateFileStartedProgressEvent(result));
	}

	ResultData& BeginFileResult(ThreadData *thrdData, HashProgressSink *observer, const tstring& path)
	{
		ResultData& result = AppendThreadDataResult(*thrdData);

		ResetResultData(result);
		SetResultState(result, RESULT_NONE);
		SetResultPath(result, path);

		EmitPathResult(observer, result);
		return result;
	}

	ResultData& BeginFileHashAttempt(ThreadData *thrdData, HashProgressSink *observer, const tstring& path, FileExecutionState *executionState, const TCHAR **resultPath)
	{
		ResetFileProgressState(&executionState->progressState);

		ResultData& result = BeginFileResult(thrdData, observer, path);
		*resultPath = GetResultPath(result).c_str();
		return result;
	}
}
