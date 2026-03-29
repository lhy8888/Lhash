#ifndef _RESULT_DIGEST_RENDER_H_
#define _RESULT_DIGEST_RENDER_H_

#include "Common/ResultDigestValueAccess.h"

struct ResultDigestDisplayInfo
{
	sunjwbase::tstring label;
	sunjwbase::tstring value;
};

static inline sunjwbase::tstring FormatResultDigestForDisplay(const sunjwbase::tstring& digestValue, bool uppercase)
{
	if (uppercase)
	{
		return sunjwbase::strtotstr(sunjwbase::str_upper(sunjwbase::tstrtostr(digestValue)));
	}

	return sunjwbase::strtotstr(sunjwbase::str_lower(sunjwbase::tstrtostr(digestValue)));
}

static inline ResultDigestDisplayInfo GetResultDigestDisplayInfo(const ResultDigestMetadata& digestMetadata, const sunjwbase::tstring& digestValue, bool uppercase)
{
	ResultDigestDisplayInfo digestDisplayInfo;
	digestDisplayInfo.label = GetResultDigestLabel(digestMetadata);
	digestDisplayInfo.value = FormatResultDigestForDisplay(digestValue, uppercase);
	return digestDisplayInfo;
}

template<typename TResultDigestDisplayVisitor>
static inline bool VisitResultDigestDisplayValues(const ResultData& result, bool uppercase, TResultDigestDisplayVisitor visitor)
{
	return VisitResultDigestMetadataValues(result, [&](int index, const ResultDigestMetadata& digestMetadata, const sunjwbase::tstring& digestValueTstr)
	{
		if (!HasResultDigest(result, GetResultDigestMetadataType(digestMetadata)))
		{
			return true;
		}

		ResultDigestDisplayInfo digestDisplayInfo = GetResultDigestDisplayInfo(digestMetadata, digestValueTstr, uppercase);
		return visitor(index, digestMetadata, digestDisplayInfo);
	});
}

#endif
