#ifndef _MFC_HASH_REQUEST_BRIDGE_H_
#define _MFC_HASH_REQUEST_BRIDGE_H_

#include "Domain/HashRequest.h"
#include "Adapters/MfcBridge/HashRequestType.h"
#include "Adapters/MfcBridge/ThreadDataExecutionAccess.h"
#include "Adapters/MfcBridge/ThreadDataInputAccess.h"

static inline HashRequest CreateHashRequest(const ThreadData& threadData)
{
	HashRequest request;
	request.files = GetThreadDataInputFiles(threadData);
	request.uppercaseDigest = GetThreadDataUppercase(threadData);

	VisitEnabledThreadDataHashAlgorithmIds(threadData, [&](const HashAlgorithmId& algorithmId)
	{
		AppendHashRequestAlgorithmId(request, algorithmId);
		return true;
	});

	return request;
}

#endif
