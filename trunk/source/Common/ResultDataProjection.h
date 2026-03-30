#ifndef _RESULT_DATA_PROJECTION_H_
#define _RESULT_DATA_PROJECTION_H_
#include "Common/HashResultProjection.h"
#include "Common/ResultDataSearch.h"
#include "Common/ResultNetProjection.h"
template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter>
static inline TResultDataNet AssignResultCoreToNet(TResultDataNet resultDataNet, const ResultData& result, TStringConverter convertString)
{
	return AssignHashResultCoreToNet<TResultDataNet, TResultStateNet>(resultDataNet, ProjectHashResult(result), convertString);
}
template<typename TResultDataNet, typename TStringConverter>
static inline TResultDataNet AssignResultDigestsToNet(TResultDataNet resultDataNet, const ResultData& result, TStringConverter convertString)
{
	return AssignHashResultDigestsToNet(resultDataNet, ProjectHashResult(result), convertString);
}
template<typename TResultDataNet, typename TResultStateNet, typename TResultPredicate, typename TStringConverter, typename TResultVisitor>
static inline void VisitProjectedMatchingResults(const ResultList& resultList, TResultPredicate predicate, TStringConverter convertString, TResultVisitor visitor);
template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter>
static inline TResultDataNet ProjectResultDataToNet(const ResultData& result, TStringConverter convertString)
{
	TResultDataNet resultDataNet = AssignResultCoreToNet<TResultDataNet, TResultStateNet>(TResultDataNet(), result, convertString);
	return AssignResultDigestsToNet(resultDataNet, result, convertString);
}
template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter, typename TResultHandler>
static inline void ProjectAndDispatchResult(const ResultData& result, TStringConverter convertString, TResultHandler resultHandler)
{
	resultHandler(ProjectResultDataToNet<TResultDataNet, TResultStateNet>(result, convertString));
}
template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter, typename TResultVisitor>
static inline void VisitProjectedResults(const ResultList& resultList, TStringConverter convertString, TResultVisitor visitor)
{
	VisitProjectedHashResults<TResultDataNet, TResultStateNet>(resultList, convertString, visitor);
}
template<typename TResultDataNet, typename TResultStateNet, typename TResultPredicate, typename TStringConverter, typename TResultVisitor>
static inline void VisitProjectedMatchingResults(const ResultList& resultList, TResultPredicate predicate, TStringConverter convertString, TResultVisitor visitor)
{
	size_t matchIndex = 0;
	VisitMatchingResults(resultList, predicate, [&](const HashResult& result)
	{
		visitor(matchIndex, ProjectHashResultToNet<TResultDataNet, TResultStateNet>(result, convertString));
		++matchIndex;
	});
}
template<typename TResultDataNet, typename TResultStateNet, typename TResultArray, typename TResultPredicate, typename TResultArrayFactory, typename TStringConverter, typename TResultArraySetter>
static inline TResultArray CreateProjectedMatchingResults(const ResultList& resultList, TResultPredicate predicate, TResultArrayFactory createResultArray, TStringConverter convertString, TResultArraySetter setProjectedResult)
{
	TResultArray projectedResults = createResultArray(CountMatchingResults(resultList, predicate));
	size_t matchIndex = 0;
	for (ResultList::const_iterator itr = resultList.begin(); itr != resultList.end(); ++itr)
	{
		const HashResult& result = *itr;
		if (predicate(result))
		{
			setProjectedResult(projectedResults, matchIndex, ProjectHashResultToNet<TResultDataNet, TResultStateNet>(result, convertString));
			++matchIndex;
		}
	}
	return projectedResults;
}
template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter, typename TResultVisitor>
static inline void VisitProjectedDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TStringConverter convertString, TResultVisitor visitor)
{
	VisitProjectedDigestMatchingHashResults<TResultDataNet, TResultStateNet>(resultList, digestText, convertString, visitor);
}
template<typename TResultDataNet, typename TResultStateNet, typename TResultArray, typename TResultArrayFactory, typename TStringConverter, typename TResultArraySetter>
static inline TResultArray CreateProjectedDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TResultArrayFactory createResultArray, TStringConverter convertString, TResultArraySetter setProjectedResult)
{
	return CreateProjectedDigestMatchingHashResults<TResultDataNet, TResultStateNet, TResultArray>(resultList, digestText, createResultArray, convertString, setProjectedResult);
}
#endif