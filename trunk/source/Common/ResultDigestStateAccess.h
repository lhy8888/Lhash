#ifndef _RESULT_DIGEST_STATE_ACCESS_H_
#define _RESULT_DIGEST_STATE_ACCESS_H_

#include "Common/ResultDigestMetadataAccess.h"

static inline const sunjwbase::tstring& GetDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)
{
	return digestStorage.values[GetResultDigestIndex(digestType)];
}

static inline sunjwbase::tstring& GetMutableDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType)
{
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

static inline const ResultDigestCompatibilityFields& GetResultDigestCompatibilityFields(const ResultData& result)
{
	return GetResultDigestState(result).compatibilityFields;
}

static inline ResultDigestCompatibilityFields& GetMutableResultDigestCompatibilityFields(ResultData& result)
{
	return GetMutableResultDigestState(result).compatibilityFields;
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

static inline sunjwbase::tstring ResultDigestCompatibilityFields::*GetCompatibilityResultDigestField(ResultDigestType digestType)
{
	return GetResultDigestMetadataCompatibilityValueField(GetResultDigestMetadata(digestType));
}

static inline const sunjwbase::tstring& GetCompatibilityResultDigest(const ResultData& result, ResultDigestType digestType)
{
	return GetResultDigestCompatibilityFields(result).*GetCompatibilityResultDigestField(digestType);
}

static inline sunjwbase::tstring& GetMutableCompatibilityResultDigest(ResultData& result, ResultDigestType digestType)
{
	return GetMutableResultDigestCompatibilityFields(result).*GetCompatibilityResultDigestField(digestType);
}

static inline void SetCompatibilityResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)
{
	GetMutableCompatibilityResultDigest(result, digestType) = digestValue;
}

static inline void ClearCompatibilityResultDigest(ResultData& result, ResultDigestType digestType)
{
	GetMutableCompatibilityResultDigest(result, digestType).clear();
}

#endif
