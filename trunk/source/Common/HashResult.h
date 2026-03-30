#ifndef _HASH_RESULT_H_
#define _HASH_RESULT_H_
#include "Common/ResultDataAccess.h"
#include "Common/ResultDigestMetadataAccess.h"
#include "Common/ResultDigestValueAccess.h"
static inline const HashResult& ProjectHashResult(const HashResult& result)
{
	return result;
}
static inline HashResult ProjectHashResult(const ResultData& result)
{
	HashResult projectedResult;
	projectedResult.state = GetResultState(result);
	projectedResult.path = GetResultPath(result);
	projectedResult.meta.size = GetResultSize(result);
	projectedResult.meta.modifiedDate = GetResultModifiedDate(result);
	projectedResult.meta.version = GetResultVersion(result);
	projectedResult.error = GetResultError(result);
	VisitResultDigestMetadataValues(result, [&](int index, const ResultDigestMetadata& digestMetadata, const sunjwbase::tstring& digestValue)
	{
		(void)index;
		if (digestValue.empty())
		{
			return true;
		}
		HashDigestResult digestResult;
		digestResult.type = GetResultDigestMetadataType(digestMetadata);
		digestResult.stableName = GetResultDigestMetadataStableName(digestMetadata);
		digestResult.displayLabel = GetResultDigestMetadataDisplayLabel(digestMetadata);
		digestResult.value = digestValue;
		projectedResult.digests.push_back(digestResult);
		return true;
	});
	return projectedResult;
}
#endif