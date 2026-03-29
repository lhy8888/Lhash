#ifndef _HASH_ENGINE_H_
#define _HASH_ENGINE_H_

#include "Common/Global.h"

#if defined (_WIN32)
#include <WinDef.h>
#endif

struct HashRequest;
class HashProgressSink;

int RunHashRequest(ThreadData *thrdData, const HashRequest& request, HashProgressSink *observer);
int WINAPI HashThreadFunc(void *param);

#endif
