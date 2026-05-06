#include "stdafx.h"

#include "Common/HashEngineInternal.h"

#include <thread>

namespace HashEngineInternal
{
	static void YieldHashThread()
	{
		std::this_thread::yield();
	}

	bool ExecuteFileHashAttemptWorkflow(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const sunjwbase::tstring& fullPath,
		bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState, ThreadPool *threadPool)
	{
		if (ShouldStopHashExecution(*executionContext))
		{
			return false;
		}

		YieldHashThread();

		const TCHAR *path = fullPath.c_str();
		HashResult& result = BeginFileHashAttempt(executionContext, fullPath, executionState, &path);

#if defined (_WIN32)
		TCHAR fExc[sunjwbase::OsFile::ERR_MSG_BUFFER_LEN] = { 0 };
#else
		char fExc[sunjwbase::OsFile::ERR_MSG_BUFFER_LEN] = { 0 };
#endif
		sunjwbase::OsFile osFile(path);
		InitializeFileAttemptState(path, &osFile, &executionState->fileAttemptState);
		OpenFileForHashing(&executionState->fileAttemptState, (void *)&fExc);
		if (executionState->fileAttemptState.isFileOpened)
		{
			bool wasStopped = ProcessOpenedFileHashing(executionContext, request, result, fileIndex, isSizeCaled, fSizes, executionState, threadPool);
			if (wasStopped)
			{
				return false;
			}
		}

		CompleteFileAttempt(executionContext, request, result, fileIndex, isSizeCaled, *executionState);
		return true;
	}
}
