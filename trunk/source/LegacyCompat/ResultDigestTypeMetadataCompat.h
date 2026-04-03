#ifndef _LEGACY_RESULT_DIGEST_TYPE_METADATA_COMPAT_H_
#define _LEGACY_RESULT_DIGEST_TYPE_METADATA_COMPAT_H_

#include "Common/ResultDigestMetadataAccess.h"
#include "LegacyCompat/HashAlgorithmTypeCompat.h"

static inline ResultDigestType GetResultDigestMetadataType(const ResultDigestMetadata& digestMetadata)
{
	return GetHashAlgorithmDescriptorType(digestMetadata);
}

static inline int GetResultDigestIndex(ResultDigestType digestType)
{
	return GetHashAlgorithmIndex(digestType);
}

static inline bool TryGetResultDigestIndex(ResultDigestType digestType, int *index)
{
	return TryGetHashAlgorithmIndex(digestType, index);
}

static inline ResultDigestType GetResultDigestTypeAt(int index)
{
	return GetHashAlgorithmTypeAt(index);
}

static inline const ResultDigestMetadata& GetResultDigestMetadata(ResultDigestType digestType)
{
	return GetHashAlgorithmDescriptor(digestType);
}

static inline bool TryGetResultDigestMetadata(ResultDigestType digestType, const ResultDigestMetadata **digestMetadata)
{
	return TryGetHashAlgorithmDescriptor(digestType, digestMetadata);
}

static inline sunjwbase::tstring GetResultDigestLabel(ResultDigestType digestType)
{
	return GetResultDigestLabel(GetResultDigestMetadata(digestType));
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
