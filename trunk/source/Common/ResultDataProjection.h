#ifndef _RESULT_DATA_PROJECTION_H_
#define _RESULT_DATA_PROJECTION_H_

#include "Common/ResultDataSearch.h"
#include "Common/ResultNetProjection.h"

template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter>
static inline TResultDataNet AssignResultCoreToNet(TResultDataNet resultDataNet, const ResultData& result, TStringConverter convertString)
{
	resultDataNet.EnumState = ConvertResultStateToNet<TResultStateNet>(GetResultState(result));
	resultDataNet.Path = convertString(GetResultPath(result).c_str());
	resultDataNet.Size = GetResultSize(result);
	resultDataNet.ModifiedDate = convertString(GetResultModifiedDate(result).c_str());
	resultDataNet.Version = convertString(GetResultVersion(result).c_str());
	resultDataNet.Error = convertString(GetResultError(result).c_str());
	return resultDataNet;
}

template<typename TResultDataNet, typename TStringConverter>
static inline TResultDataNet AssignResultDigestsToNet(TResultDataNet resultDataNet, const ResultData& result, TStringConverter convertString)
{
	for (int digestIndex = 0; digestIndex < GetResultDigestCount(); digestIndex++)
	{
		ResultDigestType digestType = GetResultDigestTypeAt(digestIndex);
		const tstring& digestValueTstr = GetResultDigest(result, digestType);
		resultDataNet = AssignResultDigestToNet(resultDataNet, digestType, convertString(digestValueTstr.c_str()));
	}
	return resultDataNet;
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
	VisitProjectedMatchingResults<TResultDataNet, TResultStateNet>(resultList, [&](const ResultData& result)
	{
		(void)result;
		return true;
	}, convertString, visitor);
}

template<typename TResultDataNet, typename TResultStateNet, typename TResultPredicate, typename TStringConverter, typename TResultVisitor>
static inline void VisitProjectedMatchingResults(const ResultList& resultList, TResultPredicate predicate, TStringConverter convertString, TResultVisitor visitor)
{
	size_t matchIndex = 0;
	VisitMatchingResults(resultList, predicate, [&](const ResultData& result)
	{
		visitor(matchIndex, ProjectResultDataToNet<TResultDataNet, TResultStateNet>(result, convertString));
		++matchIndex;
	});
}

template<typename TResultDataNet, typename TResultStateNet, typename TResultArray, typename TResultPredicate, typename TResultArrayFactory, typename TStringConverter, typename TResultArraySetter>
static inline TResultArray CreateProjectedMatchingResults(const ResultList& resultList, TResultPredicate predicate, TResultArrayFactory createResultArray, TStringConverter convertString, TResultArraySetter setProjectedResult)
{
	TResultArray projectedResults = createResultArray(CountMatchingResults(resultList, predicate));
	size_t matchIndex = 0;
	ResultList::const_iterator itr = resultList.begin();
	for (; itr != resultList.end(); ++itr)
	{
		if (predicate(*itr))
		{
			setProjectedResult(projectedResults, matchIndex, ProjectResultDataToNet<TResultDataNet, TResultStateNet>(*itr, convertString));
			++matchIndex;
		}
	}

	return projectedResults;
}

template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter, typename TResultVisitor>
static inline void VisitProjectedDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TStringConverter convertString, TResultVisitor visitor)
{
	size_t matchIndex = 0;
	VisitDigestMatchingResults(resultList, digestText, [&](const ResultData& result)
	{
		visitor(matchIndex, ProjectResultDataToNet<TResultDataNet, TResultStateNet>(result, convertString));
		++matchIndex;
	});
}

template<typename TResultDataNet, typename TResultStateNet, typename TResultArray, typename TResultArrayFactory, typename TStringConverter, typename TResultArraySetter>
static inline TResultArray CreateProjectedDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TResultArrayFactory createResultArray, TStringConverter convertString, TResultArraySetter setProjectedResult)
{
	return CreateProjectedMatchingResults<TResultDataNet, TResultStateNet, TResultArray>(resultList, [&](const ResultData& result)
	{
		return ResultMatchesDigestText(result, digestText);
	}, createResultArray, convertString, setProjectedResult);
}

#endif
