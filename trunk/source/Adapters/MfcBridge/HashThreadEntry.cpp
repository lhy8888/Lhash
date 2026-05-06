#include "stdafx.h"

#include "Adapters/MfcBridge/HashThreadEntry.h"
#include "Adapters/MfcBridge/HashThreadEntryRuntime.h"

int WINAPI HashThreadFunc(void *param)
{
	return RunMfcHashThread(param);
}
