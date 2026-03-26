using System.Text;

internal static class Program
{
    private static int Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string repoRoot = FindRepoRoot();
        List<string> failures = [];

        Run("Phase 1 core contract narrows ThreadData to a HashEngineObserver observer while keeping result shape stable", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");

            AssertContains(global, "struct ResultData", "ResultData baseline struct is missing.");
            AssertContains(global, "ResultState state;", "ResultData no longer carries result state in the baseline contract.");
            AssertContains(global, "sunjwbase::tstring path;", "ResultData no longer carries the file path in the baseline contract.");
            AssertContains(global, "uint64_t size;", "ResultData no longer carries file size in the baseline contract.");
            AssertContains(global, "sunjwbase::tstring modifiedDate;", "ResultData no longer carries modified time in the baseline contract.");
            AssertContains(global, "sunjwbase::tstring version;", "ResultData no longer carries version info in the baseline contract.");
            AssertContains(global, "sunjwbase::tstring md5;", "ResultData no longer carries the fixed MD5 field in the baseline contract.");
            AssertContains(global, "sunjwbase::tstring sha1;", "ResultData no longer carries the fixed SHA1 field in the baseline contract.");
            AssertContains(global, "sunjwbase::tstring sha256;", "ResultData no longer carries the fixed SHA256 field in the baseline contract.");
            AssertContains(global, "sunjwbase::tstring sha512;", "ResultData no longer carries the fixed SHA512 field in the baseline contract.");
            AssertContains(global, "sunjwbase::tstring error;", "ResultData no longer carries the error string in the baseline contract.");

            AssertContains(global, "class HashEngineObserver;", "Global.h is missing the new phase-1 observer forward declaration.");
            AssertContains(global, "struct ThreadData", "ThreadData baseline struct is missing.");
            AssertContains(global, "struct ThreadDataInputState", "ThreadData baseline struct is missing the grouped input-state seam.");
            AssertContains(global, "struct ThreadDataExecutionState", "ThreadData baseline struct is missing the grouped execution-state seam.");
            AssertContains(global, "HashEngineObserver *observer;", "ThreadData is not yet narrowed to a neutral HashEngineObserver observer in the phase-1 contract.");
            AssertContains(global, "ThreadDataInputState inputState;", "ThreadData no longer carries the grouped input-state field in the baseline contract.");
            AssertContains(global, "ThreadDataExecutionState executionState;", "ThreadData no longer carries the grouped execution-state field in the baseline contract.");
            AssertDoesNotContain(global, "HashEngineObserver *uiBridge;", "ThreadData still uses the UI-specific uiBridge field name in the phase-1 contract.");
            AssertDoesNotContain(global, "UIBridgeBase *uiBridge;", "ThreadData still directly depends on UIBridgeBase in the phase-1 contract.");
            AssertContains(global, "bool working;", "ThreadData no longer carries the working-state flag in the baseline contract.");
            AssertContains(global, "bool stopRequested;", "ThreadData no longer carries the stop flag in the baseline contract.");
            AssertContains(global, "bool uppercaseDigest;", "ThreadData no longer carries the uppercase flag in the baseline contract.");
            AssertContains(global, "uint64_t countedSize;", "ThreadData no longer carries totalSize in the baseline contract.");
            AssertContains(global, "uint32_t fileCount;", "ThreadData no longer carries nFiles in the baseline contract.");
            AssertContains(global, "TStrVector inputFiles;", "ThreadData no longer carries fullPaths in the baseline contract.");
            AssertContains(global, "ResultList results;", "ThreadData no longer carries resultList in the baseline contract.");
            AssertDoesNotContain(global, "bool threadWorking;", "ThreadData still exposes the legacy threadWorking field name.");
            AssertDoesNotContain(global, "bool stop;", "ThreadData still exposes the legacy stop field name.");
            AssertDoesNotContain(global, "bool uppercase;", "ThreadData still exposes the legacy uppercase field name.");
            AssertDoesNotContain(global, "uint64_t totalSize;", "ThreadData still exposes the legacy totalSize field name.");
            AssertDoesNotContain(global, "uint32_t nFiles;", "ThreadData still exposes the legacy nFiles field name.");
            AssertDoesNotContain(global, "TStrVector fullPaths;", "ThreadData still exposes the legacy fullPaths field name.");
            AssertDoesNotContain(global, "ResultList resultList;", "ThreadData still exposes the legacy resultList field name.");
        }, failures);

        Run("Phase 1 introduces neutral observer wrapper methods while keeping a thin bridge adapter on top", () =>
        {
            string observer = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineObserver.h");
            string bridgeBase = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineBridge.h");
            string bridge = ReadRepoFile(repoRoot, @"trunk\source\Common\UIBridgeBase.h");

            AssertContains(observer, "class HashEngineObserver", "Phase-1 observer seam is missing.");
            AssertContains(observer, "virtual void preparingCalc() = 0;", "HashEngineObserver does not expose preparingCalc.");
            AssertContains(observer, "virtual void removePreparingCalc() = 0;", "HashEngineObserver does not expose removePreparingCalc.");
            AssertContains(observer, "virtual void calcStop() = 0;", "HashEngineObserver does not expose calcStop.");
            AssertContains(observer, "virtual void calcFinish() = 0;", "HashEngineObserver does not expose calcFinish.");
            AssertContains(observer, "virtual void showFileName(const ResultData& result) = 0;", "HashEngineObserver does not expose showFileName.");
            AssertContains(observer, "virtual void showFileMeta(const ResultData& result) = 0;", "HashEngineObserver does not expose showFileMeta.");
            AssertContains(observer, "virtual void showFileHash(const ResultData& result, bool uppercase) = 0;", "HashEngineObserver does not expose showFileHash.");
            AssertContains(observer, "virtual void showFileErr(const ResultData& result) = 0;", "HashEngineObserver does not expose showFileErr.");
            AssertContains(observer, "virtual void updateProg(int value) = 0;", "HashEngineObserver does not expose updateProg.");
            AssertContains(observer, "virtual void updateProgWhole(int value) = 0;", "HashEngineObserver does not expose updateProgWhole.");
            AssertContains(observer, "virtual void fileCalcFinish() = 0;", "HashEngineObserver does not expose fileCalcFinish.");
            AssertContains(observer, "virtual void fileFinish() = 0;", "HashEngineObserver does not expose fileFinish.");
            AssertContains(observer, "void onPreparing()", "HashEngineObserver does not yet offer a neutral preparing wrapper.");
            AssertContains(observer, "void onPreparationFinished()", "HashEngineObserver does not yet offer a neutral preparation-finished wrapper.");
            AssertContains(observer, "void onCancelled()", "HashEngineObserver does not yet offer a neutral cancellation wrapper.");
            AssertContains(observer, "void onCompleted()", "HashEngineObserver does not yet offer a neutral completion wrapper.");
            AssertContains(observer, "void onFileStarted(const ResultData& result)", "HashEngineObserver does not yet offer a neutral file-start wrapper.");
            AssertContains(observer, "void onFileMetaReady(const ResultData& result)", "HashEngineObserver does not yet offer a neutral file-metadata wrapper.");
            AssertContains(observer, "void onFileHashReady(const ResultData& result, bool uppercase)", "HashEngineObserver does not yet offer a neutral file-hash wrapper.");
            AssertContains(observer, "void onFileFailed(const ResultData& result)", "HashEngineObserver does not yet offer a neutral file-error wrapper.");
            AssertContains(observer, "int progressMax()", "HashEngineObserver does not yet offer a neutral progress-max wrapper.");
            AssertContains(observer, "void onFileProgress(int value)", "HashEngineObserver does not yet offer a neutral file-progress wrapper.");
            AssertContains(observer, "void onTotalProgress(int value)", "HashEngineObserver does not yet offer a neutral total-progress wrapper.");
            AssertContains(observer, "void onFileCalculated()", "HashEngineObserver does not yet offer a neutral file-calculated wrapper.");
            AssertContains(observer, "void onFileFinished()", "HashEngineObserver does not yet offer a neutral file-finished wrapper.");

            AssertContains(bridgeBase, "class HashEngineBridge: public HashEngineObserver", "HashEngineBridge is missing the neutral bridge seam on top of HashEngineObserver.");
            AssertContains(bridgeBase, "virtual void lockData() = 0;", "HashEngineBridge does not keep lockData on top of HashEngineObserver.");
            AssertContains(bridgeBase, "virtual void unlockData() = 0;", "HashEngineBridge does not keep unlockData on top of HashEngineObserver.");
            AssertContains(bridge, "class UIBridgeBase: public HashEngineBridge", "UIBridgeBase is not yet reduced to a compatibility adapter on top of HashEngineBridge.");
            AssertDoesNotContain(bridge, "virtual void preparingCalc() = 0;", "UIBridgeBase still duplicates HashEngineObserver methods instead of remaining a thin compatibility adapter.");
        }, failures);

        Run("Phase 1 HashEngine uses neutral observer wrappers and tiny emission helpers while preserving the current lifecycle and same-file multi-algorithm model", () =>
        {
            string engine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");

            AssertContains(engine, "#include \"Common/HashEngineObserver.h\"", "HashEngine.cpp is not yet including the phase-1 observer seam.");
            AssertDoesNotContain(engine, "#include \"Common/UIBridgeBase.h\"", "HashEngine.cpp still directly includes UIBridgeBase in phase 1.");
            AssertContains(engine, "HashEngineObserver *observer", "HashEngine.cpp is not yet narrowed to HashEngineObserver.");
            AssertContains(engine, "static ResultData& BeginFileResult(", "HashEngine.cpp does not yet expose a tiny file-begin helper.");
            AssertContains(engine, "static uint64_t PrepareFileMetaResult(", "HashEngine.cpp does not yet expose a tiny file-meta helper.");
            AssertContains(engine, "static void AccumulatePreScannedFileSize(", "HashEngine.cpp does not yet expose a tiny pre-scan file-size helper.");
            AssertContains(engine, "static bool TryPreScanSmallBatchFileSizes(", "HashEngine.cpp does not yet expose a tiny small-batch pre-scan helper.");
            AssertContains(engine, "static bool PrepareHashingWork(", "HashEngine.cpp does not yet expose a tiny preparation-phase helper.");
            AssertContains(engine, "static bool OpenFileForHashing(", "HashEngine.cpp does not yet expose a tiny file-open helper.");
            AssertContains(engine, "static void InitializeFileAttemptState(", "HashEngine.cpp does not yet expose the grouped file-attempt state initializer introduced after phase 3.");
            AssertContains(engine, "static bool ProcessOpenedFileHashing(", "HashEngine.cpp does not yet expose a tiny opened-file processing helper.");
            AssertContains(engine, "static ResultData& BeginFileHashAttempt(", "HashEngine.cpp does not yet expose a tiny file-attempt begin helper.");
            AssertContains(engine, "static void ResetFileProgressState(", "HashEngine.cpp does not yet expose a tiny file-progress reset helper.");
            AssertContains(engine, "struct FileProgressState", "HashEngine.cpp does not yet expose the grouped file-progress state bundle introduced after phase 3.");
            AssertContains(engine, "struct FileAttemptState", "HashEngine.cpp does not yet expose the grouped file-attempt state bundle introduced after phase 3.");
            AssertContains(engine, "struct FileHashContexts", "HashEngine.cpp does not yet expose the grouped file-hash context bundle introduced after phase 3.");
            AssertContains(engine, "struct FileExecutionState", "HashEngine.cpp does not yet expose the grouped file-execution state bundle introduced after phase 3.");
            AssertContains(engine, "static void InitializeFileHashing(", "HashEngine.cpp does not yet expose a tiny file-hashing initializer helper.");
            AssertContains(engine, "static void YieldHashThread()", "HashEngine.cpp does not yet expose a tiny thread-yield helper.");
            AssertContains(engine, "static uint64_t CalculateFileChunkIterations(", "HashEngine.cpp does not yet expose a tiny chunk-iteration helper.");
            AssertContains(engine, "static void UpdateWholeProgressAfterFile(", "HashEngine.cpp does not yet expose a tiny whole-progress helper.");
            AssertContains(engine, "static void PopulateDigestResult(", "HashEngine.cpp does not yet expose a tiny digest-population helper.");
            AssertContains(engine, "typedef ResultDigestStorage FinalizedDigestBundle;", "HashEngine.cpp does not yet expose the finalized digest bundle introduced after phase 3.");
            AssertContains(engine, "static const tstring& GetFinalizedDigestValue(", "HashEngine.cpp does not yet expose the finalized-digest getter introduced after phase 3.");
            AssertContains(engine, "static void SetFinalizedDigestValue(", "HashEngine.cpp does not yet expose the finalized-digest setter introduced after phase 3.");
            AssertContains(engine, "static void FinalizeDigestStrings(", "HashEngine.cpp does not yet expose a tiny digest-finalization helper.");
            AssertContains(engine, "static void CompleteSuccessfulFileHashing(", "HashEngine.cpp does not yet expose a tiny successful-file completion helper.");
            AssertContains(engine, "static void CompleteOpenedFileAttempt(", "HashEngine.cpp does not yet expose a tiny opened-file completion helper.");
            AssertContains(engine, "static void CompleteFileAttempt(", "HashEngine.cpp does not yet expose a tiny file-attempt completion helper.");
            AssertContains(engine, "static void EmitOpenFileError(", "HashEngine.cpp does not yet expose a tiny open-file error helper.");
            AssertContains(engine, "static void EmitReadFileError(", "HashEngine.cpp does not yet expose a tiny read-file error helper.");
            AssertContains(engine, "static void FinishFileProcessing(", "HashEngine.cpp does not yet expose a tiny file-finished helper.");
            AssertContains(engine, "static void EmitPathResult(", "HashEngine.cpp does not yet expose a tiny path-result helper.");
            AssertContains(engine, "static void EmitMetaResult(", "HashEngine.cpp does not yet expose a tiny meta-result helper.");
            AssertContains(engine, "static void EmitHashResult(", "HashEngine.cpp does not yet expose a tiny hash-result helper.");
            AssertContains(engine, "static void EmitErrorResult(", "HashEngine.cpp does not yet expose a tiny error-result helper.");
            AssertContains(engine, "static void EmitErrorMessageResult(", "HashEngine.cpp does not yet expose a tiny error-message helper.");
            AssertContains(engine, "static int CancelHashing(", "HashEngine.cpp does not yet expose a tiny cancellation helper.");
            AssertContains(engine, "static int CompleteHashing(", "HashEngine.cpp does not yet expose a tiny completion helper.");
            AssertContains(engine, "ThreadPool threadPool(5);", "HashEngine no longer uses the current fixed-size thread pool in the baseline implementation.");
            AssertContains(engine, "if (GetThreadDataFileCount(*thrdData) < 200)", "HashEngine no longer performs the current small-batch pre-scan in the baseline implementation.");
            AssertContains(engine, "VisitThreadDataInputFiles(*thrdData, [&](uint32_t fileIndex, const tstring& fullPath)", "HashEngine no longer routes input-file iteration through the ThreadDataAccess visitor seam.");
            AssertContains(engine, "AccumulatePreScannedFileSize(thrdData, fSizes, fileIndex);", "HashEngine no longer routes the small-batch pre-scan loop body through the tiny helper.");
            AssertContains(engine, "bool wasCancelled = false;", "HashEngine no longer tracks small-batch pre-scan cancellation through the local helper contract.");
            AssertContains(engine, "isSizeCaled = PrepareHashingWork(thrdData, observer, fSizes, &wasCancelled);", "HashEngine no longer routes the preparation phase through the tiny helper.");
            AssertContains(engine, "if (wasCancelled)", "HashEngine no longer handles small-batch pre-scan cancellation via the helper result.");
            AssertContains(engine, "YieldHashThread();", "HashEngine no longer routes per-file scheduler yielding through the tiny helper.");
            AssertContains(engine, "future<void> taskSHA512Update", "HashEngine no longer fans out SHA512 updates in the baseline implementation.");
            AssertContains(engine, "future<void> taskSHA256Update", "HashEngine no longer fans out SHA256 updates in the baseline implementation.");
            AssertContains(engine, "future<void> taskSHA1Update", "HashEngine no longer fans out SHA1 updates in the baseline implementation.");
            AssertContains(engine, "future<void> taskMD5Update", "HashEngine no longer fans out MD5 updates in the baseline implementation.");
            AssertContains(engine, "FileExecutionState executionState = { 0 };", "HashEngine no longer creates the grouped file-execution state bundle.");
            AssertContains(engine, "ResultData& result = BeginFileHashAttempt(thrdData, observer, fullPath, &executionState, &path);", "HashEngine no longer routes the file-attempt setup through the grouped file-execution helper.");
            AssertContains(engine, "InitializeFileAttemptState(path, &osFile, &executionState.fileAttemptState);", "HashEngine no longer routes file-attempt state initialization through the grouped execution helper.");
            AssertContains(engine, "OpenFileForHashing(&executionState.fileAttemptState, (void *)&fExc);", "HashEngine no longer routes the file-open attempt through the grouped execution helper.");
            AssertContains(engine, "bool wasStopped = ProcessOpenedFileHashing(thrdData, observer, result, fileIndex, isSizeCaled, fSizes, &executionState", "HashEngine no longer routes the opened-file processing loop through the grouped execution helper.");
            AssertContains(engine, "CompleteFileAttempt(observer, thrdData, result, fileIndex, isSizeCaled, executionState);", "HashEngine no longer routes the file-attempt terminal path through the grouped execution bundle.");
            AssertContains(engine, "EmitReadFileError(observer, result);", "HashEngine no longer routes read-file failures through the tiny helper.");
            AssertContains(engine, "FinishFileProcessing(observer);", "HashEngine no longer routes file-finished callbacks through the tiny helper.");
            AssertContains(engine, "EmitErrorResult(observer, result);", "HashEngine no longer emits errors through the tiny error-result helper in the baseline implementation.");
            AssertContains(engine, "SetResultError(result, errorText);", "HashEngine error-message helper no longer writes the error text before emitting.");
            AssertContains(engine, "EmitErrorMessageResult(observer, result,", "HashEngine no longer routes error-text emission through the tiny error-message helper.");
            AssertContains(engine, "return CancelHashing(thrdData, observer);", "HashEngine no longer routes cancellation exits through the tiny cancellation helper.");
            AssertContains(engine, "return CompleteHashing(thrdData, observer);", "HashEngine no longer routes the successful exit through the tiny completion helper.");
            AssertInOrder(engine,
                [
                    "static bool ProcessOpenedFileHashing(",
                    "InitializeFileHashing(",
                    "uint64_t fsize = PrepareFileMetaResult(",
                    "uint64_t times = CalculateFileChunkIterations(fsize);",
                    "do",
                    "while (!isFileFinished && !executionState->fileAttemptState.readFailed);",
                    "return false;"
                ],
                "HashEngine opened-file processing helper no longer preserves the expected read-loop order.");
            AssertInOrder(engine,
                [
                    "static ResultData& BeginFileHashAttempt(",
                    "YieldHashThread();",
                    "ResetFileProgressState(&executionState->progressState);",
                    "ResultData& result = BeginFileResult(thrdData, observer, path);",
                    "*resultPath = GetResultPath(result).c_str();",
                    "return result;"
                ],
                "HashEngine file-attempt-begin helper no longer preserves the expected setup order.");
            AssertInOrder(engine,
                [
                    "static void ResetFileProgressState(",
                    "progressState->finishedSize = 0;",
                    "progressState->position = 0;"
                ],
                "HashEngine file-progress reset helper no longer preserves the expected reset order.");
            AssertInOrder(engine,
                [
                    "struct FileAttemptState",
                    "const TCHAR *path;",
                    "OsFile *osFile;",
                    "tstring fileVersion;",
                    "bool readFailed;",
                    "bool isFileOpened;",
                    "const TCHAR *openErrorText;"
                ],
                "HashEngine grouped file-attempt state bundle no longer keeps the current per-file attempt state together.");
            AssertInOrder(engine,
                [
                    "static void InitializeFileAttemptState(",
                    "fileAttemptState->path = path;",
                    "fileAttemptState->osFile = osFile;",
                    "fileAttemptState->fileVersion.clear();",
                    "fileAttemptState->readFailed = false;",
                    "fileAttemptState->isFileOpened = false;",
                    "fileAttemptState->openErrorText = NULL;"
                ],
                "HashEngine file-attempt state initializer no longer preserves the expected setup order.");
            AssertInOrder(engine,
                [
                    "static uint64_t CalculateFileChunkIterations(",
                    "return fsize / DataBuffer::preflen + 1;"
                ],
                "HashEngine chunk-iteration helper no longer preserves the expected calculation.");
            AssertInOrder(engine,
                [
                    "static bool OpenFileForHashing(",
                    "fileAttemptState->readFailed = false;",
                    "fileAttemptState->openErrorText = (const TCHAR *)openErrorBuffer;",
                    "fileAttemptState->isFileOpened = fileAttemptState->osFile->openReadScan(openErrorBuffer);",
                    "return fileAttemptState->isFileOpened;"
                ],
                "HashEngine file-open helper no longer preserves the expected open-and-reset order.");
            AssertInOrder(engine,
                [
                    "static bool PrepareHashingWork(",
                    "observer->onPreparing();",
                    "bool isSizeCaled = TryPreScanSmallBatchFileSizes(thrdData, fSizes, wasCancelled);",
                    "if (*wasCancelled)",
                    "observer->onPreparationFinished();",
                    "return isSizeCaled;"
                ],
                "HashEngine preparation helper no longer preserves the expected preparation order.");
            AssertInOrder(engine,
                [
                    "static bool TryPreScanSmallBatchFileSizes(",
                    "if (GetThreadDataFileCount(*thrdData) < 200)",
                    "*wasCancelled = true;",
                    "AccumulatePreScannedFileSize(thrdData, fSizes, fileIndex);",
                    "return true;"
                ],
                "HashEngine small-batch pre-scan helper no longer preserves the expected control flow.");
            AssertInOrder(engine,
                [
                    "static void AccumulatePreScannedFileSize(",
                    "OsFile osFile(path);",
                    "fSizes[fileIndex] = fSize;",
                    "AddThreadDataTotalSize(*thrdData, fSize);"
                ],
                "HashEngine pre-scan helper no longer preserves the expected size-accumulation order.");
            AssertInOrder(engine,
                [
                    "static uint64_t PrepareFileMetaResult(",
                    "SetResultModifiedDate(result, osFile.getModifiedTimeFormat());",
                    "EmitMetaResult(observer, result);",
                    "return fsize;"
                ],
                "HashEngine file-meta helper no longer preserves the expected metadata-emission order.");
            AssertInOrder(engine,
                [
                    "static int CompleteHashing(",
                    "observer->onCompleted();",
                    "SetThreadDataWorking(*thrdData, false);",
                    "return 0;"
                ],
                "HashEngine completion helper no longer preserves the expected completion order.");
            AssertDoesNotContain(engine, "result.enumState = RESULT_PATH;\r\n\r\n\t\tobserver->onFileStarted(result);", "HashEngine still inlines the path-result state transition instead of using the helper.");
            AssertDoesNotContain(engine, "YieldHashThread();\r\n\r\n\t\t// Declaration for calculator\r\n\t\tconst TCHAR *path;\r\n\t\tuint64_t fsize, times;\r\n\t\tint position;\r\n\t\tResetFileProgressState(&finishedSize, &position);\r\n\r\n\t\tMD5_CTX mdContext; // MD5 context", "HashEngine still inlines the file-attempt setup sequence instead of using the helper.");
            AssertDoesNotContain(engine, "uint64_t fSize = 0;\r\n\r\n\t\t\tconst TCHAR *path;\r\n\t\t\tpath = thrdData->fullPaths[i].c_str();\r\n\t\t\tOsFile osFile(path);\r\n\t\t\tif (osFile.openRead())\r\n\t\t\t{\r\n\t\t\t\tfSize = osFile.getLength();//fsize=status.m_size; // Fix 4GB file\r\n\t\t\t\tosFile.close();\r\n\t\t\t}\r\n\r\n\t\t\tfSizes[i] = fSize;\r\n\t\t\tthrdData->totalSize += fSize;", "HashEngine still inlines the small-batch pre-scan loop body instead of using the helper.");
            AssertDoesNotContain(engine, "if (thrdData->nFiles < 200) // not too many\r\n\t{\r\n\t\tisSizeCaled = true;\r\n\t\tfor (i = 0; i < (thrdData->nFiles); i++)\r\n\t\t{\r\n\t\t\tif (thrdData->stop)\r\n\t\t\t{\r\n\t\t\t\treturn CancelHashing(thrdData, observer);\r\n\t\t\t}\r\n\t\t\tAccumulatePreScannedFileSize(thrdData, fSizes, i);\r\n\t\t}\r\n\t}", "HashEngine still inlines the small-batch pre-scan control flow instead of using the helper.");
            AssertDoesNotContain(engine, "finishedSize = 0;\r\n\r\n\t\tResultData& result = BeginFileResult(thrdData, observer, i);\r\n\t\tpath = result.tstrPath.c_str();\r\n\r\n\t\tint position = 0;", "HashEngine still inlines the per-file progress reset instead of using the helper.");
            AssertDoesNotContain(engine, "InitializeFileHashing(observer, &mdContext, &sha1, &sha256Ctx, &sha512Ctx);\r\n\r\n\t\t\t/*", "HashEngine still inlines the opened-file processing prelude instead of using the helper.");
            AssertDoesNotContain(engine, "MD5Init(&mdContext, 0); // MD5 init\r\n\t\t\tsha1.Reset(); // SHA1 init\r\n\t\t\tsha256_init(&sha256Ctx); // SHA256 init\r\n\t\t\tSHA512_Init(&sha512Ctx); // SHA512 init\r\n\r\n\t\t\tobserver->onFileProgress(0);", "HashEngine still inlines the file hashing initialization instead of using the helper.");
            AssertDoesNotContain(engine, "tstring tstrLastModifiedTime = osFile.getModifiedTimeFormat();\r\n\t\t\tresult.tstrMDate = tstrLastModifiedTime;", "HashEngine still inlines the file modified-time extraction instead of using the helper.");
            AssertDoesNotContain(engine, "times = fsize / DataBuffer::preflen + 1;", "HashEngine still inlines the per-file chunk-count calculation instead of using the helper.");
            AssertDoesNotContain(engine, "if (!isSizeCaled)\r\n\t\t\t\t{\r\n\t\t\t\t\tif (thrdData->nFiles == 0)\r\n\t\t\t\t\t{\r\n\t\t\t\t\t\tobserver->onTotalProgress(0);\r\n\t\t\t\t\t}\r\n\t\t\t\t\telse\r\n\t\t\t\t\t{\r\n\t\t\t\t\t\tint progressMax = observer->progressMax();\r\n\t\t\t\t\t\tobserver->onTotalProgress((i + 1) * progressMax / (thrdData->nFiles));\r\n\t\t\t\t\t}\r\n\t\t\t\t}", "HashEngine still inlines the whole-progress update instead of using the helper.");
            AssertDoesNotContain(engine, "result.tstrMD5 = tstrFileMD5;\r\n\t\t\t\tresult.tstrSHA1 = tstrFileSHA1;\r\n\t\t\t\tresult.tstrSHA256 = tstrFileSHA256;\r\n\t\t\t\tresult.tstrSHA512 = tstrFileSHA512;", "HashEngine still inlines the digest-field assignment instead of using the helper.");
            AssertDoesNotContain(engine, "MD5Final(&mdContext); // MD5 final\r\n\t\t\t\tsha1.Final(); // SHA1 final\r\n\t\t\t\tsha256_final(&sha256Ctx); // SHA256 final\r\n\t\t\t\tSHA512_Final(digestSHA512, &sha512Ctx); // SHA256 final", "HashEngine still inlines digest finalization instead of using the helper.");
            AssertDoesNotContain(engine, "observer->onFileCalculated();\r\n\r\n\t\t\t\tFinalizeDigestStrings(&mdContext, &sha1, &sha256Ctx, &sha512Ctx, digestSHA512, tstrFileMD5, tstrFileSHA1, tstrFileSHA256, tstrFileSHA512);\r\n\r\n\t\t\t\tUpdateWholeProgressAfterFile(observer, thrdData, isSizeCaled, i);\r\n\r\n\t\t\t\tosFile.close();\r\n\t\t\t\t//Calculating ends\r\n\r\n\t\t\t\tPopulateDigestResult(result, tstrFileMD5, tstrFileSHA1, tstrFileSHA256, tstrFileSHA512);\r\n\r\n\t\t\t\tEmitHashResult(observer, result, thrdData->uppercase);", "HashEngine still inlines the successful file-completion flow instead of using the helper.");
            AssertDoesNotContain(engine, "EmitErrorMessageResult(observer, result, fExc);\r\n#else\r\n\t\t\tEmitErrorMessageResult(observer, result, strtotstr(string(fExc)));\r\n#endif", "HashEngine still inlines the platform-split open-file error emission instead of using the helper.");
            AssertDoesNotContain(engine, "result.enumState = RESULT_META;\r\n\r\n\t\t\tobserver->onFileMetaReady(result);", "HashEngine still inlines the meta-result state transition instead of using the helper.");
            AssertInOrder(engine,
                [
                    "static void CompleteSuccessfulFileHashing(",
                    "observer->onFileCalculated();",
                    "FinalizeDigestStrings(",
                    "UpdateWholeProgressAfterFile(",
                    "fileAttemptState.osFile->close();",
                    "PopulateDigestResult(",
                    "EmitHashResult("
                ],
                "HashEngine successful-file helper no longer preserves the expected completion order.");
            AssertInOrder(engine,
                [
                    "struct FileProgressState",
                    "uint64_t finishedSize;",
                    "uint64_t finishedSizeWhole;",
                    "int position;",
                    "int positionWhole;"
                ],
                "HashEngine grouped file-progress state bundle no longer keeps the current progress counters together.");
            AssertInOrder(engine,
                [
                    "struct FileHashContexts",
                    "MD5_CTX mdContext;",
                    "CSHA1 sha1;",
                    "SHA256_CTX sha256Ctx;",
                    "SHA512_CTX sha512Ctx;",
                    "uint8_t digestSHA512[SHA512_DIGEST_LENGTH];"
                ],
                "HashEngine grouped file-hash context bundle no longer keeps the current algorithm contexts together.");
            AssertInOrder(engine,
                [
                    "struct FileExecutionState",
                    "FileProgressState progressState;",
                    "FileAttemptState fileAttemptState;",
                    "FileHashContexts hashContexts;",
                    "FinalizedDigestBundle digestBundle;"
                ],
                "HashEngine grouped file-execution state bundle no longer keeps the current file-level execution state together.");
            AssertContains(engine, "typedef ResultDigestStorage FinalizedDigestBundle;", "HashEngine finalized digest bundle no longer reuses the centralized digest storage shape.");
            AssertInOrder(engine,
                [
                    "static void CompleteOpenedFileAttempt(",
                    "if (executionState.fileAttemptState.readFailed)",
                    "EmitReadFileError(observer, result);",
                    "CompleteSuccessfulFileHashing(",
                    "FinishFileProcessing(observer);"
                ],
                "HashEngine opened-file completion helper no longer preserves the expected branching order.");
            AssertInOrder(engine,
                [
                    "static void CompleteOpenedFileAttempt(",
                    "CompleteSuccessfulFileHashing(observer, thrdData, result, fileIndex, isSizeCaled, GetThreadDataUppercase(*thrdData), executionState);",
                    "FinishFileProcessing(observer);"
                ],
                "HashEngine no longer routes the successful opened-file path through the tiny helper.");
            AssertInOrder(engine,
                [
                    "static void CompleteFileAttempt(",
                    "if (executionState.fileAttemptState.isFileOpened)",
                    "CompleteOpenedFileAttempt(",
                    "EmitOpenFileError(observer, result, executionState.fileAttemptState.openErrorText);",
                    "FinishFileProcessing(observer);"
                ],
                "HashEngine file-attempt helper no longer preserves the expected branching order.");
            AssertInOrder(engine,
                [
                    "static void EmitOpenFileError(",
                    "EmitErrorMessageResult(observer, result, tstring(errorText));"
                ],
                "HashEngine open-file error helper no longer preserves the expected emission order.");
            AssertInOrder(engine,
                [
                    "static void EmitReadFileError(",
                    "EmitErrorMessageResult(observer, result, strtotstr(string(\"Failed to read file while hashing.\")));"
                ],
                "HashEngine read-file error helper no longer preserves the expected emission order.");
            AssertInOrder(engine,
                [
                    "static void FinishFileProcessing(",
                    "observer->onFileFinished();"
                ],
                "HashEngine file-finished helper no longer preserves the expected emission order.");
            AssertDoesNotContain(engine, "result.enumState = RESULT_ALL;\r\n\r\n\t\t\t\tobserver->onFileHashReady(result, thrdData->uppercase);", "HashEngine still inlines the hash-result state transition instead of using the helper.");
            AssertDoesNotContain(engine, "thrdData->threadWorking = false;\r\n\r\n\t\t\t\tobserver->onCancelled();\r\n\t\t\t\treturn 0;", "HashEngine still inlines the final stop-and-cancel path instead of using the helper.");
            AssertDoesNotContain(engine, "observer->onCompleted();\r\n\r\n\tthrdData->threadWorking = false;\r\n\r\n\treturn 0;", "HashEngine still inlines the successful completion path instead of using the helper.");

            AssertInOrder(engine,
                [
                    "observer->onPreparing();",
                    "bool wasCancelled = false;",
                    "isSizeCaled = PrepareHashingWork(thrdData, observer, fSizes, &wasCancelled);",
                    "if (wasCancelled)",
                    "FileExecutionState executionState = { 0 };",
                    "bool completedAllFiles = VisitThreadDataInputFiles(*thrdData, [&](uint32_t fileIndex, const tstring& fullPath)",
                    "ResultData& result = BeginFileHashAttempt(thrdData, observer, fullPath, &executionState, &path);",
                    "InitializeFileAttemptState(path, &osFile, &executionState.fileAttemptState);",
                    "OpenFileForHashing(&executionState.fileAttemptState, (void *)&fExc);",
                    "if (executionState.fileAttemptState.isFileOpened)",
                    "bool wasStopped = ProcessOpenedFileHashing(thrdData, observer, result, fileIndex, isSizeCaled, fSizes, &executionState",
                    "if (wasStopped)",
                    "CompleteFileAttempt(observer, thrdData, result, fileIndex, isSizeCaled, executionState);",
                    "return CompleteHashing(thrdData, observer);"
                ],
                "HashEngine lifecycle callbacks no longer follow the current baseline order.");
        }, failures);

        Run("Phase 1 assignment chains write observers through the neutral ThreadData field name", () =>
        {
            string mfcDialog = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string clrBridge = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpBridge = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");

            AssertContains(mfcDialog, "SetThreadDataObserver(m_thrdData, m_uiBridgeMFC);", "MFC dialog no longer wires ThreadData through the neutral observer field.");
            AssertDoesNotContain(mfcDialog, "m_thrdData.uiBridge =", "MFC dialog still writes the legacy uiBridge field.");

            AssertContains(clrBridge, "SetThreadDataObserver(*m_pThreadData, m_pUiBridgeWUI);", "CLR bridge no longer wires ThreadData through the neutral observer field.");
            AssertDoesNotContain(clrBridge, "m_pThreadData->uiBridge =", "CLR bridge still writes the legacy uiBridge field.");

            AssertContains(uwpBridge, "SetThreadDataObserver(m_threadData, m_spUiBridgeUwp.get());", "UWP bridge no longer wires ThreadData through the neutral observer field.");
            AssertDoesNotContain(uwpBridge, "m_threadData.uiBridge =", "UWP bridge still writes the legacy uiBridge field.");
        }, failures);

        Run("Phase 5 routes managed ThreadData lifecycle through dedicated ThreadDataAccess helpers", () =>
        {
            string threadDataAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataAccess.h");
            string clrBridge = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpBridge = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");
            string mfcDialog = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");

            AssertContains(threadDataAccess, "SetThreadDataObserver(ThreadData& threadData, HashEngineObserver *observer)", "ThreadDataAccess does not yet expose the observer-assignment helper.");
            AssertContains(threadDataAccess, "GetThreadDataObserver(const ThreadData& threadData)", "ThreadDataAccess does not yet expose the observer getter helper.");
            AssertContains(threadDataAccess, "GetThreadDataInputState(const ThreadData& threadData)", "ThreadDataAccess does not yet expose the grouped input-state getter helper.");
            AssertContains(threadDataAccess, "GetMutableThreadDataInputState(ThreadData& threadData)", "ThreadDataAccess does not yet expose the grouped mutable input-state helper.");
            AssertContains(threadDataAccess, "GetThreadDataExecutionState(const ThreadData& threadData)", "ThreadDataAccess does not yet expose the grouped execution-state getter helper.");
            AssertContains(threadDataAccess, "GetMutableThreadDataExecutionState(ThreadData& threadData)", "ThreadDataAccess does not yet expose the grouped mutable execution-state helper.");
            AssertContains(threadDataAccess, "GetThreadDataInputFiles(const ThreadData& threadData)", "ThreadDataAccess does not yet expose the grouped input-files getter helper.");
            AssertContains(threadDataAccess, "GetMutableThreadDataInputFiles(ThreadData& threadData)", "ThreadDataAccess does not yet expose the grouped mutable input-files helper.");
            AssertContains(threadDataAccess, "GetMutableThreadDataResults(ThreadData& threadData)", "ThreadDataAccess does not yet expose the grouped mutable result-list helper.");
            AssertContains(threadDataAccess, "ResetThreadDataInputFiles(ThreadData& threadData)", "ThreadDataAccess does not yet expose the grouped input-file reset helper.");
            AssertContains(threadDataAccess, "SetThreadDataFileCount(ThreadData& threadData, uint32_t fileCount)", "ThreadDataAccess does not yet expose the file-count helper.");
            AssertContains(threadDataAccess, "AddThreadDataFullPath(ThreadData& threadData, const sunjwbase::tstring& fullPath)", "ThreadDataAccess does not yet expose the full-path append helper.");
            AssertContains(threadDataAccess, "AppendThreadDataInputFile(ThreadData& threadData, const sunjwbase::tstring& fullPath)", "ThreadDataAccess does not yet expose the grouped input-file append helper.");
            AssertContains(threadDataAccess, "ResetThreadDataInputFilesAndAppend(ThreadData& threadData, uint32_t fileCount, TInputFileFactory inputFileFactory)", "ThreadDataAccess does not yet expose the grouped batch input-file append helper.");
            AssertContains(threadDataAccess, "AppendTrimmedThreadDataInputFile(ThreadData& threadData, const sunjwbase::tstring& fullPath)", "ThreadDataAccess does not yet expose the trimmed input-file append helper.");
            AssertContains(threadDataAccess, "AppendThreadDataInputFiles(ThreadData& threadData, const TStrVector& fullPaths)", "ThreadDataAccess does not yet expose the grouped input-file list append helper.");
            AssertContains(threadDataAccess, "ReplaceThreadDataInputFiles(ThreadData& threadData, const TStrVector& fullPaths)", "ThreadDataAccess does not yet expose the grouped input-file replacement helper.");
            AssertContains(threadDataAccess, "ReplaceTrimmedThreadDataInputFiles(ThreadData& threadData, const TStrVector& fullPaths)", "ThreadDataAccess does not yet expose the grouped trimmed input-file replacement helper.");
            AssertContains(threadDataAccess, "AppendThreadDataResult(ThreadData& threadData)", "ThreadDataAccess does not yet expose the result-append helper.");
            AssertContains(threadDataAccess, "ClearThreadDataResults(ThreadData& threadData)", "ThreadDataAccess does not yet expose the grouped result-clear helper.");
            AssertContains(threadDataAccess, "ResetThreadDataForNewSession(ThreadData& threadData)", "ThreadDataAccess does not yet expose the grouped reset helper.");
            AssertContains(threadDataAccess, "SetThreadDataStop(ThreadData& threadData, bool stopValue)", "ThreadDataAccess does not yet expose the stop-flag helper.");
            AssertContains(threadDataAccess, "SetThreadDataWorking(ThreadData& threadData, bool working)", "ThreadDataAccess does not yet expose the working-state setter helper.");
            AssertContains(threadDataAccess, "IsThreadDataWorking(const ThreadData& threadData)", "ThreadDataAccess does not yet expose the working-state getter helper.");
            AssertContains(threadDataAccess, "SetThreadDataUppercase(ThreadData& threadData, bool uppercase)", "ThreadDataAccess does not yet expose the uppercase helper.");
            AssertContains(threadDataAccess, "GetThreadDataUppercase(const ThreadData& threadData)", "ThreadDataAccess does not yet expose the uppercase getter helper.");
            AssertContains(threadDataAccess, "GetThreadDataTotalSize(const ThreadData& threadData)", "ThreadDataAccess does not yet expose the total-size helper.");
            AssertContains(threadDataAccess, "ResetThreadDataTotalSize(ThreadData& threadData)", "ThreadDataAccess does not yet expose the total-size reset helper.");
            AssertContains(threadDataAccess, "AddThreadDataTotalSize(ThreadData& threadData, uint64_t sizeDelta)", "ThreadDataAccess does not yet expose the total-size increment helper.");
            AssertContains(threadDataAccess, "ReplaceThreadDataCountedFileSize(ThreadData& threadData, uint64_t previousSize, uint64_t currentSize)", "ThreadDataAccess does not yet expose the counted-file-size replacement helper.");
            AssertContains(threadDataAccess, "GetThreadDataFileCount(const ThreadData& threadData)", "ThreadDataAccess does not yet expose the file-count getter helper.");
            AssertContains(threadDataAccess, "GetThreadDataResultCount(const ThreadData& threadData)", "ThreadDataAccess does not yet expose the result-count helper.");
            AssertContains(threadDataAccess, "HasThreadDataInputFiles(const ThreadData& threadData)", "ThreadDataAccess does not yet expose the input-file presence helper.");
            AssertContains(threadDataAccess, "ShouldStopThreadData(const ThreadData& threadData)", "ThreadDataAccess does not yet expose the stop-flag getter helper.");
            AssertContains(threadDataAccess, "GetThreadDataFullPath(const ThreadData& threadData, uint32_t fileIndex)", "ThreadDataAccess does not yet expose the grouped path getter helper.");
            AssertContains(threadDataAccess, "GetThreadDataResults(const ThreadData& threadData)", "ThreadDataAccess does not yet expose the grouped result-list getter.");
            AssertContains(threadDataAccess, "VisitThreadDataResults(const ThreadData& threadData, TResultVisitor visitor)", "ThreadDataAccess does not yet expose the grouped result-list visitor.");
            AssertContains(threadDataAccess, "AddThreadDataFullPath(threadData, fullPath);", "ThreadDataAccess grouped input-file append helper does not yet reuse the full-path seam.");
            AssertContains(threadDataAccess, "ResetThreadDataInputFiles(threadData);", "ThreadDataAccess grouped batch input-file append helper does not yet reuse the input reset seam.");
            AssertContains(threadDataAccess, "SetThreadDataFileCount(threadData, fileCount);", "ThreadDataAccess grouped batch input-file append helper does not yet reuse the file-count seam.");
            AssertContains(threadDataAccess, "AddThreadDataFullPath(threadData, inputFileFactory(fileIndex));", "ThreadDataAccess grouped batch input-file append helper does not yet reuse the full-path append seam.");
            AssertContains(threadDataAccess, "sunjwbase::tstring trimmedPath = sunjwbase::strtrim(fullPath);", "ThreadDataAccess trimmed input-file append helper does not yet normalize text through the shared trim seam.");
            AssertContains(threadDataAccess, "AppendThreadDataInputFile(threadData, trimmedPath);", "ThreadDataAccess trimmed input-file append helper does not yet reuse the grouped input-file append seam.");
            AssertContains(threadDataAccess, "AppendThreadDataInputFile(threadData, *itr);", "ThreadDataAccess grouped input-file list append helper does not yet reuse the grouped single-file append seam.");
            AssertContains(threadDataAccess, "AppendThreadDataInputFiles(threadData, fullPaths);", "ThreadDataAccess grouped input-file replacement helper does not yet reuse the grouped list append seam.");
            AssertContains(threadDataAccess, "AppendTrimmedThreadDataInputFile(threadData, *itr);", "ThreadDataAccess grouped trimmed input-file replacement helper does not yet reuse the trimmed single-file append seam.");
            AssertContains(threadDataAccess, "ClearThreadDataResults(threadData);", "ThreadDataAccess grouped reset helper does not yet reuse the result-clear seam.");
            AssertContains(threadDataAccess, "ResetThreadDataInputFiles(threadData);", "ThreadDataAccess grouped reset helper does not yet reuse the input-file reset seam.");
            AssertContains(threadDataAccess, "return threadData.inputState;", "ThreadDataAccess grouped input-state getter does not yet route through the neutral ThreadData field name.");
            AssertContains(threadDataAccess, "return threadData.executionState;", "ThreadDataAccess grouped execution-state getter does not yet route through the neutral ThreadData field name.");
            AssertContains(threadDataAccess, "return GetThreadDataInputState(threadData).inputFiles;", "ThreadDataAccess grouped input-files getter does not yet route through the grouped input-state seam.");
            AssertContains(threadDataAccess, "return GetMutableThreadDataInputState(threadData).inputFiles;", "ThreadDataAccess grouped mutable input-files helper does not yet route through the grouped input-state seam.");
            AssertContains(threadDataAccess, "return GetMutableThreadDataExecutionState(threadData).results;", "ThreadDataAccess grouped mutable result-list helper does not yet route through the grouped execution-state seam.");
            AssertContains(threadDataAccess, "GetMutableThreadDataExecutionState(threadData).working = working;", "ThreadDataAccess working-state setter does not yet route through the neutral ThreadData field name.");
            AssertContains(threadDataAccess, "return GetThreadDataExecutionState(threadData).working;", "ThreadDataAccess working-state getter does not yet route through the neutral ThreadData field name.");
            AssertContains(threadDataAccess, "GetMutableThreadDataExecutionState(threadData).stopRequested = stopValue;", "ThreadDataAccess stop setter does not yet route through the neutral ThreadData field name.");
            AssertContains(threadDataAccess, "return GetThreadDataExecutionState(threadData).stopRequested;", "ThreadDataAccess stop getter does not yet route through the neutral ThreadData field name.");
            AssertContains(threadDataAccess, "GetMutableThreadDataExecutionState(threadData).uppercaseDigest = uppercase;", "ThreadDataAccess uppercase setter does not yet route through the neutral ThreadData field name.");
            AssertContains(threadDataAccess, "return GetThreadDataExecutionState(threadData).uppercaseDigest;", "ThreadDataAccess uppercase getter does not yet route through the neutral ThreadData field name.");
            AssertContains(threadDataAccess, "GetMutableThreadDataExecutionState(threadData).countedSize += sizeDelta;", "ThreadDataAccess total-size increment helper does not yet route through the neutral ThreadData field name.");
            AssertContains(threadDataAccess, "return GetThreadDataInputState(threadData).fileCount;", "ThreadDataAccess file-count getter does not yet route through the grouped input-state seam.");
            AssertContains(threadDataAccess, "return GetThreadDataInputFiles(threadData)[fileIndex];", "ThreadDataAccess grouped path getter does not yet route through the neutral ThreadData field name.");
            AssertContains(threadDataAccess, "return GetThreadDataExecutionState(threadData).results;", "ThreadDataAccess grouped result-list getter does not yet route through the neutral ThreadData field name.");

            AssertContains(clrBridge, "#include \"Common/ThreadDataAccess.h\"", "CLR bridge does not yet consume the ThreadDataAccess seam.");
            AssertContains(clrBridge, "SetThreadDataObserver(*m_pThreadData, m_pUiBridgeWUI);", "CLR bridge does not yet route observer assignment through ThreadDataAccess.");
            AssertContains(clrBridge, "ResetThreadDataForNewSession(*m_pThreadData);", "CLR bridge does not yet route Clear() through ThreadDataAccess.");
            AssertContains(clrBridge, "SetThreadDataStop(*m_pThreadData, val);", "CLR bridge does not yet route SetStop() through ThreadDataAccess.");
            AssertContains(clrBridge, "SetThreadDataUppercase(*m_pThreadData, val);", "CLR bridge does not yet route SetUppercase() through ThreadDataAccess.");
            AssertContains(clrBridge, "GetThreadDataTotalSize(*m_pThreadData);", "CLR bridge does not yet route GetTotalSize() through ThreadDataAccess.");
            AssertContains(clrBridge, "GetThreadDataResultCount(*m_pThreadData);", "CLR bridge does not yet route GetResultCount() through ThreadDataAccess.");
            AssertContains(clrBridge, "ReplaceThreadDataInputFiles(*m_pThreadData, ConvertSystemStringArrayToTStrVector(filePaths));", "CLR bridge does not yet route AddFiles() through the current compile-safe ThreadDataAccess batch-input helper.");

            AssertContains(uwpBridge, "#include \"Common/ThreadDataAccess.h\"", "UWP bridge does not yet consume the ThreadDataAccess seam.");
            AssertContains(uwpBridge, "SetThreadDataObserver(m_threadData, m_spUiBridgeUwp.get());", "UWP bridge does not yet route observer assignment through ThreadDataAccess.");
            AssertContains(uwpBridge, "ResetThreadDataForNewSession(m_threadData);", "UWP bridge does not yet route Clear() through ThreadDataAccess.");
            AssertContains(uwpBridge, "SetThreadDataStop(m_threadData, val);", "UWP bridge does not yet route SetStop() through ThreadDataAccess.");
            AssertContains(uwpBridge, "SetThreadDataUppercase(m_threadData, val);", "UWP bridge does not yet route SetUppercase() through ThreadDataAccess.");
            AssertContains(uwpBridge, "GetThreadDataTotalSize(m_threadData);", "UWP bridge does not yet route GetTotalSize() through ThreadDataAccess.");
            AssertContains(uwpBridge, "ResetThreadDataInputFilesAndAppend(m_threadData, filePaths->Length, [&](uint32_t fileIndex)", "UWP bridge does not yet route AddFiles() through the grouped ThreadDataAccess batch-input helper.");

            AssertContains(mfcDialog, "#include \"Common/ThreadDataAccess.h\"", "MFC dialog does not yet consume the ThreadDataAccess seam.");
            AssertContains(mfcDialog, "SetThreadDataObserver(m_thrdData, m_uiBridgeMFC);", "MFC dialog does not yet route observer assignment through ThreadDataAccess.");
            AssertContains(mfcDialog, "ResetThreadDataForNewSession(m_thrdData);", "MFC dialog does not yet route dialog initialization through ThreadDataAccess.");
            AssertContains(mfcDialog, "ReplaceThreadDataInputFiles(m_thrdData, Paras);", "MFC dialog does not yet route command-line file loading through the grouped ThreadDataAccess replacement helper.");
            AssertContains(mfcDialog, "AppendThreadDataInputFile(m_thrdData, tstrDragFilename);", "MFC dialog does not yet route drag-drop path appends through ThreadDataAccess.");
            AssertContains(mfcDialog, "HasThreadDataInputFiles(m_thrdData)", "MFC dialog does not yet route input-file presence checks through ThreadDataAccess.");
            AssertContains(mfcDialog, "SetThreadDataUppercase(m_thrdData, (m_chkUppercase.GetCheck() != FALSE));", "MFC dialog does not yet route uppercase updates through ThreadDataAccess.");
            AssertContains(mfcDialog, "SetThreadDataWorking(m_thrdData, false);", "MFC dialog does not yet route initial working-state resets through ThreadDataAccess.");
            AssertContains(mfcDialog, "IsThreadDataWorking(m_thrdData)", "MFC dialog does not yet route working-state checks through ThreadDataAccess.");
            AssertContains(mfcDialog, "GetThreadDataUppercase(m_thrdData)", "MFC dialog does not yet route uppercase reads through ThreadDataAccess.");
            AssertContains(mfcDialog, "GetThreadDataTotalSize(m_thrdData)", "MFC dialog does not yet route total-size reads through ThreadDataAccess.");
            AssertContains(mfcDialog, "GetThreadDataResults(m_thrdData)", "MFC dialog does not yet route grouped result-list reads through ThreadDataAccess.");
            AssertContains(mfcDialog, "VisitThreadDataResults(m_thrdData, [&](const ResultData& result)", "MFC dialog does not yet route grouped result iteration through ThreadDataAccess.");
            AssertContains(mfcDialog, "SetThreadDataStop(m_thrdData, false);", "MFC dialog does not yet route work-thread start stop-flag resets through ThreadDataAccess.");
            AssertContains(mfcDialog, "SetThreadDataStop(m_thrdData, true);", "MFC dialog does not yet route work-thread stop requests through ThreadDataAccess.");
            AssertContains(mfcDialog, "ResetThreadDataInputFiles(m_thrdData);", "MFC dialog does not yet route file-path clearing through ThreadDataAccess.");
            AssertContains(mfcDialog, "ReplaceTrimmedThreadDataInputFiles(m_thrdData, Paras);", "MFC dialog does not yet route WM_COPYDATA file loading through the grouped trimmed ThreadDataAccess replacement helper.");
            AssertContains(mfcDialog, "ClearThreadDataResults(m_thrdData);", "MFC dialog does not yet route result clearing through ThreadDataAccess.");
        }, failures);

        Run("Phase 2 routes digest access through a neutral ResultData seam while keeping the fixed four-digest contract", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string engine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");
            string mfcDialog = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string clrMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");
            string bridgeWuiHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.h");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(digestAccess, "enum ResultDigestType", "ResultDigestAccess is missing the neutral digest enum.");
            AssertContains(digestAccess, "RESULT_DIGEST_MD5", "ResultDigestAccess no longer exposes the MD5 digest slot.");
            AssertContains(digestAccess, "RESULT_DIGEST_SHA1", "ResultDigestAccess no longer exposes the SHA1 digest slot.");
            AssertContains(digestAccess, "RESULT_DIGEST_SHA256", "ResultDigestAccess no longer exposes the SHA256 digest slot.");
            AssertContains(digestAccess, "RESULT_DIGEST_SHA512", "ResultDigestAccess no longer exposes the SHA512 digest slot.");
            AssertContains(digestAccess, "GetResultDigestCount()", "ResultDigestAccess is missing the neutral digest count helper.");
            AssertContains(digestAccess, "GetResultDigestTypeAt(int index)", "ResultDigestAccess is missing the neutral digest order helper.");
            AssertContains(digestAccess, "GetResultDigestLabel(ResultDigestType digestType)", "ResultDigestAccess is missing the neutral digest label helper.");
            AssertContains(digestAccess, "GetResultDigest(const ResultData& result, ResultDigestType digestType)", "ResultDigestAccess is missing the neutral digest getter.");
            AssertContains(digestAccess, "GetMutableResultDigest(ResultData& result, ResultDigestType digestType)", "ResultDigestAccess is missing the mutable digest getter.");
            AssertContains(digestAccess, "SetResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)", "ResultDigestAccess is missing the neutral digest setter.");
            AssertContains(digestAccess, "ResultContainsDigest(const ResultData& result, const sunjwbase::tstring& digestText)", "ResultDigestAccess is missing the neutral digest search helper.");
            AssertContains(digestAccess, "VisitResultDigests([&](ResultDigestType digestType)", "ResultDigestAccess no longer routes digest search through the neutral digest iteration helper.");
            AssertContains(digestAccess, "GetResultDigest(result, digestType).find(digestText)", "ResultDigestAccess no longer routes digest search through the neutral digest order helper.");

            AssertContains(engine, "#include \"Common/ResultDigestAccess.h\"", "HashEngine.cpp is not yet using the neutral digest-access seam.");
            AssertContains(engine, "typedef ResultDigestStorage FinalizedDigestBundle;", "HashEngine does not yet centralize finalized digest strings through the finalized digest bundle.");
            AssertContains(engine, "for (int index = 0; index < GetResultDigestCount(); index++)", "HashEngine no longer routes digest publishing through the neutral digest iteration helper.");
            AssertContains(engine, "ResultDigestType digestType = GetResultDigestTypeAt(index);", "HashEngine no longer routes digest publishing through the neutral digest order helper.");
            AssertContains(engine, "GetFinalizedDigestValue(digestBundle, digestType)", "HashEngine does not yet read finalized digest strings through the finalized digest seam.");
            AssertContains(engine, "SetResultDigest(result, digestType, GetFinalizedDigestValue(digestBundle, digestType));", "HashEngine no longer writes finalized digests through the neutral seam.");
            AssertDoesNotContain(engine, "SetResultDigest(result, RESULT_DIGEST_MD5, tstrFileMD5);", "HashEngine still hardcodes the MD5 digest slot instead of iterating through the neutral seam.");
            AssertDoesNotContain(engine, "SetResultDigest(result, RESULT_DIGEST_SHA1, tstrFileSHA1);", "HashEngine still hardcodes the SHA1 digest slot instead of iterating through the neutral seam.");
            AssertDoesNotContain(engine, "SetResultDigest(result, RESULT_DIGEST_SHA256, tstrFileSHA256);", "HashEngine still hardcodes the SHA256 digest slot instead of iterating through the neutral seam.");
            AssertDoesNotContain(engine, "SetResultDigest(result, RESULT_DIGEST_SHA512, tstrFileSHA512);", "HashEngine still hardcodes the SHA512 digest slot instead of iterating through the neutral seam.");
            AssertDoesNotContain(engine, "result.tstrMD5 = tstrFileMD5;", "HashEngine still writes MD5 directly instead of going through the digest seam.");
            AssertDoesNotContain(engine, "result.tstrSHA1 = tstrFileSHA1;", "HashEngine still writes SHA1 directly instead of going through the digest seam.");
            AssertDoesNotContain(engine, "result.tstrSHA256 = tstrFileSHA256;", "HashEngine still writes SHA256 directly instead of going through the digest seam.");
            AssertDoesNotContain(engine, "result.tstrSHA512 = tstrFileSHA512;", "HashEngine still writes SHA512 directly instead of going through the digest seam.");

            AssertContains(bridgeMfc, "#include \"Common/ResultDigestAccess.h\"", "UIBridgeMFC.cpp is not yet using the neutral digest-access seam.");
            AssertContains(bridgeMfc, "VisitResultDigestDisplayValues(result, uppercase, [&](int index, const ResultDigestMetadata& digestMetadata, const ResultDigestDisplayInfo& digestDisplayInfo)", "UIBridgeMFC no longer iterates formatted digest display values through the neutral visitor seam.");
            AssertDoesNotContain(bridgeMfc, "result.tstrMD5", "UIBridgeMFC still reads MD5 directly instead of using the digest seam.");
            AssertDoesNotContain(bridgeMfc, "result.tstrSHA1", "UIBridgeMFC still reads SHA1 directly instead of using the digest seam.");
            AssertDoesNotContain(bridgeMfc, "result.tstrSHA256", "UIBridgeMFC still reads SHA256 directly instead of using the digest seam.");
            AssertDoesNotContain(bridgeMfc, "result.tstrSHA512", "UIBridgeMFC still reads SHA512 directly instead of using the digest seam.");
            AssertDoesNotContain(bridgeMfc, "AppendTextToBuffer(_T(\"\\r\\nSHA1: \"))", "UIBridgeMFC still hardcodes digest label emission instead of using the digest seam.");
            AssertDoesNotContain(bridgeMfc, "AppendTextToBuffer(_T(\"\\r\\nSHA256: \"))", "UIBridgeMFC still hardcodes digest label emission instead of using the digest seam.");
            AssertDoesNotContain(bridgeMfc, "AppendTextToBuffer(_T(\"\\r\\nSHA512: \"))", "UIBridgeMFC still hardcodes digest label emission instead of using the digest seam.");

            AssertContains(mfcDialog, "VisitPathAndDigestMatchingResults(GetThreadDataResults(m_thrdData), tstrFileToFind, tstrHashToFind, [&](const ResultData& result)", "MFC dialog no longer routes digest search through the neutral digest seam.");
            AssertContains(clrMgmt, "CountDigestMatchingResults(resultList, tstrHashToFind)", "CLR bridge search no longer routes digest-match counting through the neutral digest seam.");
            AssertContains(clrMgmt, "ResultMatchesDigestText(*itr, tstrHashToFind)", "CLR bridge search no longer routes per-result digest matching through the neutral digest seam.");
            AssertContains(clrMgmt, "ProjectResultDataToNet<ResultDataNet, ResultStateNet>(*itr, ConvertTstrToSystemString)", "CLR bridge search no longer routes projected result creation through the centralized projection seam.");
            AssertContains(uwpMgmt, "CreateProjectedDigestMatchingResults<ResultDataNet, ResultStateNet, Array<ResultDataNet>^>(GetThreadDataResults(m_threadData), tstrHashToFind, [&](size_t resultCount)", "UWP bridge search no longer routes through the neutral digest seam.");

            AssertContains(resultAccess, "template<typename TResultDataNet, typename TResultString>", "ResultDataAccess does not yet expose the centralized ResultDataNet digest-assignment template.");
            AssertContains(resultAccess, "static inline TResultDataNet AssignResultDigestToNet(TResultDataNet resultDataNet, ResultDigestType digestType, TResultString digestValue)", "ResultDataAccess does not yet expose the centralized ResultDataNet digest-assignment helper.");
            AssertContains(resultAccess, "template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter>", "ResultDataAccess does not yet expose the centralized ResultDataNet projection template.");
            AssertContains(resultAccess, "static inline TResultDataNet ProjectResultDataToNet(const ResultData& result, TStringConverter convertString)", "ResultDataAccess does not yet expose the centralized ResultDataNet projection helper.");
            AssertContains(resultAccess, "for (int digestIndex = 0; digestIndex < GetResultDigestCount(); digestIndex++)", "ResultDataAccess does not yet route managed digest projection through the centralized digest loop.");
            AssertContains(resultAccess, "ResultDigestType digestType = GetResultDigestTypeAt(digestIndex);", "ResultDataAccess does not yet resolve digest projection order through the centralized digest metadata seam.");
            AssertContains(resultAccess, "const tstring& digestValueTstr = GetResultDigest(result, digestType);", "ResultDataAccess does not yet read digest projection values through the centralized digest access seam.");
            AssertContains(resultAccess, "AssignResultDigestToNet(resultDataNet, digestType, convertString(digestValueTstr.c_str()));", "ResultDataAccess does not yet compose digest projection through the centralized digest-assignment seam.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route ResultDataNet projection through the centralized projection helper.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route ResultDataNet projection through the dedicated managed bridge helper.");
            AssertDoesNotContain(bridgeWui, "VisitResultDigestValues(result, [&](ResultDigestType digestType, const tstring& digestValueTstr)", "WinUI bridge still keeps local digest iteration instead of using the centralized ResultDataNet projection seam.");
            AssertDoesNotContain(bridgeWui, "static void AssignDigestToNet(ResultDataNet% resultDataNet, ResultDigestType digestType, String^ digestValue)", "WinUI bridge still keeps a local digest-assignment helper instead of using the centralized ResultDataNet seam.");
            AssertDoesNotContain(bridgeWui, "resultDataNet.MD5 = digestValue;\r\n\t\t\tbreak;", "WinUI bridge still inlines MD5 digest assignment inside the iteration path instead of using the bridge-local assignment helper.");
            AssertDoesNotContain(bridgeWui, "resultDataNet.SHA1 = digestValue;\r\n\t\t\tbreak;", "WinUI bridge still inlines SHA1 digest assignment inside the iteration path instead of using the bridge-local assignment helper.");
            AssertDoesNotContain(bridgeWui, "resultDataNet.SHA256 = digestValue;\r\n\t\t\tbreak;", "WinUI bridge still inlines SHA256 digest assignment inside the iteration path instead of using the bridge-local assignment helper.");
            AssertDoesNotContain(bridgeWui, "resultDataNet.SHA512 = digestValue;\r\n\t\t\tbreak;", "WinUI bridge still inlines SHA512 digest assignment inside the iteration path instead of using the bridge-local assignment helper.");
            AssertDoesNotContain(bridgeWui, "GetResultDigest(result, RESULT_DIGEST_MD5)", "WinUI bridge still hardcodes the MD5 digest slot instead of iterating through the neutral digest seam.");
            AssertDoesNotContain(bridgeWui, "GetResultDigest(result, RESULT_DIGEST_SHA1)", "WinUI bridge still hardcodes the SHA1 digest slot instead of iterating through the neutral digest seam.");
            AssertDoesNotContain(bridgeWui, "GetResultDigest(result, RESULT_DIGEST_SHA256)", "WinUI bridge still hardcodes the SHA256 digest slot instead of iterating through the neutral digest seam.");
            AssertDoesNotContain(bridgeWui, "GetResultDigest(result, RESULT_DIGEST_SHA512)", "WinUI bridge still hardcodes the SHA512 digest slot instead of iterating through the neutral digest seam.");

            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route ResultDataNet projection through the centralized projection helper.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route ResultDataNet projection through the dedicated managed bridge helper.");
            AssertDoesNotContain(bridgeUwp, "VisitResultDigestValues(result, [&](ResultDigestType digestType, const tstring& digestValueTstr)", "UWP bridge still keeps local digest iteration instead of using the centralized ResultDataNet projection seam.");
            AssertDoesNotContain(bridgeUwp, "static void AssignDigestToNet(ResultDataNet& resultDataNet, ResultDigestType digestType, String^ digestValue)", "UWP bridge still keeps a local digest-assignment helper instead of using the centralized ResultDataNet seam.");
            AssertDoesNotContain(bridgeUwp, "resultDataNet.MD5 = digestValue;\r\n\t\t\tbreak;", "UWP bridge still inlines MD5 digest assignment inside the iteration path instead of using the bridge-local assignment helper.");
            AssertDoesNotContain(bridgeUwp, "resultDataNet.SHA1 = digestValue;\r\n\t\t\tbreak;", "UWP bridge still inlines SHA1 digest assignment inside the iteration path instead of using the bridge-local assignment helper.");
            AssertDoesNotContain(bridgeUwp, "resultDataNet.SHA256 = digestValue;\r\n\t\t\tbreak;", "UWP bridge still inlines SHA256 digest assignment inside the iteration path instead of using the bridge-local assignment helper.");
            AssertDoesNotContain(bridgeUwp, "resultDataNet.SHA512 = digestValue;\r\n\t\t\tbreak;", "UWP bridge still inlines SHA512 digest assignment inside the iteration path instead of using the bridge-local assignment helper.");
            AssertDoesNotContain(bridgeUwp, "GetResultDigest(result, RESULT_DIGEST_MD5)", "UWP bridge still hardcodes the MD5 digest slot instead of iterating through the neutral digest seam.");
            AssertDoesNotContain(bridgeUwp, "GetResultDigest(result, RESULT_DIGEST_SHA1)", "UWP bridge still hardcodes the SHA1 digest slot instead of iterating through the neutral digest seam.");
            AssertDoesNotContain(bridgeUwp, "GetResultDigest(result, RESULT_DIGEST_SHA256)", "UWP bridge still hardcodes the SHA256 digest slot instead of iterating through the neutral digest seam.");
            AssertDoesNotContain(bridgeUwp, "GetResultDigest(result, RESULT_DIGEST_SHA512)", "UWP bridge still hardcodes the SHA512 digest slot instead of iterating through the neutral digest seam.");
        }, failures);

        Run("Phase 3 introduces internal digest storage behind ResultData while preserving the legacy four-field compatibility surface", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");
            string engine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(global, "struct ResultDigestStorage", "ResultData digest storage has not yet been wrapped in the dedicated storage struct introduced in phase 3.");
            AssertContains(global, "sunjwbase::tstring values[RESULT_DIGEST_STORAGE_COUNT];", "ResultDigestStorage does not yet expose the internal digest storage introduced in phase 3.");
            AssertContains(global, "struct ResultDigestState", "ResultData digest compatibility surface has not yet been wrapped in the dedicated digest-state struct introduced in phase 3.");
            AssertContains(global, "ResultDigestStorage storage;", "ResultDigestState does not yet route internal digest storage through the dedicated storage struct introduced in phase 3.");
            AssertContains(global, "struct ResultDigestCompatibilityFields", "ResultData digest compatibility surface has not yet been wrapped in the dedicated compatibility-fields struct introduced in phase 3.");
            AssertContains(global, "ResultDigestCompatibilityFields compatibilityFields;", "ResultDigestState does not yet route the digest compatibility surface through the dedicated compatibility-fields struct introduced in phase 3.");
            AssertContains(global, "ResultDigestState digestState;", "ResultData does not yet route digest storage and legacy compatibility through the dedicated digest-state struct introduced in phase 3.");
            AssertContains(global, "sunjwbase::tstring md5;", "Phase 3 must still preserve the legacy MD5 compatibility field.");
            AssertContains(global, "sunjwbase::tstring sha1;", "Phase 3 must still preserve the legacy SHA1 compatibility field.");
            AssertContains(global, "sunjwbase::tstring sha256;", "Phase 3 must still preserve the legacy SHA256 compatibility field.");
            AssertContains(global, "sunjwbase::tstring sha512;", "Phase 3 must still preserve the legacy SHA512 compatibility field.");

            AssertContains(digestAccess, "GetResultDigestIndex(ResultDigestType digestType)", "ResultDigestAccess is missing the neutral digest-index helper introduced in phase 3.");
            AssertContains(digestAccess, "GetCompatibilityResultDigest(const ResultData& result, ResultDigestType digestType)", "ResultDigestAccess is missing the const digest compatibility helper introduced in phase 3.");
            AssertContains(digestAccess, "GetMutableCompatibilityResultDigest(ResultData& result, ResultDigestType digestType)", "ResultDigestAccess is missing the mutable digest compatibility helper introduced in phase 3.");
            AssertContains(digestAccess, "if (HasStoredResultDigest(result, digestType))", "ResultDigestAccess does not yet read through the internal digest storage first in phase 3.");
            AssertContains(digestAccess, "return GetCompatibilityResultDigest(result, digestType);", "ResultDigestAccess does not yet preserve the compatibility fallback path in phase 3.");
            AssertContains(digestAccess, "return GetMutableStoredResultDigest(result, digestType);", "ResultDigestAccess does not yet expose mutable access through the internal digest storage in phase 3.");
            AssertContains(digestAccess, "GetMutableCompatibilityResultDigest(result, digestType) = digestValue;", "ResultDigestAccess does not yet mirror the new digest storage back into the compatibility fields in phase 3.");

            AssertContains(engine, "SetResultDigest(result, digestType, GetFinalizedDigestValue(digestBundle, digestType));", "HashEngine no longer routes finalized digest writes through the phase-3 digest seam.");
            AssertContains(bridgeMfc, "VisitResultDigestDisplayValues(result, uppercase, [&](int index, const ResultDigestMetadata& digestMetadata, const ResultDigestDisplayInfo& digestDisplayInfo)", "MFC digest rendering no longer reads through the phase-5 formatted digest-display visitor seam.");
        }, failures);

        Run("Phase 3 resets digest storage through the neutral seam when a file result is created", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");
            string engine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");

            AssertContains(digestAccess, "ResetResultDigests(ResultData& result)", "ResultDigestAccess does not yet expose the digest-reset helper introduced in phase 3.");
            AssertContains(digestAccess, "VisitResultDigests([&](ResultDigestType digestType)", "ResultDigestAccess digest-reset helper does not yet iterate through the centralized visitor helper.");
            AssertContains(digestAccess, "ClearStoredResultDigest(result, digestType);", "ResultDigestAccess digest-reset helper does not yet clear internal digest storage.");
            AssertContains(digestAccess, "ClearCompatibilityResultDigest(result, digestType);", "ResultDigestAccess digest-reset helper does not yet clear the digest compatibility fields.");

            AssertContains(engine, "ResetResultData(result);", "HashEngine does not yet reset digest storage through the grouped reset seam when a file result is created.");
            AssertInOrder(engine,
                [
                    "static ResultData& BeginFileResult(",
                    "ResultData& result = AppendThreadDataResult(*thrdData);",
                    "ResetResultData(result);",
                    "SetResultState(result, RESULT_NONE);",
                    "SetResultPath(result, path);",
                    "EmitPathResult(observer, result);"
                ],
                "HashEngine file-result begin helper no longer resets digest storage before publishing the file path.");
        }, failures);

        Run("Phase 3 centralizes the internal digest storage count instead of duplicating the magic number", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(global, "RESULT_DIGEST_STORAGE_COUNT = 4", "Global.h does not yet centralize the internal digest storage count in phase 3.");
            AssertContains(global, "sunjwbase::tstring values[RESULT_DIGEST_STORAGE_COUNT];", "ResultData internal digest storage is not yet bound to the centralized storage-count constant.");
            AssertContains(digestAccess, "return RESULT_DIGEST_STORAGE_COUNT;", "ResultDigestAccess does not yet route digest count through the centralized storage-count constant.");
        }, failures);

        Run("Phase 3 routes direct internal digest-slot access through stored-digest helpers", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(digestAccess, "GetStoredResultDigest(const ResultData& result, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the const stored-digest helper introduced in phase 3.");
            AssertContains(digestAccess, "GetMutableStoredResultDigest(ResultData& result, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the mutable stored-digest helper introduced in phase 3.");
            AssertContains(digestAccess, "return GetDigestStorageValue(GetResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest helpers do not yet route into the dedicated internal digest storage struct.");
            AssertContains(digestAccess, "return GetStoredResultDigest(result, digestType);", "ResultDigestAccess getter does not yet read through the stored-digest helper.");
            AssertContains(digestAccess, "return GetMutableStoredResultDigest(result, digestType);", "ResultDigestAccess mutable getter does not yet route through the stored-digest helper.");
            AssertContains(digestAccess, "ClearDigestStorageValue(GetMutableResultDigestStorage(result), digestType);", "ResultDigestAccess digest-reset helper does not yet clear digests through the stored-digest helper.");
        }, failures);

        Run("Phase 3 routes legacy digest synchronization through dedicated compatibility helpers", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(digestAccess, "SetCompatibilityResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)", "ResultDigestAccess does not yet expose the digest compatibility write helper introduced in phase 3.");
            AssertContains(digestAccess, "ClearCompatibilityResultDigest(ResultData& result, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the digest compatibility clear helper introduced in phase 3.");
            AssertContains(digestAccess, "SetCompatibilityResultDigest(result, digestType, digestValue);", "ResultDigestAccess setter does not yet route compatibility synchronization through the dedicated helper.");
            AssertContains(digestAccess, "ClearCompatibilityResultDigest(result, digestType);", "ResultDigestAccess digest-reset helper does not yet route compatibility clearing through the dedicated helper.");
        }, failures);

        Run("Phase 3 routes legacy digest field selection through a single compatibility-field helper", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(digestAccess, "GetCompatibilityResultDigestField(ResultDigestType digestType)", "ResultDigestAccess does not yet expose the compatibility digest field-selector helper introduced in phase 3.");
            AssertContains(digestAccess, "GetResultDigestMetadataCompatibilityValueField(const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess does not yet expose the metadata compatibility-field accessor.");
            AssertContains(digestAccess, "return GetResultDigestMetadataCompatibilityValueField(GetResultDigestMetadata(digestType));", "ResultDigestAccess compatibility field-selector helper does not yet route through the metadata compatibility-field accessor.");
            AssertContains(digestAccess, "return GetResultDigestCompatibilityFields(result).*GetCompatibilityResultDigestField(digestType);", "ResultDigestAccess compatibility getters do not yet route through the compatibility-field helper and dedicated digest compatibility state.");
        }, failures);

        Run("Phase 3 centralizes digest metadata so type, label, and legacy-field mapping share one source of truth", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(digestAccess, "struct ResultDigestMetadata", "ResultDigestAccess does not yet expose the centralized digest metadata struct introduced in phase 3.");
            AssertContains(digestAccess, "GetResultDigestMetadataAt(int index)", "ResultDigestAccess does not yet expose the centralized digest metadata lookup helper introduced in phase 3.");
            AssertContains(digestAccess, "{ RESULT_DIGEST_MD5, \"MD5\", &ResultDigestCompatibilityFields::md5 }", "ResultDigestAccess metadata table does not yet map MD5.");
            AssertContains(digestAccess, "{ RESULT_DIGEST_SHA1, \"SHA1\", &ResultDigestCompatibilityFields::sha1 }", "ResultDigestAccess metadata table does not yet map SHA1.");
            AssertContains(digestAccess, "{ RESULT_DIGEST_SHA256, \"SHA256\", &ResultDigestCompatibilityFields::sha256 }", "ResultDigestAccess metadata table does not yet map SHA256.");
            AssertContains(digestAccess, "{ RESULT_DIGEST_SHA512, \"SHA512\", &ResultDigestCompatibilityFields::sha512 }", "ResultDigestAccess metadata table does not yet map SHA512.");
            AssertContains(digestAccess, "GetResultDigestMetadataType(const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess does not yet expose the metadata type accessor.");
            AssertContains(digestAccess, "GetResultDigestMetadataDisplayLabel(const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess does not yet expose the metadata label accessor.");
            AssertContains(digestAccess, "return GetResultDigestMetadataType(GetResultDigestMetadataAt(index));", "ResultDigestAccess digest-order helper does not yet route through the metadata type accessor.");
            AssertContains(digestAccess, "GetResultDigestLabel(const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess does not yet expose the metadata-label overload introduced in phase 5.");
            AssertContains(digestAccess, "return GetResultDigestMetadataDisplayLabel(digestMetadata);", "ResultDigestAccess metadata-label overload does not yet route through the metadata label accessor.");
            AssertContains(digestAccess, "return GetResultDigestMetadataCompatibilityValueField(GetResultDigestMetadata(digestType));", "ResultDigestAccess compatibility field-selector helper does not yet route through the metadata compatibility-field accessor.");
        }, failures);

        Run("Phase 3 routes type-based metadata lookup and stored-digest presence checks through dedicated helpers", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(digestAccess, "GetResultDigestMetadata(ResultDigestType digestType)", "ResultDigestAccess does not yet expose the type-based metadata lookup helper introduced in phase 3.");
            AssertContains(digestAccess, "GetResultDigestMetadataAt(GetResultDigestIndex(digestType))", "ResultDigestAccess type-based metadata lookup helper does not yet route through index lookup.");
            AssertContains(digestAccess, "HasStoredResultDigest(const ResultData& result, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the stored-digest presence helper introduced in phase 3.");
            AssertContains(digestAccess, "return HasDigestStorageValue(GetResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest presence helper does not yet route through the stored-digest helper.");
            AssertContains(digestAccess, "return GetResultDigestLabel(GetResultDigestMetadata(digestType));", "ResultDigestAccess digest-label helper does not yet route through the metadata-label overload.");
            AssertContains(digestAccess, "return GetResultDigestMetadataCompatibilityValueField(GetResultDigestMetadata(digestType));", "ResultDigestAccess compatibility field-selector helper does not yet route through the type-based metadata lookup helper.");
            AssertContains(digestAccess, "if (HasStoredResultDigest(result, digestType))", "ResultDigestAccess digest getter does not yet route stored-presence checks through the dedicated helper.");
        }, failures);

        Run("Phase 3 routes stored digest writes and clears through dedicated internal-storage helpers", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(digestAccess, "SetStoredResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)", "ResultDigestAccess does not yet expose the stored-digest write helper introduced in phase 3.");
            AssertContains(digestAccess, "ClearStoredResultDigest(ResultData& result, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the stored-digest clear helper introduced in phase 3.");
            AssertContains(digestAccess, "SetStoredResultDigest(result, digestType, digestValue);", "ResultDigestAccess setter does not yet route internal storage writes through the dedicated helper.");
            AssertContains(digestAccess, "ClearStoredResultDigest(result, digestType);", "ResultDigestAccess reset helper does not yet route internal storage clearing through the dedicated helper.");
        }, failures);

        Run("Phase 3 routes digest-index lookup through centralized metadata instead of a standalone switch", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(digestAccess, "GetResultDigestIndex(ResultDigestType digestType)", "ResultDigestAccess does not yet expose the digest-index helper.");
            AssertContains(digestAccess, "VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess digest-index helper does not yet route through centralized metadata iteration.");
            AssertContains(digestAccess, "if (GetResultDigestMetadataType(digestMetadata) == digestType)", "ResultDigestAccess digest-index helper does not yet route through the metadata type accessor.");
            AssertContains(digestAccess, "digestIndex = index;", "ResultDigestAccess digest-index helper no longer captures the matching neutral index.");
            AssertDoesNotContain(digestAccess, "switch (digestType)", "ResultDigestAccess digest-index helper still uses the old standalone switch mapping.");
        }, failures);

        Run("Phase 3 routes digest iteration through a dedicated metadata visitor helper", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(digestAccess, "template<typename TResultDigestVisitor>", "ResultDigestAccess does not yet expose the digest-visitor template introduced in phase 3.");
            AssertContains(digestAccess, "VisitResultDigests(TResultDigestVisitor visitor)", "ResultDigestAccess does not yet expose the centralized digest visitor helper.");
            AssertContains(digestAccess, "VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess digest visitor helper does not yet route through the centralized metadata visitor helper.");
            AssertContains(digestAccess, "return visitor(GetResultDigestMetadataType(digestMetadata));", "ResultDigestAccess digest visitor helper does not yet route through the metadata type accessor.");
            AssertContains(digestAccess, "VisitResultDigests([&](ResultDigestType digestType)", "ResultDigestAccess does not yet route digest iteration through the centralized visitor helper.");
            AssertContains(digestAccess, "return true;", "ResultDigestAccess digest visitor usage no longer preserves the current early-success semantics.");
        }, failures);

        Run("Phase 3 routes metadata iteration through a dedicated metadata visitor helper", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(digestAccess, "template<typename TResultDigestMetadataVisitor>", "ResultDigestAccess does not yet expose the metadata-visitor template introduced in phase 3.");
            AssertContains(digestAccess, "VisitResultDigestMetadata(TResultDigestMetadataVisitor visitor)", "ResultDigestAccess does not yet expose the centralized metadata visitor helper.");
            AssertContains(digestAccess, "visitor(index, GetResultDigestMetadataAt(index))", "ResultDigestAccess metadata visitor helper does not yet route through the centralized metadata lookup helper.");
            AssertContains(digestAccess, "VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess does not yet route metadata iteration through the centralized metadata visitor helper.");
            AssertContains(digestAccess, "return visitor(GetResultDigestMetadataType(digestMetadata));", "ResultDigestAccess digest visitor does not yet route through the metadata visitor helper.");
            AssertContains(digestAccess, "if (GetResultDigestMetadataType(digestMetadata) == digestType)", "ResultDigestAccess digest-index helper does not yet route through the metadata visitor helper.");
            AssertContains(digestAccess, "digestIndex = index;", "ResultDigestAccess digest-index helper no longer captures the centralized metadata index.");
        }, failures);

        Run("Phase 3 routes digest-value iteration through a dedicated value visitor helper and reuses it in managed bridges", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(digestAccess, "template<typename TResultDigestValueVisitor>", "ResultDigestAccess does not yet expose the digest-value visitor template introduced in phase 3.");
            AssertContains(digestAccess, "VisitResultDigestValues(const ResultData& result, TResultDigestValueVisitor visitor)", "ResultDigestAccess does not yet expose the centralized digest-value visitor helper.");
            AssertContains(digestAccess, "return VisitResultDigests([&](ResultDigestType digestType)", "ResultDigestAccess digest-value visitor helper does not yet route through the centralized digest visitor helper.");
            AssertContains(digestAccess, "return visitor(digestType, GetResultDigest(result, digestType));", "ResultDigestAccess digest-value visitor helper does not yet feed values through the neutral digest seam.");

            AssertContains(resultAccess, "for (int digestIndex = 0; digestIndex < GetResultDigestCount(); digestIndex++)", "ResultDataAccess does not yet consume digest values through the centralized digest loop helper when projecting managed result data.");
            AssertContains(resultAccess, "ResultDigestType digestType = GetResultDigestTypeAt(digestIndex);", "ResultDataAccess does not yet resolve digest order through the centralized digest metadata seam when projecting managed result data.");
            AssertContains(resultAccess, "const tstring& digestValueTstr = GetResultDigest(result, digestType);", "ResultDataAccess does not yet read digest values through the centralized digest access seam when projecting managed result data.");
            AssertContains(resultAccess, "convertString(digestValueTstr.c_str())", "ResultDataAccess does not yet convert digest values from the digest-value visitor payload when projecting managed result data.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet consume digest values through the centralized ResultDataNet projection helper.");
            AssertDoesNotContain(bridgeWui, "String^ digestValue = ConvertTstrToSystemString(GetResultDigest(result, digestType).c_str());", "WinUI bridge still performs inline digest lookup instead of consuming the digest-value visitor payload.");

            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet consume digest values through the centralized ResultDataNet projection helper.");
            AssertDoesNotContain(bridgeUwp, "String^ digestValue = ConvertToPlatStr(GetResultDigest(result, digestType).c_str());", "UWP bridge still performs inline digest lookup instead of consuming the digest-value visitor payload.");
        }, failures);

        Run("Phase 3 promotes ResultDigestStorage into a reusable neutral storage seam and reuses it for finalized digest bundles", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");
            string engine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");

            AssertContains(digestAccess, "GetDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the reusable digest-storage getter seam.");
            AssertContains(digestAccess, "GetMutableDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the reusable mutable digest-storage seam.");
            AssertContains(digestAccess, "HasDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the reusable digest-storage presence seam.");
            AssertContains(digestAccess, "SetDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType, const sunjwbase::tstring& digestValue)", "ResultDigestAccess does not yet expose the reusable digest-storage write seam.");
            AssertContains(digestAccess, "ClearDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the reusable digest-storage clear seam.");
            AssertContains(digestAccess, "return digestStorage.values[GetResultDigestIndex(digestType)];", "ResultDigestAccess reusable digest-storage seam does not yet route through the centralized digest index.");
            AssertContains(digestAccess, "return GetDigestStorageValue(GetResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest getter does not yet reuse the neutral digest-storage seam.");
            AssertContains(digestAccess, "return GetMutableDigestStorageValue(GetMutableResultDigestStorage(result), digestType);", "ResultDigestAccess mutable stored-digest getter does not yet reuse the neutral digest-storage seam.");
            AssertContains(digestAccess, "return HasDigestStorageValue(GetResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest presence helper does not yet reuse the neutral digest-storage seam.");
            AssertContains(digestAccess, "SetDigestStorageValue(GetMutableResultDigestStorage(result), digestType, digestValue);", "ResultDigestAccess stored-digest setter does not yet reuse the neutral digest-storage seam.");
            AssertContains(digestAccess, "ClearDigestStorageValue(GetMutableResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest clear helper does not yet reuse the neutral digest-storage seam.");

            AssertContains(engine, "typedef ResultDigestStorage FinalizedDigestBundle;", "HashEngine does not yet reuse ResultDigestStorage as the finalized digest bundle.");
            AssertContains(engine, "GetDigestStorageValue(digestBundle, digestType)", "HashEngine finalized-digest getter does not yet reuse the neutral digest-storage seam.");
            AssertContains(engine, "SetDigestStorageValue(digestBundle, digestType, digestValue);", "HashEngine finalized-digest setter does not yet reuse the neutral digest-storage seam.");
            AssertDoesNotContain(engine, "tstring digestValues[RESULT_DIGEST_STORAGE_COUNT];", "HashEngine still duplicates digest storage layout inside FinalizedDigestBundle instead of reusing the neutral storage seam.");
        }, failures);

        Run("Phase 3 routes ResultData digest-state access through dedicated state and field helpers", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(digestAccess, "GetResultDigestState(const ResultData& result)", "ResultDigestAccess does not yet expose the const digest-state helper introduced in phase 3.");
            AssertContains(digestAccess, "GetMutableResultDigestState(ResultData& result)", "ResultDigestAccess does not yet expose the mutable digest-state helper introduced in phase 3.");
            AssertContains(digestAccess, "GetResultDigestStorage(const ResultData& result)", "ResultDigestAccess does not yet expose the const result-digest-storage helper introduced in phase 3.");
            AssertContains(digestAccess, "GetMutableResultDigestStorage(ResultData& result)", "ResultDigestAccess does not yet expose the mutable result-digest-storage helper introduced in phase 3.");
            AssertContains(digestAccess, "GetResultDigestCompatibilityFields(const ResultData& result)", "ResultDigestAccess does not yet expose the const digest-compatibility-fields helper introduced in phase 3.");
            AssertContains(digestAccess, "GetMutableResultDigestCompatibilityFields(ResultData& result)", "ResultDigestAccess does not yet expose the mutable digest-compatibility-fields helper introduced in phase 3.");
            AssertContains(digestAccess, "return result.digestState;", "ResultDigestAccess digest-state helpers do not yet route through ResultData::digestState.");
            AssertContains(digestAccess, "return GetResultDigestState(result).storage;", "ResultDigestAccess const digest-storage helper does not yet route through the digest-state seam.");
            AssertContains(digestAccess, "return GetMutableResultDigestState(result).storage;", "ResultDigestAccess mutable digest-storage helper does not yet route through the digest-state seam.");
            AssertContains(digestAccess, "return GetResultDigestState(result).compatibilityFields;", "ResultDigestAccess const digest-compatibility-fields helper does not yet route through the digest-state seam.");
            AssertContains(digestAccess, "return GetMutableResultDigestState(result).compatibilityFields;", "ResultDigestAccess mutable digest-compatibility-fields helper does not yet route through the digest-state seam.");
            AssertContains(digestAccess, "return GetDigestStorageValue(GetResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest getter does not yet route through the result-digest-storage helper.");
            AssertContains(digestAccess, "return GetMutableDigestStorageValue(GetMutableResultDigestStorage(result), digestType);", "ResultDigestAccess mutable stored-digest getter does not yet route through the result-digest-storage helper.");
            AssertContains(digestAccess, "return HasDigestStorageValue(GetResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest presence helper does not yet route through the result-digest-storage helper.");
            AssertContains(digestAccess, "SetDigestStorageValue(GetMutableResultDigestStorage(result), digestType, digestValue);", "ResultDigestAccess stored-digest setter does not yet route through the result-digest-storage helper.");
            AssertContains(digestAccess, "ClearDigestStorageValue(GetMutableResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest clear helper does not yet route through the result-digest-storage helper.");
            AssertContains(digestAccess, "return GetResultDigestCompatibilityFields(result).*GetCompatibilityResultDigestField(digestType);", "ResultDigestAccess compatibility getter does not yet route through the digest-compatibility-fields helper.");
            AssertContains(digestAccess, "return GetMutableResultDigestCompatibilityFields(result).*GetCompatibilityResultDigestField(digestType);", "ResultDigestAccess mutable compatibility getter does not yet route through the digest-compatibility-fields helper.");
        }, failures);

        Run("Phase 3 routes digest metadata and values through a shared visitor seam for legacy MFC rendering", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(digestAccess, "template<typename TResultDigestMetadataValueVisitor>", "ResultDigestAccess does not yet expose the digest-metadata-value visitor template introduced in phase 3.");
            AssertContains(digestAccess, "VisitResultDigestMetadataValues(const ResultData& result, TResultDigestMetadataValueVisitor visitor)", "ResultDigestAccess does not yet expose the centralized digest-metadata-value visitor helper.");
            AssertContains(digestAccess, "return VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess digest-metadata-value visitor does not yet route through centralized metadata iteration.");
            AssertContains(digestAccess, "return visitor(index, digestMetadata, GetResultDigest(result, GetResultDigestMetadataType(digestMetadata)));", "ResultDigestAccess digest-metadata-value visitor does not yet feed value lookups through the metadata type accessor.");
            AssertContains(digestAccess, "VisitResultDigestDisplayValues(const ResultData& result, bool uppercase, TResultDigestDisplayVisitor visitor)", "ResultDigestAccess does not yet expose the centralized formatted digest-display visitor helper.");

            AssertContains(digestAccess, "struct ResultDigestDisplayInfo", "ResultDigestAccess does not yet expose the grouped digest display-info structure.");
            AssertContains(digestAccess, "GetResultDigestDisplayInfo(const ResultDigestMetadata& digestMetadata, const sunjwbase::tstring& digestValue, bool uppercase)", "ResultDigestAccess does not yet expose the grouped digest display-info helper.");
            AssertContains(digestAccess, "digestDisplayInfo.label = GetResultDigestLabel(digestMetadata);", "ResultDigestAccess digest display-info helper does not yet route labels through the metadata seam.");
            AssertContains(digestAccess, "digestDisplayInfo.value = FormatResultDigestForDisplay(digestValue, uppercase);", "ResultDigestAccess digest display-info helper does not yet route formatting through the centralized display seam.");
            AssertContains(digestAccess, "ResultDigestDisplayInfo digestDisplayInfo = GetResultDigestDisplayInfo(digestMetadata, digestValueTstr, uppercase);", "ResultDigestAccess formatted digest-display visitor does not yet route label/value assembly through the grouped digest display-info helper.");
            AssertContains(digestAccess, "return visitor(index, digestMetadata, digestDisplayInfo);", "ResultDigestAccess formatted digest-display visitor does not yet pass grouped digest display-info to consumers.");

            AssertContains(bridgeMfc, "VisitResultDigestDisplayValues(result, uppercase, [&](int index, const ResultDigestMetadata& digestMetadata, const ResultDigestDisplayInfo& digestDisplayInfo)", "Legacy MFC digest renderer does not yet consume formatted digest display values through the centralized visitor seam.");
            AssertDoesNotContain(bridgeMfc, "for (int index = 0; index < GetResultDigestCount(); index++)", "Legacy MFC digest renderer still performs manual index iteration instead of using the centralized metadata-value visitor seam.");
            AssertDoesNotContain(bridgeMfc, "ResultDigestType digestType = GetResultDigestTypeAt(index);", "Legacy MFC digest renderer still resolves digest order manually instead of using the centralized metadata-value visitor seam.");
            AssertDoesNotContain(bridgeMfc, "GetResultDigest(result, digestType)", "Legacy MFC digest renderer still performs inline digest lookup instead of using the visitor payload.");
        }, failures);

        Run("Phase 4 introduces a neutral ResultData access seam for non-digest UI and bridge reads", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");
            string filesHashDlg = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");

            AssertContains(resultAccess, "GetResultPath(const ResultData& result)", "ResultDataAccess does not yet expose the neutral path getter introduced in phase 4.");
            AssertContains(resultAccess, "GetResultSize(const ResultData& result)", "ResultDataAccess does not yet expose the neutral size getter introduced in phase 4.");
            AssertContains(resultAccess, "GetResultModifiedDate(const ResultData& result)", "ResultDataAccess does not yet expose the neutral modified-date getter introduced in phase 4.");
            AssertContains(resultAccess, "GetResultVersion(const ResultData& result)", "ResultDataAccess does not yet expose the neutral version getter introduced in phase 4.");
            AssertContains(resultAccess, "GetResultError(const ResultData& result)", "ResultDataAccess does not yet expose the neutral error getter introduced in phase 4.");
            AssertContains(resultAccess, "return GetResultCoreState(result).path;", "ResultDataAccess path getter does not yet route through the current ResultData field.");
            AssertContains(resultAccess, "return GetResultCoreState(result).size;", "ResultDataAccess size getter does not yet route through the current ResultData field.");
            AssertContains(resultAccess, "return GetResultCoreState(result).modifiedDate;", "ResultDataAccess modified-date getter does not yet route through the current ResultData field.");
            AssertContains(resultAccess, "return GetResultCoreState(result).version;", "ResultDataAccess version getter does not yet route through the current ResultData field.");
            AssertContains(resultAccess, "return GetResultCoreState(result).error;", "ResultDataAccess error getter does not yet route through the current ResultData field.");

            AssertContains(bridgeMfc, "#include \"Common/ResultDataAccess.h\"", "Legacy MFC bridge does not yet consume the neutral result-data access seam.");
            AssertContains(bridgeMfc, "AppendLabelValueLineToHyperEdit(GetStringByKey(FILENAME_STRING),", "Legacy MFC bridge does not yet route the file path through the current ResultDataAccess-backed append helper.");
            AssertContains(bridgeMfc, "GetResultSizeDisplayInfo(result)", "Legacy MFC bridge does not yet read the file size through ResultDataAccess.");
            AssertContains(bridgeMfc, "GetResultModifiedDate(result)", "Legacy MFC bridge does not yet read the modified date through the current ResultDataAccess-backed metadata-display helper.");
            AssertContains(bridgeMfc, "GetResultVersion(result)", "Legacy MFC bridge does not yet read the version through ResultDataAccess.");
            AssertContains(bridgeMfc, "AppendTextLineToHyperEdit(GetResultError(result), hyerEdit);", "Legacy MFC bridge does not yet read the error text through the current ResultDataAccess-backed append helper.");

            AssertContains(resultAccess, "resultDataNet.Path = convertString(GetResultPath(result).c_str());", "ResultDataAccess does not yet read the file path through the centralized ResultDataNet projection helper.");
            AssertContains(resultAccess, "resultDataNet.Size = GetResultSize(result);", "ResultDataAccess does not yet read the file size through the centralized ResultDataNet projection helper.");
            AssertContains(resultAccess, "resultDataNet.ModifiedDate = convertString(GetResultModifiedDate(result).c_str());", "ResultDataAccess does not yet read the modified date through the centralized ResultDataNet projection helper.");
            AssertContains(resultAccess, "resultDataNet.Version = convertString(GetResultVersion(result).c_str());", "ResultDataAccess does not yet read the version through the centralized ResultDataNet projection helper.");
            AssertContains(resultAccess, "resultDataNet.Error = convertString(GetResultError(result).c_str());", "ResultDataAccess does not yet read the error text through the centralized ResultDataNet projection helper.");
            AssertContains(resultAccess, "AssignResultCoreToNet(TResultDataNet resultDataNet, const ResultData& result, TStringConverter convertString)", "ResultDataAccess does not yet expose the centralized core-field ResultDataNet assignment helper.");
            AssertContains(resultAccess, "AssignResultDigestsToNet(TResultDataNet resultDataNet, const ResultData& result, TStringConverter convertString)", "ResultDataAccess does not yet expose the centralized digest ResultDataNet assignment helper.");
            AssertContains(resultAccess, "TResultDataNet resultDataNet = AssignResultCoreToNet<TResultDataNet, TResultStateNet>(TResultDataNet(), result, convertString);", "ResultDataAccess does not yet route ResultDataNet projection through the centralized core assignment helper.");
            AssertContains(resultAccess, "return AssignResultDigestsToNet(resultDataNet, result, convertString);", "ResultDataAccess does not yet route ResultDataNet projection through the centralized digest assignment helper.");

            AssertContains(bridgeWui, "#include \"Common/ResultDataAccess.h\"", "WinUI bridge does not yet consume the neutral result-data access seam.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route non-digest reads through the centralized ResultDataNet projection helper.");

            AssertContains(bridgeUwp, "#include \"Common/ResultDataAccess.h\"", "UWP bridge does not yet consume the neutral result-data access seam.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route non-digest reads through the centralized ResultDataNet projection helper.");

            AssertContains(filesHashDlg, "#include \"Common/ResultDataAccess.h\"", "Legacy MFC search flow does not yet consume the neutral result-data access seam.");
            AssertContains(filesHashDlg, "VisitPathAndDigestMatchingResults(GetThreadDataResults(m_thrdData), tstrFileToFind, tstrHashToFind, [&](const ResultData& result)", "Legacy MFC search flow does not yet read result paths through the neutral ResultDataAccess path-match seam.");
        }, failures);

        Run("Phase 4 routes non-digest ResultData writes through dedicated ResultDataAccess setters", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string engine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");

            AssertContains(resultAccess, "SetResultPath(ResultData& result, const sunjwbase::tstring& path)", "ResultDataAccess does not yet expose the neutral path setter introduced in phase 4.");
            AssertContains(resultAccess, "SetResultSize(ResultData& result, uint64_t size)", "ResultDataAccess does not yet expose the neutral size setter introduced in phase 4.");
            AssertContains(resultAccess, "SetResultModifiedDate(ResultData& result, const sunjwbase::tstring& modifiedDate)", "ResultDataAccess does not yet expose the neutral modified-date setter introduced in phase 4.");
            AssertContains(resultAccess, "SetResultVersion(ResultData& result, const sunjwbase::tstring& version)", "ResultDataAccess does not yet expose the neutral version setter introduced in phase 4.");
            AssertContains(resultAccess, "SetResultError(ResultData& result, const sunjwbase::tstring& errorText)", "ResultDataAccess does not yet expose the neutral error setter introduced in phase 4.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).path = path;", "ResultDataAccess path setter does not yet route through the current ResultData field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).size = size;", "ResultDataAccess size setter does not yet route through the current ResultData field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).modifiedDate = modifiedDate;", "ResultDataAccess modified-date setter does not yet route through the current ResultData field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).version = version;", "ResultDataAccess version setter does not yet route through the current ResultData field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).error = errorText;", "ResultDataAccess error setter does not yet route through the current ResultData field.");

            AssertContains(engine, "#include \"Common/ResultDataAccess.h\"", "HashEngine.cpp does not yet consume the neutral ResultData access seam.");
            AssertContains(engine, "SetResultPath(result, path);", "HashEngine does not yet route result path writes through ResultDataAccess.");
            AssertContains(engine, "SetResultModifiedDate(result, osFile.getModifiedTimeFormat());", "HashEngine does not yet route modified-date writes through ResultDataAccess.");
            AssertContains(engine, "SetResultSize(result, fsize);", "HashEngine does not yet route size writes through ResultDataAccess.");
            AssertContains(engine, "SetResultVersion(result, tstrFileVersion);", "HashEngine does not yet route version writes through ResultDataAccess.");
            AssertContains(engine, "SetResultError(result, errorText);", "HashEngine does not yet route error writes through ResultDataAccess.");
            AssertContains(engine, "*resultPath = GetResultPath(result).c_str();", "HashEngine does not yet route file-attempt path binding through ResultDataAccess.");
        }, failures);

        Run("Phase 4 routes ResultState reads and writes through the neutral ResultDataAccess seam", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string engine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(resultAccess, "GetResultState(const ResultData& result)", "ResultDataAccess does not yet expose the neutral ResultState getter introduced in phase 4.");
            AssertContains(resultAccess, "SetResultState(ResultData& result, ResultState resultState)", "ResultDataAccess does not yet expose the neutral ResultState setter introduced in phase 4.");
            AssertContains(resultAccess, "return GetResultCoreState(result).state;", "ResultDataAccess ResultState getter does not yet route through the current ResultData field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).state = resultState;", "ResultDataAccess ResultState setter does not yet route through the current ResultData field.");

            AssertContains(engine, "SetResultState(result, RESULT_PATH);", "HashEngine does not yet route path-state writes through ResultDataAccess.");
            AssertContains(engine, "SetResultState(result, RESULT_NONE);", "HashEngine does not yet route none-state writes through ResultDataAccess.");
            AssertContains(engine, "SetResultState(result, RESULT_META);", "HashEngine does not yet route meta-state writes through ResultDataAccess.");
            AssertContains(engine, "SetResultState(result, RESULT_ALL);", "HashEngine does not yet route hash-state writes through ResultDataAccess.");
            AssertContains(engine, "SetResultState(result, RESULT_ERROR);", "HashEngine does not yet route error-state writes through ResultDataAccess.");

            AssertContains(bridgeMfc, "ResultState resultState = GetResultState(result);", "Legacy MFC renderer does not yet read ResultState through ResultDataAccess.");
            AssertDoesNotContain(bridgeMfc, "if (result.enumState == RESULT_NONE)", "Legacy MFC renderer still branches directly on ResultData::enumState.");

            AssertContains(resultAccess, "resultDataNet.EnumState = ConvertResultStateToNet<TResultStateNet>(GetResultState(result));", "ResultDataAccess does not yet route managed ResultState projection through the centralized ResultDataNet projection helper.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet read ResultState through the centralized ResultDataNet projection helper.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet read ResultState through the centralized ResultDataNet projection helper.");
        }, failures);

        Run("Phase 5 routes MFC result-section rendering policy through dedicated ResultDataAccess helpers", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(resultAccess, "struct ResultRenderPolicy", "ResultDataAccess does not yet expose the grouped ResultState render-policy structure.");
            AssertContains(resultAccess, "GetResultRenderPolicy(ResultState resultState)", "ResultDataAccess does not yet expose the grouped ResultState render-policy helper.");
            AssertContains(resultAccess, "IsResultStateNone(ResultState resultState)", "ResultDataAccess does not yet expose the neutral ResultState-empty helper for MFC rendering.");
            AssertContains(resultAccess, "ShouldRenderResultFileName(ResultState resultState)", "ResultDataAccess does not yet expose the file-name render-policy helper.");
            AssertContains(resultAccess, "ShouldRenderResultMeta(ResultState resultState)", "ResultDataAccess does not yet expose the metadata render-policy helper.");
            AssertContains(resultAccess, "ShouldRenderResultHash(ResultState resultState)", "ResultDataAccess does not yet expose the hash render-policy helper.");
            AssertContains(resultAccess, "ShouldRenderResultError(ResultState resultState)", "ResultDataAccess does not yet expose the error render-policy helper.");
            AssertContains(resultAccess, "ShouldAppendResultTrailingLineBreak(ResultState resultState)", "ResultDataAccess does not yet expose the trailing-line-break render-policy helper.");
            AssertContains(resultAccess, "return GetResultRenderPolicy(resultState).renderFileName;", "ResultDataAccess file-name render-policy helper does not yet route through the grouped render policy.");
            AssertContains(resultAccess, "return GetResultRenderPolicy(resultState).renderMeta;", "ResultDataAccess metadata render-policy helper does not yet route through the grouped render policy.");
            AssertContains(resultAccess, "return GetResultRenderPolicy(resultState).renderHash;", "ResultDataAccess hash render-policy helper does not yet route through the grouped render policy.");
            AssertContains(resultAccess, "return GetResultRenderPolicy(resultState).renderError;", "ResultDataAccess error render-policy helper does not yet route through the grouped render policy.");
            AssertContains(resultAccess, "return GetResultRenderPolicy(resultState).appendTrailingLineBreak;", "ResultDataAccess trailing-line-break helper does not yet route through the grouped render policy.");

            AssertContains(bridgeMfc, "if (IsResultStateNone(resultState))", "Legacy MFC renderer does not yet route empty-state checks through the ResultDataAccess render-policy helper.");
            AssertContains(bridgeMfc, "if (ShouldAppendResultTrailingLineBreak(resultState))", "Legacy MFC renderer does not yet route trailing-line-break checks through the ResultDataAccess render-policy helper.");

            AssertDoesNotContain(bridgeMfc, "if (resultState == RESULT_NONE)", "Legacy MFC renderer still performs inline empty-state checks instead of using the ResultDataAccess helper.");
            AssertDoesNotContain(bridgeMfc, "if (resultState != RESULT_ALL &&", "Legacy MFC renderer still performs inline trailing-break checks instead of using the ResultDataAccess helper.");
        }, failures);

        Run("Phase 5 routes MFC result-section iteration through a centralized ResultDataAccess visitor", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(resultAccess, "enum ResultRenderSectionType", "ResultDataAccess does not yet expose the result render-section enum introduced in phase 5.");
            AssertContains(resultAccess, "RESULT_RENDER_SECTION_FILE_NAME", "ResultDataAccess is missing the file-name render section.");
            AssertContains(resultAccess, "RESULT_RENDER_SECTION_META", "ResultDataAccess is missing the metadata render section.");
            AssertContains(resultAccess, "RESULT_RENDER_SECTION_HASH", "ResultDataAccess is missing the hash render section.");
            AssertContains(resultAccess, "RESULT_RENDER_SECTION_ERROR", "ResultDataAccess is missing the error render section.");
            AssertContains(resultAccess, "template<typename TResultRenderSectionVisitor>", "ResultDataAccess does not yet expose the render-section visitor template.");
            AssertContains(resultAccess, "VisitRenderableResultSections(ResultState resultState, TResultRenderSectionVisitor visitor)", "ResultDataAccess does not yet expose the centralized render-section visitor helper.");
            AssertContains(resultAccess, "visitor(RESULT_RENDER_SECTION_FILE_NAME)", "ResultDataAccess render-section visitor does not yet route file-name rendering through the centralized visitor path.");
            AssertContains(resultAccess, "visitor(RESULT_RENDER_SECTION_META)", "ResultDataAccess render-section visitor does not yet route metadata rendering through the centralized visitor path.");
            AssertContains(resultAccess, "visitor(RESULT_RENDER_SECTION_HASH)", "ResultDataAccess render-section visitor does not yet route hash rendering through the centralized visitor path.");
            AssertContains(resultAccess, "visitor(RESULT_RENDER_SECTION_ERROR)", "ResultDataAccess render-section visitor does not yet route error rendering through the centralized visitor path.");
            AssertContains(resultAccess, "DispatchResultRenderSectionByType(ResultRenderSectionType renderSection, TFileNameAction onFileName, TMetaAction onMeta, THashAction onHash, TErrorAction onError)", "ResultDataAccess does not yet expose the centralized render-section dispatch helper.");

            AssertContains(bridgeMfc, "VisitRenderableResultSections(resultState, [&](ResultRenderSectionType renderSection)", "UIBridgeMFC does not yet route result-section iteration through the centralized render-section visitor.");
            AssertContains(bridgeMfc, "DispatchResultRenderSectionByType(renderSection, [&]()", "UIBridgeMFC does not yet consume render sections through the centralized dispatch seam.");
            AssertDoesNotContain(bridgeMfc, "if (ShouldRenderResultFileName(resultState))", "UIBridgeMFC still performs inline file-name render-policy checks instead of using the centralized visitor.");
            AssertDoesNotContain(bridgeMfc, "if (ShouldRenderResultMeta(resultState))", "UIBridgeMFC still performs inline metadata render-policy checks instead of using the centralized visitor.");
            AssertDoesNotContain(bridgeMfc, "if (ShouldRenderResultHash(resultState))", "UIBridgeMFC still performs inline hash render-policy checks instead of using the centralized visitor.");
            AssertDoesNotContain(bridgeMfc, "if (ShouldRenderResultError(resultState))", "UIBridgeMFC still performs inline error render-policy checks instead of using the centralized visitor.");
        }, failures);

        Run("Phase 5 routes MFC render-section dispatch through a dedicated bridge helper", () =>
        {
            string bridgeMfcHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");

            AssertContains(bridgeMfcHeader, "AppendResultRenderSectionToHyperEdit(const ResultData& result,", "UIBridgeMFC does not yet expose the dedicated render-section dispatch helper.");
            AssertContains(bridgeMfc, "void UIBridgeMFC::AppendResultRenderSectionToHyperEdit(const ResultData& result,", "UIBridgeMFC does not yet implement the dedicated render-section dispatch helper.");
            AssertContains(resultAccess, "DispatchResultRenderSectionByType(ResultRenderSectionType renderSection, TFileNameAction onFileName, TMetaAction onMeta, THashAction onHash, TErrorAction onError)", "ResultDataAccess does not yet expose the centralized render-section dispatch helper.");
            AssertContains(bridgeMfc, "DispatchResultRenderSectionByType(renderSection, [&]()", "UIBridgeMFC render-section dispatch helper does not yet route sections through the centralized dispatch seam.");
            AssertContains(bridgeMfc, "AppendResultRenderSectionToHyperEdit(result, renderSection, uppercase, hyerEdit);", "UIBridgeMFC does not yet route render-section dispatch through the dedicated helper.");
            AssertDoesNotContain(bridgeMfc, "switch (renderSection)\r\n\t\t{\r\n\t\tcase RESULT_RENDER_SECTION_FILE_NAME:", "UIBridgeMFC still keeps the render-section dispatch switch inline inside AppendResultToHyperEdit instead of using the dedicated helper.");
        }, failures);

        Run("Phase 5 routes MFC main-hyperedit refresh flow through a dedicated bridge helper", () =>
        {
            string bridgeMfcHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(bridgeMfcHeader, "template<typename TAppendAction>", "UIBridgeMFC does not yet expose the templated main-hyperedit refresh helper.");
            AssertContains(bridgeMfcHeader, "void PostThreadInfoMessage(WPARAM wParam, LPARAM lParam = 0)", "UIBridgeMFC does not yet expose the centralized thread-info post-message helper.");
            AssertContains(bridgeMfcHeader, "::PostMessage(m_hWnd, WM_THREAD_INFO, wParam, lParam);", "UIBridgeMFC thread-info post-message helper does not yet wrap the shared WM_THREAD_INFO dispatch.");
            AssertContains(bridgeMfcHeader, "void RefreshMainHyperEdit()", "UIBridgeMFC does not yet expose the dedicated main-hyperedit refresh-message helper.");
            AssertContains(bridgeMfcHeader, "PostThreadInfoMessage(WP_REFRESH_TEXT);", "UIBridgeMFC main-hyperedit refresh-message helper does not yet route refresh notifications through the centralized thread-info seam.");
            AssertContains(bridgeMfcHeader, "void UpdateMainHyperEdit(TAppendAction appendAction, bool refreshAfterUpdate = false)", "UIBridgeMFC does not yet expose the generalized main-hyperedit update helper.");
            AssertContains(bridgeMfcHeader, "void AppendToMainHyperEditAndRefresh(TAppendAction appendAction)", "UIBridgeMFC does not yet expose the dedicated main-hyperedit refresh helper.");
            AssertContains(bridgeMfcHeader, "appendAction(m_mainHyperEdit);", "UIBridgeMFC main-hyperedit refresh helper does not yet forward the hyper-edit instance through the callback.");
            AssertContains(bridgeMfcHeader, "UpdateMainHyperEdit(appendAction, true);", "UIBridgeMFC main-hyperedit refresh helper does not yet compose through the generalized update helper.");
            AssertContains(bridgeMfcHeader, "RefreshMainHyperEdit();", "UIBridgeMFC main-hyperedit refresh helper does not yet centralize refresh notifications through the dedicated refresh-message helper.");
            AssertContains(bridgeMfcHeader, "struct MainHyperEditSnapshot", "UIBridgeMFC does not yet expose the preparing-state hyperedit snapshot structure.");
            AssertContains(bridgeMfcHeader, "static MainHyperEditSnapshot CaptureHyperEditSnapshot(CHyperEditHash *hyperEdit);", "UIBridgeMFC does not yet expose the hyperedit snapshot capture helper.");
            AssertContains(bridgeMfcHeader, "static void RestoreHyperEditSnapshot(const MainHyperEditSnapshot& snapshot,", "UIBridgeMFC does not yet expose the hyperedit snapshot restore helper.");
            AssertContains(bridgeMfcHeader, "void AppendResultSectionAndRefresh(const ResultData& result,", "UIBridgeMFC does not yet expose the section-and-refresh helper.");
            AssertContains(bridgeMfcHeader, "AppendResultRenderSectionToHyperEdit(result, renderSection, uppercase, hyperEdit);", "UIBridgeMFC section-and-refresh helper does not yet dispatch through the centralized render-section helper.");

            AssertContains(bridgeMfc, "AppendResultSectionAndRefresh(result, RESULT_RENDER_SECTION_FILE_NAME, false);", "UIBridgeMFC does not yet route file-name rendering through the dedicated refresh helper.");
            AssertContains(bridgeMfc, "AppendResultSectionAndRefresh(result, RESULT_RENDER_SECTION_META, false);", "UIBridgeMFC does not yet route file-meta rendering through the dedicated refresh helper.");
            AssertContains(bridgeMfc, "AppendResultSectionAndRefresh(result, RESULT_RENDER_SECTION_HASH, uppercase);", "UIBridgeMFC does not yet route file-hash rendering through the dedicated refresh helper.");
            AssertContains(bridgeMfc, "AppendResultSectionAndRefresh(result, RESULT_RENDER_SECTION_ERROR, false);", "UIBridgeMFC does not yet route file-error rendering through the dedicated refresh helper.");
            AssertContains(bridgeMfc, "PostThreadInfoMessage(WP_WORKING);", "UIBridgeMFC does not yet route preparing-state notifications through the thread-info helper.");
            AssertContains(bridgeMfc, "PostThreadInfoMessage(WP_STOPPED);", "UIBridgeMFC does not yet route stop notifications through the thread-info helper.");
            AssertContains(bridgeMfc, "PostThreadInfoMessage(WP_FINISHED);", "UIBridgeMFC does not yet route finish notifications through the thread-info helper.");
            AssertContains(bridgeMfc, "PostThreadInfoMessage(WP_PROG_WHOLE, value);", "UIBridgeMFC does not yet route whole-progress notifications through the thread-info helper.");
            AssertContains(bridgeMfc, "}, true);", "UIBridgeMFC does not yet route preparing refresh notifications through the generalized main-hyperedit update helper.");
            AssertContains(bridgeMfc, "UpdateMainHyperEdit([&](CHyperEditHash *hyperEdit)", "UIBridgeMFC does not yet route preparing/removal text-buffer mutations through the generalized main-hyperedit update helper.");
            AssertContains(bridgeMfc, "m_preparingSnapshot = CaptureHyperEditSnapshot(hyperEdit);", "UIBridgeMFC does not yet capture the preparing-state hyperedit snapshot through the dedicated helper.");
            AssertContains(bridgeMfc, "RestoreHyperEditSnapshot(m_preparingSnapshot, hyperEdit);", "UIBridgeMFC does not yet restore the preparing-state hyperedit snapshot through the dedicated helper.");

            AssertDoesNotContain(bridgeMfc, "lockData();\r\n\t{\r\n\t\tAppendFileNameToHyperEdit(result, m_mainHyperEdit);", "UIBridgeMFC still inlines the showFileName refresh flow instead of using the dedicated helper.");
            AssertDoesNotContain(bridgeMfc, "lockData();\r\n\t{\r\n\t\tAppendFileMetaToHyperEdit(result, m_mainHyperEdit);", "UIBridgeMFC still inlines the showFileMeta refresh flow instead of using the dedicated helper.");
            AssertDoesNotContain(bridgeMfc, "lockData();\r\n\t{\r\n\t\tAppendFileHashToHyperEdit(result, uppercase, m_mainHyperEdit);", "UIBridgeMFC still inlines the showFileHash refresh flow instead of using the dedicated helper.");
            AssertDoesNotContain(bridgeMfc, "lockData();\r\n\t{\r\n\t\tAppendFileErrToHyperEdit(result, m_mainHyperEdit);", "UIBridgeMFC still inlines the showFileErr refresh flow instead of using the dedicated helper.");
            AssertDoesNotContain(bridgeMfc, "::PostMessage(m_hWnd, WM_THREAD_INFO, WP_REFRESH_TEXT, 0);", "UIBridgeMFC still posts refresh notifications inline instead of using the dedicated refresh-message helper.");
            AssertDoesNotContain(bridgeMfc, "lockData();\r\n\t{\r\n\t\tm_tstrNoPreparing = m_mainHyperEdit->GetTextBuffer().GetBuffer();", "UIBridgeMFC still mutates the preparing buffer inline instead of using the generalized main-hyperedit update helper.");
            AssertDoesNotContain(bridgeMfc, "hyperEdit->ClearTextBuffer();\r\n\t\thyperEdit->AppendTextToBuffer(m_tstrNoPreparing.c_str());", "UIBridgeMFC still restores the preparing-state snapshot inline instead of using the dedicated snapshot helper.");
        }, failures);

        Run("Phase 5 routes managed bridge result projection dispatch through dedicated bridge helpers", () =>
        {
            string managedDispatch = ReadRepoFile(repoRoot, @"trunk\source\Common\ManagedBridgeDispatch.h");
            string bridgeWuiHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.h");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");

            AssertContains(resultAccess, "template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter, typename TResultHandler>", "ResultDataAccess does not yet expose the centralized project-and-dispatch helper template.");
            AssertContains(resultAccess, "ProjectAndDispatchResult(const ResultData& result, TStringConverter convertString, TResultHandler resultHandler)", "ResultDataAccess does not yet expose the centralized project-and-dispatch helper.");
            AssertContains(resultAccess, "resultHandler(ProjectResultDataToNet<TResultDataNet, TResultStateNet>(result, convertString));", "ResultDataAccess project-and-dispatch helper does not yet compose projection and dispatch through the centralized projection seam.");
            AssertContains(managedDispatch, "TResultDataNet resultDataNet = ProjectResultDataToNet<TResultDataNet, TResultStateNet>(result, convertString);", "Common managed-bridge dispatch header does not yet materialize projected managed results through the centralized projection seam.");
            AssertDoesNotContain(managedDispatch, "ProjectManagedBridgeResultAndDispatch(const ResultData& result, TStringConverter convertString, TResultHandler resultHandler)", "Common managed-bridge dispatch header still keeps the redundant managed projection-dispatch wrapper.");

            AssertContains(bridgeWuiHeader, "#include \"Common/ManagedBridgeDispatch.h\"", "WinUI bridge header does not yet include the common managed-bridge dispatch header.");
            AssertDoesNotContain(bridgeWuiHeader, "#include \"Common/ManagedBridgeHelpers.h\"", "WinUI bridge header still depends on the deprecated managed-bridge helper header.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route showFile* methods through the common managed projection-dispatch helper.");
            AssertContains(bridgeWui, "m_uiBridgeDelegates->ShowFileName(resultDataNet);", "WinUI bridge no longer forwards projected file-name results through the current delegate path.");
            AssertContains(bridgeWui, "m_uiBridgeDelegates->ShowFileMeta(resultDataNet);", "WinUI bridge no longer forwards projected file-meta results through the current delegate path.");
            AssertContains(bridgeWui, "m_uiBridgeDelegates->ShowFileHash(resultDataNet, hashUppercase);", "WinUI bridge no longer forwards projected file-hash results through the current delegate path.");
            AssertContains(bridgeWui, "m_uiBridgeDelegates->ShowFileErr(resultDataNet);", "WinUI bridge no longer forwards projected file-error results through the current delegate path.");
            AssertDoesNotContain(bridgeWuiHeader, "void ProjectManagedResultAndDispatch(const ResultData& result, TResultHandler resultHandler)", "WinUI bridge still keeps the local managed projection-dispatch template instead of using the centralized seam.");
            AssertDoesNotContain(bridgeWui, "ResultDataNet resultDataNet = ConvertResultDataToNet(result);", "WinUI bridge still inlines projected result creation inside showFile* methods instead of using the dedicated helper.");

            AssertContains(bridgeUwpHeader, "#include \"Common/ManagedBridgeDispatch.h\"", "UWP bridge header does not yet include the common managed-bridge dispatch header.");
            AssertDoesNotContain(bridgeUwpHeader, "#include \"Common/ManagedBridgeHelpers.h\"", "UWP bridge header still depends on the deprecated managed-bridge helper header.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route showFile* methods through the common managed projection-dispatch helper.");
            AssertContains(bridgeUwp, "m_uiBridgeDelegate->ShowFileName(resultDataNet);", "UWP bridge no longer forwards projected file-name results through the current delegate path.");
            AssertContains(bridgeUwp, "m_uiBridgeDelegate->ShowFileMeta(resultDataNet);", "UWP bridge no longer forwards projected file-meta results through the current delegate path.");
            AssertContains(bridgeUwp, "m_uiBridgeDelegate->ShowFileHash(resultDataNet, hashUppercase);", "UWP bridge no longer forwards projected file-hash results through the current delegate path.");
            AssertContains(bridgeUwp, "m_uiBridgeDelegate->ShowFileErr(resultDataNet);", "UWP bridge no longer forwards projected file-error results through the current delegate path.");
            AssertDoesNotContain(bridgeUwpHeader, "void ProjectManagedResultAndDispatch(const ResultData& result, TResultHandler resultHandler)", "UWP bridge still keeps the local managed projection-dispatch template instead of using the centralized seam.");
            AssertDoesNotContain(bridgeUwp, "ResultDataNet resultDataNet = ConvertResultDataToNet(result);", "UWP bridge still inlines projected result creation inside showFile* methods instead of using the dedicated helper.");
        }, failures);

        Run("Phase 5 routes managed showFile forwarding through dedicated bridge dispatch helpers", () =>
        {
            string managedDispatch = ReadRepoFile(repoRoot, @"trunk\source\Common\ManagedBridgeDispatch.h");
            string bridgeWuiHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.h");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(managedDispatch, "enum ManagedResultDispatchType", "Common managed-bridge dispatch header does not yet expose the dedicated managed result-dispatch type.");
            AssertContains(managedDispatch, "DispatchManagedResultByType(ManagedResultDispatchType dispatchType, TResultDataNet resultDataNet, bool uppercase", "Common managed-bridge dispatch header does not yet expose the centralized managed result-dispatch helper.");
            AssertContains(managedDispatch, "DispatchManagedBridgeResultByType(const ResultData& result, ManagedResultDispatchType dispatchType, bool uppercase", "Common managed-bridge dispatch header does not yet expose the shared managed result-dispatch wrapper.");
            AssertContains(managedDispatch, "DispatchManagedResultByType(dispatchType, resultDataNet, uppercase, onFileName, onFileMeta, onFileHash, onFileError);", "Common managed-bridge dispatch header does not yet compose managed result dispatch through the lower-level dispatch seam.");
            AssertContains(bridgeWuiHeader, "#include \"Common/ManagedBridgeDispatch.h\"", "WinUI bridge header does not yet include the common managed-bridge dispatch header.");
            AssertDoesNotContain(bridgeWuiHeader, "#include \"Common/ManagedBridgeHelpers.h\"", "WinUI bridge header still includes the deprecated managed-bridge helper header.");
            AssertContains(bridgeWuiHeader, "void DispatchProjectedResultToDelegate(const ResultData& result, ManagedResultDispatchType dispatchType, bool uppercase = false);", "WinUI bridge does not yet expose the dedicated managed result-dispatch helper.");
            AssertContains(bridgeWui, "void UIBridgeWUI::DispatchProjectedResultToDelegate(const ResultData& result, ManagedResultDispatchType dispatchType, bool uppercase)", "WinUI bridge does not yet implement the dedicated managed result-dispatch helper.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge managed result-dispatch helper does not yet route delegate forwarding through the common managed-bridge helper.");
            AssertContains(bridgeWui, "DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_NAME);", "WinUI bridge does not yet route file-name forwarding through the dedicated managed result-dispatch helper.");
            AssertContains(bridgeWui, "DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_META);", "WinUI bridge does not yet route file-meta forwarding through the dedicated managed result-dispatch helper.");
            AssertContains(bridgeWui, "DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_HASH, uppercase);", "WinUI bridge does not yet route file-hash forwarding through the dedicated managed result-dispatch helper.");
            AssertContains(bridgeWui, "DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_ERROR);", "WinUI bridge does not yet route file-error forwarding through the dedicated managed result-dispatch helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::showFileName(const ResultData& result)\r\n{\r\n\tProjectManagedResultAndDispatch(result, [&](ResultDataNet resultDataNet)", "WinUI bridge still inlines file-name projection-dispatch instead of using the dedicated managed result-dispatch helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::showFileMeta(const ResultData& result)\r\n{\r\n\tProjectManagedResultAndDispatch(result, [&](ResultDataNet resultDataNet)", "WinUI bridge still inlines file-meta projection-dispatch instead of using the dedicated managed result-dispatch helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::showFileHash(const ResultData& result, bool uppercase)\r\n{\r\n\tProjectManagedResultAndDispatch(result, [&](ResultDataNet resultDataNet)", "WinUI bridge still inlines file-hash projection-dispatch instead of using the dedicated managed result-dispatch helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::showFileErr(const ResultData& result)\r\n{\r\n\tProjectManagedResultAndDispatch(result, [&](ResultDataNet resultDataNet)", "WinUI bridge still inlines file-error projection-dispatch instead of using the dedicated managed result-dispatch helper.");

            AssertContains(bridgeUwpHeader, "#include \"Common/ManagedBridgeDispatch.h\"", "UWP bridge header does not yet include the common managed-bridge dispatch header.");
            AssertDoesNotContain(bridgeUwpHeader, "#include \"Common/ManagedBridgeHelpers.h\"", "UWP bridge header still includes the deprecated managed-bridge helper header.");
            AssertContains(bridgeUwpHeader, "void DispatchProjectedResultToDelegate(const ResultData& result, ManagedResultDispatchType dispatchType, bool uppercase = false);", "UWP bridge does not yet expose the dedicated managed result-dispatch helper.");
            AssertContains(bridgeUwp, "void UIBridgeUwp::DispatchProjectedResultToDelegate(const ResultData& result, ManagedResultDispatchType dispatchType, bool uppercase)", "UWP bridge does not yet implement the dedicated managed result-dispatch helper.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge managed result-dispatch helper does not yet route delegate forwarding through the common managed-bridge helper.");
            AssertContains(bridgeUwp, "DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_NAME);", "UWP bridge does not yet route file-name forwarding through the dedicated managed result-dispatch helper.");
            AssertContains(bridgeUwp, "DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_META);", "UWP bridge does not yet route file-meta forwarding through the dedicated managed result-dispatch helper.");
            AssertContains(bridgeUwp, "DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_HASH, uppercase);", "UWP bridge does not yet route file-hash forwarding through the dedicated managed result-dispatch helper.");
            AssertContains(bridgeUwp, "DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_ERROR);", "UWP bridge does not yet route file-error forwarding through the dedicated managed result-dispatch helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::showFileName(const ResultData& result)\r\n{\r\n\tProjectManagedResultAndDispatch(result, [&](ResultDataNet resultDataNet)", "UWP bridge still inlines file-name projection-dispatch instead of using the dedicated managed result-dispatch helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::showFileMeta(const ResultData& result)\r\n{\r\n\tProjectManagedResultAndDispatch(result, [&](ResultDataNet resultDataNet)", "UWP bridge still inlines file-meta projection-dispatch instead of using the dedicated managed result-dispatch helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::showFileHash(const ResultData& result, bool uppercase)\r\n{\r\n\tProjectManagedResultAndDispatch(result, [&](ResultDataNet resultDataNet)", "UWP bridge still inlines file-hash projection-dispatch instead of using the dedicated managed result-dispatch helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::showFileErr(const ResultData& result)\r\n{\r\n\tProjectManagedResultAndDispatch(result, [&](ResultDataNet resultDataNet)", "UWP bridge still inlines file-error projection-dispatch instead of using the dedicated managed result-dispatch helper.");
        }, failures);

        Run("Phase 5 routes managed bridge delegate forwarding through dedicated bridge helpers", () =>
        {
            string managedDispatch = ReadRepoFile(repoRoot, @"trunk\source\Common\ManagedBridgeDispatch.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(managedDispatch, "DispatchManagedBridgeDelegateActionByType(ManagedDelegateActionType actionType, int value", "Common managed-bridge dispatch header does not yet expose the shared bridge-level delegate-action wrapper.");
            AssertContains(managedDispatch, "DispatchManagedBridgeDelegateQueryByType(ManagedDelegateQueryType queryType, TProgMaxQuery queryProgMax)", "Common managed-bridge dispatch header does not yet expose the shared bridge-level delegate-query wrapper.");
            AssertDoesNotContain(managedDispatch, "ProjectManagedBridgeResultAndDispatch", "Common managed-bridge dispatch header still keeps the redundant bridge-level projection wrapper.");
            AssertContains(bridgeWui, "DispatchManagedBridgeDelegateActionByType(actionType, value, [&]()", "WinUI bridge does not yet route delegate actions through the common forwarding helper.");
            AssertContains(bridgeWui, "return DispatchManagedBridgeDelegateQueryByType<int>(queryType, [&]()", "WinUI bridge does not yet route delegate queries through the common forwarding helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::preparingCalc()\r\n{\r\n\tm_uiBridgeDelegates->PreparingCalc();\r\n}", "WinUI bridge still forwards PreparingCalc inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::removePreparingCalc()\r\n{\r\n\tm_uiBridgeDelegates->RemovePreparingCalc();\r\n}", "WinUI bridge still forwards RemovePreparingCalc inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::calcStop()\r\n{\r\n\tm_uiBridgeDelegates->CalcStop();\r\n}", "WinUI bridge still forwards CalcStop inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::calcFinish()\r\n{\r\n\tm_uiBridgeDelegates->CalcFinish();\r\n}", "WinUI bridge still forwards CalcFinish inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeWui, "int UIBridgeWUI::getProgMax()\r\n{\r\n\treturn m_uiBridgeDelegates->GetProgMax();\r\n}", "WinUI bridge still forwards GetProgMax inline instead of using the dedicated delegate-query helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::updateProgWhole(int value)\r\n{\r\n\tm_uiBridgeDelegates->UpdateProgWhole(value);\r\n}", "WinUI bridge still forwards UpdateProgWhole inline instead of using the dedicated delegate-action helper.");

            AssertContains(bridgeUwp, "DispatchManagedBridgeDelegateActionByType(actionType, value, [&]()", "UWP bridge does not yet route delegate actions through the common forwarding helper.");
            AssertContains(bridgeUwp, "return DispatchManagedBridgeDelegateQueryByType<int>(queryType, [&]()", "UWP bridge does not yet route delegate queries through the common forwarding helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::preparingCalc()\r\n{\r\n\tm_uiBridgeDelegate->PreparingCalc();\r\n}", "UWP bridge still forwards PreparingCalc inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::removePreparingCalc()\r\n{\r\n\tm_uiBridgeDelegate->RemovePreparingCalc();\r\n}", "UWP bridge still forwards RemovePreparingCalc inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::calcStop()\r\n{\r\n\tm_uiBridgeDelegate->CalcStop();\r\n}", "UWP bridge still forwards CalcStop inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::calcFinish()\r\n{\r\n\tm_uiBridgeDelegate->CalcFinish();\r\n}", "UWP bridge still forwards CalcFinish inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeUwp, "int UIBridgeUwp::getProgMax()\r\n{\r\n\treturn m_uiBridgeDelegate->GetProgMax();\r\n}", "UWP bridge still forwards GetProgMax inline instead of using the dedicated delegate-query helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::updateProgWhole(int value)\r\n{\r\n\tm_uiBridgeDelegate->UpdateProgWhole(value);\r\n}", "UWP bridge still forwards UpdateProgWhole inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(managedDispatch, "ForwardManagedDelegateAction(TDelegateAction delegateAction)", "Common managed-bridge dispatch header still keeps the redundant delegate-action forwarding wrapper.");
            AssertDoesNotContain(managedDispatch, "ForwardManagedDelegateQuery(TDelegateQuery delegateQuery)", "Common managed-bridge dispatch header still keeps the redundant delegate-query forwarding wrapper.");
        }, failures);

        Run("Phase 5 routes managed bridge delegate queries through dedicated query-type dispatch helpers", () =>
        {
            string managedDispatch = ReadRepoFile(repoRoot, @"trunk\source\Common\ManagedBridgeDispatch.h");
            string bridgeWuiHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.h");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(managedDispatch, "enum ManagedDelegateQueryType", "Common managed-bridge dispatch header does not yet expose the dedicated delegate-query type.");
            AssertContains(managedDispatch, "DispatchManagedDelegateQueryByType(ManagedDelegateQueryType queryType, TProgMaxQuery queryProgMax)", "Common managed-bridge dispatch header does not yet expose the centralized delegate-query helper.");
            AssertContains(managedDispatch, "DispatchManagedBridgeDelegateQueryByType(ManagedDelegateQueryType queryType, TProgMaxQuery queryProgMax)", "Common managed-bridge dispatch header does not yet expose the bridge-level delegate-query wrapper.");
            AssertContains(managedDispatch, "return DispatchManagedDelegateQueryByType(queryType, [&]()", "Common managed-bridge dispatch header does not yet compose delegate-query forwarding through the dispatch seam.");
            AssertDoesNotContain(managedDispatch, "return ForwardManagedDelegateQuery<TResult>([&]()", "Common managed-bridge dispatch header still keeps the redundant delegate-query forwarding wrapper in the bridge-level helper.");
            AssertContains(bridgeWuiHeader, "int DispatchDelegateQueryByType(ManagedDelegateQueryType queryType);", "WinUI bridge does not yet expose the dedicated delegate-query dispatch helper.");
            AssertContains(bridgeWui, "int UIBridgeWUI::DispatchDelegateQueryByType(ManagedDelegateQueryType queryType)", "WinUI bridge does not yet implement the dedicated delegate-query dispatch helper.");
            AssertContains(bridgeWui, "return DispatchManagedBridgeDelegateQueryByType<int>(queryType, [&]()", "WinUI bridge delegate-query dispatch helper does not yet route queries through the bridge-level helper.");
            AssertContains(bridgeWui, "return DispatchDelegateQueryByType(MANAGED_DELEGATE_QUERY_PROG_MAX);", "WinUI bridge does not yet route GetProgMax through the dedicated delegate-query dispatch helper.");
            AssertDoesNotContain(bridgeWui, "int UIBridgeWUI::getProgMax()\r\n{\r\n\treturn DispatchDelegateQuery<int>([&]()", "WinUI bridge still keeps GetProgMax's inline delegate-query lambda instead of routing through the dedicated query-type helper.");

            AssertContains(bridgeUwpHeader, "int DispatchDelegateQueryByType(ManagedDelegateQueryType queryType);", "UWP bridge does not yet expose the dedicated delegate-query dispatch helper.");
            AssertContains(bridgeUwp, "int UIBridgeUwp::DispatchDelegateQueryByType(ManagedDelegateQueryType queryType)", "UWP bridge does not yet implement the dedicated delegate-query dispatch helper.");
            AssertContains(bridgeUwp, "return DispatchManagedBridgeDelegateQueryByType<int>(queryType, [&]()", "UWP bridge delegate-query dispatch helper does not yet route queries through the bridge-level helper.");
            AssertContains(bridgeUwp, "return DispatchDelegateQueryByType(MANAGED_DELEGATE_QUERY_PROG_MAX);", "UWP bridge does not yet route GetProgMax through the dedicated delegate-query dispatch helper.");
            AssertDoesNotContain(bridgeUwp, "int UIBridgeUwp::getProgMax()\r\n{\r\n\treturn DispatchDelegateQuery<int>([&]()", "UWP bridge still keeps GetProgMax's inline delegate-query lambda instead of routing through the dedicated query-type helper.");
        }, failures);

        Run("Phase 5 routes managed bridge delegate actions through dedicated action-type dispatch helpers", () =>
        {
            string managedDispatch = ReadRepoFile(repoRoot, @"trunk\source\Common\ManagedBridgeDispatch.h");
            string bridgeWuiHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.h");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(managedDispatch, "enum ManagedDelegateActionType", "Common managed-bridge dispatch header does not yet expose the dedicated delegate-action type.");
            AssertContains(managedDispatch, "DispatchManagedDelegateActionByType(ManagedDelegateActionType actionType, int value", "Common managed-bridge dispatch header does not yet expose the centralized delegate-action helper.");
            AssertContains(managedDispatch, "DispatchManagedBridgeDelegateActionByType(ManagedDelegateActionType actionType, int value", "Common managed-bridge dispatch header does not yet expose the bridge-level delegate-action wrapper.");
            AssertContains(managedDispatch, "DispatchManagedDelegateActionByType(actionType, value, [&]()", "Common managed-bridge dispatch header does not yet compose delegate-action forwarding through the dispatch seam.");
            AssertDoesNotContain(managedDispatch, "ForwardManagedDelegateAction([&]()", "Common managed-bridge dispatch header still keeps the redundant delegate-action forwarding wrapper in the bridge-level helper.");
            AssertContains(bridgeWuiHeader, "void DispatchDelegateActionByType(ManagedDelegateActionType actionType, int value = 0);", "WinUI bridge does not yet expose the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeWui, "void UIBridgeWUI::DispatchDelegateActionByType(ManagedDelegateActionType actionType, int value)", "WinUI bridge does not yet implement the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeWui, "DispatchManagedBridgeDelegateActionByType(actionType, value, [&]()", "WinUI bridge delegate-action dispatch helper does not yet route actions through the bridge-level helper.");
            AssertContains(bridgeWui, "DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_PREPARING_CALC);", "WinUI bridge does not yet route PreparingCalc through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeWui, "DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_REMOVE_PREPARING_CALC);", "WinUI bridge does not yet route RemovePreparingCalc through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeWui, "DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_CALC_STOP);", "WinUI bridge does not yet route CalcStop through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeWui, "DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_CALC_FINISH);", "WinUI bridge does not yet route CalcFinish through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeWui, "DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_UPDATE_PROG_WHOLE, value);", "WinUI bridge does not yet route UpdateProgWhole through the dedicated delegate-action dispatch helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::preparingCalc()\r\n{\r\n\tDispatchDelegateAction([&]()", "WinUI bridge still keeps PreparingCalc's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::removePreparingCalc()\r\n{\r\n\tDispatchDelegateAction([&]()", "WinUI bridge still keeps RemovePreparingCalc's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::calcStop()\r\n{\r\n\tDispatchDelegateAction([&]()", "WinUI bridge still keeps CalcStop's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::calcFinish()\r\n{\r\n\tDispatchDelegateAction([&]()", "WinUI bridge still keeps CalcFinish's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::updateProgWhole(int value)\r\n{\r\n\tDispatchDelegateAction([&]()", "WinUI bridge still keeps UpdateProgWhole's inline delegate-action lambda instead of routing through the dedicated action-type helper.");

            AssertContains(bridgeUwpHeader, "void DispatchDelegateActionByType(ManagedDelegateActionType actionType, int value = 0);", "UWP bridge does not yet expose the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeUwp, "void UIBridgeUwp::DispatchDelegateActionByType(ManagedDelegateActionType actionType, int value)", "UWP bridge does not yet implement the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeDelegateActionByType(actionType, value, [&]()", "UWP bridge delegate-action dispatch helper does not yet route actions through the bridge-level helper.");
            AssertContains(bridgeUwp, "DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_PREPARING_CALC);", "UWP bridge does not yet route PreparingCalc through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeUwp, "DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_REMOVE_PREPARING_CALC);", "UWP bridge does not yet route RemovePreparingCalc through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeUwp, "DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_CALC_STOP);", "UWP bridge does not yet route CalcStop through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeUwp, "DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_CALC_FINISH);", "UWP bridge does not yet route CalcFinish through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeUwp, "DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_UPDATE_PROG_WHOLE, value);", "UWP bridge does not yet route UpdateProgWhole through the dedicated delegate-action dispatch helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::preparingCalc()\r\n{\r\n\tDispatchDelegateAction([&]()", "UWP bridge still keeps PreparingCalc's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::removePreparingCalc()\r\n{\r\n\tDispatchDelegateAction([&]()", "UWP bridge still keeps RemovePreparingCalc's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::calcStop()\r\n{\r\n\tDispatchDelegateAction([&]()", "UWP bridge still keeps CalcStop's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::calcFinish()\r\n{\r\n\tDispatchDelegateAction([&]()", "UWP bridge still keeps CalcFinish's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::updateProgWhole(int value)\r\n{\r\n\tDispatchDelegateAction([&]()", "UWP bridge still keeps UpdateProgWhole's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
        }, failures);

        Run("Phase 5 routes managed bridge text conversion through dedicated bridge helpers", () =>
        {
            string bridgeWuiHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.h");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(bridgeWuiHeader, "static System::String^ ConvertManagedResultText(const TCHAR* resultText);", "WinUI bridge does not yet expose the dedicated managed text-conversion helper.");
            AssertContains(bridgeWui, "return ConvertManagedResultText(resultText);", "WinUI bridge does not yet route projected-result text conversion through the dedicated helper.");
            AssertContains(bridgeWui, "String^ UIBridgeWUI::ConvertManagedResultText(const TCHAR* resultText)", "WinUI bridge does not yet define the dedicated managed text-conversion helper.");
            AssertContains(bridgeWui, "return ConvertTstrToSystemString(resultText);", "WinUI bridge text-conversion helper does not yet reuse the existing CLR string conversion path.");
            AssertDoesNotContain(bridgeWuiHeader, "return ConvertTstrToSystemString(resultText);", "WinUI bridge still keeps the text-conversion lambda inline in the header instead of using the dedicated helper.");

            AssertContains(bridgeUwpHeader, "static Platform::String^ ConvertManagedResultText(const TCHAR* resultText);", "UWP bridge does not yet expose the dedicated managed text-conversion helper.");
            AssertContains(bridgeUwp, "return ConvertManagedResultText(resultText);", "UWP bridge does not yet route projected-result text conversion through the dedicated helper.");
            AssertContains(bridgeUwp, "String^ UIBridgeUwp::ConvertManagedResultText(const TCHAR* resultText)", "UWP bridge does not yet define the dedicated managed text-conversion helper.");
            AssertContains(bridgeUwp, "return ConvertToPlatStr(resultText);", "UWP bridge text-conversion helper does not yet reuse the existing platform string conversion path.");
            AssertDoesNotContain(bridgeUwpHeader, "return ConvertToPlatStr(resultText);", "UWP bridge still keeps the text-conversion lambda inline in the header instead of using the dedicated helper.");
        }, failures);

        Run("Phase 5 removes bridge-local single-result projection wrappers after centralizing dispatch", () =>
        {
            string bridgeWuiHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.h");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertDoesNotContain(bridgeWuiHeader, "ResultDataNet ConvertResultDataToNet(const ResultData& result);", "WinUI bridge still declares the local single-result projection wrapper after centralizing dispatch.");
            AssertDoesNotContain(bridgeWui, "ResultDataNet FilesHashWUI::ConvertResultDataToNet(const ResultData& result)", "WinUI bridge still defines the local single-result projection wrapper after centralizing dispatch.");
            AssertDoesNotContain(bridgeUwpHeader, "static ResultDataNet ConvertResultDataToNet(const ResultData& result);", "UWP bridge still declares the local single-result projection wrapper after centralizing dispatch.");
            AssertDoesNotContain(bridgeUwp, "ResultDataNet UIBridgeUwp::ConvertResultDataToNet(const ResultData& result)", "UWP bridge still defines the local single-result projection wrapper after centralizing dispatch.");
        }, failures);

        Run("Phase 5 routes managed result-list projection through a centralized ResultDataAccess helper", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string clrMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");

            AssertContains(resultAccess, "template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter, typename TResultVisitor>", "ResultDataAccess does not yet expose the centralized result-list projection visitor template.");
            AssertContains(resultAccess, "VisitProjectedResults(const ResultList& resultList, TStringConverter convertString, TResultVisitor visitor)", "ResultDataAccess does not yet expose the centralized result-list projection helper.");
            AssertContains(resultAccess, "VisitProjectedMatchingResults<TResultDataNet, TResultStateNet>(resultList, [&](const ResultData& result)", "ResultDataAccess does not yet route full result-list projection through the centralized matching-projection seam.");
            AssertContains(resultAccess, "template<typename TResultDataNet, typename TResultStateNet, typename TResultPredicate, typename TStringConverter, typename TResultVisitor>", "ResultDataAccess does not yet expose the centralized matching-result projection visitor template.");
            AssertContains(resultAccess, "VisitProjectedMatchingResults(const ResultList& resultList, TResultPredicate predicate, TStringConverter convertString, TResultVisitor visitor)", "ResultDataAccess does not yet expose the centralized matching-result projection helper.");
            AssertContains(resultAccess, "visitor(matchIndex, ProjectResultDataToNet<TResultDataNet, TResultStateNet>(result, convertString));", "ResultDataAccess does not yet route matching-result projection through the centralized single-result projection helper.");
            AssertContains(resultAccess, "template<typename TResultDataNet, typename TResultStateNet, typename TResultArray, typename TResultPredicate, typename TResultArrayFactory, typename TStringConverter, typename TResultArraySetter>", "ResultDataAccess does not yet expose the centralized projected-match collection helper template.");
            AssertContains(resultAccess, "CreateProjectedMatchingResults(const ResultList& resultList, TResultPredicate predicate, TResultArrayFactory createResultArray, TStringConverter convertString, TResultArraySetter setProjectedResult)", "ResultDataAccess does not yet expose the centralized projected-match collection helper.");
            AssertContains(resultAccess, "TResultArray projectedResults = createResultArray(CountMatchingResults(resultList, predicate));", "ResultDataAccess projected-match collection helper does not yet size the target collection through the centralized match-count seam.");
            AssertContains(resultAccess, "VisitProjectedMatchingResults<TResultDataNet, TResultStateNet>(resultList, predicate, convertString, [&](size_t index, TResultDataNet resultDataNet)", "ResultDataAccess projected-match collection helper does not yet reuse the centralized projected-matching seam.");
            AssertContains(resultAccess, "setProjectedResult(projectedResults, index, resultDataNet);", "ResultDataAccess projected-match collection helper does not yet route projected writes through the supplied collection setter.");
            AssertContains(resultAccess, "CreateProjectedDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TResultArrayFactory createResultArray, TStringConverter convertString, TResultArraySetter setProjectedResult)", "ResultDataAccess does not yet expose the centralized managed digest-match array projection helper.");
            AssertContains(resultAccess, "return CreateProjectedMatchingResults<TResultDataNet, TResultStateNet, TResultArray>(resultList, [&](const ResultData& result)", "ResultDataAccess managed digest-match array projection helper does not yet reuse the centralized projected-match collection helper.");
            AssertContains(resultAccess, "return ResultMatchesDigestText(result, digestText);", "ResultDataAccess managed digest-match array projection helper does not yet reuse the centralized digest-match predicate seam.");

            AssertContains(clrMgmt, "cli::array<ResultDataNet>^ projectedResults = gcnew cli::array<ResultDataNet>(static_cast<int>(CountDigestMatchingResults(resultList, tstrHashToFind)));", "CLR bridge management layer does not yet allocate projected managed results through the current compile-safe digest-match count seam.");
            AssertContains(clrMgmt, "projectedResults[projectedIndex] = ProjectResultDataToNet<ResultDataNet, ResultStateNet>(*itr, ConvertTstrToSystemString);", "CLR bridge management layer no longer writes projected results into the managed array through the current projection path.");
            AssertDoesNotContain(clrMgmt, "ResultList findResultList;", "CLR bridge management layer still stages matching results in a temporary list instead of using the centralized matching-result projection helper.");
            AssertDoesNotContain(clrMgmt, "ResultDataNet resultDataNet = ConvertResultDataToNet(*itr);", "CLR bridge management layer still performs inline per-item projection instead of using the centralized result-list projection helper.");

            AssertContains(uwpMgmt, "CreateProjectedDigestMatchingResults<ResultDataNet, ResultStateNet, Array<ResultDataNet>^>(GetThreadDataResults(m_threadData), tstrHashToFind, [&](size_t resultCount)", "UWP bridge management layer does not yet route matching-result array projection through the centralized ResultDataAccess helper.");
            AssertContains(uwpMgmt, "projectedResults[index] = resultDataNet;", "UWP bridge management layer no longer writes projected results into the managed array through the current path.");
            AssertDoesNotContain(uwpMgmt, "ResultList findResultList;", "UWP bridge management layer still stages matching results in a temporary list instead of using the centralized matching-result projection helper.");
            AssertDoesNotContain(uwpMgmt, "ResultDataNet resultDataNet = UIBridgeUwp::ConvertResultDataToNet(*itr);", "UWP bridge management layer still performs inline per-item projection instead of using the centralized result-list projection helper.");
        }, failures);

        Run("Phase 5 routes result-list matching through a centralized ResultDataAccess helper", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string clrMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");

            AssertContains(resultAccess, "template<typename TResultPredicate, typename TResultVisitor>", "ResultDataAccess does not yet expose the centralized result-list matching visitor template.");
            AssertContains(resultAccess, "VisitMatchingResults(const ResultList& resultList, TResultPredicate predicate, TResultVisitor visitor)", "ResultDataAccess does not yet expose the centralized result-list matching helper.");
            AssertContains(resultAccess, "CountMatchingResults(const ResultList& resultList, TResultPredicate predicate)", "ResultDataAccess does not yet expose the centralized result-list match-count helper.");
            AssertContains(resultAccess, "CountDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText)", "ResultDataAccess does not yet expose the centralized digest-match count helper.");
            AssertContains(resultAccess, "ResultMatchesDigestText(const ResultData& result, const sunjwbase::tstring& digestText)", "ResultDataAccess does not yet expose the centralized digest-match predicate helper.");
            AssertContains(resultAccess, "NormalizeResultPathSearchText(const sunjwbase::tstring& pathText)", "ResultDataAccess does not yet expose the centralized path-search normalization helper.");
            AssertContains(resultAccess, "ResultMatchesPathText(const ResultData& result, const sunjwbase::tstring& pathText)", "ResultDataAccess does not yet expose the centralized path-match predicate helper.");
            AssertContains(resultAccess, "NormalizeDigestSearchText(const sunjwbase::tstring& digestText)", "ResultDataAccess does not yet expose the centralized digest-search normalization helper.");
            AssertContains(resultAccess, "if (predicate(*itr))", "ResultDataAccess matching helper does not yet gate result visits through the supplied predicate.");
            AssertContains(resultAccess, "visitor(*itr);", "ResultDataAccess matching helper does not yet forward matched results through the supplied visitor.");
            AssertContains(resultAccess, "++matchCount;", "ResultDataAccess matching helper does not yet return the number of matched results.");
            AssertContains(resultAccess, "VisitMatchingResults(resultList, predicate, [&](const ResultData& result)", "ResultDataAccess match-count helper does not yet reuse the centralized matching visitor seam.");
            AssertContains(resultAccess, "return digestText.size() > 0 &&", "ResultDataAccess digest-match predicate helper does not yet guard empty search text through the centralized seam.");
            AssertContains(resultAccess, "ResultContainsDigest(result, digestText);", "ResultDataAccess digest-match predicate helper does not yet reuse the centralized digest seam.");
            AssertContains(resultAccess, "return sunjwbase::strtotstr(sunjwbase::str_lower(sunjwbase::tstrtostr(pathText)));", "ResultDataAccess path-search normalization helper does not yet lowercase through the centralized seam.");
            AssertContains(resultAccess, "return NormalizeResultPathSearchText(GetResultPath(result)).find(pathText) != sunjwbase::tstring::npos;", "ResultDataAccess path-match predicate helper does not yet reuse the centralized path-search normalization seam.");
            AssertContains(resultAccess, "normalizedDigestText = sunjwbase::strtotstr(sunjwbase::str_upper(sunjwbase::tstrtostr(normalizedDigestText)));", "ResultDataAccess digest-search normalization helper does not yet uppercase through the centralized seam.");
            AssertContains(resultAccess, "normalizedDigestText = sunjwbase::strtrim(normalizedDigestText);", "ResultDataAccess digest-search normalization helper does not yet trim through the centralized seam.");
            AssertContains(resultAccess, "return VisitDigestMatchingResults(resultList, digestText, [&](const ResultData& result)", "ResultDataAccess digest-match count helper does not yet reuse the centralized digest-match visitor seam.");

            AssertContains(clrMgmt, "tstrHashToFind = NormalizeDigestSearchText(tstrHashToFind);", "CLR bridge management layer does not yet route digest-search normalization through the centralized ResultDataAccess helper.");
            AssertContains(clrMgmt, "cli::array<ResultDataNet>^ projectedResults = gcnew cli::array<ResultDataNet>(static_cast<int>(CountDigestMatchingResults(resultList, tstrHashToFind)));", "CLR bridge management layer does not yet route digest-match array allocation through the current compile-safe projection helper.");
            AssertDoesNotContain(clrMgmt, "for (; itr != m_pThreadData->resultList.end(); ++itr)", "CLR bridge management layer still performs manual result-list filtering instead of using the centralized matching helper.");
            AssertDoesNotContain(clrMgmt, "tstrHashToFind = strtotstr(str_upper(tstrtostr(tstrHashToFind)));", "CLR bridge management layer still uppercases digest search text inline instead of using the centralized normalization helper.");
            AssertDoesNotContain(clrMgmt, "tstrHashToFind = strtrim(tstrHashToFind);", "CLR bridge management layer still trims digest search text inline instead of using the centralized normalization helper.");

            AssertContains(uwpMgmt, "tstrHashToFind = NormalizeDigestSearchText(tstrHashToFind);", "UWP bridge management layer does not yet route digest-search normalization through the centralized ResultDataAccess helper.");
            AssertContains(uwpMgmt, "CreateProjectedDigestMatchingResults<ResultDataNet, ResultStateNet, Array<ResultDataNet>^>", "UWP bridge management layer does not yet route digest-match array allocation through the centralized projection helper.");
            AssertDoesNotContain(uwpMgmt, "for (; itr != m_threadData.resultList.end(); ++itr)", "UWP bridge management layer still performs manual result-list filtering instead of using the centralized matching helper.");
            AssertDoesNotContain(uwpMgmt, "tstrHashToFind = strtotstr(str_upper(tstrtostr(tstrHashToFind)));", "UWP bridge management layer still uppercases digest search text inline instead of using the centralized normalization helper.");
            AssertDoesNotContain(uwpMgmt, "tstrHashToFind = strtrim(tstrHashToFind);", "UWP bridge management layer still trims digest search text inline instead of using the centralized normalization helper.");
        }, failures);

        Run("Phase 5 routes managed digest-match projection through dedicated ResultDataAccess helpers", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string clrMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");

            AssertContains(resultAccess, "VisitProjectedDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TStringConverter convertString, TResultVisitor visitor)", "ResultDataAccess does not yet expose the centralized digest-match projection helper.");
            AssertContains(resultAccess, "VisitProjectedMatchingResults<TResultDataNet, TResultStateNet>(resultList, [&](const ResultData& result)", "ResultDataAccess digest-match projection helper does not yet reuse the centralized matching-projection seam.");
            AssertContains(resultAccess, "return ResultMatchesDigestText(result, digestText);", "ResultDataAccess digest-match projection helper does not yet reuse the centralized digest-match predicate seam.");

            AssertContains(clrMgmt, "projectedResults[projectedIndex] = ProjectResultDataToNet<ResultDataNet, ResultStateNet>(*itr, ConvertTstrToSystemString);", "CLR bridge management layer does not yet route digest-match projection through the current compile-safe ResultDataAccess helper.");
            AssertDoesNotContain(clrMgmt, "VisitProjectedMatchingResults<ResultDataNet, ResultStateNet>(m_pThreadData->resultList, [&](const ResultData& result)", "CLR bridge management layer still keeps the inline digest-match projection lambda instead of using the dedicated helper.");

            AssertContains(uwpMgmt, "CreateProjectedDigestMatchingResults<ResultDataNet, ResultStateNet, Array<ResultDataNet>^>(GetThreadDataResults(m_threadData), tstrHashToFind, [&](size_t resultCount)", "UWP bridge management layer does not yet route digest-match projection through the dedicated ResultDataAccess helper.");
            AssertDoesNotContain(uwpMgmt, "VisitProjectedMatchingResults<ResultDataNet, ResultStateNet>(m_threadData.resultList, [&](const ResultData& result)", "UWP bridge management layer still keeps the inline digest-match projection lambda instead of using the dedicated helper.");
        }, failures);

        Run("Phase 5 routes digest-match counting and projection through a dedicated matching visitor seam", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");

            AssertContains(resultAccess, "VisitDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TResultVisitor visitor)", "ResultDataAccess does not yet expose the centralized digest-match visitor helper.");
            AssertContains(resultAccess, "return VisitDigestMatchingResults(resultList, digestText, [&](const ResultData& result)", "ResultDataAccess digest-match count helper does not yet route through the centralized digest-match visitor seam.");
            AssertContains(resultAccess, "VisitDigestMatchingResults(resultList, digestText, [&](const ResultData& result)", "ResultDataAccess digest-match projection helper does not yet route through the centralized digest-match visitor seam.");
        }, failures);

        Run("Phase 5 routes MFC result search iteration through the centralized ResultDataAccess matching helper", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string filesHashDlg = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");

            AssertContains(resultAccess, "ResultMatchesPathAndDigestText(const ResultData& result, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText)", "ResultDataAccess does not yet expose the centralized path+digest-match predicate helper.");
            AssertContains(resultAccess, "VisitPathAndDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, TResultVisitor visitor)", "ResultDataAccess does not yet expose the centralized path+digest matching visitor helper.");
            AssertContains(resultAccess, "return ResultMatchesPathText(result, pathText) &&", "ResultDataAccess path+digest-match predicate helper does not yet compose through the shared path-match seam.");
            AssertContains(resultAccess, "ResultMatchesDigestText(result, digestText);", "ResultDataAccess path+digest-match predicate helper does not yet compose through the shared digest-match seam.");
            AssertContains(resultAccess, "return VisitMatchingResults(resultList, [&](const ResultData& result)", "ResultDataAccess path+digest matching helper does not yet reuse the centralized matching visitor seam.");
            AssertContains(resultAccess, "return ResultMatchesPathAndDigestText(result, pathText, digestText);", "ResultDataAccess path+digest matching helper does not yet reuse the centralized combined match predicate seam.");

            AssertContains(filesHashDlg, "VisitPathAndDigestMatchingResults(GetThreadDataResults(m_thrdData), tstrFileToFind, tstrHashToFind, [&](const ResultData& result)", "Legacy MFC search flow does not yet route result iteration through the centralized path+digest matching helper.");
            AssertContains(filesHashDlg, "tstring tstrFileToFind = NormalizeResultPathSearchText(strFile.GetString());", "Legacy MFC search flow does not yet normalize path search text through the centralized ResultDataAccess helper.");
            AssertContains(filesHashDlg, "tstring tstrHashToFind = NormalizeDigestSearchText(strHash.GetString());", "Legacy MFC search flow does not yet normalize digest search text through the centralized ResultDataAccess helper.");
            AssertContains(filesHashDlg, "AppendResult(result);", "Legacy MFC search flow no longer appends matched results through the current path.");
            AssertDoesNotContain(filesHashDlg, "strHash.MakeUpper();", "Legacy MFC search flow still uppercases digest search text inline instead of using the centralized normalization seam.");
            AssertDoesNotContain(filesHashDlg, "strFile.MakeLower();", "Legacy MFC search flow still lowercases path search text inline instead of using the centralized normalization seam.");
            AssertDoesNotContain(filesHashDlg, "CString strPathLower = CString(GetResultPath(result).c_str());", "Legacy MFC search flow still lowercases result paths inline instead of using the centralized path-match seam.");
            AssertDoesNotContain(filesHashDlg, "ResultContainsDigest(result, strHash.GetString())", "Legacy MFC search flow still evaluates digest matches inline instead of using the centralized normalized digest-match seam.");
            AssertDoesNotContain(filesHashDlg, "return ResultMatchesPathText(result, tstrFileToFind) &&", "Legacy MFC search flow still keeps the inline path+digest predicate instead of using the centralized combined match helper.");
        }, failures);

        Run("Phase 4 centralizes non-digest ResultData fields behind a dedicated core-state structure", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");

            AssertContains(global, "struct ResultCoreState", "Global.h does not yet expose the grouped non-digest ResultData core-state structure introduced in phase 4.");
            AssertContains(global, "ResultCoreState coreState;", "ResultData does not yet group its non-digest fields into the dedicated core-state structure.");
            AssertContains(global, "ResultState state;", "ResultCoreState no longer keeps the current result-state field in the compatibility shape.");
            AssertContains(global, "sunjwbase::tstring path;", "ResultCoreState no longer keeps the current path field in the compatibility shape.");
            AssertContains(global, "uint64_t size;", "ResultCoreState no longer keeps the current size field in the compatibility shape.");
            AssertContains(global, "sunjwbase::tstring modifiedDate;", "ResultCoreState no longer keeps the current modified-date field in the compatibility shape.");
            AssertContains(global, "sunjwbase::tstring version;", "ResultCoreState no longer keeps the current version field in the compatibility shape.");
            AssertContains(global, "sunjwbase::tstring error;", "ResultCoreState no longer keeps the current error field in the compatibility shape.");

            AssertContains(resultAccess, "GetResultCoreState(result).path", "ResultDataAccess path getter does not yet route through the grouped ResultCoreState structure.");
            AssertContains(resultAccess, "GetResultCoreState(result).size", "ResultDataAccess size getter does not yet route through the grouped ResultCoreState structure.");
            AssertContains(resultAccess, "GetResultCoreState(result).modifiedDate", "ResultDataAccess modified-date getter does not yet route through the grouped ResultCoreState structure.");
            AssertContains(resultAccess, "GetResultCoreState(result).version", "ResultDataAccess version getter does not yet route through the grouped ResultCoreState structure.");
            AssertContains(resultAccess, "GetResultCoreState(result).error", "ResultDataAccess error getter does not yet route through the grouped ResultCoreState structure.");
            AssertContains(resultAccess, "GetResultCoreState(result).state", "ResultDataAccess state getter does not yet route through the grouped ResultCoreState structure.");

            AssertContains(resultAccess, "GetMutableResultCoreState(result).path = path;", "ResultDataAccess path setter does not yet route through the grouped ResultCoreState structure.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).size = size;", "ResultDataAccess size setter does not yet route through the grouped ResultCoreState structure.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).modifiedDate = modifiedDate;", "ResultDataAccess modified-date setter does not yet route through the grouped ResultCoreState structure.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).version = version;", "ResultDataAccess version setter does not yet route through the grouped ResultCoreState structure.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).error = errorText;", "ResultDataAccess error setter does not yet route through the grouped ResultCoreState structure.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).state = resultState;", "ResultDataAccess state setter does not yet route through the grouped ResultCoreState structure.");
        }, failures);

        Run("Phase 4 routes ResultCoreState access through dedicated core-state helpers", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");

            AssertContains(resultAccess, "GetResultCoreState(const ResultData& result)", "ResultDataAccess does not yet expose the grouped ResultCoreState getter introduced in phase 4.");
            AssertContains(resultAccess, "GetMutableResultCoreState(ResultData& result)", "ResultDataAccess does not yet expose the grouped ResultCoreState mutable getter introduced in phase 4.");
            AssertContains(resultAccess, "return result.coreState;", "ResultDataAccess core-state helper does not yet route through the current ResultData core field.");

            AssertContains(resultAccess, "GetResultCoreState(result).path", "ResultDataAccess path getter does not yet route through the grouped ResultCoreState helper.");
            AssertContains(resultAccess, "GetResultCoreState(result).size", "ResultDataAccess size getter does not yet route through the grouped ResultCoreState helper.");
            AssertContains(resultAccess, "GetResultCoreState(result).modifiedDate", "ResultDataAccess modified-date getter does not yet route through the grouped ResultCoreState helper.");
            AssertContains(resultAccess, "GetResultCoreState(result).version", "ResultDataAccess version getter does not yet route through the grouped ResultCoreState helper.");
            AssertContains(resultAccess, "GetResultCoreState(result).error", "ResultDataAccess error getter does not yet route through the grouped ResultCoreState helper.");
            AssertContains(resultAccess, "GetResultCoreState(result).state", "ResultDataAccess ResultState getter does not yet route through the grouped ResultCoreState helper.");

            AssertContains(resultAccess, "GetMutableResultCoreState(result).path = path;", "ResultDataAccess path setter does not yet route through the grouped ResultCoreState helper.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).size = size;", "ResultDataAccess size setter does not yet route through the grouped ResultCoreState helper.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).modifiedDate = modifiedDate;", "ResultDataAccess modified-date setter does not yet route through the grouped ResultCoreState helper.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).version = version;", "ResultDataAccess version setter does not yet route through the grouped ResultCoreState helper.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).error = errorText;", "ResultDataAccess error setter does not yet route through the grouped ResultCoreState helper.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).state = resultState;", "ResultDataAccess ResultState setter does not yet route through the grouped ResultCoreState helper.");
        }, failures);

        Run("Phase 4 resets grouped non-digest ResultCoreState through a dedicated helper before publishing a file result", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string engine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");

            AssertContains(resultAccess, "ResetResultCoreState(ResultData& result)", "ResultDataAccess does not yet expose the grouped ResultCoreState reset helper introduced in phase 4.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).state = RESULT_NONE;", "ResultDataAccess core-state reset helper does not yet clear the ResultState field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).path.clear();", "ResultDataAccess core-state reset helper does not yet clear the path field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).size = 0;", "ResultDataAccess core-state reset helper does not yet clear the size field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).modifiedDate.clear();", "ResultDataAccess core-state reset helper does not yet clear the modified-date field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).version.clear();", "ResultDataAccess core-state reset helper does not yet clear the version field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).error.clear();", "ResultDataAccess core-state reset helper does not yet clear the error field.");

            AssertContains(engine, "ResetResultData(result);", "HashEngine does not yet reset the grouped ResultCoreState before publishing a new file result.");
            AssertInOrder(engine,
                [
                    "static ResultData& BeginFileResult(",
                    "ResultData& result = AppendThreadDataResult(*thrdData);",
                    "ResetResultData(result);",
                    "SetResultState(result, RESULT_NONE);",
                    "SetResultPath(result, path);",
                    "EmitPathResult(observer, result);"
                ],
                "HashEngine file-result begin helper no longer resets grouped core state before publishing the file path.");
        }, failures);

        Run("Phase 4 composes core and digest resets through a single ResultData reset helper", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string engine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");

            AssertContains(resultAccess, "#include \"Common/ResultDigestAccess.h\"", "ResultDataAccess does not yet consume the digest seam when composing the grouped result reset helper.");
            AssertContains(resultAccess, "ResetResultData(ResultData& result)", "ResultDataAccess does not yet expose the grouped ResultData reset helper introduced in phase 4.");
            AssertContains(resultAccess, "ResetResultCoreState(result);", "ResultDataAccess grouped ResultData reset helper does not yet reset grouped core state.");
            AssertContains(resultAccess, "ResetResultDigests(result);", "ResultDataAccess grouped ResultData reset helper does not yet reset digest state through the existing digest seam.");

            AssertContains(engine, "ResetResultData(result);", "HashEngine does not yet route new ResultData initialization through the grouped reset helper.");
            AssertDoesNotContain(engine, "ResetResultCoreState(result);\r\n\tResetResultDigests(result);", "HashEngine still manually sequences the separate core and digest reset helpers instead of using the grouped result reset helper.");
            AssertInOrder(engine,
                [
                    "static ResultData& BeginFileResult(",
                    "ResultData& result = AppendThreadDataResult(*thrdData);",
                    "ResetResultData(result);",
                    "SetResultState(result, RESULT_NONE);",
                    "SetResultPath(result, path);",
                    "EmitPathResult(observer, result);"
                ],
                "HashEngine file-result begin helper no longer routes grouped result reset through the dedicated helper before publishing the file path.");
        }, failures);

        Run("Phase 4 routes managed ResultStateNet conversion through dedicated bridge helpers", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(resultAccess, "template<typename TResultStateNet>", "ResultDataAccess does not yet expose the centralized ResultStateNet conversion template introduced after phase 4.");
            AssertContains(resultAccess, "static inline TResultStateNet ConvertResultStateToNet(ResultState resultState)", "ResultDataAccess does not yet expose the centralized ResultStateNet conversion helper introduced after phase 4.");
            AssertContains(resultAccess, "resultDataNet.EnumState = ConvertResultStateToNet<TResultStateNet>(GetResultState(result));", "ResultDataAccess does not yet route ResultStateNet assignment through the centralized helper.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route ResultStateNet assignment through the centralized ResultDataNet projection helper.");
            AssertDoesNotContain(bridgeWui, "switch (GetResultState(result))", "WinUI bridge still inlines ResultStateNet conversion instead of using the dedicated helper.");
            AssertDoesNotContain(bridgeWui, "static ResultStateNet ConvertResultStateToNet(ResultState resultState)", "WinUI bridge still keeps a local ResultStateNet conversion helper instead of using the centralized ResultDataAccess helper.");

            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route ResultStateNet assignment through the centralized ResultDataNet projection helper.");
            AssertDoesNotContain(bridgeUwp, "switch (GetResultState(result))", "UWP bridge still inlines ResultStateNet conversion instead of using the dedicated helper.");
            AssertDoesNotContain(bridgeUwp, "static ResultStateNet ConvertResultStateToNet(ResultState resultState)", "UWP bridge still keeps a local ResultStateNet conversion helper instead of using the centralized ResultDataAccess helper.");
        }, failures);

        Run("Phase 5 routes ResultState and digest-type projection through dedicated ResultDataAccess dispatch helpers", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");

            AssertContains(resultAccess, "DispatchResultStateByType(ResultState resultState, TNoneAction onNone, TPathAction onPath, TMetaAction onMeta, TAllAction onAll, TErrorAction onError)", "ResultDataAccess does not yet expose the grouped ResultState dispatch helper.");
            AssertContains(resultAccess, "DispatchResultStateByType(resultState,", "ResultDataAccess does not yet route ResultState render-policy through the grouped dispatch helper.");
            AssertContains(resultAccess, "DispatchResultDigestValueByType(ResultDigestType digestType, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)", "ResultDataAccess does not yet expose the grouped digest-type dispatch helper.");
            AssertContains(resultAccess, "switch (resultState)", "ResultDataAccess ResultStateNet conversion helper does not yet use the compile-safe explicit ResultState switch.");
            AssertContains(resultAccess, "return TResultStateNet::ResultPath;", "ResultDataAccess ResultStateNet conversion helper does not yet map RESULT_PATH through the compile-safe explicit switch.");
            AssertContains(resultAccess, "switch (digestType)", "ResultDataAccess digest assignment helper does not yet use the compile-safe explicit digest-type switch.");
            AssertContains(resultAccess, "resultDataNet.MD5 = digestValue;", "ResultDataAccess digest assignment helper does not yet map MD5 through the compile-safe explicit switch.");

            AssertDoesNotContain(resultAccess, "static inline ResultRenderPolicy GetResultRenderPolicy(ResultState resultState)\r\n{\r\n\tswitch (resultState)", "ResultDataAccess render-policy helper still performs an inline ResultState switch instead of using the grouped dispatch helper.");
        }, failures);

        Run("Phase 4 routes optional result-version checks through a dedicated ResultDataAccess helper", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(resultAccess, "HasResultVersion(const ResultData& result)", "ResultDataAccess does not yet expose the neutral version-presence helper introduced in phase 4.");
            AssertContains(resultAccess, "return GetResultVersion(result) != _T(\"\");", "ResultDataAccess version-presence helper does not yet route through the neutral version getter.");

            AssertContains(bridgeMfc, "VisitRenderableResultMetaLineDisplayInfos(result, [&](ResultMetaLineType metaLine, const ResultMetaLineDisplayInfo& metaLineDisplayInfo)", "Legacy MFC metadata renderer does not yet route optional version checks through the shared metadata-line display-info seam.");
            AssertDoesNotContain(bridgeMfc, "if (GetResultVersion(result) != _T(\"\"))", "Legacy MFC metadata renderer still checks version presence inline instead of using the helper.");
        }, failures);

        Run("Phase 5 neutralizes the internal ResultCoreState state field behind the existing ResultDataAccess seam", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");

            AssertContains(global, "ResultState state;", "ResultCoreState does not yet expose the neutral internal state field introduced in phase 5.");
            AssertDoesNotContain(global, "ResultState enumState;", "ResultCoreState still uses the legacy enumState field name instead of the neutral internal state field.");

            AssertContains(resultAccess, "return GetResultCoreState(result).state;", "ResultDataAccess ResultState getter does not yet route through the neutral internal state field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).state = resultState;", "ResultDataAccess ResultState setter does not yet route through the neutral internal state field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).state = RESULT_NONE;", "ResultDataAccess core-state reset helper does not yet clear the neutral internal state field.");
        }, failures);

        Run("Phase 5 neutralizes the internal ResultCoreState path field behind the existing ResultDataAccess seam", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");

            AssertContains(global, "sunjwbase::tstring path;", "ResultCoreState does not yet expose the neutral internal path field introduced in phase 5.");
            AssertDoesNotContain(global, "sunjwbase::tstring tstrPath;", "ResultCoreState still uses the legacy tstrPath field name instead of the neutral internal path field.");

            AssertContains(resultAccess, "return GetResultCoreState(result).path;", "ResultDataAccess path getter does not yet route through the neutral internal path field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).path = path;", "ResultDataAccess path setter does not yet route through the neutral internal path field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).path.clear();", "ResultDataAccess core-state reset helper does not yet clear the neutral internal path field.");
        }, failures);

        Run("Phase 5 neutralizes the internal ResultCoreState size field behind the existing ResultDataAccess seam", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");

            AssertContains(global, "uint64_t size;", "ResultCoreState does not yet expose the neutral internal size field introduced in phase 5.");
            AssertDoesNotContain(global, "uint64_t ulSize;", "ResultCoreState still uses the legacy ulSize field name instead of the neutral internal size field.");

            AssertContains(resultAccess, "return GetResultCoreState(result).size;", "ResultDataAccess size getter does not yet route through the neutral internal size field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).size = size;", "ResultDataAccess size setter does not yet route through the neutral internal size field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).size = 0;", "ResultDataAccess core-state reset helper does not yet clear the neutral internal size field.");
        }, failures);

        Run("Phase 5 neutralizes the internal ResultCoreState metadata and error fields behind the existing ResultDataAccess seam", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");

            AssertContains(global, "sunjwbase::tstring modifiedDate;", "ResultCoreState does not yet expose the neutral internal modified-date field introduced in phase 5.");
            AssertContains(global, "sunjwbase::tstring version;", "ResultCoreState does not yet expose the neutral internal version field introduced in phase 5.");
            AssertContains(global, "sunjwbase::tstring error;", "ResultCoreState does not yet expose the neutral internal error field introduced in phase 5.");
            AssertDoesNotContain(global, "sunjwbase::tstring tstrMDate;", "ResultCoreState still uses the legacy tstrMDate field name instead of the neutral internal modified-date field.");
            AssertDoesNotContain(global, "sunjwbase::tstring tstrVersion;", "ResultCoreState still uses the legacy tstrVersion field name instead of the neutral internal version field.");
            AssertDoesNotContain(global, "sunjwbase::tstring tstrError;", "ResultCoreState still uses the legacy tstrError field name instead of the neutral internal error field.");

            AssertContains(resultAccess, "return GetResultCoreState(result).modifiedDate;", "ResultDataAccess modified-date getter does not yet route through the neutral internal modified-date field.");
            AssertContains(resultAccess, "return GetResultCoreState(result).version;", "ResultDataAccess version getter does not yet route through the neutral internal version field.");
            AssertContains(resultAccess, "return GetResultCoreState(result).error;", "ResultDataAccess error getter does not yet route through the neutral internal error field.");

            AssertContains(resultAccess, "GetMutableResultCoreState(result).modifiedDate = modifiedDate;", "ResultDataAccess modified-date setter does not yet route through the neutral internal modified-date field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).version = version;", "ResultDataAccess version setter does not yet route through the neutral internal version field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).error = errorText;", "ResultDataAccess error setter does not yet route through the neutral internal error field.");

            AssertContains(resultAccess, "GetMutableResultCoreState(result).modifiedDate.clear();", "ResultDataAccess core-state reset helper does not yet clear the neutral internal modified-date field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).version.clear();", "ResultDataAccess core-state reset helper does not yet clear the neutral internal version field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).error.clear();", "ResultDataAccess core-state reset helper does not yet clear the neutral internal error field.");
        }, failures);

        Run("Phase 5 neutralizes the internal ResultDigestCompatibilityFields names behind the existing ResultDigestAccess seam", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(global, "struct ResultDigestCompatibilityFields", "ResultData digest compatibility surface is missing.");
            AssertContains(global, "sunjwbase::tstring md5;", "ResultDigestCompatibilityFields does not yet expose the neutral MD5 field name.");
            AssertContains(global, "sunjwbase::tstring sha1;", "ResultDigestCompatibilityFields does not yet expose the neutral SHA1 field name.");
            AssertContains(global, "sunjwbase::tstring sha256;", "ResultDigestCompatibilityFields does not yet expose the neutral SHA256 field name.");
            AssertContains(global, "sunjwbase::tstring sha512;", "ResultDigestCompatibilityFields does not yet expose the neutral SHA512 field name.");
            AssertDoesNotContain(global, "sunjwbase::tstring tstrMD5;", "ResultDigestCompatibilityFields still uses the legacy MD5 field name internally.");
            AssertDoesNotContain(global, "sunjwbase::tstring tstrSHA1;", "ResultDigestCompatibilityFields still uses the legacy SHA1 field name internally.");
            AssertDoesNotContain(global, "sunjwbase::tstring tstrSHA256;", "ResultDigestCompatibilityFields still uses the legacy SHA256 field name internally.");
            AssertDoesNotContain(global, "sunjwbase::tstring tstrSHA512;", "ResultDigestCompatibilityFields still uses the legacy SHA512 field name internally.");

            AssertContains(digestAccess, "&ResultDigestCompatibilityFields::md5", "ResultDigestAccess metadata does not yet route MD5 through the neutral digest compatibility field name.");
            AssertContains(digestAccess, "&ResultDigestCompatibilityFields::sha1", "ResultDigestAccess metadata does not yet route SHA1 through the neutral digest compatibility field name.");
            AssertContains(digestAccess, "&ResultDigestCompatibilityFields::sha256", "ResultDigestAccess metadata does not yet route SHA256 through the neutral digest compatibility field name.");
            AssertContains(digestAccess, "&ResultDigestCompatibilityFields::sha512", "ResultDigestAccess metadata does not yet route SHA512 through the neutral digest compatibility field name.");
            AssertDoesNotContain(digestAccess, "&ResultLegacyDigestFields::md5", "ResultDigestAccess still routes MD5 through the legacy digest compatibility struct name.");
            AssertDoesNotContain(digestAccess, "&ResultLegacyDigestFields::sha1", "ResultDigestAccess still routes SHA1 through the legacy digest compatibility struct name.");
            AssertDoesNotContain(digestAccess, "&ResultLegacyDigestFields::sha256", "ResultDigestAccess still routes SHA256 through the legacy digest compatibility struct name.");
            AssertDoesNotContain(digestAccess, "&ResultLegacyDigestFields::sha512", "ResultDigestAccess still routes SHA512 through the legacy digest compatibility struct name.");
        }, failures);

        Run("Phase 5 neutralizes the internal ResultDigestStorage field name behind the existing ResultDigestAccess seam", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(global, "sunjwbase::tstring values[RESULT_DIGEST_STORAGE_COUNT];", "ResultDigestStorage does not yet expose the neutral internal storage field name.");
            AssertDoesNotContain(global, "sunjwbase::tstring tstrDigests[RESULT_DIGEST_STORAGE_COUNT];", "ResultDigestStorage still uses the legacy tstrDigests field name internally.");

            AssertContains(digestAccess, "return digestStorage.values[GetResultDigestIndex(digestType)];", "ResultDigestAccess digest-storage seam does not yet route through the neutral internal storage field name.");
            AssertDoesNotContain(digestAccess, "return digestStorage.tstrDigests[GetResultDigestIndex(digestType)];", "ResultDigestAccess digest-storage seam still routes through the legacy internal storage field name.");
        }, failures);

        Run("Phase 5 neutralizes the internal ResultDigestState field names behind the existing ResultDigestAccess seam", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(global, "ResultDigestStorage storage;", "ResultDigestState does not yet expose the neutral internal storage field name.");
            AssertContains(global, "ResultDigestCompatibilityFields compatibilityFields;", "ResultDigestState does not yet expose the neutral internal compatibility-fields name.");
            AssertDoesNotContain(global, "ResultDigestStorage digestStorage;", "ResultDigestState still uses the legacy digestStorage field name internally.");
            AssertDoesNotContain(global, "ResultLegacyDigestFields legacyFields;", "ResultDigestState still uses the legacy digest compatibility struct name internally.");

            AssertContains(digestAccess, "return GetResultDigestState(result).storage;", "ResultDigestAccess const digest-storage helper does not yet route through the neutral ResultDigestState storage field name.");
            AssertContains(digestAccess, "return GetMutableResultDigestState(result).storage;", "ResultDigestAccess mutable digest-storage helper does not yet route through the neutral ResultDigestState storage field name.");
            AssertContains(digestAccess, "return GetResultDigestState(result).compatibilityFields;", "ResultDigestAccess const digest-compatibility-fields helper does not yet route through the neutral ResultDigestState compatibility-fields name.");
            AssertContains(digestAccess, "return GetMutableResultDigestState(result).compatibilityFields;", "ResultDigestAccess mutable digest-compatibility-fields helper does not yet route through the neutral ResultDigestState compatibility-fields name.");
            AssertDoesNotContain(digestAccess, "return GetResultDigestState(result).digestStorage;", "ResultDigestAccess still routes through the legacy ResultDigestState digestStorage field name.");
            AssertDoesNotContain(digestAccess, "return GetMutableResultDigestState(result).digestStorage;", "ResultDigestAccess mutable helpers still route through the legacy ResultDigestState digestStorage field name.");
            AssertDoesNotContain(digestAccess, "return GetResultDigestState(result).legacyDigests;", "ResultDigestAccess still routes through the legacy ResultDigestState legacyDigests field name.");
            AssertDoesNotContain(digestAccess, "return GetMutableResultDigestState(result).legacyDigests;", "ResultDigestAccess mutable helpers still route through the legacy ResultDigestState legacyDigests field name.");
        }, failures);

        Run("Phase 5 neutralizes the internal ResultData grouped state field names behind the existing access seams", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(global, "ResultCoreState coreState;", "ResultData does not yet expose the neutral grouped core-state field name.");
            AssertContains(global, "ResultDigestState digestState;", "ResultData does not yet expose the neutral grouped digest-state field name.");
            AssertDoesNotContain(global, "ResultCoreState core;", "ResultData still uses the legacy grouped core field name internally.");
            AssertDoesNotContain(global, "ResultDigestState digests;", "ResultData still uses the legacy grouped digests field name internally.");

            AssertContains(resultAccess, "return result.coreState;", "ResultDataAccess core-state helpers do not yet route through the neutral ResultData grouped core-state field name.");
            AssertDoesNotContain(resultAccess, "return result.core;", "ResultDataAccess still routes through the legacy ResultData grouped core field name.");

            AssertContains(digestAccess, "return result.digestState;", "ResultDigestAccess digest-state helpers do not yet route through the neutral ResultData grouped digest-state field name.");
            AssertDoesNotContain(digestAccess, "return result.digests;", "ResultDigestAccess still routes through the legacy ResultData grouped digests field name.");
        }, failures);

        Run("Phase 5 neutralizes the internal ResultDigestMetadata field names behind the existing digest-access seam", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(digestAccess, "ResultDigestType type;", "ResultDigestMetadata does not yet expose the neutral digest type field name.");
            AssertContains(digestAccess, "sunjwbase::tstring ResultDigestCompatibilityFields::*compatibilityValueField;", "ResultDigestMetadata does not yet expose the neutral compatibility-field pointer name.");
            AssertDoesNotContain(digestAccess, "ResultDigestType digestType;", "ResultDigestMetadata still uses the legacy digestType field name internally.");
            AssertDoesNotContain(digestAccess, "sunjwbase::tstring ResultLegacyDigestFields::*legacyValueField;", "ResultDigestMetadata still uses the legacy digest compatibility struct or pointer name internally.");

            AssertContains(digestAccess, "return visitor(GetResultDigestMetadataType(digestMetadata));", "ResultDigestAccess digest visitor does not yet route through the neutral ResultDigestMetadata type accessor.");
            AssertContains(digestAccess, "return visitor(index, digestMetadata, GetResultDigest(result, GetResultDigestMetadataType(digestMetadata)));", "ResultDigestAccess digest metadata-value visitor does not yet route through the neutral ResultDigestMetadata type accessor.");
            AssertContains(digestAccess, "return GetResultDigestMetadataType(GetResultDigestMetadataAt(index));", "ResultDigestAccess digest-order helper does not yet route through the neutral ResultDigestMetadata type accessor.");
            AssertContains(digestAccess, "if (GetResultDigestMetadataType(digestMetadata) == digestType)", "ResultDigestAccess digest-index helper does not yet route through the neutral ResultDigestMetadata type accessor.");
            AssertContains(digestAccess, "return GetResultDigestMetadataCompatibilityValueField(GetResultDigestMetadata(digestType));", "ResultDigestAccess compatibility field-selector helper does not yet route through the neutral ResultDigestMetadata compatibility-field accessor.");

            AssertContains(digestAccess, "GetResultDigestLabel(digestMetadata)", "ResultDigestAccess does not yet route digest labels through the neutral ResultDigestMetadata seam.");
        }, failures);

        Run("Phase 5 routes projected matching traversal through the existing matching-results seam", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");

            AssertContains(resultAccess, "VisitMatchingResults(resultList, predicate, [&](const ResultData& result)", "ResultDataAccess does not yet route projected matching traversal through the existing matching-results seam.");
            AssertContains(resultAccess, "visitor(matchIndex, ProjectResultDataToNet<TResultDataNet, TResultStateNet>(result, convertString));", "ResultDataAccess projected matching traversal does not yet reuse the shared matching seam before projecting each result.");
        }, failures);

        Run("Phase 5 centralizes MFC label and text emission through dedicated hyperedit append helpers", () =>
        {
            string bridgeMfcHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(bridgeMfcHeader, "static void AppendLineBreakToHyperEdit", "UIBridgeMFC does not yet expose the shared line-break append helper.");
            AssertContains(bridgeMfcHeader, "static void AppendTextLineToHyperEdit", "UIBridgeMFC does not yet expose the text-line append helper.");
            AssertContains(bridgeMfcHeader, "static void AppendLabelValueToHyperEdit", "UIBridgeMFC does not yet expose the label/value append helper.");
            AssertContains(bridgeMfcHeader, "static void AppendLabelValueLineToHyperEdit", "UIBridgeMFC does not yet expose the label/value-line append helper.");
            AssertContains(bridgeMfcHeader, "static void AppendLabelLinkLineToHyperEdit", "UIBridgeMFC does not yet expose the label/link-line append helper.");

            AssertContains(bridgeMfc, "AppendTextLineToHyperEdit(GetStringByKey(MAINDLG_WAITING_START), hyperEdit);", "UIBridgeMFC preparing banner no longer routes through the text-line append helper.");
            AssertContains(bridgeMfc, "void UIBridgeMFC::AppendLineBreakToHyperEdit(CHyperEditHash *hyerEdit)", "UIBridgeMFC does not yet implement the shared line-break append helper.");
            AssertContains(bridgeMfc, "AppendLineBreakToHyperEdit(hyerEdit);", "UIBridgeMFC no longer routes trailing line breaks through the shared line-break helper.");
            AssertContains(bridgeMfc, "AppendLabelValueLineToHyperEdit(GetStringByKey(FILENAME_STRING),", "UIBridgeMFC file-name renderer does not yet route through the label/value-line append helper.");
            AssertContains(bridgeMfc, "AppendLabelValueToHyperEdit(metaLineDisplayInfo.label.c_str(),", "UIBridgeMFC metadata-line display append helper does not yet route through the label/value append helper.");
            AssertContains(bridgeMfcHeader, "static void AppendResultDigestDisplayInfoToHyperEdit", "UIBridgeMFC does not yet expose the grouped digest display-info append helper.");
            AssertContains(bridgeMfc, "AppendLabelLinkLineToHyperEdit(digestDisplayInfo.label.c_str(),", "UIBridgeMFC digest display-info append helper does not yet route through the label/link-line append helper.");
            AssertContains(bridgeMfc, "AppendResultDigestDisplayInfoToHyperEdit(digestDisplayInfo, hyerEdit);", "UIBridgeMFC digest renderer does not yet route through the grouped digest display-info append helper.");
            AssertContains(bridgeMfc, "AppendTextLineToHyperEdit(GetResultError(result), hyerEdit);", "UIBridgeMFC error renderer does not yet route through the text-line append helper.");
        }, failures);

        Run("Phase 5 routes MFC metadata line rendering through dedicated ResultDataAccess and bridge helpers", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string bridgeMfcHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(resultAccess, "struct ResultSizeDisplayInfo", "ResultDataAccess does not yet expose the grouped file-size display structure.");
            AssertContains(resultAccess, "GetResultSizeDisplayInfo(const ResultData& result)", "ResultDataAccess does not yet expose the centralized file-size display helper.");
            AssertContains(resultAccess, "sprintf_s(chSizeBuff, 1024, \"%I64u\", GetResultSize(result));", "ResultDataAccess file-size display helper does not yet centralize the byte-size formatting.");
            AssertContains(resultAccess, "resultSizeDisplayInfo.shortSizeText = strtotstr(Utils::ConvertSizeToShortSizeStr(GetResultSize(result)));", "ResultDataAccess file-size display helper does not yet centralize the short-size formatting.");
            AssertContains(resultAccess, "enum ResultMetaLineType", "ResultDataAccess does not yet expose the result-metadata line type enum.");
            AssertContains(resultAccess, "DispatchResultMetaLineByType(ResultMetaLineType metaLine, TFileSizeAction onFileSize, TModifiedDateAction onModifiedDate, TVersionAction onVersion)", "ResultDataAccess does not yet expose the centralized metadata-line dispatch helper.");
            AssertContains(resultAccess, "VisitRenderableResultMetaLines(const ResultData& result, TResultMetaLineVisitor visitor)", "ResultDataAccess does not yet expose the renderable metadata-line visitor helper.");
            AssertContains(resultAccess, "if (!visitor(RESULT_META_LINE_FILE_SIZE))", "ResultDataAccess metadata-line visitor does not yet render file size through the shared seam.");
            AssertContains(resultAccess, "if (!visitor(RESULT_META_LINE_MODIFIED_DATE))", "ResultDataAccess metadata-line visitor does not yet render modified date through the shared seam.");
            AssertContains(resultAccess, "if (HasResultVersion(result) &&", "ResultDataAccess metadata-line visitor does not yet gate version rendering through the shared seam.");

            AssertContains(bridgeMfcHeader, "static void AppendResultMetaLineToHyperEdit", "UIBridgeMFC does not yet expose the metadata-line dispatch helper.");
            AssertContains(bridgeMfcHeader, "struct ResultMetaLineDisplayInfo", "UIBridgeMFC does not yet expose the grouped metadata-line display-info structure.");
            AssertContains(bridgeMfcHeader, "static ResultMetaLineDisplayInfo GetResultMetaLineDisplayInfo", "UIBridgeMFC does not yet expose the metadata-line display-info builder helper.");
            AssertContains(bridgeMfcHeader, "static void AppendResultMetaLineDisplayInfoToHyperEdit", "UIBridgeMFC does not yet expose the metadata-line display-info append helper.");
            AssertContains(bridgeMfcHeader, "VisitRenderableResultMetaLineDisplayInfos(const ResultData& result,", "UIBridgeMFC does not yet expose the metadata-line display-info visitor helper.");
            AssertContains(bridgeMfc, "VisitRenderableResultMetaLineDisplayInfos(result, [&](ResultMetaLineType metaLine, const ResultMetaLineDisplayInfo& metaLineDisplayInfo)", "UIBridgeMFC file-meta renderer does not yet route metadata-line iteration through the grouped display-info visitor helper.");
            AssertContains(bridgeMfc, "AppendResultMetaLineDisplayInfoToHyperEdit(metaLineDisplayInfo, hyerEdit);", "UIBridgeMFC file-meta renderer does not yet route metadata-line emission through the grouped display-info append helper.");
            AssertContains(bridgeMfc, "ResultSizeDisplayInfo resultSizeDisplayInfo = GetResultSizeDisplayInfo(result);", "UIBridgeMFC file-size metadata renderer does not yet consume the centralized file-size display helper.");
            AssertContains(bridgeMfc, "ResultMetaLineDisplayInfo UIBridgeMFC::GetResultMetaLineDisplayInfo(const ResultData& result,", "UIBridgeMFC does not yet implement the metadata-line display-info builder helper.");
            AssertContains(bridgeMfc, "DispatchResultMetaLineByType(metaLine, [&]()", "UIBridgeMFC metadata-line display-info builder does not yet route metadata-line selection through ResultDataAccess.");
            AssertContains(bridgeMfc, "void UIBridgeMFC::AppendResultMetaLineDisplayInfoToHyperEdit(const ResultMetaLineDisplayInfo& metaLineDisplayInfo,", "UIBridgeMFC does not yet implement the metadata-line display-info append helper.");
            AssertContains(bridgeMfc, "AppendResultMetaLineDisplayInfoToHyperEdit(GetResultMetaLineDisplayInfo(result, metaLine), hyerEdit);", "UIBridgeMFC metadata-line dispatch helper does not yet route through the grouped display-info seam.");
            AssertDoesNotContain(bridgeMfc, "sprintf_s(chSizeBuff, 1024, \"%I64u\", GetResultSize(result));", "UIBridgeMFC still formats file size inline instead of using the centralized file-size display helper.");
            AssertDoesNotContain(bridgeMfc, "Utils::ConvertSizeToShortSizeStr(GetResultSize(result))", "UIBridgeMFC still formats the short file-size string inline instead of using the centralized file-size display helper.");
            AssertDoesNotContain(bridgeMfc, "switch (metaLine)", "UIBridgeMFC still keeps metadata-line selection logic inline instead of routing through the centralized metadata-line dispatch helper.");
        }, failures);

        Run("Legacy MFC renderer still depends on fixed digest fields and uppercase formatting rules", () =>
        {
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");

            AssertContains(bridgeMfc, "AppendFileHashToHyperEdit", "Legacy MFC digest renderer is missing.");
            AssertContains(digestAccess, "FormatResultDigestForDisplay(const sunjwbase::tstring& digestValue, bool uppercase)", "ResultDigestAccess does not yet expose the centralized digest-display formatting helper.");
            AssertContains(digestAccess, "VisitResultDigestDisplayValues(const ResultData& result, bool uppercase, TResultDigestDisplayVisitor visitor)", "ResultDigestAccess does not yet expose the centralized formatted digest-display visitor helper.");
            AssertContains(digestAccess, "GetResultDigestDisplayInfo(const ResultDigestMetadata& digestMetadata, const sunjwbase::tstring& digestValue, bool uppercase)", "ResultDigestAccess does not yet expose the grouped digest display-info helper.");
            AssertContains(digestAccess, "return sunjwbase::strtotstr(sunjwbase::str_upper(sunjwbase::tstrtostr(digestValue)));", "ResultDigestAccess digest-display formatting helper does not yet uppercase digest values through the centralized seam.");
            AssertContains(digestAccess, "return sunjwbase::strtotstr(sunjwbase::str_lower(sunjwbase::tstrtostr(digestValue)));", "ResultDigestAccess digest-display formatting helper does not yet lowercase digest values through the centralized seam.");
            AssertContains(bridgeMfc, "VisitResultDigestDisplayValues(result, uppercase, [&](int index, const ResultDigestMetadata& digestMetadata, const ResultDigestDisplayInfo& digestDisplayInfo)", "Legacy MFC renderer does not yet route digest value formatting through the centralized digest-display visitor seam.");
            AssertDoesNotContain(bridgeMfc, "str_upper(tstrtostr(tstrFileDigest))", "Legacy MFC renderer still uppercases digest values inline instead of using the centralized digest-display seam.");
            AssertDoesNotContain(bridgeMfc, "str_lower(tstrtostr(tstrFileDigest))", "Legacy MFC renderer still lowercases digest values inline instead of using the centralized digest-display seam.");
            AssertContains(bridgeMfc, "AppendResultDigestDisplayInfoToHyperEdit(digestDisplayInfo, hyerEdit);", "Legacy MFC renderer no longer prints digest labels through the grouped digest display-info helper.");
            AssertContains(bridgeMfc, "AppendFileErrToHyperEdit", "Legacy MFC error renderer is missing.");
        }, failures);

        Run("Phase 6 extracts a desktop native core project and routes the legacy desktop app through linker coupling", () =>
        {
            string legacyProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string legacyFilters = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj.filters");
            string legacySolution = ReadRepoFile(repoRoot, @"trunk\fileshash15.sln");
            string desktopNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(desktopNativeProject, "<ProjectName>fHashNativeCore</ProjectName>", "Desktop native core project is missing.");
            AssertContains(desktopNativeProject, "..\\..\\trunk\\source\\Algorithms\\MD5.cpp", "Desktop native core project does not yet own MD5.cpp.");
            AssertContains(desktopNativeProject, "..\\..\\trunk\\source\\Common\\HashEngine.cpp", "Desktop native core project does not yet own HashEngine.cpp.");
            AssertContains(desktopNativeProject, "..\\..\\trunk\\source\\OsUtils\\OsFileWinApi.cpp", "Desktop native core project does not yet own OsFileWinApi.cpp.");
            AssertContains(desktopNativeProject, "..\\..\\trunk\\source\\WinCommon\\WindowsComm.cpp", "Desktop native core project does not yet own WindowsComm.cpp.");

            AssertDoesNotContain(legacyProject, "source\\Algorithms\\MD5.cpp", "Legacy desktop project still directly compiles MD5.cpp instead of linking the desktop native core.");
            AssertDoesNotContain(legacyProject, "source\\Algorithms\\SHA1.cpp", "Legacy desktop project still directly compiles SHA1.cpp instead of linking the desktop native core.");
            AssertDoesNotContain(legacyProject, "source\\Algorithms\\sha256.cpp", "Legacy desktop project still directly compiles sha256.cpp instead of linking the desktop native core.");
            AssertDoesNotContain(legacyProject, "source\\Algorithms\\sha512.cpp", "Legacy desktop project still directly compiles sha512.cpp instead of linking the desktop native core.");
            AssertDoesNotContain(legacyProject, "source\\Common\\HashEngine.cpp", "Legacy desktop project still directly compiles HashEngine.cpp instead of linking the desktop native core.");
            AssertDoesNotContain(legacyProject, "source\\Common\\strhelper.cpp", "Legacy desktop project still directly compiles strhelper.cpp instead of linking the desktop native core.");
            AssertDoesNotContain(legacyProject, "source\\OsUtils\\OsFileWinApi.cpp", "Legacy desktop project still directly compiles OsFileWinApi.cpp instead of linking the desktop native core.");
            AssertDoesNotContain(legacyProject, "source\\OsUtils\\OsThreadWinApi.cpp", "Legacy desktop project still directly compiles OsThreadWinApi.cpp instead of linking the desktop native core.");
            AssertDoesNotContain(legacyProject, "source\\WinCommon\\WindowsComm.cpp", "Legacy desktop project still directly compiles WindowsComm.cpp instead of linking the desktop native core.");
            AssertContains(legacyProject, "<AdditionalDependencies>fHashNativeCore.lib;version.lib;%(AdditionalDependencies)</AdditionalDependencies>", "Legacy desktop project does not yet link the extracted desktop native core library.");
            AssertContains(legacyProject, "<AdditionalLibraryDirectories>$(SolutionDir)$(Platform)\\$(Configuration)\\fHashNativeCore\\;%(AdditionalLibraryDirectories)</AdditionalLibraryDirectories>", "Legacy desktop project does not yet resolve the extracted desktop native core library through the current output-path coupling.");

            AssertDoesNotContain(legacyFilters, "source\\Algorithms\\MD5.cpp", "Legacy desktop filters still expose MD5.cpp even though the source moved to the desktop native core project.");
            AssertDoesNotContain(legacyFilters, "source\\Common\\HashEngine.cpp", "Legacy desktop filters still expose HashEngine.cpp even though the source moved to the desktop native core project.");
            AssertDoesNotContain(legacyFilters, "source\\OsUtils\\OsFileWinApi.cpp", "Legacy desktop filters still expose OsFileWinApi.cpp even though the source moved to the desktop native core project.");

            AssertContains(legacySolution, "Project(\"{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}\") = \"fHashNativeCore\", \"..\\sub-proj\\fHashNativeCore\\fHashNativeCore.vcxproj\", \"{E500D56F-3EE3-403C-A24C-034822AE3DF5}\"", "fileshash15.sln does not yet include the extracted desktop native core project.");
            AssertContains(legacySolution, "{E500D56F-3EE3-403C-A24C-034822AE3DF5} = {E500D56F-3EE3-403C-A24C-034822AE3DF5}", "fileshash15.sln does not yet make the legacy desktop app depend on the extracted desktop native core project.");
            AssertContains(legacySolution, "{E500D56F-3EE3-403C-A24C-034822AE3DF5}.Debug|Win32.ActiveCfg = Debug|Win32", "fileshash15.sln is missing the desktop native core Win32 debug mapping.");
            AssertContains(legacySolution, "{E500D56F-3EE3-403C-A24C-034822AE3DF5}.Release|x64.Build.0 = Release|x64", "fileshash15.sln is missing the desktop native core x64 release build mapping.");

            AssertContains(nativeProject, "..\\..\\trunk\\source\\Algorithms\\MD5.cpp", "WUINative no longer compiles MD5.cpp in the baseline layout.");
            AssertContains(nativeProject, "..\\..\\trunk\\source\\Common\\HashEngine.cpp", "WUINative no longer compiles HashEngine.cpp in the baseline layout.");
            AssertContains(nativeProject, "..\\..\\trunk\\source\\OsUtils\\OsFileWinApi.cpp", "WUINative no longer compiles OsFileWinApi.cpp in the baseline layout.");
        }, failures);

        Run("CLR bridge still depends on the native library through linker configuration in the baseline", () =>
        {
            string clrBridge = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\fHashClrBridge.vcxproj");

            AssertContains(clrBridge, "<AdditionalDependencies>fHashWUINative.lib;Version.lib;%(AdditionalDependencies)</AdditionalDependencies>", "CLR bridge no longer links the native library through AdditionalDependencies in the baseline layout.");
            AssertContains(clrBridge, "<AdditionalLibraryDirectories>$(ProjectDir)..\\fHashWUINative\\$(Platform)\\$(Configuration)\\fHashWUINative\\;$(SolutionDir)$(Platform)\\$(Configuration)\\fHashWUINative\\;%(AdditionalLibraryDirectories)</AdditionalLibraryDirectories>", "CLR bridge no longer resolves the native library through the current output-path coupling.");
        }, failures);

        Run("Phase 7 routes platform bridges through HashEngineBridge while leaving UIBridgeBase as a compatibility shim", () =>
        {
            string bridgeBase = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineBridge.h");
            string compatibilityBridge = ReadRepoFile(repoRoot, @"trunk\source\Common\UIBridgeBase.h");
            string bridgeMfcHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.h");
            string bridgeWuiHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.h");
            string bridgeUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.h");
            string bridgeMacHeader = ReadRepoFile(repoRoot, @"trunk\source\OSXUI\UIBridgeMacSwift.h");

            AssertContains(bridgeBase, "#include \"Common/HashEngineObserver.h\"", "HashEngineBridge does not yet layer directly on top of HashEngineObserver.");
            AssertContains(bridgeBase, "class HashEngineBridge: public HashEngineObserver", "HashEngineBridge is missing.");
            AssertContains(compatibilityBridge, "class UIBridgeBase: public HashEngineBridge", "UIBridgeBase is not yet reduced to a compatibility shim.");
            AssertDoesNotContain(compatibilityBridge, "virtual void showFileName(const ResultData& result) = 0;", "UIBridgeBase still duplicates file-notification methods instead of remaining a thin shim.");

            AssertContains(bridgeMfcHeader, "#include \"Common/HashEngineBridge.h\"", "MFC bridge header does not yet include the neutral HashEngineBridge seam.");
            AssertContains(bridgeMfcHeader, "class UIBridgeMFC: public HashEngineBridge", "MFC bridge does not yet inherit HashEngineBridge directly.");
            AssertDoesNotContain(bridgeMfcHeader, "#include \"Common/UIBridgeBase.h\"", "MFC bridge header still depends directly on the compatibility shim.");
            AssertDoesNotContain(bridgeMfcHeader, "class UIBridgeMFC: public UIBridgeBase", "MFC bridge still inherits the compatibility shim instead of HashEngineBridge.");

            AssertContains(bridgeWuiHeader, "#include \"Common/HashEngineBridge.h\"", "WinUI bridge header does not yet include the neutral HashEngineBridge seam.");
            AssertContains(bridgeWuiHeader, "class UIBridgeWUI : public HashEngineBridge", "WinUI bridge does not yet inherit HashEngineBridge directly.");
            AssertDoesNotContain(bridgeWuiHeader, "#include \"Common/UIBridgeBase.h\"", "WinUI bridge header still depends directly on the compatibility shim.");
            AssertDoesNotContain(bridgeWuiHeader, "class UIBridgeWUI : public UIBridgeBase", "WinUI bridge still inherits the compatibility shim instead of HashEngineBridge.");

            AssertContains(bridgeUwpHeader, "#include \"Common/HashEngineBridge.h\"", "UWP bridge header does not yet include the neutral HashEngineBridge seam.");
            AssertContains(bridgeUwpHeader, "class UIBridgeUwp : public HashEngineBridge", "UWP bridge does not yet inherit HashEngineBridge directly.");
            AssertDoesNotContain(bridgeUwpHeader, "#include \"Common/UIBridgeBase.h\"", "UWP bridge header still depends directly on the compatibility shim.");
            AssertDoesNotContain(bridgeUwpHeader, "class UIBridgeUwp : public UIBridgeBase", "UWP bridge still inherits the compatibility shim instead of HashEngineBridge.");

            AssertContains(bridgeMacHeader, "#include \"Common/HashEngineBridge.h\"", "macOS bridge header does not yet include the neutral HashEngineBridge seam.");
            AssertContains(bridgeMacHeader, "class UIBridgeMacSwift: public HashEngineBridge", "macOS bridge does not yet inherit HashEngineBridge directly.");
            AssertDoesNotContain(bridgeMacHeader, "#include \"Common/UIBridgeBase.h\"", "macOS bridge header still depends directly on the compatibility shim.");
            AssertDoesNotContain(bridgeMacHeader, "class UIBridgeMacSwift: public UIBridgeBase", "macOS bridge still inherits the compatibility shim instead of HashEngineBridge.");
        }, failures);

        if (failures.Count > 0)
        {
            Console.Error.WriteLine("Refactor baseline checks failed:");
            foreach (string failure in failures)
            {
                Console.Error.WriteLine($"- {failure}");
            }

            return 1;
        }

        Console.WriteLine("All refactor baseline checks passed.");
        return 0;
    }

    private static string ReadRepoFile(string repoRoot, string relativePath)
    {
        string path = Path.Combine(repoRoot, relativePath);
        return File.ReadAllText(path, DetectEncoding(path));
    }

    private static Encoding DetectEncoding(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 3 &&
            bytes[0] == 0xEF &&
            bytes[1] == 0xBB &&
            bytes[2] == 0xBF)
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        }

        return Encoding.GetEncoding(936);
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }

    private static void Run(string name, Action test, List<string> errors)
    {
        try
        {
            test();
            Console.WriteLine($"PASS: {name}");
        }
        catch (Exception ex)
        {
            errors.Add($"{name}: {ex.Message}");
            Console.WriteLine($"FAIL: {name}");
        }
    }

    private static void AssertContains(string content, string expected, string failureMessage)
    {
        if (!content.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(failureMessage);
        }
    }

    private static void AssertDoesNotContain(string content, string expected, string failureMessage)
    {
        if (content.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(failureMessage);
        }
    }

    private static void AssertInOrder(string content, IReadOnlyList<string> fragments, string failureMessage)
    {
        int lastIndex = -1;
        foreach (string fragment in fragments)
        {
            int index = content.IndexOf(fragment, lastIndex + 1, StringComparison.Ordinal);
            if (index < 0 || index < lastIndex)
            {
                throw new InvalidOperationException(failureMessage);
            }

            lastIndex = index;
        }
    }
}
