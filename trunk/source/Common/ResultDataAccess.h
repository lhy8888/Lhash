#ifndef _RESULT_DATA_ACCESS_H_
#define _RESULT_DATA_ACCESS_H_

#include "Common/strhelper.h"
#include "Common/ResultDigestValueAccess.h"

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
