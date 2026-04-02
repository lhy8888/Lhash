#include "stdafx.h"

#include "Common/HashEngineInternal.h"

using namespace sunjwbase;

namespace HashEngineInternal
{
	void AccumulatePreScannedFileSize(HashExecutionContext *executionContext, const HashRequest& request, ULLongVector& fSizes, uint32_t fileIndex)
	{
		const TCHAR *path = GetHashRequestFileAt(request, fileIndex).c_str();
		uint64_t fSize = ResolveHashPreScannedFileSize(path);
		TrackHashPreScannedFileSize(executionContext, fSizes, fileIndex, fSize);
	}

	bool TryPreScanSmallBatchFileSizes(HashExecutionContext *executionContext, const HashRequest& request, const HashPreparationPlan& preparationPlan, ULLongVector& fSizes, bool *wasCancelled)
	{
		if (ShouldPreScanHashRequestFileSizes(preparationPlan, request))
		{
			RunHashPreScanVisitWorkflow(executionContext, request, fSizes, wasCancelled);
			return true;
		}

		return false;
	}

	bool PrepareHashingWork(HashExecutionContext *executionContext, const HashRequest& request, const HashPreparationPlan& preparationPlan, ULLongVector& fSizes, bool *wasCancelled)
	{
		return ExecuteHashPreparationWorkflow(executionContext, request, preparationPlan, fSizes, wasCancelled);
	}

	void EmitPathResult(HashExecutionContext *executionContext, HashResult& result)
	{
		PublishFilePathResult(executionContext, result);
	}

	HashResult& BeginFileResult(HashExecutionContext *executionContext, const tstring& path)
	{
		return ExecuteFileResultBeginWorkflow(executionContext, path);
	}

	HashResult& BeginFileHashAttempt(HashExecutionContext *executionContext, const tstring& path, FileExecutionState *executionState, const TCHAR **resultPath)
	{
		return ExecuteFileHashAttemptBeginWorkflow(executionContext, path, executionState, resultPath);
	}
}
