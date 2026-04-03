#include "stdafx.h"

#include "Common/HashEngine.h"
#include "Common/HashExecutionContext.h"
#include "Common/HashRequestProjection.h"
#include "Common/ThreadDataExecutionAccess.h"

int WINAPI HashThreadFunc(void *param)
{
	ThreadData *thrdData = (ThreadData *)param;
	HashExecutionContext executionContext = CreateHashExecutionContext(
		GetThreadDataObserver(*thrdData),
		GetMutableThreadDataHashJobState(*thrdData),
		GetMutableThreadDataHashCancellationState(*thrdData));
	HashRequest request = CreateHashRequest(*thrdData);

	return RunHashRequest(&executionContext, request);
}
