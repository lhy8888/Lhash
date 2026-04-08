#ifndef _HASH_DIGEST_UPDATER_H_
#define _HASH_DIGEST_UPDATER_H_

#include "Domain/HashRequest.h"
#include "Common/HashDigestOperationRegistry.h"

class ThreadPool;

namespace HashEngineInternal
{
	struct FileHashContexts;

	struct DigestUpdateRequest
	{
		std::vector<HashDigestOperationDescriptor> operationDescriptors;
	};

	template<typename TDigestUpdateOperationVisitor>
	static inline bool VisitDigestUpdateRequestOperations(const DigestUpdateRequest& digestUpdateRequest, TDigestUpdateOperationVisitor visitor)
	{
		for (size_t index = 0; index < digestUpdateRequest.operationDescriptors.size(); ++index)
		{
			if (!visitor(digestUpdateRequest.operationDescriptors[index]))
			{
				return false;
			}
		}

		return true;
	}

	DigestUpdateRequest CreateDigestUpdateRequest(const HashRequest& request);

	void UpdateDigestContextsSequential(const DigestUpdateRequest& digestUpdateRequest, FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen);

	void UpdateDigestContextsParallel(const DigestUpdateRequest& digestUpdateRequest, FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen, ThreadPool *threadPool);
}

#endif
