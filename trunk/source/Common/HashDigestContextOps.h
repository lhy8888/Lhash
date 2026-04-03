#ifndef _HASH_DIGEST_CONTEXT_OPS_H_
#define _HASH_DIGEST_CONTEXT_OPS_H_

#include "Common/Global.h"

namespace HashEngineInternal
{
	struct FileHashContexts;

	void InitializeHashDigestContextById(FileHashContexts *hashContexts, const HashAlgorithmId& algorithmId);
	void InitializeHashDigestContext(FileHashContexts *hashContexts, ResultDigestType digestType);
	void FinalizeHashDigestContextById(FileHashContexts& hashContexts, const HashAlgorithmId& algorithmId, ResultDigestStorage& digestBundle);
	void FinalizeHashDigestContext(FileHashContexts& hashContexts, ResultDigestType digestType, ResultDigestStorage& digestBundle);
}

#endif
