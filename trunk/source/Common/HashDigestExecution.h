#ifndef _HASH_DIGEST_EXECUTION_H_
#define _HASH_DIGEST_EXECUTION_H_

#include "Common/HashExecutionContext.h"
#include "Common/HashDigestExecutionMode.h"
#include "Common/HashDigestUpdater.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
class ThreadPool;
#endif

namespace HashEngineInternal
{
	struct FileExecutionState;

	bool ExecuteOpenedFileDigestUpdate(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, HashDigestExecutionMode digestExecutionMode, uint64_t fsize, bool isSizeCaled,
		FileExecutionState *executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		, ThreadPool *threadPool
#endif
	);
}

#endif
