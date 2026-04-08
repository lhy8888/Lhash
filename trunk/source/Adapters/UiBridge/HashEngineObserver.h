#ifndef _HASH_ENGINE_OBSERVER_H_
#define _HASH_ENGINE_OBSERVER_H_

#include "Runtime/HashProgressSink.h"

class HashProgressEventBridge: public HashProgressSink
{
public:
	HashProgressEventBridge() {}
	virtual ~HashProgressEventBridge() {}

	virtual int progressMax()
	{
		return getProgressValueMax();
	}

	virtual void onProgressEvent(const ProgressEvent& progressEvent)
	{
		switch (progressEvent.type)
		{
		case PROGRESS_EVENT_JOB_PREPARING:
			handleJobPreparingEvent();
			break;
		case PROGRESS_EVENT_JOB_PREPARATION_FINISHED:
			handleJobPreparationFinishedEvent();
			break;
		case PROGRESS_EVENT_JOB_CANCELLED:
			handleJobCancelledEvent();
			break;
		case PROGRESS_EVENT_JOB_COMPLETED:
			handleJobCompletedEvent();
			break;
		case PROGRESS_EVENT_FILE_STARTED:
		case PROGRESS_EVENT_FILE_META_READY:
		case PROGRESS_EVENT_FILE_HASH_READY:
		case PROGRESS_EVENT_FILE_FAILED:
			handleFileResultProgressEvent(progressEvent.result, progressEvent.type, progressEvent.uppercaseDigest);
			break;
		case PROGRESS_EVENT_FILE_PROGRESS:
			handleFileProgressEvent(progressEvent.value);
			break;
		case PROGRESS_EVENT_TOTAL_PROGRESS:
			handleTotalProgressEvent(progressEvent.value);
			break;
		case PROGRESS_EVENT_FILE_CALCULATED:
			handleFileCalculatedEvent();
			break;
		case PROGRESS_EVENT_FILE_FINISHED:
			handleFileFinishedEvent();
			break;
		case PROGRESS_EVENT_NONE:
		default:
			break;
		}
	}

	virtual void handleJobPreparingEvent() = 0;
	virtual void handleJobPreparationFinishedEvent() = 0;
	virtual void handleJobCancelledEvent() = 0;
	virtual void handleJobCompletedEvent() = 0;
	virtual void handleFileResultProgressEvent(const HashResult& result,
												ProgressEventType eventType,
												bool uppercaseDigest) = 0;
	virtual int getProgressValueMax() = 0;
	virtual void handleFileProgressEvent(int value) = 0;
	virtual void handleTotalProgressEvent(int value) = 0;
	virtual void handleFileCalculatedEvent() = 0;
	virtual void handleFileFinishedEvent() = 0;
};

typedef HashProgressEventBridge HashEngineObserver;

#endif
