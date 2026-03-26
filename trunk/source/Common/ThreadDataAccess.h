#ifndef _THREAD_DATA_ACCESS_H_
#define _THREAD_DATA_ACCESS_H_

#include "Common/Global.h"

static inline void SetThreadDataObserver(ThreadData& threadData, HashEngineObserver *observer)
{
	threadData.observer = observer;
}

static inline HashEngineObserver *GetThreadDataObserver(const ThreadData& threadData)
{
	return threadData.observer;
}

static inline const ThreadDataInputState& GetThreadDataInputState(const ThreadData& threadData)
{
	return threadData.inputState;
}

static inline ThreadDataInputState& GetMutableThreadDataInputState(ThreadData& threadData)
{
	return threadData.inputState;
}

static inline const ThreadDataExecutionState& GetThreadDataExecutionState(const ThreadData& threadData)
{
	return threadData.executionState;
}

static inline ThreadDataExecutionState& GetMutableThreadDataExecutionState(ThreadData& threadData)
{
	return threadData.executionState;
}

static inline const TStrVector& GetThreadDataInputFiles(const ThreadData& threadData)
{
	return GetThreadDataInputState(threadData).inputFiles;
}

static inline TStrVector& GetMutableThreadDataInputFiles(ThreadData& threadData)
{
	return GetMutableThreadDataInputState(threadData).inputFiles;
}

static inline ResultList& GetMutableThreadDataResults(ThreadData& threadData)
{
	return GetMutableThreadDataExecutionState(threadData).results;
}

static inline void SetThreadDataFileCount(ThreadData& threadData, uint32_t fileCount);
static inline uint32_t GetThreadDataFileCount(const ThreadData& threadData);
static inline void SetThreadDataWorking(ThreadData& threadData, bool working);
static inline void SetThreadDataStop(ThreadData& threadData, bool stopValue);
static inline void SetThreadDataUppercase(ThreadData& threadData, bool uppercase);
static inline void ResetThreadDataTotalSize(ThreadData& threadData);
static inline const ResultList& GetThreadDataResults(const ThreadData& threadData);

static inline void ResetThreadDataInputFiles(ThreadData& threadData)
{
	SetThreadDataFileCount(threadData, 0);
	GetMutableThreadDataInputFiles(threadData).clear();
}

static inline void SetThreadDataFileCount(ThreadData& threadData, uint32_t fileCount)
{
	GetMutableThreadDataInputState(threadData).fileCount = fileCount;
}

static inline void AddThreadDataFullPath(ThreadData& threadData, const sunjwbase::tstring& fullPath)
{
	GetMutableThreadDataInputFiles(threadData).push_back(fullPath);
}

static inline void AppendThreadDataInputFile(ThreadData& threadData, const sunjwbase::tstring& fullPath)
{
	AddThreadDataFullPath(threadData, fullPath);
	SetThreadDataFileCount(threadData, GetThreadDataFileCount(threadData) + 1);
}

template<typename TInputFileFactory>
static inline void ResetThreadDataInputFilesAndAppend(ThreadData& threadData, uint32_t fileCount, TInputFileFactory inputFileFactory)
{
	ResetThreadDataInputFiles(threadData);
	SetThreadDataFileCount(threadData, fileCount);
	for (uint32_t fileIndex = 0; fileIndex < fileCount; ++fileIndex)
	{
		AddThreadDataFullPath(threadData, inputFileFactory(fileIndex));
	}
}

static inline bool AppendTrimmedThreadDataInputFile(ThreadData& threadData, const sunjwbase::tstring& fullPath)
{
	sunjwbase::tstring trimmedPath = sunjwbase::strtrim(fullPath);
	if (trimmedPath.length() == 0)
	{
		return false;
	}

	AppendThreadDataInputFile(threadData, trimmedPath);
	return true;
}

static inline void AppendThreadDataInputFiles(ThreadData& threadData, const TStrVector& fullPaths)
{
	for (TStrVector::const_iterator itr = fullPaths.begin(); itr != fullPaths.end(); ++itr)
	{
		AppendThreadDataInputFile(threadData, *itr);
	}
}

