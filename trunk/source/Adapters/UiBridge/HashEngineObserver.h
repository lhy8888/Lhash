#ifndef _HASH_ENGINE_OBSERVER_H_
#define _HASH_ENGINE_OBSERVER_H_

#include "Common/HashProgressSink.h"

class HashEngineObserver: public HashProgressSink
{
public:
	HashEngineObserver() {}
	virtual ~HashEngineObserver() {}

	void onPreparing()
	{
		preparingCalc();
	}

	void onPreparationFinished()
	{
		removePreparingCalc();
	}

	void onCancelled()
	{
		calcStop();
	}

	void onCompleted()
	{
		calcFinish();
	}

	void onFileStarted(const HashResult& result)
	{
		showFileName(result);
	}

	void onFileMetaReady(const HashResult& result)
	{
		showFileMeta(result);
	}

	void onFileHashReady(const HashResult& result, bool uppercase)
	{
		showFileHash(result, uppercase);
	}

	void onFileFailed(const HashResult& result)
	{
		showFileErr(result);
	}

	virtual int progressMax()
	{
		return getProgMax();
	}

	void onFileProgress(int value)
	{
		updateProg(value);
	}

	void onTotalProgress(int value)
	{
		updateProgWhole(value);
	}

	void onFileCalculated()
	{
		fileCalcFinish();
	}

	void onFileFinished()
	{
		fileFinish();
	}

	virtual void onProgressEvent(const ProgressEvent& progressEvent)
	{
		switch (progressEvent.type)
		{
		case PROGRESS_EVENT_JOB_PREPARING:
			preparingCalc();
			break;
		case PROGRESS_EVENT_JOB_PREPARATION_FINISHED:
			removePreparingCalc();
			break;
		case PROGRESS_EVENT_JOB_CANCELLED:
			calcStop();
			break;
		case PROGRESS_EVENT_JOB_COMPLETED:
			calcFinish();
			break;
		case PROGRESS_EVENT_FILE_STARTED:
			onFileStarted(progressEvent.result);
			break;
		case PROGRESS_EVENT_FILE_META_READY:
			onFileMetaReady(progressEvent.result);
			break;
		case PROGRESS_EVENT_FILE_HASH_READY:
			onFileHashReady(progressEvent.result, progressEvent.uppercaseDigest);
			break;
		case PROGRESS_EVENT_FILE_FAILED:
			onFileFailed(progressEvent.result);
			break;
		case PROGRESS_EVENT_FILE_PROGRESS:
			updateProg(progressEvent.value);
			break;
		case PROGRESS_EVENT_TOTAL_PROGRESS:
			updateProgWhole(progressEvent.value);
			break;
		case PROGRESS_EVENT_FILE_CALCULATED:
			fileCalcFinish();
			break;
		case PROGRESS_EVENT_FILE_FINISHED:
			fileFinish();
			break;
		case PROGRESS_EVENT_NONE:
		default:
			break;
		}
	}

	virtual void preparingCalc() = 0;
	virtual void removePreparingCalc() = 0;
	virtual void calcStop() = 0;
	virtual void calcFinish() = 0;

	virtual void showFileName(const HashResult& result) = 0;
	virtual void showFileMeta(const HashResult& result) = 0;
	virtual void showFileHash(const HashResult& result, bool uppercase) = 0;
	virtual void showFileErr(const HashResult& result) = 0;

	virtual int getProgMax() = 0;
	virtual void updateProg(int value) = 0;
	virtual void updateProgWhole(int value) = 0;

	virtual void fileCalcFinish() = 0;
	virtual void fileFinish() = 0;
};

#endif
