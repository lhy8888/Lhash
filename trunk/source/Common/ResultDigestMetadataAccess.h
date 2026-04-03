#ifndef _RESULT_DIGEST_METADATA_ACCESS_H_
#define _RESULT_DIGEST_METADATA_ACCESS_H_

#include "Common/HashAlgorithmRegistry.h"

typedef HashAlgorithmDescriptor ResultDigestMetadata;

static inline ResultDigestType GetResultDigestMetadataType(const ResultDigestMetadata& digestMetadata)
{
	return GetHashAlgorithmDescriptorType(digestMetadata);
}

static inline HashAlgorithmId GetResultDigestMetadataId(const ResultDigestMetadata& digestMetadata)
{
	return GetHashAlgorithmDescriptorId(digestMetadata);
}

static inline sunjwbase::tstring GetResultDigestMetadataDisplayLabel(const ResultDigestMetadata& digestMetadata)
{
	return GetHashAlgorithmDescriptorDisplayLabel(digestMetadata);
}

static inline sunjwbase::tstring GetResultDigestMetadataStableName(const ResultDigestMetadata& digestMetadata)
{
	return GetHashAlgorithmDescriptorStableName(digestMetadata);
}

static inline int GetResultDigestCount()
{
	return GetRegisteredHashAlgorithmCount();
}

static inline int GetResultDigestIndex(ResultDigestType digestType)
{
	return GetHashAlgorithmIndex(digestType);
}

static inline int GetResultDigestIndexById(const HashAlgorithmId& algorithmId)
{
	return GetHashAlgorithmIndexById(algorithmId);
}

static inline bool TryGetResultDigestIndex(ResultDigestType digestType, int *index)
{
	return TryGetHashAlgorithmIndex(digestType, index);
}

static inline bool TryGetResultDigestIndexById(const HashAlgorithmId& algorithmId, int *index)
{
	return TryGetHashAlgorithmIndexById(algorithmId, index);
}

static inline ResultDigestType GetResultDigestTypeAt(int index)
{
	return GetHashAlgorithmTypeAt(index);
}

static inline const ResultDigestMetadata& GetResultDigestMetadataAt(int index)
{
	return GetHashAlgorithmDescriptorAt(index);
}

static inline HashAlgorithmId GetResultDigestIdAt(int index)
{
	return GetResultDigestMetadataId(GetResultDigestMetadataAt(index));
}

static inline const ResultDigestMetadata& GetResultDigestMetadata(ResultDigestType digestType)
{
	return GetHashAlgorithmDescriptor(digestType);
}

static inline bool TryGetResultDigestMetadata(ResultDigestType digestType, const ResultDigestMetadata **digestMetadata)
{
	return TryGetHashAlgorithmDescriptor(digestType, digestMetadata);
}

static inline const ResultDigestMetadata& GetResultDigestMetadataById(const HashAlgorithmId& algorithmId)
{
	const ResultDigestMetadata *digestMetadata = NULL;
	if (!TryGetHashAlgorithmDescriptorById(algorithmId, &digestMetadata) || digestMetadata == NULL)
	{
		return GetResultDigestMetadata(RESULT_DIGEST_UNKNOWN);
	}

	return *digestMetadata;
}

static inline bool TryGetResultDigestMetadataById(const HashAlgorithmId& algorithmId, const ResultDigestMetadata **digestMetadata)
{
	return TryGetHashAlgorithmDescriptorById(algorithmId, digestMetadata);
}

static inline sunjwbase::tstring GetResultDigestLabel(const ResultDigestMetadata& digestMetadata)
{
	return GetResultDigestMetadataDisplayLabel(digestMetadata);
}

static inline sunjwbase::tstring GetResultDigestLabel(ResultDigestType digestType)
{
	return GetResultDigestLabel(GetResultDigestMetadata(digestType));
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

template<typename TResultDigestIdVisitor>
static inline bool VisitResultDigestIds(TResultDigestIdVisitor visitor)
{
	return VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)
	{
		(void)index;
		return visitor(GetResultDigestMetadataId(digestMetadata));
	});
}

#endif
