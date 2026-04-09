#include "stdafx.h"

#include <string>
#include <vector>

#include "Runtime/Hash/BLAKE3HashProvider.h"

namespace
{
	static char RenderUppercaseHexNibble(unsigned char nibble)
	{
		static const char kHexDigits[] = "0123456789ABCDEF";
		return kHexDigits[nibble & 0x0F];
	}

	static std::string RenderUppercaseHexString(const std::vector<uint8_t>& bytes)
	{
		std::string renderedHex;
		renderedHex.reserve(bytes.size() * 2);

		for (size_t index = 0; index < bytes.size(); ++index)
		{
			unsigned char byteValue = bytes[index];
			renderedHex.push_back(RenderUppercaseHexNibble(static_cast<unsigned char>(byteValue >> 4)));
			renderedHex.push_back(RenderUppercaseHexNibble(byteValue));
		}

		return renderedHex;
	}
}

namespace HashRuntime
{
	void InitializeBlake3Hasher(blake3_hasher *hasher)
	{
		if (hasher == NULL)
		{
			return;
		}

		blake3_hasher_init(hasher);
	}

	void UpdateBlake3Hasher(blake3_hasher& hasher, const unsigned char *data, size_t dataLen)
	{
		if (data == NULL || dataLen == 0)
		{
			return;
		}

		blake3_hasher_update(&hasher, data, dataLen);
	}

	sunjwbase::tstring FinalizeBlake3HasherHex(const blake3_hasher& hasher, size_t outputBytes)
	{
		if (outputBytes == 0)
		{
			return sunjwbase::tstring();
		}

		std::vector<uint8_t> outputBytesBuffer(outputBytes);
		blake3_hasher_finalize(&hasher, &outputBytesBuffer[0], outputBytesBuffer.size());

		return sunjwbase::strtotstr(RenderUppercaseHexString(outputBytesBuffer));
	}
}
