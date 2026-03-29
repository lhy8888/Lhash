#ifndef _HASH_RESULT_COMPATIBILITY_H_
#define _HASH_RESULT_COMPATIBILITY_H_

#include "Common/HashResult.h"

static inline void PopulateCompatibilityResultData(ResultData& compatibilityResult, const HashResult& hashResult)
{
	ResetResultData(compatibilityResult);
	SetResultState(compatibilityResult, hashResult.state);
	SetResultPath(compatibilityResult, hashResult.path);
	SetResultSize(compatibilityResult, hashResult.meta.size);
	SetResultModifiedDate(compatibilityResult, hashResult.meta.modifiedDate);
	SetResultVersion(compatibilityResult, hashResult.meta.version);
	SetResultError(compatibilityResult, hashResult.error);

	for (size_t digestIndex = 0; digestIndex < hashResult.digests.size(); ++digestIndex)
	{
		const HashDigestResult& digestResult = hashResult.digests[digestIndex];
		SetResultDigest(compatibilityResult, digestResult.type, digestResult.value);
	}
}

static inline ResultData CreateCompatibilityResultData(const HashResult& hashResult)
{
	ResultData compatibilityResult;
	PopulateCompatibilityResultData(compatibilityResult, hashResult);
	return compatibilityResult;
}

#endif
