#ifndef _THREAD_DATA_RESULT_ACCESS_H_
#define _THREAD_DATA_RESULT_ACCESS_H_

#include "Common/Global.h"
#include "Common/ThreadDataExecutionAccess.h"

static inline ResultList& GetMutableThreadDataResults(ThreadData& threadData)
{
	return GetMutableThreadDataExecutionState(threadData).results;
}

static inline const ResultList& GetThreadDataResults(const ThreadData& threadData)
{
	return GetThreadDataExecutionState(threadData).results;
}

static inline ResultData& AppendThreadDataResult(ThreadData& threadData)
{
	ResultData resultNew;
	GetMutableThreadDataResults(threadData).push_back(resultNew);
	return GetMutableThreadDataResults(threadData).back();
}

static inline void ClearThreadDataResults(ThreadData& threadData)
{
	GetMutableThreadDataResults(threadData).clear();
}

static inline uint64_t GetThreadDataResultCount(const ThreadData& threadData)
{
	return GetThreadDataResults(threadData).size();
}

template<typename TResultVisitor>
static inline size_t VisitThreadDataResults(const ThreadData& threadData, TResultVisitor visitor)
{
	size_t visitCount = 0;
	ResultList::const_iterator itr = GetThreadDataResults(threadData).begin();
	for (; itr != GetThreadDataResults(threadData).end(); ++itr)
	{
		visitor(*itr);
		visitCount++;
	}
	return visitCount;
}

#endif
