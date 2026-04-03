#include "stdafx.h"

#include "Common/HashEngineInternal.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
#include "Common/ThreadPool.h"
#endif

#include <future>

namespace HashEngineInternal
{
	static bool ContainsDigestUpdateOperation(const DigestUpdateRequest& digestUpdateRequest, ResultDigestType digestType)
	{
		for (size_t operationIndex = 0; operationIndex < digestUpdateRequest.operationDescriptors.size(); ++operationIndex)
		{
			if (digestUpdateRequest.operationDescriptors[operationIndex].digestType == digestType)
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
			if (ContainsDigestUpdateOperation(digestUpdateRequest, digestType))
			{
				return true;
			}

			HashDigestOperationDescriptor operationDescriptor = {};
			if (!TryGetHashDigestOperationDescriptor(digestType, &operationDescriptor))
			{
				return true;
			}

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
