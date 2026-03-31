#ifndef _RESULT_DIGEST_VALUE_ACCESS_H_
#define _RESULT_DIGEST_VALUE_ACCESS_H_

#include "Common/ResultDigestStateAccess.h"

static inline const sunjwbase::tstring& GetResultDigest(const ResultData& result, ResultDigestType digestType)
{
	return GetStoredResultDigest(result, digestType);
}

static inline sunjwbase::tstring& GetMutableResultDigest(ResultData& result, ResultDigestType digestType)
{
	return GetMutableStoredResultDigest(result, digestType);
}

static inline bool HasResultDigest(const ResultData& result, ResultDigestType digestType)
{
	return !GetResultDigest(result, digestType).empty();
}

template<typename TResultDigestValueVisitor>
static inline bool VisitResultDigestValues(const ResultData& result, TResultDigestValueVisitor visitor)
{
	return VisitResultDigests([&](ResultDigestType digestType)
	{
		return visitor(digestType, GetResultDigest(result, digestType));
	});
}

template<typename TResultDigestMetadataValueVisitor>
static inline bool VisitResultDigestMetadataValues(const ResultData& result, TResultDigestMetadataValueVisitor visitor)
{
	return VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)
	{
		return visitor(index, digestMetadata, GetResultDigest(result, GetResultDigestMetadataType(digestMetadata)));
	});
}

static inline bool HasAnyResultDigests(const ResultData& result)
{
	bool hasDigests = false;

	VisitResultDigests([&](ResultDigestType digestType)
	{
		if (HasResultDigest(result, digestType))
		{
			hasDigests = true;
			return false;
		}

		return true;
	});

	return hasDigests;
}

static inline void SetResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)
{
	SetStoredResultDigest(result, digestType, digestValue);
}

static inline void ResetResultDigests(ResultData& result)
{
	VisitResultDigests([&](ResultDigestType digestType)
	{
		ClearStoredResultDigest(result, digestType);
		return true;
	});
}

static inline bool ResultContainsDigest(const ResultData& result, const sunjwbase::tstring& digestText)
{
	bool containsDigest = false;

	VisitResultDigests([&](ResultDigestType digestType)
	{
		if (GetResultDigest(result, digestType).find(digestText) != sunjwbase::tstring::npos)
		{
			containsDigest = true;
			return false;
		}

		return true;
	});

	return containsDigest;
}

#endif
