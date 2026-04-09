#ifndef _HASH_ALGORITHM_REGISTRY_CORE_H_
#define _HASH_ALGORITHM_REGISTRY_CORE_H_

#include <string>
#include <vector>

#include "Common/strhelper.h"

typedef sunjwbase::tstring HashAlgorithmId;

struct HashAlgorithmDescriptor
{
	const char *stableName;
	const char *displayLabel;
	bool requiresDigestOperations;
	bool enabledByDefault;
};

struct HashAlgorithmDescriptorRegistry
{
	const HashAlgorithmDescriptor *descriptors;
	int count;
};

inline std::vector<HashAlgorithmDescriptor>& GetMutableHashAlgorithmDescriptorStorage()
{
	static std::vector<HashAlgorithmDescriptor> descriptorStorage;
	return descriptorStorage;
}

inline bool& GetHashAlgorithmDefaultsInitializedFlag()
{
	static bool defaultsInitialized = false;
	return defaultsInitialized;
}

inline HashAlgorithmDescriptorRegistry& GetMutableHashAlgorithmDescriptorRegistryView()
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

static inline HashAlgorithmId NormalizeHashAlgorithmId(const HashAlgorithmId& algorithmId)
{
	std::string normalizedStableName = sunjwbase::str_lower(sunjwbase::tstrtostr(algorithmId));
	return sunjwbase::strtotstr(normalizedStableName);
}

static inline HashAlgorithmId GetHashAlgorithmDescriptorId(const HashAlgorithmDescriptor& algorithmDescriptor)
{
	return NormalizeHashAlgorithmId(sunjwbase::strtotstr(std::string(algorithmDescriptor.stableName)));
}

static inline bool IsHashAlgorithmDescriptorIdEqual(const HashAlgorithmDescriptor& left, const HashAlgorithmDescriptor& right)
{
	return GetHashAlgorithmDescriptorId(left) == GetHashAlgorithmDescriptorId(right);
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
		const bool sameDescriptorId = IsHashAlgorithmDescriptorIdEqual(descriptorStorage[descriptorIndex], algorithmDescriptor);
		if (!sameDescriptorId)
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
	if (GetHashAlgorithmDefaultsInitializedFlag())
	{
		return;
	}

	RegisterHashAlgorithmDescriptor({ "md5", "MD5", true, true });
	RegisterHashAlgorithmDescriptor({ "sha1", "SHA1", true, true });
	RegisterHashAlgorithmDescriptor({ "sha256", "SHA256", true, true });
	RegisterHashAlgorithmDescriptor({ "sha512", "SHA512", true, true });
	RegisterHashAlgorithmDescriptor({ "blake3-256", "BLAKE3-256", true, false });
	RegisterHashAlgorithmDescriptor({ "blake3-512", "BLAKE3-512", true, false });
	RegisterHashAlgorithmDescriptor({ "blake3-xof", "BLAKE3 XOF", true, false });
	GetHashAlgorithmDefaultsInitializedFlag() = true;
}

static inline const HashAlgorithmDescriptor& GetUnknownHashAlgorithmDescriptor()
{
	static const HashAlgorithmDescriptor unknownAlgorithmDescriptor =
	{
		"unknown",
		"UNKNOWN",
		false,
		false
	};
	return unknownAlgorithmDescriptor;
}

static inline bool DoesHashAlgorithmDescriptorRequireDigestOperations(const HashAlgorithmDescriptor& algorithmDescriptor)
{
	return algorithmDescriptor.requiresDigestOperations;
}

static inline bool IsHashAlgorithmDescriptorEnabledByDefault(const HashAlgorithmDescriptor& algorithmDescriptor)
{
	return algorithmDescriptor.enabledByDefault;
}

static inline sunjwbase::tstring GetHashAlgorithmDescriptorStableName(const HashAlgorithmDescriptor& algorithmDescriptor)
{
	return sunjwbase::strtotstr(std::string(algorithmDescriptor.stableName));
}

static inline sunjwbase::tstring GetHashAlgorithmDescriptorDisplayLabel(const HashAlgorithmDescriptor& algorithmDescriptor)
{
	return sunjwbase::strtotstr(std::string(algorithmDescriptor.displayLabel));
}

static inline bool ClearHashAlgorithmDescriptorsForTesting()
{
	GetMutableHashAlgorithmDescriptorStorage().clear();
	RefreshHashAlgorithmDescriptorRegistryView();
	GetHashAlgorithmDefaultsInitializedFlag() = false;
	return true;
}

static inline bool ResetHashAlgorithmDescriptorsToDefaultsForTesting()
{
	ClearHashAlgorithmDescriptorsForTesting();
	EnsureDefaultHashAlgorithmDescriptorsRegistered();
	return true;
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

static inline int GetHashAlgorithmIndexById(const HashAlgorithmId& algorithmId)
{
	HashAlgorithmId normalizedAlgorithmId = NormalizeHashAlgorithmId(algorithmId);
	if (normalizedAlgorithmId.empty())
	{
		return -1;
	}

	for (int index = 0; index < GetRegisteredHashAlgorithmCount(); ++index)
	{
		const HashAlgorithmDescriptor& descriptor = GetHashAlgorithmDescriptorAt(index);
		if (GetHashAlgorithmDescriptorId(descriptor) == normalizedAlgorithmId)
		{
			return index;
		}
	}

	return -1;
}

static inline bool TryGetHashAlgorithmIndexById(const HashAlgorithmId& algorithmId, int *algorithmIndex)
{
	int resolvedIndex = GetHashAlgorithmIndexById(algorithmId);
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

static inline bool TryGetHashAlgorithmDescriptorById(const HashAlgorithmId& algorithmId, const HashAlgorithmDescriptor **algorithmDescriptor)
{
	int algorithmIndex = -1;
	if (!TryGetHashAlgorithmIndexById(algorithmId, &algorithmIndex))
	{
		return false;
	}

	if (algorithmDescriptor != NULL)
	{
		*algorithmDescriptor = &GetHashAlgorithmDescriptorAt(algorithmIndex);
	}
	return true;
}

static inline bool IsRegisteredHashAlgorithmId(const HashAlgorithmId& algorithmId)
{
	return TryGetHashAlgorithmDescriptorById(algorithmId, NULL);
}

#endif
