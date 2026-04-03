#include "stdafx.h"

#include "Common/HashEngineInternal.h"
#include "Common/strhelper.h"

using namespace std;

namespace HashEngineInternal
{
	typedef void (*DigestContextInitializeAction)(FileHashContexts *hashContexts);
	typedef void (*DigestContextFinalizeAction)(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle);

	struct DigestContextOperation
	{
		ResultDigestType digestType;
		DigestContextInitializeAction initializeAction;
		DigestContextFinalizeAction finalizeAction;
	};

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
		for (int p = 0; p < SHA512_DIGEST_LENGTH; p++)
		{
			char buf[8] = { 0 };
#if defined (_WIN32)
			sprintf_s(buf, 8, "%02X", hashContexts.digestSHA512[p]);
#else
			snprintf(buf, 8, "%02X", hashContexts.digestSHA512[p]);
#endif
			strSHA512.append(std::string(buf));
		}
		SetDigestStorageValue(digestBundle, RESULT_DIGEST_SHA512, sunjwbase::strtotstr(strSHA512));
	}

	static const DigestContextOperation *GetDigestContextOperations(size_t *operationCount)
	{
		static const DigestContextOperation digestContextOperations[] =
		{
			{ RESULT_DIGEST_MD5, InitializeMD5DigestContext, FinalizeMD5DigestContext },
			{ RESULT_DIGEST_SHA1, InitializeSHA1DigestContext, FinalizeSHA1DigestContext },
			{ RESULT_DIGEST_SHA256, InitializeSHA256DigestContext, FinalizeSHA256DigestContext },
			{ RESULT_DIGEST_SHA512, InitializeSHA512DigestContext, FinalizeSHA512DigestContext }
		};
		*operationCount = sizeof(digestContextOperations) / sizeof(DigestContextOperation);
		return digestContextOperations;
	}

	static bool TryGetDigestContextOperation(ResultDigestType digestType, DigestContextOperation *digestContextOperation)
	{
		size_t operationCount = 0;
		const DigestContextOperation *digestContextOperations = GetDigestContextOperations(&operationCount);
		for (size_t operationIndex = 0; operationIndex < operationCount; ++operationIndex)
		{
			if (digestContextOperations[operationIndex].digestType != digestType)
			{
				continue;
			}

			*digestContextOperation = digestContextOperations[operationIndex];
			return true;
		}

		return false;
	}

	void InitializeHashDigestContext(FileHashContexts *hashContexts, ResultDigestType digestType)
	{
		DigestContextOperation digestContextOperation = { 0 };
		if (TryGetDigestContextOperation(digestType, &digestContextOperation))
		{
			digestContextOperation.initializeAction(hashContexts);
			return;
		}

		switch (digestType)
		{
		case RESULT_DIGEST_MD5:
			MD5Init(&hashContexts->mdContext, 0);
			break;
		case RESULT_DIGEST_SHA1:
			hashContexts->sha1.Reset();
			break;
		case RESULT_DIGEST_SHA256:
			sha256_init(&hashContexts->sha256Ctx);
			break;
		case RESULT_DIGEST_SHA512:
			SHA512_Init(&hashContexts->sha512Ctx);
			break;
		}
	}

	void FinalizeHashDigestContext(FileHashContexts& hashContexts, ResultDigestType digestType, ResultDigestStorage& digestBundle)
	{
		DigestContextOperation digestContextOperation = { 0 };
		if (TryGetDigestContextOperation(digestType, &digestContextOperation))
		{
			digestContextOperation.finalizeAction(hashContexts, digestBundle);
			return;
		}

		switch (digestType)
		{
		case RESULT_DIGEST_MD5:
			FinalizeMD5DigestContext(hashContexts, digestBundle);
			break;
		case RESULT_DIGEST_SHA1:
			FinalizeSHA1DigestContext(hashContexts, digestBundle);
			break;
		case RESULT_DIGEST_SHA256:
			FinalizeSHA256DigestContext(hashContexts, digestBundle);
			break;
		case RESULT_DIGEST_SHA512:
			FinalizeSHA512DigestContext(hashContexts, digestBundle);
			break;
		}
	}
}
