#ifndef _HASH_DIGEST_QUEUE_H_
#define _HASH_DIGEST_QUEUE_H_

#include "Runtime/HashExecutionContext.h"
#include "Common/HashDigestQueuePlan.h"
#include "Common/HashDigestUpdater.h"

class ThreadPool;

namespace HashEngineInternal
{
	struct FileExecutionState;

	class DigestDataBuffer
	{
	public:
		explicit DigestDataBuffer(unsigned int preferredLength);
		~DigestDataBuffer();

		unsigned int datalen;
		unsigned int capacity;
		unsigned char *data;
	};

	unsigned int NormalizeDigestDataBufferPreferredLength(unsigned int preferredLength);
	uint64_t CalculateFileChunkIterations(uint64_t fileSize, unsigned int preferredLength);
	bool ReadDigestDataBuffer(FileExecutionState *executionState, DigestDataBuffer& dataBuffer);

	bool ProcessOpenedFileHashingParallel(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fileSize, bool isSizeCaled,
		unsigned int preferredBufferLength, const HashDigestQueuePlan& digestQueuePlan, FileExecutionState *executionState, ThreadPool *threadPool);
}

#endif
