#ifndef _HASH_ENGINE_OBSERVER_H_
#define _HASH_ENGINE_OBSERVER_H_

#include "Common/HashProgressSink.h"

class HashEngineObserver: public HashProgressSink
{
public:
	HashEngineObserver() {}
	virtual ~HashEngineObserver() {}

	virtual int progressMax()
	{
		return queryProgressMax();
	}

	virtual void onProgressEvent(const ProgressEvent& progressEvent)
	{
		switch (progressEvent.type)
		{
		case PROGRESS_EVENT_JOB_PREPARING:
			onJobPreparing();
			break;
		case PROGRESS_EVENT_JOB_PREPARATION_FINISHED:
			onJobPreparationFinished();
			break;
		case PROGRESS_EVENT_JOB_CANCELLED:
			onJobCancelled();
			break;
		case PROGRESS_EVENT_JOB_COMPLETED:
			onJobCompleted();
			break;
		case PROGRESS_EVENT_FILE_STARTED:
		case PROGRESS_EVENT_FILE_META_READY:
		case PROGRESS_EVENT_FILE_HASH_READY:
		case PROGRESS_EVENT_FILE_FAILED:
			onFileResultEvent(progressEvent.result, progressEvent.type, progressEvent.uppercaseDigest);
			break;
		case PROGRESS_EVENT_FILE_PROGRESS:
			onFileProgressValue(progressEvent.value);
			break;
		case PROGRESS_EVENT_TOTAL_PROGRESS:
			onTotalProgressValue(progressEvent.value);
			break;
		case PROGRESS_EVENT_FILE_CALCULATED:
			onFileCalculated();
			break;
		case PROGRESS_EVENT_FILE_FINISHED:
			onFileFinished();
			break;
		case PROGRESS_EVENT_NONE:
		default:
			break;
		}
	}

	virtual void onJobPreparing() = 0;
	virtual void onJobPreparationFinished() = 0;
	virtual void onJobCancelled() = 0;
	virtual void onJobCompleted() = 0;
	virtual void onFileResultEvent(const HashResult& result,
									ProgressEventType eventType,
									bool uppercaseDigest) = 0;
	virtual int queryProgressMax() = 0;
	virtual void onFileProgressValue(int value) = 0;
	virtual void onTotalProgressValue(int value) = 0;
	virtual void onFileCalculated() = 0;
	virtual void onFileFinished() = 0;
};

#endif
