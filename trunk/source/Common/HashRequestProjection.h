#ifndef _HASH_REQUEST_PROJECTION_H_
#define _HASH_REQUEST_PROJECTION_H_

#include "Common/HashRequest.h"
#include "Common/ThreadDataExecutionAccess.h"
#include "Common/ThreadDataInputAccess.h"

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
