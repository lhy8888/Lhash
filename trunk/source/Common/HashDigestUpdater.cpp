#include "stdafx.h"

#include "Common/HashEngineInternal.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
#include "Common/ThreadPool.h"
#endif

#include <future>

namespace HashEngineInternal
{
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

	static bool TryUpdateDigestContext(ResultDigestType digestType, FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		HashDigestOperationDescriptor operationDescriptor = { 0 };
		if (!TryGetHashDigestOperationDescriptor(digestType, &operationDescriptor))
		{
			return false;
		}

		if (operationDescriptor.updateAction == NULL)
		{
			return false;
		}

		operationDescriptor.updateAction(hashContexts, data, dataLen);
		return true;
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
			TryUpdateDigestContext(RESULT_DIGEST_MD5, hashContexts, data, dataLen);
		}
		if (digestUpdateRequest.sha1Enabled)
		{
			TryUpdateDigestContext(RESULT_DIGEST_SHA1, hashContexts, data, dataLen);
		}
		if (digestUpdateRequest.sha256Enabled)
		{
			TryUpdateDigestContext(RESULT_DIGEST_SHA256, hashContexts, data, dataLen);
		}
		if (digestUpdateRequest.sha512Enabled)
		{
			TryUpdateDigestContext(RESULT_DIGEST_SHA512, hashContexts, data, dataLen);
		}

		VisitDigestUpdateRequestAlgorithms(digestUpdateRequest, [&](ResultDigestType digestType)
		{
			if (IsLegacyDigestType(digestType))
			{
				return true;
			}

			TryUpdateDigestContext(digestType, hashContexts, data, dataLen);
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
			taskSHA512Update = threadPool->enqueue([&hashContexts, data, dataLen]()
			{
				TryUpdateDigestContext(RESULT_DIGEST_SHA512, hashContexts, data, dataLen);
			});
		}
		if (digestUpdateRequest.sha256Enabled)
		{
			taskSHA256Update = threadPool->enqueue([&hashContexts, data, dataLen]()
			{
				TryUpdateDigestContext(RESULT_DIGEST_SHA256, hashContexts, data, dataLen);
			});
		}
		if (digestUpdateRequest.sha1Enabled)
		{
			taskSHA1Update = threadPool->enqueue([&hashContexts, data, dataLen]()
			{
				TryUpdateDigestContext(RESULT_DIGEST_SHA1, hashContexts, data, dataLen);
			});
		}
		if (digestUpdateRequest.md5Enabled)
		{
			taskMD5Update = threadPool->enqueue([&hashContexts, data, dataLen]()
			{
				TryUpdateDigestContext(RESULT_DIGEST_MD5, hashContexts, data, dataLen);
			});
		}

		VisitDigestUpdateRequestAlgorithms(digestUpdateRequest, [&](ResultDigestType digestType)
		{
			if (IsLegacyDigestType(digestType))
			{
				return true;
			}

			extensionDigestUpdateTasks.push_back(threadPool->enqueue([&hashContexts, data, dataLen, digestType]()
			{
				TryUpdateDigestContext(digestType, hashContexts, data, dataLen);
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
