#ifndef _RESULT_DATA_SEARCH_H_
#define _RESULT_DATA_SEARCH_H_
#include "Common/HashResultSearch.h"
static inline bool ResultMatchesDigestText(const ResultData& result, const sunjwbase::tstring& digestText)
{
	return HashResultMatchesDigestText(ProjectHashResult(result), digestText);
}
static inline bool ResultMatchesPathText(const ResultData& result, const sunjwbase::tstring& pathText)
{
	return HashResultMatchesPathText(ProjectHashResult(result), pathText);
}
static inline bool ResultMatchesPathAndDigestText(const ResultData& result, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText)
{
	return HashResultMatchesPathAndDigestText(ProjectHashResult(result), pathText, digestText);
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
	return VisitMatchingResults(resultList, predicate, [&](const HashResult& result)
	{
		(void)result;
	});
}
template<typename TResultVisitor>
static inline size_t VisitDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TResultVisitor visitor)
{
	return VisitMatchingResults(resultList, [&](const HashResult& result)
	{
		return HashResultMatchesDigestText(result, digestText);
	}, visitor);
}
template<typename TResultVisitor>
static inline size_t VisitPathAndDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, TResultVisitor visitor)
{
	return VisitMatchingResults(resultList, [&](const HashResult& result)
	{
		return HashResultMatchesPathAndDigestText(result, pathText, digestText);
	}, visitor);
}
static inline size_t CountDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText)
{
	return CountDigestMatchingHashResults(resultList, digestText);
}
#endif