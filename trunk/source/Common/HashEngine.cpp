#include "stdafx.h"

#include "HashEngine.h"

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

	HashJobExecutionPlan executionPlan = { 0 };
	InitializeHashJobExecutionPlan(request, &executionPlan);

	ResetHashExecutionTotalSize(*executionContext);
	bool isSizeCaled = false;
	ULLongVector fSizes(GetHashRequestFileCount(request));

	bool wasCancelled = false;
	isSizeCaled = PrepareHashingWork(executionContext, request, GetHashJobPreparationPlan(executionPlan), fSizes, &wasCancelled);
	if (wasCancelled)
	{
		return CancelHashing(executionContext);
	}

	if (!RunHashScheduler(executionContext, request, executionPlan, isSizeCaled, fSizes))
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
