#ifndef _LEGACY_HASH_REQUEST_PROJECTION_H_
#define _LEGACY_HASH_REQUEST_PROJECTION_H_

#include "Domain/HashRequest.h"
#include "LegacyCompat/HashRequestTypeCompat.h"
#include "LegacyCompat/ThreadDataExecutionAccess.h"
#include "LegacyCompat/ThreadDataInputAccess.h"

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
