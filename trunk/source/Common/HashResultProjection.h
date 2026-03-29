#ifndef _HASH_RESULT_PROJECTION_H_
#define _HASH_RESULT_PROJECTION_H_

#include "Common/HashResult.h"
#include "Common/HashResultSearch.h"
#include "Common/ResultNetProjection.h"

template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter>
static inline TResultDataNet AssignHashResultCoreToNet(TResultDataNet resultDataNet, const HashResult& result, TStringConverter convertString)
{
	resultDataNet.EnumState = ConvertResultStateToNet<TResultStateNet>(result.state);
	resultDataNet.Path = convertString(result.path.c_str());
	resultDataNet.Size = result.meta.size;
	resultDataNet.ModifiedDate = convertString(result.meta.modifiedDate.c_str());
	resultDataNet.Version = convertString(result.meta.version.c_str());
	resultDataNet.Error = convertString(result.error.c_str());
	return resultDataNet;
}

template<typename TResultDataNet, typename TStringConverter>
static inline TResultDataNet AssignHashResultDigestsToNet(TResultDataNet resultDataNet, const HashResult& result, TStringConverter convertString)
{
	for (size_t digestIndex = 0; digestIndex < result.digests.size(); ++digestIndex)
	{
		const HashDigestResult& digestResult = result.digests[digestIndex];
		resultDataNet = AssignResultDigestToNet(resultDataNet, digestResult.type, convertString(digestResult.value.c_str()));
	}

	return resultDataNet;
}

template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter>
static inline TResultDataNet ProjectHashResultToNet(const HashResult& result, TStringConverter convertString)
{
	TResultDataNet resultDataNet = AssignHashResultCoreToNet<TResultDataNet, TResultStateNet>(TResultDataNet(), result, convertString);
	return AssignHashResultDigestsToNet(resultDataNet, result, convertString);
}

template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter, typename TResultVisitor>
static inline void VisitProjectedHashResults(const ResultList& resultList, TStringConverter convertString, TResultVisitor visitor)
{
	size_t matchIndex = 0;
	ResultList::const_iterator itr = resultList.begin();
	for (; itr != resultList.end(); ++itr)
	{
		HashResult hashResult = ProjectHashResult(*itr);
		visitor(matchIndex, ProjectHashResultToNet<TResultDataNet, TResultStateNet>(hashResult, convertString));
		++matchIndex;
	}
}

template<typename THashResultPredicate>
static inline size_t CountMatchingHashResults(const ResultList& resultList, THashResultPredicate predicate)
{
	size_t matchCount = 0;
	ResultList::const_iterator itr = resultList.begin();
	for (; itr != resultList.end(); ++itr)
	{
		HashResult hashResult = ProjectHashResult(*itr);
		if (predicate(hashResult))
		{
			++matchCount;
		}
	}

	return matchCount;
}

template<typename TResultDataNet, typename TResultStateNet, typename THashResultPredicate, typename TStringConverter, typename TResultVisitor>
static inline void VisitProjectedMatchingHashResults(const ResultList& resultList, THashResultPredicate predicate, TStringConverter convertString, TResultVisitor visitor)
{
	size_t matchIndex = 0;
	ResultList::const_iterator itr = resultList.begin();
	for (; itr != resultList.end(); ++itr)
	{
		HashResult hashResult = ProjectHashResult(*itr);
		if (predicate(hashResult))
		{
			visitor(matchIndex, ProjectHashResultToNet<TResultDataNet, TResultStateNet>(hashResult, convertString));
			++matchIndex;
		}
	}
}

template<typename TResultDataNet, typename TResultStateNet, typename TResultArray, typename THashResultPredicate, typename TResultArrayFactory, typename TStringConverter, typename TResultArraySetter>
static inline TResultArray CreateProjectedMatchingHashResults(const ResultList& resultList, THashResultPredicate predicate, TResultArrayFactory createResultArray, TStringConverter convertString, TResultArraySetter setProjectedResult)
{
	TResultArray projectedResults = createResultArray(CountMatchingHashResults(resultList, predicate));
	size_t matchIndex = 0;
	ResultList::const_iterator itr = resultList.begin();
	for (; itr != resultList.end(); ++itr)
	{
		HashResult hashResult = ProjectHashResult(*itr);
		if (predicate(hashResult))
		{
			setProjectedResult(projectedResults, matchIndex, ProjectHashResultToNet<TResultDataNet, TResultStateNet>(hashResult, convertString));
			++matchIndex;
		}
	}

	return projectedResults;
}

template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter, typename TResultVisitor>
static inline void VisitProjectedDigestMatchingHashResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TStringConverter convertString, TResultVisitor visitor)
{
	VisitProjectedMatchingHashResults<TResultDataNet, TResultStateNet>(resultList, [&](const HashResult& result)
	{
		return HashResultMatchesDigestText(result, digestText);
	}, convertString, visitor);
}

template<typename TResultDataNet, typename TResultStateNet, typename TResultArray, typename TResultArrayFactory, typename TStringConverter, typename TResultArraySetter>
static inline TResultArray CreateProjectedDigestMatchingHashResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TResultArrayFactory createResultArray, TStringConverter convertString, TResultArraySetter setProjectedResult)
{
	return CreateProjectedMatchingHashResults<TResultDataNet, TResultStateNet, TResultArray>(resultList, [&](const HashResult& result)
	{
		return HashResultMatchesDigestText(result, digestText);
	}, createResultArray, convertString, setProjectedResult);
}

#endif
