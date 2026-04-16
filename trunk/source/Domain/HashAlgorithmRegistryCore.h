#ifndef _HASH_ALGORITHM_REGISTRY_CORE_H_
#define _HASH_ALGORITHM_REGISTRY_CORE_H_

#include <string>
#include <mutex>
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
	std::vector<HashAlgorithmDescriptor> descriptors;
	int count;
};

inline std::vector<HashAlgorithmDescriptor>& GetMutableHashAlgorithmDescriptorStorage()
{
	static std::vector<HashAlgorithmDescriptor> descriptorStorage;
	return descriptorStorage;
}

inline std::mutex& GetHashAlgorithmDescriptorRegistryMutex()
{
	static std::mutex registryMutex;
	return registryMutex;
}

inline bool& GetHashAlgorithmDefaultsInitializedFlag()
{
	static bool defaultsInitialized = false;
	return defaultsInitialized;
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

static inline bool RegisterHashAlgorithmDescriptorUnlocked(const HashAlgorithmDescriptor& algorithmDescriptor)
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
		return true;
	}

	descriptorStorage.push_back(algorithmDescriptor);
	return true;
}

static inline bool RegisterHashAlgorithmDescriptor(const HashAlgorithmDescriptor& algorithmDescriptor)
{
	std::lock_guard<std::mutex> lock(GetHashAlgorithmDescriptorRegistryMutex());
	return RegisterHashAlgorithmDescriptorUnlocked(algorithmDescriptor);
}

static inline void EnsureDefaultHashAlgorithmDescriptorsRegistered()
{
	std::lock_guard<std::mutex> lock(GetHashAlgorithmDescriptorRegistryMutex());
	if (GetHashAlgorithmDefaultsInitializedFlag())
	{
		return;
	}

	RegisterHashAlgorithmDescriptorUnlocked({ "md5", "MD5 (Deprecated)", true, false });
	RegisterHashAlgorithmDescriptorUnlocked({ "sha1", "SHA1 (Deprecated)", true, false });
#if defined(FHASH_WITH_OPENSSL3_VENDOR)
	RegisterHashAlgorithmDescriptorUnlocked({ "openssl-sha-256", "SHA-256", true, true });
	RegisterHashAlgorithmDescriptorUnlocked({ "openssl-sha-384", "SHA-384", true, false });
	RegisterHashAlgorithmDescriptorUnlocked({ "openssl-sha-512", "SHA-512", true, true });
	RegisterHashAlgorithmDescriptorUnlocked({ "openssl-sha3-256", "SHA3-256", true, false });
	RegisterHashAlgorithmDescriptorUnlocked({ "openssl-sha3-384", "SHA3-384", true, false });
	RegisterHashAlgorithmDescriptorUnlocked({ "openssl-sha3-512", "SHA3-512", true, false });
	RegisterHashAlgorithmDescriptorUnlocked({ "openssl-blake2b-512", "BLAKE2b-512", true, false });
	RegisterHashAlgorithmDescriptorUnlocked({ "openssl-blake2s-256", "BLAKE2s-256", true, false });
	RegisterHashAlgorithmDescriptorUnlocked({ "openssl-shake128-256", "SHAKE128-256", true, false });
	RegisterHashAlgorithmDescriptorUnlocked({ "openssl-shake256-512", "SHAKE256-512", true, false });
#endif
	RegisterHashAlgorithmDescriptorUnlocked({ "blake3-256", "BLAKE3-256", true, false });
	RegisterHashAlgorithmDescriptorUnlocked({ "blake3-512", "BLAKE3-512", true, false });
	RegisterHashAlgorithmDescriptorUnlocked({ "blake3-xof", "BLAKE3 XOF", true, false });
	RegisterHashAlgorithmDescriptorUnlocked({ "xxh3-64", "XXH3-64", true, false });
	RegisterHashAlgorithmDescriptorUnlocked({ "xxh3-128", "XXH3-128", true, false });
	RegisterHashAlgorithmDescriptorUnlocked({ "crc32c", "CRC32C", true, false });
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
	std::lock_guard<std::mutex> lock(GetHashAlgorithmDescriptorRegistryMutex());
	GetMutableHashAlgorithmDescriptorStorage().clear();
	GetHashAlgorithmDefaultsInitializedFlag() = false;
	return true;
}

static inline bool ResetHashAlgorithmDescriptorsToDefaultsForTesting()
{
	ClearHashAlgorithmDescriptorsForTesting();
	EnsureDefaultHashAlgorithmDescriptorsRegistered();
	return true;
}

