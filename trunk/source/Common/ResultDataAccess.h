#ifndef _RESULT_DATA_ACCESS_H_
#define _RESULT_DATA_ACCESS_H_

#include <stdio.h>

#include "Common/strhelper.h"
#include "Common/Utils.h"
#include "Common/ResultDigestAccess.h"

using sunjwbase::strtotstr;
using sunjwbase::tstring;

static inline const ResultCoreState& GetResultCoreState(const ResultData& result)
{
	return result.coreState;
}

static inline ResultCoreState& GetMutableResultCoreState(ResultData& result)
{
	return result.coreState;
}

static inline const sunjwbase::tstring& GetResultPath(const ResultData& result)
{
	return GetResultCoreState(result).path;
}

static inline uint64_t GetResultSize(const ResultData& result)
{
	return GetResultCoreState(result).size;
}

static inline const sunjwbase::tstring& GetResultModifiedDate(const ResultData& result)
{
	return GetResultCoreState(result).modifiedDate;
}

static inline const sunjwbase::tstring& GetResultVersion(const ResultData& result)
{
	return GetResultCoreState(result).version;
}

static inline bool HasResultVersion(const ResultData& result)
{
	return GetResultVersion(result) != _T("");
}

static inline const sunjwbase::tstring& GetResultError(const ResultData& result)
{
	return GetResultCoreState(result).error;
}

static inline ResultState GetResultState(const ResultData& result)
{
	return GetResultCoreState(result).state;
}

static inline bool IsResultStateNone(ResultState resultState)
{
	return resultState == RESULT_NONE;
}

struct ResultRenderPolicy
{
	bool renderFileName;
	bool renderMeta;
	bool renderHash;
	bool renderError;
	bool appendTrailingLineBreak;
};

template<typename TNoneAction, typename TPathAction, typename TMetaAction, typename TAllAction, typename TErrorAction>
static inline void DispatchResultStateByType(ResultState resultState, TNoneAction onNone, TPathAction onPath, TMetaAction onMeta, TAllAction onAll, TErrorAction onError)
{
	switch (resultState)
	{
	case RESULT_PATH:
		onPath();
		break;
	case RESULT_META:
		onMeta();
		break;
	case RESULT_ALL:
		onAll();
		break;
	case RESULT_ERROR:
		onError();
		break;
	case RESULT_NONE:
	default:
		onNone();
		break;
	}
}

static inline ResultRenderPolicy GetResultRenderPolicy(ResultState resultState)
{
	ResultRenderPolicy renderPolicy = { false, false, false, false, true };

	DispatchResultStateByType(resultState,
		[&]()
		{
			renderPolicy = { false, false, false, false, true };
		},
		[&]()
		{
			renderPolicy = { true, false, false, false, true };
		},
		[&]()
		{
			renderPolicy = { true, true, false, false, true };
		},
		[&]()
		{
			renderPolicy = { true, true, true, false, false };
		},
		[&]()
		{
			renderPolicy = { true, false, false, true, false };
		});

	return renderPolicy;
}

struct ResultSizeDisplayInfo
{
	sunjwbase::tstring sizeText;
	sunjwbase::tstring shortSizeText;
};

static inline ResultSizeDisplayInfo GetResultSizeDisplayInfo(const ResultData& result)
{
	char chSizeBuff[1024] = { 0 };
	ResultSizeDisplayInfo resultSizeDisplayInfo;

	sprintf_s(chSizeBuff, 1024, "%I64u", GetResultSize(result));
	resultSizeDisplayInfo.sizeText = strtotstr(std::string(chSizeBuff));
	resultSizeDisplayInfo.shortSizeText = strtotstr(Utils::ConvertSizeToShortSizeStr(GetResultSize(result)));

	return resultSizeDisplayInfo;
}

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

static inline bool ShouldRenderResultFileName(ResultState resultState)
{
	return GetResultRenderPolicy(resultState).renderFileName;
}

static inline bool ShouldRenderResultMeta(ResultState resultState)
{
	return GetResultRenderPolicy(resultState).renderMeta;
}

static inline bool ShouldRenderResultHash(ResultState resultState)
{
	return GetResultRenderPolicy(resultState).renderHash;
}

static inline bool ShouldRenderResultError(ResultState resultState)
{
	return GetResultRenderPolicy(resultState).renderError;
}

static inline bool ShouldAppendResultTrailingLineBreak(ResultState resultState)
{
	return GetResultRenderPolicy(resultState).appendTrailingLineBreak;
}

enum ResultRenderSectionType
{
	RESULT_RENDER_SECTION_FILE_NAME = 0,
	RESULT_RENDER_SECTION_META,
	RESULT_RENDER_SECTION_HASH,
	RESULT_RENDER_SECTION_ERROR
};

enum ResultMetaLineType
{
	RESULT_META_LINE_FILE_SIZE = 0,
	RESULT_META_LINE_MODIFIED_DATE,
	RESULT_META_LINE_VERSION
};

