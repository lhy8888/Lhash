#ifndef _LEGACY_THREAD_DATA_EXECUTION_ACCESS_H_
#define _LEGACY_THREAD_DATA_EXECUTION_ACCESS_H_

#include "LegacyCompat/LegacyThreadData.h"
#include "LegacyCompat/HashAlgorithmTypeCompat.h"

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

static inline const HashExecutionPreferenceState& GetThreadDataHashExecutionPreferenceState(const ThreadData& threadData)
{
	return GetThreadDataExecutionState(threadData).preferences;
}

static inline HashExecutionPreferenceState& GetMutableThreadDataHashExecutionPreferenceState(ThreadData& threadData)
{
	return GetMutableThreadDataExecutionState(threadData).preferences;
}

static inline const HashCancellationState& GetThreadDataHashCancellationState(const ThreadData& threadData)
{
	return GetThreadDataExecutionState(threadData).cancellation;
}

static inline HashCancellationState& GetMutableThreadDataHashCancellationState(ThreadData& threadData)
{
	return GetMutableThreadDataExecutionState(threadData).cancellation;
}

static inline const HashJobState& GetThreadDataHashJobState(const ThreadData& threadData)
{
	return GetThreadDataExecutionState(threadData).jobState;
}

static inline HashJobState& GetMutableThreadDataHashJobState(ThreadData& threadData)
{
	return GetMutableThreadDataExecutionState(threadData).jobState;
}

static inline const HashAlgorithmSelectionState& GetThreadDataHashAlgorithmSelectionState(const ThreadData& threadData)
{
	return GetThreadDataHashExecutionPreferenceState(threadData).hashAlgorithms;
}

static inline HashAlgorithmSelectionState& GetMutableThreadDataHashAlgorithmSelectionState(ThreadData& threadData)
{
	HashAlgorithmSelectionState& hashAlgorithmSelectionState = GetMutableThreadDataHashExecutionPreferenceState(threadData).hashAlgorithms;
	EnsureThreadDataHashAlgorithmSelectionStateSize(hashAlgorithmSelectionState);
	return hashAlgorithmSelectionState;
}

static inline void SetThreadDataWorking(ThreadData& threadData, bool working)
{
	GetMutableThreadDataHashJobState(threadData).working.store(working);
}

static inline bool IsThreadDataWorking(const ThreadData& threadData)
{
	return GetThreadDataHashJobState(threadData).working.load();
}

static inline void SetThreadDataStop(ThreadData& threadData, bool stopValue)
{
	GetMutableThreadDataHashCancellationState(threadData).stopRequested.store(stopValue);
}

static inline bool ShouldStopThreadData(const ThreadData& threadData)
{
	return GetThreadDataHashCancellationState(threadData).stopRequested.load();
}

static inline void SetThreadDataUppercase(ThreadData& threadData, bool uppercase)
{
	GetMutableThreadDataHashExecutionPreferenceState(threadData).uppercaseDigest = uppercase;
}

static inline bool GetThreadDataUppercase(const ThreadData& threadData)
{
	return GetThreadDataHashExecutionPreferenceState(threadData).uppercaseDigest;
}

static inline void SetThreadDataHashAlgorithmEnabled(ThreadData& threadData, ResultDigestType digestType, bool enabled)
{
	int algorithmIndex;
	if (!TryGetHashAlgorithmIndex(digestType, &algorithmIndex))
	{
		return;
	}

	GetMutableThreadDataHashAlgorithmSelectionState(threadData).enabled[static_cast<size_t>(algorithmIndex)] = enabled;
}

static inline bool IsThreadDataHashAlgorithmEnabled(const ThreadData& threadData, ResultDigestType digestType)
{
	int algorithmIndex;
	if (!TryGetHashAlgorithmIndex(digestType, &algorithmIndex))
	{
		return false;
	}

	const HashAlgorithmSelectionState& hashAlgorithmSelectionState = GetThreadDataHashAlgorithmSelectionState(threadData);
	size_t digestIndex = static_cast<size_t>(algorithmIndex);
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
	return GetThreadDataHashJobState(threadData).countedSize;
}

static inline void ResetThreadDataTotalSize(ThreadData& threadData)
{
	GetMutableThreadDataHashJobState(threadData).countedSize = 0;
}

static inline void AddThreadDataTotalSize(ThreadData& threadData, uint64_t sizeDelta)
{
	GetMutableThreadDataHashJobState(threadData).countedSize += sizeDelta;
}

static inline void ReplaceThreadDataCountedFileSize(ThreadData& threadData, uint64_t previousSize, uint64_t currentSize)
{
	GetMutableThreadDataHashJobState(threadData).countedSize = GetThreadDataTotalSize(threadData) + currentSize - previousSize;
}

#endif
