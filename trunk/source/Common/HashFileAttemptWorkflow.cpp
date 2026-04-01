#include "stdafx.h"

#include "Common/HashEngineInternal.h"

#if defined (__APPLE__) || defined (__unix)
#include <unistd.h>
#include <sys/types.h>
#include <sched.h>
#endif

namespace HashEngineInternal
{
	static void YieldHashThread()
	{
#if defined (_WIN32)
		Sleep(3);
#else
		sched_yield();
#endif
	}

	bool ExecuteFileHashAttemptWorkflow(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const sunjwbase::tstring& fullPath,
		bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		, ThreadPool *threadPool
#endif
	)
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
			bool wasStopped = ProcessOpenedFileHashing(executionContext, request, result, fileIndex, isSizeCaled, fSizes, executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
				, threadPool
#endif
			);
			if (wasStopped)
			{
				return false;
			}
		}

		CompleteFileAttempt(executionContext, request, result, fileIndex, isSizeCaled, *executionState);
		return true;
	}
}