template<typename TFileSizeAction, typename TModifiedDateAction, typename TVersionAction>
static inline void DispatchResultMetaLineByType(ResultMetaLineType metaLine, TFileSizeAction onFileSize, TModifiedDateAction onModifiedDate, TVersionAction onVersion)
{
	switch (metaLine)
	{
	case RESULT_META_LINE_FILE_SIZE:
		onFileSize();
		break;
	case RESULT_META_LINE_MODIFIED_DATE:
		onModifiedDate();
		break;
	case RESULT_META_LINE_VERSION:
		onVersion();
		break;
	}
}

template<typename TResultRenderSectionVisitor>
static inline bool VisitRenderableResultSections(ResultState resultState, TResultRenderSectionVisitor visitor)
{
	const ResultRenderPolicy renderPolicy = GetResultRenderPolicy(resultState);

	if (renderPolicy.renderFileName &&
		!visitor(RESULT_RENDER_SECTION_FILE_NAME))
	{
		return false;
	}

	if (renderPolicy.renderMeta &&
		!visitor(RESULT_RENDER_SECTION_META))
	{
		return false;
	}

	if (renderPolicy.renderHash &&
		!visitor(RESULT_RENDER_SECTION_HASH))
	{
		return false;
	}

	if (renderPolicy.renderError &&
		!visitor(RESULT_RENDER_SECTION_ERROR))
	{
		return false;
	}

	return true;
}

template<typename TFileNameAction, typename TMetaAction, typename THashAction, typename TErrorAction>
static inline void DispatchResultRenderSectionByType(ResultRenderSectionType renderSection, TFileNameAction onFileName, TMetaAction onMeta, THashAction onHash, TErrorAction onError)
{
	switch (renderSection)
	{
	case RESULT_RENDER_SECTION_FILE_NAME:
		onFileName();
		break;
	case RESULT_RENDER_SECTION_META:
		onMeta();
		break;
	case RESULT_RENDER_SECTION_HASH:
		onHash();
		break;
	case RESULT_RENDER_SECTION_ERROR:
		onError();
		break;
	}
}

template<typename TResultMetaLineVisitor>
static inline bool VisitRenderableResultMetaLines(const ResultData& result, TResultMetaLineVisitor visitor)
{
	if (!visitor(RESULT_META_LINE_FILE_SIZE))
	{
		return false;
	}

	if (!visitor(RESULT_META_LINE_MODIFIED_DATE))
	{
		return false;
	}

	if (HasResultVersion(result) &&
		!visitor(RESULT_META_LINE_VERSION))
	{
		return false;
	}

	return true;
}

template<typename TResultStateNet>
static inline TResultStateNet ConvertResultStateToNet(ResultState resultState)
{
	TResultStateNet resultStateNet = TResultStateNet::ResultNone;

	DispatchResultStateByType(resultState,
		[&]()
		{
			resultStateNet = TResultStateNet::ResultNone;
		},
		[&]()
		{
			resultStateNet = TResultStateNet::ResultPath;
		},
		[&]()
		{
			resultStateNet = TResultStateNet::ResultMeta;
		},
		[&]()
		{
			resultStateNet = TResultStateNet::ResultAll;
		},
		[&]()
		{
			resultStateNet = TResultStateNet::ResultError;
		});

	return resultStateNet;
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
	DispatchResultDigestValueByType(digestType,
		[&]()
		{
			resultDataNet.MD5 = digestValue;
		},
		[&]()
		{
			resultDataNet.SHA1 = digestValue;
		},
		[&]()
		{
			resultDataNet.SHA256 = digestValue;
		},
		[&]()
		{
			resultDataNet.SHA512 = digestValue;
		});

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

	VisitProjectedMatchingResults<TResultDataNet, TResultStateNet>(resultList, predicate, convertString, [&](size_t index, TResultDataNet resultDataNet)
	{
		setProjectedResult(projectedResults, index, resultDataNet);
	});

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

static inline void SetResultPath(ResultData& result, const sunjwbase::tstring& path)
{
	GetMutableResultCoreState(result).path = path;
}

static inline void SetResultSize(ResultData& result, uint64_t size)
{
	GetMutableResultCoreState(result).size = size;
}

static inline void SetResultModifiedDate(ResultData& result, const sunjwbase::tstring& modifiedDate)
{
	GetMutableResultCoreState(result).modifiedDate = modifiedDate;
}

static inline void SetResultVersion(ResultData& result, const sunjwbase::tstring& version)
{
	GetMutableResultCoreState(result).version = version;
}

static inline void SetResultError(ResultData& result, const sunjwbase::tstring& errorText)
{
	GetMutableResultCoreState(result).error = errorText;
}

static inline void SetResultState(ResultData& result, ResultState resultState)
{
	GetMutableResultCoreState(result).state = resultState;
}

static inline void ResetResultCoreState(ResultData& result)
{
	GetMutableResultCoreState(result).state = RESULT_NONE;
	GetMutableResultCoreState(result).path.clear();
	GetMutableResultCoreState(result).size = 0;
	GetMutableResultCoreState(result).modifiedDate.clear();
	GetMutableResultCoreState(result).version.clear();
	GetMutableResultCoreState(result).error.clear();
}

static inline void ResetResultData(ResultData& result)
{
	ResetResultCoreState(result);
	ResetResultDigests(result);
}

#endif
