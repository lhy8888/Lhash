#ifndef _HASH_ENGINE_H_
#define _HASH_ENGINE_H_

#include "Common/Global.h"

#if defined (_WIN32)
#include <WinDef.h>
#endif

struct HashRequest;
struct HashExecutionContext;

int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request);
int WINAPI HashThreadFunc(void *param);

#endif
