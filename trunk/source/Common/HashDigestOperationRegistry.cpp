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

	static const HashAlgorithmId& GetBlake3_256AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("blake3-256");
		return algorithmId;
	}

	static const HashAlgorithmId& GetBlake3_512AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("blake3-512");
		return algorithmId;
	}

	static const HashAlgorithmId& GetBlake3XofAlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("blake3-xof");
		return algorithmId;
	}

	static const HashAlgorithmId& GetXXH3_64AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("xxh3-64");
		return algorithmId;
	}

	static const HashAlgorithmId& GetXXH3_128AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("xxh3-128");
		return algorithmId;
	}

	static const HashAlgorithmId& GetCRC32CAlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("crc32c");
		return algorithmId;
	}

	static const HashAlgorithmId& GetOpenSslSha256AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("openssl-sha-256");
		return algorithmId;
	}

	static const HashAlgorithmId& GetOpenSslSha384AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("openssl-sha-384");
		return algorithmId;
	}

	static const HashAlgorithmId& GetOpenSslSha512AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("openssl-sha-512");
		return algorithmId;
	}

	static const HashAlgorithmId& GetOpenSslSha3_256AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("openssl-sha3-256");
		return algorithmId;
	}

	static const HashAlgorithmId& GetOpenSslSha3_384AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("openssl-sha3-384");
		return algorithmId;
	}

	static const HashAlgorithmId& GetOpenSslSha3_512AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("openssl-sha3-512");
		return algorithmId;
	}

	static const HashAlgorithmId& GetOpenSslBlake2b_160AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("openssl-blake2b-160");
		return algorithmId;
	}

	static const HashAlgorithmId& GetOpenSslBlake2b_256AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("openssl-blake2b-256");
		return algorithmId;
	}

	static const HashAlgorithmId& GetOpenSslBlake2b_512AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("openssl-blake2b-512");
		return algorithmId;
	}

	static const HashAlgorithmId& GetOpenSslBlake2s_128AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("openssl-blake2s-128");
		return algorithmId;
	}

	static const HashAlgorithmId& GetOpenSslBlake2s_256AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("openssl-blake2s-256");
		return algorithmId;
	}

	static const HashAlgorithmId& GetOpenSslShake128_256AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("openssl-shake128-256");
		return algorithmId;
	}

	static const HashAlgorithmId& GetOpenSslShake256_512AlgorithmId()
	{
		static const HashAlgorithmId algorithmId = CreateKnownAlgorithmId("openssl-shake256-512");
		return algorithmId;
	}

	static void InitializeOpenSslDigestContext(
		HashRuntime::OpenSslEvpHashContext *hashContext,
		const char *const *algorithmNames,
		size_t algorithmNameCount,
		bool xofMode,
		size_t digestOutputBytes)
	{
		HashRuntime::InitializeOpenSslEvpHashContext(hashContext, algorithmNames, algorithmNameCount, xofMode, digestOutputBytes);
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

	static void InitializeBLAKE3_256DigestContext(FileHashContexts *hashContexts)
	{
		HashRuntime::InitializeBlake3Hasher(&hashContexts->blake3_256);
	}

	static void InitializeBLAKE3_512DigestContext(FileHashContexts *hashContexts)
	{
		HashRuntime::InitializeBlake3Hasher(&hashContexts->blake3_512);
	}

	static void InitializeBLAKE3XofDigestContext(FileHashContexts *hashContexts)
	{
		HashRuntime::InitializeBlake3Hasher(&hashContexts->blake3Xof);
	}

	static void InitializeXXH3_64DigestContext(FileHashContexts *hashContexts)
	{
		HashRuntime::InitializeXXH3_64Hasher(&hashContexts->xxh3_64);
	}

	static void InitializeXXH3_128DigestContext(FileHashContexts *hashContexts)
	{
		HashRuntime::InitializeXXH3_128Hasher(&hashContexts->xxh3_128);
	}

	static void InitializeCRC32CDigestContext(FileHashContexts *hashContexts)
	{
		HashRuntime::InitializeCRC32CHasher(&hashContexts->crc32c);
	}

	static void InitializeOpenSslSha256DigestContext(FileHashContexts *hashContexts)
	{
		static const char *const kAlgorithmNames[] = { "SHA256", "SHA-256", "SHA2-256" };
		InitializeOpenSslDigestContext(&hashContexts->openSslSha256, kAlgorithmNames, sizeof(kAlgorithmNames) / sizeof(kAlgorithmNames[0]), false, HashRuntime::OPENSSL_SHA_256_OUTPUT_BYTES);
	}

	static void InitializeOpenSslSha384DigestContext(FileHashContexts *hashContexts)
	{
		static const char *const kAlgorithmNames[] = { "SHA384", "SHA-384", "SHA2-384" };
		InitializeOpenSslDigestContext(&hashContexts->openSslSha384, kAlgorithmNames, sizeof(kAlgorithmNames) / sizeof(kAlgorithmNames[0]), false, HashRuntime::OPENSSL_SHA_384_OUTPUT_BYTES);
	}

	static void InitializeOpenSslSha512DigestContext(FileHashContexts *hashContexts)
	{
		static const char *const kAlgorithmNames[] = { "SHA512", "SHA-512", "SHA2-512" };
		InitializeOpenSslDigestContext(&hashContexts->openSslSha512, kAlgorithmNames, sizeof(kAlgorithmNames) / sizeof(kAlgorithmNames[0]), false, HashRuntime::OPENSSL_SHA_512_OUTPUT_BYTES);
	}

	static void InitializeOpenSslSha3_256DigestContext(FileHashContexts *hashContexts)
	{
		static const char *const kAlgorithmNames[] = { "SHA3-256", "SHA3_256" };
		InitializeOpenSslDigestContext(&hashContexts->openSslSha3_256, kAlgorithmNames, sizeof(kAlgorithmNames) / sizeof(kAlgorithmNames[0]), false, HashRuntime::OPENSSL_SHA3_256_OUTPUT_BYTES);
	}

	static void InitializeOpenSslSha3_384DigestContext(FileHashContexts *hashContexts)
	{
		static const char *const kAlgorithmNames[] = { "SHA3-384", "SHA3_384" };
		InitializeOpenSslDigestContext(&hashContexts->openSslSha3_384, kAlgorithmNames, sizeof(kAlgorithmNames) / sizeof(kAlgorithmNames[0]), false, HashRuntime::OPENSSL_SHA3_384_OUTPUT_BYTES);
	}

	static void InitializeOpenSslSha3_512DigestContext(FileHashContexts *hashContexts)
	{
		static const char *const kAlgorithmNames[] = { "SHA3-512", "SHA3_512" };
		InitializeOpenSslDigestContext(&hashContexts->openSslSha3_512, kAlgorithmNames, sizeof(kAlgorithmNames) / sizeof(kAlgorithmNames[0]), false, HashRuntime::OPENSSL_SHA3_512_OUTPUT_BYTES);
	}

	static void InitializeOpenSslBlake2b_160DigestContext(FileHashContexts *hashContexts)
	{
		static const char *const kAlgorithmNames[] = { "BLAKE2b512", "BLAKE2B512", "BLAKE2b-512", "BLAKE2B-512" };
		InitializeOpenSslDigestContext(&hashContexts->openSslBlake2b_160, kAlgorithmNames, sizeof(kAlgorithmNames) / sizeof(kAlgorithmNames[0]), false, HashRuntime::OPENSSL_BLAKE2B_160_OUTPUT_BYTES);
	}

	static void InitializeOpenSslBlake2b_256DigestContext(FileHashContexts *hashContexts)
	{
		static const char *const kAlgorithmNames[] = { "BLAKE2b512", "BLAKE2B512", "BLAKE2b-512", "BLAKE2B-512" };
		InitializeOpenSslDigestContext(&hashContexts->openSslBlake2b_256, kAlgorithmNames, sizeof(kAlgorithmNames) / sizeof(kAlgorithmNames[0]), false, HashRuntime::OPENSSL_BLAKE2B_256_OUTPUT_BYTES);
	}

	static void InitializeOpenSslBlake2b_512DigestContext(FileHashContexts *hashContexts)
	{
		static const char *const kAlgorithmNames[] = { "BLAKE2b512", "BLAKE2B512", "BLAKE2b-512", "BLAKE2B-512" };
		InitializeOpenSslDigestContext(&hashContexts->openSslBlake2b_512, kAlgorithmNames, sizeof(kAlgorithmNames) / sizeof(kAlgorithmNames[0]), false, HashRuntime::OPENSSL_BLAKE2B_512_OUTPUT_BYTES);
	}

	static void InitializeOpenSslBlake2s_128DigestContext(FileHashContexts *hashContexts)
	{
		static const char *const kAlgorithmNames[] = { "BLAKE2s256", "BLAKE2S256", "BLAKE2s-256", "BLAKE2S-256" };
		InitializeOpenSslDigestContext(&hashContexts->openSslBlake2s_128, kAlgorithmNames, sizeof(kAlgorithmNames) / sizeof(kAlgorithmNames[0]), false, HashRuntime::OPENSSL_BLAKE2S_128_OUTPUT_BYTES);
	}

	static void InitializeOpenSslBlake2s_256DigestContext(FileHashContexts *hashContexts)
	{
		static const char *const kAlgorithmNames[] = { "BLAKE2s256", "BLAKE2S256", "BLAKE2s-256", "BLAKE2S-256" };
		InitializeOpenSslDigestContext(&hashContexts->openSslBlake2s_256, kAlgorithmNames, sizeof(kAlgorithmNames) / sizeof(kAlgorithmNames[0]), false, HashRuntime::OPENSSL_BLAKE2S_256_OUTPUT_BYTES);
	}

	static void InitializeOpenSslShake128_256DigestContext(FileHashContexts *hashContexts)
	{
		static const char *const kAlgorithmNames[] = { "SHAKE128", "SHAKE-128" };
		InitializeOpenSslDigestContext(&hashContexts->openSslShake128_256, kAlgorithmNames, sizeof(kAlgorithmNames) / sizeof(kAlgorithmNames[0]), true, HashRuntime::OPENSSL_SHAKE128_256_OUTPUT_BYTES);
	}

	static void InitializeOpenSslShake256_512DigestContext(FileHashContexts *hashContexts)
	{
		static const char *const kAlgorithmNames[] = { "SHAKE256", "SHAKE-256" };
		InitializeOpenSslDigestContext(&hashContexts->openSslShake256_512, kAlgorithmNames, sizeof(kAlgorithmNames) / sizeof(kAlgorithmNames[0]), true, HashRuntime::OPENSSL_SHAKE256_512_OUTPUT_BYTES);
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

	static void UpdateBLAKE3_256DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateBlake3Hasher(hashContexts.blake3_256, data, static_cast<size_t>(dataLen));
	}

	static void UpdateBLAKE3_512DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateBlake3Hasher(hashContexts.blake3_512, data, static_cast<size_t>(dataLen));
	}

	static void UpdateBLAKE3XofDigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateBlake3Hasher(hashContexts.blake3Xof, data, static_cast<size_t>(dataLen));
	}

	static void UpdateXXH3_64DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateXXH3_64Hasher(hashContexts.xxh3_64, data, static_cast<size_t>(dataLen));
	}

	static void UpdateXXH3_128DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateXXH3_128Hasher(hashContexts.xxh3_128, data, static_cast<size_t>(dataLen));
	}

	static void UpdateCRC32CDigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateCRC32CHasher(hashContexts.crc32c, data, static_cast<size_t>(dataLen));
	}

	static void UpdateOpenSslSha256DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateOpenSslEvpHashContext(hashContexts.openSslSha256, data, static_cast<size_t>(dataLen));
	}

	static void UpdateOpenSslSha384DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateOpenSslEvpHashContext(hashContexts.openSslSha384, data, static_cast<size_t>(dataLen));
	}

	static void UpdateOpenSslSha512DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateOpenSslEvpHashContext(hashContexts.openSslSha512, data, static_cast<size_t>(dataLen));
	}

	static void UpdateOpenSslSha3_256DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateOpenSslEvpHashContext(hashContexts.openSslSha3_256, data, static_cast<size_t>(dataLen));
	}

	static void UpdateOpenSslSha3_384DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateOpenSslEvpHashContext(hashContexts.openSslSha3_384, data, static_cast<size_t>(dataLen));
	}

	static void UpdateOpenSslSha3_512DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateOpenSslEvpHashContext(hashContexts.openSslSha3_512, data, static_cast<size_t>(dataLen));
	}

	static void UpdateOpenSslBlake2b_160DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateOpenSslEvpHashContext(hashContexts.openSslBlake2b_160, data, static_cast<size_t>(dataLen));
	}

	static void UpdateOpenSslBlake2b_256DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateOpenSslEvpHashContext(hashContexts.openSslBlake2b_256, data, static_cast<size_t>(dataLen));
	}

	static void UpdateOpenSslBlake2b_512DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateOpenSslEvpHashContext(hashContexts.openSslBlake2b_512, data, static_cast<size_t>(dataLen));
	}

	static void UpdateOpenSslBlake2s_128DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateOpenSslEvpHashContext(hashContexts.openSslBlake2s_128, data, static_cast<size_t>(dataLen));
	}

	static void UpdateOpenSslBlake2s_256DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateOpenSslEvpHashContext(hashContexts.openSslBlake2s_256, data, static_cast<size_t>(dataLen));
	}

	static void UpdateOpenSslShake128_256DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateOpenSslEvpHashContext(hashContexts.openSslShake128_256, data, static_cast<size_t>(dataLen));
	}

	static void UpdateOpenSslShake256_512DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashRuntime::UpdateOpenSslEvpHashContext(hashContexts.openSslShake256_512, data, static_cast<size_t>(dataLen));
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

	static void FinalizeBLAKE3_256DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetBlake3_256AlgorithmId(),
			HashRuntime::FinalizeBlake3HasherHex(hashContexts.blake3_256, HashRuntime::BLAKE3_256_OUTPUT_BYTES));
	}

	static void FinalizeBLAKE3_512DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetBlake3_512AlgorithmId(),
			HashRuntime::FinalizeBlake3HasherHex(hashContexts.blake3_512, HashRuntime::BLAKE3_512_OUTPUT_BYTES));
	}

	static void FinalizeBLAKE3XofDigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetBlake3XofAlgorithmId(),
			HashRuntime::FinalizeBlake3HasherHex(hashContexts.blake3Xof, HashRuntime::BLAKE3_XOF_OUTPUT_BYTES));
	}

	static void FinalizeXXH3_64DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetXXH3_64AlgorithmId(),
			HashRuntime::FinalizeXXH3_64HasherHex(hashContexts.xxh3_64));
	}

	static void FinalizeXXH3_128DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetXXH3_128AlgorithmId(),
			HashRuntime::FinalizeXXH3_128HasherHex(hashContexts.xxh3_128));
	}

	static void FinalizeCRC32CDigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetCRC32CAlgorithmId(),
			HashRuntime::FinalizeCRC32CHasherHex(hashContexts.crc32c));
	}

	static void FinalizeOpenSslSha256DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetOpenSslSha256AlgorithmId(),
			HashRuntime::FinalizeOpenSslEvpHashContextHex(hashContexts.openSslSha256, HashRuntime::OPENSSL_SHA_256_OUTPUT_BYTES));
	}

	static void FinalizeOpenSslSha384DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetOpenSslSha384AlgorithmId(),
			HashRuntime::FinalizeOpenSslEvpHashContextHex(hashContexts.openSslSha384, HashRuntime::OPENSSL_SHA_384_OUTPUT_BYTES));
	}

	static void FinalizeOpenSslSha512DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetOpenSslSha512AlgorithmId(),
			HashRuntime::FinalizeOpenSslEvpHashContextHex(hashContexts.openSslSha512, HashRuntime::OPENSSL_SHA_512_OUTPUT_BYTES));
	}

	static void FinalizeOpenSslSha3_256DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetOpenSslSha3_256AlgorithmId(),
			HashRuntime::FinalizeOpenSslEvpHashContextHex(hashContexts.openSslSha3_256, HashRuntime::OPENSSL_SHA3_256_OUTPUT_BYTES));
	}

	static void FinalizeOpenSslSha3_384DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetOpenSslSha3_384AlgorithmId(),
			HashRuntime::FinalizeOpenSslEvpHashContextHex(hashContexts.openSslSha3_384, HashRuntime::OPENSSL_SHA3_384_OUTPUT_BYTES));
	}

	static void FinalizeOpenSslSha3_512DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetOpenSslSha3_512AlgorithmId(),
			HashRuntime::FinalizeOpenSslEvpHashContextHex(hashContexts.openSslSha3_512, HashRuntime::OPENSSL_SHA3_512_OUTPUT_BYTES));
	}

	static void FinalizeOpenSslBlake2b_160DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetOpenSslBlake2b_160AlgorithmId(),
			HashRuntime::FinalizeOpenSslEvpHashContextHex(hashContexts.openSslBlake2b_160, HashRuntime::OPENSSL_BLAKE2B_160_OUTPUT_BYTES));
	}

	static void FinalizeOpenSslBlake2b_256DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetOpenSslBlake2b_256AlgorithmId(),
			HashRuntime::FinalizeOpenSslEvpHashContextHex(hashContexts.openSslBlake2b_256, HashRuntime::OPENSSL_BLAKE2B_256_OUTPUT_BYTES));
	}

	static void FinalizeOpenSslBlake2b_512DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetOpenSslBlake2b_512AlgorithmId(),
			HashRuntime::FinalizeOpenSslEvpHashContextHex(hashContexts.openSslBlake2b_512, HashRuntime::OPENSSL_BLAKE2B_512_OUTPUT_BYTES));
	}

	static void FinalizeOpenSslBlake2s_128DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetOpenSslBlake2s_128AlgorithmId(),
			HashRuntime::FinalizeOpenSslEvpHashContextHex(hashContexts.openSslBlake2s_128, HashRuntime::OPENSSL_BLAKE2S_128_OUTPUT_BYTES));
	}

	static void FinalizeOpenSslBlake2s_256DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetOpenSslBlake2s_256AlgorithmId(),
			HashRuntime::FinalizeOpenSslEvpHashContextHex(hashContexts.openSslBlake2s_256, HashRuntime::OPENSSL_BLAKE2S_256_OUTPUT_BYTES));
	}

	static void FinalizeOpenSslShake128_256DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetOpenSslShake128_256AlgorithmId(),
			HashRuntime::FinalizeOpenSslEvpHashContextHex(hashContexts.openSslShake128_256, HashRuntime::OPENSSL_SHAKE128_256_OUTPUT_BYTES));
	}

	static void FinalizeOpenSslShake256_512DigestContext(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		SetDigestStorageValueById(
			digestBundle,
			GetOpenSslShake256_512AlgorithmId(),
			HashRuntime::FinalizeOpenSslEvpHashContextHex(hashContexts.openSslShake256_512, HashRuntime::OPENSSL_SHAKE256_512_OUTPUT_BYTES));
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
		RegisterHashDigestOperationDescriptor({
			GetBlake3_256AlgorithmId(),
			InitializeBLAKE3_256DigestContext,
			UpdateBLAKE3_256DigestContext,
			FinalizeBLAKE3_256DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetBlake3_512AlgorithmId(),
			InitializeBLAKE3_512DigestContext,
			UpdateBLAKE3_512DigestContext,
			FinalizeBLAKE3_512DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetBlake3XofAlgorithmId(),
			InitializeBLAKE3XofDigestContext,
			UpdateBLAKE3XofDigestContext,
			FinalizeBLAKE3XofDigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetXXH3_64AlgorithmId(),
			InitializeXXH3_64DigestContext,
			UpdateXXH3_64DigestContext,
			FinalizeXXH3_64DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetXXH3_128AlgorithmId(),
			InitializeXXH3_128DigestContext,
			UpdateXXH3_128DigestContext,
			FinalizeXXH3_128DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetCRC32CAlgorithmId(),
			InitializeCRC32CDigestContext,
			UpdateCRC32CDigestContext,
			FinalizeCRC32CDigestContext
		});
#if defined(FHASH_WITH_OPENSSL3_VENDOR)
		RegisterHashDigestOperationDescriptor({
			GetOpenSslSha256AlgorithmId(),
			InitializeOpenSslSha256DigestContext,
			UpdateOpenSslSha256DigestContext,
			FinalizeOpenSslSha256DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetOpenSslSha384AlgorithmId(),
			InitializeOpenSslSha384DigestContext,
			UpdateOpenSslSha384DigestContext,
			FinalizeOpenSslSha384DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetOpenSslSha512AlgorithmId(),
			InitializeOpenSslSha512DigestContext,
			UpdateOpenSslSha512DigestContext,
			FinalizeOpenSslSha512DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetOpenSslSha3_256AlgorithmId(),
			InitializeOpenSslSha3_256DigestContext,
			UpdateOpenSslSha3_256DigestContext,
			FinalizeOpenSslSha3_256DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetOpenSslSha3_384AlgorithmId(),
			InitializeOpenSslSha3_384DigestContext,
			UpdateOpenSslSha3_384DigestContext,
			FinalizeOpenSslSha3_384DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetOpenSslSha3_512AlgorithmId(),
			InitializeOpenSslSha3_512DigestContext,
			UpdateOpenSslSha3_512DigestContext,
			FinalizeOpenSslSha3_512DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetOpenSslBlake2b_160AlgorithmId(),
			InitializeOpenSslBlake2b_160DigestContext,
			UpdateOpenSslBlake2b_160DigestContext,
			FinalizeOpenSslBlake2b_160DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetOpenSslBlake2b_256AlgorithmId(),
			InitializeOpenSslBlake2b_256DigestContext,
			UpdateOpenSslBlake2b_256DigestContext,
			FinalizeOpenSslBlake2b_256DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetOpenSslBlake2b_512AlgorithmId(),
			InitializeOpenSslBlake2b_512DigestContext,
			UpdateOpenSslBlake2b_512DigestContext,
			FinalizeOpenSslBlake2b_512DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetOpenSslBlake2s_128AlgorithmId(),
			InitializeOpenSslBlake2s_128DigestContext,
			UpdateOpenSslBlake2s_128DigestContext,
			FinalizeOpenSslBlake2s_128DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetOpenSslBlake2s_256AlgorithmId(),
			InitializeOpenSslBlake2s_256DigestContext,
			UpdateOpenSslBlake2s_256DigestContext,
			FinalizeOpenSslBlake2s_256DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetOpenSslShake128_256AlgorithmId(),
			InitializeOpenSslShake128_256DigestContext,
			UpdateOpenSslShake128_256DigestContext,
			FinalizeOpenSslShake128_256DigestContext
		});
		RegisterHashDigestOperationDescriptor({
			GetOpenSslShake256_512AlgorithmId(),
			InitializeOpenSslShake256_512DigestContext,
			UpdateOpenSslShake256_512DigestContext,
			FinalizeOpenSslShake256_512DigestContext
		});
#endif
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
