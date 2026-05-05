#include "stdafx.h"

#include "Adapters/ThreadDataBridge/HashThreadEntryRuntime.h"

int WINAPI HashThreadFunc(void *param)
{
	return RunLegacyHashThread(param);
}
