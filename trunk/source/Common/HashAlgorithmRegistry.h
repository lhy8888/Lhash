#ifndef _HASH_ALGORITHM_REGISTRY_H_
#define _HASH_ALGORITHM_REGISTRY_H_
#include "Common/Global.h"

struct HashAlgorithmDescriptor
{
	ResultDigestType type;
	const char *stableName;
	const char *displayLabel;
};

struct HashAlgorithmDescriptorRegistry
{
	const HashAlgorithmDescriptor *descriptors;
	int count;
};

static inline std::vector<HashAlgorithmDescriptor>& GetMutableHashAlgorithmDescriptorStorage()
{
	static std::vector<HashAlgorithmDescriptor> descriptorStorage;
	return descriptorStorage;
}

static inline HashAlgorithmDescriptorRegistry& GetMutableHashAlgorithmDescriptorRegistryView()
{
	static HashAlgorithmDescriptorRegistry registryView = { NULL, 0 };
	return registryView;
}

static inline bool IsHashAlgorithmDescriptorValid(const HashAlgorithmDescriptor& algorithmDescriptor)
{
	return algorithmDescriptor.stableName != NULL &&
		algorithmDescriptor.stableName[0] != '\0' &&
		algorithmDescriptor.displayLabel != NULL &&
		algorithmDescriptor.displayLabel[0] != '\0';
}

static inline void RefreshHashAlgorithmDescriptorRegistryView()
{
	std::vector<HashAlgorithmDescriptor>& descriptorStorage = GetMutableHashAlgorithmDescriptorStorage();
	HashAlgorithmDescriptorRegistry& registryView = GetMutableHashAlgorithmDescriptorRegistryView();
	registryView.descriptors = descriptorStorage.empty() ? NULL : &descriptorStorage[0];
	registryView.count = static_cast<int>(descriptorStorage.size());
}

static inline bool RegisterHashAlgorithmDescriptor(const HashAlgorithmDescriptor& algorithmDescriptor)
{
	if (!IsHashAlgorithmDescriptorValid(algorithmDescriptor))
	{
		return false;
	}

	std::vector<HashAlgorithmDescriptor>& descriptorStorage = GetMutableHashAlgorithmDescriptorStorage();
	for (size_t descriptorIndex = 0; descriptorIndex < descriptorStorage.size(); ++descriptorIndex)
	{
		if (descriptorStorage[descriptorIndex].type != algorithmDescriptor.type)
		{
			continue;
		}

		descriptorStorage[descriptorIndex] = algorithmDescriptor;
		RefreshHashAlgorithmDescriptorRegistryView();
		return true;
	}

	descriptorStorage.push_back(algorithmDescriptor);
	RefreshHashAlgorithmDescriptorRegistryView();
	return true;
}

static inline void EnsureDefaultHashAlgorithmDescriptorsRegistered()
{
	static bool defaultsInitialized = false;
	if (defaultsInitialized)
	{
		return;
	}

	RegisterHashAlgorithmDescriptor({ RESULT_DIGEST_MD5, "md5", "MD5" });
	RegisterHashAlgorithmDescriptor({ RESULT_DIGEST_SHA1, "sha1", "SHA1" });
	RegisterHashAlgorithmDescriptor({ RESULT_DIGEST_SHA256, "sha256", "SHA256" });
	RegisterHashAlgorithmDescriptor({ RESULT_DIGEST_SHA512, "sha512", "SHA512" });
	defaultsInitialized = true;
}

static inline const HashAlgorithmDescriptor& GetUnknownHashAlgorithmDescriptor()
{
	static const HashAlgorithmDescriptor unknownAlgorithmDescriptor =
	{
		RESULT_DIGEST_UNKNOWN,
		"unknown",
		"UNKNOWN"
	};
	return unknownAlgorithmDescriptor;
}

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

static inline const HashAlgorithmDescriptorRegistry& GetHashAlgorithmDescriptorRegistry()
{
	EnsureDefaultHashAlgorithmDescriptorsRegistered();
	RefreshHashAlgorithmDescriptorRegistryView();
	return GetMutableHashAlgorithmDescriptorRegistryView();
}

static inline const HashAlgorithmDescriptor *GetRegisteredHashAlgorithmDescriptors()
{
	return GetHashAlgorithmDescriptorRegistry().descriptors;
}

static inline int GetRegisteredHashAlgorithmCount()
{
	return GetHashAlgorithmDescriptorRegistry().count;
}

static inline const HashAlgorithmDescriptor& GetHashAlgorithmDescriptorAt(int index)
{
	const HashAlgorithmDescriptor *algorithmDescriptors = GetRegisteredHashAlgorithmDescriptors();
	if (index < 0 || index >= GetRegisteredHashAlgorithmCount())
	{
		return GetUnknownHashAlgorithmDescriptor();
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
	int algorithmIndex = -1;
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
	int algorithmIndex;
	if (!TryGetHashAlgorithmIndex(digestType, &algorithmIndex))
	{
		return GetUnknownHashAlgorithmDescriptor();
	}
	return GetHashAlgorithmDescriptorAt(algorithmIndex);
}

static inline bool TryGetHashAlgorithmDescriptor(ResultDigestType digestType, const HashAlgorithmDescriptor **algorithmDescriptor)
{
	int algorithmIndex;
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

static inline ResultDigestType GetHashAlgorithmTypeAt(int index)
{
	const HashAlgorithmDescriptor& algorithmDescriptor = GetHashAlgorithmDescriptorAt(index);
	return GetHashAlgorithmDescriptorType(algorithmDescriptor);
}

static inline bool IsRegisteredHashAlgorithmType(ResultDigestType digestType)
{
	return TryGetHashAlgorithmIndex(digestType, NULL);
}

static inline bool TryGetHashAlgorithmType(int digestTypeValue, ResultDigestType *digestType)
{
	ResultDigestType candidateDigestType = static_cast<ResultDigestType>(digestTypeValue);
	if (!IsRegisteredHashAlgorithmType(candidateDigestType))
	{
		return false;
	}
	if (digestType == NULL)
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
