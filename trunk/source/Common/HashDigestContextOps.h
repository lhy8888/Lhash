#ifndef _HASH_DIGEST_CONTEXT_OPS_H_
#define _HASH_DIGEST_CONTEXT_OPS_H_

#include "Common/HashTypes.h"

namespace HashEngineInternal
{
	struct FileHashContexts;

	void InitializeHashDigestContextById(FileHashContexts *hashContexts, const HashAlgorithmId& algorithmId);
	bool FinalizeHashDigestContextById(FileHashContexts& hashContexts, const HashAlgorithmId& algorithmId, ResultDigestStorage& digestBundle, sunjwbase::tstring *errorText);
}

#endif
