#include "stdafx.h"

#include "Common/HashEngineInternal.h"
#include "Common/strhelper.h"

using namespace std;

namespace HashEngineInternal
{
	static HashAlgorithmId CreateKnownAlgorithmId(const char *stableName)
	{
		return NormalizeHashAlgorithmId(sunjwbase::strtotstr(std::string(stableName)));
	}

	static const HashAlgorithmId& GetMd5AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("md5");
		return algorithmId;
	}

	static const HashAlgorithmId& GetSha1AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("sha1");
		return algorithmId;
	}

	static const HashAlgorithmId& GetSha256AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("sha256");
		return algorithmId;
	}

	static const HashAlgorithmId& GetSha512AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("sha512");
		return algorithmId;
	}

	static void InitializeMD5DigestContext(FileHashContexts *hashContexts)
	{
		MD5Init(&hashContexts->mdContext, 0);
	}

	static void InitializeSHA1DigestContext(FileHashContexts *hashContexts)
	{
		hashContexts->sha1.Reset();
	}

	static void InitializeSHA256DigestContext(FileHashContexts *hashContexts)
	{
		sha256_init(&hashContexts->sha256Ctx);
	}

	static void InitializeSHA512DigestContext(FileHashContexts *hashContexts)
	{
		SHA512_Init(&hashContexts->sha512Ctx);
	}

	static void UpdateMD5DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		MD5Update(&hashContexts.mdContext, data, dataLen);
	}

	static void UpdateSHA1DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		hashContexts.sha1.Update(data, dataLen);
	}

	static void UpdateSHA256DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		sha256_update(&hashContexts.sha256Ctx, data, dataLen);
	}

	static void UpdateSHA512DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		SHA512_Update(&hashContexts.sha512Ctx, data, dataLen);
	}

	static void FinalizeMD5DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		char chHashBuff[1024] = { 0 };
		MD5Final(&hashContexts.mdContext);
#if defined (_WIN32)
		sprintf_s(chHashBuff, 1024,
			"%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X",
			hashContexts.mdContext.digest[0],
			hashContexts.mdContext.digest[1],
			hashContexts.mdContext.digest[2],
			hashContexts.mdContext.digest[3],
			hashContexts.mdContext.digest[4],
			hashContexts.mdContext.digest[5],
			hashContexts.mdContext.digest[6],
			hashContexts.mdContext.digest[7],
			hashContexts.mdContext.digest[8],
			hashContexts.mdContext.digest[9],
			hashContexts.mdContext.digest[10],
			hashContexts.mdContext.digest[11],
			hashContexts.mdContext.digest[12],
			hashContexts.mdContext.digest[13],
			hashContexts.mdContext.digest[14],
			hashContexts.mdContext.digest[15]);
#else
		snprintf(chHashBuff, 1024,
			"%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X",
			hashContexts.mdContext.digest[0],
			hashContexts.mdContext.digest[1],
			hashContexts.mdContext.digest[2],
			hashContexts.mdContext.digest[3],
			hashContexts.mdContext.digest[4],
			hashContexts.mdContext.digest[5],
			hashContexts.mdContext.digest[6],
			hashContexts.mdContext.digest[7],
			hashContexts.mdContext.digest[8],
			hashContexts.mdContext.digest[9],
			hashContexts.mdContext.digest[10],
			hashContexts.mdContext.digest[11],
			hashContexts.mdContext.digest[12],
			hashContexts.mdContext.digest[13],
			hashContexts.mdContext.digest[14],
			hashContexts.mdContext.digest[15]);
