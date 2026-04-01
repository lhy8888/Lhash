#ifndef _HASH_DIGEST_SINGLE_PASS_H_
#define _HASH_DIGEST_SINGLE_PASS_H_

#include "Common/HashExecutionContext.h"
#include "Common/HashDigestUpdater.h"

namespace HashEngineInternal
{
	struct FileExecutionState;

	bool ProcessOpenedFileHashingSinglePass(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fsize, bool isSizeCaled,
		FileExecutionState *executionState);
}

#endif
