#include "stdafx.h"

#include <stdio.h>
#include <string>

#include "Runtime/Hash/CRC32CHashProvider.h"

namespace HashRuntime
{
	void InitializeCRC32CHasher(uint32_t *hasher)
	{
		if (hasher == NULL)
		{
			return;
		}

		*hasher = 0;
	}

	void UpdateCRC32CHasher(uint32_t& hasher, const unsigned char *data, size_t dataLen)
	{
		if (data == NULL || dataLen == 0)
		{
			return;
		}

		hasher = crc32c_extend(hasher, data, dataLen);
	}

	sunjwbase::tstring FinalizeCRC32CHasherHex(uint32_t hasher)
	{
		char digestText[16] = { 0 };
#if defined(_WIN32)
		sprintf_s(digestText, sizeof(digestText), "%08X", hasher);
#else
		snprintf(digestText, sizeof(digestText), "%08X", hasher);
#endif
		return sunjwbase::strtotstr(std::string(digestText));
	}
}
