#include "stdafx.h"

#include "Common/HashEngineInternal.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
#include "Common/ThreadPool.h"
#endif

#include <future>

namespace HashEngineInternal
{
	static void MD5UpdateWrapper(MD5_CTX *mdContext, unsigned char *inBuf, unsigned int inLen)
	{
		MD5Update(mdContext, inBuf, inLen);
	}

	static void SHA1UpdateWrapper(CSHA1 *sha1, unsigned char *data, unsigned int len)
	{
		sha1->Update(data, len);
	}

	static void SHA256UpdateWrapper(struct sha256_ctx *ctx, const unsigned char *buffer, uint32_t length)
	{
		sha256_update(ctx, buffer, length);
	}

	static void SHA512UpdateWrapper(SHA512_CTX *context, void *datain, size_t len)
	{
		SHA512_Update(context, datain, len);
	}

	DigestUpdateRequest CreateDigestUpdateRequest(const HashRequest& request)
	{
		DigestUpdateRequest digestUpdateRequest = { 0 };
		digestUpdateRequest.sha512Enabled = HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA512);
		digestUpdateRequest.sha256Enabled = HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA256);
		digestUpdateRequest.sha1Enabled = HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA1);
		digestUpdateRequest.md5Enabled = HasHashRequestAlgorithm(request, RESULT_DIGEST_MD5);
		return digestUpdateRequest;
	}

	void UpdateDigestContextsSequential(const DigestUpdateRequest& digestUpdateRequest, FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		if (digestUpdateRequest.md5Enabled)
		{
			MD5UpdateWrapper(&hashContexts.mdContext, data, dataLen);
		}
		if (digestUpdateRequest.sha1Enabled)
		{
			SHA1UpdateWrapper(&hashContexts.sha1, data, dataLen);
		}
		if (digestUpdateRequest.sha256Enabled)
		{
			SHA256UpdateWrapper(&hashContexts.sha256Ctx, data, dataLen);
		}
		if (digestUpdateRequest.sha512Enabled)
		{
			SHA512UpdateWrapper(&hashContexts.sha512Ctx, data, dataLen);
		}
	}

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
	void UpdateDigestContextsParallel(const DigestUpdateRequest& digestUpdateRequest, FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen, ThreadPool *threadPool)
	{
		std::future<void> taskSHA512Update;
		std::future<void> taskSHA256Update;
		std::future<void> taskSHA1Update;
		std::future<void> taskMD5Update;

		if (digestUpdateRequest.sha512Enabled)
		{
			taskSHA512Update = threadPool->enqueue(SHA512UpdateWrapper, &hashContexts.sha512Ctx, data, dataLen);
		}
		if (digestUpdateRequest.sha256Enabled)
		{
			taskSHA256Update = threadPool->enqueue(SHA256UpdateWrapper, &hashContexts.sha256Ctx, data, dataLen);
		}
		if (digestUpdateRequest.sha1Enabled)
		{
			taskSHA1Update = threadPool->enqueue(SHA1UpdateWrapper, &hashContexts.sha1, data, dataLen);
		}
		if (digestUpdateRequest.md5Enabled)
		{
			taskMD5Update = threadPool->enqueue(MD5UpdateWrapper, &hashContexts.mdContext, data, dataLen);
		}

		if (digestUpdateRequest.sha512Enabled)
		{
			taskSHA512Update.wait();
		}
		if (digestUpdateRequest.sha256Enabled)
		{
			taskSHA256Update.wait();
		}
		if (digestUpdateRequest.sha1Enabled)
		{
			taskSHA1Update.wait();
		}
		if (digestUpdateRequest.md5Enabled)
		{
			taskMD5Update.wait();
		}
	}
#endif
}
