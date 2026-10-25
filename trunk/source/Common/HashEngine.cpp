#include "stdafx.h"

#include "HashEngine.h"

#include "Common/HashEngineInternal.h"

using namespace std;
using namespace sunjwbase;
using namespace HashEngineInternal;

namespace
{
	class HashExecutionWorkingScope
	{
	public:
		explicit HashExecutionWorkingScope(HashExecutionContext& executionContext)
			: executionContext_(executionContext)
		{
			SetHashExecutionWorking(executionContext_, true);
		}
		~HashExecutionWorkingScope()
		{
			SetHashExecutionWorking(executionContext_, false);
		}
		HashExecutionWorkingScope(const HashExecutionWorkingScope&) = delete;
		HashExecutionWorkingScope& operator=(const HashExecutionWorkingScope&) = delete;
	private:
		HashExecutionContext& executionContext_;
	};
}

static int CancelHashing(HashExecutionContext *executionContext)
{
	HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
	ExecuteCancelledHashingWorkflow(executionContext, observer);
	return 0;
}

static int CompleteHashing(HashExecutionContext *executionContext)
{
	HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
	ExecuteCompletedHashingWorkflow(executionContext, observer);
	return 0;
}

int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request)
{
	HashExecutionWorkingScope workingScope(*executionContext);

	HashJobExecutionPlan executionPlan = {};
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
