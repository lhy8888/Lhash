#ifndef _PROGRESS_EVENT_H_
#define _PROGRESS_EVENT_H_

#include "Common/HashResult.h"

enum ProgressEventType
{
	PROGRESS_EVENT_NONE = 0,
	PROGRESS_EVENT_JOB_PREPARING,
	PROGRESS_EVENT_JOB_PREPARATION_FINISHED,
	PROGRESS_EVENT_JOB_CANCELLED,
	PROGRESS_EVENT_JOB_COMPLETED,
	PROGRESS_EVENT_FILE_STARTED,
	PROGRESS_EVENT_FILE_META_READY,
	PROGRESS_EVENT_FILE_HASH_READY,
	PROGRESS_EVENT_FILE_FAILED,
	PROGRESS_EVENT_FILE_PROGRESS,
	PROGRESS_EVENT_TOTAL_PROGRESS,
	PROGRESS_EVENT_FILE_CALCULATED,
	PROGRESS_EVENT_FILE_FINISHED
};

struct ProgressEvent
{
	ProgressEvent()
		: type(PROGRESS_EVENT_NONE),
		value(0),
		uppercaseDigest(false)
	{
	}

	ProgressEventType type;
	HashResult result;
	int value;
	bool uppercaseDigest;
};

static inline ProgressEvent CreateLifecycleProgressEvent(ProgressEventType eventType)
{
	ProgressEvent progressEvent;
	progressEvent.type = eventType;
	return progressEvent;
}

static inline ProgressEvent CreateResultProgressEvent(ProgressEventType eventType, const ResultData& result)
{
	ProgressEvent progressEvent = CreateLifecycleProgressEvent(eventType);
	progressEvent.result = ProjectHashResult(result);
	return progressEvent;
}

static inline ProgressEvent CreatePreparingProgressEvent()
{
	return CreateLifecycleProgressEvent(PROGRESS_EVENT_JOB_PREPARING);
}

static inline ProgressEvent CreatePreparationFinishedProgressEvent()
{
	return CreateLifecycleProgressEvent(PROGRESS_EVENT_JOB_PREPARATION_FINISHED);
}

static inline ProgressEvent CreateCancelledProgressEvent()
{
	return CreateLifecycleProgressEvent(PROGRESS_EVENT_JOB_CANCELLED);
}

static inline ProgressEvent CreateCompletedProgressEvent()
{
	return CreateLifecycleProgressEvent(PROGRESS_EVENT_JOB_COMPLETED);
}

static inline ProgressEvent CreateFileStartedProgressEvent(const ResultData& result)
{
	return CreateResultProgressEvent(PROGRESS_EVENT_FILE_STARTED, result);
}

static inline ProgressEvent CreateFileMetaReadyProgressEvent(const ResultData& result)
{
	return CreateResultProgressEvent(PROGRESS_EVENT_FILE_META_READY, result);
}

static inline ProgressEvent CreateFileHashReadyProgressEvent(const ResultData& result, bool uppercaseDigest)
{
	ProgressEvent progressEvent = CreateResultProgressEvent(PROGRESS_EVENT_FILE_HASH_READY, result);
	progressEvent.uppercaseDigest = uppercaseDigest;
	return progressEvent;
}

static inline ProgressEvent CreateFileFailedProgressEvent(const ResultData& result)
{
	return CreateResultProgressEvent(PROGRESS_EVENT_FILE_FAILED, result);
}

static inline ProgressEvent CreateValueProgressEvent(ProgressEventType eventType, int value)
{
	ProgressEvent progressEvent = CreateLifecycleProgressEvent(eventType);
	progressEvent.value = value;
	return progressEvent;
}

static inline ProgressEvent CreateFileProgressEvent(int value)
{
	return CreateValueProgressEvent(PROGRESS_EVENT_FILE_PROGRESS, value);
}

static inline ProgressEvent CreateTotalProgressEvent(int value)
{
	return CreateValueProgressEvent(PROGRESS_EVENT_TOTAL_PROGRESS, value);
}

static inline ProgressEvent CreateFileCalculatedProgressEvent()
{
	return CreateLifecycleProgressEvent(PROGRESS_EVENT_FILE_CALCULATED);
}

static inline ProgressEvent CreateFileFinishedProgressEvent()
{
	return CreateLifecycleProgressEvent(PROGRESS_EVENT_FILE_FINISHED);
}

#endif
