#ifndef _RESULT_DIGEST_VALUE_ACCESS_H_
#define _RESULT_DIGEST_VALUE_ACCESS_H_

#include "Common/ResultDigestStateAccess.h"

static inline const sunjwbase::tstring& GetResultDigestById(const ResultData& result, const HashAlgorithmId& algorithmId)
{
	return GetStoredResultDigestById(result, algorithmId);
}

static inline sunjwbase::tstring& GetMutableResultDigestById(ResultData& result, const HashAlgorithmId& algorithmId)
{
	return GetMutableStoredResultDigestById(result, algorithmId);
}

static inline bool HasResultDigestById(const ResultData& result, const HashAlgorithmId& algorithmId)
{
	return !GetResultDigestById(result, algorithmId).empty();
}

template<typename TResultDigestMetadataValueVisitor>
static inline bool VisitResultDigestMetadataValues(const ResultData& result, TResultDigestMetadataValueVisitor visitor)
{
	return VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)
	{
		HashAlgorithmId algorithmId = GetResultDigestMetadataId(digestMetadata);
		return visitor(index, digestMetadata, GetResultDigestById(result, algorithmId));
	});
}

template<typename TResultDigestMetadataIdValueVisitor>
static inline bool VisitResultDigestMetadataIdValues(const ResultData& result, TResultDigestMetadataIdValueVisitor visitor)
{
	return VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)
	{
		HashAlgorithmId algorithmId = GetResultDigestMetadataId(digestMetadata);
		return visitor(index, digestMetadata, algorithmId, GetResultDigestById(result, algorithmId));
	});
}

static inline bool HasAnyResultDigests(const ResultData& result)
{
	bool hasDigests = false;

	VisitResultDigestIds([&](const HashAlgorithmId& algorithmId)
	{
		if (HasResultDigestById(result, algorithmId))
		{
			hasDigests = true;
			return false;
		}

		return true;
	});

	return hasDigests;
}

static inline void SetResultDigestById(ResultData& result, const HashAlgorithmId& algorithmId, const sunjwbase::tstring& digestValue)
{
	SetStoredResultDigestById(result, algorithmId, digestValue);
}

static inline void ClearResultDigestById(ResultData& result, const HashAlgorithmId& algorithmId)
{
	ClearStoredResultDigestById(result, algorithmId);
}

static inline void ResetResultDigests(ResultData& result)
{
	VisitResultDigestIds([&](const HashAlgorithmId& algorithmId)
	{
		ClearStoredResultDigestById(result, algorithmId);
		return true;
	});
}

static inline bool ResultContainsDigest(const ResultData& result, const sunjwbase::tstring& digestText)
{
	bool containsDigest = false;

	VisitResultDigestIds([&](const HashAlgorithmId& algorithmId)
	{
		if (GetResultDigestById(result, algorithmId).find(digestText) != sunjwbase::tstring::npos)
		{
			containsDigest = true;
			return false;
		}

		return true;
	});

	return containsDigest;
}

#endif
