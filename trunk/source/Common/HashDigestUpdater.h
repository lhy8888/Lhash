#ifndef _HASH_DIGEST_UPDATER_H_
#define _HASH_DIGEST_UPDATER_H_

#include "Common/HashRequest.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
class ThreadPool;
#endif

namespace HashEngineInternal
{
	struct FileHashContexts;

	struct DigestUpdateRequest
	{
		std::vector<ResultDigestType> algorithms;
	};

	template<typename TDigestTypeVisitor>
	static inline bool VisitDigestUpdateRequestAlgorithms(const DigestUpdateRequest& digestUpdateRequest, TDigestTypeVisitor visitor)
	{
		for (size_t index = 0; index < digestUpdateRequest.algorithms.size(); ++index)
		{
			if (!visitor(digestUpdateRequest.algorithms[index]))
			{
				return false;
			}
		}
		return true;
	}

	static inline bool HasDigestUpdateRequestAlgorithm(const DigestUpdateRequest& digestUpdateRequest, ResultDigestType digestType)
	{
		for (size_t index = 0; index < digestUpdateRequest.algorithms.size(); ++index)
		{
			if (digestUpdateRequest.algorithms[index] == digestType)
			{
				return true;
			}
		}

		return false;
	}

	DigestUpdateRequest CreateDigestUpdateRequest(const HashRequest& request);

	void UpdateDigestContextsSequential(const DigestUpdateRequest& digestUpdateRequest, FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen);

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
	void UpdateDigestContextsParallel(const DigestUpdateRequest& digestUpdateRequest, FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen, ThreadPool *threadPool);
#endif
}

#endif
