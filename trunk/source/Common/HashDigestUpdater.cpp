#include "stdafx.h"

#include "Common/HashEngineInternal.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
#include "Common/ThreadPool.h"
#endif

#include <future>

namespace HashEngineInternal
{
	static bool TryResolveDigestUpdateOperationDescriptor(ResultDigestType digestType, HashDigestOperationDescriptor *operationDescriptor)
	{
		if (!IsRegisteredHashAlgorithmType(digestType))
		{
			return false;
		}

		if (!TryGetHashDigestOperationDescriptor(digestType, operationDescriptor))
		{
			return false;
		}

		if (operationDescriptor == NULL)
		{
			return false;
		}

		return operationDescriptor->digestType == digestType &&
			IsHashDigestOperationDescriptorComplete(*operationDescriptor);
	}

	DigestUpdateRequest CreateDigestUpdateRequest(const HashRequest& request)
	{
		DigestUpdateRequest digestUpdateRequest = {};
		if (!IsHashDigestOperationRegistryConsistent())
		{
			return digestUpdateRequest;
		}
		HashAlgorithmSelectionState selectedAlgorithms = CreateHashRequestAlgorithmSelectionState(request);

		VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)
		{
			(void)index;
			ResultDigestType digestType = GetHashAlgorithmDescriptorType(algorithmDescriptor);
			if (!IsHashRequestAlgorithmSelected(selectedAlgorithms, digestType))
			{
				return true;
			}

			HashDigestOperationDescriptor operationDescriptor = {};
			if (!TryResolveDigestUpdateOperationDescriptor(digestType, &operationDescriptor))
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
