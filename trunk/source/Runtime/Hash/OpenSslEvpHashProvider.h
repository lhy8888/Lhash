#ifndef _RUNTIME_HASH_OPENSSL_EVP_HASH_PROVIDER_H_
#define _RUNTIME_HASH_OPENSSL_EVP_HASH_PROVIDER_H_

#include <stddef.h>

#include "Common/Global.h"

#if defined(FHASH_WITH_OPENSSL3_VENDOR)
#include <openssl/evp.h>
#else
typedef struct evp_md_ctx_st EVP_MD_CTX;
typedef struct evp_md_st EVP_MD;
#endif

namespace HashRuntime
{
	struct OpenSslEvpHashContext
	{
		OpenSslEvpHashContext()
			: mdContext(NULL),
			mdImplementation(NULL),
			xofMode(false),
			digestOutputBytes(0)
		{
		}

		EVP_MD_CTX *mdContext;
		EVP_MD *mdImplementation;
		bool xofMode;
		size_t digestOutputBytes;
	};

	static const size_t OPENSSL_SHA_256_OUTPUT_BYTES = 32;
	static const size_t OPENSSL_SHA_384_OUTPUT_BYTES = 48;
	static const size_t OPENSSL_SHA_512_OUTPUT_BYTES = 64;
	static const size_t OPENSSL_SHA3_256_OUTPUT_BYTES = 32;
	static const size_t OPENSSL_SHA3_384_OUTPUT_BYTES = 48;
	static const size_t OPENSSL_SHA3_512_OUTPUT_BYTES = 64;
	static const size_t OPENSSL_BLAKE2B_160_OUTPUT_BYTES = 20;
	static const size_t OPENSSL_BLAKE2B_256_OUTPUT_BYTES = 32;
	static const size_t OPENSSL_BLAKE2B_512_OUTPUT_BYTES = 64;
	static const size_t OPENSSL_BLAKE2S_128_OUTPUT_BYTES = 16;
	static const size_t OPENSSL_BLAKE2S_256_OUTPUT_BYTES = 32;
	static const size_t OPENSSL_SHAKE128_256_OUTPUT_BYTES = 32;
	static const size_t OPENSSL_SHAKE256_512_OUTPUT_BYTES = 64;

	bool InitializeOpenSslEvpHashContext(
		OpenSslEvpHashContext *hashContext,
		const char *const *algorithmNames,
		size_t algorithmNameCount,
		bool xofMode,
		size_t digestOutputBytes);

	void UpdateOpenSslEvpHashContext(OpenSslEvpHashContext& hashContext, const unsigned char *data, size_t dataLen);
	sunjwbase::tstring FinalizeOpenSslEvpHashContextHex(OpenSslEvpHashContext& hashContext, size_t outputBytes);
	void CleanupOpenSslEvpHashContext(OpenSslEvpHashContext *hashContext);
}

#endif
