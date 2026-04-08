#include "stdafx.h"

#include "LegacyCompat/HashThreadEntryRuntime.h"

int WINAPI HashThreadFunc(void *param)
{
	return RunLegacyHashThread(param);
}
