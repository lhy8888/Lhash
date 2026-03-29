#ifndef _HASH_RESULT_H_
#define _HASH_RESULT_H_

#include <vector>

#include "Common/ResultDataAccess.h"
#include "Common/ResultDigestMetadataAccess.h"
#include "Common/ResultDigestValueAccess.h"

struct HashDigestResult
{
	HashDigestResult()
		: type(RESULT_DIGEST_MD5)
	{
	}

	ResultDigestType type;
	sunjwbase::tstring stableName;
	sunjwbase::tstring displayLabel;
	sunjwbase::tstring value;
};

struct HashFileMeta
{
	HashFileMeta()
		: size(0)
	{
	}

	uint64_t size;
	sunjwbase::tstring modifiedDate;
	sunjwbase::tstring version;
};

struct HashResult
{
	HashResult()
		: state(RESULT_NONE)
	{
	}

	ResultState state;
	sunjwbase::tstring path;
	HashFileMeta meta;
	sunjwbase::tstring error;
	std::vector<HashDigestResult> digests;
};

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
