#ifndef _HASH_RESULT_SEARCH_H_
#define _HASH_RESULT_SEARCH_H_

#include "Common/HashResult.h"

static inline sunjwbase::tstring NormalizeHashResultPathSearchText(const sunjwbase::tstring& pathText)
{
	return sunjwbase::strtotstr(sunjwbase::str_lower(sunjwbase::tstrtostr(pathText)));
}

static inline sunjwbase::tstring NormalizeHashResultDigestSearchText(const sunjwbase::tstring& digestText)
{
	sunjwbase::tstring normalizedDigestText = digestText;
	normalizedDigestText = sunjwbase::strtotstr(sunjwbase::str_upper(sunjwbase::tstrtostr(normalizedDigestText)));
	normalizedDigestText = sunjwbase::strtrim(normalizedDigestText);
	return normalizedDigestText;
}

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

static inline bool HashResultMatchesPathText(const HashResult& result, const sunjwbase::tstring& pathText)
{
	return NormalizeHashResultPathSearchText(result.path).find(pathText) != sunjwbase::tstring::npos;
}

static inline bool HashResultMatchesPathAndDigestText(const HashResult& result, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText)
{
	return HashResultMatchesPathText(result, pathText) &&
		HashResultMatchesDigestText(result, digestText);
}

template<typename THashResultVisitor>
static inline size_t VisitHashResults(const ResultList& resultList, THashResultVisitor visitor)
{
	size_t visitCount = 0;
	ResultList::const_iterator itr = resultList.begin();
	for (; itr != resultList.end(); ++itr)
	{
		HashResult hashResult = ProjectHashResult(*itr);
		visitor(hashResult);
		++visitCount;
	}

	return visitCount;
}

template<typename THashResultPredicate, typename THashResultVisitor>
static inline size_t VisitMatchingHashResults(const ResultList& resultList, THashResultPredicate predicate, THashResultVisitor visitor)
{
	size_t matchCount = 0;
	ResultList::const_iterator itr = resultList.begin();
	for (; itr != resultList.end(); ++itr)
	{
		HashResult hashResult = ProjectHashResult(*itr);
		if (predicate(hashResult))
		{
			visitor(hashResult);
			++matchCount;
		}
	}

	return matchCount;
}

template<typename THashResultPredicate>
static inline size_t CountMatchingHashResults(const ResultList& resultList, THashResultPredicate predicate)
{
	return VisitMatchingHashResults(resultList, predicate, [&](const HashResult& result)
	{
		(void)result;
	});
}

template<typename THashResultVisitor>
static inline size_t VisitDigestMatchingHashResults(const ResultList& resultList, const sunjwbase::tstring& digestText, THashResultVisitor visitor)
{
	return VisitMatchingHashResults(resultList, [&](const HashResult& result)
	{
		return HashResultMatchesDigestText(result, digestText);
	}, visitor);
}

template<typename THashResultVisitor>
static inline size_t VisitPathAndDigestMatchingHashResults(const ResultList& resultList, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, THashResultVisitor visitor)
{
	return VisitMatchingHashResults(resultList, [&](const HashResult& result)
	{
		return HashResultMatchesPathAndDigestText(result, pathText, digestText);
	}, visitor);
}

static inline size_t CountDigestMatchingHashResults(const ResultList& resultList, const sunjwbase::tstring& digestText)
{
	return VisitDigestMatchingHashResults(resultList, digestText, [&](const HashResult& result)
	{
		(void)result;
	});
}

#endif
