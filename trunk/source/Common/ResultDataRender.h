#ifndef _RESULT_DATA_RENDER_H_
#define _RESULT_DATA_RENDER_H_

#include <string>

#include "Common/ResultDataAccess.h"
#include "Common/Utils.h"

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

static inline bool IsResultStateNone(ResultState resultState)
{
	return resultState == RESULT_NONE;
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
	ResultSizeDisplayInfo resultSizeDisplayInfo;

	resultSizeDisplayInfo.sizeText = sunjwbase::strtotstr(std::to_string(GetResultSize(result)));
	resultSizeDisplayInfo.shortSizeText = sunjwbase::strtotstr(Utils::ConvertSizeToShortSizeStr(GetResultSize(result)));

	return resultSizeDisplayInfo;
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

#endif