#endif
		SetDigestStorageValueById(digestBundle, GetMd5AlgorithmId(), sunjwbase::strtotstr(string(chHashBuff)));
	}

	static void FinalizeSHA1DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		char strSHA1[256] = { 0 };
		hashContexts.sha1.Final();
		hashContexts.sha1.ReportHash(strSHA1, CSHA1::REPORT_HEX);
		SetDigestStorageValueById(digestBundle, GetSha1AlgorithmId(), sunjwbase::strtotstr(string(strSHA1)));
	}

	static void FinalizeSHA256DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		string strSHA256;
		sha256_final(&hashContexts.sha256Ctx);
		sha256_digest(&hashContexts.sha256Ctx, &strSHA256);
		SetDigestStorageValueById(digestBundle, GetSha256AlgorithmId(), sunjwbase::strtotstr(strSHA256));
	}

	static void FinalizeSHA512DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		string strSHA512;
		SHA512_Final(hashContexts.digestSHA512, &hashContexts.sha512Ctx);
		strSHA512.clear();
		for (int digestIndex = 0; digestIndex < SHA512_DIGEST_LENGTH; ++digestIndex)
		{
			char hexByte[8] = { 0 };
#if defined (_WIN32)
			sprintf_s(hexByte, 8, "%02X", hashContexts.digestSHA512[digestIndex]);
#else
			snprintf(hexByte, 8, "%02X", hashContexts.digestSHA512[digestIndex]);
#endif
			strSHA512.append(hexByte);
		}
		SetDigestStorageValueById(digestBundle, GetSha512AlgorithmId(), sunjwbase::strtotstr(strSHA512));
	}

	static std::vector<HashDigestOperationDescriptor>& GetMutableHashDigestOperationDescriptorStorage()
	{
		static std::vector<HashDigestOperationDescriptor> operationDescriptorStorage;
		return operationDescriptorStorage;
	}

	static HashAlgorithmId ResolveHashDigestOperationDescriptorAlgorithmId(const HashDigestOperationDescriptor& operationDescriptor)
	{
		return NormalizeHashAlgorithmId(operationDescriptor.algorithmId);
	}

	bool RegisterHashDigestOperationDescriptor(const HashDigestOperationDescriptor& operationDescriptor)
	{
		HashAlgorithmId normalizedAlgorithmId = ResolveHashDigestOperationDescriptorAlgorithmId(operationDescriptor);
		if (normalizedAlgorithmId.empty())
		{
			return false;
		}

		if (!IsRegisteredHashAlgorithmId(normalizedAlgorithmId))
		{
			return false;
		}

		HashDigestOperationDescriptor resolvedDescriptor = operationDescriptor;
		resolvedDescriptor.algorithmId = normalizedAlgorithmId;
		if (!IsHashDigestOperationDescriptorComplete(resolvedDescriptor))
		{
			return false;
		}

		std::vector<HashDigestOperationDescriptor>& operationDescriptorStorage = GetMutableHashDigestOperationDescriptorStorage();
		for (size_t descriptorIndex = 0; descriptorIndex < operationDescriptorStorage.size(); ++descriptorIndex)
		{
			HashAlgorithmId existingAlgorithmId =
				ResolveHashDigestOperationDescriptorAlgorithmId(operationDescriptorStorage[descriptorIndex]);
			if (existingAlgorithmId != normalizedAlgorithmId)
			{
				continue;
			}

			operationDescriptorStorage[descriptorIndex] = resolvedDescriptor;
			return true;
		}

		operationDescriptorStorage.push_back(resolvedDescriptor);
		return true;
	}

	static void EnsureDefaultHashDigestOperationDescriptorsRegistered()
	{
		static bool defaultsInitialized = false;
		if (defaultsInitialized)
		{
			return;
		}

		RegisterHashDigestOperationDescriptor({
			GetMd5AlgorithmId(),
			InitializeMD5DigestContext,
			UpdateMD5DigestContext,
			FinalizeMD5DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetSha1AlgorithmId(),
			InitializeSHA1DigestContext,
			UpdateSHA1DigestContext,
			FinalizeSHA1DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetSha256AlgorithmId(),
			InitializeSHA256DigestContext,
			UpdateSHA256DigestContext,
			FinalizeSHA256DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetSha512AlgorithmId(),
			InitializeSHA512DigestContext,
			UpdateSHA512DigestContext,
			FinalizeSHA512DigestContext
		});
		defaultsInitialized = true;
	}

	bool IsHashDigestOperationDescriptorComplete(const HashDigestOperationDescriptor& operationDescriptor)
	{
		HashAlgorithmId algorithmId = ResolveHashDigestOperationDescriptorAlgorithmId(operationDescriptor);
		return !algorithmId.empty() &&
			operationDescriptor.initializeAction != NULL &&
			operationDescriptor.updateAction != NULL &&
			operationDescriptor.finalizeAction != NULL;
	}

	const HashDigestOperationDescriptor *GetHashDigestOperationDescriptors(int *descriptorCount)
	{
		EnsureDefaultHashDigestOperationDescriptorsRegistered();
		std::vector<HashDigestOperationDescriptor>& operationDescriptors = GetMutableHashDigestOperationDescriptorStorage();

		if (descriptorCount != NULL)
		{
			*descriptorCount = static_cast<int>(operationDescriptors.size());
		}

		return operationDescriptors.empty() ? NULL : &operationDescriptors[0];
	}

	bool TryGetHashDigestOperationDescriptorById(const HashAlgorithmId& algorithmId, HashDigestOperationDescriptor *operationDescriptor)
	{
		HashAlgorithmId normalizedAlgorithmId = NormalizeHashAlgorithmId(algorithmId);
		if (normalizedAlgorithmId.empty() || !IsRegisteredHashAlgorithmId(normalizedAlgorithmId))
		{
			return false;
		}

		int descriptorCount = 0;
		const HashDigestOperationDescriptor *operationDescriptors = GetHashDigestOperationDescriptors(&descriptorCount);
		for (int descriptorIndex = 0; descriptorIndex < descriptorCount; ++descriptorIndex)
		{
			HashAlgorithmId descriptorAlgorithmId =
				ResolveHashDigestOperationDescriptorAlgorithmId(operationDescriptors[descriptorIndex]);
			if (descriptorAlgorithmId != normalizedAlgorithmId)
			{
				continue;
			}

			if (operationDescriptor != NULL)
			{
				*operationDescriptor = operationDescriptors[descriptorIndex];
				operationDescriptor->algorithmId = normalizedAlgorithmId;
			}

			return true;
		}

		return false;
	}

	bool IsHashDigestOperationDescriptorSupportedById(const HashAlgorithmId& algorithmId)
	{
		HashDigestOperationDescriptor operationDescriptor = {};
		if (!TryGetHashDigestOperationDescriptorById(algorithmId, &operationDescriptor))
		{
			return false;
		}

		HashAlgorithmId normalizedAlgorithmId = NormalizeHashAlgorithmId(algorithmId);
		HashAlgorithmId descriptorAlgorithmId = ResolveHashDigestOperationDescriptorAlgorithmId(operationDescriptor);
		if (descriptorAlgorithmId != normalizedAlgorithmId)
		{
			return false;
		}

		operationDescriptor.algorithmId = normalizedAlgorithmId;
		return IsHashDigestOperationDescriptorComplete(operationDescriptor);
	}

	bool IsHashDigestOperationRegistryConsistent()
	{
		bool isConsistent = true;

		VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)
		{
			(void)index;
			if (!DoesHashAlgorithmDescriptorRequireDigestOperations(algorithmDescriptor))
			{
				// Descriptor-only algorithms are allowed to exist before a native digest backend lands.
				return true;
			}

			HashAlgorithmId algorithmId = GetHashAlgorithmDescriptorId(algorithmDescriptor);
			if (!IsHashDigestOperationDescriptorSupportedById(algorithmId))
			{
				isConsistent = false;
				return false;
			}

			return true;
		});

		return isConsistent;
	}
}
