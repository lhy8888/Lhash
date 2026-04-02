#include "stdafx.h"

#include "Common/HashEngineInternal.h"

using namespace sunjwbase;

namespace HashEngineInternal
{
	void AccumulatePreScannedFileSize(HashExecutionContext *executionContext, const HashRequest& request, ULLongVector& fSizes, uint32_t fileIndex)
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
		AddHashExecutionTotalSize(*executionContext, fSize);
	}

	bool TryPreScanSmallBatchFileSizes(HashExecutionContext *executionContext, const HashRequest& request, const HashPreparationPlan& preparationPlan, ULLongVector& fSizes, bool *wasCancelled)
	{
		if (ShouldPreScanHashRequestFileSizes(preparationPlan, request))
		{
			VisitHashRequestFiles(request, [&](uint32_t fileIndex, const tstring& fullPath)
			{
				(void)fullPath;
				if (ShouldStopHashExecution(*executionContext))
				{
					*wasCancelled = true;
					return false;
				}

				AccumulatePreScannedFileSize(executionContext, request, fSizes, fileIndex);
				return true;
			});

			return true;
		}

		return false;
	}

	bool PrepareHashingWork(HashExecutionContext *executionContext, const HashRequest& request, const HashPreparationPlan& preparationPlan, ULLongVector& fSizes, bool *wasCancelled)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		observer->onProgressEvent(CreatePreparingProgressEvent());
		bool isSizeCaled = TryPreScanSmallBatchFileSizes(executionContext, request, preparationPlan, fSizes, wasCancelled);
		if (*wasCancelled)
		{
			return isSizeCaled;
		}

		observer->onProgressEvent(CreatePreparationFinishedProgressEvent());
		return isSizeCaled;
	}

	void EmitPathResult(HashExecutionContext *executionContext, HashResult& result)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		result.state = RESULT_PATH;
		observer->onProgressEvent(CreateFileStartedProgressEvent(result));
	}

	HashResult& BeginFileResult(HashExecutionContext *executionContext, const tstring& path)
	{
		HashResult& result = AppendHashExecutionResult(*executionContext);
		result = HashResult();
		result.path = path;

		EmitPathResult(executionContext, result);
		return result;
	}

	HashResult& BeginFileHashAttempt(HashExecutionContext *executionContext, const tstring& path, FileExecutionState *executionState, const TCHAR **resultPath)
	{
		ResetFileProgressState(&executionState->progressState);

		HashResult& result = BeginFileResult(executionContext, path);
		*resultPath = result.path.c_str();
		return result;
	}
}
