#ifndef _THREAD_DATA_EXECUTION_ACCESS_H_
#define _THREAD_DATA_EXECUTION_ACCESS_H_

#include "Common/Global.h"
#include "Common/HashAlgorithmRegistry.h"

static inline void EnsureThreadDataHashAlgorithmSelectionStateSize(HashAlgorithmSelectionState& hashAlgorithmSelectionState)
{
	size_t registeredAlgorithmCount = static_cast<size_t>(GetRegisteredHashAlgorithmCount());
	if (hashAlgorithmSelectionState.enabled.empty())
	{
		hashAlgorithmSelectionState.enabled.assign(registeredAlgorithmCount, true);
		return;
	}
	if (hashAlgorithmSelectionState.enabled.size() < registeredAlgorithmCount)
	{
		hashAlgorithmSelectionState.enabled.resize(registeredAlgorithmCount, true);
	}
}

static inline void SetThreadDataObserver(ThreadData& threadData, HashProgressSink *observer)
{
	threadData.observer = observer;
}

static inline HashProgressSink *GetThreadDataObserver(const ThreadData& threadData)
{
	return threadData.observer;
}

static inline const ThreadDataExecutionState& GetThreadDataExecutionState(const ThreadData& threadData)
{
	return threadData.executionState;
}

static inline ThreadDataExecutionState& GetMutableThreadDataExecutionState(ThreadData& threadData)
{
	return threadData.executionState;
}

static inline const HashAlgorithmSelectionState& GetThreadDataHashAlgorithmSelectionState(const ThreadData& threadData)
{
	return GetThreadDataExecutionState(threadData).hashAlgorithms;
}

static inline HashAlgorithmSelectionState& GetMutableThreadDataHashAlgorithmSelectionState(ThreadData& threadData)
{
	HashAlgorithmSelectionState& hashAlgorithmSelectionState = GetMutableThreadDataExecutionState(threadData).hashAlgorithms;
	EnsureThreadDataHashAlgorithmSelectionStateSize(hashAlgorithmSelectionState);
	return hashAlgorithmSelectionState;
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

static inline void SetThreadDataHashAlgorithmEnabled(ThreadData& threadData, ResultDigestType digestType, bool enabled)
{
	GetMutableThreadDataHashAlgorithmSelectionState(threadData).enabled[GetHashAlgorithmIndex(digestType)] = enabled;
}

static inline bool IsThreadDataHashAlgorithmEnabled(const ThreadData& threadData, ResultDigestType digestType)
{
	const HashAlgorithmSelectionState& hashAlgorithmSelectionState = GetThreadDataHashAlgorithmSelectionState(threadData);
	size_t digestIndex = static_cast<size_t>(GetHashAlgorithmIndex(digestType));
	if (digestIndex >= hashAlgorithmSelectionState.enabled.size())
	{
		return true;
	}
	return hashAlgorithmSelectionState.enabled[digestIndex];
}

template<typename THashAlgorithmVisitor>
static inline bool VisitEnabledThreadDataHashAlgorithms(const ThreadData& threadData, THashAlgorithmVisitor visitor)
{
	return VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)
	{
		(void)index;
		ResultDigestType digestType = GetHashAlgorithmDescriptorType(algorithmDescriptor);
		if (!IsThreadDataHashAlgorithmEnabled(threadData, digestType))
		{
			return true;
		}

		return visitor(digestType);
	});
}

static inline size_t GetEnabledThreadDataHashAlgorithmCount(const ThreadData& threadData)
{
	size_t enabledCount = 0;

	VisitEnabledThreadDataHashAlgorithms(threadData, [&](ResultDigestType digestType)
	{
		(void)digestType;
		++enabledCount;
		return true;
	});

	return enabledCount;
}

static inline bool HasEnabledThreadDataHashAlgorithms(const ThreadData& threadData)
{
	return GetEnabledThreadDataHashAlgorithmCount(threadData) > 0;
}

static inline void ResetThreadDataHashAlgorithms(ThreadData& threadData)
{
	VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)
	{
		(void)index;
		ResultDigestType digestType = GetHashAlgorithmDescriptorType(algorithmDescriptor);
		SetThreadDataHashAlgorithmEnabled(threadData, digestType, true);
		return true;
	});
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

#endif
