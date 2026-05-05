#ifndef _RUNTIME_HASH_CRC32C_HASH_PROVIDER_H_
#define _RUNTIME_HASH_CRC32C_HASH_PROVIDER_H_

#include <stdint.h>
#include <stddef.h>

#include "Common/HashTypes.h"
#include "crc32c/crc32c.h"

namespace HashRuntime
{
	static const size_t CRC32C_OUTPUT_BYTES = sizeof(uint32_t);

	void InitializeCRC32CHasher(uint32_t *hasher);
	void UpdateCRC32CHasher(uint32_t& hasher, const unsigned char *data, size_t dataLen);
	sunjwbase::tstring FinalizeCRC32CHasherHex(uint32_t hasher);
}

#endif
