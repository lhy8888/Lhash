#ifndef _RESULT_DIGEST_METADATA_ACCESS_H_
#define _RESULT_DIGEST_METADATA_ACCESS_H_

#include "Domain/HashAlgorithmRegistryCore.h"

typedef HashAlgorithmDescriptor ResultDigestMetadata;

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

static inline int GetResultDigestIndexById(const HashAlgorithmId& algorithmId)
{
	return GetHashAlgorithmIndexById(algorithmId);
}

static inline bool TryGetResultDigestIndexById(const HashAlgorithmId& algorithmId, int *index)
{
	return TryGetHashAlgorithmIndexById(algorithmId, index);
}

static inline ResultDigestMetadata GetResultDigestMetadataAt(int index)
{
	return GetHashAlgorithmDescriptorAt(index);
}

static inline HashAlgorithmId GetResultDigestIdAt(int index)
{
	return GetResultDigestMetadataId(GetResultDigestMetadataAt(index));
}

static inline ResultDigestMetadata GetResultDigestMetadataById(const HashAlgorithmId& algorithmId)
{
	ResultDigestMetadata digestMetadata = GetUnknownHashAlgorithmDescriptor();
	if (!TryGetHashAlgorithmDescriptorById(algorithmId, &digestMetadata))
	{
		return GetUnknownHashAlgorithmDescriptor();
	}

	return digestMetadata;
}

static inline bool TryGetResultDigestMetadataById(const HashAlgorithmId& algorithmId, ResultDigestMetadata *digestMetadata)
{
	return TryGetHashAlgorithmDescriptorById(algorithmId, digestMetadata);
}

static inline sunjwbase::tstring GetResultDigestLabel(const ResultDigestMetadata& digestMetadata)
{
	return GetResultDigestMetadataDisplayLabel(digestMetadata);
}

template<typename TResultDigestMetadataVisitor>
static inline bool VisitResultDigestMetadata(TResultDigestMetadataVisitor visitor)
{
	for (int index = 0; index < GetResultDigestCount(); ++index)
	{
		if (!visitor(index, GetResultDigestMetadataAt(index)))
		{
			return false;
		}
	}

	return true;
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
