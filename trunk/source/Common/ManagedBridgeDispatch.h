#ifndef _MANAGED_BRIDGE_DISPATCH_H_
#define _MANAGED_BRIDGE_DISPATCH_H_

#include "Common/HashResultProjection.h"

enum ManagedResultEventType
{
	MANAGED_RESULT_EVENT_FILE_STARTED,
	MANAGED_RESULT_EVENT_FILE_META_READY,
	MANAGED_RESULT_EVENT_FILE_HASH_READY,
	MANAGED_RESULT_EVENT_FILE_FAILED
};

template<typename TResultDataNet, typename TFileStartedAction, typename TFileMetadataAction, typename TFileHashAction, typename TFileErrorAction>
static inline void DispatchManagedResultEventByType(ManagedResultEventType eventType, TResultDataNet resultDataNet, bool uppercase, TFileStartedAction onFileStarted, TFileMetadataAction onFileMetadata, TFileHashAction onFileHash, TFileErrorAction onFileError)
{
	switch (eventType)
	{
	case MANAGED_RESULT_EVENT_FILE_STARTED:
		onFileStarted(resultDataNet);
		break;
	case MANAGED_RESULT_EVENT_FILE_META_READY:
		onFileMetadata(resultDataNet);
		break;
	case MANAGED_RESULT_EVENT_FILE_HASH_READY:
		onFileHash(resultDataNet, uppercase);
		break;
	case MANAGED_RESULT_EVENT_FILE_FAILED:
		onFileError(resultDataNet);
		break;
	}
}

enum ManagedBridgeLifecycleEventType
{
	MANAGED_BRIDGE_LIFECYCLE_JOB_PREPARING,
	MANAGED_BRIDGE_LIFECYCLE_JOB_PREPARATION_FINISHED,
	MANAGED_BRIDGE_LIFECYCLE_JOB_CANCELLED,
	MANAGED_BRIDGE_LIFECYCLE_JOB_COMPLETED,
	MANAGED_BRIDGE_LIFECYCLE_TOTAL_PROGRESS
};

template<typename TJobPreparingAction, typename TJobPreparationFinishedAction, typename TJobCancelledAction, typename TJobCompletedAction, typename TTotalProgressAction>
static inline void DispatchManagedLifecycleEventByType(ManagedBridgeLifecycleEventType eventType, int value, TJobPreparingAction onJobPreparing, TJobPreparationFinishedAction onJobPreparationFinished, TJobCancelledAction onJobCancelled, TJobCompletedAction onJobCompleted, TTotalProgressAction onTotalProgress)
{
	switch (eventType)
	{
	case MANAGED_BRIDGE_LIFECYCLE_JOB_PREPARING:
		onJobPreparing();
		break;
	case MANAGED_BRIDGE_LIFECYCLE_JOB_PREPARATION_FINISHED:
		onJobPreparationFinished();
		break;
	case MANAGED_BRIDGE_LIFECYCLE_JOB_CANCELLED:
		onJobCancelled();
		break;
	case MANAGED_BRIDGE_LIFECYCLE_JOB_COMPLETED:
		onJobCompleted();
		break;
	case MANAGED_BRIDGE_LIFECYCLE_TOTAL_PROGRESS:
		onTotalProgress(value);
		break;
	}
}

enum ManagedBridgeQueryType
{
	MANAGED_BRIDGE_QUERY_PROGRESS_VALUE_MAX
};

template<typename TProgressValueMaxQuery>
static inline int DispatchManagedQueryByType(ManagedBridgeQueryType queryType, TProgressValueMaxQuery queryProgressValueMax)
{
	switch (queryType)
	{
	case MANAGED_BRIDGE_QUERY_PROGRESS_VALUE_MAX:
		return queryProgressValueMax();
	}

	return 0;
}

template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter, typename TFileStartedAction, typename TFileMetadataAction, typename TFileHashAction, typename TFileErrorAction>
static inline void DispatchManagedBridgeResultEventByType(const HashResult& result, ManagedResultEventType eventType, bool uppercase, TStringConverter convertString, TFileStartedAction onFileStarted, TFileMetadataAction onFileMetadata, TFileHashAction onFileHash, TFileErrorAction onFileError)
{
	TResultDataNet resultDataNet = ProjectHashResultToNet<TResultDataNet, TResultStateNet>(result, convertString);
	DispatchManagedResultEventByType(eventType, resultDataNet, uppercase, onFileStarted, onFileMetadata, onFileHash, onFileError);
}

template<typename TJobPreparingAction, typename TJobPreparationFinishedAction, typename TJobCancelledAction, typename TJobCompletedAction, typename TTotalProgressAction>
static inline void DispatchManagedBridgeLifecycleEventByType(ManagedBridgeLifecycleEventType eventType, int value, TJobPreparingAction onJobPreparing, TJobPreparationFinishedAction onJobPreparationFinished, TJobCancelledAction onJobCancelled, TJobCompletedAction onJobCompleted, TTotalProgressAction onTotalProgress)
{
	DispatchManagedLifecycleEventByType(eventType, value, [&]()
	{
		onJobPreparing();
	}, [&]()
	{
		onJobPreparationFinished();
	}, [&]()
	{
		onJobCancelled();
	}, [&]()
	{
		onJobCompleted();
	}, [&](int progressValue)
	{
		onTotalProgress(progressValue);
	});
}

template<typename TResult, typename TProgressValueMaxQuery>
static inline TResult DispatchManagedBridgeQueryByType(ManagedBridgeQueryType queryType, TProgressValueMaxQuery queryProgressValueMax)
{
	return DispatchManagedQueryByType(queryType, [&]()
	{
		return queryProgressValueMax();
	});
}

#endif
