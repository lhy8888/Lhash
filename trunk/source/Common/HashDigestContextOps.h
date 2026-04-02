#ifndef _HASH_DIGEST_CONTEXT_OPS_H_
#define _HASH_DIGEST_CONTEXT_OPS_H_

#include "Common/Global.h"

namespace HashEngineInternal
{
	struct FileHashContexts;

	void InitializeHashDigestContext(FileHashContexts *hashContexts, ResultDigestType digestType);
	void FinalizeHashDigestContext(FileHashContexts& hashContexts, ResultDigestType digestType, ResultDigestStorage& digestBundle);
}

#endif
