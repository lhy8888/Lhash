#ifndef _HASH_DIGEST_LIFECYCLE_H_
#define _HASH_DIGEST_LIFECYCLE_H_

#include "Common/Global.h"
#include "Common/HashExecutionContext.h"
#include "Common/HashRequest.h"
#include "Common/HashResult.h"

namespace HashEngineInternal
{
	struct FileHashContexts;

	void InitializeFileHashing(const HashRequest& request, HashExecutionContext *executionContext, FileHashContexts *hashContexts);
	const sunjwbase::tstring& GetFinalizedDigestValueById(const ResultDigestStorage& digestBundle, const HashAlgorithmId& algorithmId);
	const sunjwbase::tstring& GetFinalizedDigestValue(const ResultDigestStorage& digestBundle, ResultDigestType digestType);
	void SetFinalizedDigestValueById(ResultDigestStorage& digestBundle, const HashAlgorithmId& algorithmId, const sunjwbase::tstring& digestValue);
	void SetFinalizedDigestValue(ResultDigestStorage& digestBundle, ResultDigestType digestType, const sunjwbase::tstring& digestValue);
	void PopulateDigestResult(const HashRequest& request, HashResult& result, const ResultDigestStorage& digestBundle);
	void FinalizeDigestStrings(const HashRequest& request, FileHashContexts& hashContexts, ResultDigestStorage& digestBundle);
}

#endif
