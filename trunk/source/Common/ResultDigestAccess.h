#ifndef _RESULT_DIGEST_ACCESS_H_
#define _RESULT_DIGEST_ACCESS_H_

#include "Common/HashAlgorithmRegistry.h"

typedef HashAlgorithmDescriptor ResultDigestMetadata;

static inline ResultDigestType GetResultDigestMetadataType(const ResultDigestMetadata& digestMetadata)
{
	return GetHashAlgorithmDescriptorType(digestMetadata);
}

static inline sunjwbase::tstring GetResultDigestMetadataDisplayLabel(const ResultDigestMetadata& digestMetadata)
{
	return GetHashAlgorithmDescriptorDisplayLabel(digestMetadata);
}

static inline sunjwbase::tstring ResultDigestCompatibilityFields::*GetResultDigestMetadataCompatibilityValueField(const ResultDigestMetadata& digestMetadata)
{
	return GetHashAlgorithmDescriptorCompatibilityValueField(digestMetadata);
}

static inline int GetResultDigestCount()
{
	return GetRegisteredHashAlgorithmCount();
}

static inline int GetResultDigestIndex(ResultDigestType digestType);
static inline ResultDigestType GetResultDigestTypeAt(int index);
static inline const ResultDigestMetadata& GetResultDigestMetadataAt(int index);
static inline const sunjwbase::tstring& GetResultDigest(const ResultData& result, ResultDigestType digestType);
static inline bool HasResultDigest(const ResultData& result, ResultDigestType digestType);
static inline sunjwbase::tstring GetResultDigestLabel(const ResultDigestMetadata& digestMetadata);

template<typename TResultDigestMetadataVisitor>
static inline bool VisitResultDigestMetadata(TResultDigestMetadataVisitor visitor)
{
	for (int index = 0; index < GetResultDigestCount(); index++)
	{
		if (!visitor(index, GetResultDigestMetadataAt(index)))
		{
			return false;
		}
	}

	return true;
}

template<typename TResultDigestVisitor>
static inline bool VisitResultDigests(TResultDigestVisitor visitor)
{
	return VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)
	{
		(void)index;
		return visitor(GetResultDigestMetadataType(digestMetadata));
	});
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

static inline const ResultDigestMetadata& GetResultDigestMetadataAt(int index)
{
	return GetHashAlgorithmDescriptorAt(index);
}

static inline const ResultDigestMetadata& GetResultDigestMetadata(ResultDigestType digestType)
{
	return GetHashAlgorithmDescriptor(digestType);
}

static inline ResultDigestType GetResultDigestTypeAt(int index)
{
	return GetHashAlgorithmTypeAt(index);
}

static inline sunjwbase::tstring GetResultDigestLabel(const ResultDigestMetadata& digestMetadata)
{
	return GetResultDigestMetadataDisplayLabel(digestMetadata);
}

static inline sunjwbase::tstring GetResultDigestLabel(ResultDigestType digestType)
{
	return GetResultDigestLabel(GetResultDigestMetadata(digestType));
}

static inline int GetResultDigestIndex(ResultDigestType digestType)
{
	return GetHashAlgorithmIndex(digestType);
}

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

static inline const sunjwbase::tstring& GetResultDigest(const ResultData& result, ResultDigestType digestType)
{
	if (HasStoredResultDigest(result, digestType))
	{
		return GetStoredResultDigest(result, digestType);
	}

	return GetCompatibilityResultDigest(result, digestType);
}

static inline sunjwbase::tstring& GetMutableResultDigest(ResultData& result, ResultDigestType digestType)
{
	return GetMutableStoredResultDigest(result, digestType);
}

static inline bool HasResultDigest(const ResultData& result, ResultDigestType digestType)
{
	return !GetResultDigest(result, digestType).empty();
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
	SetCompatibilityResultDigest(result, digestType, digestValue);
}

static inline void ResetResultDigests(ResultData& result)
{
	VisitResultDigests([&](ResultDigestType digestType)
	{
		ClearStoredResultDigest(result, digestType);
		ClearCompatibilityResultDigest(result, digestType);
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
