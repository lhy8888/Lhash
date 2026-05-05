#ifndef _HASH_ENGINE_H_
#define _HASH_ENGINE_H_

#include "Common/HashTypes.h"

struct HashRequest;
struct HashExecutionContext;

// RunHashRequest is synchronous. The caller must keep the execution context
// and its observed state alive until this function returns.
int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request);

#endif
