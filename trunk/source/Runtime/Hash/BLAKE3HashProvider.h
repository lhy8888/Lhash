#ifndef _RUNTIME_HASH_BLAKE3_HASH_PROVIDER_H_
#define _RUNTIME_HASH_BLAKE3_HASH_PROVIDER_H_

#include <stddef.h>

#include "Common/HashTypes.h"
#include "blake3.h"

namespace HashRuntime
{
	static const size_t BLAKE3_256_OUTPUT_BYTES = BLAKE3_OUT_LEN;
	static const size_t BLAKE3_512_OUTPUT_BYTES = 64;
	static const size_t BLAKE3_XOF_OUTPUT_BYTES = 128;

	void InitializeBlake3Hasher(blake3_hasher *hasher);
	void UpdateBlake3Hasher(blake3_hasher& hasher, const unsigned char *data, size_t dataLen);
	sunjwbase::tstring FinalizeBlake3HasherHex(const blake3_hasher& hasher, size_t outputBytes);
}

#endif
