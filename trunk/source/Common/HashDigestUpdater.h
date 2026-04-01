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
	};

	DigestUpdateRequest CreateDigestUpdateRequest(const HashRequest& request);

	void UpdateDigestContextsSequential(const DigestUpdateRequest& digestUpdateRequest, FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen);

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
	void UpdateDigestContextsParallel(const DigestUpdateRequest& digestUpdateRequest, FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen, ThreadPool *threadPool);
#endif
}

#endif
