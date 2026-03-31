#include "stdafx.h"

#include "HashEngine.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
#include "Common/ThreadPool.h"
#endif

#include "Common/HashExecutionContext.h"
#include "Common/ThreadDataAccess.h"
#include "Common/HashEngineInternal.h"

using namespace std;
using namespace sunjwbase;
using namespace HashEngineInternal;

static int CancelHashing(HashExecutionContext *executionContext)
{
	HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
	SetHashExecutionWorking(*executionContext, false);
	observer->onProgressEvent(CreateCancelledProgressEvent());
	return 0;
}

static int CompleteHashing(HashExecutionContext *executionContext)
{
	HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
	observer->onProgressEvent(CreateCompletedProgressEvent());
	SetHashExecutionWorking(*executionContext, false);
	return 0;
}

int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request)
{
	SetHashExecutionWorking(*executionContext, true);

	ResetHashExecutionTotalSize(*executionContext);
	bool isSizeCaled = false;
	ULLongVector fSizes(GetHashRequestFileCount(request));

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
	ThreadPool threadPool(5);
#endif

	bool wasCancelled = false;
	isSizeCaled = PrepareHashingWork(executionContext, request, fSizes, &wasCancelled);
	if (wasCancelled)
	{
		return CancelHashing(executionContext);
	}

	bool completedAllFiles = VisitHashRequestFiles(request, [&](uint32_t fileIndex, const tstring& fullPath)
	{
		return RunFileHashAttempt(executionContext, request, fileIndex, fullPath, isSizeCaled, fSizes
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
			, &threadPool
#endif
		);
	});

	if (!completedAllFiles)
	{
		return CancelHashing(executionContext);
	}

	return CompleteHashing(executionContext);
}

int WINAPI HashThreadFunc(void *param)
{
	ThreadData *thrdData = (ThreadData *)param;
	HashExecutionContext executionContext = CreateHashExecutionContext(*thrdData);
	HashRequest request = CreateHashRequest(*thrdData);

	return RunHashRequest(&executionContext, request);
}
