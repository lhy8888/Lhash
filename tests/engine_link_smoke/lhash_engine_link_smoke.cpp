#include "stdafx.h"

#include "Common/HashEngine.h"
#include "Domain/HashRequest.h"
#include "Runtime/HashExecutionContext.h"

int main()
{
    HashRequest request;
    HashJobState jobState;
    HashCancellationState cancellationState;
    HashExecutionContext ctx(NULL, jobState, cancellationState);
    return RunHashRequest(&ctx, request) == 0 ? 0 : 1;
}
