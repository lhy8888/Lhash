#ifndef _LEGACY_RESULT_DIGEST_TYPE_STATE_COMPAT_H_
#define _LEGACY_RESULT_DIGEST_TYPE_STATE_COMPAT_H_

#include "Common/ResultDigestStateAccess.h"
#include "Adapters/ThreadDataBridge/ResultDigestTypeMetadataCompat.h"

static inline bool TryResolveDigestStorageIndex(ResultDigestType digestType, size_t *digestIndex)
{
	int index = -1;
	if (!TryGetResultDigestIndex(digestType, &index) || index < 0)
	{
		return false;
	}

	if (digestIndex != NULL)
	{
		*digestIndex = static_cast<size_t>(index);
	}

	return true;
}

static inline const sunjwbase::tstring& GetDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)
{
	return GetDigestStorageValueById(digestStorage, GetHashAlgorithmId(digestType));
}

static inline bool TryGetMutableDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType, sunjwbase::tstring **digestValue)
{
	return TryGetMutableDigestStorageValueById(digestStorage, GetHashAlgorithmId(digestType), digestValue);
}

static inline bool HasDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)
{
	return HasDigestStorageValueById(digestStorage, GetHashAlgorithmId(digestType));
}

static inline void SetDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType, const sunjwbase::tstring& digestValue)
{
	SetDigestStorageValueById(digestStorage, GetHashAlgorithmId(digestType), digestValue);
}

static inline void ClearDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType)
{
	ClearDigestStorageValueById(digestStorage, GetHashAlgorithmId(digestType));
}

static inline const sunjwbase::tstring& GetStoredResultDigest(const ResultData& result, ResultDigestType digestType)
{
	return GetStoredResultDigestById(result, GetHashAlgorithmId(digestType));
}

static inline bool TryGetMutableStoredResultDigest(ResultData& result, ResultDigestType digestType, sunjwbase::tstring **digestValue)
{
	return TryGetMutableStoredResultDigestById(result, GetHashAlgorithmId(digestType), digestValue);
}

static inline bool HasStoredResultDigest(const ResultData& result, ResultDigestType digestType)
{
	return HasStoredResultDigestById(result, GetHashAlgorithmId(digestType));
}

static inline void SetStoredResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)
{
	SetStoredResultDigestById(result, GetHashAlgorithmId(digestType), digestValue);
}

static inline void ClearStoredResultDigest(ResultData& result, ResultDigestType digestType)
{
	ClearStoredResultDigestById(result, GetHashAlgorithmId(digestType));
}

#endif
