#include "stdafx.h"

#include "Common/HashEngine.h"
#include "Common/HashThreadEntryProjection.h"

int WINAPI HashThreadFunc(void *param)
{
	ThreadData *thrdData = (ThreadData *)param;
	HashExecutionContext executionContext = CreateThreadDataHashExecutionContext(*thrdData);
	HashRequest request = CreateThreadDataHashRequest(*thrdData);

	return RunHashRequest(&executionContext, request);
}
