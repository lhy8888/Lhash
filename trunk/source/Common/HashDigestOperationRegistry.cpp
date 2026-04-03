#include "stdafx.h"

#include "Common/HashEngineInternal.h"
#include "Common/strhelper.h"

using namespace std;

namespace HashEngineInternal
{
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
		SetDigestStorageValue(digestBundle, RESULT_DIGEST_MD5, sunjwbase::strtotstr(string(chHashBuff)));
	}

	static void FinalizeSHA1DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		char strSHA1[256] = { 0 };
		hashContexts.sha1.Final();
		hashContexts.sha1.ReportHash(strSHA1, CSHA1::REPORT_HEX);
		SetDigestStorageValue(digestBundle, RESULT_DIGEST_SHA1, sunjwbase::strtotstr(string(strSHA1)));
	}

	static void FinalizeSHA256DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		string strSHA256;
		sha256_final(&hashContexts.sha256Ctx);
		sha256_digest(&hashContexts.sha256Ctx, &strSHA256);
		SetDigestStorageValue(digestBundle, RESULT_DIGEST_SHA256, sunjwbase::strtotstr(strSHA256));
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
		SetDigestStorageValue(digestBundle, RESULT_DIGEST_SHA512, sunjwbase::strtotstr(strSHA512));
	}

	static std::vector<HashDigestOperationDescriptor>& GetMutableHashDigestOperationDescriptorStorage()
	{
		static std::vector<HashDigestOperationDescriptor> operationDescriptorStorage;
		return operationDescriptorStorage;
	}

	bool RegisterHashDigestOperationDescriptor(const HashDigestOperationDescriptor& operationDescriptor)
	{
		if (!IsRegisteredHashAlgorithmType(operationDescriptor.digestType))
		{
			return false;
		}

		if (!IsHashDigestOperationDescriptorComplete(operationDescriptor))
		{
			return false;
		}

		std::vector<HashDigestOperationDescriptor>& operationDescriptorStorage = GetMutableHashDigestOperationDescriptorStorage();
		for (size_t descriptorIndex = 0; descriptorIndex < operationDescriptorStorage.size(); ++descriptorIndex)
		{
			if (operationDescriptorStorage[descriptorIndex].digestType != operationDescriptor.digestType)
			{
				continue;
			}

			operationDescriptorStorage[descriptorIndex] = operationDescriptor;
			return true;
		}

		operationDescriptorStorage.push_back(operationDescriptor);
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
			RESULT_DIGEST_MD5,
			InitializeMD5DigestContext,
			UpdateMD5DigestContext,
			FinalizeMD5DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			RESULT_DIGEST_SHA1,
			InitializeSHA1DigestContext,
			UpdateSHA1DigestContext,
			FinalizeSHA1DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			RESULT_DIGEST_SHA256,
			InitializeSHA256DigestContext,
			UpdateSHA256DigestContext,
			FinalizeSHA256DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			RESULT_DIGEST_SHA512,
			InitializeSHA512DigestContext,
			UpdateSHA512DigestContext,
			FinalizeSHA512DigestContext
		});
		defaultsInitialized = true;
	}

	bool IsHashDigestOperationDescriptorComplete(const HashDigestOperationDescriptor& operationDescriptor)
	{
		return operationDescriptor.initializeAction != NULL &&
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

	bool TryGetHashDigestOperationDescriptor(ResultDigestType digestType, HashDigestOperationDescriptor *operationDescriptor)
	{
		int descriptorCount = 0;
		const HashDigestOperationDescriptor *operationDescriptors = GetHashDigestOperationDescriptors(&descriptorCount);
		for (int descriptorIndex = 0; descriptorIndex < descriptorCount; ++descriptorIndex)
		{
			if (operationDescriptors[descriptorIndex].digestType != digestType)
			{
				continue;
			}

			if (operationDescriptor != NULL)
			{
				*operationDescriptor = operationDescriptors[descriptorIndex];
			}
			return true;
		}

		return false;
	}

	bool TryGetHashDigestOperationDescriptorById(const HashAlgorithmId& algorithmId, HashDigestOperationDescriptor *operationDescriptor)
	{
		const HashAlgorithmDescriptor *algorithmDescriptor = NULL;
		if (!TryGetHashAlgorithmDescriptorById(algorithmId, &algorithmDescriptor) || algorithmDescriptor == NULL)
		{
			return false;
		}

		return TryGetHashDigestOperationDescriptor(GetHashAlgorithmDescriptorType(*algorithmDescriptor), operationDescriptor);
	}

	bool IsHashDigestOperationDescriptorSupported(ResultDigestType digestType)
	{
		HashDigestOperationDescriptor operationDescriptor = {};
		if (!TryGetHashDigestOperationDescriptor(digestType, &operationDescriptor))
		{
			return false;
		}

		return operationDescriptor.digestType == digestType &&
			IsHashDigestOperationDescriptorComplete(operationDescriptor);
	}

	bool IsHashDigestOperationDescriptorSupportedById(const HashAlgorithmId& algorithmId)
	{
		HashDigestOperationDescriptor operationDescriptor = {};
		if (!TryGetHashDigestOperationDescriptorById(algorithmId, &operationDescriptor))
		{
			return false;
		}

		const HashAlgorithmDescriptor *algorithmDescriptor = NULL;
		if (!TryGetHashAlgorithmDescriptorById(algorithmId, &algorithmDescriptor) || algorithmDescriptor == NULL)
		{
			return false;
		}

		return operationDescriptor.digestType == GetHashAlgorithmDescriptorType(*algorithmDescriptor) &&
			IsHashDigestOperationDescriptorComplete(operationDescriptor);
	}

	bool IsHashDigestOperationRegistryConsistent()
	{
		bool isConsistent = true;

		VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)
		{
			(void)index;
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
