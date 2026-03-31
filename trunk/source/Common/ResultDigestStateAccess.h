#ifndef _RESULT_DIGEST_STATE_ACCESS_H_
#define _RESULT_DIGEST_STATE_ACCESS_H_

#include "Common/ResultDigestMetadataAccess.h"

static inline void EnsureDigestStorageSize(ResultDigestStorage& digestStorage)
{
	size_t digestCount = static_cast<size_t>(GetResultDigestCount());
	if (digestStorage.values.size() < digestCount)
	{
		digestStorage.values.resize(digestCount);
	}
}

static inline const sunjwbase::tstring& GetDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)
{
	size_t digestIndex = static_cast<size_t>(GetResultDigestIndex(digestType));
	if (digestIndex >= digestStorage.values.size())
	{
		static const sunjwbase::tstring emptyDigestValue;
		return emptyDigestValue;
	}
	return digestStorage.values[digestIndex];
}

static inline sunjwbase::tstring& GetMutableDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType)
{
	EnsureDigestStorageSize(digestStorage);
	return digestStorage.values[GetResultDigestIndex(digestType)];
}

static inline bool HasDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)
{
	return !GetDigestStorageValue(digestStorage, digestType).empty();
}

static inline void SetDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType, const sunjwbase::tstring& digestValue)
{
	GetMutableDigestStorageValue(digestStorage, digestType) = digestValue;
}

static inline void ClearDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType)
{
	GetMutableDigestStorageValue(digestStorage, digestType).clear();
}

static inline const ResultDigestState& GetResultDigestState(const ResultData& result)
{
	return result.digestState;
}

static inline ResultDigestState& GetMutableResultDigestState(ResultData& result)
{
	return result.digestState;
}

static inline const ResultDigestStorage& GetResultDigestStorage(const ResultData& result)
{
	return GetResultDigestState(result).storage;
}

static inline ResultDigestStorage& GetMutableResultDigestStorage(ResultData& result)
{
	return GetMutableResultDigestState(result).storage;
}

static inline const sunjwbase::tstring& GetStoredResultDigest(const ResultData& result, ResultDigestType digestType)
{
	return GetDigestStorageValue(GetResultDigestStorage(result), digestType);
}

static inline sunjwbase::tstring& GetMutableStoredResultDigest(ResultData& result, ResultDigestType digestType)
{
	return GetMutableDigestStorageValue(GetMutableResultDigestStorage(result), digestType);
}

static inline bool HasStoredResultDigest(const ResultData& result, ResultDigestType digestType)
{
	return HasDigestStorageValue(GetResultDigestStorage(result), digestType);
}

static inline void SetStoredResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)
{
	SetDigestStorageValue(GetMutableResultDigestStorage(result), digestType, digestValue);
}

static inline void ClearStoredResultDigest(ResultData& result, ResultDigestType digestType)
{
	ClearDigestStorageValue(GetMutableResultDigestStorage(result), digestType);
}

#endif
