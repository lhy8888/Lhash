#ifndef _HASH_ENGINE_INTERNAL_H_
#define _HASH_ENGINE_INTERNAL_H_

#include <cstring>

#include "Common/HashEngine.h"
#include "Common/HashDigestBufferPlan.h"
#include "Common/HashDigestCompletion.h"
#include "Common/HashDigestContextOps.h"
#include "Common/HashDigestExecution.h"
#include "Common/HashDigestExecutionMode.h"
#include "Common/HashDigestLifecycle.h"
#include "Common/HashDigestOperationRegistry.h"
#include "Common/HashDigestQueue.h"
#include "Common/HashDigestQueuePlan.h"
#include "Common/HashDigestRuntimePlan.h"
#include "Common/HashDigestPipeline.h"
#include "Common/HashDigestSinglePass.h"
#include "Common/HashDigestUpdater.h"
#include "Common/HashFileAttemptCompletionWorkflow.h"
#include "Common/HashFileAttemptWorkflow.h"
#include "Common/HashFileResultWorkflow.h"
#include "Common/HashFileAttemptStateOps.h"
#include "Common/HashFileSizeAccounting.h"
#include "Common/HashFileVersionResolver.h"
#include "Common/HashJobExecutionPlan.h"
#include "Common/HashJobLifecycleWorkflow.h"
#include "Common/HashPreparationPlan.h"
#include "Common/HashPreScanSizeProbe.h"
#include "Common/HashPreScanSizeAccounting.h"
#include "Common/HashPreScanWorkflow.h"
#include "Common/HashPreparationWorkflow.h"
#include "Common/HashProgressTracker.h"
#include "Runtime/Hash/BLAKE3HashProvider.h"
#include "Runtime/HashExecutionContext.h"
#include "Runtime/HashProgressSink.h"
#include "Common/HashErrorResultWorkflow.h"
#include "Common/HashResultEventWorkflow.h"
#include "Common/HashResultPublisher.h"
#include "Common/HashSuccessfulFileCompletionWorkflow.h"
#include "Domain/HashRequest.h"
#include "Common/HashSchedulerDispatch.h"
#include "Common/HashSchedulerPlan.h"
#include "Common/ResultDigestMetadataAccess.h"
#include "Common/ResultDigestStateAccess.h"

#include "OsUtils/OsFile.h"

#include "Algorithms/MD5.h"
#include "Algorithms/SHA1.h"
#include "Algorithms/sha256.h"
#include "Algorithms/sha512.h"

class ThreadPool;

namespace HashEngineInternal
{
	struct FileProgressState
	{
		FileProgressState()
			: finishedSize(0),
			finishedSizeWhole(0),
			position(0),
			positionWhole(0)
		{
		}

		uint64_t finishedSize;
		uint64_t finishedSizeWhole;
		int position;
		int positionWhole;
	};

	struct FileAttemptState
	{
		FileAttemptState()
			: path(NULL),
			osFile(NULL),
			fileVersion(),
			readFailed(false),
			isFileOpened(false),
			openErrorText(NULL)
		{
		}

		const TCHAR *path;
		sunjwbase::OsFile *osFile;
		sunjwbase::tstring fileVersion;
		bool readFailed;
		bool isFileOpened;
		const TCHAR *openErrorText;
	};

	struct FileHashContexts
	{
		FileHashContexts()
			: mdContext(),
			sha1(),
			sha256Ctx(),
			sha512Ctx(),
			blake3_256(),
			blake3_512(),
			blake3Xof()
		{
			std::memset(digestSHA512, 0, sizeof(digestSHA512));
		}

		MD5_CTX mdContext;
		CSHA1 sha1;
		SHA256_CTX sha256Ctx;
		SHA512_CTX sha512Ctx;
		blake3_hasher blake3_256;
		blake3_hasher blake3_512;
		blake3_hasher blake3Xof;
		uint8_t digestSHA512[SHA512_DIGEST_LENGTH];
	};

	typedef ResultDigestStorage FinalizedDigestBundle;

	struct FileExecutionState
	{
		FileExecutionState()
			: progressState(),
			fileAttemptState(),
			hashContexts(),
			executionPlan(),
			digestBundle()
		{
		}

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
		FileExecutionState *executionState, ThreadPool *threadPool);

	void EmitPathResult(HashExecutionContext *executionContext, HashResult& result);
	HashResult& BeginFileResult(HashExecutionContext *executionContext, const sunjwbase::tstring& path);
	HashResult& BeginFileHashAttempt(HashExecutionContext *executionContext, const sunjwbase::tstring& path, FileExecutionState *executionState, const TCHAR **resultPath);

	uint64_t PrepareFileMetaResult(HashExecutionContext *executionContext, HashResult& result,
		sunjwbase::OsFile& osFile, const TCHAR *path, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, sunjwbase::tstring& tstrFileVersion);
}

#endif
