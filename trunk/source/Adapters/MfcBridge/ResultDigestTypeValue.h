#ifndef _MFC_RESULT_DIGEST_TYPE_VALUE_H_
#define _MFC_RESULT_DIGEST_TYPE_VALUE_H_

#include "Common/ResultDigestValueAccess.h"
#include "Adapters/MfcBridge/ResultDigestTypeState.h"

static inline const sunjwbase::tstring& GetResultDigest(const ResultData& result, ResultDigestType digestType)
{
	return GetStoredResultDigest(result, digestType);
}

static inline bool TryGetMutableResultDigest(ResultData& result, ResultDigestType digestType, sunjwbase::tstring **digestValue)
{
	return TryGetMutableStoredResultDigest(result, digestType, digestValue);
}

static inline bool HasResultDigest(const ResultData& result, ResultDigestType digestType)
{
	return !GetResultDigest(result, digestType).empty();
}

static inline void SetResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)
{
	SetStoredResultDigest(result, digestType, digestValue);
}

#endif
