#ifndef _HASH_ALGORITHM_REGISTRY_H_
#define _HASH_ALGORITHM_REGISTRY_H_

#include "Common/Global.h"

enum ResultDigestType
{
	RESULT_DIGEST_MD5 = 0,
	RESULT_DIGEST_SHA1,
	RESULT_DIGEST_SHA256,
	RESULT_DIGEST_SHA512
};

struct HashAlgorithmDescriptor
{
	ResultDigestType type;
	const char *stableName;
	const char *displayLabel;
	sunjwbase::tstring ResultDigestCompatibilityFields::*compatibilityValueField;
};

static inline ResultDigestType GetHashAlgorithmDescriptorType(const HashAlgorithmDescriptor& algorithmDescriptor)
{
	return algorithmDescriptor.type;
}

static inline sunjwbase::tstring GetHashAlgorithmDescriptorStableName(const HashAlgorithmDescriptor& algorithmDescriptor)
{
	return sunjwbase::strtotstr(std::string(algorithmDescriptor.stableName));
}

static inline sunjwbase::tstring GetHashAlgorithmDescriptorDisplayLabel(const HashAlgorithmDescriptor& algorithmDescriptor)
{
	return sunjwbase::strtotstr(std::string(algorithmDescriptor.displayLabel));
}

static inline sunjwbase::tstring ResultDigestCompatibilityFields::*GetHashAlgorithmDescriptorCompatibilityValueField(const HashAlgorithmDescriptor& algorithmDescriptor)
{
	return algorithmDescriptor.compatibilityValueField;
}

static inline int GetRegisteredHashAlgorithmCount()
{
	return HASH_ALGORITHM_REGISTRY_COUNT;
}

static inline const HashAlgorithmDescriptor& GetHashAlgorithmDescriptorAt(int index)
{
	static const HashAlgorithmDescriptor algorithmDescriptors[HASH_ALGORITHM_REGISTRY_COUNT] =
	{
		{ RESULT_DIGEST_MD5, "md5", "MD5", &ResultDigestCompatibilityFields::md5 },
		{ RESULT_DIGEST_SHA1, "sha1", "SHA1", &ResultDigestCompatibilityFields::sha1 },
		{ RESULT_DIGEST_SHA256, "sha256", "SHA256", &ResultDigestCompatibilityFields::sha256 },
		{ RESULT_DIGEST_SHA512, "sha512", "SHA512", &ResultDigestCompatibilityFields::sha512 }
	};

	if (index < 0 || index >= HASH_ALGORITHM_REGISTRY_COUNT)
	{
		return algorithmDescriptors[0];
	}

	return algorithmDescriptors[index];
}

template<typename THashAlgorithmVisitor>
static inline bool VisitRegisteredHashAlgorithms(THashAlgorithmVisitor visitor)
{
	for (int index = 0; index < GetRegisteredHashAlgorithmCount(); ++index)
	{
		if (!visitor(index, GetHashAlgorithmDescriptorAt(index)))
		{
			return false;
		}
	}

	return true;
}

static inline int GetHashAlgorithmIndex(ResultDigestType digestType)
{
	int algorithmIndex = 0;

	VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)
	{
		if (GetHashAlgorithmDescriptorType(algorithmDescriptor) == digestType)
		{
			algorithmIndex = index;
			return false;
		}

		return true;
	});

	return algorithmIndex;
}

static inline const HashAlgorithmDescriptor& GetHashAlgorithmDescriptor(ResultDigestType digestType)
{
	return GetHashAlgorithmDescriptorAt(GetHashAlgorithmIndex(digestType));
}

static inline ResultDigestType GetHashAlgorithmTypeAt(int index)
{
	return GetHashAlgorithmDescriptorType(GetHashAlgorithmDescriptorAt(index));
}

static inline bool IsRegisteredHashAlgorithmType(ResultDigestType digestType)
{
	int algorithmIndex = GetHashAlgorithmIndex(digestType);
	return GetHashAlgorithmTypeAt(algorithmIndex) == digestType;
}

static inline bool TryGetHashAlgorithmType(int digestTypeValue, ResultDigestType *digestType)
{
	ResultDigestType candidateDigestType = static_cast<ResultDigestType>(digestTypeValue);
	if (!IsRegisteredHashAlgorithmType(candidateDigestType))
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
