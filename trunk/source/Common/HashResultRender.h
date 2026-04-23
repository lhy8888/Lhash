#ifndef _HASH_RESULT_RENDER_H_
#define _HASH_RESULT_RENDER_H_

#include <string>

#include "Common/HashResult.h"
#include "Common/ResultDataRender.h"
#include "Common/ResultDigestRender.h"
#include "Common/Utils.h"

static inline bool HasHashResultVersion(const HashResult& result)
{
	return !result.meta.version.empty();
}

static inline ResultSizeDisplayInfo GetHashResultSizeDisplayInfo(const HashResult& result)
{
	ResultSizeDisplayInfo resultSizeDisplayInfo;

	resultSizeDisplayInfo.sizeText = sunjwbase::strtotstr(std::to_string(result.meta.size));
	resultSizeDisplayInfo.shortSizeText = sunjwbase::strtotstr(Utils::ConvertSizeToShortSizeStr(result.meta.size));

	return resultSizeDisplayInfo;
}

template<typename TResultMetaLineVisitor>
static inline bool VisitRenderableHashResultMetaLines(const HashResult& result, TResultMetaLineVisitor visitor)
{
	if (!visitor(RESULT_META_LINE_FILE_SIZE))
	{
		return false;
	}

	if (!visitor(RESULT_META_LINE_MODIFIED_DATE))
	{
		return false;
	}

	if (HasHashResultVersion(result) &&
		!visitor(RESULT_META_LINE_VERSION))
	{
		return false;
	}

	return true;
}

template<typename TResultDigestDisplayVisitor>
static inline bool VisitHashResultDigestDisplayValues(const HashResult& result, bool uppercase, TResultDigestDisplayVisitor visitor)
{
	for (size_t digestIndex = 0; digestIndex < result.digests.size(); ++digestIndex)
	{
		const HashDigestResult& digestResult = result.digests[digestIndex];
		ResultDigestDisplayInfo digestDisplayInfo;
		digestDisplayInfo.label = digestResult.displayLabel;
		digestDisplayInfo.value = FormatResultDigestForDisplay(digestResult.value, uppercase);
		if (!visitor(static_cast<int>(digestIndex), digestResult, digestDisplayInfo))
		{
			return false;
		}
	}

	return true;
}

#endif
