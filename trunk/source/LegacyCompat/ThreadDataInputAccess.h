#ifndef _LEGACY_THREAD_DATA_INPUT_ACCESS_H_
#define _LEGACY_THREAD_DATA_INPUT_ACCESS_H_

#include "LegacyCompat/LegacyThreadData.h"

static inline const ThreadDataInputState& GetThreadDataInputState(const ThreadData& threadData)
{
	return threadData.inputState;
}

static inline ThreadDataInputState& GetMutableThreadDataInputState(ThreadData& threadData)
{
	return threadData.inputState;
}

static inline const TStrVector& GetThreadDataInputFiles(const ThreadData& threadData)
{
	return GetThreadDataInputState(threadData).inputFiles;
}

static inline TStrVector& GetMutableThreadDataInputFiles(ThreadData& threadData)
{
	return GetMutableThreadDataInputState(threadData).inputFiles;
}

static inline void SetThreadDataFileCount(ThreadData& threadData, uint32_t fileCount)
{
	GetMutableThreadDataInputState(threadData).fileCount = fileCount;
}

static inline uint32_t GetThreadDataFileCount(const ThreadData& threadData)
{
	return GetThreadDataInputState(threadData).fileCount;
}

static inline void ResetThreadDataInputFiles(ThreadData& threadData)
{
	SetThreadDataFileCount(threadData, 0);
	GetMutableThreadDataInputFiles(threadData).clear();
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

static inline bool HasThreadDataInputFiles(const ThreadData& threadData)
{
	return (GetThreadDataFileCount(threadData) > 0);
}

static inline const sunjwbase::tstring& GetThreadDataFullPath(const ThreadData& threadData, uint32_t fileIndex)
{
	return GetThreadDataInputFiles(threadData)[fileIndex];
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

#endif
