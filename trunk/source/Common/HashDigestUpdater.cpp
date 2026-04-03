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
		if (HasDigestUpdateRequestAlgorithm(digestUpdateRequest, RESULT_DIGEST_MD5))
		{
			TryUpdateDigestContext(RESULT_DIGEST_MD5, hashContexts, data, dataLen);
		}
		if (HasDigestUpdateRequestAlgorithm(digestUpdateRequest, RESULT_DIGEST_SHA1))
		{
			TryUpdateDigestContext(RESULT_DIGEST_SHA1, hashContexts, data, dataLen);
		}
		if (HasDigestUpdateRequestAlgorithm(digestUpdateRequest, RESULT_DIGEST_SHA256))
		{
			TryUpdateDigestContext(RESULT_DIGEST_SHA256, hashContexts, data, dataLen);
		}
		if (HasDigestUpdateRequestAlgorithm(digestUpdateRequest, RESULT_DIGEST_SHA512))
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

		if (HasDigestUpdateRequestAlgorithm(digestUpdateRequest, RESULT_DIGEST_SHA512))
		{
			taskSHA512Update = threadPool->enqueue([&hashContexts, data, dataLen]()
			{
				TryUpdateDigestContext(RESULT_DIGEST_SHA512, hashContexts, data, dataLen);
			});
		}
		if (HasDigestUpdateRequestAlgorithm(digestUpdateRequest, RESULT_DIGEST_SHA256))
		{
			taskSHA256Update = threadPool->enqueue([&hashContexts, data, dataLen]()
			{
				TryUpdateDigestContext(RESULT_DIGEST_SHA256, hashContexts, data, dataLen);
			});
		}
		if (HasDigestUpdateRequestAlgorithm(digestUpdateRequest, RESULT_DIGEST_SHA1))
		{
			taskSHA1Update = threadPool->enqueue([&hashContexts, data, dataLen]()
			{
				TryUpdateDigestContext(RESULT_DIGEST_SHA1, hashContexts, data, dataLen);
			});
		}
		if (HasDigestUpdateRequestAlgorithm(digestUpdateRequest, RESULT_DIGEST_MD5))
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

		if (HasDigestUpdateRequestAlgorithm(digestUpdateRequest, RESULT_DIGEST_SHA512))
		{
			taskSHA512Update.wait();
		}
		if (HasDigestUpdateRequestAlgorithm(digestUpdateRequest, RESULT_DIGEST_SHA256))
		{
			taskSHA256Update.wait();
		}
		if (HasDigestUpdateRequestAlgorithm(digestUpdateRequest, RESULT_DIGEST_SHA1))
		{
			taskSHA1Update.wait();
		}
		if (HasDigestUpdateRequestAlgorithm(digestUpdateRequest, RESULT_DIGEST_MD5))
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