static inline void ReplaceThreadDataInputFiles(ThreadData& threadData, const TStrVector& fullPaths)
{
	ResetThreadDataInputFiles(threadData);
	AppendThreadDataInputFiles(threadData, fullPaths);
}

static inline void ReplaceTrimmedThreadDataInputFiles(ThreadData& threadData, const TStrVector& fullPaths)
{
	ResetThreadDataInputFiles(threadData);
	for (TStrVector::const_iterator itr = fullPaths.begin(); itr != fullPaths.end(); ++itr)
	{
		AppendTrimmedThreadDataInputFile(threadData, *itr);
	}
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

static inline void ResetThreadDataForNewSession(ThreadData& threadData)
{
	SetThreadDataWorking(threadData, false);
	SetThreadDataStop(threadData, false);
	SetThreadDataUppercase(threadData, false);
	ResetThreadDataTotalSize(threadData);

	ResetThreadDataInputFiles(threadData);
	ClearThreadDataResults(threadData);
}

static inline void SetThreadDataWorking(ThreadData& threadData, bool working)
{
	GetMutableThreadDataExecutionState(threadData).working = working;
}

static inline bool IsThreadDataWorking(const ThreadData& threadData)
{
	return GetThreadDataExecutionState(threadData).working;
}

static inline void SetThreadDataStop(ThreadData& threadData, bool stopValue)
{
	GetMutableThreadDataExecutionState(threadData).stopRequested = stopValue;
}

static inline bool ShouldStopThreadData(const ThreadData& threadData)
{
	return GetThreadDataExecutionState(threadData).stopRequested;
}

static inline void SetThreadDataUppercase(ThreadData& threadData, bool uppercase)
{
	GetMutableThreadDataExecutionState(threadData).uppercaseDigest = uppercase;
}

static inline bool GetThreadDataUppercase(const ThreadData& threadData)
{
	return GetThreadDataExecutionState(threadData).uppercaseDigest;
}

static inline uint64_t GetThreadDataTotalSize(const ThreadData& threadData)
{
	return GetThreadDataExecutionState(threadData).countedSize;
}

static inline void ResetThreadDataTotalSize(ThreadData& threadData)
{
	GetMutableThreadDataExecutionState(threadData).countedSize = 0;
}

static inline void AddThreadDataTotalSize(ThreadData& threadData, uint64_t sizeDelta)
{
	GetMutableThreadDataExecutionState(threadData).countedSize += sizeDelta;
}

static inline void ReplaceThreadDataCountedFileSize(ThreadData& threadData, uint64_t previousSize, uint64_t currentSize)
{
	GetMutableThreadDataExecutionState(threadData).countedSize = GetThreadDataTotalSize(threadData) + currentSize - previousSize;
}

static inline uint32_t GetThreadDataFileCount(const ThreadData& threadData)
{
	return GetThreadDataInputState(threadData).fileCount;
}

static inline uint64_t GetThreadDataResultCount(const ThreadData& threadData)
{
	return GetThreadDataResults(threadData).size();
}

static inline bool HasThreadDataInputFiles(const ThreadData& threadData)
{
	return (GetThreadDataFileCount(threadData) > 0);
}

static inline const sunjwbase::tstring& GetThreadDataFullPath(const ThreadData& threadData, uint32_t fileIndex)
{
	return GetThreadDataInputFiles(threadData)[fileIndex];
}

static inline const ResultList& GetThreadDataResults(const ThreadData& threadData)
{
	return GetThreadDataExecutionState(threadData).results;
}

template<typename TInputFileVisitor>
static inline bool VisitThreadDataInputFiles(const ThreadData& threadData, TInputFileVisitor visitor)
{
	for (uint32_t fileIndex = 0; fileIndex < GetThreadDataFileCount(threadData); ++fileIndex)
	{
		if (!visitor(fileIndex, GetThreadDataFullPath(threadData, fileIndex)))
		{
			return false;
		}
	}

	return true;
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
