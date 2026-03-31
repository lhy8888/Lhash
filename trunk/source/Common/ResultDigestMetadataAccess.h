#ifndef _RESULT_DIGEST_METADATA_ACCESS_H_
#define _RESULT_DIGEST_METADATA_ACCESS_H_

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

static inline ResultDigestType GetResultDigestTypeAt(int index)
{
	return GetHashAlgorithmTypeAt(index);
}

static inline const ResultDigestMetadata& GetResultDigestMetadataAt(int index)
{
	return GetHashAlgorithmDescriptorAt(index);
}

static inline const ResultDigestMetadata& GetResultDigestMetadata(ResultDigestType digestType)
{
	return GetHashAlgorithmDescriptor(digestType);
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

#endif
