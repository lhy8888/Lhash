#ifndef _HASH_ENGINE_INTERNAL_H_
#define _HASH_ENGINE_INTERNAL_H_

#include "Common/HashEngine.h"
#include "Common/HashEngineObserver.h"
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

	void AccumulatePreScannedFileSize(ThreadData *thrdData, ULLongVector& fSizes, uint32_t fileIndex);
	bool TryPreScanSmallBatchFileSizes(ThreadData *thrdData, ULLongVector& fSizes, bool *wasCancelled);
	bool PrepareHashingWork(ThreadData *thrdData, HashEngineObserver *observer, ULLongVector& fSizes, bool *wasCancelled);

	void InitializeFileAttemptState(const TCHAR *path, sunjwbase::OsFile *osFile, FileAttemptState *fileAttemptState);
	bool OpenFileForHashing(FileAttemptState *fileAttemptState, void *openErrorBuffer);
	void ResetFileProgressState(FileProgressState *progressState);

	void EmitPathResult(HashEngineObserver *observer, ResultData& result);
	ResultData& BeginFileResult(ThreadData *thrdData, HashEngineObserver *observer, const sunjwbase::tstring& path);
	ResultData& BeginFileHashAttempt(ThreadData *thrdData, HashEngineObserver *observer, const sunjwbase::tstring& path, FileExecutionState *executionState, const TCHAR **resultPath);

	uint64_t PrepareFileMetaResult(ThreadData *thrdData, HashEngineObserver *observer, ResultData& result,
		sunjwbase::OsFile& osFile, const TCHAR *path, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, sunjwbase::tstring& tstrFileVersion);
	void InitializeFileHashing(const ThreadData& threadData, HashEngineObserver *observer, FileHashContexts *hashContexts);
	void UpdateWholeProgressAfterFile(HashEngineObserver *observer, ThreadData *thrdData, bool isSizeCaled, uint32_t fileIndex);
	const sunjwbase::tstring& GetFinalizedDigestValue(const FinalizedDigestBundle& digestBundle, ResultDigestType digestType);
	void SetFinalizedDigestValue(FinalizedDigestBundle& digestBundle, ResultDigestType digestType, const sunjwbase::tstring& digestValue);
	void PopulateDigestResult(const ThreadData& threadData, ResultData& result, const FinalizedDigestBundle& digestBundle);
	void FinalizeDigestStrings(const ThreadData& threadData, FileHashContexts& hashContexts, FinalizedDigestBundle& digestBundle);

	void CompleteSuccessfulFileHashing(HashEngineObserver *observer, ThreadData *thrdData, ResultData& result, uint32_t fileIndex, bool isSizeCaled, bool uppercase,
		FileExecutionState& executionState);
	void CompleteOpenedFileAttempt(HashEngineObserver *observer, ThreadData *thrdData, ResultData& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState);
	void EmitMetaResult(HashEngineObserver *observer, ResultData& result);
	void EmitHashResult(HashEngineObserver *observer, ResultData& result, bool uppercase);
	void EmitErrorResult(HashEngineObserver *observer, ResultData& result);
	void EmitErrorMessageResult(HashEngineObserver *observer, ResultData& result, const sunjwbase::tstring& errorText);
	void EmitOpenFileError(HashEngineObserver *observer, ResultData& result, const TCHAR *errorText);
	void EmitReadFileError(HashEngineObserver *observer, ResultData& result);
	void FinishFileProcessing(HashEngineObserver *observer);
	void CompleteFileAttempt(HashEngineObserver *observer, ThreadData *thrdData, ResultData& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState);
}

#endif
