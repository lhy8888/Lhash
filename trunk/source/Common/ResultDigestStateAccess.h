#ifndef _RESULT_DIGEST_STATE_ACCESS_H_
#define _RESULT_DIGEST_STATE_ACCESS_H_

#include "Common/HashTypes.h"
#include "Common/ResultDigestMetadataAccess.h"

static inline bool TryResolveDigestStorageIndexById(const HashAlgorithmId& algorithmId, size_t *digestIndex)
{
	int index = -1;
	if (!TryGetResultDigestIndexById(algorithmId, &index) || index < 0)
	{
		return false;
	}

	if (digestIndex != NULL)
	{
		*digestIndex = static_cast<size_t>(index);
	}

	return true;
}

static inline void EnsureDigestStorageSize(ResultDigestStorage& digestStorage)
{
	size_t digestCount = static_cast<size_t>(GetResultDigestCount());
	if (digestStorage.values.size() < digestCount)
	{
		digestStorage.values.resize(digestCount);
	}
}

static inline const sunjwbase::tstring& GetDigestStorageValueById(const ResultDigestStorage& digestStorage, const HashAlgorithmId& algorithmId)
{
	size_t digestIndex = 0;
	if (!TryResolveDigestStorageIndexById(algorithmId, &digestIndex))
	{
		static const sunjwbase::tstring emptyDigestValue;
		return emptyDigestValue;
	}
	if (digestIndex >= digestStorage.values.size())
	{
		static const sunjwbase::tstring emptyDigestValue;
		return emptyDigestValue;
	}
	return digestStorage.values[digestIndex];
}

static inline bool TryGetMutableDigestStorageValueById(ResultDigestStorage& digestStorage, const HashAlgorithmId& algorithmId, sunjwbase::tstring **digestValue)
{
	size_t digestIndex = 0;
	if (!TryResolveDigestStorageIndexById(algorithmId, &digestIndex))
	{
		if (digestValue != NULL)
		{
			*digestValue = NULL;
		}

		return false;
	}

	EnsureDigestStorageSize(digestStorage);
	if (digestValue != NULL)
	{
		*digestValue = &digestStorage.values[digestIndex];
	}

	return true;
}

static inline bool HasDigestStorageValueById(const ResultDigestStorage& digestStorage, const HashAlgorithmId& algorithmId)
{
	return !GetDigestStorageValueById(digestStorage, algorithmId).empty();
}

static inline void SetDigestStorageValueById(ResultDigestStorage& digestStorage, const HashAlgorithmId& algorithmId, const sunjwbase::tstring& digestValue)
{
	size_t digestIndex = 0;
	if (!TryResolveDigestStorageIndexById(algorithmId, &digestIndex))
	{
		return;
	}

	EnsureDigestStorageSize(digestStorage);
	digestStorage.values[digestIndex] = digestValue;
}

static inline void ClearDigestStorageValueById(ResultDigestStorage& digestStorage, const HashAlgorithmId& algorithmId)
{
	size_t digestIndex = 0;
	if (!TryResolveDigestStorageIndexById(algorithmId, &digestIndex))
	{
		return;
	}

	EnsureDigestStorageSize(digestStorage);
	digestStorage.values[digestIndex].clear();
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

static inline const sunjwbase::tstring& GetStoredResultDigestById(const ResultData& result, const HashAlgorithmId& algorithmId)
{
	return GetDigestStorageValueById(GetResultDigestStorage(result), algorithmId);
}

static inline bool TryGetMutableStoredResultDigestById(ResultData& result, const HashAlgorithmId& algorithmId, sunjwbase::tstring **digestValue)
{
	return TryGetMutableDigestStorageValueById(GetMutableResultDigestStorage(result), algorithmId, digestValue);
}

static inline bool HasStoredResultDigestById(const ResultData& result, const HashAlgorithmId& algorithmId)
{
	return HasDigestStorageValueById(GetResultDigestStorage(result), algorithmId);
}

static inline void SetStoredResultDigestById(ResultData& result, const HashAlgorithmId& algorithmId, const sunjwbase::tstring& digestValue)
{
	SetDigestStorageValueById(GetMutableResultDigestStorage(result), algorithmId, digestValue);
}

static inline void ClearStoredResultDigestById(ResultData& result, const HashAlgorithmId& algorithmId)
{
	ClearDigestStorageValueById(GetMutableResultDigestStorage(result), algorithmId);
}

#endif
