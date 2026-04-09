#ifndef _RUNTIME_HASH_XXHASH3_HASH_PROVIDER_H_
#define _RUNTIME_HASH_XXHASH3_HASH_PROVIDER_H_

#include <stddef.h>

#include "Common/Global.h"

#ifndef XXH_STATIC_LINKING_ONLY
#define XXH_STATIC_LINKING_ONLY
#endif
#include "xxhash.h"

namespace HashRuntime
{
	static const size_t XXH3_64_OUTPUT_BYTES = sizeof(XXH64_hash_t);
	static const size_t XXH3_128_OUTPUT_BYTES = sizeof(XXH128_hash_t);

	void InitializeXXH3_64Hasher(XXH3_state_t *hasher);
	void InitializeXXH3_128Hasher(XXH3_state_t *hasher);
	void UpdateXXH3_64Hasher(XXH3_state_t& hasher, const unsigned char *data, size_t dataLen);
	void UpdateXXH3_128Hasher(XXH3_state_t& hasher, const unsigned char *data, size_t dataLen);
	sunjwbase::tstring FinalizeXXH3_64HasherHex(const XXH3_state_t& hasher);
	sunjwbase::tstring FinalizeXXH3_128HasherHex(const XXH3_state_t& hasher);
}

#endif
