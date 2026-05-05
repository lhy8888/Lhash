#ifndef _HASH_ENGINE_H_
#define _HASH_ENGINE_H_

#include "Common/HashTypes.h"

struct HashRequest;
struct HashExecutionContext;

int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request);

#endif
