#ifndef _LEGACY_HASH_ALGORITHM_TYPE_COMPAT_H_
#define _LEGACY_HASH_ALGORITHM_TYPE_COMPAT_H_

#include <string>

#include "Common/HashAlgorithmRegistry.h"
#include "LegacyCompat/ResultDigestTypeCompat.h"

static inline bool TryGetHashAlgorithmId(ResultDigestType digestType, HashAlgorithmId *algorithmId)
{
	HashAlgorithmId resolvedAlgorithmId;
	switch (digestType)
	{
	case RESULT_DIGEST_MD5:
		resolvedAlgorithmId = NormalizeHashAlgorithmId(sunjwbase::strtotstr(std::string("md5")));
		break;
	case RESULT_DIGEST_SHA1:
		resolvedAlgorithmId = NormalizeHashAlgorithmId(sunjwbase::strtotstr(std::string("sha1")));
		break;
	case RESULT_DIGEST_SHA256:
		resolvedAlgorithmId = NormalizeHashAlgorithmId(sunjwbase::strtotstr(std::string("sha256")));
		break;
	case RESULT_DIGEST_SHA512:
		resolvedAlgorithmId = NormalizeHashAlgorithmId(sunjwbase::strtotstr(std::string("sha512")));
		break;
	case RESULT_DIGEST_UNKNOWN:
	default:
		return false;
	}

	if (algorithmId != NULL)
	{
		*algorithmId = resolvedAlgorithmId;
	}
	return true;
}

static inline bool TryGetHashAlgorithmTypeById(const HashAlgorithmId& algorithmId, ResultDigestType *digestType)
{
	HashAlgorithmId normalizedAlgorithmId = NormalizeHashAlgorithmId(algorithmId);
	if (normalizedAlgorithmId.empty())
	{
		return false;
	}

	struct LegacyHashAlgorithmTypeMapping
	{
		ResultDigestType digestType;
		const char *stableName;
	};

	static const LegacyHashAlgorithmTypeMapping legacyMappings[] =
	{
		{ RESULT_DIGEST_MD5, "md5" },
		{ RESULT_DIGEST_SHA1, "sha1" },
		{ RESULT_DIGEST_SHA256, "sha256" },
		{ RESULT_DIGEST_SHA512, "sha512" }
	};

	for (int mappingIndex = 0; mappingIndex < static_cast<int>(sizeof(legacyMappings) / sizeof(legacyMappings[0])); ++mappingIndex)
	{
		HashAlgorithmId mappedAlgorithmId =
			NormalizeHashAlgorithmId(sunjwbase::strtotstr(std::string(legacyMappings[mappingIndex].stableName)));
		if (mappedAlgorithmId != normalizedAlgorithmId)
		{
			continue;
		}

		if (digestType != NULL)
		{
			*digestType = legacyMappings[mappingIndex].digestType;
		}
		return true;
	}

	return false;
}

static inline int GetHashAlgorithmIndex(ResultDigestType digestType)
{
	HashAlgorithmId algorithmId;
	if (!TryGetHashAlgorithmId(digestType, &algorithmId))
	{
		return -1;
	}

	return GetHashAlgorithmIndexById(algorithmId);
}

static inline bool TryGetHashAlgorithmIndex(ResultDigestType digestType, int *algorithmIndex)
{
	int resolvedIndex = GetHashAlgorithmIndex(digestType);
	if (resolvedIndex < 0)
	{
		return false;
	}

	if (algorithmIndex != NULL)
	{
		*algorithmIndex = resolvedIndex;
	}
	return true;
}

static inline const HashAlgorithmDescriptor& GetHashAlgorithmDescriptor(ResultDigestType digestType)
{
	int algorithmIndex = -1;
	if (!TryGetHashAlgorithmIndex(digestType, &algorithmIndex))
	{
		return GetUnknownHashAlgorithmDescriptor();
	}
	return GetHashAlgorithmDescriptorAt(algorithmIndex);
}

static inline bool TryGetHashAlgorithmDescriptor(ResultDigestType digestType, const HashAlgorithmDescriptor **algorithmDescriptor)
{
	int algorithmIndex = -1;
	if (!TryGetHashAlgorithmIndex(digestType, &algorithmIndex))
	{
		return false;
	}

	if (algorithmDescriptor != NULL)
	{
		*algorithmDescriptor = &GetHashAlgorithmDescriptorAt(algorithmIndex);
	}
	return true;
}

static inline HashAlgorithmId GetHashAlgorithmId(ResultDigestType digestType)
{
	HashAlgorithmId algorithmId;
	if (!TryGetHashAlgorithmId(digestType, &algorithmId))
	{
		return HashAlgorithmId();
	}

	return algorithmId;
}

static inline ResultDigestType GetHashAlgorithmTypeAt(int index)
{
	ResultDigestType digestType = RESULT_DIGEST_UNKNOWN;
	if (!TryGetHashAlgorithmTypeById(GetHashAlgorithmDescriptorId(GetHashAlgorithmDescriptorAt(index)), &digestType))
	{
		return RESULT_DIGEST_UNKNOWN;
	}

	return digestType;
}

static inline bool IsRegisteredHashAlgorithmType(ResultDigestType digestType)
{
	return TryGetHashAlgorithmIndex(digestType, NULL);
}

static inline bool TryGetHashAlgorithmType(int digestTypeValue, ResultDigestType *digestType)
{
	ResultDigestType candidateDigestType = static_cast<ResultDigestType>(digestTypeValue);
	if (!IsRegisteredHashAlgorithmType(candidateDigestType) || digestType == NULL)
	{
		return false;
	}

	*digestType = candidateDigestType;
	return true;
}

static inline sunjwbase::tstring GetHashAlgorithmDisplayLabel(ResultDigestType digestType)
{
	return GetHashAlgorithmDescriptorDisplayLabel(GetHashAlgorithmDescriptor(digestType));
}

#endif
