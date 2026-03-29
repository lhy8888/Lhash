#ifndef _HASH_RESULT_SEARCH_H_
#define _HASH_RESULT_SEARCH_H_

#include "Common/HashResult.h"

static inline bool HashResultContainsDigest(const HashResult& result, const sunjwbase::tstring& digestText)
{
	for (size_t digestIndex = 0; digestIndex < result.digests.size(); ++digestIndex)
	{
		if (result.digests[digestIndex].value.find(digestText) != sunjwbase::tstring::npos)
		{
			return true;
		}
	}

	return false;
}

static inline bool HashResultMatchesDigestText(const HashResult& result, const sunjwbase::tstring& digestText)
{
	return digestText.size() > 0 &&
		HashResultContainsDigest(result, digestText);
}

#endif
