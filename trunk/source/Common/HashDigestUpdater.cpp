#include "stdafx.h"

#include "Common/HashEngineInternal.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
#include "Common/ThreadPool.h"
#endif

#include <future>

namespace HashEngineInternal
{
	typedef void (*DigestUpdateAction)(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen);

	struct DigestUpdateOperation
	{
		ResultDigestType digestType;
		DigestUpdateAction updateAction;
	};

	static bool IsLegacyDigestType(ResultDigestType digestType)
	{
		switch (digestType)
		{
		case RESULT_DIGEST_MD5:
		case RESULT_DIGEST_SHA1:
		case RESULT_DIGEST_SHA256:
		case RESULT_DIGEST_SHA512:
			return true;
		}

		return false;
	}

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

	static void UpdateMD5DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		MD5UpdateWrapper(&hashContexts.mdContext, data, dataLen);
	}

	static void UpdateSHA1DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		SHA1UpdateWrapper(&hashContexts.sha1, data, dataLen);
	}

	static void UpdateSHA256DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		SHA256UpdateWrapper(&hashContexts.sha256Ctx, data, dataLen);
	}

	static void UpdateSHA512DigestContext(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		SHA512UpdateWrapper(&hashContexts.sha512Ctx, data, dataLen);
	}

	static const DigestUpdateOperation *GetDigestUpdateOperations(size_t *operationCount)
	{
		static const DigestUpdateOperation digestUpdateOperations[] =
		{
			{ RESULT_DIGEST_MD5, UpdateMD5DigestContext },
			{ RESULT_DIGEST_SHA1, UpdateSHA1DigestContext },
			{ RESULT_DIGEST_SHA256, UpdateSHA256DigestContext },
			{ RESULT_DIGEST_SHA512, UpdateSHA512DigestContext }
		};
		*operationCount = sizeof(digestUpdateOperations) / sizeof(DigestUpdateOperation);
		return digestUpdateOperations;
	}

	static bool TryGetDigestUpdateOperation(ResultDigestType digestType, DigestUpdateOperation *digestUpdateOperation)
	{
		size_t operationCount = 0;
		const DigestUpdateOperation *digestUpdateOperations = GetDigestUpdateOperations(&operationCount);
		for (size_t operationIndex = 0; operationIndex < operationCount; ++operationIndex)
		{
			if (digestUpdateOperations[operationIndex].digestType != digestType)
			{
				continue;
			}

			*digestUpdateOperation = digestUpdateOperations[operationIndex];
			return true;
		}

		return false;
	}

	static bool ContainsDigestUpdateAlgorithm(const DigestUpdateRequest& digestUpdateRequest, ResultDigestType digestType)
	{
		for (size_t algorithmIndex = 0; algorithmIndex < digestUpdateRequest.algorithms.size(); ++algorithmIndex)
		{
			if (digestUpdateRequest.algorithms[algorithmIndex] == digestType)
			{
				return true;
			}
		}

		return false;
	}

	DigestUpdateRequest CreateDigestUpdateRequest(const HashRequest& request)
	{
		DigestUpdateRequest digestUpdateRequest = { 0 };
		digestUpdateRequest.sha512Enabled = HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA512);
		digestUpdateRequest.sha256Enabled = HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA256);
		digestUpdateRequest.sha1Enabled = HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA1);
		digestUpdateRequest.md5Enabled = HasHashRequestAlgorithm(request, RESULT_DIGEST_MD5);

		VisitHashRequestAlgorithms(request, [&](ResultDigestType digestType)
		{
			if (!IsRegisteredHashAlgorithmType(digestType))
			{
				return true;
			}
			if (ContainsDigestUpdateAlgorithm(digestUpdateRequest, digestType))
			{
				return true;
			}

			digestUpdateRequest.algorithms.push_back(digestType);
			return true;
		});
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

		VisitDigestUpdateRequestAlgorithms(digestUpdateRequest, [&](ResultDigestType digestType)
		{
			if (IsLegacyDigestType(digestType))
			{
				return true;
			}

			DigestUpdateOperation digestUpdateOperation = { 0 };
			if (!TryGetDigestUpdateOperation(digestType, &digestUpdateOperation))
			{
				return true;
			}

			digestUpdateOperation.updateAction(hashContexts, data, dataLen);
			return true;
		});
	}

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
	void UpdateDigestContextsParallel(const DigestUpdateRequest& digestUpdateRequest, FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen, ThreadPool *threadPool)
	{
		std::future<void> taskSHA512Update;
		std::future<void> taskSHA256Update;
		std::future<void> taskSHA1Update;
		std::future<void> taskMD5Update;
		std::vector<std::future<void>> extensionDigestUpdateTasks;

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

		VisitDigestUpdateRequestAlgorithms(digestUpdateRequest, [&](ResultDigestType digestType)
		{
			if (IsLegacyDigestType(digestType))
			{
				return true;
			}

			DigestUpdateOperation digestUpdateOperation = { 0 };
			if (!TryGetDigestUpdateOperation(digestType, &digestUpdateOperation))
			{
				return true;
			}

			extensionDigestUpdateTasks.push_back(threadPool->enqueue([&hashContexts, data, dataLen, digestUpdateOperation]()
			{
				digestUpdateOperation.updateAction(hashContexts, data, dataLen);
			}));
			return true;
		});

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

		for (size_t taskIndex = 0; taskIndex < extensionDigestUpdateTasks.size(); ++taskIndex)
		{
			extensionDigestUpdateTasks[taskIndex].wait();
		}
	}
#endif
}
