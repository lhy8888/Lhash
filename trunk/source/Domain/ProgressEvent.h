#ifndef _DOMAIN_PROGRESS_EVENT_H_
#define _DOMAIN_PROGRESS_EVENT_H_

#include "Domain/HashResult.h"

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
		taskIndex(kInvalidHashTaskIndex),
		value(0),
		uppercaseDigest(false)
	{
	}

	ProgressEventType type;
	uint32_t taskIndex;
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

static inline ProgressEvent CreateResultProgressEvent(ProgressEventType eventType, const HashResult& result);
static inline ProgressEvent CreateFileStartedProgressEvent(const HashResult& result);
static inline ProgressEvent CreateFileMetaReadyProgressEvent(const HashResult& result);
static inline ProgressEvent CreateFileHashReadyProgressEvent(const HashResult& result, bool uppercaseDigest);
static inline ProgressEvent CreateFileFailedProgressEvent(const HashResult& result);

static inline ProgressEvent CreateResultProgressEvent(ProgressEventType eventType, const HashResult& result)
{
	ProgressEvent progressEvent = CreateLifecycleProgressEvent(eventType);
	progressEvent.result = result;
	progressEvent.taskIndex = result.taskIndex;
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

static inline ProgressEvent CreateFileStartedProgressEvent(const HashResult& result)
{
	return CreateResultProgressEvent(PROGRESS_EVENT_FILE_STARTED, result);
}

static inline ProgressEvent CreateFileMetaReadyProgressEvent(const HashResult& result)
{
	return CreateResultProgressEvent(PROGRESS_EVENT_FILE_META_READY, result);
}

static inline ProgressEvent CreateFileHashReadyProgressEvent(const HashResult& result, bool uppercaseDigest)
{
	ProgressEvent progressEvent = CreateResultProgressEvent(PROGRESS_EVENT_FILE_HASH_READY, result);
	progressEvent.uppercaseDigest = uppercaseDigest;
	return progressEvent;
}

static inline ProgressEvent CreateFileFailedProgressEvent(const HashResult& result)
{
	return CreateResultProgressEvent(PROGRESS_EVENT_FILE_FAILED, result);
}

static inline ProgressEvent CreateValueProgressEvent(ProgressEventType eventType, int value)
{
	ProgressEvent progressEvent = CreateLifecycleProgressEvent(eventType);
	progressEvent.value = value;
	return progressEvent;
}

static inline ProgressEvent CreateFileProgressEvent(int value, uint32_t taskIndex = kInvalidHashTaskIndex)
{
	ProgressEvent progressEvent = CreateValueProgressEvent(PROGRESS_EVENT_FILE_PROGRESS, value);
	progressEvent.taskIndex = taskIndex;
	return progressEvent;
}

static inline ProgressEvent CreateTotalProgressEvent(int value)
{
	return CreateValueProgressEvent(PROGRESS_EVENT_TOTAL_PROGRESS, value);
}

static inline ProgressEvent CreateFileCalculatedProgressEvent(uint32_t taskIndex = kInvalidHashTaskIndex)
{
	ProgressEvent progressEvent = CreateLifecycleProgressEvent(PROGRESS_EVENT_FILE_CALCULATED);
	progressEvent.taskIndex = taskIndex;
	return progressEvent;
}

static inline ProgressEvent CreateFileFinishedProgressEvent(uint32_t taskIndex = kInvalidHashTaskIndex)
{
	ProgressEvent progressEvent = CreateLifecycleProgressEvent(PROGRESS_EVENT_FILE_FINISHED);
	progressEvent.taskIndex = taskIndex;
	return progressEvent;
}

#endif
