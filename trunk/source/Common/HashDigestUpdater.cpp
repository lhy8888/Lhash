#include "stdafx.h"

#include "Common/HashEngineInternal.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
#include "Common/ThreadPool.h"
#endif

#include <future>

namespace HashEngineInternal
{
	static bool IsRequestedDigestAlgorithmId(
		const std::vector<HashAlgorithmId>& normalizedAlgorithmIds,
		const HashAlgorithmId& candidateAlgorithmId)
	{
		return std::find(
			normalizedAlgorithmIds.begin(),
			normalizedAlgorithmIds.end(),
			candidateAlgorithmId) != normalizedAlgorithmIds.end();
	}

	static bool TryResolveDigestUpdateOperationDescriptor(const HashAlgorithmId& algorithmId, HashDigestOperationDescriptor *operationDescriptor)
	{
		if (!IsRegisteredHashAlgorithmId(algorithmId))
		{
			return false;
		}

		if (!TryGetHashDigestOperationDescriptorById(algorithmId, operationDescriptor))
		{
			return false;
		}

		if (operationDescriptor == NULL)
		{
			return false;
		}

		HashAlgorithmId resolvedAlgorithmId = NormalizeHashAlgorithmId(operationDescriptor->algorithmId);
		if (resolvedAlgorithmId.empty() && IsRegisteredHashAlgorithmType(operationDescriptor->digestType))
		{
			resolvedAlgorithmId = NormalizeHashAlgorithmId(GetHashAlgorithmId(operationDescriptor->digestType));
		}

		return !resolvedAlgorithmId.empty() &&
			resolvedAlgorithmId == NormalizeHashAlgorithmId(algorithmId) &&
			IsHashDigestOperationDescriptorComplete(*operationDescriptor);
	}

	DigestUpdateRequest CreateDigestUpdateRequest(const HashRequest& request)
	{
		DigestUpdateRequest digestUpdateRequest = {};
		if (!IsHashDigestOperationRegistryConsistent())
		{
			return digestUpdateRequest;
		}
		const std::vector<HashAlgorithmId> normalizedAlgorithmIds = GetHashRequestNormalizedAlgorithmIds(request);

		VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)
		{
			(void)index;
			HashAlgorithmId algorithmId = GetHashAlgorithmDescriptorId(algorithmDescriptor);
			if (!IsRequestedDigestAlgorithmId(normalizedAlgorithmIds, algorithmId))
			{
				return true;
			}

			HashDigestOperationDescriptor operationDescriptor = {};
			if (!TryResolveDigestUpdateOperationDescriptor(algorithmId, &operationDescriptor))
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
