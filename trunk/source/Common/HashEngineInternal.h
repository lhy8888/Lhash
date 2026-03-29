#ifndef _HASH_ENGINE_INTERNAL_H_
#define _HASH_ENGINE_INTERNAL_H_

#include "Common/HashEngine.h"
#include "Common/HashExecutionContext.h"
#include "Common/HashProgressSink.h"
#include "Common/HashRequest.h"
#include "Common/ThreadDataExecutionAccess.h"
#include "Common/ThreadDataInputAccess.h"
#include "Common/ThreadDataResultAccess.h"
#include "Common/ResultDataAccess.h"
#include "Common/ResultDigestStateAccess.h"

#include "OsUtils/OsFile.h"

#include "Algorithms/MD5.h"
#include "Algorithms/SHA1.h"
#include "Algorithms/sha256.h"
#include "Algorithms/sha512.h"

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
		FinalizedDigestBundle digestBundle;
	};

	void AccumulatePreScannedFileSize(HashExecutionContext *executionContext, const HashRequest& request, ULLongVector& fSizes, uint32_t fileIndex);
	bool TryPreScanSmallBatchFileSizes(HashExecutionContext *executionContext, const HashRequest& request, ULLongVector& fSizes, bool *wasCancelled);
	bool PrepareHashingWork(HashExecutionContext *executionContext, const HashRequest& request, ULLongVector& fSizes, bool *wasCancelled);

	void InitializeFileAttemptState(const TCHAR *path, sunjwbase::OsFile *osFile, FileAttemptState *fileAttemptState);
	bool OpenFileForHashing(FileAttemptState *fileAttemptState, void *openErrorBuffer);
	void ResetFileProgressState(FileProgressState *progressState);

	void EmitPathResult(HashExecutionContext *executionContext, ResultData& result);
	ResultData& BeginFileResult(HashExecutionContext *executionContext, const sunjwbase::tstring& path);
	ResultData& BeginFileHashAttempt(HashExecutionContext *executionContext, const sunjwbase::tstring& path, FileExecutionState *executionState, const TCHAR **resultPath);

	uint64_t PrepareFileMetaResult(HashExecutionContext *executionContext, ResultData& result,
		sunjwbase::OsFile& osFile, const TCHAR *path, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, sunjwbase::tstring& tstrFileVersion);
	void InitializeFileHashing(const HashRequest& request, HashExecutionContext *executionContext, FileHashContexts *hashContexts);
	void UpdateWholeProgressAfterFile(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, uint32_t fileIndex);
	const sunjwbase::tstring& GetFinalizedDigestValue(const FinalizedDigestBundle& digestBundle, ResultDigestType digestType);
	void SetFinalizedDigestValue(FinalizedDigestBundle& digestBundle, ResultDigestType digestType, const sunjwbase::tstring& digestValue);
	void PopulateDigestResult(const HashRequest& request, ResultData& result, const FinalizedDigestBundle& digestBundle);
	void FinalizeDigestStrings(const HashRequest& request, FileHashContexts& hashContexts, FinalizedDigestBundle& digestBundle);

	void CompleteSuccessfulFileHashing(HashExecutionContext *executionContext, const HashRequest& request, ResultData& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState);
	void CompleteOpenedFileAttempt(HashExecutionContext *executionContext, const HashRequest& request, ResultData& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState);
	void EmitMetaResult(HashExecutionContext *executionContext, ResultData& result);
	void EmitHashResult(HashExecutionContext *executionContext, ResultData& result, bool uppercase);
	void EmitErrorResult(HashExecutionContext *executionContext, ResultData& result);
	void EmitErrorMessageResult(HashExecutionContext *executionContext, ResultData& result, const sunjwbase::tstring& errorText);
	void EmitOpenFileError(HashExecutionContext *executionContext, ResultData& result, const TCHAR *errorText);
	void EmitReadFileError(HashExecutionContext *executionContext, ResultData& result);
	void FinishFileProcessing(HashExecutionContext *executionContext);
	void CompleteFileAttempt(HashExecutionContext *executionContext, const HashRequest& request, ResultData& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState);
}

#endif
