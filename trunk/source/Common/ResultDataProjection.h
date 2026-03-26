#ifndef _RESULT_DATA_PROJECTION_H_
#define _RESULT_DATA_PROJECTION_H_

#include "Common/ResultDataSearch.h"

template<typename TResultStateNet>
static inline TResultStateNet ConvertResultStateToNet(ResultState resultState)
{
	switch (resultState)
	{
	case RESULT_PATH:
		return TResultStateNet::ResultPath;
	case RESULT_META:
		return TResultStateNet::ResultMeta;
	case RESULT_ALL:
		return TResultStateNet::ResultAll;
	case RESULT_ERROR:
		return TResultStateNet::ResultError;
	case RESULT_NONE:
	default:
		return TResultStateNet::ResultNone;
	}
}

template<typename TMd5Action, typename TSha1Action, typename TSha256Action, typename TSha512Action>
static inline void DispatchResultDigestValueByType(ResultDigestType digestType, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)
{
	switch (digestType)
	{
	case RESULT_DIGEST_MD5:
		onMd5();
		break;
	case RESULT_DIGEST_SHA1:
		onSha1();
		break;
	case RESULT_DIGEST_SHA256:
		onSha256();
		break;
	case RESULT_DIGEST_SHA512:
		onSha512();
		break;
	}
}

template<typename TResultDataNet, typename TResultString>
static inline TResultDataNet AssignResultDigestToNet(TResultDataNet resultDataNet, ResultDigestType digestType, TResultString digestValue)
{
	switch (digestType)
	{
	case RESULT_DIGEST_MD5:
		resultDataNet.MD5 = digestValue;
		break;
	case RESULT_DIGEST_SHA1:
		resultDataNet.SHA1 = digestValue;
		break;
	case RESULT_DIGEST_SHA256:
		resultDataNet.SHA256 = digestValue;
		break;
	case RESULT_DIGEST_SHA512:
		resultDataNet.SHA512 = digestValue;
		break;
	}
	return resultDataNet;
}

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
