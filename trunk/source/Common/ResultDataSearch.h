#ifndef _RESULT_DATA_SEARCH_H_
#define _RESULT_DATA_SEARCH_H_

#include "Common/ResultDataAccess.h"

static inline bool ResultMatchesDigestText(const ResultData& result, const sunjwbase::tstring& digestText)
{
	return digestText.size() > 0 &&
		ResultContainsDigest(result, digestText);
}

static inline sunjwbase::tstring NormalizeResultPathSearchText(const sunjwbase::tstring& pathText)
{
	return sunjwbase::strtotstr(sunjwbase::str_lower(sunjwbase::tstrtostr(pathText)));
}

static inline bool ResultMatchesPathText(const ResultData& result, const sunjwbase::tstring& pathText)
{
	return NormalizeResultPathSearchText(GetResultPath(result)).find(pathText) != sunjwbase::tstring::npos;
}

static inline bool ResultMatchesPathAndDigestText(const ResultData& result, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText)
{
	return ResultMatchesPathText(result, pathText) &&
		ResultMatchesDigestText(result, digestText);
}

static inline sunjwbase::tstring NormalizeDigestSearchText(const sunjwbase::tstring& digestText)
{
	sunjwbase::tstring normalizedDigestText = digestText;
	normalizedDigestText = sunjwbase::strtotstr(sunjwbase::str_upper(sunjwbase::tstrtostr(normalizedDigestText)));
	normalizedDigestText = sunjwbase::strtrim(normalizedDigestText);
	return normalizedDigestText;
}

template<typename TResultPredicate, typename TResultVisitor>
static inline size_t VisitMatchingResults(const ResultList& resultList, TResultPredicate predicate, TResultVisitor visitor)
{
	size_t matchCount = 0;
	ResultList::const_iterator itr = resultList.begin();
	for (; itr != resultList.end(); ++itr)
	{
		if (predicate(*itr))
		{
			visitor(*itr);
			++matchCount;
		}
	}

	return matchCount;
}

template<typename TResultPredicate>
static inline size_t CountMatchingResults(const ResultList& resultList, TResultPredicate predicate)
{
	return VisitMatchingResults(resultList, predicate, [&](const ResultData& result)
	{
		(void)result;
	});
}

template<typename TResultVisitor>
static inline size_t VisitDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TResultVisitor visitor)
{
	return VisitMatchingResults(resultList, [&](const ResultData& result)
	{
		return ResultMatchesDigestText(result, digestText);
	}, visitor);
}

template<typename TResultVisitor>
static inline size_t VisitPathAndDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, TResultVisitor visitor)
{
	return VisitMatchingResults(resultList, [&](const ResultData& result)
	{
		return ResultMatchesPathAndDigestText(result, pathText, digestText);
	}, visitor);
}

static inline size_t CountDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText)
{
	return VisitDigestMatchingResults(resultList, digestText, [&](const ResultData& result)
	{
		(void)result;
	});
}

#endif