static inline std::vector<HashAlgorithmDescriptor> GetHashAlgorithmDescriptorSnapshot()
{
	EnsureDefaultHashAlgorithmDescriptorsRegistered();
	std::lock_guard<std::mutex> lock(GetHashAlgorithmDescriptorRegistryMutex());
	return GetMutableHashAlgorithmDescriptorStorage();
}

static inline HashAlgorithmDescriptorRegistry GetHashAlgorithmDescriptorRegistry()
{
	std::vector<HashAlgorithmDescriptor> descriptorStorage = GetHashAlgorithmDescriptorSnapshot();
	HashAlgorithmDescriptorRegistry registryView =
	{
		descriptorStorage,
		static_cast<int>(descriptorStorage.size())
	};
	return registryView;
}

static inline std::vector<HashAlgorithmDescriptor> GetRegisteredHashAlgorithmDescriptors()
{
	return GetHashAlgorithmDescriptorRegistry().descriptors;
}

static inline int GetRegisteredHashAlgorithmCount()
{
	EnsureDefaultHashAlgorithmDescriptorsRegistered();
	std::lock_guard<std::mutex> lock(GetHashAlgorithmDescriptorRegistryMutex());
	return static_cast<int>(GetMutableHashAlgorithmDescriptorStorage().size());
}

static inline HashAlgorithmDescriptor GetHashAlgorithmDescriptorAt(int index)
{
	EnsureDefaultHashAlgorithmDescriptorsRegistered();
	std::lock_guard<std::mutex> lock(GetHashAlgorithmDescriptorRegistryMutex());
	std::vector<HashAlgorithmDescriptor>& algorithmDescriptors = GetMutableHashAlgorithmDescriptorStorage();
	if (index < 0 || index >= static_cast<int>(algorithmDescriptors.size()))
	{
		return GetUnknownHashAlgorithmDescriptor();
	}
	return algorithmDescriptors[static_cast<size_t>(index)];
}

static inline int GetHashAlgorithmIndexById(const HashAlgorithmId& algorithmId)
{
	HashAlgorithmId normalizedAlgorithmId = NormalizeHashAlgorithmId(algorithmId);
	if (normalizedAlgorithmId.empty())
	{
		return -1;
	}

	EnsureDefaultHashAlgorithmDescriptorsRegistered();
	std::lock_guard<std::mutex> lock(GetHashAlgorithmDescriptorRegistryMutex());
	std::vector<HashAlgorithmDescriptor>& descriptorStorage = GetMutableHashAlgorithmDescriptorStorage();
	for (size_t index = 0; index < descriptorStorage.size(); ++index)
	{
		if (GetHashAlgorithmDescriptorId(descriptorStorage[index]) == normalizedAlgorithmId)
		{
			return static_cast<int>(index);
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
	std::vector<HashAlgorithmDescriptor> algorithmDescriptors = GetHashAlgorithmDescriptorSnapshot();
	for (size_t index = 0; index < algorithmDescriptors.size(); ++index)
	{
		if (!visitor(static_cast<int>(index), algorithmDescriptors[index]))
		{
			return false;
		}
	}
	return true;
}

static inline bool TryGetHashAlgorithmDescriptorById(const HashAlgorithmId& algorithmId, HashAlgorithmDescriptor *algorithmDescriptor)
{
	HashAlgorithmId normalizedAlgorithmId = NormalizeHashAlgorithmId(algorithmId);
	if (normalizedAlgorithmId.empty())
	{
		return false;
	}

	EnsureDefaultHashAlgorithmDescriptorsRegistered();
	std::lock_guard<std::mutex> lock(GetHashAlgorithmDescriptorRegistryMutex());
	std::vector<HashAlgorithmDescriptor>& descriptorStorage = GetMutableHashAlgorithmDescriptorStorage();
	for (size_t index = 0; index < descriptorStorage.size(); ++index)
	{
		if (GetHashAlgorithmDescriptorId(descriptorStorage[index]) != normalizedAlgorithmId)
		{
			continue;
		}

		if (algorithmDescriptor != NULL)
		{
			*algorithmDescriptor = descriptorStorage[index];
		}
		return true;
	}

	return false;
}

static inline bool IsRegisteredHashAlgorithmId(const HashAlgorithmId& algorithmId)
{
	return TryGetHashAlgorithmDescriptorById(algorithmId, NULL);
}

#endif
