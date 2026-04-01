#ifndef _HASH_DIGEST_QUEUE_H_
#define _HASH_DIGEST_QUEUE_H_

#include "Common/HashExecutionContext.h"
#include "Common/HashDigestUpdater.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
class ThreadPool;
#endif

namespace HashEngineInternal
{
	struct FileExecutionState;

	class DigestDataBuffer
	{
	public:
		DigestDataBuffer();
		~DigestDataBuffer();

		static unsigned int preflen;

		unsigned int datalen;
		unsigned char *data;
	};

	unsigned int GetDigestDataBufferPreferredLength();
	uint64_t CalculateFileChunkIterations(uint64_t fileSize);
	bool ReadDigestDataBuffer(FileExecutionState *executionState, DigestDataBuffer& dataBuffer);

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
	bool ProcessOpenedFileHashingParallel(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fileSize, bool isSizeCaled,
		FileExecutionState *executionState, ThreadPool *threadPool);
#endif
}

#endif
