#include "stdafx.h"

#include "Common/HashEngineInternal.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
#include "Common/ThreadPool.h"
#endif

#include <future>

namespace HashEngineInternal
{
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
		DigestUpdateRequest digestUpdateRequest = {};

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

			HashDigestOperationDescriptor operationDescriptor = {};
			if (!TryGetHashDigestOperationDescriptor(digestType, &operationDescriptor))
			{
				return true;
			}

			digestUpdateRequest.algorithms.push_back(digestType);
			digestUpdateRequest.operationDescriptors.push_back(operationDescriptor);
			return true;
		});
		return digestUpdateRequest;
	}

	void UpdateDigestContextsSequential(const DigestUpdateRequest& digestUpdateRequest, FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen)
	{
		VisitDigestUpdateRequestOperations(digestUpdateRequest, [&](const HashDigestOperationDescriptor& operationDescriptor)
		{
			if (operationDescriptor.updateAction == NULL)
			{
				return true;
			}

			operationDescriptor.updateAction(hashContexts, data, dataLen);
			return true;
		});
	}

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
	void UpdateDigestContextsParallel(const DigestUpdateRequest& digestUpdateRequest, FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen, ThreadPool *threadPool)
	{
		std::vector<std::future<void>> digestUpdateTasks;

		VisitDigestUpdateRequestOperations(digestUpdateRequest, [&](const HashDigestOperationDescriptor& operationDescriptor)
		{
			if (operationDescriptor.updateAction == NULL)
			{
				return true;
			}

			digestUpdateTasks.push_back(threadPool->enqueue([&hashContexts, data, dataLen, operationDescriptor]()
			{
				operationDescriptor.updateAction(hashContexts, data, dataLen);
			}));

			return true;
		});

		for (size_t taskIndex = 0; taskIndex < digestUpdateTasks.size(); ++taskIndex)
		{
			digestUpdateTasks[taskIndex].wait();
		}
	}
#endif
}
