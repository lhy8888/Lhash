#ifndef _THREAD_DATA_RESULT_ACCESS_H_
#define _THREAD_DATA_RESULT_ACCESS_H_

#include "Common/Global.h"
#include "Common/HashResultSearch.h"
#include "Common/ThreadDataExecutionAccess.h"

static inline HashResultList& GetMutableThreadDataResults(ThreadData& threadData)
{
	return GetMutableThreadDataExecutionState(threadData).results;
}

static inline const HashResultList& GetThreadDataResults(const ThreadData& threadData)
{
	return GetThreadDataExecutionState(threadData).results;
}

static inline HashResult& AppendThreadDataResult(ThreadData& threadData)
{
	HashResult resultNew;
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
	HashResultList::const_iterator itr = GetThreadDataResults(threadData).begin();
	for (; itr != GetThreadDataResults(threadData).end(); ++itr)
	{
		visitor(*itr);
		visitCount++;
	}
	return visitCount;
}

template<typename THashResultVisitor>
static inline size_t VisitThreadDataHashResults(const ThreadData& threadData, THashResultVisitor visitor)
{
	return VisitHashResults(GetThreadDataResults(threadData), visitor);
}

template<typename THashResultVisitor>
static inline size_t VisitThreadDataDigestMatchingHashResults(const ThreadData& threadData, const sunjwbase::tstring& digestText, THashResultVisitor visitor)
{
	return VisitDigestMatchingHashResults(GetThreadDataResults(threadData), digestText, visitor);
}

template<typename THashResultVisitor>
static inline size_t VisitThreadDataPathAndDigestMatchingHashResults(const ThreadData& threadData, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, THashResultVisitor visitor)
{
	return VisitPathAndDigestMatchingHashResults(GetThreadDataResults(threadData), pathText, digestText, visitor);
}

#endif
