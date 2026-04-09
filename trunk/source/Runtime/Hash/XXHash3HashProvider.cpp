#include "stdafx.h"

#include <string>

#include "Runtime/Hash/XXHash3HashProvider.h"

namespace
{
	template <typename TCanonical>
	static std::string RenderUppercaseHexString(const TCanonical& canonicalBytes)
	{
		static const char kHexDigits[] = "0123456789ABCDEF";
		std::string renderedHex;
		renderedHex.reserve(sizeof(canonicalBytes.digest) * 2);

		for (size_t index = 0; index < sizeof(canonicalBytes.digest); ++index)
		{
			unsigned char byteValue = canonicalBytes.digest[index];
			renderedHex.push_back(kHexDigits[(byteValue >> 4) & 0x0F]);
			renderedHex.push_back(kHexDigits[byteValue & 0x0F]);
		}

		return renderedHex;
	}
}

namespace HashRuntime
{
	void InitializeXXH3_64Hasher(XXH3_state_t *hasher)
	{
		if (hasher == NULL)
		{
			return;
		}

		(void)XXH3_64bits_reset(hasher);
	}

	void InitializeXXH3_128Hasher(XXH3_state_t *hasher)
	{
		if (hasher == NULL)
		{
			return;
		}

		(void)XXH3_128bits_reset(hasher);
	}

	void UpdateXXH3_64Hasher(XXH3_state_t& hasher, const unsigned char *data, size_t dataLen)
	{
		if (data == NULL || dataLen == 0)
		{
			return;
		}

		(void)XXH3_64bits_update(&hasher, data, dataLen);
	}

	void UpdateXXH3_128Hasher(XXH3_state_t& hasher, const unsigned char *data, size_t dataLen)
	{
		if (data == NULL || dataLen == 0)
		{
			return;
		}

		(void)XXH3_128bits_update(&hasher, data, dataLen);
	}

	sunjwbase::tstring FinalizeXXH3_64HasherHex(const XXH3_state_t& hasher)
	{
		XXH64_hash_t digestValue = XXH3_64bits_digest(&hasher);
		XXH64_canonical_t canonicalDigest = {};
		XXH64_canonicalFromHash(&canonicalDigest, digestValue);
		return sunjwbase::strtotstr(RenderUppercaseHexString(canonicalDigest));
	}

	sunjwbase::tstring FinalizeXXH3_128HasherHex(const XXH3_state_t& hasher)
	{
		XXH128_hash_t digestValue = XXH3_128bits_digest(&hasher);
		XXH128_canonical_t canonicalDigest = {};
		XXH128_canonicalFromHash(&canonicalDigest, digestValue);
		return sunjwbase::strtotstr(RenderUppercaseHexString(canonicalDigest));
	}
}
