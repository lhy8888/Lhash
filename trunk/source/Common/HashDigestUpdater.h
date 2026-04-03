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
		bool sha512Enabled;
		bool sha256Enabled;
		bool sha1Enabled;
		bool md5Enabled;
		std::vector<ResultDigestType> algorithms;
	};

	template<typename TDigestTypeVisitor>
	static inline bool VisitDigestUpdateRequestAlgorithms(const DigestUpdateRequest& digestUpdateRequest, TDigestTypeVisitor visitor)
	{
		if (!digestUpdateRequest.algorithms.empty())
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

		if (digestUpdateRequest.md5Enabled && !visitor(RESULT_DIGEST_MD5))
		{
			return false;
		}
		if (digestUpdateRequest.sha1Enabled && !visitor(RESULT_DIGEST_SHA1))
		{
			return false;
		}
		if (digestUpdateRequest.sha256Enabled && !visitor(RESULT_DIGEST_SHA256))
		{
			return false;
		}
		if (digestUpdateRequest.sha512Enabled && !visitor(RESULT_DIGEST_SHA512))
		{
			return false;
		}

		return true;
	}

	DigestUpdateRequest CreateDigestUpdateRequest(const HashRequest& request);

	void UpdateDigestContextsSequential(const DigestUpdateRequest& digestUpdateRequest, FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen);

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
	void UpdateDigestContextsParallel(const DigestUpdateRequest& digestUpdateRequest, FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen, ThreadPool *threadPool);
#endif
}

#endif
