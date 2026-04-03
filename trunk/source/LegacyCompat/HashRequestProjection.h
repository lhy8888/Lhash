#ifndef _LEGACY_HASH_REQUEST_PROJECTION_H_
#define _LEGACY_HASH_REQUEST_PROJECTION_H_

#include "Common/HashRequest.h"
#include "LegacyCompat/ThreadDataExecutionAccess.h"
#include "LegacyCompat/ThreadDataInputAccess.h"

static inline HashRequest CreateHashRequest(const ThreadData& threadData)
{
	HashRequest request;
	request.files = GetThreadDataInputFiles(threadData);
	request.uppercaseDigest = GetThreadDataUppercase(threadData);

	VisitEnabledThreadDataHashAlgorithms(threadData, [&](ResultDigestType digestType)
	{
		AppendHashRequestAlgorithm(request, digestType);
		return true;
	});

	return request;
}

#endif
