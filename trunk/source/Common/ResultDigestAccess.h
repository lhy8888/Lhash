#ifndef _RESULT_DIGEST_ACCESS_H_
#define _RESULT_DIGEST_ACCESS_H_

#include "Common/Global.h"

enum ResultDigestType
{
	RESULT_DIGEST_MD5 = 0,
	RESULT_DIGEST_SHA1,
	RESULT_DIGEST_SHA256,
	RESULT_DIGEST_SHA512
};

struct ResultDigestMetadata
{
	ResultDigestType type;
	const char *displayLabel;
	sunjwbase::tstring ResultDigestCompatibilityFields::*compatibilityValueField;
};

struct ResultDigestDisplayInfo
{
	sunjwbase::tstring label;
	sunjwbase::tstring value;
};

static inline ResultDigestType GetResultDigestMetadataType(const ResultDigestMetadata& digestMetadata)
{
	return digestMetadata.type;
}

static inline sunjwbase::tstring GetResultDigestMetadataDisplayLabel(const ResultDigestMetadata& digestMetadata)
{
	return sunjwbase::strtotstr(std::string(digestMetadata.displayLabel));
}

static inline sunjwbase::tstring ResultDigestCompatibilityFields::*GetResultDigestMetadataCompatibilityValueField(const ResultDigestMetadata& digestMetadata)
{
	return digestMetadata.compatibilityValueField;
}

static inline int GetResultDigestCount()
{
	return RESULT_DIGEST_STORAGE_COUNT;
}

static inline int GetResultDigestIndex(ResultDigestType digestType);
static inline ResultDigestType GetResultDigestTypeAt(int index);
static inline const ResultDigestMetadata& GetResultDigestMetadataAt(int index);
static inline const sunjwbase::tstring& GetResultDigest(const ResultData& result, ResultDigestType digestType);
static inline bool HasResultDigest(const ResultData& result, ResultDigestType digestType);
static inline sunjwbase::tstring GetResultDigestLabel(const ResultDigestMetadata& digestMetadata);

static inline sunjwbase::tstring FormatResultDigestForDisplay(const sunjwbase::tstring& digestValue, bool uppercase)
{
	if (uppercase)
	{
		return sunjwbase::strtotstr(sunjwbase::str_upper(sunjwbase::tstrtostr(digestValue)));
	}

	return sunjwbase::strtotstr(sunjwbase::str_lower(sunjwbase::tstrtostr(digestValue)));
}

static inline ResultDigestDisplayInfo GetResultDigestDisplayInfo(const ResultDigestMetadata& digestMetadata, const sunjwbase::tstring& digestValue, bool uppercase)
{
	ResultDigestDisplayInfo digestDisplayInfo;
	digestDisplayInfo.label = GetResultDigestLabel(digestMetadata);
	digestDisplayInfo.value = FormatResultDigestForDisplay(digestValue, uppercase);
	return digestDisplayInfo;
}

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

template<typename TResultDigestDisplayVisitor>
static inline bool VisitResultDigestDisplayValues(const ResultData& result, bool uppercase, TResultDigestDisplayVisitor visitor)
{
	return VisitResultDigestMetadataValues(result, [&](int index, const ResultDigestMetadata& digestMetadata, const sunjwbase::tstring& digestValueTstr)
	{
		if (!HasResultDigest(result, GetResultDigestMetadataType(digestMetadata)))
		{
			return true;
		}

		ResultDigestDisplayInfo digestDisplayInfo = GetResultDigestDisplayInfo(digestMetadata, digestValueTstr, uppercase);
		return visitor(index, digestMetadata, digestDisplayInfo);
	});
}

static inline const ResultDigestMetadata& GetResultDigestMetadataAt(int index)
{
	static const ResultDigestMetadata digestMetadata[RESULT_DIGEST_STORAGE_COUNT] =
	{
		{ RESULT_DIGEST_MD5, "MD5", &ResultDigestCompatibilityFields::md5 },
		{ RESULT_DIGEST_SHA1, "SHA1", &ResultDigestCompatibilityFields::sha1 },
		{ RESULT_DIGEST_SHA256, "SHA256", &ResultDigestCompatibilityFields::sha256 },
		{ RESULT_DIGEST_SHA512, "SHA512", &ResultDigestCompatibilityFields::sha512 }
	};

	if (index < 0 || index >= RESULT_DIGEST_STORAGE_COUNT)
	{
		return digestMetadata[0];
	}

	return digestMetadata[index];
}

static inline const ResultDigestMetadata& GetResultDigestMetadata(ResultDigestType digestType)
{
	return GetResultDigestMetadataAt(GetResultDigestIndex(digestType));
}

static inline ResultDigestType GetResultDigestTypeAt(int index)
{
	return GetResultDigestMetadataType(GetResultDigestMetadataAt(index));
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
	int digestIndex = 0;

	VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)
	{
		if (GetResultDigestMetadataType(digestMetadata) == digestType)
		{
			digestIndex = index;
			return false;
		}

		return true;
	});

	return digestIndex;
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
