#ifndef _HASH_DIGEST_LIFECYCLE_H_
#define _HASH_DIGEST_LIFECYCLE_H_

#include "Common/Global.h"
#include "Runtime/HashExecutionContext.h"
#include "Domain/HashRequest.h"
#include "Common/HashResult.h"

namespace HashEngineInternal
{
	struct FileHashContexts;

	void InitializeFileHashing(const HashRequest& request, HashExecutionContext *executionContext, FileHashContexts *hashContexts);
	const sunjwbase::tstring& GetFinalizedDigestValueById(const ResultDigestStorage& digestBundle, const HashAlgorithmId& algorithmId);
	void SetFinalizedDigestValueById(ResultDigestStorage& digestBundle, const HashAlgorithmId& algorithmId, const sunjwbase::tstring& digestValue);
	void PopulateDigestResult(const HashRequest& request, HashResult& result, const ResultDigestStorage& digestBundle);
	void FinalizeDigestStrings(const HashRequest& request, FileHashContexts& hashContexts, ResultDigestStorage& digestBundle);
}

#endif
