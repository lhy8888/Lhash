#ifndef _HASH_ENGINE_INTERNAL_H_
#define _HASH_ENGINE_INTERNAL_H_

#include "Common/HashEngine.h"
#include "Common/HashDigestBufferPlan.h"
#include "Common/HashDigestCompletion.h"
#include "Common/HashDigestContextOps.h"
#include "Common/HashDigestExecution.h"
#include "Common/HashDigestExecutionMode.h"
#include "Common/HashDigestLifecycle.h"
#include "Common/HashDigestQueue.h"
#include "Common/HashDigestQueuePlan.h"
#include "Common/HashDigestRuntimePlan.h"
#include "Common/HashDigestPipeline.h"
#include "Common/HashDigestSinglePass.h"
#include "Common/HashDigestUpdater.h"
#include "Common/HashFileAttemptWorkflow.h"
#include "Common/HashFileResultWorkflow.h"
#include "Common/HashFileAttemptStateOps.h"
#include "Common/HashFileSizeAccounting.h"
#include "Common/HashFileVersionResolver.h"
#include "Common/HashJobExecutionPlan.h"
#include "Common/HashPreparationPlan.h"
#include "Common/HashPreScanSizeProbe.h"
#include "Common/HashPreScanSizeAccounting.h"
#include "Common/HashPreScanWorkflow.h"
#include "Common/HashPreparationWorkflow.h"
#include "Common/HashProgressTracker.h"
#include "Common/HashExecutionContext.h"
#include "Common/HashProgressSink.h"
#include "Common/HashResultPublisher.h"
#include "Common/HashRequest.h"
#include "Common/HashSchedulerDispatch.h"
#include "Common/HashSchedulerPlan.h"
#include "Common/ThreadDataExecutionAccess.h"
#include "Common/ThreadDataInputAccess.h"
#include "Common/ThreadDataResultAccess.h"
#include "Common/ResultDigestMetadataAccess.h"
#include "Common/ResultDigestStateAccess.h"

#include "OsUtils/OsFile.h"

#include "Algorithms/MD5.h"
#include "Algorithms/SHA1.h"
#include "Algorithms/sha256.h"
#include "Algorithms/sha512.h"

#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
class ThreadPool;
#endif

namespace HashEngineInternal
{
	struct FileProgressState
	{
		uint64_t finishedSize;
		uint64_t finishedSizeWhole;
		int position;
		int positionWhole;
	};

	struct FileAttemptState
	{
		const TCHAR *path;
		sunjwbase::OsFile *osFile;
		sunjwbase::tstring fileVersion;
		bool readFailed;
		bool isFileOpened;
		const TCHAR *openErrorText;
	};

	struct FileHashContexts
	{
		MD5_CTX mdContext;
		CSHA1 sha1;
		SHA256_CTX sha256Ctx;
		SHA512_CTX sha512Ctx;
		uint8_t digestSHA512[SHA512_DIGEST_LENGTH];
	};

	typedef ResultDigestStorage FinalizedDigestBundle;

	struct FileExecutionState
	{
		FileProgressState progressState;
		FileAttemptState fileAttemptState;
		FileHashContexts hashContexts;
		HashJobExecutionPlan executionPlan;
		FinalizedDigestBundle digestBundle;
	};

	void AccumulatePreScannedFileSize(HashExecutionContext *executionContext, const HashRequest& request, ULLongVector& fSizes, uint32_t fileIndex);
	bool TryPreScanSmallBatchFileSizes(HashExecutionContext *executionContext, const HashRequest& request, const HashPreparationPlan& preparationPlan, ULLongVector& fSizes, bool *wasCancelled);
	bool PrepareHashingWork(HashExecutionContext *executionContext, const HashRequest& request, const HashPreparationPlan& preparationPlan, ULLongVector& fSizes, bool *wasCancelled);
	bool RunHashScheduler(HashExecutionContext *executionContext, const HashRequest& request, const HashJobExecutionPlan& executionPlan, bool isSizeCaled, ULLongVector& fSizes);

	bool RunFileHashAttempt(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const sunjwbase::tstring& fullPath, bool isSizeCaled, ULLongVector& fSizes,
		FileExecutionState *executionState
#if !defined (FHASH_SINGLE_THREAD_HASH_UPDATE)
		, ThreadPool *threadPool
#endif
	);

	void EmitPathResult(HashExecutionContext *executionContext, HashResult& result);
	HashResult& BeginFileResult(HashExecutionContext *executionContext, const sunjwbase::tstring& path);
	HashResult& BeginFileHashAttempt(HashExecutionContext *executionContext, const sunjwbase::tstring& path, FileExecutionState *executionState, const TCHAR **resultPath);

	uint64_t PrepareFileMetaResult(HashExecutionContext *executionContext, HashResult& result,
		sunjwbase::OsFile& osFile, const TCHAR *path, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, sunjwbase::tstring& tstrFileVersion);
}

#endif
