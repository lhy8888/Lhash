#ifndef _HASH_ENGINE_OBSERVER_H_
#define _HASH_ENGINE_OBSERVER_H_

struct ResultData;

class HashEngineObserver
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

	void onFileStarted(const ResultData& result)
	{
		showFileName(result);
	}

	void onFileMetaReady(const ResultData& result)
	{
		showFileMeta(result);
	}

	void onFileHashReady(const ResultData& result, bool uppercase)
	{
		showFileHash(result, uppercase);
	}

	void onFileFailed(const ResultData& result)
	{
		showFileErr(result);
	}

	int progressMax()
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

	virtual void preparingCalc() = 0;
	virtual void removePreparingCalc() = 0;
	virtual void calcStop() = 0;
	virtual void calcFinish() = 0;

	virtual void showFileName(const ResultData& result) = 0;
	virtual void showFileMeta(const ResultData& result) = 0;
	virtual void showFileHash(const ResultData& result, bool uppercase) = 0;
	virtual void showFileErr(const ResultData& result) = 0;

	virtual int getProgMax() = 0;
	virtual void updateProg(int value) = 0;
	virtual void updateProgWhole(int value) = 0;

	virtual void fileCalcFinish() = 0;
	virtual void fileFinish() = 0;
};

#endif
