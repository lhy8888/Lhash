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
            AssertContains(global, "ResultDigestStorage storage;", "ResultData no longer carries digest storage through the grouped digest-state contract.");
            AssertDoesNotContain(global, "sunjwbase::tstring md5;", "Core ResultData should no longer carry the fixed MD5 field after the algorithm-domain cleanup.");
            AssertDoesNotContain(global, "sunjwbase::tstring sha1;", "Core ResultData should no longer carry the fixed SHA1 field after the algorithm-domain cleanup.");
            AssertDoesNotContain(global, "sunjwbase::tstring sha256;", "Core ResultData should no longer carry the fixed SHA256 field after the algorithm-domain cleanup.");
            AssertDoesNotContain(global, "sunjwbase::tstring sha512;", "Core ResultData should no longer carry the fixed SHA512 field after the algorithm-domain cleanup.");
            AssertContains(global, "sunjwbase::tstring error;", "ResultData no longer carries the error string in the baseline contract.");

            AssertContains(global, "class HashProgressSink;", "Global.h is missing the new phase-34 progress-sink forward declaration.");
            AssertContains(global, "struct ThreadData", "ThreadData baseline struct is missing.");
            AssertContains(global, "struct ThreadDataInputState", "ThreadData baseline struct is missing the grouped input-state seam.");
            AssertContains(global, "struct ThreadDataExecutionState", "ThreadData baseline struct is missing the grouped execution-state seam.");
            AssertContains(global, "HashProgressSink *observer;", "ThreadData is not yet narrowed to a neutral HashProgressSink observer in the phase-34 contract.");
            AssertContains(global, "ThreadDataInputState inputState;", "ThreadData no longer carries the grouped input-state field in the baseline contract.");
            AssertContains(global, "ThreadDataExecutionState executionState;", "ThreadData no longer carries the grouped execution-state field in the baseline contract.");
            AssertDoesNotContain(global, "HashEngineObserver *uiBridge;", "ThreadData still uses the UI-specific uiBridge field name in the phase-1 contract.");
            AssertDoesNotContain(global, "UIBridgeBase *uiBridge;", "ThreadData still directly depends on UIBridgeBase in the phase-1 contract.");
            AssertContains(global, "struct HashExecutionPreferenceState", "ThreadData baseline contract is missing the grouped execution-preference seam.");
            AssertContains(global, "struct HashCancellationState", "ThreadData baseline contract is missing the grouped cancellation seam.");
            AssertContains(global, "struct HashJobState", "ThreadData baseline contract is missing the grouped job-state seam.");
            AssertContains(global, "std::atomic<bool> working;", "ThreadData no longer carries the grouped working-state flag in the baseline contract.");
            AssertContains(global, "std::atomic<bool> stopRequested;", "ThreadData no longer carries the grouped stop flag in the baseline contract.");
            AssertContains(global, "bool uppercaseDigest;", "ThreadData no longer carries the grouped uppercase flag in the baseline contract.");
            AssertContains(global, "uint64_t countedSize;", "ThreadData no longer carries grouped totalSize in the baseline contract.");
            AssertContains(global, "uint32_t fileCount;", "ThreadData no longer carries nFiles in the baseline contract.");
            AssertContains(global, "TStrVector inputFiles;", "ThreadData no longer carries fullPaths in the baseline contract.");
            AssertContains(global, "HashResultList results;", "ThreadData no longer carries the grouped HashResultList execution store in the baseline contract.");
            AssertContains(global, "HashExecutionPreferenceState preferences;", "ThreadData no longer carries the grouped execution-preference state field in the baseline contract.");
            AssertContains(global, "HashCancellationState cancellation;", "ThreadData no longer carries the grouped cancellation state field in the baseline contract.");
            AssertContains(global, "HashJobState jobState;", "ThreadData no longer carries the grouped job-state field in the baseline contract.");
            AssertContains(global, "typedef HashResultList ResultList;", "Global.h no longer preserves the temporary ResultList compatibility alias while the legacy seams are being retired.");
            AssertDoesNotContain(global, "bool threadWorking;", "ThreadData still exposes the legacy threadWorking field name.");
            AssertDoesNotContain(global, "bool stop;", "ThreadData still exposes the legacy stop field name.");
            AssertDoesNotContain(global, "bool uppercase;", "ThreadData still exposes the legacy uppercase field name.");
            AssertDoesNotContain(global, "uint64_t totalSize;", "ThreadData still exposes the legacy totalSize field name.");
            AssertDoesNotContain(global, "uint32_t nFiles;", "ThreadData still exposes the legacy nFiles field name.");
            AssertDoesNotContain(global, "TStrVector fullPaths;", "ThreadData still exposes the legacy fullPaths field name.");
            AssertDoesNotContain(global, "ResultList resultList;", "ThreadData still exposes the legacy resultList field name.");
        }, failures);

        Run("Phase 1 keeps the core on HashProgressSink while moving adapter bridge semantics onto event-oriented callbacks", () =>
        {
            string observer = ReadRepoFile(repoRoot, @"trunk\source\Adapters\UiBridge\HashEngineObserver.h");
            string bridgeBase = ReadRepoFile(repoRoot, @"trunk\source\Adapters\UiBridge\HashEngineBridge.h");
            string legacyObserverPath = Path.Combine(repoRoot, @"trunk\source\Common\HashEngineObserver.h");
            string legacyBridgePath = Path.Combine(repoRoot, @"trunk\source\Common\HashEngineBridge.h");
            string legacyUiBridgeBasePath = Path.Combine(repoRoot, @"trunk\source\Common\UIBridgeBase.h");

            AssertContains(observer, "class HashEngineObserver", "Phase-1 observer seam is missing.");
            AssertContains(observer, "virtual void onJobPreparing() = 0;", "HashEngineObserver does not expose onJobPreparing.");
            AssertContains(observer, "virtual void onJobPreparationFinished() = 0;", "HashEngineObserver does not expose onJobPreparationFinished.");
            AssertContains(observer, "virtual void onJobCancelled() = 0;", "HashEngineObserver does not expose onJobCancelled.");
            AssertContains(observer, "virtual void onJobCompleted() = 0;", "HashEngineObserver does not expose onJobCompleted.");
            AssertContains(observer, "virtual void onFileResultEvent(const HashResult& result,", "HashEngineObserver does not yet expose a HashResult-based file-result event seam.");
            AssertContains(observer, "virtual int queryProgressMax() = 0;", "HashEngineObserver does not expose queryProgressMax.");
            AssertContains(observer, "virtual void onFileProgressValue(int value) = 0;", "HashEngineObserver does not expose onFileProgressValue.");
            AssertContains(observer, "virtual void onTotalProgressValue(int value) = 0;", "HashEngineObserver does not expose onTotalProgressValue.");
            AssertContains(observer, "virtual void onFileCalculated() = 0;", "HashEngineObserver does not expose onFileCalculated.");
            AssertContains(observer, "virtual void onFileFinished() = 0;", "HashEngineObserver does not expose onFileFinished.");
            AssertContains(observer, "onFileResultEvent(progressEvent.result, progressEvent.type, progressEvent.uppercaseDigest);", "HashEngineObserver does not yet route file-result lifecycle through the event-oriented callback.");
            AssertDoesNotContain(observer, "virtual void showFileName(const HashResult& result) = 0;", "HashEngineObserver still keeps the UI-specific showFileName contract.");
            AssertDoesNotContain(observer, "virtual void updateProgWhole(int value) = 0;", "HashEngineObserver still keeps the UI-specific updateProgWhole contract.");

            AssertContains(bridgeBase, "#include \"Adapters/UiBridge/HashEngineObserver.h\"", "HashEngineBridge does not yet layer directly on top of the shared adapter observer seam.");
            AssertContains(bridgeBase, "class HashEngineBridge: public HashEngineObserver", "HashEngineBridge is missing the neutral bridge seam on top of HashEngineObserver.");
            AssertContains(bridgeBase, "virtual void lockData() = 0;", "HashEngineBridge does not keep lockData on top of HashEngineObserver.");
            AssertContains(bridgeBase, "virtual void unlockData() = 0;", "HashEngineBridge does not keep unlockData on top of HashEngineObserver.");
            if (File.Exists(legacyObserverPath))
            {
                failures.Add("HashEngineObserver still lives in Common instead of the shared adapter seam.");
            }
            if (File.Exists(legacyBridgePath))
            {
                failures.Add("HashEngineBridge still lives in Common instead of the shared adapter seam.");
            }
            if (File.Exists(legacyUiBridgeBasePath))
            {
                failures.Add("UIBridgeBase still exists even though the shared adapter bridge seam has replaced it.");
            }
        }, failures);

        Run("Phase 1 HashEngine uses neutral observer wrappers and tiny emission helpers while preserving the current lifecycle and same-file multi-algorithm model", () =>
        {
            string engine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string fileRunner = string.Join(
                "\r\n",
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileRunner.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp"));
            string engineImpl = ReadHashEngineImplementation(repoRoot);

            AssertDoesNotContain(engine, "#include \"Common/HashEngineObserver.h\"", "HashEngine.cpp still directly includes HashEngineObserver after the progress-sink refactor.");
            AssertDoesNotContain(engine, "#include \"Common/UIBridgeBase.h\"", "HashEngine.cpp still directly includes UIBridgeBase in phase 1.");
            AssertContains(engine, "HashProgressSink *observer", "HashEngine.cpp is not yet narrowed to HashProgressSink.");
            AssertContains(engineImpl, "HashResult& BeginFileResult(", "HashEngine implementation set does not yet expose a tiny file-begin helper.");
            AssertContains(engineImpl, "uint64_t PrepareFileMetaResult(", "HashEngine implementation set does not yet expose a tiny file-meta helper.");
            AssertContains(engineImpl, "AccumulatePreScannedFileSize(", "HashEngine implementation set does not yet expose a tiny pre-scan file-size helper.");
            AssertContains(engineImpl, "TryPreScanSmallBatchFileSizes(", "HashEngine implementation set does not yet expose a tiny small-batch pre-scan helper.");
            AssertContains(engineImpl, "PrepareHashingWork(", "HashEngine implementation set does not yet expose a tiny preparation-phase helper.");
            AssertContains(engineImpl, "OpenFileForHashing(", "HashEngine implementation set does not yet expose a tiny file-open helper.");
            AssertContains(engineImpl, "InitializeFileAttemptState(", "HashEngine implementation set does not yet expose the grouped file-attempt state initializer introduced after phase 3.");
            AssertContains(fileRunner, "bool ProcessOpenedFileHashing(", "HashDigestPipeline.cpp does not yet expose a tiny opened-file processing helper.");
            AssertContains(engineImpl, "BeginFileHashAttempt(", "HashEngine implementation set does not yet expose a tiny file-attempt begin helper.");
            AssertContains(engineImpl, "ResetFileProgressState(", "HashEngine implementation set does not yet expose a tiny file-progress reset helper.");
            AssertContains(engineImpl, "struct FileProgressState", "HashEngine implementation set does not yet expose the grouped file-progress state bundle introduced after phase 3.");
            AssertContains(engineImpl, "struct FileAttemptState", "HashEngine implementation set does not yet expose the grouped file-attempt state bundle introduced after phase 3.");
            AssertContains(engineImpl, "struct FileHashContexts", "HashEngine implementation set does not yet expose the grouped file-hash context bundle introduced after phase 3.");
            AssertContains(engineImpl, "struct FileExecutionState", "HashEngine implementation set does not yet expose the grouped file-execution state bundle introduced after phase 3.");
            AssertContains(engineImpl, "InitializeFileHashing(", "HashEngine implementation set does not yet expose a tiny file-hashing initializer helper.");
            AssertContains(fileRunner, "static void YieldHashThread()", "HashFileRunner.cpp does not yet expose a tiny thread-yield helper.");
            AssertContains(fileRunner, "uint64_t CalculateFileChunkIterations(", "HashDigestPipeline.cpp does not yet expose a tiny chunk-iteration helper.");
            AssertContains(engineImpl, "UpdateWholeProgressAfterFile(", "HashEngine implementation set does not yet expose a tiny whole-progress helper.");
            AssertContains(engineImpl, "PopulateDigestResult(", "HashEngine implementation set does not yet expose a tiny digest-population helper.");
            AssertContains(engineImpl, "typedef ResultDigestStorage FinalizedDigestBundle;", "HashEngine implementation set does not yet expose the finalized digest bundle introduced after phase 3.");
            AssertContains(engineImpl, "GetFinalizedDigestValue(", "HashEngine implementation set does not yet expose the finalized-digest getter introduced after phase 3.");
            AssertContains(engineImpl, "SetFinalizedDigestValue(", "HashEngine implementation set does not yet expose the finalized-digest setter introduced after phase 3.");
            AssertContains(engineImpl, "FinalizeDigestStrings(", "HashEngine implementation set does not yet expose a tiny digest-finalization helper.");
            AssertContains(engineImpl, "CompleteSuccessfulFileHashing(", "HashEngine implementation set does not yet expose a tiny successful-file completion helper.");
            AssertContains(engineImpl, "CompleteOpenedFileAttempt(", "HashEngine implementation set does not yet expose a tiny opened-file completion helper.");
            AssertContains(engineImpl, "CompleteFileAttempt(", "HashEngine implementation set does not yet expose a tiny file-attempt completion helper.");
            AssertContains(engineImpl, "EmitOpenFileError(", "HashEngine implementation set does not yet expose a tiny open-file error helper.");
            AssertContains(engineImpl, "EmitReadFileError(", "HashEngine implementation set does not yet expose a tiny read-file error helper.");
            AssertContains(engineImpl, "FinishFileProcessing(", "HashEngine implementation set does not yet expose a tiny file-finished helper.");
            AssertContains(engineImpl, "EmitPathResult(", "HashEngine implementation set does not yet expose a tiny path-result helper.");
            AssertContains(engineImpl, "EmitMetaResult(", "HashEngine implementation set does not yet expose a tiny meta-result helper.");
            AssertContains(engineImpl, "EmitHashResult(", "HashEngine implementation set does not yet expose a tiny hash-result helper.");
            AssertContains(engineImpl, "EmitErrorResult(", "HashEngine implementation set does not yet expose a tiny error-result helper.");
            AssertContains(engineImpl, "EmitErrorMessageResult(", "HashEngine implementation set does not yet expose a tiny error-message helper.");
            AssertContains(engine, "static int CancelHashing(", "HashEngine.cpp does not yet expose a tiny cancellation helper.");
            AssertContains(engine, "static int CompleteHashing(", "HashEngine.cpp does not yet expose a tiny completion helper.");
            AssertContains(engineImpl, "ThreadPool threadPool(5);", "HashEngine implementation set no longer uses the current fixed-size thread pool in the baseline implementation.");
            AssertContains(engineImpl, "if (GetHashRequestFileCount(request) < 200)", "HashEngine no longer performs the current small-batch pre-scan in the baseline implementation.");
            AssertContains(engineImpl, "VisitHashRequestFiles(request, [&](uint32_t fileIndex, const tstring& fullPath)", "HashEngine implementation set no longer routes input-file iteration through the HashRequest contract seam.");
            AssertContains(engineImpl, "AccumulatePreScannedFileSize(executionContext, request, fSizes, fileIndex);", "HashEngine no longer routes the small-batch pre-scan loop body through the tiny helper.");
            AssertContains(engine, "bool wasCancelled = false;", "HashEngine no longer tracks small-batch pre-scan cancellation through the local helper contract.");
            AssertContains(engine, "isSizeCaled = PrepareHashingWork(executionContext, request, fSizes, &wasCancelled);", "HashEngine no longer routes the preparation phase through the tiny helper.");
            AssertContains(engine, "if (wasCancelled)", "HashEngine no longer handles small-batch pre-scan cancellation via the helper result.");
            AssertContains(fileRunner, "YieldHashThread();", "HashFileRunner no longer routes per-file scheduler yielding through the tiny helper.");
            AssertContains(fileRunner, "future<void> taskSHA512Update", "HashFileRunner no longer fans out SHA512 updates in the baseline implementation.");
            AssertContains(fileRunner, "future<void> taskSHA256Update", "HashFileRunner no longer fans out SHA256 updates in the baseline implementation.");
            AssertContains(fileRunner, "future<void> taskSHA1Update", "HashFileRunner no longer fans out SHA1 updates in the baseline implementation.");
            AssertContains(fileRunner, "future<void> taskMD5Update", "HashFileRunner no longer fans out MD5 updates in the baseline implementation.");
            AssertContains(engineImpl, "FileExecutionState executionState = { 0 };", "HashEngine implementation set no longer creates the grouped file-execution state bundle.");
            AssertContains(fileRunner, "HashResult& result = BeginFileHashAttempt(executionContext, fullPath, executionState, &path);", "HashFileRunner no longer routes the file-attempt setup through the grouped file-execution helper.");
            AssertContains(fileRunner, "InitializeFileAttemptState(path, &osFile, &executionState->fileAttemptState);", "HashFileRunner no longer routes file-attempt state initialization through the grouped execution helper.");
            AssertContains(fileRunner, "OpenFileForHashing(&executionState->fileAttemptState, (void *)&fExc);", "HashFileRunner no longer routes the file-open attempt through the grouped execution helper.");
            AssertContains(fileRunner, "bool wasStopped = ProcessOpenedFileHashing(executionContext, request, result, fileIndex, isSizeCaled, fSizes, executionState", "HashFileRunner no longer routes the opened-file processing loop through the grouped execution helper.");
            AssertContains(fileRunner, "CompleteFileAttempt(executionContext, request, result, fileIndex, isSizeCaled, *executionState);", "HashFileRunner no longer routes the file-attempt terminal path through the grouped execution bundle.");
            AssertContains(engineImpl, "EmitReadFileError(executionContext, result);", "HashEngine no longer routes read-file failures through the tiny helper.");
            AssertContains(engineImpl, "FinishFileProcessing(executionContext);", "HashEngine no longer routes file-finished callbacks through the tiny helper.");
            AssertContains(engineImpl, "EmitErrorResult(executionContext, result);", "HashEngine no longer emits errors through the tiny error-result helper in the baseline implementation.");
            AssertContains(engineImpl, "result.error = errorText;", "HashEngine error-message helper no longer writes the HashResult error text before emitting.");
            AssertContains(engineImpl, "EmitErrorMessageResult(executionContext, result,", "HashEngine no longer routes error-text emission through the tiny error-message helper.");
            AssertContains(engine, "return CancelHashing(executionContext);", "HashEngine no longer routes cancellation exits through the tiny cancellation helper.");
            AssertContains(engine, "return CompleteHashing(executionContext);", "HashEngine no longer routes the successful exit through the tiny completion helper.");
            AssertInOrder(fileRunner,
                [
                    "bool ProcessOpenedFileHashing(",
                    "InitializeFileHashing(",
                    "uint64_t fsize = PrepareFileMetaResult(",
                    "uint64_t times = CalculateFileChunkIterations(fsize);",
                    "do",
                    "while (!isFileFinished && !executionState->fileAttemptState.readFailed);",
                    "return false;"
                ],
                "HashEngine opened-file processing helper no longer preserves the expected read-loop order.");
            AssertInOrder(engineImpl,
                [
                    "BeginFileHashAttempt(",
                    "ResetFileProgressState(&executionState->progressState);",
                    "HashResult& result = BeginFileResult(executionContext, path);",
                    "*resultPath = result.path.c_str();",
                    "return result;"
                ],
                "HashEngine file-attempt-begin helper no longer preserves the expected setup order.");
            AssertInOrder(engineImpl,
                [
                    "ResetFileProgressState(",
                    "progressState->finishedSize = 0;",
                    "progressState->position = 0;"
                ],
                "HashEngine file-progress reset helper no longer preserves the expected reset order.");
            AssertInOrder(engineImpl,
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
            AssertInOrder(engineImpl,
                [
                    "InitializeFileAttemptState(",
                    "fileAttemptState->path = path;",
                    "fileAttemptState->osFile = osFile;",
                    "fileAttemptState->fileVersion.clear();",
                    "fileAttemptState->readFailed = false;",
                    "fileAttemptState->isFileOpened = false;",
                    "fileAttemptState->openErrorText = NULL;"
                ],
                "HashEngine file-attempt state initializer no longer preserves the expected setup order.");
            AssertInOrder(fileRunner,
                [
                    "uint64_t CalculateFileChunkIterations(",
                    "return fsize / DataBuffer::preflen + 1;"
                ],
                "HashEngine chunk-iteration helper no longer preserves the expected calculation.");
            AssertInOrder(engineImpl,
                [
                    "OpenFileForHashing(",
                    "fileAttemptState->readFailed = false;",
                    "fileAttemptState->openErrorText = (const TCHAR *)openErrorBuffer;",
                    "fileAttemptState->isFileOpened = fileAttemptState->osFile->openReadScan(openErrorBuffer);",
                    "return fileAttemptState->isFileOpened;"
                ],
                "HashEngine file-open helper no longer preserves the expected open-and-reset order.");
            AssertInOrder(engineImpl,
                [
                    "PrepareHashingWork(",
                    "observer->onProgressEvent(CreatePreparingProgressEvent());",
                    "bool isSizeCaled = TryPreScanSmallBatchFileSizes(executionContext, request, fSizes, wasCancelled);",
                    "if (*wasCancelled)",
                    "observer->onProgressEvent(CreatePreparationFinishedProgressEvent());",
                    "return isSizeCaled;"
                ],
                "HashEngine preparation helper no longer preserves the expected preparation order.");
            AssertInOrder(engineImpl,
                [
                    "TryPreScanSmallBatchFileSizes(",
                    "if (GetHashRequestFileCount(request) < 200)",
                    "*wasCancelled = true;",
                    "AccumulatePreScannedFileSize(executionContext, request, fSizes, fileIndex);",
                    "return true;"
                ],
                "HashEngine small-batch pre-scan helper no longer preserves the expected control flow.");
            AssertInOrder(engineImpl,
                [
                    "AccumulatePreScannedFileSize(",
                    "OsFile osFile(path);",
                    "fSizes[fileIndex] = fSize;",
                    "AddHashExecutionTotalSize(*executionContext, fSize);"
                ],
                "HashEngine pre-scan helper no longer preserves the expected size-accumulation order.");
            AssertInOrder(engineImpl,
                [
                    "PrepareFileMetaResult(",
                    "result.meta.modifiedDate = osFile.getModifiedTimeFormat();",
                    "EmitMetaResult(executionContext, result);",
                    "return fsize;"
                ],
                "HashEngine file-meta helper no longer preserves the expected metadata-emission order.");
            AssertInOrder(engine,
                [
                    "static int CompleteHashing(",
                    "observer->onProgressEvent(CreateCompletedProgressEvent());",
                    "SetHashExecutionWorking(*executionContext, false);",
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
            AssertInOrder(engineImpl,
                [
                    "CompleteSuccessfulFileHashing(",
                    "observer->onProgressEvent(CreateFileCalculatedProgressEvent());",
                    "FinalizeDigestStrings(",
                    "UpdateWholeProgressAfterFile(",
                    "fileAttemptState.osFile->close();",
                    "PopulateDigestResult(",
                    "EmitHashResult("
                ],
                "HashEngine successful-file helper no longer preserves the expected completion order.");
            AssertInOrder(engineImpl,
                [
                    "struct FileProgressState",
                    "uint64_t finishedSize;",
                    "uint64_t finishedSizeWhole;",
                    "int position;",
                    "int positionWhole;"
                ],
                "HashEngine grouped file-progress state bundle no longer keeps the current progress counters together.");
            AssertInOrder(engineImpl,
                [
                    "struct FileHashContexts",
                    "MD5_CTX mdContext;",
                    "CSHA1 sha1;",
                    "SHA256_CTX sha256Ctx;",
                    "SHA512_CTX sha512Ctx;",
                    "uint8_t digestSHA512[SHA512_DIGEST_LENGTH];"
                ],
                "HashEngine grouped file-hash context bundle no longer keeps the current algorithm contexts together.");
            AssertInOrder(engineImpl,
                [
                    "struct FileExecutionState",
                    "FileProgressState progressState;",
                    "FileAttemptState fileAttemptState;",
                    "FileHashContexts hashContexts;",
                    "FinalizedDigestBundle digestBundle;"
                ],
                "HashEngine grouped file-execution state bundle no longer keeps the current file-level execution state together.");
            AssertContains(engineImpl, "typedef ResultDigestStorage FinalizedDigestBundle;", "HashEngine finalized digest bundle no longer reuses the centralized digest storage shape.");
            AssertInOrder(engineImpl,
                [
                    "CompleteOpenedFileAttempt(",
                    "if (executionState.fileAttemptState.readFailed)",
                    "EmitReadFileError(executionContext, result);",
                    "CompleteSuccessfulFileHashing(",
                    "FinishFileProcessing(executionContext);"
                ],
                "HashEngine opened-file completion helper no longer preserves the expected branching order.");
            AssertInOrder(engineImpl,
                [
                    "CompleteOpenedFileAttempt(",
                    "CompleteSuccessfulFileHashing(executionContext, request, result, fileIndex, isSizeCaled, executionState);",
                    "FinishFileProcessing(executionContext);"
                ],
                "HashEngine no longer routes the successful opened-file path through the tiny helper.");
            AssertInOrder(engineImpl,
                [
                    "CompleteFileAttempt(",
                    "if (executionState.fileAttemptState.isFileOpened)",
                    "CompleteOpenedFileAttempt(",
                    "EmitOpenFileError(executionContext, result, executionState.fileAttemptState.openErrorText);",
                    "FinishFileProcessing(executionContext);"
                ],
                "HashEngine file-attempt helper no longer preserves the expected branching order.");
            AssertInOrder(engineImpl,
                [
                    "EmitOpenFileError(",
                    "EmitErrorMessageResult(executionContext, result, sunjwbase::tstring(errorText));"
                ],
                "HashEngine open-file error helper no longer preserves the expected emission order.");
            AssertInOrder(engineImpl,
                [
                    "EmitReadFileError(",
                    "EmitErrorMessageResult(executionContext, result, sunjwbase::strtotstr(std::string(\"Failed to read file while hashing.\")));"
                ],
                "HashEngine read-file error helper no longer preserves the expected emission order.");
            AssertInOrder(engineImpl,
                [
                    "FinishFileProcessing(",
                    "observer->onProgressEvent(CreateFileFinishedProgressEvent());"
                ],
                "HashEngine file-finished helper no longer preserves the expected emission order.");
            AssertDoesNotContain(engine, "result.enumState = RESULT_ALL;\r\n\r\n\t\t\t\tobserver->onFileHashReady(result, thrdData->uppercase);", "HashEngine still inlines the hash-result state transition instead of using the helper.");
            AssertDoesNotContain(engine, "thrdData->threadWorking = false;\r\n\r\n\t\t\t\tobserver->onCancelled();\r\n\t\t\t\treturn 0;", "HashEngine still inlines the final stop-and-cancel path instead of using the helper.");
            AssertDoesNotContain(engine, "observer->onCompleted();\r\n\r\n\tthrdData->threadWorking = false;\r\n\r\n\treturn 0;", "HashEngine still inlines the successful completion path instead of using the helper.");

            AssertInOrder(engine,
                [
                    "bool wasCancelled = false;",
                    "isSizeCaled = PrepareHashingWork(executionContext, request, fSizes, &wasCancelled);",
                    "if (wasCancelled)",
                    "if (!RunHashScheduler(executionContext, request, isSizeCaled, fSizes))",
                    "return CompleteHashing(executionContext);"
                ],
                "HashEngine lifecycle callbacks no longer follow the current baseline order.");
        }, failures);

        Run("Phase 1 assignment chains write observers through the neutral ThreadData field name", () =>
        {
            string mfcDialog = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcInitializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string clrBridge = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpBridge = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");
            string mfcDialogAndInitialization = mfcDialog + Environment.NewLine + mfcInitializationController;

            AssertContains(mfcDialogAndInitialization, "SetThreadDataObserver(*threadData, *uiBridgeMFC);", "MFC dialog no longer wires ThreadData through the neutral observer field.");
            AssertDoesNotContain(mfcDialog, "m_thrdData.uiBridge =", "MFC dialog still writes the legacy uiBridge field.");

            AssertContains(clrBridge, "SetThreadDataObserver(*m_pThreadData, m_pUiBridgeWUI);", "CLR bridge no longer wires ThreadData through the neutral observer field.");
            AssertDoesNotContain(clrBridge, "m_pThreadData->uiBridge =", "CLR bridge still writes the legacy uiBridge field.");

            AssertContains(uwpBridge, "SetThreadDataObserver(m_threadData, m_spUiBridgeUwp.get());", "UWP bridge no longer wires ThreadData through the neutral observer field.");
            AssertDoesNotContain(uwpBridge, "m_threadData.uiBridge =", "UWP bridge still writes the legacy uiBridge field.");
        }, failures);

        Run("Phase 5 routes managed ThreadData lifecycle through dedicated ThreadDataAccess helpers", () =>
        {
            string threadDataAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataAccess.h");
            string threadExecutionAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataExecutionAccess.h");
            string threadInputAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataInputAccess.h");
            string threadResultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataResultAccess.h");
            string threadAccess = string.Join("\r\n", threadDataAccess, threadExecutionAccess, threadInputAccess, threadResultAccess);
            string clrBridge = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpBridge = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");
            string mfcDialog = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcInitializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string mfcResultViewController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashResultViewController.cpp");
            string mfcResultLifecycle = string.Join("\r\n", mfcDialog, mfcResultViewController);

            AssertContains(threadAccess, "SetThreadDataObserver(ThreadData& threadData, HashProgressSink *observer)", "ThreadData access seams do not yet expose the progress-sink assignment helper.");
            AssertContains(threadAccess, "GetThreadDataObserver(const ThreadData& threadData)", "ThreadData access seams do not yet expose the observer getter helper.");
            AssertContains(threadAccess, "GetThreadDataInputState(const ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped input-state getter helper.");
            AssertContains(threadAccess, "GetMutableThreadDataInputState(ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped mutable input-state helper.");
            AssertContains(threadAccess, "GetThreadDataExecutionState(const ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped execution-state getter helper.");
            AssertContains(threadAccess, "GetMutableThreadDataExecutionState(ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped mutable execution-state helper.");
            AssertContains(threadAccess, "GetThreadDataInputFiles(const ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped input-files getter helper.");
            AssertContains(threadAccess, "GetMutableThreadDataInputFiles(ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped mutable input-files helper.");
            AssertContains(threadAccess, "GetMutableThreadDataResults(ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped mutable result-list helper.");
            AssertContains(threadAccess, "ResetThreadDataInputFiles(ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped input-file reset helper.");
            AssertContains(threadAccess, "SetThreadDataFileCount(ThreadData& threadData, uint32_t fileCount)", "ThreadData access seams do not yet expose the file-count helper.");
            AssertContains(threadAccess, "AddThreadDataFullPath(ThreadData& threadData, const sunjwbase::tstring& fullPath)", "ThreadData access seams do not yet expose the full-path append helper.");
            AssertContains(threadAccess, "AppendThreadDataInputFile(ThreadData& threadData, const sunjwbase::tstring& fullPath)", "ThreadData access seams do not yet expose the grouped input-file append helper.");
            AssertContains(threadAccess, "ResetThreadDataInputFilesAndAppend(ThreadData& threadData, uint32_t fileCount, TInputFileFactory inputFileFactory)", "ThreadData access seams do not yet expose the grouped batch input-file append helper.");
            AssertContains(threadAccess, "AppendTrimmedThreadDataInputFile(ThreadData& threadData, const sunjwbase::tstring& fullPath)", "ThreadData access seams do not yet expose the trimmed input-file append helper.");
            AssertContains(threadAccess, "AppendThreadDataInputFiles(ThreadData& threadData, const TStrVector& fullPaths)", "ThreadData access seams do not yet expose the grouped input-file list append helper.");
            AssertContains(threadAccess, "ReplaceThreadDataInputFiles(ThreadData& threadData, const TStrVector& fullPaths)", "ThreadData access seams do not yet expose the grouped input-file replacement helper.");
            AssertContains(threadAccess, "ReplaceTrimmedThreadDataInputFiles(ThreadData& threadData, const TStrVector& fullPaths)", "ThreadData access seams do not yet expose the grouped trimmed input-file replacement helper.");
            AssertContains(threadAccess, "AppendThreadDataResult(ThreadData& threadData)", "ThreadData access seams do not yet expose the result-append helper.");
            AssertContains(threadAccess, "ClearThreadDataResults(ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped result-clear helper.");
            AssertContains(threadAccess, "ResetThreadDataForNewSession(ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped reset helper.");
            AssertContains(threadAccess, "SetThreadDataStop(ThreadData& threadData, bool stopValue)", "ThreadData access seams do not yet expose the stop-flag helper.");
            AssertContains(threadAccess, "SetThreadDataWorking(ThreadData& threadData, bool working)", "ThreadData access seams do not yet expose the working-state setter helper.");
            AssertContains(threadAccess, "IsThreadDataWorking(const ThreadData& threadData)", "ThreadData access seams do not yet expose the working-state getter helper.");
            AssertContains(threadAccess, "SetThreadDataUppercase(ThreadData& threadData, bool uppercase)", "ThreadData access seams do not yet expose the uppercase helper.");
            AssertContains(threadAccess, "GetThreadDataUppercase(const ThreadData& threadData)", "ThreadData access seams do not yet expose the uppercase getter helper.");
            AssertContains(threadAccess, "GetThreadDataTotalSize(const ThreadData& threadData)", "ThreadData access seams do not yet expose the total-size helper.");
            AssertContains(threadAccess, "ResetThreadDataTotalSize(ThreadData& threadData)", "ThreadData access seams do not yet expose the total-size reset helper.");
            AssertContains(threadAccess, "AddThreadDataTotalSize(ThreadData& threadData, uint64_t sizeDelta)", "ThreadData access seams do not yet expose the total-size increment helper.");
            AssertContains(threadAccess, "ReplaceThreadDataCountedFileSize(ThreadData& threadData, uint64_t previousSize, uint64_t currentSize)", "ThreadData access seams do not yet expose the counted-file-size replacement helper.");
            AssertContains(threadAccess, "GetThreadDataFileCount(const ThreadData& threadData)", "ThreadData access seams do not yet expose the file-count getter helper.");
            AssertContains(threadAccess, "GetThreadDataResultCount(const ThreadData& threadData)", "ThreadData access seams do not yet expose the result-count helper.");
            AssertContains(threadAccess, "HasThreadDataInputFiles(const ThreadData& threadData)", "ThreadData access seams do not yet expose the input-file presence helper.");
            AssertContains(threadAccess, "ShouldStopThreadData(const ThreadData& threadData)", "ThreadData access seams do not yet expose the stop-flag getter helper.");
            AssertContains(threadAccess, "GetThreadDataFullPath(const ThreadData& threadData, uint32_t fileIndex)", "ThreadData access seams do not yet expose the grouped path getter helper.");
            AssertContains(threadAccess, "GetThreadDataResults(const ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped result-list getter.");
            AssertContains(threadAccess, "VisitThreadDataResults(const ThreadData& threadData, TResultVisitor visitor)", "ThreadData access seams do not yet expose the grouped result-list visitor.");
            AssertContains(threadAccess, "AddThreadDataFullPath(threadData, fullPath);", "ThreadData access seams grouped input-file append helper does not yet reuse the full-path seam.");
            AssertContains(threadAccess, "ResetThreadDataInputFiles(threadData);", "ThreadData access seams grouped batch input-file append helper does not yet reuse the input reset seam.");
            AssertContains(threadAccess, "SetThreadDataFileCount(threadData, fileCount);", "ThreadData access seams grouped batch input-file append helper does not yet reuse the file-count seam.");
            AssertContains(threadAccess, "AddThreadDataFullPath(threadData, inputFileFactory(fileIndex));", "ThreadData access seams grouped batch input-file append helper does not yet reuse the full-path append seam.");
            AssertContains(threadAccess, "sunjwbase::tstring trimmedPath = sunjwbase::strtrim(fullPath);", "ThreadData access seams trimmed input-file append helper does not yet normalize text through the shared trim seam.");
            AssertContains(threadAccess, "AppendThreadDataInputFile(threadData, trimmedPath);", "ThreadData access seams trimmed input-file append helper does not yet reuse the grouped input-file append seam.");
            AssertContains(threadAccess, "AppendThreadDataInputFile(threadData, *itr);", "ThreadData access seams grouped input-file list append helper does not yet reuse the grouped single-file append seam.");
            AssertContains(threadAccess, "AppendThreadDataInputFiles(threadData, fullPaths);", "ThreadData access seams grouped input-file replacement helper does not yet reuse the grouped list append seam.");
            AssertContains(threadAccess, "AppendTrimmedThreadDataInputFile(threadData, *itr);", "ThreadData access seams grouped trimmed input-file replacement helper does not yet reuse the trimmed single-file append seam.");
            AssertContains(threadAccess, "ClearThreadDataResults(threadData);", "ThreadData access seams grouped reset helper does not yet reuse the result-clear seam.");
            AssertContains(threadDataAccess, "ResetThreadDataInputFiles(threadData);", "ThreadDataAccess grouped reset helper does not yet reuse the input-file reset seam.");
            AssertContains(threadAccess, "return threadData.inputState;", "ThreadData access seams grouped input-state getter does not yet route through the neutral ThreadData field name.");
            AssertContains(threadAccess, "return threadData.executionState;", "ThreadData access seams grouped execution-state getter does not yet route through the neutral ThreadData field name.");
            AssertContains(threadAccess, "return GetThreadDataInputState(threadData).inputFiles;", "ThreadData access seams grouped input-files getter does not yet route through the grouped input-state seam.");
            AssertContains(threadAccess, "return GetMutableThreadDataInputState(threadData).inputFiles;", "ThreadData access seams grouped mutable input-files helper does not yet route through the grouped input-state seam.");
            AssertContains(threadAccess, "return GetMutableThreadDataHashJobState(threadData).results;", "ThreadData access seams grouped mutable result-list helper does not yet route through the grouped job-state seam.");
            AssertContains(threadAccess, "GetThreadDataHashExecutionPreferenceState(const ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped execution-preference seam.");
            AssertContains(threadAccess, "GetMutableThreadDataHashExecutionPreferenceState(ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped mutable execution-preference seam.");
            AssertContains(threadAccess, "GetThreadDataHashCancellationState(const ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped cancellation seam.");
            AssertContains(threadAccess, "GetMutableThreadDataHashCancellationState(ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped mutable cancellation seam.");
            AssertContains(threadAccess, "GetThreadDataHashJobState(const ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped job-state seam.");
            AssertContains(threadAccess, "GetMutableThreadDataHashJobState(ThreadData& threadData)", "ThreadData access seams do not yet expose the grouped mutable job-state seam.");
            AssertContains(threadAccess, "GetMutableThreadDataHashJobState(threadData).working.store(working);", "ThreadData access seams working-state setter does not yet route through the grouped job-state seam.");
            AssertContains(threadAccess, "return GetThreadDataHashJobState(threadData).working.load();", "ThreadData access seams working-state getter does not yet route through the grouped job-state seam.");
            AssertContains(threadAccess, "GetMutableThreadDataHashCancellationState(threadData).stopRequested.store(stopValue);", "ThreadData access seams stop setter does not yet route through the grouped cancellation seam.");
            AssertContains(threadAccess, "return GetThreadDataHashCancellationState(threadData).stopRequested.load();", "ThreadData access seams stop getter does not yet route through the grouped cancellation seam.");
            AssertContains(threadAccess, "GetMutableThreadDataHashExecutionPreferenceState(threadData).uppercaseDigest = uppercase;", "ThreadData access seams uppercase setter does not yet route through the grouped preference seam.");
            AssertContains(threadAccess, "return GetThreadDataHashExecutionPreferenceState(threadData).uppercaseDigest;", "ThreadData access seams uppercase getter does not yet route through the grouped preference seam.");
            AssertContains(threadAccess, "GetMutableThreadDataHashJobState(threadData).countedSize += sizeDelta;", "ThreadData access seams total-size increment helper does not yet route through the grouped job-state seam.");
            AssertContains(threadAccess, "return GetThreadDataInputState(threadData).fileCount;", "ThreadData access seams file-count getter does not yet route through the grouped input-state seam.");
            AssertContains(threadAccess, "return GetThreadDataInputFiles(threadData)[fileIndex];", "ThreadData access seams grouped path getter does not yet route through the neutral ThreadData field name.");
            AssertContains(threadAccess, "return GetThreadDataHashJobState(threadData).results;", "ThreadData access seams grouped result-list getter does not yet route through the grouped job-state seam.");

            AssertContains(clrBridge, "#include \"Common/ManagedHashMgmtAccess.h\"", "CLR bridge does not yet consume the current shared managed thread-data seam.");
            AssertContains(clrBridge, "SetThreadDataObserver(*m_pThreadData, m_pUiBridgeWUI);", "CLR bridge does not yet route observer assignment through ThreadDataAccess.");
            AssertContains(clrBridge, "ResetThreadDataForNewSession(*m_pThreadData);", "CLR bridge does not yet route Clear() through ThreadDataAccess.");
            AssertContains(clrBridge, "SetThreadDataStop(*m_pThreadData, val);", "CLR bridge does not yet route SetStop() through ThreadDataAccess.");
            AssertContains(clrBridge, "SetThreadDataUppercase(*m_pThreadData, val);", "CLR bridge does not yet route SetUppercase() through ThreadDataAccess.");
            AssertContains(clrBridge, "GetThreadDataTotalSize(*m_pThreadData);", "CLR bridge does not yet route GetTotalSize() through ThreadDataAccess.");
            AssertContains(clrBridge, "GetThreadDataResultCount(*m_pThreadData);", "CLR bridge does not yet route GetResultCount() through ThreadDataAccess.");
            AssertContains(clrBridge, "ReplaceThreadDataInputFilesFromManagedArray(*m_pThreadData, filePaths, ConvertManagedFilePathToTstr);", "CLR bridge does not yet route AddFiles() through the current compile-safe managed input-file helper.");

            AssertContains(uwpBridge, "#include \"Common/ManagedHashMgmtAccess.h\"", "UWP bridge does not yet consume the current shared managed thread-data seam.");
            AssertContains(uwpBridge, "SetThreadDataObserver(m_threadData, m_spUiBridgeUwp.get());", "UWP bridge does not yet route observer assignment through ThreadDataAccess.");
            AssertContains(uwpBridge, "ResetThreadDataForNewSession(m_threadData);", "UWP bridge does not yet route Clear() through ThreadDataAccess.");
            AssertContains(uwpBridge, "SetThreadDataStop(m_threadData, val);", "UWP bridge does not yet route SetStop() through ThreadDataAccess.");
            AssertContains(uwpBridge, "SetThreadDataUppercase(m_threadData, val);", "UWP bridge does not yet route SetUppercase() through ThreadDataAccess.");
            AssertContains(uwpBridge, "GetThreadDataTotalSize(m_threadData);", "UWP bridge does not yet route GetTotalSize() through ThreadDataAccess.");
            AssertContains(uwpBridge, "ReplaceThreadDataInputFilesFromManagedArray(m_threadData, filePaths, ConvertManagedFilePathToTstr);", "UWP bridge does not yet route AddFiles() through the grouped managed input-file helper.");

            AssertContains(mfcDialog, "#include \"Common/ThreadDataAccess.h\"", "MFC dialog does not yet consume the ThreadDataAccess seam.");
            string mfcInputController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInputController.cpp");
            string mfcMessageController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashMessageController.cpp");
            string mfcSearchController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSearchController.cpp");
            string mfcSessionController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSessionController.cpp");
            string mfcLifecycleController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashLifecycleController.cpp");
            string mfcDialogAndInitialization = string.Join("\r\n", mfcDialog, mfcInitializationController);
            string mfcDialogAndInput = mfcDialog + Environment.NewLine + mfcInputController;
            string mfcDialogAndMessage = mfcDialog + Environment.NewLine + mfcMessageController;
            string mfcDialogAndSearch = mfcDialog + Environment.NewLine + mfcSearchController;
            string mfcDialogAndLifecycle = mfcDialog + Environment.NewLine + mfcLifecycleController;
            string mfcDialogAndSession = mfcDialog + Environment.NewLine + mfcSessionController;
            AssertContains(mfcDialogAndInitialization, "SetThreadDataObserver(*threadData, *uiBridgeMFC);", "MFC dialog does not yet route observer assignment through ThreadDataAccess.");
            AssertContains(mfcDialogAndInitialization, "ResetThreadDataForNewSession(*threadData);", "MFC dialog does not yet route dialog initialization through ThreadDataAccess.");
            AssertContains(mfcDialogAndInput, "ReplaceThreadDataInputFiles(*m_threadData, parameters);", "MFC file-input flow does not yet route command-line file loading through the grouped ThreadDataAccess replacement helper.");
            AssertContains(mfcDialogAndInput, "AppendThreadDataInputFile(*m_threadData, tstrDragFilename);", "MFC file-input flow does not yet route drag-drop path appends through ThreadDataAccess.");
            AssertContains(mfcDialogAndInitialization, "HasThreadDataInputFiles(*threadData)", "MFC dialog does not yet route input-file presence checks through ThreadDataAccess.");
            AssertContains(mfcSessionController, "SetThreadDataUppercase(*m_threadData, (m_chkUppercase != NULL && m_chkUppercase->GetCheck() != FALSE));", "MFC session flow does not yet route uppercase updates through ThreadDataAccess.");
            AssertContains(mfcDialogAndInitialization, "SetThreadDataWorking(*threadData, false);", "MFC dialog does not yet route initial working-state resets through ThreadDataAccess.");
            AssertContains(mfcDialogAndMessage, "IsThreadDataWorking(*m_threadData)", "MFC dialog does not yet route working-state checks through ThreadDataAccess.");
            AssertContains(mfcDialogAndSearch, "GetThreadDataUppercase(*m_threadData)", "MFC search flow does not yet route uppercase reads through ThreadDataAccess.");
            AssertContains(mfcDialogAndLifecycle, "GetThreadDataTotalSize(*m_threadData)", "MFC dialog does not yet route total-size reads through ThreadDataAccess.");
            AssertContains(mfcDialogAndSearch, "VisitThreadDataHashResults(*m_threadData, [&](const HashResult& result)", "MFC search flow does not yet route grouped result traversal through ThreadDataAccess.");
            AssertContains(mfcDialogAndSearch, "VisitThreadDataHashResults(*m_threadData, [&](const HashResult& result)", "MFC search flow does not yet route grouped HashResult iteration through ThreadDataAccess.");
            AssertContains(mfcSessionController, "SetThreadDataStop(*m_threadData, false);", "MFC session flow does not yet route work-thread start stop-flag resets through ThreadDataAccess.");
            AssertContains(mfcSessionController, "SetThreadDataStop(*m_threadData, true);", "MFC session flow does not yet route work-thread stop requests through ThreadDataAccess.");
            AssertContains(mfcDialogAndInput, "ResetThreadDataInputFiles(*m_threadData);", "MFC file-input flow does not yet route file-path clearing through ThreadDataAccess.");
            AssertContains(mfcDialogAndInput, "ReplaceTrimmedThreadDataInputFiles(*m_threadData, parameters);", "MFC file-input flow does not yet route WM_COPYDATA file loading through the grouped trimmed ThreadDataAccess replacement helper.");
            AssertContains(mfcResultLifecycle, "ClearThreadDataResults(threadData);", "Legacy desktop result clearing does not yet route through ThreadDataAccess.");
        }, failures);

        Run("Phase 2 routes digest access through a neutral ResultData seam while keeping the fixed four-digest contract", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string hashAlgorithmRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashAlgorithmRegistry.h");
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string engine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string engineImpl = ReadHashEngineImplementation(repoRoot);
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");
            string mfcDialog = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string clrMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");
            string bridgeWuiHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.h");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(global, "enum ResultDigestType", "Global.h is missing the neutral digest enum that now anchors the mainline result contract.");
            AssertContains(global, "RESULT_DIGEST_MD5", "Global.h no longer exposes the MD5 digest slot.");
            AssertContains(global, "RESULT_DIGEST_SHA1", "Global.h no longer exposes the SHA1 digest slot.");
            AssertContains(global, "RESULT_DIGEST_SHA256", "Global.h no longer exposes the SHA256 digest slot.");
            AssertContains(global, "RESULT_DIGEST_SHA512", "Global.h no longer exposes the SHA512 digest slot.");
            AssertContains(digestAccess, "GetResultDigestCount()", "ResultDigestAccess is missing the neutral digest count helper.");
            AssertContains(digestAccess, "GetResultDigestTypeAt(int index)", "ResultDigestAccess is missing the neutral digest order helper.");
            AssertContains(digestAccess, "GetResultDigestLabel(ResultDigestType digestType)", "ResultDigestAccess is missing the neutral digest label helper.");
            AssertContains(digestAccess, "GetResultDigest(const ResultData& result, ResultDigestType digestType)", "ResultDigestAccess is missing the neutral digest getter.");
            AssertContains(digestAccess, "GetMutableResultDigest(ResultData& result, ResultDigestType digestType)", "ResultDigestAccess is missing the mutable digest getter.");
            AssertContains(digestAccess, "SetResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)", "ResultDigestAccess is missing the neutral digest setter.");
            AssertContains(digestAccess, "ResultContainsDigest(const ResultData& result, const sunjwbase::tstring& digestText)", "ResultDigestAccess is missing the neutral digest search helper.");
            AssertContains(digestAccess, "VisitResultDigests([&](ResultDigestType digestType)", "ResultDigestAccess no longer routes digest search through the neutral digest iteration helper.");
            AssertContains(digestAccess, "GetResultDigest(result, digestType).find(digestText)", "ResultDigestAccess no longer routes digest search through the neutral digest order helper.");

            AssertContains(engineImpl, "typedef ResultDigestStorage FinalizedDigestBundle;", "HashEngine does not yet centralize finalized digest strings through the finalized digest bundle.");
            AssertContains(engineImpl, "VisitHashRequestAlgorithms(request, [&](ResultDigestType digestType)", "HashEngine no longer routes digest publishing through the neutral digest iteration helper.");
            AssertContains(engineImpl, "GetFinalizedDigestValue(digestBundle, digestType)", "HashEngine does not yet read finalized digest strings through the finalized digest seam.");
            AssertContains(engineImpl, "HashDigestResult digestResult;", "HashEngine no longer materializes finalized digests through the neutral HashResult digest seam.");
            AssertContains(engineImpl, "digestResult.value = digestValue;", "HashEngine no longer assigns finalized digest text through the neutral HashResult digest seam.");
            AssertContains(engineImpl, "result.digests.push_back(digestResult);", "HashEngine no longer writes finalized digests through the HashResult digest collection seam.");
            AssertDoesNotContain(engine, "SetResultDigest(result, RESULT_DIGEST_MD5, tstrFileMD5);", "HashEngine still hardcodes the MD5 digest slot instead of iterating through the neutral seam.");
            AssertDoesNotContain(engine, "SetResultDigest(result, RESULT_DIGEST_SHA1, tstrFileSHA1);", "HashEngine still hardcodes the SHA1 digest slot instead of iterating through the neutral seam.");
            AssertDoesNotContain(engine, "SetResultDigest(result, RESULT_DIGEST_SHA256, tstrFileSHA256);", "HashEngine still hardcodes the SHA256 digest slot instead of iterating through the neutral seam.");
            AssertDoesNotContain(engine, "SetResultDigest(result, RESULT_DIGEST_SHA512, tstrFileSHA512);", "HashEngine still hardcodes the SHA512 digest slot instead of iterating through the neutral seam.");
            AssertDoesNotContain(engine, "result.tstrMD5 = tstrFileMD5;", "HashEngine still writes MD5 directly instead of going through the digest seam.");
            AssertDoesNotContain(engine, "result.tstrSHA1 = tstrFileSHA1;", "HashEngine still writes SHA1 directly instead of going through the digest seam.");
            AssertDoesNotContain(engine, "result.tstrSHA256 = tstrFileSHA256;", "HashEngine still writes SHA256 directly instead of going through the digest seam.");
            AssertDoesNotContain(engine, "result.tstrSHA512 = tstrFileSHA512;", "HashEngine still writes SHA512 directly instead of going through the digest seam.");

            AssertContains(bridgeMfc, "#include \"Common/ResultDigestRender.h\"", "UIBridgeMFC.cpp is not yet using the split digest-render seam.");
            AssertContains(bridgeMfc, "VisitResultDigestDisplayValues(result, uppercase, [&](int index, const ResultDigestMetadata& digestMetadata, const ResultDigestDisplayInfo& digestDisplayInfo)", "UIBridgeMFC no longer iterates formatted digest display values through the neutral visitor seam.");
            AssertDoesNotContain(bridgeMfc, "result.tstrMD5", "UIBridgeMFC still reads MD5 directly instead of using the digest seam.");
            AssertDoesNotContain(bridgeMfc, "result.tstrSHA1", "UIBridgeMFC still reads SHA1 directly instead of using the digest seam.");
            AssertDoesNotContain(bridgeMfc, "result.tstrSHA256", "UIBridgeMFC still reads SHA256 directly instead of using the digest seam.");
            AssertDoesNotContain(bridgeMfc, "result.tstrSHA512", "UIBridgeMFC still reads SHA512 directly instead of using the digest seam.");
            AssertDoesNotContain(bridgeMfc, "AppendTextToBuffer(_T(\"\\r\\nSHA1: \"))", "UIBridgeMFC still hardcodes digest label emission instead of using the digest seam.");
            AssertDoesNotContain(bridgeMfc, "AppendTextToBuffer(_T(\"\\r\\nSHA256: \"))", "UIBridgeMFC still hardcodes digest label emission instead of using the digest seam.");
            AssertDoesNotContain(bridgeMfc, "AppendTextToBuffer(_T(\"\\r\\nSHA512: \"))", "UIBridgeMFC still hardcodes digest label emission instead of using the digest seam.");

            string mfcSearchController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSearchController.cpp");
            AssertContains(mfcSearchController, "VisitThreadDataPathAndDigestMatchingHashResults(*m_threadData, tstrFileToFind, tstrHashToFind, [&](const HashResult& result)", "MFC search controller no longer routes digest search through the HashResult visitor seam.");
            AssertContains(clrMgmt, "CreateProjectedHashResultNetArray(size_t resultCount)", "CLR bridge search does not yet expose the compile-safe managed hash-result array factory.");
            AssertContains(clrMgmt, "SetProjectedHashResultNet(cli::array<HashResultNet>^ projectedResults, size_t index, HashResultNet hashResultNet)", "CLR bridge search does not yet expose the compile-safe managed hash-result array setter.");
            AssertContains(clrMgmt, "CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, cli::array<HashResultNet>^>(", "CLR bridge search no longer routes through the centralized managed hash-result projection seam.");
            AssertDoesNotContain(clrMgmt, "CreateCompatibilityResultDataNetArray(", "CLR bridge search still keeps the legacy ResultDataNet compatibility array helper instead of returning HashResultNet directly.");
            AssertDoesNotContain(clrMgmt, "FindResult(String^ sstrHashToFind)", "CLR bridge search still exposes the legacy ResultDataNet find API instead of using HashResultNet directly.");
            AssertContains(uwpMgmt, "CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, Array<HashResultNet>^>(", "UWP bridge search no longer routes through the centralized managed hash-result projection seam.");
            AssertDoesNotContain(uwpMgmt, "CreateCompatibilityResultDataNetArray(", "UWP bridge search still keeps the legacy ResultDataNet compatibility array helper instead of returning HashResultNet directly.");
            AssertDoesNotContain(uwpMgmt, "FindResult(String^ pstrHashToFind)", "UWP bridge search still exposes the legacy ResultDataNet find API instead of using HashResultNet directly.");
            string resultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataProjection.h");
            string resultNetProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultNetProjection.h");

            AssertContains(resultNetProjection, "template<typename TResultDataNet, typename TResultString>", "ResultNetProjection does not yet expose the centralized ResultDataNet digest-assignment template.");
            AssertContains(resultNetProjection, "static inline TResultDataNet AssignResultDigestToNet(TResultDataNet resultDataNet, ResultDigestType digestType, TResultString digestValue)", "ResultNetProjection does not yet expose the centralized ResultDataNet digest-assignment helper.");
            AssertContains(resultProjection, "template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter>", "ResultDataProjection does not yet expose the centralized ResultDataNet projection template.");
            AssertContains(resultProjection, "static inline TResultDataNet ProjectResultDataToNet(const ResultData& result, TStringConverter convertString)", "ResultDataProjection does not yet expose the centralized ResultDataNet projection helper.");
            AssertContains(resultProjection, "AssignHashResultDigestsToNet(resultDataNet, ProjectHashResult(result), convertString);", "ResultDataProjection does not yet route managed digest projection through HashResultProjection.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route HashResultNet projection through the centralized projection helper.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route HashResultNet projection through the dedicated managed bridge helper.");
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

            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route HashResultNet projection through the centralized projection helper.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route HashResultNet projection through the dedicated managed bridge helper.");
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
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string engineImpl = ReadHashEngineImplementation(repoRoot);
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(global, "struct ResultDigestStorage", "ResultData digest storage has not yet been wrapped in the dedicated storage struct introduced in phase 3.");
            AssertContains(global, "std::vector<sunjwbase::tstring> values;", "ResultDigestStorage does not yet expose the registry-sized internal digest storage introduced in the algorithm-domain cleanup.");
            AssertContains(global, "struct ResultDigestState", "ResultData digest state is not yet wrapped in the dedicated digest-state struct introduced in phase 3.");
            AssertContains(global, "ResultDigestStorage storage;", "ResultDigestState does not yet route internal digest storage through the dedicated storage struct introduced in phase 3.");
            AssertDoesNotContain(global, "struct ResultDigestCompatibilityFields", "Core digest state still keeps the old fixed four-slot compatibility struct after the algorithm-domain cleanup.");
            AssertDoesNotContain(global, "compatibilityFields;", "Core digest state still keeps the old fixed four-slot compatibility field after the algorithm-domain cleanup.");
            AssertContains(global, "ResultDigestState digestState;", "ResultData does not yet route digest storage and legacy compatibility through the dedicated digest-state struct introduced in phase 3.");
            AssertDoesNotContain(global, "sunjwbase::tstring md5;", "Core digest state still keeps the legacy MD5 compatibility field after the algorithm-domain cleanup.");
            AssertDoesNotContain(global, "sunjwbase::tstring sha1;", "Core digest state still keeps the legacy SHA1 compatibility field after the algorithm-domain cleanup.");
            AssertDoesNotContain(global, "sunjwbase::tstring sha256;", "Core digest state still keeps the legacy SHA256 compatibility field after the algorithm-domain cleanup.");
            AssertDoesNotContain(global, "sunjwbase::tstring sha512;", "Core digest state still keeps the legacy SHA512 compatibility field after the algorithm-domain cleanup.");

            AssertContains(digestAccess, "GetResultDigestIndex(ResultDigestType digestType)", "ResultDigestAccess is missing the neutral digest-index helper introduced in phase 3.");
            AssertDoesNotContain(digestAccess, "GetCompatibilityResultDigest(const ResultData& result, ResultDigestType digestType)", "Digest access still exposes the removed compatibility getter after the algorithm-domain cleanup.");
            AssertDoesNotContain(digestAccess, "GetMutableCompatibilityResultDigest(ResultData& result, ResultDigestType digestType)", "Digest access still exposes the removed mutable compatibility getter after the algorithm-domain cleanup.");
            AssertContains(digestAccess, "return GetStoredResultDigest(result, digestType);", "ResultDigestAccess does not yet route digest reads through the registry-sized stored-digest path.");
            AssertContains(digestAccess, "return GetMutableStoredResultDigest(result, digestType);", "ResultDigestAccess does not yet expose mutable access through the internal digest storage in phase 3.");
            AssertDoesNotContain(digestAccess, "GetMutableCompatibilityResultDigest(result, digestType) = digestValue;", "Digest writes still mirror back into removed compatibility fields.");

            AssertContains(engineImpl, "const ResultDigestMetadata& digestMetadata = GetResultDigestMetadata(digestType);", "HashEngine no longer routes finalized digest metadata lookup through the phase-3 digest seam.");
            AssertContains(engineImpl, "result.digests.push_back(digestResult);", "HashEngine no longer routes finalized digest writes through the HashResult digest seam.");
            AssertContains(bridgeMfc, "VisitResultDigestDisplayValues(result, uppercase, [&](int index, const ResultDigestMetadata& digestMetadata, const ResultDigestDisplayInfo& digestDisplayInfo)", "MFC digest rendering no longer reads through the phase-5 formatted digest-display visitor seam.");
        }, failures);

        Run("Phase 3 resets digest storage through the neutral seam when a file result is created", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string engineImpl = ReadHashEngineImplementation(repoRoot);

            AssertContains(digestAccess, "ResetResultDigests(ResultData& result)", "ResultDigestAccess does not yet expose the digest-reset helper introduced in phase 3.");
            AssertContains(digestAccess, "VisitResultDigests([&](ResultDigestType digestType)", "ResultDigestAccess digest-reset helper does not yet iterate through the centralized visitor helper.");
            AssertContains(digestAccess, "ClearStoredResultDigest(result, digestType);", "ResultDigestAccess digest-reset helper does not yet clear internal digest storage.");
            AssertDoesNotContain(digestAccess, "ClearCompatibilityResultDigest(result, digestType);", "ResultDigestAccess digest-reset helper still clears removed compatibility fields.");

            AssertContains(engineImpl, "result = HashResult();", "HashEngine does not yet reset HashResult storage through the grouped reset seam when a file result is created.");
            AssertInOrder(engineImpl,
                [
                    "BeginFileResult(",
                    "HashResult& result = AppendHashExecutionResult(*executionContext);",
                    "result = HashResult();",
                    "result.path = path;",
                    "EmitPathResult(executionContext, result);"
                ],
                "HashEngine file-result begin helper no longer resets digest storage before publishing the file path.");
        }, failures);

        Run("Phase 3 centralizes the internal digest storage count instead of duplicating the magic number", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string hashAlgorithmRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashAlgorithmRegistry.h");
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(global, "std::vector<sunjwbase::tstring> values;", "ResultData internal digest storage is not yet routed through a registry-sized vector.");
            AssertContains(hashAlgorithmRegistry, "struct HashAlgorithmDescriptorRegistry", "HashAlgorithmRegistry does not yet wrap the descriptor table in a dedicated registry object.");
            AssertContains(hashAlgorithmRegistry, "GetHashAlgorithmDescriptorRegistry()", "HashAlgorithmRegistry does not yet expose the centralized descriptor-registry helper.");
            AssertContains(hashAlgorithmRegistry, "sizeof(algorithmDescriptors) / sizeof(HashAlgorithmDescriptor)", "HashAlgorithmRegistry does not yet derive the registered algorithm count from the descriptor table.");
            AssertContains(digestAccess, "return GetRegisteredHashAlgorithmCount();", "ResultDigestAccess does not yet route digest count through the centralized registry-count helper.");
        }, failures);

        Run("Phase 3 routes direct internal digest-slot access through stored-digest helpers", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(digestAccess, "GetStoredResultDigest(const ResultData& result, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the const stored-digest helper introduced in phase 3.");
            AssertContains(digestAccess, "GetMutableStoredResultDigest(ResultData& result, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the mutable stored-digest helper introduced in phase 3.");
            AssertContains(digestAccess, "return GetDigestStorageValue(GetResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest helpers do not yet route into the dedicated internal digest storage struct.");
            AssertContains(digestAccess, "return GetStoredResultDigest(result, digestType);", "ResultDigestAccess getter does not yet read through the stored-digest helper.");
            AssertContains(digestAccess, "return GetMutableStoredResultDigest(result, digestType);", "ResultDigestAccess mutable getter does not yet route through the stored-digest helper.");
            AssertContains(digestAccess, "ClearDigestStorageValue(GetMutableResultDigestStorage(result), digestType);", "ResultDigestAccess digest-reset helper does not yet clear digests through the stored-digest helper.");
        }, failures);

        Run("Phase 3 routes legacy digest synchronization through dedicated compatibility helpers", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertDoesNotContain(digestAccess, "SetCompatibilityResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)", "Digest access still exposes the removed compatibility write helper.");
            AssertDoesNotContain(digestAccess, "ClearCompatibilityResultDigest(ResultData& result, ResultDigestType digestType)", "Digest access still exposes the removed compatibility clear helper.");
            AssertDoesNotContain(digestAccess, "SetCompatibilityResultDigest(result, digestType, digestValue);", "Digest writes still synchronize removed compatibility helpers.");
            AssertDoesNotContain(digestAccess, "ClearCompatibilityResultDigest(result, digestType);", "Digest resets still synchronize removed compatibility helpers.");
        }, failures);

        Run("Phase 3 routes legacy digest field selection through a single compatibility-field helper", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertDoesNotContain(digestAccess, "GetCompatibilityResultDigestField(ResultDigestType digestType)", "Digest access still exposes the removed compatibility field-selector helper.");
            AssertDoesNotContain(digestAccess, "GetResultDigestMetadataCompatibilityValueField(const ResultDigestMetadata& digestMetadata)", "Digest metadata still exposes the removed compatibility-field accessor.");
            AssertDoesNotContain(digestAccess, "return GetHashAlgorithmDescriptorCompatibilityValueField(digestMetadata);", "Digest metadata still routes through the removed compatibility-field seam.");
            AssertDoesNotContain(digestAccess, "return GetResultDigestMetadataCompatibilityValueField(GetResultDigestMetadata(digestType));", "Digest access still routes through the removed compatibility metadata selector.");
            AssertDoesNotContain(digestAccess, "return GetResultDigestCompatibilityFields(result).*GetCompatibilityResultDigestField(digestType);", "Digest access still reads removed compatibility fields.");
        }, failures);

        Run("Phase 3 centralizes digest metadata so type, label, and legacy-field mapping share one source of truth", () =>
        {
            string hashAlgorithmRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashAlgorithmRegistry.h");
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(hashAlgorithmRegistry, "struct HashAlgorithmDescriptor", "HashAlgorithmRegistry does not yet expose the centralized algorithm metadata struct introduced in phase 11.");
            AssertContains(digestAccess, "typedef HashAlgorithmDescriptor ResultDigestMetadata;", "ResultDigestAccess does not yet bridge digest metadata onto the centralized algorithm descriptor.");
            AssertContains(digestAccess, "GetResultDigestMetadataAt(int index)", "ResultDigestAccess does not yet expose the centralized digest metadata lookup helper introduced in phase 3.");
            AssertContains(hashAlgorithmRegistry, "{ RESULT_DIGEST_MD5, \"md5\", \"MD5\" }", "HashAlgorithmRegistry metadata table does not yet map MD5.");
            AssertContains(hashAlgorithmRegistry, "{ RESULT_DIGEST_SHA1, \"sha1\", \"SHA1\" }", "HashAlgorithmRegistry metadata table does not yet map SHA1.");
            AssertContains(hashAlgorithmRegistry, "{ RESULT_DIGEST_SHA256, \"sha256\", \"SHA256\" }", "HashAlgorithmRegistry metadata table does not yet map SHA256.");
            AssertContains(hashAlgorithmRegistry, "{ RESULT_DIGEST_SHA512, \"sha512\", \"SHA512\" }", "HashAlgorithmRegistry metadata table does not yet map SHA512.");
            AssertContains(digestAccess, "GetResultDigestMetadataType(const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess does not yet expose the metadata type accessor.");
            AssertContains(digestAccess, "GetResultDigestMetadataDisplayLabel(const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess does not yet expose the metadata label accessor.");
            AssertContains(digestAccess, "return GetHashAlgorithmTypeAt(index);", "ResultDigestAccess digest-order helper does not yet route through the registry type accessor.");
            AssertContains(digestAccess, "GetResultDigestLabel(const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess does not yet expose the metadata-label overload introduced in phase 5.");
            AssertContains(digestAccess, "return GetResultDigestMetadataDisplayLabel(digestMetadata);", "ResultDigestAccess metadata-label overload does not yet route through the metadata label accessor.");
            AssertContains(digestAccess, "return GetHashAlgorithmDescriptor(digestType);", "ResultDigestAccess type-based metadata lookup does not yet route through the registry descriptor seam.");
        }, failures);

        Run("Phase 3 routes type-based metadata lookup and stored-digest presence checks through dedicated helpers", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(digestAccess, "GetResultDigestMetadata(ResultDigestType digestType)", "ResultDigestAccess does not yet expose the type-based metadata lookup helper introduced in phase 3.");
            AssertContains(digestAccess, "return GetHashAlgorithmDescriptor(digestType);", "ResultDigestAccess type-based metadata lookup helper does not yet route through the registry descriptor seam.");
            AssertContains(digestAccess, "HasStoredResultDigest(const ResultData& result, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the stored-digest presence helper introduced in phase 3.");
            AssertContains(digestAccess, "return HasDigestStorageValue(GetResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest presence helper does not yet route through the stored-digest helper.");
            AssertContains(digestAccess, "return GetResultDigestLabel(GetResultDigestMetadata(digestType));", "ResultDigestAccess digest-label helper does not yet route through the metadata-label overload.");
            AssertDoesNotContain(digestAccess, "GetResultDigestMetadataCompatibilityValueField", "ResultDigestAccess still exposes compatibility-field metadata lookup after the algorithm-domain cleanup.");
            AssertContains(digestAccess, "return GetStoredResultDigest(result, digestType);", "ResultDigestAccess digest getter does not yet route through the dedicated stored-digest helper.");
        }, failures);

        Run("Phase 3 routes stored digest writes and clears through dedicated internal-storage helpers", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(digestAccess, "SetStoredResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)", "ResultDigestAccess does not yet expose the stored-digest write helper introduced in phase 3.");
            AssertContains(digestAccess, "ClearStoredResultDigest(ResultData& result, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the stored-digest clear helper introduced in phase 3.");
            AssertContains(digestAccess, "SetStoredResultDigest(result, digestType, digestValue);", "ResultDigestAccess setter does not yet route internal storage writes through the dedicated helper.");
            AssertContains(digestAccess, "ClearStoredResultDigest(result, digestType);", "ResultDigestAccess reset helper does not yet route internal storage clearing through the dedicated helper.");
        }, failures);

        Run("Phase 3 routes digest-index lookup through centralized metadata instead of a standalone switch", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(digestAccess, "GetResultDigestIndex(ResultDigestType digestType)", "ResultDigestAccess does not yet expose the digest-index helper.");
            AssertContains(digestAccess, "return GetHashAlgorithmIndex(digestType);", "ResultDigestAccess digest-index helper does not yet route through the registry index seam.");
            AssertDoesNotContain(digestAccess, "switch (digestType)", "ResultDigestAccess digest-index helper still uses the old standalone switch mapping.");
        }, failures);

        Run("Phase 3 routes digest iteration through a dedicated metadata visitor helper", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(digestAccess, "template<typename TResultDigestVisitor>", "ResultDigestAccess does not yet expose the digest-visitor template introduced in phase 3.");
            AssertContains(digestAccess, "VisitResultDigests(TResultDigestVisitor visitor)", "ResultDigestAccess does not yet expose the centralized digest visitor helper.");
            AssertContains(digestAccess, "VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess digest visitor helper does not yet route through the centralized metadata visitor helper.");
            AssertContains(digestAccess, "return visitor(GetResultDigestMetadataType(digestMetadata));", "ResultDigestAccess digest visitor helper does not yet route through the metadata type accessor.");
            AssertContains(digestAccess, "VisitResultDigests([&](ResultDigestType digestType)", "ResultDigestAccess does not yet route digest iteration through the centralized visitor helper.");
            AssertContains(digestAccess, "return true;", "ResultDigestAccess digest visitor usage no longer preserves the current early-success semantics.");
        }, failures);

        Run("Phase 3 routes metadata iteration through a dedicated metadata visitor helper", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(digestAccess, "template<typename TResultDigestMetadataVisitor>", "ResultDigestAccess does not yet expose the metadata-visitor template introduced in phase 3.");
            AssertContains(digestAccess, "VisitResultDigestMetadata(TResultDigestMetadataVisitor visitor)", "ResultDigestAccess does not yet expose the centralized metadata visitor helper.");
            AssertContains(digestAccess, "visitor(index, GetResultDigestMetadataAt(index))", "ResultDigestAccess metadata visitor helper does not yet route through the centralized metadata lookup helper.");
            AssertContains(digestAccess, "VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess does not yet route metadata iteration through the centralized metadata visitor helper.");
            AssertContains(digestAccess, "return visitor(GetResultDigestMetadataType(digestMetadata));", "ResultDigestAccess digest visitor does not yet route through the metadata visitor helper.");
            AssertContains(digestAccess, "return GetHashAlgorithmIndex(digestType);", "ResultDigestAccess digest-index helper does not yet route through the centralized registry seam.");
        }, failures);

        Run("Phase 3 routes digest-value iteration through a dedicated value visitor helper and reuses it in managed bridges", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string resultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataProjection.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(digestAccess, "template<typename TResultDigestValueVisitor>", "ResultDigestAccess does not yet expose the digest-value visitor template introduced in phase 3.");
            AssertContains(digestAccess, "VisitResultDigestValues(const ResultData& result, TResultDigestValueVisitor visitor)", "ResultDigestAccess does not yet expose the centralized digest-value visitor helper.");
            AssertContains(digestAccess, "return VisitResultDigests([&](ResultDigestType digestType)", "ResultDigestAccess digest-value visitor helper does not yet route through the centralized digest visitor helper.");
            AssertContains(digestAccess, "return visitor(digestType, GetResultDigest(result, digestType));", "ResultDigestAccess digest-value visitor helper does not yet feed values through the neutral digest seam.");

            AssertContains(resultProjection, "AssignHashResultDigestsToNet(resultDataNet, ProjectHashResult(result), convertString);", "ResultDataProjection does not yet consume digest values through HashResultProjection when projecting managed result data.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet consume digest values through the centralized HashResultNet projection helper.");
            AssertDoesNotContain(bridgeWui, "String^ digestValue = ConvertTstrToSystemString(GetResultDigest(result, digestType).c_str());", "WinUI bridge still performs inline digest lookup instead of consuming the digest-value visitor payload.");

            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet consume digest values through the centralized HashResultNet projection helper.");
            AssertDoesNotContain(bridgeUwp, "String^ digestValue = ConvertToPlatStr(GetResultDigest(result, digestType).c_str());", "UWP bridge still performs inline digest lookup instead of consuming the digest-value visitor payload.");
        }, failures);

        Run("Phase 3 promotes ResultDigestStorage into a reusable neutral storage seam and reuses it for finalized digest bundles", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string engineImpl = ReadHashEngineImplementation(repoRoot);

            AssertContains(digestAccess, "GetDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the reusable digest-storage getter seam.");
            AssertContains(digestAccess, "GetMutableDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the reusable mutable digest-storage seam.");
            AssertContains(digestAccess, "HasDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the reusable digest-storage presence seam.");
            AssertContains(digestAccess, "SetDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType, const sunjwbase::tstring& digestValue)", "ResultDigestAccess does not yet expose the reusable digest-storage write seam.");
            AssertContains(digestAccess, "ClearDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the reusable digest-storage clear seam.");
            AssertContains(digestAccess, "EnsureDigestStorageSize(digestStorage);", "ResultDigestAccess reusable digest-storage seam does not yet size storage from the registry before mutation.");
            AssertContains(digestAccess, "return digestStorage.values[digestIndex];", "ResultDigestAccess reusable digest-storage seam does not yet route through the centralized digest index.");
            AssertContains(digestAccess, "return GetDigestStorageValue(GetResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest getter does not yet reuse the neutral digest-storage seam.");
            AssertContains(digestAccess, "return GetMutableDigestStorageValue(GetMutableResultDigestStorage(result), digestType);", "ResultDigestAccess mutable stored-digest getter does not yet reuse the neutral digest-storage seam.");
            AssertContains(digestAccess, "return HasDigestStorageValue(GetResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest presence helper does not yet reuse the neutral digest-storage seam.");
            AssertContains(digestAccess, "SetDigestStorageValue(GetMutableResultDigestStorage(result), digestType, digestValue);", "ResultDigestAccess stored-digest setter does not yet reuse the neutral digest-storage seam.");
            AssertContains(digestAccess, "ClearDigestStorageValue(GetMutableResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest clear helper does not yet reuse the neutral digest-storage seam.");

            AssertContains(engineImpl, "typedef ResultDigestStorage FinalizedDigestBundle;", "HashEngine does not yet reuse ResultDigestStorage as the finalized digest bundle.");
            AssertContains(engineImpl, "GetDigestStorageValue(digestBundle, digestType)", "HashEngine finalized-digest getter does not yet reuse the neutral digest-storage seam.");
            AssertContains(engineImpl, "SetDigestStorageValue(digestBundle, digestType, digestValue);", "HashEngine finalized-digest setter does not yet reuse the neutral digest-storage seam.");
            AssertDoesNotContain(engineImpl, "tstring digestValues[RESULT_DIGEST_STORAGE_COUNT];", "HashEngine still duplicates digest storage layout inside FinalizedDigestBundle instead of reusing the neutral storage seam.");
        }, failures);

        Run("Phase 3 routes ResultData digest-state access through dedicated state and field helpers", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(digestAccess, "GetResultDigestState(const ResultData& result)", "ResultDigestAccess does not yet expose the const digest-state helper introduced in phase 3.");
            AssertContains(digestAccess, "GetMutableResultDigestState(ResultData& result)", "ResultDigestAccess does not yet expose the mutable digest-state helper introduced in phase 3.");
            AssertContains(digestAccess, "GetResultDigestStorage(const ResultData& result)", "ResultDigestAccess does not yet expose the const result-digest-storage helper introduced in phase 3.");
            AssertContains(digestAccess, "GetMutableResultDigestStorage(ResultData& result)", "ResultDigestAccess does not yet expose the mutable result-digest-storage helper introduced in phase 3.");
            AssertDoesNotContain(digestAccess, "GetResultDigestCompatibilityFields(const ResultData& result)", "Core digest access still exposes the removed digest-compatibility-fields helper.");
            AssertDoesNotContain(digestAccess, "GetMutableResultDigestCompatibilityFields(ResultData& result)", "Core digest access still exposes the removed mutable digest-compatibility-fields helper.");
            AssertContains(digestAccess, "return result.digestState;", "ResultDigestAccess digest-state helpers do not yet route through ResultData::digestState.");
            AssertContains(digestAccess, "return GetResultDigestState(result).storage;", "ResultDigestAccess const digest-storage helper does not yet route through the digest-state seam.");
            AssertContains(digestAccess, "return GetMutableResultDigestState(result).storage;", "ResultDigestAccess mutable digest-storage helper does not yet route through the digest-state seam.");
            AssertDoesNotContain(digestAccess, "compatibilityFields", "Core digest access still routes through removed compatibility fields.");
            AssertContains(digestAccess, "return GetDigestStorageValue(GetResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest getter does not yet route through the result-digest-storage helper.");
            AssertContains(digestAccess, "return GetMutableDigestStorageValue(GetMutableResultDigestStorage(result), digestType);", "ResultDigestAccess mutable stored-digest getter does not yet route through the result-digest-storage helper.");
            AssertContains(digestAccess, "return HasDigestStorageValue(GetResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest presence helper does not yet route through the result-digest-storage helper.");
            AssertContains(digestAccess, "SetDigestStorageValue(GetMutableResultDigestStorage(result), digestType, digestValue);", "ResultDigestAccess stored-digest setter does not yet route through the result-digest-storage helper.");
            AssertContains(digestAccess, "ClearDigestStorageValue(GetMutableResultDigestStorage(result), digestType);", "ResultDigestAccess stored-digest clear helper does not yet route through the result-digest-storage helper.");
            AssertDoesNotContain(digestAccess, "GetCompatibilityResultDigestField", "Core digest access still exposes the removed compatibility-field selector.");
        }, failures);

        Run("Phase 3 routes digest metadata and values through a shared visitor seam for legacy MFC rendering", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string digestRender = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestRender.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(digestAccess, "template<typename TResultDigestMetadataValueVisitor>", "ResultDigestAccess does not yet expose the digest-metadata-value visitor template introduced in phase 3.");
            AssertContains(digestAccess, "VisitResultDigestMetadataValues(const ResultData& result, TResultDigestMetadataValueVisitor visitor)", "ResultDigestAccess does not yet expose the centralized digest-metadata-value visitor helper.");
            AssertContains(digestAccess, "return VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess digest-metadata-value visitor does not yet route through centralized metadata iteration.");
            AssertContains(digestAccess, "return visitor(index, digestMetadata, GetResultDigest(result, GetResultDigestMetadataType(digestMetadata)));", "ResultDigestAccess digest-metadata-value visitor does not yet feed value lookups through the metadata type accessor.");
            AssertContains(digestRender, "VisitResultDigestDisplayValues(const ResultData& result, bool uppercase, TResultDigestDisplayVisitor visitor)", "ResultDigestRender does not yet expose the centralized formatted digest-display visitor helper.");

            AssertContains(digestRender, "struct ResultDigestDisplayInfo", "ResultDigestRender does not yet expose the grouped digest display-info structure.");
            AssertContains(digestRender, "GetResultDigestDisplayInfo(const ResultDigestMetadata& digestMetadata, const sunjwbase::tstring& digestValue, bool uppercase)", "ResultDigestRender does not yet expose the grouped digest display-info helper.");
            AssertContains(digestRender, "digestDisplayInfo.label = GetResultDigestLabel(digestMetadata);", "ResultDigestRender digest display-info helper does not yet route labels through the metadata seam.");
            AssertContains(digestRender, "digestDisplayInfo.value = FormatResultDigestForDisplay(digestValue, uppercase);", "ResultDigestRender digest display-info helper does not yet route formatting through the centralized display seam.");
            AssertContains(digestRender, "ResultDigestDisplayInfo digestDisplayInfo = GetResultDigestDisplayInfo(digestMetadata, digestValueTstr, uppercase);", "ResultDigestRender formatted digest-display visitor does not yet route label/value assembly through the grouped digest display-info helper.");
            AssertContains(digestRender, "return visitor(index, digestMetadata, digestDisplayInfo);", "ResultDigestRender formatted digest-display visitor does not yet pass grouped digest display-info to consumers.");

            AssertContains(bridgeMfc, "VisitResultDigestDisplayValues(result, uppercase, [&](int index, const ResultDigestMetadata& digestMetadata, const ResultDigestDisplayInfo& digestDisplayInfo)", "Legacy MFC digest renderer does not yet consume formatted digest display values through the centralized visitor seam.");
            AssertDoesNotContain(bridgeMfc, "for (int index = 0; index < GetResultDigestCount(); index++)", "Legacy MFC digest renderer still performs manual index iteration instead of using the centralized metadata-value visitor seam.");
            AssertDoesNotContain(bridgeMfc, "ResultDigestType digestType = GetResultDigestTypeAt(index);", "Legacy MFC digest renderer still resolves digest order manually instead of using the centralized metadata-value visitor seam.");
            AssertDoesNotContain(bridgeMfc, "GetResultDigest(result, digestType)", "Legacy MFC digest renderer still performs inline digest lookup instead of using the visitor payload.");
        }, failures);

        Run("Phase 4 introduces a neutral ResultData access seam for non-digest UI and bridge reads", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string resultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataProjection.h");
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

            AssertContains(bridgeMfc, "#include \"Common/ResultDataRender.h\"", "Legacy MFC bridge does not yet consume the split result-data render seam.");
            AssertContains(bridgeMfc, "AppendLabelValueLineToHyperEdit(GetStringByKey(FILENAME_STRING),", "Legacy MFC bridge does not yet route the file path through the current ResultDataAccess-backed append helper.");
            AssertContains(bridgeMfc, "GetResultSizeDisplayInfo(result)", "Legacy MFC bridge does not yet read the file size through ResultDataAccess.");
            AssertContains(bridgeMfc, "GetResultModifiedDate(result)", "Legacy MFC bridge does not yet read the modified date through the current ResultDataAccess-backed metadata-display helper.");
            AssertContains(bridgeMfc, "GetResultVersion(result)", "Legacy MFC bridge does not yet read the version through ResultDataAccess.");
            AssertContains(bridgeMfc, "AppendTextLineToHyperEdit(GetResultError(result), hyerEdit);", "Legacy MFC bridge does not yet read the error text through the current ResultDataAccess-backed append helper.");

            AssertContains(resultProjection, "AssignHashResultCoreToNet<TResultDataNet, TResultStateNet>(resultDataNet, ProjectHashResult(result), convertString);", "ResultDataProjection does not yet read core fields through HashResultProjection.");
            AssertContains(resultProjection, "AssignResultCoreToNet(TResultDataNet resultDataNet, const ResultData& result, TStringConverter convertString)", "ResultDataProjection does not yet expose the centralized core-field ResultDataNet assignment helper.");
            AssertContains(resultProjection, "AssignResultDigestsToNet(TResultDataNet resultDataNet, const ResultData& result, TStringConverter convertString)", "ResultDataProjection does not yet expose the centralized digest ResultDataNet assignment helper.");
            AssertContains(resultProjection, "TResultDataNet resultDataNet = AssignResultCoreToNet<TResultDataNet, TResultStateNet>(TResultDataNet(), result, convertString);", "ResultDataProjection does not yet route ResultDataNet projection through the centralized core assignment helper.");
            AssertContains(resultProjection, "return AssignResultDigestsToNet(resultDataNet, result, convertString);", "ResultDataProjection does not yet route ResultDataNet projection through the centralized digest assignment helper.");

            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route non-digest reads through the centralized HashResultNet projection helper.");

            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route non-digest reads through the centralized HashResultNet projection helper.");

            string filesHashSearchController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSearchController.cpp");
            AssertContains(filesHashSearchController, "#include \"Common/HashResultSearch.h\"", "Legacy MFC search flow does not yet consume the shared HashResult search seam.");
            AssertContains(filesHashSearchController, "VisitThreadDataPathAndDigestMatchingHashResults(*m_threadData, tstrFileToFind, tstrHashToFind, [&](const HashResult& result)", "Legacy MFC search flow does not yet read result paths through the shared HashResult path-match seam.");
        }, failures);

        Run("Phase 4 routes non-digest ResultData writes through dedicated ResultDataAccess setters", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string engine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string engineImpl = ReadHashEngineImplementation(repoRoot);

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

            AssertContains(engineImpl, "result.path = path;", "HashEngine does not yet route result path writes through the HashResult contract.");
            AssertContains(engineImpl, "result.meta.modifiedDate = osFile.getModifiedTimeFormat();", "HashEngine does not yet route modified-date writes through the HashResult contract.");
            AssertContains(engineImpl, "result.meta.size = fsize;", "HashEngine does not yet route size writes through the HashResult contract.");
            AssertContains(engineImpl, "result.meta.version = tstrFileVersion;", "HashEngine does not yet route version writes through the HashResult contract.");
            AssertContains(engineImpl, "result.error = errorText;", "HashEngine does not yet route error writes through the HashResult contract.");
            AssertContains(engineImpl, "*resultPath = result.path.c_str();", "HashEngine does not yet route file-attempt path binding through the HashResult contract.");
        }, failures);

        Run("Phase 4 routes ResultState reads and writes through the neutral ResultDataAccess seam", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string resultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataProjection.h");
            string engineImpl = ReadHashEngineImplementation(repoRoot);
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(resultAccess, "GetResultState(const ResultData& result)", "ResultDataAccess does not yet expose the neutral ResultState getter introduced in phase 4.");
            AssertContains(resultAccess, "SetResultState(ResultData& result, ResultState resultState)", "ResultDataAccess does not yet expose the neutral ResultState setter introduced in phase 4.");
            AssertContains(resultAccess, "return GetResultCoreState(result).state;", "ResultDataAccess ResultState getter does not yet route through the current ResultData field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).state = resultState;", "ResultDataAccess ResultState setter does not yet route through the current ResultData field.");

            AssertContains(engineImpl, "result.state = RESULT_PATH;", "HashEngine does not yet route path-state writes through the HashResult contract.");
            AssertContains(engineImpl, "result.state = RESULT_META;", "HashEngine does not yet route meta-state writes through the HashResult contract.");
            AssertContains(engineImpl, "result.state = RESULT_ALL;", "HashEngine does not yet route hash-state writes through the HashResult contract.");
            AssertContains(engineImpl, "result.state = RESULT_ERROR;", "HashEngine does not yet route error-state writes through the HashResult contract.");

            AssertContains(bridgeMfc, "ResultState resultState = GetResultState(result);", "Legacy MFC renderer does not yet read ResultState through ResultDataAccess.");
            AssertDoesNotContain(bridgeMfc, "if (result.enumState == RESULT_NONE)", "Legacy MFC renderer still branches directly on ResultData::enumState.");

            AssertContains(resultProjection, "AssignHashResultCoreToNet<TResultDataNet, TResultStateNet>(resultDataNet, ProjectHashResult(result), convertString);", "ResultDataProjection does not yet route managed ResultState projection through HashResultProjection.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet read HashResultState through the centralized HashResultNet projection helper.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet read HashResultState through the centralized HashResultNet projection helper.");
        }, failures);

        Run("Phase 5 routes MFC result-section rendering policy through dedicated ResultDataAccess helpers", () =>
        {
            string resultRender = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataRender.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(resultRender, "struct ResultRenderPolicy", "ResultDataRender does not yet expose the grouped ResultState render-policy structure.");
            AssertContains(resultRender, "GetResultRenderPolicy(ResultState resultState)", "ResultDataRender does not yet expose the grouped ResultState render-policy helper.");
            AssertContains(resultRender, "IsResultStateNone(ResultState resultState)", "ResultDataRender does not yet expose the neutral ResultState-empty helper for MFC rendering.");
            AssertContains(resultRender, "ShouldRenderResultFileName(ResultState resultState)", "ResultDataRender does not yet expose the file-name render-policy helper.");
            AssertContains(resultRender, "ShouldRenderResultMeta(ResultState resultState)", "ResultDataRender does not yet expose the metadata render-policy helper.");
            AssertContains(resultRender, "ShouldRenderResultHash(ResultState resultState)", "ResultDataRender does not yet expose the hash render-policy helper.");
            AssertContains(resultRender, "ShouldRenderResultError(ResultState resultState)", "ResultDataRender does not yet expose the error render-policy helper.");
            AssertContains(resultRender, "ShouldAppendResultTrailingLineBreak(ResultState resultState)", "ResultDataRender does not yet expose the trailing-line-break render-policy helper.");
            AssertContains(resultRender, "return GetResultRenderPolicy(resultState).renderFileName;", "ResultDataRender file-name render-policy helper does not yet route through the grouped render policy.");
            AssertContains(resultRender, "return GetResultRenderPolicy(resultState).renderMeta;", "ResultDataRender metadata render-policy helper does not yet route through the grouped render policy.");
            AssertContains(resultRender, "return GetResultRenderPolicy(resultState).renderHash;", "ResultDataRender hash render-policy helper does not yet route through the grouped render policy.");
            AssertContains(resultRender, "return GetResultRenderPolicy(resultState).renderError;", "ResultDataRender error render-policy helper does not yet route through the grouped render policy.");
            AssertContains(resultRender, "return GetResultRenderPolicy(resultState).appendTrailingLineBreak;", "ResultDataRender trailing-line-break helper does not yet route through the grouped render policy.");

            AssertContains(bridgeMfc, "if (IsResultStateNone(resultState))", "Legacy MFC renderer does not yet route empty-state checks through the ResultDataAccess render-policy helper.");
            AssertContains(bridgeMfc, "if (ShouldAppendResultTrailingLineBreak(resultState))", "Legacy MFC renderer does not yet route trailing-line-break checks through the ResultDataAccess render-policy helper.");

            AssertDoesNotContain(bridgeMfc, "if (resultState == RESULT_NONE)", "Legacy MFC renderer still performs inline empty-state checks instead of using the ResultDataAccess helper.");
            AssertDoesNotContain(bridgeMfc, "if (resultState != RESULT_ALL &&", "Legacy MFC renderer still performs inline trailing-break checks instead of using the ResultDataAccess helper.");
        }, failures);

        Run("Phase 5 routes MFC result-section iteration through a centralized ResultDataAccess visitor", () =>
        {
            string resultRender = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataRender.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(resultRender, "enum ResultRenderSectionType", "ResultDataRender does not yet expose the result render-section enum introduced in phase 5.");
            AssertContains(resultRender, "RESULT_RENDER_SECTION_FILE_NAME", "ResultDataRender is missing the file-name render section.");
            AssertContains(resultRender, "RESULT_RENDER_SECTION_META", "ResultDataRender is missing the metadata render section.");
            AssertContains(resultRender, "RESULT_RENDER_SECTION_HASH", "ResultDataRender is missing the hash render section.");
            AssertContains(resultRender, "RESULT_RENDER_SECTION_ERROR", "ResultDataRender is missing the error render section.");
            AssertContains(resultRender, "template<typename TResultRenderSectionVisitor>", "ResultDataRender does not yet expose the render-section visitor template.");
            AssertContains(resultRender, "VisitRenderableResultSections(ResultState resultState, TResultRenderSectionVisitor visitor)", "ResultDataRender does not yet expose the centralized render-section visitor helper.");
            AssertContains(resultRender, "visitor(RESULT_RENDER_SECTION_FILE_NAME)", "ResultDataRender render-section visitor does not yet route file-name rendering through the centralized visitor path.");
            AssertContains(resultRender, "visitor(RESULT_RENDER_SECTION_META)", "ResultDataRender render-section visitor does not yet route metadata rendering through the centralized visitor path.");
            AssertContains(resultRender, "visitor(RESULT_RENDER_SECTION_HASH)", "ResultDataRender render-section visitor does not yet route hash rendering through the centralized visitor path.");
            AssertContains(resultRender, "visitor(RESULT_RENDER_SECTION_ERROR)", "ResultDataRender render-section visitor does not yet route error rendering through the centralized visitor path.");
            AssertContains(resultRender, "DispatchResultRenderSectionByType(ResultRenderSectionType renderSection, TFileNameAction onFileName, TMetaAction onMeta, THashAction onHash, TErrorAction onError)", "ResultDataRender does not yet expose the centralized render-section dispatch helper.");

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
            string resultRender = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataRender.h");

            AssertContains(bridgeMfcHeader, "AppendResultRenderSectionToHyperEdit(const ResultData& result,", "UIBridgeMFC does not yet expose the dedicated render-section dispatch helper.");
            AssertContains(bridgeMfc, "void UIBridgeMFC::AppendResultRenderSectionToHyperEdit(const ResultData& result,", "UIBridgeMFC does not yet implement the dedicated render-section dispatch helper.");
            AssertContains(resultRender, "DispatchResultRenderSectionByType(ResultRenderSectionType renderSection, TFileNameAction onFileName, TMetaAction onMeta, THashAction onHash, TErrorAction onError)", "ResultDataRender does not yet expose the centralized render-section dispatch helper.");
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
            AssertContains(bridgeMfc, "AppendResultSectionAndRefresh(result, RESULT_RENDER_SECTION_HASH, uppercaseDigest);", "UIBridgeMFC does not yet route file-hash rendering through the dedicated refresh helper.");
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
            string resultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataProjection.h");

            AssertContains(resultProjection, "template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter, typename TResultHandler>", "ResultDataProjection does not yet expose the centralized project-and-dispatch helper template.");
            AssertContains(resultProjection, "ProjectAndDispatchResult(const ResultData& result, TStringConverter convertString, TResultHandler resultHandler)", "ResultDataProjection does not yet expose the centralized project-and-dispatch helper.");
            AssertContains(resultProjection, "resultHandler(ProjectResultDataToNet<TResultDataNet, TResultStateNet>(result, convertString));", "ResultDataProjection project-and-dispatch helper does not yet compose projection and dispatch through the centralized projection seam.");
            AssertContains(managedDispatch, "TResultDataNet resultDataNet = ProjectHashResultToNet<TResultDataNet, TResultStateNet>(result, convertString);", "Common managed-bridge dispatch header does not yet materialize projected managed results through the centralized projection seam.");
            AssertDoesNotContain(managedDispatch, "ProjectManagedBridgeResultAndDispatch(const ResultData& result, TStringConverter convertString, TResultHandler resultHandler)", "Common managed-bridge dispatch header still keeps the redundant managed projection-dispatch wrapper.");

            AssertContains(bridgeWuiHeader, "#include \"Common/ManagedBridgeDispatch.h\"", "WinUI bridge header does not yet include the common managed-bridge dispatch header.");
            AssertDoesNotContain(bridgeWuiHeader, "#include \"Common/ManagedBridgeHelpers.h\"", "WinUI bridge header still depends on the deprecated managed-bridge helper header.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route showFile* methods through the common managed projection-dispatch helper.");
            AssertContains(bridgeWui, "m_uiBridgeDelegates->ShowFileName(hashResultNet);", "WinUI bridge no longer forwards projected file-name results through the current delegate path.");
            AssertContains(bridgeWui, "m_uiBridgeDelegates->ShowFileMeta(hashResultNet);", "WinUI bridge no longer forwards projected file-meta results through the current delegate path.");
            AssertContains(bridgeWui, "m_uiBridgeDelegates->ShowFileHash(hashResultNet, hashUppercase);", "WinUI bridge no longer forwards projected file-hash results through the current delegate path.");
            AssertContains(bridgeWui, "m_uiBridgeDelegates->ShowFileErr(hashResultNet);", "WinUI bridge no longer forwards projected file-error results through the current delegate path.");
            AssertDoesNotContain(bridgeWuiHeader, "void ProjectManagedResultAndDispatch(const ResultData& result, TResultHandler resultHandler)", "WinUI bridge still keeps the local managed projection-dispatch template instead of using the centralized seam.");
            AssertDoesNotContain(bridgeWui, "ResultDataNet resultDataNet = ConvertResultDataToNet(result);", "WinUI bridge still inlines projected result creation inside showFile* methods instead of using the dedicated helper.");

            AssertContains(bridgeUwpHeader, "#include \"Common/ManagedBridgeDispatch.h\"", "UWP bridge header does not yet include the common managed-bridge dispatch header.");
            AssertDoesNotContain(bridgeUwpHeader, "#include \"Common/ManagedBridgeHelpers.h\"", "UWP bridge header still depends on the deprecated managed-bridge helper header.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route showFile* methods through the common managed projection-dispatch helper.");
            AssertContains(bridgeUwp, "m_uiBridgeDelegate->ShowFileName(hashResultNet);", "UWP bridge no longer forwards projected file-name results through the current delegate path.");
            AssertContains(bridgeUwp, "m_uiBridgeDelegate->ShowFileMeta(hashResultNet);", "UWP bridge no longer forwards projected file-meta results through the current delegate path.");
            AssertContains(bridgeUwp, "m_uiBridgeDelegate->ShowFileHash(hashResultNet, hashUppercase);", "UWP bridge no longer forwards projected file-hash results through the current delegate path.");
            AssertContains(bridgeUwp, "m_uiBridgeDelegate->ShowFileErr(hashResultNet);", "UWP bridge no longer forwards projected file-error results through the current delegate path.");
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
            AssertDoesNotContain(managedDispatch, "DispatchManagedBridgeResultByType(const ResultData& result, ManagedResultDispatchType dispatchType, bool uppercase", "Common managed-bridge dispatch header still exposes the deprecated ResultData-based managed result-dispatch wrapper.");
            AssertContains(managedDispatch, "DispatchManagedResultByType(dispatchType, resultDataNet, uppercase, onFileName, onFileMeta, onFileHash, onFileError);", "Common managed-bridge dispatch header does not yet compose managed result dispatch through the lower-level dispatch seam.");
            AssertContains(bridgeWuiHeader, "#include \"Common/ManagedBridgeDispatch.h\"", "WinUI bridge header does not yet include the common managed-bridge dispatch header.");
            AssertDoesNotContain(bridgeWuiHeader, "#include \"Common/ManagedBridgeHelpers.h\"", "WinUI bridge header still includes the deprecated managed-bridge helper header.");
            AssertContains(bridgeWuiHeader, "void DispatchProjectedResultToDelegate(const HashResult& result, ManagedResultDispatchType dispatchType, bool uppercase = false);", "WinUI bridge does not yet expose the dedicated managed result-dispatch helper.");
            AssertContains(bridgeWui, "void UIBridgeWUI::DispatchProjectedResultToDelegate(const HashResult& result, ManagedResultDispatchType dispatchType, bool uppercase)", "WinUI bridge does not yet implement the dedicated managed result-dispatch helper.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge managed result-dispatch helper does not yet route delegate forwarding through the common managed-bridge helper.");
            AssertContains(bridgeWui, "void UIBridgeWUI::onFileResultEvent(const HashResult& result,", "WinUI bridge does not yet route file-result events through the dedicated managed result-dispatch helper.");
            AssertContains(bridgeWui, "DispatchProjectedResultToDelegate(result,", "WinUI bridge does not yet route file-result events through the dedicated managed result-dispatch helper.");
            AssertContains(bridgeWui, "GetManagedResultDispatchType(eventType)", "WinUI bridge does not yet derive managed dispatch from ProgressEventType.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::showFileName(const HashResult& result)", "WinUI bridge still exposes the old showFileName bridge method.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::showFileHash(const HashResult& result, bool uppercase)", "WinUI bridge still exposes the old showFileHash bridge method.");

            AssertContains(bridgeUwpHeader, "#include \"Common/ManagedBridgeDispatch.h\"", "UWP bridge header does not yet include the common managed-bridge dispatch header.");
            AssertDoesNotContain(bridgeUwpHeader, "#include \"Common/ManagedBridgeHelpers.h\"", "UWP bridge header still includes the deprecated managed-bridge helper header.");
            AssertContains(bridgeUwpHeader, "void DispatchProjectedResultToDelegate(const HashResult& result, ManagedResultDispatchType dispatchType, bool uppercase = false);", "UWP bridge does not yet expose the dedicated managed result-dispatch helper.");
            AssertContains(bridgeUwp, "void UIBridgeUwp::DispatchProjectedResultToDelegate(const HashResult& result, ManagedResultDispatchType dispatchType, bool uppercase)", "UWP bridge does not yet implement the dedicated managed result-dispatch helper.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge managed result-dispatch helper does not yet route delegate forwarding through the common managed-bridge helper.");
            AssertContains(bridgeUwp, "void UIBridgeUwp::onFileResultEvent(const HashResult& result,", "UWP bridge does not yet route file-result events through the dedicated managed result-dispatch helper.");
            AssertContains(bridgeUwp, "DispatchProjectedResultToDelegate(result,", "UWP bridge does not yet route file-result events through the dedicated managed result-dispatch helper.");
            AssertContains(bridgeUwp, "GetManagedResultDispatchType(eventType)", "UWP bridge does not yet derive managed dispatch from ProgressEventType.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::showFileName(const HashResult& result)", "UWP bridge still exposes the old showFileName bridge method.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::showFileHash(const HashResult& result, bool uppercase)", "UWP bridge still exposes the old showFileHash bridge method.");
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
            string resultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataProjection.h");
            string clrMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");

            AssertContains(resultProjection, "template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter, typename TResultVisitor>", "ResultDataProjection does not yet expose the centralized result-list projection visitor template.");
            AssertContains(resultProjection, "VisitProjectedResults(const ResultList& resultList, TStringConverter convertString, TResultVisitor visitor)", "ResultDataProjection does not yet expose the centralized result-list projection helper.");
            AssertContains(resultProjection, "VisitProjectedHashResults<TResultDataNet, TResultStateNet>(resultList, convertString, visitor);", "ResultDataProjection does not yet route full result-list projection through HashResultProjection.");
            AssertContains(resultProjection, "template<typename TResultDataNet, typename TResultStateNet, typename TResultPredicate, typename TStringConverter, typename TResultVisitor>", "ResultDataProjection does not yet expose the centralized matching-result projection visitor template.");
            AssertContains(resultProjection, "VisitProjectedMatchingResults(const ResultList& resultList, TResultPredicate predicate, TStringConverter convertString, TResultVisitor visitor)", "ResultDataProjection does not yet expose the centralized matching-result projection helper.");
            AssertContains(resultProjection, "visitor(matchIndex, ProjectHashResultToNet<TResultDataNet, TResultStateNet>(result, convertString));", "ResultDataProjection does not yet route matching-result projection through the centralized HashResult projection helper.");
            AssertContains(resultProjection, "template<typename TResultDataNet, typename TResultStateNet, typename TResultArray, typename TResultPredicate, typename TResultArrayFactory, typename TStringConverter, typename TResultArraySetter>", "ResultDataProjection does not yet expose the centralized projected-match collection helper template.");
            AssertContains(resultProjection, "CreateProjectedMatchingResults(const ResultList& resultList, TResultPredicate predicate, TResultArrayFactory createResultArray, TStringConverter convertString, TResultArraySetter setProjectedResult)", "ResultDataProjection does not yet expose the centralized projected-match collection helper.");
            AssertContains(resultProjection, "TResultArray projectedResults = createResultArray(CountMatchingResults(resultList, predicate));", "ResultDataProjection projected-match collection helper does not yet size the target collection through the centralized match-count seam.");
            AssertContains(resultProjection, "ResultList::const_iterator itr = resultList.begin();", "ResultDataProjection projected-match collection helper does not yet expose the compile-safe explicit traversal used for materialized projections.");
            AssertContains(resultProjection, "setProjectedResult(projectedResults, matchIndex, ProjectHashResultToNet<TResultDataNet, TResultStateNet>(result, convertString));", "ResultDataProjection projected-match collection helper does not yet route projected writes through the supplied collection setter.");
            AssertContains(resultProjection, "CreateProjectedDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TResultArrayFactory createResultArray, TStringConverter convertString, TResultArraySetter setProjectedResult)", "ResultDataProjection does not yet expose the centralized managed digest-match array projection helper.");
            AssertContains(resultProjection, "return CreateProjectedDigestMatchingHashResults<TResultDataNet, TResultStateNet, TResultArray>(resultList, digestText, createResultArray, convertString, setProjectedResult);", "ResultDataProjection managed digest-match array projection helper does not yet defer to the shared HashResult digest materialization seam.");

            AssertContains(clrMgmt, "CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, cli::array<HashResultNet>^>(", "CLR bridge management layer does not yet allocate projected managed hash results through the centralized digest-match collection seam.");
            AssertContains(clrMgmt, "projectedResults[static_cast<int>(index)] = hashResultNet;", "CLR bridge management layer no longer writes projected hash results into the managed array through the current projection path.");
            AssertDoesNotContain(clrMgmt, "ResultList findResultList;", "CLR bridge management layer still stages matching results in a temporary list instead of using the centralized matching-result projection helper.");
            AssertDoesNotContain(clrMgmt, "ResultDataNet resultDataNet = ConvertResultDataToNet(*itr);", "CLR bridge management layer still performs inline per-item projection instead of using the centralized result-list projection helper.");

            AssertContains(uwpMgmt, "CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, Array<HashResultNet>^>(", "UWP bridge management layer does not yet route matching-result array projection through the centralized managed helper.");
            AssertContains(uwpMgmt, "projectedResults[static_cast<unsigned int>(index)] = hashResultNet;", "UWP bridge management layer no longer writes projected hash results into the managed array through the current path.");
            AssertDoesNotContain(uwpMgmt, "ResultList findResultList;", "UWP bridge management layer still stages matching results in a temporary list instead of using the centralized matching-result projection helper.");
            AssertDoesNotContain(uwpMgmt, "ResultDataNet resultDataNet = UIBridgeUwp::ConvertResultDataToNet(*itr);", "UWP bridge management layer still performs inline per-item projection instead of using the centralized result-list projection helper.");
        }, failures);

        Run("Phase 5 routes result-list matching through a centralized ResultDataAccess helper", () =>
        {
            string resultSearch = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataSearch.h");
            string hashResultSearch = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultSearch.h");
            string clrMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");

            AssertContains(resultSearch, "template<typename TResultPredicate, typename TResultVisitor>", "ResultDataSearch does not yet expose the centralized result-list matching visitor template.");
            AssertContains(resultSearch, "VisitMatchingResults(const ResultList& resultList, TResultPredicate predicate, TResultVisitor visitor)", "ResultDataSearch does not yet expose the centralized result-list matching helper.");
            AssertContains(resultSearch, "CountMatchingResults(const ResultList& resultList, TResultPredicate predicate)", "ResultDataSearch does not yet expose the centralized result-list match-count helper.");
            AssertContains(resultSearch, "CountDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText)", "ResultDataSearch does not yet expose the centralized digest-match count helper.");
            AssertContains(resultSearch, "ResultMatchesDigestText(const ResultData& result, const sunjwbase::tstring& digestText)", "ResultDataSearch does not yet expose the centralized digest-match predicate helper.");
            AssertContains(hashResultSearch, "NormalizeHashResultPathSearchText(const sunjwbase::tstring& pathText)", "HashResultSearch does not yet expose the centralized HashResult path-search normalization helper.");
            AssertContains(resultSearch, "ResultMatchesPathText(const ResultData& result, const sunjwbase::tstring& pathText)", "ResultDataSearch does not yet expose the centralized path-match predicate helper.");
            AssertContains(hashResultSearch, "NormalizeHashResultDigestSearchText(const sunjwbase::tstring& digestText)", "HashResultSearch does not yet expose the centralized HashResult digest-search normalization helper.");
            AssertContains(resultSearch, "if (predicate(*itr))", "ResultDataSearch matching helper does not yet gate result visits through the supplied predicate.");
            AssertContains(resultSearch, "visitor(*itr);", "ResultDataSearch matching helper does not yet forward matched results through the supplied visitor.");
            AssertContains(resultSearch, "++matchCount;", "ResultDataSearch matching helper does not yet return the number of matched results.");
            AssertContains(resultSearch, "VisitMatchingResults(resultList, predicate, [&](const HashResult& result)", "ResultDataSearch match-count helper does not yet reuse the centralized matching visitor seam.");
            AssertContains(resultSearch, "return HashResultMatchesDigestText(ProjectHashResult(result), digestText);", "ResultDataSearch digest-match predicate helper does not yet reuse the centralized HashResult digest seam.");
            AssertContains(hashResultSearch, "return sunjwbase::strtotstr(sunjwbase::str_lower(sunjwbase::tstrtostr(pathText)));", "HashResultSearch path-search normalization helper does not yet lowercase through the centralized seam.");
            AssertContains(resultSearch, "return HashResultMatchesPathText(ProjectHashResult(result), pathText);", "ResultDataSearch path-match predicate helper does not yet reuse the centralized HashResult path-search seam.");
            AssertContains(hashResultSearch, "normalizedDigestText = sunjwbase::strtotstr(sunjwbase::str_upper(sunjwbase::tstrtostr(normalizedDigestText)));", "HashResultSearch digest-search normalization helper does not yet uppercase through the centralized seam.");
            AssertContains(hashResultSearch, "normalizedDigestText = sunjwbase::strtrim(normalizedDigestText);", "HashResultSearch digest-search normalization helper does not yet trim through the centralized seam.");
            AssertContains(resultSearch, "return CountDigestMatchingHashResults(resultList, digestText);", "ResultDataSearch digest-match count helper does not yet defer to the shared HashResult digest-count seam.");

            AssertContains(clrMgmt, "CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, cli::array<HashResultNet>^>(", "CLR bridge management layer does not yet route digest-search and projection through the centralized managed helper.");
            AssertDoesNotContain(clrMgmt, "for (; itr != m_pThreadData->resultList.end(); ++itr)", "CLR bridge management layer still performs manual result-list filtering instead of using the centralized matching helper.");
            AssertDoesNotContain(clrMgmt, "for (; itr != resultList.end(); ++itr)", "CLR bridge management layer still performs manual result-list filtering instead of using the centralized matching helper.");
            AssertDoesNotContain(clrMgmt, "tstrHashToFind = strtotstr(str_upper(tstrtostr(tstrHashToFind)));", "CLR bridge management layer still uppercases digest search text inline instead of using the centralized normalization helper.");
            AssertDoesNotContain(clrMgmt, "tstrHashToFind = strtrim(tstrHashToFind);", "CLR bridge management layer still trims digest search text inline instead of using the centralized normalization helper.");

            AssertContains(uwpMgmt, "CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, Array<HashResultNet>^>(", "UWP bridge management layer does not yet route digest-search and projection through the centralized managed helper.");
            AssertDoesNotContain(uwpMgmt, "for (; itr != m_threadData.resultList.end(); ++itr)", "UWP bridge management layer still performs manual result-list filtering instead of using the centralized matching helper.");
            AssertDoesNotContain(uwpMgmt, "tstrHashToFind = strtotstr(str_upper(tstrtostr(tstrHashToFind)));", "UWP bridge management layer still uppercases digest search text inline instead of using the centralized normalization helper.");
            AssertDoesNotContain(uwpMgmt, "tstrHashToFind = strtrim(tstrHashToFind);", "UWP bridge management layer still trims digest search text inline instead of using the centralized normalization helper.");
        }, failures);

        Run("Phase 5 routes managed digest-match projection through dedicated ResultDataAccess helpers", () =>
        {
            string resultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataProjection.h");
            string clrMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");

            AssertContains(resultProjection, "VisitProjectedDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TStringConverter convertString, TResultVisitor visitor)", "ResultDataProjection does not yet expose the centralized digest-match projection helper.");
            AssertContains(resultProjection, "VisitProjectedDigestMatchingHashResults<TResultDataNet, TResultStateNet>(resultList, digestText, convertString, visitor);", "ResultDataProjection digest-match projection helper does not yet defer to the shared HashResult digest-projection seam.");

            AssertContains(clrMgmt, "CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, cli::array<HashResultNet>^>(", "CLR bridge management layer does not yet route digest-match projection through the current compile-safe managed helper.");
            AssertDoesNotContain(clrMgmt, "VisitProjectedMatchingResults<ResultDataNet, ResultStateNet>(m_pThreadData->resultList, [&](const ResultData& result)", "CLR bridge management layer still keeps the inline digest-match projection lambda instead of using the dedicated helper.");

            AssertContains(uwpMgmt, "CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, Array<HashResultNet>^>(", "UWP bridge management layer does not yet route digest-match projection through the dedicated managed helper.");
            AssertDoesNotContain(uwpMgmt, "VisitProjectedMatchingResults<ResultDataNet, ResultStateNet>(m_threadData.resultList, [&](const ResultData& result)", "UWP bridge management layer still keeps the inline digest-match projection lambda instead of using the dedicated helper.");
        }, failures);

        Run("Phase 5 routes digest-match counting and projection through a dedicated matching visitor seam", () =>
        {
            string resultSearch = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataSearch.h");

            AssertContains(resultSearch, "VisitDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TResultVisitor visitor)", "ResultDataSearch does not yet expose the centralized digest-match visitor helper.");
            AssertContains(resultSearch, "return CountDigestMatchingHashResults(resultList, digestText);", "ResultDataSearch digest-match count helper does not yet route through the shared HashResult digest-count seam.");
        }, failures);

        Run("Phase 5 routes MFC result search iteration through the centralized ResultDataAccess matching helper", () =>
        {
            string resultSearch = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataSearch.h");
            string filesHashSearchController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSearchController.cpp");

            AssertContains(resultSearch, "ResultMatchesPathAndDigestText(const ResultData& result, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText)", "ResultDataSearch does not yet expose the centralized path+digest-match predicate helper.");
            AssertContains(resultSearch, "VisitPathAndDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, TResultVisitor visitor)", "ResultDataSearch does not yet expose the centralized path+digest matching visitor helper.");
            AssertContains(resultSearch, "return HashResultMatchesPathAndDigestText(ProjectHashResult(result), pathText, digestText);", "ResultDataSearch path+digest-match predicate helper does not yet compose through the shared HashResult path+digest seam.");
            AssertContains(resultSearch, "return VisitMatchingResults(resultList, [&](const HashResult& result)", "ResultDataSearch path+digest matching helper does not yet reuse the centralized matching visitor seam.");
            AssertContains(resultSearch, "return HashResultMatchesPathAndDigestText(result, pathText, digestText);", "ResultDataSearch path+digest matching helper does not yet reuse the centralized combined match predicate seam.");

            AssertContains(filesHashSearchController, "VisitThreadDataPathAndDigestMatchingHashResults(*m_threadData, tstrFileToFind, tstrHashToFind, [&](const HashResult& result)", "Legacy MFC search flow does not yet route result iteration through the centralized HashResult path+digest matching helper.");
            AssertContains(filesHashSearchController, "tstring tstrFileToFind = NormalizeHashResultPathSearchText(m_strFindFile.GetString());", "Legacy MFC search flow does not yet normalize path search text through the centralized HashResult search seam.");
            AssertContains(filesHashSearchController, "tstring tstrHashToFind = NormalizeHashResultDigestSearchText(m_strFindHash.GetString());", "Legacy MFC search flow does not yet normalize digest search text through the centralized HashResult search seam.");
            AssertContains(filesHashSearchController, "AppendResult(result);", "Legacy MFC search flow does not yet render matched HashResult values directly.");
            AssertDoesNotContain(filesHashSearchController, "strHash.MakeUpper();", "Legacy MFC search flow still uppercases digest search text inline instead of using the centralized normalization seam.");
            AssertDoesNotContain(filesHashSearchController, "strFile.MakeLower();", "Legacy MFC search flow still lowercases path search text inline instead of using the centralized normalization seam.");
            AssertDoesNotContain(filesHashSearchController, "CString strPathLower = CString(GetResultPath(result).c_str());", "Legacy MFC search flow still lowercases result paths inline instead of using the centralized path-match seam.");
            AssertDoesNotContain(filesHashSearchController, "ResultContainsDigest(result, strHash.GetString())", "Legacy MFC search flow still evaluates digest matches inline instead of using the centralized normalized digest-match seam.");
            AssertDoesNotContain(filesHashSearchController, "return ResultMatchesPathText(result, tstrFileToFind) &&", "Legacy MFC search flow still keeps the inline path+digest predicate instead of using the centralized combined match helper.");
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
            string engineImpl = ReadHashEngineImplementation(repoRoot);

            AssertContains(resultAccess, "ResetResultCoreState(ResultData& result)", "ResultDataAccess does not yet expose the grouped ResultCoreState reset helper introduced in phase 4.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).state = RESULT_NONE;", "ResultDataAccess core-state reset helper does not yet clear the ResultState field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).path.clear();", "ResultDataAccess core-state reset helper does not yet clear the path field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).size = 0;", "ResultDataAccess core-state reset helper does not yet clear the size field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).modifiedDate.clear();", "ResultDataAccess core-state reset helper does not yet clear the modified-date field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).version.clear();", "ResultDataAccess core-state reset helper does not yet clear the version field.");
            AssertContains(resultAccess, "GetMutableResultCoreState(result).error.clear();", "ResultDataAccess core-state reset helper does not yet clear the error field.");

            AssertContains(engineImpl, "result = HashResult();", "HashEngine does not yet reset the grouped HashResult state before publishing a new file result.");
            AssertInOrder(engineImpl,
                [
                    "BeginFileResult(",
                    "HashResult& result = AppendHashExecutionResult(*executionContext);",
                    "result = HashResult();",
                    "result.path = path;",
                    "EmitPathResult(executionContext, result);"
                ],
                "HashEngine file-result begin helper no longer resets grouped core state before publishing the file path.");
        }, failures);

        Run("Phase 4 composes core and digest resets through a single ResultData reset helper", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string engineImpl = ReadHashEngineImplementation(repoRoot);

            AssertContains(resultAccess, "#include \"Common/ResultDigestValueAccess.h\"", "ResultDataAccess does not yet consume the digest value seam when composing the grouped result reset helper.");
            AssertContains(resultAccess, "ResetResultData(ResultData& result)", "ResultDataAccess does not yet expose the grouped ResultData reset helper introduced in phase 4.");
            AssertContains(resultAccess, "ResetResultCoreState(result);", "ResultDataAccess grouped ResultData reset helper does not yet reset grouped core state.");
            AssertContains(resultAccess, "ResetResultDigests(result);", "ResultDataAccess grouped ResultData reset helper does not yet reset digest state through the existing digest seam.");

            AssertContains(engineImpl, "result = HashResult();", "HashEngine does not yet route new HashResult initialization through the grouped reset helper.");
            AssertInOrder(engineImpl,
                [
                    "BeginFileResult(",
                    "HashResult& result = AppendHashExecutionResult(*executionContext);",
                    "result = HashResult();",
                    "result.path = path;",
                    "EmitPathResult(executionContext, result);"
                ],
                "HashEngine file-result begin helper no longer routes grouped result reset through the dedicated helper before publishing the file path.");
        }, failures);

        Run("Phase 4 routes managed ResultStateNet conversion through dedicated bridge helpers", () =>
        {
            string resultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataProjection.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            string resultNetProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultNetProjection.h");

            AssertContains(resultNetProjection, "template<typename TResultStateNet>", "ResultNetProjection does not yet expose the centralized ResultStateNet conversion template introduced after phase 4.");
            AssertContains(resultNetProjection, "static inline TResultStateNet ConvertResultStateToNet(ResultState resultState)", "ResultNetProjection does not yet expose the centralized ResultStateNet conversion helper introduced after phase 4.");
            AssertContains(resultProjection, "AssignHashResultCoreToNet<TResultDataNet, TResultStateNet>(resultDataNet, ProjectHashResult(result), convertString);", "ResultDataProjection does not yet route ResultStateNet assignment through HashResultProjection.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route HashResultStateNet assignment through the centralized HashResultNet projection helper.");
            AssertDoesNotContain(bridgeWui, "switch (GetResultState(result))", "WinUI bridge still inlines ResultStateNet conversion instead of using the dedicated helper.");
            AssertDoesNotContain(bridgeWui, "static ResultStateNet ConvertResultStateToNet(ResultState resultState)", "WinUI bridge still keeps a local ResultStateNet conversion helper instead of using the centralized ResultDataAccess helper.");

            AssertContains(bridgeUwp, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route HashResultStateNet assignment through the centralized HashResultNet projection helper.");
            AssertDoesNotContain(bridgeUwp, "switch (GetResultState(result))", "UWP bridge still inlines ResultStateNet conversion instead of using the dedicated helper.");
            AssertDoesNotContain(bridgeUwp, "static ResultStateNet ConvertResultStateToNet(ResultState resultState)", "UWP bridge still keeps a local ResultStateNet conversion helper instead of using the centralized ResultDataAccess helper.");
        }, failures);

        Run("Phase 5 routes ResultState and digest-type projection through dedicated ResultDataAccess dispatch helpers", () =>
        {
            string resultRender = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataRender.h");
            string resultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataProjection.h");
            string resultNetProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultNetProjection.h");

            AssertContains(resultRender, "DispatchResultStateByType(ResultState resultState, TNoneAction onNone, TPathAction onPath, TMetaAction onMeta, TAllAction onAll, TErrorAction onError)", "ResultDataRender does not yet expose the grouped ResultState dispatch helper.");
            AssertContains(resultRender, "DispatchResultStateByType(resultState,", "ResultDataRender does not yet route ResultState render-policy through the grouped dispatch helper.");
            AssertContains(resultNetProjection, "DispatchResultDigestValueByType(ResultDigestType digestType, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)", "ResultNetProjection does not yet expose the grouped digest-type dispatch helper.");
            AssertContains(resultNetProjection, "switch (resultState)", "ResultNetProjection ResultStateNet conversion helper does not yet use the compile-safe explicit ResultState switch.");
            AssertContains(resultNetProjection, "return TResultStateNet::ResultPath;", "ResultNetProjection ResultStateNet conversion helper does not yet map RESULT_PATH through the compile-safe explicit switch.");
            AssertContains(resultNetProjection, "switch (digestType)", "ResultNetProjection digest assignment helper does not yet use the compile-safe explicit digest-type switch.");
            AssertContains(resultNetProjection, "resultDataNet.MD5 = digestValue;", "ResultNetProjection digest assignment helper does not yet map MD5 through the compile-safe explicit switch.");

            AssertDoesNotContain(resultRender, "static inline ResultRenderPolicy GetResultRenderPolicy(ResultState resultState)\r\n{\r\n\tswitch (resultState)", "ResultDataRender render-policy helper still performs an inline ResultState switch instead of using the grouped dispatch helper.");
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
            string hashAlgorithmRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashAlgorithmRegistry.h");

            AssertDoesNotContain(global, "struct ResultDigestCompatibilityFields", "Core digest compatibility fields should no longer live in Global.h after the algorithm-domain cleanup.");
            AssertDoesNotContain(global, "sunjwbase::tstring md5;", "Core digest compatibility fields should no longer expose the fixed MD5 slot.");
            AssertDoesNotContain(global, "sunjwbase::tstring sha1;", "Core digest compatibility fields should no longer expose the fixed SHA1 slot.");
            AssertDoesNotContain(global, "sunjwbase::tstring sha256;", "Core digest compatibility fields should no longer expose the fixed SHA256 slot.");
            AssertDoesNotContain(global, "sunjwbase::tstring sha512;", "Core digest compatibility fields should no longer expose the fixed SHA512 slot.");

            AssertDoesNotContain(hashAlgorithmRegistry, "&ResultDigestCompatibilityFields::md5", "HashAlgorithmRegistry should no longer route MD5 through a compatibility-field pointer.");
            AssertDoesNotContain(hashAlgorithmRegistry, "&ResultDigestCompatibilityFields::sha1", "HashAlgorithmRegistry should no longer route SHA1 through a compatibility-field pointer.");
            AssertDoesNotContain(hashAlgorithmRegistry, "&ResultDigestCompatibilityFields::sha256", "HashAlgorithmRegistry should no longer route SHA256 through a compatibility-field pointer.");
            AssertDoesNotContain(hashAlgorithmRegistry, "&ResultDigestCompatibilityFields::sha512", "HashAlgorithmRegistry should no longer route SHA512 through a compatibility-field pointer.");
        }, failures);

        Run("Phase 5 neutralizes the internal ResultDigestStorage field name behind the existing ResultDigestAccess seam", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(global, "std::vector<sunjwbase::tstring> values;", "ResultDigestStorage does not yet expose the neutral registry-sized storage vector.");
            AssertDoesNotContain(global, "tstrDigests", "ResultDigestStorage still uses the legacy tstrDigests field name internally.");

            AssertContains(digestAccess, "return digestStorage.values[digestIndex];", "ResultDigestAccess digest-storage seam does not yet route through the neutral internal storage vector.");
            AssertDoesNotContain(digestAccess, "digestStorage.tstrDigests", "ResultDigestAccess digest-storage seam still routes through the legacy internal storage field name.");
        }, failures);

        Run("Phase 5 neutralizes the internal ResultDigestState field names behind the existing ResultDigestAccess seam", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(global, "ResultDigestStorage storage;", "ResultDigestState does not yet expose the neutral internal storage field name.");
            AssertDoesNotContain(global, "compatibilityFields", "ResultDigestState still exposes legacy compatibility fields in the core model.");
            AssertDoesNotContain(global, "ResultDigestStorage digestStorage;", "ResultDigestState still uses the legacy digestStorage field name internally.");
            AssertDoesNotContain(global, "ResultLegacyDigestFields legacyFields;", "ResultDigestState still uses the legacy digest compatibility struct name internally.");

            AssertContains(digestAccess, "return GetResultDigestState(result).storage;", "ResultDigestAccess const digest-storage helper does not yet route through the neutral ResultDigestState storage field name.");
            AssertContains(digestAccess, "return GetMutableResultDigestState(result).storage;", "ResultDigestAccess mutable digest-storage helper does not yet route through the neutral ResultDigestState storage field name.");
            AssertDoesNotContain(digestAccess, "compatibilityFields", "ResultDigestAccess still routes through legacy compatibility fields.");
            AssertDoesNotContain(digestAccess, "return GetResultDigestState(result).digestStorage;", "ResultDigestAccess still routes through the legacy ResultDigestState digestStorage field name.");
            AssertDoesNotContain(digestAccess, "return GetMutableResultDigestState(result).digestStorage;", "ResultDigestAccess mutable helpers still route through the legacy ResultDigestState digestStorage field name.");
            AssertDoesNotContain(digestAccess, "return GetResultDigestState(result).legacyDigests;", "ResultDigestAccess still routes through the legacy ResultDigestState legacyDigests field name.");
            AssertDoesNotContain(digestAccess, "return GetMutableResultDigestState(result).legacyDigests;", "ResultDigestAccess mutable helpers still route through the legacy ResultDigestState legacyDigests field name.");
        }, failures);

        Run("Phase 5 neutralizes the internal ResultData grouped state field names behind the existing access seams", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

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
            string hashAlgorithmRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashAlgorithmRegistry.h");
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string digestRender = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestRender.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(hashAlgorithmRegistry, "ResultDigestType type;", "HashAlgorithmRegistry does not yet expose the neutral digest type field name.");
            AssertDoesNotContain(hashAlgorithmRegistry, "ResultDigestType digestType;", "HashAlgorithmRegistry still uses the legacy digestType field name internally.");
            AssertDoesNotContain(hashAlgorithmRegistry, "compatibilityValueField", "HashAlgorithmRegistry still carries compatibility-field pointers in the core descriptor.");

            AssertContains(digestAccess, "return visitor(GetResultDigestMetadataType(digestMetadata));", "ResultDigestAccess digest visitor does not yet route through the neutral ResultDigestMetadata type accessor.");
            AssertContains(digestAccess, "return visitor(index, digestMetadata, GetResultDigest(result, GetResultDigestMetadataType(digestMetadata)));", "ResultDigestAccess digest metadata-value visitor does not yet route through the neutral ResultDigestMetadata type accessor.");
            AssertContains(digestAccess, "return GetHashAlgorithmTypeAt(index);", "ResultDigestAccess digest-order helper does not yet route through the registry type accessor.");
            AssertContains(digestAccess, "return GetHashAlgorithmIndex(digestType);", "ResultDigestAccess digest-index helper does not yet route through the registry index accessor.");
            AssertDoesNotContain(digestAccess, "GetResultDigestMetadataCompatibilityValueField", "ResultDigestAccess still exposes compatibility-field metadata lookup after the algorithm-domain cleanup.");

            AssertContains(digestRender, "GetResultDigestLabel(digestMetadata)", "ResultDigestRender does not yet route digest labels through the neutral ResultDigestMetadata seam.");
        }, failures);

        Run("Phase 5 routes projected matching traversal through the existing matching-results seam", () =>
        {
            string resultSearch = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataSearch.h");
            string resultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataProjection.h");

            AssertContains(resultSearch, "VisitMatchingResults(resultList, predicate, [&](const HashResult& result)", "ResultDataSearch does not yet route projected matching traversal through the existing matching-results seam.");
            AssertContains(resultProjection, "visitor(matchIndex, ProjectHashResultToNet<TResultDataNet, TResultStateNet>(result, convertString));", "ResultDataProjection projected matching traversal does not yet reuse the shared matching seam before projecting each result.");
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
            string resultRender = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataRender.h");
            string bridgeMfcHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(resultRender, "struct ResultSizeDisplayInfo", "ResultDataRender does not yet expose the grouped file-size display structure.");
            AssertContains(resultRender, "GetResultSizeDisplayInfo(const ResultData& result)", "ResultDataRender does not yet expose the centralized file-size display helper.");
            AssertContains(resultRender, "sprintf_s(chSizeBuff, 1024, \"%I64u\", GetResultSize(result));", "ResultDataRender file-size display helper does not yet centralize the byte-size formatting.");
            AssertContains(resultRender, "resultSizeDisplayInfo.shortSizeText = sunjwbase::strtotstr(Utils::ConvertSizeToShortSizeStr(GetResultSize(result)));", "ResultDataRender file-size display helper does not yet centralize the short-size formatting.");
            AssertContains(resultRender, "enum ResultMetaLineType", "ResultDataRender does not yet expose the result-metadata line type enum.");
            AssertContains(resultRender, "DispatchResultMetaLineByType(ResultMetaLineType metaLine, TFileSizeAction onFileSize, TModifiedDateAction onModifiedDate, TVersionAction onVersion)", "ResultDataRender does not yet expose the centralized metadata-line dispatch helper.");
            AssertContains(resultRender, "VisitRenderableResultMetaLines(const ResultData& result, TResultMetaLineVisitor visitor)", "ResultDataRender does not yet expose the renderable metadata-line visitor helper.");
            AssertContains(resultRender, "if (!visitor(RESULT_META_LINE_FILE_SIZE))", "ResultDataRender metadata-line visitor does not yet render file size through the shared seam.");
            AssertContains(resultRender, "if (!visitor(RESULT_META_LINE_MODIFIED_DATE))", "ResultDataRender metadata-line visitor does not yet render modified date through the shared seam.");
            AssertContains(resultRender, "if (HasResultVersion(result) &&", "ResultDataRender metadata-line visitor does not yet gate version rendering through the shared seam.");

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
            string digestRender = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestRender.h");

            AssertContains(bridgeMfc, "AppendFileHashToHyperEdit", "Legacy MFC digest renderer is missing.");
            AssertContains(digestRender, "FormatResultDigestForDisplay(const sunjwbase::tstring& digestValue, bool uppercase)", "ResultDigestRender does not yet expose the centralized digest-display formatting helper.");
            AssertContains(digestRender, "VisitResultDigestDisplayValues(const ResultData& result, bool uppercase, TResultDigestDisplayVisitor visitor)", "ResultDigestRender does not yet expose the centralized formatted digest-display visitor helper.");
            AssertContains(digestRender, "GetResultDigestDisplayInfo(const ResultDigestMetadata& digestMetadata, const sunjwbase::tstring& digestValue, bool uppercase)", "ResultDigestRender does not yet expose the grouped digest display-info helper.");
            AssertContains(digestRender, "return sunjwbase::strtotstr(sunjwbase::str_upper(sunjwbase::tstrtostr(digestValue)));", "ResultDigestRender digest-display formatting helper does not yet uppercase digest values through the centralized seam.");
            AssertContains(digestRender, "return sunjwbase::strtotstr(sunjwbase::str_lower(sunjwbase::tstrtostr(digestValue)));", "ResultDigestRender digest-display formatting helper does not yet lowercase digest values through the centralized seam.");
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

            AssertContains(nativeProject, "..\\..\\trunk\\source\\WinCommon\\AdvTaskbar.cpp", "WUINative no longer keeps its platform-specific AdvTaskbar layer in the baseline layout.");
            AssertContains(nativeProject, "..\\..\\trunk\\source\\WinCommon\\ClipboardHelper.cpp", "WUINative no longer keeps its platform-specific ClipboardHelper layer in the baseline layout.");
            AssertContains(nativeProject, "..\\..\\trunk\\source\\WinCommon\\FileVersionHelper.cpp", "WUINative no longer keeps its platform-specific FileVersionHelper layer in the baseline layout.");
        }, failures);

        Run("CLR bridge still depends on the native library through linker configuration in the baseline", () =>
        {
            string clrBridge = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\fHashClrBridge.vcxproj");

            AssertContains(clrBridge, "fHashWUINative.lib;fHashNativeCore.lib;Version.lib;%(AdditionalDependencies)", "CLR bridge no longer links the WinUI platform layer and native core through AdditionalDependencies in the baseline layout.");
            AssertContains(clrBridge, @"$(ProjectDir)..\fHashWUINative\$(Platform)\$(Configuration)\fHashWUINative\", "CLR bridge no longer resolves the WinUI platform layer through the current output-path coupling.");
            AssertContains(clrBridge, @"$(ProjectDir)..\fHashNativeCore\$(Platform)\$(Configuration)\fHashNativeCore\", "CLR bridge no longer resolves the native core through the current output-path coupling.");
        }, failures);

        Run("Phase 7 routes platform bridges through the shared adapter bridge seam and removes the empty UIBridgeBase compatibility shim", () =>
        {
            string bridgeBase = ReadRepoFile(repoRoot, @"trunk\source\Adapters\UiBridge\HashEngineBridge.h");
            string bridgeMfcHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.h");
            string bridgeWuiHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.h");
            string bridgeUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.h");
            string bridgeMacHeader = ReadRepoFile(repoRoot, @"trunk\source\OSXUI\UIBridgeMacSwift.h");
            string compatibilityBridgePath = Path.Combine(repoRoot, @"trunk\source\Common\UIBridgeBase.h");

            AssertContains(bridgeBase, "#include \"Adapters/UiBridge/HashEngineObserver.h\"", "HashEngineBridge does not yet layer directly on top of the adapter observer seam.");
            AssertContains(bridgeBase, "class HashEngineBridge: public HashEngineObserver", "HashEngineBridge is missing.");
            if (File.Exists(compatibilityBridgePath))
            {
                failures.Add("UIBridgeBase still exists even though the shared adapter bridge seam should have replaced it.");
            }

            AssertContains(bridgeMfcHeader, "#include \"Adapters/UiBridge/HashEngineBridge.h\"", "MFC bridge header does not yet include the shared adapter HashEngineBridge seam.");
            AssertContains(bridgeMfcHeader, "class UIBridgeMFC: public HashEngineBridge", "MFC bridge does not yet inherit HashEngineBridge directly.");
            AssertDoesNotContain(bridgeMfcHeader, "#include \"Common/UIBridgeBase.h\"", "MFC bridge header still depends directly on the compatibility shim.");
            AssertDoesNotContain(bridgeMfcHeader, "class UIBridgeMFC: public UIBridgeBase", "MFC bridge still inherits the compatibility shim instead of HashEngineBridge.");

            AssertContains(bridgeWuiHeader, "#include \"Adapters/UiBridge/HashEngineBridge.h\"", "WinUI bridge header does not yet include the shared adapter HashEngineBridge seam.");
            AssertContains(bridgeWuiHeader, "class UIBridgeWUI : public HashEngineBridge", "WinUI bridge does not yet inherit HashEngineBridge directly.");
            AssertDoesNotContain(bridgeWuiHeader, "#include \"Common/UIBridgeBase.h\"", "WinUI bridge header still depends directly on the compatibility shim.");
            AssertDoesNotContain(bridgeWuiHeader, "class UIBridgeWUI : public UIBridgeBase", "WinUI bridge still inherits the compatibility shim instead of HashEngineBridge.");

            AssertContains(bridgeUwpHeader, "#include \"Adapters/UiBridge/HashEngineBridge.h\"", "UWP bridge header does not yet include the shared adapter HashEngineBridge seam.");
            AssertContains(bridgeUwpHeader, "class UIBridgeUwp : public HashEngineBridge", "UWP bridge does not yet inherit HashEngineBridge directly.");
            AssertDoesNotContain(bridgeUwpHeader, "#include \"Common/UIBridgeBase.h\"", "UWP bridge header still depends directly on the compatibility shim.");
            AssertDoesNotContain(bridgeUwpHeader, "class UIBridgeUwp : public UIBridgeBase", "UWP bridge still inherits the compatibility shim instead of HashEngineBridge.");

            AssertContains(bridgeMacHeader, "#include \"Adapters/UiBridge/HashEngineBridge.h\"", "macOS bridge header does not yet include the shared adapter HashEngineBridge seam.");
            AssertContains(bridgeMacHeader, "class UIBridgeMacSwift: public HashEngineBridge", "macOS bridge does not yet inherit HashEngineBridge directly.");
            AssertDoesNotContain(bridgeMacHeader, "#include \"Common/UIBridgeBase.h\"", "macOS bridge header still depends directly on the compatibility shim.");
            AssertDoesNotContain(bridgeMacHeader, "class UIBridgeMacSwift: public UIBridgeBase", "macOS bridge still inherits the compatibility shim instead of HashEngineBridge.");
        }, failures);

        Run("Phase 8 introduces thread-scoped hash algorithm selection while keeping the current four algorithms enabled by default", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string threadAccess = string.Join(
                "\r\n",
                ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataAccess.h"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataExecutionAccess.h"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataInputAccess.h"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataResultAccess.h"));
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string digestRender = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestRender.h");
            string engineImpl = ReadHashEngineImplementation(repoRoot);

            AssertContains(global, "struct HashAlgorithmSelectionState", "Global.h does not yet expose the grouped hash-algorithm selection state introduced in phase 8.");
            AssertContains(global, "std::vector<bool> enabled;", "Hash-algorithm selection state does not yet store the current enabled flags through a registry-sized vector.");
            AssertContains(global, "HashAlgorithmSelectionState hashAlgorithms;", "ThreadData execution state does not yet carry the hash-algorithm selection state.");

            AssertContains(threadAccess, "#include \"Common/HashAlgorithmRegistry.h\"", "ThreadData access seams do not yet include the hash-algorithm registry seam needed for algorithm selection.");
            AssertContains(threadAccess, "GetThreadDataHashAlgorithmSelectionState(const ThreadData& threadData)", "ThreadDataAccess does not yet expose the const hash-algorithm selection helper.");
            AssertContains(threadAccess, "GetMutableThreadDataHashAlgorithmSelectionState(ThreadData& threadData)", "ThreadDataAccess does not yet expose the mutable hash-algorithm selection helper.");
            AssertContains(threadAccess, "SetThreadDataHashAlgorithmEnabled(ThreadData& threadData, ResultDigestType digestType, bool enabled)", "ThreadDataAccess does not yet expose the hash-algorithm enable/disable helper.");
            AssertContains(threadAccess, "IsThreadDataHashAlgorithmEnabled(const ThreadData& threadData, ResultDigestType digestType)", "ThreadDataAccess does not yet expose the hash-algorithm enabled-state helper.");
            AssertContains(threadAccess, "VisitEnabledThreadDataHashAlgorithms(const ThreadData& threadData, THashAlgorithmVisitor visitor)", "ThreadDataAccess does not yet expose the enabled-hash-algorithm visitor seam.");
            AssertContains(threadAccess, "VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)", "ThreadDataAccess does not yet route algorithm iteration through the registry seam.");
            AssertContains(threadAccess, "ResetThreadDataHashAlgorithms(ThreadData& threadData)", "ThreadDataAccess does not yet expose the default hash-algorithm reset helper.");
            AssertContains(threadAccess, "ResetThreadDataHashAlgorithms(threadData);", "New ThreadData sessions do not yet reset hash algorithms to the default enabled set.");
            AssertContains(threadAccess, "hashAlgorithmSelectionState.enabled.assign(registeredAlgorithmCount, true);", "ThreadDataAccess does not yet default a new algorithm-selection vector to enabled.");

            AssertContains(engineImpl, "InitializeFileHashing(const HashRequest& request, HashExecutionContext *executionContext, FileHashContexts *hashContexts)", "HashEngine does not yet thread the algorithm-selection state into file-hashing initialization.");
            AssertContains(engineImpl, "VisitHashRequestAlgorithms(request, [&](ResultDigestType digestType)", "HashEngine does not yet route digest initialization/finalization/publication through the HashRequest algorithm seam.");
            AssertContains(engineImpl, "FinalizeDigestStrings(request, executionState.hashContexts, executionState.digestBundle);", "HashEngine does not yet finalize digests through the request-scoped algorithm-selection seam.");
            AssertContains(engineImpl, "PopulateDigestResult(request, result, executionState.digestBundle);", "HashEngine does not yet publish digests through the request-scoped algorithm-selection seam.");
            AssertContains(engineImpl, "HasHashRequestAlgorithm(request, RESULT_DIGEST_MD5)", "HashEngine does not yet gate MD5 updates through the algorithm-selection seam.");
            AssertContains(engineImpl, "HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA1)", "HashEngine does not yet gate SHA1 updates through the algorithm-selection seam.");
            AssertContains(engineImpl, "HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA256)", "HashEngine does not yet gate SHA256 updates through the algorithm-selection seam.");
            AssertContains(engineImpl, "HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA512)", "HashEngine does not yet gate SHA512 updates through the algorithm-selection seam.");
            AssertContains(engineImpl, "if (!result.digests.empty())", "HashEngine does not yet suppress hash-result publication when no algorithms are enabled.");

            AssertContains(digestAccess, "HasResultDigest(const ResultData& result, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the result-digest presence helper needed for selective rendering.");
            AssertContains(digestAccess, "HasAnyResultDigests(const ResultData& result)", "ResultDigestAccess does not yet expose the grouped result-digest presence helper.");
            AssertContains(digestRender, "if (!HasResultDigest(result, GetResultDigestMetadataType(digestMetadata)))", "ResultDigestRender formatted display visitor does not yet skip disabled or absent digest values.");
        }, failures);

        Run("Phase 8 exposes algorithm selection through managed and UI entry points", () =>
        {
            string hashMgmtClrHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.h");
            string hashMgmtClr = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string hashMgmtUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.h");
            string hashMgmtUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");
            string winUiXaml = ReadRepoFile(repoRoot, @"trunk\source\WinUI\MainPage.xaml");
            string winUiPage = ReadRepoFile(repoRoot, @"trunk\source\WinUI\MainPage.xaml.cs");
            string winUiRes = ReadRepoFile(repoRoot, @"trunk\source\WinUI\Strings\en-US\Resources.resw");
            string uwpXaml = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\MainPage.xaml");
            string uwpPage = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\MainPage.xaml.cs");
            string uwpRes = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\Strings\en-US\Resources.resw");
            string mfcHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.h");
            string mfcDialog = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcInitializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string mfcSessionController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSessionController.cpp");
            string mfcRc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\fileshash.rc");
            string mfcStringsBase = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIStringsBase.cpp");
            string mfcStringsZh = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIStringsZHCN.cpp");
            string mfcDialogAndInitialization = mfcDialog + Environment.NewLine + mfcInitializationController;

            AssertContains(hashMgmtClrHeader, "public enum class HashAlgorithmTypeNet", "CLR bridge does not yet expose the managed hash-algorithm enum.");
            AssertContains(hashMgmtClrHeader, "void ResetHashAlgorithms();", "CLR bridge does not yet expose the managed algorithm reset helper.");
            AssertContains(hashMgmtClrHeader, "void SetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm, bool val);", "CLR bridge does not yet expose the managed algorithm enable helper.");
            AssertContains(hashMgmtClrHeader, "bool GetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm);", "CLR bridge does not yet expose the managed algorithm query helper.");
            AssertContains(hashMgmtClr, "ResetThreadDataHashAlgorithms(*m_pThreadData);", "CLR bridge does not yet reset native algorithm selections from the managed seam.");
            AssertContains(hashMgmtClr, "::SetManagedHashAlgorithmEnabledByDigestType(*m_pThreadData, digestTypeValue, val);", "CLR bridge does not yet forward algorithm enablement through the shared managed helper.");
            AssertContains(hashMgmtClr, "if (!HasEnabledThreadDataHashAlgorithms(*m_pThreadData))", "CLR bridge does not yet reject zero-algorithm hash starts.");

            AssertContains(hashMgmtUwpHeader, "public enum class HashAlgorithmTypeNet", "UWP bridge does not yet expose the managed hash-algorithm enum.");
            AssertContains(hashMgmtUwpHeader, "void ResetHashAlgorithms();", "UWP bridge does not yet expose the managed algorithm reset helper.");
            AssertContains(hashMgmtUwpHeader, "void SetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm, Platform::Boolean val);", "UWP bridge does not yet expose the managed algorithm enable helper.");
            AssertContains(hashMgmtUwpHeader, "Platform::Boolean GetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm);", "UWP bridge does not yet expose the managed algorithm query helper.");
            AssertContains(hashMgmtUwp, "ResetThreadDataHashAlgorithms(m_threadData);", "UWP bridge does not yet reset native algorithm selections from the managed seam.");
            AssertContains(hashMgmtUwp, "::SetManagedHashAlgorithmEnabledByDigestType(m_threadData, digestTypeValue, val);", "UWP bridge does not yet forward algorithm enablement through the shared managed helper.");
            AssertContains(hashMgmtUwp, "if (!HasEnabledThreadDataHashAlgorithms(m_threadData))", "UWP bridge does not yet reject zero-algorithm hash starts.");

            AssertContains(winUiXaml, "StackPanelHashAlgorithms", "WinUI page does not yet expose the dynamic hash-algorithm container.");
            AssertContains(winUiPage, "KeyHashAlgorithmPrefix", "WinUI page does not yet persist dynamic hash-algorithm toggle state.");
            AssertContains(winUiPage, "UpdateHashAlgorithmStat(bool saveLocalSetting = true)", "WinUI page does not yet synchronize algorithm selections into HashMgmt.");
            AssertContains(winUiPage, "ValidateHashAlgorithmSelectionAsync()", "WinUI page does not yet validate algorithm selection before start.");
            AssertContains(winUiPage, "if (!await ValidateHashAlgorithmSelectionAsync())", "WinUI page does not yet block zero-algorithm starts.");
            AssertContains(winUiPage, "m_mainWindow.HashMgmt.ResetHashAlgorithms();", "WinUI page does not yet reset managed selections before reapplying the current checkbox state.");
            AssertContains(winUiPage, "LoadHashAlgorithmControls()", "WinUI page does not yet materialize dynamic hash-algorithm controls.");
            AssertContains(winUiPage, "m_mainWindow.HashMgmt.GetSupportedHashAlgorithms();", "WinUI page does not yet fetch supported algorithms from the managed seam.");
            AssertContains(winUiPage, "SetHashAlgorithmEnabledByDigestType(hashAlgorithm.DigestType, hashAlgorithmEnabled);", "WinUI page does not yet forward dynamic hash-algorithm selections.");
            AssertContains(winUiPage, "WinUIHelper.LoadLocalSettings(GetHashAlgorithmSettingKey(hashAlgorithm)) ?? true", "WinUI page does not yet default dynamic hash-algorithm entries to enabled on first load.");
            AssertContains(winUiPage, "AppendDigestHashToTextMain(List<Inline> inlines, string digestLabel, string digestValue)", "WinUI page does not yet centralize selective digest display rendering.");
            AssertContains(winUiPage, "if (string.IsNullOrEmpty(digestValue))", "WinUI page does not yet skip empty digest values.");
            AssertContains(winUiRes, "HashAlgorithmDialogTitle", "WinUI resources do not yet include the algorithm-selection dialog title.");
            AssertContains(winUiRes, "HashAlgorithmDialogMessage", "WinUI resources do not yet include the algorithm-selection dialog message.");

            AssertContains(uwpXaml, "StackPanelHashAlgorithms", "UWP page does not yet expose the dynamic hash-algorithm container.");
            AssertContains(uwpPage, "KeyHashAlgorithmPrefix", "UWP page does not yet persist dynamic hash-algorithm toggle state.");
            AssertContains(uwpPage, "UpdateHashAlgorithmStat(bool saveLocalSetting = true)", "UWP page does not yet synchronize algorithm selections into HashMgmt.");
            AssertContains(uwpPage, "ValidateHashAlgorithmSelectionAsync()", "UWP page does not yet validate algorithm selection before start.");
            AssertContains(uwpPage, "if (!await ValidateHashAlgorithmSelectionAsync())", "UWP page does not yet block zero-algorithm starts.");
            AssertContains(uwpPage, "m_hashMgmt.ResetHashAlgorithms();", "UWP page does not yet reset managed selections before reapplying the current checkbox state.");
            AssertContains(uwpPage, "LoadHashAlgorithmControls()", "UWP page does not yet materialize dynamic hash-algorithm controls.");
            AssertContains(uwpPage, "m_hashMgmt.GetSupportedHashAlgorithms();", "UWP page does not yet fetch supported algorithms from the managed seam.");
            AssertContains(uwpPage, "SetHashAlgorithmEnabledByDigestType(hashAlgorithm.DigestType, hashAlgorithmEnabled);", "UWP page does not yet forward dynamic hash-algorithm selections.");
            AssertContains(uwpPage, "UwpHelper.LoadLocalSettings(GetHashAlgorithmSettingKey(hashAlgorithm)) ?? true", "UWP page does not yet default dynamic hash-algorithm entries to enabled on first load.");
            AssertContains(uwpPage, "AppendDigestHashToTextMain(List<Inline> inlines, string digestLabel, string digestValue)", "UWP page does not yet centralize selective digest display rendering.");
            AssertContains(uwpPage, "if (string.IsNullOrEmpty(digestValue))", "UWP page does not yet skip empty digest values.");
            AssertContains(uwpRes, "HashAlgorithmDialogTitle", "UWP resources do not yet include the algorithm-selection dialog title.");
            AssertContains(uwpRes, "HashAlgorithmDialogMessage", "UWP resources do not yet include the algorithm-selection dialog message.");

            AssertContains(mfcHeader, "FilesHashAlgorithmSelectionController m_hashAlgorithmSelectionController;", "MFC dialog does not yet keep the desktop hash-algorithm controller seam.");
            AssertContains(mfcDialogAndInitialization, "hashAlgorithmSelectionController->ResetChecks();", "MFC dialog does not yet default the algorithm checkboxes through the dedicated controller seam.");
            AssertContains(mfcDialogAndInitialization, "hashAlgorithmSelectionController->SyncSelections();", "MFC dialog does not yet synchronize algorithm selections through the dedicated controller seam.");
            AssertContains(mfcSessionController, "m_hashAlgorithmSelectionController->ValidateSelection(noSelectionMessage);", "MFC session controller does not yet block zero-algorithm starts through the dedicated controller seam.");
            AssertContains(mfcRc, "IDC_CHECK_MD5", "MFC resources do not yet include the MD5 checkbox.");
            AssertContains(mfcRc, "IDC_CHECK_SHA1", "MFC resources do not yet include the SHA1 checkbox.");
            AssertContains(mfcRc, "IDC_CHECK_SHA256", "MFC resources do not yet include the SHA256 checkbox.");
            AssertContains(mfcRc, "IDC_CHECK_SHA512", "MFC resources do not yet include the SHA512 checkbox.");
            AssertContains(mfcStringsBase, "MAINDLG_SELECT_HASH_ALGORITHM", "MFC English strings do not yet include the algorithm-selection warning.");
            AssertContains(mfcStringsZh, "MAINDLG_SELECT_HASH_ALGORITHM", "MFC Chinese strings do not yet include the algorithm-selection warning.");
        }, failures);

        Run("Phase 9 splits mixed result access responsibilities into dedicated search, projection, and render seams", () =>
        {
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string resultSearch = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataSearch.h");
            string resultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataProjection.h");
            string resultRender = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataRender.h");
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string digestRender = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestRender.h");
            string managedBridgeDispatch = ReadRepoFile(repoRoot, @"trunk\source\Common\ManagedBridgeDispatch.h");
            string bridgeMfcHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");
            string filesHashDlg = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string hashMgmtClr = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string hashMgmtUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");

            AssertDoesNotContain(resultAccess, "VisitMatchingResults(const ResultList& resultList, TResultPredicate predicate, TResultVisitor visitor)", "ResultDataAccess still owns result-list search traversal after the phase 9 seam split.");
            AssertDoesNotContain(resultAccess, "ProjectResultDataToNet(const ResultData& result, TStringConverter convertString)", "ResultDataAccess still owns managed projection after the phase 9 seam split.");
            AssertDoesNotContain(resultAccess, "struct ResultRenderPolicy", "ResultDataAccess still owns render policy state after the phase 9 seam split.");

            AssertContains(resultSearch, "#include \"Common/HashResultSearch.h\"", "ResultDataSearch does not yet layer on top of the shared HashResult search seam.");
            AssertContains(resultSearch, "VisitMatchingResults(const ResultList& resultList, TResultPredicate predicate, TResultVisitor visitor)", "ResultDataSearch does not yet own result-list traversal.");
            AssertContains(resultSearch, "VisitDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TResultVisitor visitor)", "ResultDataSearch does not yet own digest-match traversal.");

            AssertContains(resultProjection, "#include \"Common/ResultDataSearch.h\"", "ResultDataProjection does not yet layer on top of ResultDataSearch.");
            AssertContains(resultProjection, "ProjectResultDataToNet(const ResultData& result, TStringConverter convertString)", "ResultDataProjection does not yet own managed result projection.");
            AssertContains(resultProjection, "CreateProjectedDigestMatchingResults(const ResultList& resultList, const sunjwbase::tstring& digestText, TResultArrayFactory createResultArray, TStringConverter convertString, TResultArraySetter setProjectedResult)", "ResultDataProjection does not yet own projected digest-match materialization.");

            AssertContains(resultRender, "#include \"Common/ResultDataAccess.h\"", "ResultDataRender does not yet layer on top of the core ResultData access seam.");
            AssertContains(resultRender, "struct ResultRenderPolicy", "ResultDataRender does not yet own render policy state.");
            AssertContains(resultRender, "VisitRenderableResultMetaLines(const ResultData& result, TResultMetaLineVisitor visitor)", "ResultDataRender does not yet own result metadata rendering traversal.");

            AssertDoesNotContain(digestAccess, "struct ResultDigestDisplayInfo", "ResultDigestAccess still owns digest display grouping after the phase 9 seam split.");
            AssertDoesNotContain(digestAccess, "VisitResultDigestDisplayValues(const ResultData& result, bool uppercase, TResultDigestDisplayVisitor visitor)", "ResultDigestAccess still owns digest display traversal after the phase 9 seam split.");
            AssertContains(digestRender, "#include \"Common/ResultDigestValueAccess.h\"", "ResultDigestRender does not yet layer on top of the digest value seam.");
            AssertContains(digestRender, "struct ResultDigestDisplayInfo", "ResultDigestRender does not yet own grouped digest display information.");
            AssertContains(digestRender, "VisitResultDigestDisplayValues(const ResultData& result, bool uppercase, TResultDigestDisplayVisitor visitor)", "ResultDigestRender does not yet own digest display traversal.");

            AssertContains(managedBridgeDispatch, "#include \"Common/HashResultProjection.h\"", "ManagedBridgeDispatch does not yet consume the split HashResult projection seam.");
            AssertContains(bridgeMfcHeader, "#include \"Common/HashResultRender.h\"", "Legacy MFC bridge header does not yet consume the HashResult render seam layered on top of the split render helpers.");
            AssertContains(bridgeMfc, "#include \"Common/ResultDataRender.h\"", "Legacy MFC bridge implementation does not yet consume the split result-data render seam.");
            AssertContains(bridgeMfc, "#include \"Common/ResultDigestRender.h\"", "Legacy MFC bridge implementation does not yet consume the split digest render seam.");
            string filesHashSearchController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSearchController.cpp");
            AssertContains(filesHashSearchController, "#include \"Common/HashResultSearch.h\"", "Legacy MFC search flow does not yet consume the shared HashResult search seam.");
            AssertContains(hashMgmtClr, "#include \"Common/ManagedHashMgmtAccess.h\"", "CLR search bridge does not yet consume the shared managed hash-management seam after the phase 9 projection/search split.");
            AssertContains(hashMgmtUwp, "#include \"Common/ManagedHashMgmtAccess.h\"", "UWP search bridge does not yet consume the shared managed hash-management seam after the phase 9 projection/search split.");
        }, failures);

        Run("Phase 10 splits HashEngine preparation, result-finalization, and result-publication helpers into dedicated implementation files", () =>
        {
            string engine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string fileRunner = string.Join(
                "\r\n",
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileRunner.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp"));
            string scheduler = ReadRepoFile(repoRoot, @"trunk\source\Common\HashScheduler.cpp");
            string engineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string enginePreparation = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp");
            string engineResult = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp");
            string resultPublisher = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");

            AssertContains(engine, "#include \"Common/HashEngineInternal.h\"", "HashEngine.cpp does not yet consume the new internal HashEngine split seam.");
            AssertContains(fileRunner, "bool ProcessOpenedFileHashing(", "HashDigestPipeline.cpp no longer owns the opened-file read/update orchestration.");
            AssertContains(engine, "int WINAPI HashThreadFunc(void *param)", "HashEngine.cpp no longer owns the thread orchestration entry point.");
            AssertDoesNotContain(engine, "static uint64_t PrepareFileMetaResult(", "HashEngine.cpp still owns file-metadata finalization instead of delegating it to the split result implementation file.");
            AssertDoesNotContain(engine, "static void CompleteSuccessfulFileHashing(", "HashEngine.cpp still owns successful-file completion instead of delegating it to the split result implementation file.");
            AssertDoesNotContain(engine, "static bool PrepareHashingWork(", "HashEngine.cpp still owns preparation helpers instead of delegating them to the split preparation implementation file.");

            AssertContains(engineInternal, "namespace HashEngineInternal", "HashEngineInternal.h does not yet expose the private split namespace.");
            AssertContains(engineInternal, "struct FileProgressState", "HashEngineInternal.h does not yet own the grouped file-progress state.");
            AssertContains(engineInternal, "struct FileAttemptState", "HashEngineInternal.h does not yet own the grouped file-attempt state.");
            AssertContains(engineInternal, "struct FileHashContexts", "HashEngineInternal.h does not yet own the grouped file-hash context state.");
            AssertContains(engineInternal, "struct FileExecutionState", "HashEngineInternal.h does not yet own the grouped file-execution state.");

            AssertContains(enginePreparation, "void AccumulatePreScannedFileSize(", "HashEnginePreparation.cpp does not yet own the pre-scan size helper.");
            AssertContains(enginePreparation, "bool PrepareHashingWork(", "HashEnginePreparation.cpp does not yet own the preparation helper.");
            AssertContains(enginePreparation, "HashResult& BeginFileResult(", "HashEnginePreparation.cpp does not yet own the file-result begin helper.");
            AssertContains(enginePreparation, "HashResult& BeginFileHashAttempt(", "HashEnginePreparation.cpp does not yet own the file-attempt begin helper.");

            AssertContains(engineResult, "uint64_t PrepareFileMetaResult(", "HashEngineResult.cpp does not yet own the file-metadata helper.");
            AssertContains(engineResult, "void InitializeFileHashing(", "HashEngineResult.cpp does not yet own the file-hashing initialization helper.");
            AssertContains(engineResult, "void FinalizeDigestStrings(", "HashEngineResult.cpp does not yet own digest finalization.");
            AssertContains(resultPublisher, "void CompleteSuccessfulFileHashing(", "HashResultPublisher.cpp does not yet own successful-file completion.");
            AssertContains(resultPublisher, "void CompleteFileAttempt(", "HashResultPublisher.cpp does not yet own file-attempt completion.");
            AssertContains(fileRunner, "bool RunFileHashAttempt(", "HashFileRunner.cpp does not yet own the single-file execution seam.");
            AssertContains(scheduler, "FileExecutionState executionState = { 0 };", "HashScheduler.cpp does not yet preserve grouped file-execution state for the runner seam.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileRunner.cpp", "Desktop native core project does not yet compile HashFileRunner.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashEnginePreparation.cpp", "Desktop native core project does not yet compile HashEnginePreparation.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashEngineResult.cpp", "Desktop native core project does not yet compile HashEngineResult.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashResultPublisher.cpp", "Desktop native core project does not yet compile HashResultPublisher.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashEngineInternal.h", "Desktop native core project does not yet include HashEngineInternal.h.");
            AssertContains(nativeProject, "<SolutionDir Condition=\"'$(SolutionDir)'==''\">$(ProjectDir)..\\..\\trunk\\</SolutionDir>", "Desktop native core project is missing the standalone SolutionDir fallback required by the direct CI build.");
            AssertContains(nativeProject, "<FHashRuntimeSuffix Condition=\"'$(FHashDynamicRuntime)'=='true'\">-md</FHashRuntimeSuffix>", "Desktop native core project is missing the runtime-variant suffix required for CLR-compatible WinUI builds.");
            AssertContains(nativeProject, @"$(ProjectDir);$(ProjectDir)..\..\trunk\source\;$(SolutionDir)source\;%(AdditionalIncludeDirectories)", "Desktop native core project is missing the standalone include-root fallback required by the direct CI build.");
            AssertContains(nativeProject, "<UseOfMfc Condition=\"'$(FHashDynamicRuntime)'=='true'\">Dynamic</UseOfMfc>", "Desktop native core project is missing the CLR-compatible shared-MFC override.");
            AssertContains(nativeProject, "<RuntimeLibrary Condition=\"'$(FHashDynamicRuntime)'=='true'\">MultiThreadedDLL</RuntimeLibrary>", "Desktop native core project is missing the CLR-compatible dynamic runtime override.");
            AssertContains(nativeProject, @"$(MSBuildProjectName)$(FHashRuntimeSuffix)", "Desktop native core project does not yet route output directories through the runtime-variant suffix.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileRunner.cpp", "Desktop native core filters do not yet expose HashFileRunner.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashEnginePreparation.cpp", "Desktop native core filters do not yet expose HashEnginePreparation.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashEngineResult.cpp", "Desktop native core filters do not yet expose HashEngineResult.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashResultPublisher.cpp", "Desktop native core filters do not yet expose HashResultPublisher.cpp.");

            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashFileRunner.cpp", "WinUI native project still compiles HashFileRunner.cpp instead of consuming fHashNativeCore.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashEnginePreparation.cpp", "WinUI native project still compiles HashEnginePreparation.cpp instead of consuming fHashNativeCore.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashEngineResult.cpp", "WinUI native project still compiles HashEngineResult.cpp instead of consuming fHashNativeCore.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileRunner.cpp", "UWP native project does not yet compile HashFileRunner.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashEnginePreparation.cpp", "UWP native project does not yet compile HashEnginePreparation.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashEngineResult.cpp", "UWP native project does not yet compile HashEngineResult.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashResultPublisher.cpp", "UWP native project does not yet compile HashResultPublisher.cpp.");
        }, failures);

        Run("Phase 11 introduces a dedicated hash-algorithm registry seam while keeping the current four built-in algorithms intact", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string hashAlgorithmRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashAlgorithmRegistry.h");
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string threadAccess = string.Join(
                "\r\n",
                ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataAccess.h"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataExecutionAccess.h"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataInputAccess.h"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataResultAccess.h"));

            AssertContains(global, "std::vector<bool> enabled;", "Global.h does not yet route algorithm-selection state through a registry-sized vector.");

            AssertContains(hashAlgorithmRegistry, "struct HashAlgorithmDescriptor", "HashAlgorithmRegistry does not yet expose the dedicated hash-algorithm descriptor.");
            AssertContains(hashAlgorithmRegistry, "ResultDigestType type;", "HashAlgorithmRegistry does not yet expose the algorithm type field.");
            AssertContains(hashAlgorithmRegistry, "const char *stableName;", "HashAlgorithmRegistry does not yet expose the stable algorithm name field.");
            AssertContains(hashAlgorithmRegistry, "const char *displayLabel;", "HashAlgorithmRegistry does not yet expose the display label field.");
            AssertContains(hashAlgorithmRegistry, "GetRegisteredHashAlgorithmCount()", "HashAlgorithmRegistry does not yet expose the registry-count helper.");
            AssertContains(hashAlgorithmRegistry, "VisitRegisteredHashAlgorithms(THashAlgorithmVisitor visitor)", "HashAlgorithmRegistry does not yet expose the algorithm visitor seam.");
            AssertContains(hashAlgorithmRegistry, "{ RESULT_DIGEST_MD5, \"md5\", \"MD5\" }", "HashAlgorithmRegistry does not yet register MD5.");
            AssertContains(hashAlgorithmRegistry, "{ RESULT_DIGEST_SHA1, \"sha1\", \"SHA1\" }", "HashAlgorithmRegistry does not yet register SHA1.");
            AssertContains(hashAlgorithmRegistry, "{ RESULT_DIGEST_SHA256, \"sha256\", \"SHA256\" }", "HashAlgorithmRegistry does not yet register SHA256.");
            AssertContains(hashAlgorithmRegistry, "{ RESULT_DIGEST_SHA512, \"sha512\", \"SHA512\" }", "HashAlgorithmRegistry does not yet register SHA512.");

            AssertContains(digestAccess, "#include \"Common/HashAlgorithmRegistry.h\"", "ResultDigestAccess does not yet layer on top of the hash-algorithm registry seam.");
            AssertContains(digestAccess, "typedef HashAlgorithmDescriptor ResultDigestMetadata;", "ResultDigestAccess does not yet bridge digest metadata onto the new registry descriptor.");
            AssertContains(digestAccess, "return GetRegisteredHashAlgorithmCount();", "ResultDigestAccess does not yet route digest count through the registry seam.");
            AssertContains(digestAccess, "return GetHashAlgorithmDescriptorAt(index);", "ResultDigestAccess does not yet route metadata lookup through the registry seam.");
            AssertContains(digestAccess, "return GetHashAlgorithmDescriptor(digestType);", "ResultDigestAccess does not yet route type lookup through the registry seam.");
            AssertContains(digestAccess, "return GetHashAlgorithmTypeAt(index);", "ResultDigestAccess does not yet route digest order through the registry seam.");
            AssertContains(digestAccess, "return GetHashAlgorithmIndex(digestType);", "ResultDigestAccess does not yet route digest index lookup through the registry seam.");

            AssertContains(threadAccess, "#include \"Common/HashAlgorithmRegistry.h\"", "ThreadData access seams do not yet consume the hash-algorithm registry seam.");
            AssertContains(threadAccess, "GetHashAlgorithmIndex(digestType)", "ThreadData access seams do not yet route selection storage through the registry index seam.");
            AssertContains(threadAccess, "VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)", "ThreadData access seams do not yet route enabled-algorithm iteration through the registry seam.");
            AssertContains(threadAccess, "ResultDigestType digestType = GetHashAlgorithmDescriptorType(algorithmDescriptor);", "ThreadData access seams do not yet resolve enabled algorithm types through the registry seam.");
        }, failures);

        Run("Phase 12 routes managed and XAML algorithm entry through dynamic registry-driven descriptors while keeping the legacy desktop checkbox surface intact", () =>
        {
            string hashAlgorithmRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashAlgorithmRegistry.h");
            string hashMgmtClrHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.h");
            string hashMgmtClr = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string hashMgmtUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.h");
            string hashMgmtUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");
            string winUiXaml = ReadRepoFile(repoRoot, @"trunk\source\WinUI\MainPage.xaml");
            string winUiPage = ReadRepoFile(repoRoot, @"trunk\source\WinUI\MainPage.xaml.cs");
            string uwpXaml = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\MainPage.xaml");
            string uwpPage = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\MainPage.xaml.cs");

            AssertContains(hashAlgorithmRegistry, "TryGetHashAlgorithmType(int digestTypeValue, ResultDigestType *digestType)", "HashAlgorithmRegistry does not yet expose the generic digest-type conversion seam.");
            AssertContains(hashAlgorithmRegistry, "IsRegisteredHashAlgorithmType(ResultDigestType digestType)", "HashAlgorithmRegistry does not yet expose the registered-type validation helper.");

            AssertContains(hashMgmtClrHeader, "public ref class HashAlgorithmDescriptorNet sealed", "CLR bridge does not yet expose the managed algorithm descriptor.");
            AssertContains(hashMgmtClrHeader, "cli::array<HashAlgorithmDescriptorNet^>^ GetSupportedHashAlgorithms();", "CLR bridge does not yet expose the supported-algorithm list helper.");
            AssertContains(hashMgmtClrHeader, "void SetHashAlgorithmEnabledByDigestType(int digestType, bool val);", "CLR bridge does not yet expose the generic digest-type enable helper.");
            AssertContains(hashMgmtClrHeader, "bool GetHashAlgorithmEnabledByDigestType(int digestType);", "CLR bridge does not yet expose the generic digest-type query helper.");
            AssertContains(hashMgmtClr, "#include \"Common/ManagedHashMgmtAccess.h\"", "CLR bridge does not yet include the shared managed hash-management seam.");
            AssertContains(hashMgmtClr, "CreateSupportedHashAlgorithmDescriptors()", "CLR bridge does not yet materialize a dynamic managed algorithm descriptor list.");
            AssertContains(hashMgmtUwpHeader, "public ref class HashAlgorithmDescriptorNet sealed", "UWP bridge does not yet expose the managed algorithm descriptor.");
            AssertContains(hashMgmtUwpHeader, "Platform::Array<HashAlgorithmDescriptorNet^>^ GetSupportedHashAlgorithms();", "UWP bridge does not yet expose the supported-algorithm list helper.");
            AssertContains(hashMgmtUwpHeader, "void SetHashAlgorithmEnabledByDigestType(int digestType, Platform::Boolean val);", "UWP bridge does not yet expose the generic digest-type enable helper.");
            AssertContains(hashMgmtUwpHeader, "Platform::Boolean GetHashAlgorithmEnabledByDigestType(int digestType);", "UWP bridge does not yet expose the generic digest-type query helper.");
            AssertContains(hashMgmtUwp, "#include \"Common/ManagedHashMgmtAccess.h\"", "UWP bridge does not yet include the shared managed hash-management seam.");
            AssertContains(hashMgmtUwp, "CreateSupportedHashAlgorithmDescriptors()", "UWP bridge does not yet materialize a dynamic managed algorithm descriptor list.");

            AssertContains(winUiXaml, "StackPanelHashAlgorithms", "WinUI page does not yet expose the dynamic algorithm container.");
            AssertContains(winUiPage, "HashAlgorithmDescriptorNet[] m_hashAlgorithms", "WinUI page does not yet store the managed algorithm descriptor list.");
            AssertContains(winUiPage, "Dictionary<int, CheckBox> m_hashAlgorithmCheckBoxes", "WinUI page does not yet track dynamic algorithm checkboxes by digest type.");
            AssertContains(winUiPage, "GetHashAlgorithmSettingKey(HashAlgorithmDescriptorNet hashAlgorithm)", "WinUI page does not yet route algorithm persistence through stable-name keys.");
            AssertContains(winUiPage, "LoadHashAlgorithmControls()", "WinUI page does not yet materialize dynamic algorithm controls.");
            AssertContains(winUiPage, "StackPanelHashAlgorithms.Children.Add(checkBox);", "WinUI page does not yet append dynamic algorithm checkboxes.");
            AssertContains(winUiPage, "SetHashAlgorithmControlsEnabled(bool enabled)", "WinUI page does not yet centralize dynamic algorithm enable/disable state.");
            AssertDoesNotContain(winUiPage, "CheckBoxHashMd5", "WinUI page still hardcodes the MD5 checkbox after introducing dynamic algorithm controls.");

            AssertContains(uwpXaml, "StackPanelHashAlgorithms", "UWP page does not yet expose the dynamic algorithm container.");
            AssertContains(uwpPage, "HashAlgorithmDescriptorNet[] m_hashAlgorithms", "UWP page does not yet store the managed algorithm descriptor list.");
            AssertContains(uwpPage, "Dictionary<int, CheckBox> m_hashAlgorithmCheckBoxes", "UWP page does not yet track dynamic algorithm checkboxes by digest type.");
            AssertContains(uwpPage, "GetHashAlgorithmSettingKey(HashAlgorithmDescriptorNet hashAlgorithm)", "UWP page does not yet route algorithm persistence through stable-name keys.");
            AssertContains(uwpPage, "LoadHashAlgorithmControls()", "UWP page does not yet materialize dynamic algorithm controls.");
            AssertContains(uwpPage, "StackPanelHashAlgorithms.Children.Add(checkBox);", "UWP page does not yet append dynamic algorithm checkboxes.");
            AssertContains(uwpPage, "SetHashAlgorithmControlsEnabled(bool enabled)", "UWP page does not yet centralize dynamic algorithm enable/disable state.");
            AssertDoesNotContain(uwpPage, "CheckBoxHashMd5", "UWP page still hardcodes the MD5 checkbox after introducing dynamic algorithm controls.");
        }, failures);

        Run("Phase 13 extracts the legacy desktop hash-algorithm checkbox flow into a dedicated MFC controller seam", () =>
        {
            string mfcControllerHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashAlgorithmSelectionController.h");
            string mfcController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashAlgorithmSelectionController.cpp");
            string mfcHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.h");
            string mfcDialog = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcInitializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string mfcSessionController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSessionController.cpp");
            string mfcProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string mfcProjectFilters = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj.filters");
            string mfcDialogAndInitialization = mfcDialog + Environment.NewLine + mfcInitializationController;

            AssertContains(mfcControllerHeader, "class FilesHashAlgorithmSelectionController", "Phase 13 is missing the dedicated MFC hash-algorithm selection controller.");
            AssertContains(mfcControllerHeader, "void Initialize(ThreadData* threadData", "Phase 13 controller does not yet expose the checkbox/thread initialization seam.");
            AssertContains(mfcControllerHeader, "void ResetChecks();", "Phase 13 controller does not yet expose the default-checkbox helper.");
            AssertContains(mfcControllerHeader, "void SyncSelections();", "Phase 13 controller does not yet expose the selection-sync helper.");
            AssertContains(mfcControllerHeader, "BOOL ValidateSelection(LPCTSTR noSelectionMessage) const;", "Phase 13 controller does not yet expose the zero-selection validation helper.");
            AssertContains(mfcControllerHeader, "void SetEnabled(BOOL enabled);", "Phase 13 controller does not yet expose the checkbox enable/disable helper.");
            AssertContains(mfcController, "#include \"Common/ThreadDataExecutionAccess.h\"", "Phase 13 controller does not yet layer on top of the thread-data execution seam.");
            AssertContains(mfcController, "VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)", "Phase 13 controller does not yet route checkbox traversal through the registry seam.");
            AssertContains(mfcController, "SetThreadDataHashAlgorithmEnabled(*m_threadData, digestType, (checkBox->GetCheck() != FALSE));", "Phase 13 controller does not yet route checkbox state into ThreadDataAccess.");
            AssertContains(mfcController, "HasEnabledThreadDataHashAlgorithms(*m_threadData)", "Phase 13 controller does not yet validate zero-algorithm selection through ThreadDataAccess.");
            AssertContains(mfcController, "AfxMessageBox(noSelectionMessage, MB_OK | MB_ICONWARNING);", "Phase 13 controller does not yet keep the legacy desktop warning path.");

            AssertContains(mfcHeader, "#include \"FilesHashAlgorithmSelectionController.h\"", "FilesHashDlg.h does not yet include the dedicated phase 13 controller.");
            AssertContains(mfcHeader, "FilesHashAlgorithmSelectionController m_hashAlgorithmSelectionController;", "FilesHashDlg.h does not yet store the dedicated phase 13 controller.");
            AssertDoesNotContain(mfcHeader, "void ResetHashAlgorithmChecks();", "FilesHashDlg.h still exposes the legacy inline checkbox-reset helper after phase 13.");
            AssertDoesNotContain(mfcHeader, "void SyncHashAlgorithmSelections();", "FilesHashDlg.h still exposes the legacy inline selection-sync helper after phase 13.");
            AssertDoesNotContain(mfcHeader, "BOOL ValidateHashAlgorithmSelection();", "FilesHashDlg.h still exposes the legacy inline selection-validation helper after phase 13.");

            AssertContains(mfcDialogAndInitialization, "hashAlgorithmSelectionController->Initialize(threadData, parentWnd);", "FilesHashDlg.cpp does not yet initialize the dedicated phase 13 controller.");
            AssertContains(mfcDialogAndInitialization, "hashAlgorithmSelectionController->ResetChecks();", "FilesHashDlg.cpp does not yet route default checkbox setup through the dedicated controller.");
            AssertContains(mfcDialogAndInitialization, "hashAlgorithmSelectionController->SyncSelections();", "FilesHashDlg.cpp does not yet route checkbox state synchronization through the dedicated controller.");
            AssertContains(mfcSessionController, "m_hashAlgorithmSelectionController->ValidateSelection(noSelectionMessage);", "FilesHashSessionController.cpp does not yet route zero-algorithm validation through the dedicated controller.");
            AssertContains(mfcSessionController, "m_hashAlgorithmSelectionController->SetEnabled(FALSE);", "FilesHashSessionController.cpp does not yet route working-state checkbox disablement through the dedicated controller.");
            AssertContains(mfcSessionController, "m_hashAlgorithmSelectionController->SetEnabled(TRUE);", "FilesHashSessionController.cpp does not yet route idle-state checkbox enablement through the dedicated controller.");
            AssertDoesNotContain(mfcDialog, "void CFilesHashDlg::ResetHashAlgorithmChecks()", "FilesHashDlg.cpp still owns the legacy inline checkbox-reset implementation after phase 13.");
            AssertDoesNotContain(mfcDialog, "void CFilesHashDlg::SyncHashAlgorithmSelections()", "FilesHashDlg.cpp still owns the legacy inline selection-sync implementation after phase 13.");
            AssertDoesNotContain(mfcDialog, "void CFilesHashDlg::ValidateHashAlgorithmSelection()", "FilesHashDlg.cpp still owns the legacy inline selection-validation implementation after phase 13.");

            AssertContains(mfcProject, "source\\WinMFC\\FilesHashAlgorithmSelectionController.cpp", "fileshash.vcxproj does not yet compile the dedicated phase 13 controller.");
            AssertContains(mfcProject, "source\\WinMFC\\FilesHashAlgorithmSelectionController.h", "fileshash.vcxproj does not yet include the dedicated phase 13 controller header.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashAlgorithmSelectionController.cpp", "fileshash.vcxproj.filters does not yet track the dedicated phase 13 controller source.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashAlgorithmSelectionController.h", "fileshash.vcxproj.filters does not yet track the dedicated phase 13 controller header.");
        }, failures);

        Run("Phase 14 extracts shell ExplorerCommand launch flow into a shared core seam and compiles shell extensions in CI", () =>
        {
            string shellCore = ReadRepoFile(repoRoot, @"trunk\source\WinCommon\ShellExplorerCommandCore.h");
            string wuiShellVerb = ReadRepoFile(repoRoot, @"sub-proj\fHashWUIShellExt\ExplorerCommandVerb.cpp");
            string uwpShellVerb = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpShellExt\ExplorerCommandVerb.cpp");
            string wuiShellProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUIShellExt\fHashWUIShellExt.vcxproj");
            string uwpShellProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpShellExt\fHashUwpShellExt.vcxproj");
            string workflow = ReadRepoFile(repoRoot, @".github\workflows\windows-build.yml");

            AssertContains(shellCore, "ResolveWindowsAppExePath(PCWSTR pszExecName, LPWSTR pszPath, size_t cchPath)", "Phase 14 is missing the shared WindowsApps executable-path resolver.");
            AssertContains(shellCore, "BuildShellItemCommandLine(IShellItemArray *psia", "Phase 14 is missing the shared shell-item command-line builder.");
            AssertContains(shellCore, "LaunchShellCommandLine(const sunjwbase::tstring& tstrExecPath, const sunjwbase::tstring& tstrExecCmd)", "Phase 14 is missing the shared detached process launcher.");

            AssertContains(wuiShellVerb, "#include \"WinCommon/ShellExplorerCommandCore.h\"", "WinUI shell extension does not yet include the shared phase 14 shell-command core.");
            AssertContains(wuiShellVerb, "BuildShellItemCommandLine(psia, tstrExecPath, L\"-paths\", &tstrExecCmd);", "WinUI shell extension does not yet route command-line construction through the shared phase 14 shell-command core.");
            AssertContains(wuiShellVerb, "LaunchShellCommandLine(tstrExecPath, tstrExecCmd);", "WinUI shell extension does not yet route process launch through the shared phase 14 shell-command core.");
            AssertDoesNotContain(wuiShellVerb, "__inline HRESULT ResolveWindowsAppExePath(", "WinUI shell extension still keeps a local WindowsApps path helper after phase 14.");
            AssertDoesNotContain(wuiShellVerb, "BOOL bCreated = CreateProcess(", "WinUI shell extension still keeps an inline CreateProcess launch path after phase 14.");

            AssertContains(uwpShellVerb, "#include \"WinCommon/ShellExplorerCommandCore.h\"", "UWP shell extension does not yet include the shared phase 14 shell-command core.");
            AssertContains(uwpShellVerb, "BuildShellItemCommandLine(psia, tstrExecPath, NULL, &tstrExecCmd);", "UWP shell extension does not yet route command-line construction through the shared phase 14 shell-command core.");
            AssertContains(uwpShellVerb, "LaunchShellCommandLine(tstrExecPath, tstrExecCmd);", "UWP shell extension does not yet route process launch through the shared phase 14 shell-command core.");
            AssertDoesNotContain(uwpShellVerb, "__inline HRESULT ResolveWindowsAppExePath(", "UWP shell extension still keeps a local WindowsApps path helper after phase 14.");
            AssertDoesNotContain(uwpShellVerb, "BOOL bCreated = CreateProcess(", "UWP shell extension still keeps an inline CreateProcess launch path after phase 14.");

            AssertContains(wuiShellProject, "<SolutionDir Condition=\"'$(SolutionDir)'==''\">$(ProjectDir)..\\..\\trunk\\</SolutionDir>", "WinUI shell extension project does not yet define a standalone-build SolutionDir fallback for phase 14.");
            AssertContains(wuiShellProject, "$(ProjectDir);$(ProjectDir)..\\..\\trunk\\source\\;", "WinUI shell extension project does not yet route include paths through an explicit ProjectDir-to-trunk source seam in phase 14.");
            AssertContains(wuiShellProject, "$(ProjectDir)..\\..\\trunk\\fHashWUIWap\\ShellExt\\", "WinUI shell extension project does not yet route release post-build output through an explicit ProjectDir-to-trunk WAP seam in phase 14.");
            AssertContains(wuiShellProject, "$(ProjectDir)..\\..\\trunk\\fHashWUIWap\\;", "WinUI shell extension project does not yet route resource includes through an explicit ProjectDir-to-trunk WAP seam in phase 14.");
            AssertContains(uwpShellProject, "<SolutionDir Condition=\"'$(SolutionDir)'==''\">$(ProjectDir)..\\..\\trunk\\</SolutionDir>", "UWP shell extension project does not yet define a standalone-build SolutionDir fallback for phase 14.");
            AssertContains(uwpShellProject, "$(ProjectDir);$(ProjectDir)..\\..\\trunk\\source\\;", "UWP shell extension project does not yet route include paths through an explicit ProjectDir-to-trunk source seam in phase 14.");
            AssertContains(uwpShellProject, "$(ProjectDir)..\\..\\trunk\\source\\WinUWP\\", "UWP shell extension project does not yet route post-build output through an explicit ProjectDir-to-trunk WinUWP seam in phase 14.");
            AssertContains(uwpShellProject, "$(ProjectDir)..\\..\\trunk\\fHashUwpWap\\;", "UWP shell extension project does not yet route resource includes through an explicit ProjectDir-to-trunk WAP seam in phase 14.");

            AssertContains(workflow, "build-wui-shell-ext-x64:", "Windows workflow does not yet compile the WinUI shell extension in phase 14.");
            AssertContains(workflow, "build-uwp-shell-ext-x64:", "Windows workflow does not yet compile the UWP shell extension in phase 14.");
            AssertContains(workflow, "msbuild sub-proj/fHashWUIShellExt/fHashWUIShellExt.vcxproj", "Windows workflow does not yet build the WinUI shell extension project in phase 14.");
            AssertContains(workflow, "msbuild sub-proj/fHashUwpShellExt/fHashUwpShellExt.vcxproj", "Windows workflow does not yet build the UWP shell extension project in phase 14.");
            AssertContains(workflow, "build-wui-shell-ext-x64", "Release gating does not yet include the WinUI shell extension job in phase 14.");
            AssertContains(workflow, "build-uwp-shell-ext-x64", "Release gating does not yet include the UWP shell extension job in phase 14.");
        }, failures);

        Run("Phase 15 extracts shell registration flow into a shared core seam", () =>
        {
            string shellRegisterHeader = ReadRepoFile(repoRoot, @"trunk\source\WinCommon\ShellRegisterExtension.h");
            string shellRegisterImpl = ReadRepoFile(repoRoot, @"trunk\source\WinCommon\ShellRegisterExtensionImpl.h");
            string wuiRegisterHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWUIShellExt\RegisterExtension.h");
            string wuiRegisterCpp = ReadRepoFile(repoRoot, @"sub-proj\fHashWUIShellExt\RegisterExtension.cpp");
            string uwpRegisterHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpShellExt\RegisterExtension.h");
            string uwpRegisterCpp = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpShellExt\RegisterExtension.cpp");

            AssertContains(shellRegisterHeader, "class CRegisterExtension", "Phase 15 is missing the shared shell registration class declaration.");
            AssertContains(shellRegisterHeader, "HRESULT RegisterExplorerCommandVerb", "Phase 15 is missing the shared shell registration explorer-command contract.");
            AssertContains(shellRegisterImpl, "#include \"ShellRegisterExtension.h\"", "Phase 15 shared shell registration implementation does not yet depend on the shared registration header.");
            AssertContains(shellRegisterImpl, "HRESULT CRegisterExtension::RegisterAppAsLocalServer", "Phase 15 shared shell registration implementation is missing the common local-server registration flow.");
            AssertContains(shellRegisterImpl, "HRESULT CRegisterExtension::RegisterExplorerCommandVerb", "Phase 15 shared shell registration implementation is missing the common explorer-command registration flow.");
            AssertContains(shellRegisterImpl, "HRESULT CRegisterExtension::RegSetKeyValuePrintf", "Phase 15 shared shell registration implementation is missing the common registry write helper.");

            AssertContains(wuiRegisterHeader, "#include \"WinCommon/ShellRegisterExtension.h\"", "WinUI shell extension does not yet route RegisterExtension declarations through the shared phase 15 core.");
            AssertDoesNotContain(wuiRegisterHeader, "class CRegisterExtension", "WinUI shell extension still keeps a local CRegisterExtension declaration after phase 15.");
            AssertContains(wuiRegisterCpp, "#include \"WinCommon/ShellRegisterExtensionImpl.h\"", "WinUI shell extension does not yet route RegisterExtension implementation through the shared phase 15 core.");
            AssertDoesNotContain(wuiRegisterCpp, "HRESULT CRegisterExtension::RegisterAppAsLocalServer", "WinUI shell extension still keeps a local registration implementation after phase 15.");

            AssertContains(uwpRegisterHeader, "#include \"WinCommon/ShellRegisterExtension.h\"", "UWP shell extension does not yet route RegisterExtension declarations through the shared phase 15 core.");
            AssertDoesNotContain(uwpRegisterHeader, "class CRegisterExtension", "UWP shell extension still keeps a local CRegisterExtension declaration after phase 15.");
            AssertContains(uwpRegisterCpp, "#include \"WinCommon/ShellRegisterExtensionImpl.h\"", "UWP shell extension does not yet route RegisterExtension implementation through the shared phase 15 core.");
            AssertDoesNotContain(uwpRegisterCpp, "HRESULT CRegisterExtension::RegisterAppAsLocalServer", "UWP shell extension still keeps a local registration implementation after phase 15.");
        }, failures);

        Run("Phase 16 extracts the legacy desktop search flow into a dedicated MFC controller seam", () =>
        {
            string searchControllerHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSearchController.h");
            string searchControllerCpp = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSearchController.cpp");
            string commandController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashCommandController.cpp");
            string initializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string lifecycleController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashLifecycleController.cpp");
            string dlgHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.h");
            string dlgCpp = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string mfcProjectFilters = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj.filters");
            string dlgCppAndInitialization = dlgCpp + Environment.NewLine + initializationController;
            string dlgCppAndCommandAndLifecycle = string.Join(Environment.NewLine, dlgCpp, commandController, lifecycleController);

            AssertContains(searchControllerHeader, "class FilesHashSearchController", "Phase 16 is missing the dedicated MFC search controller declaration.");
            AssertContains(searchControllerHeader, "BOOL BeginSearch", "Phase 16 search controller is missing the begin-search entry point.");
            AssertContains(searchControllerHeader, "void ClearSearch", "Phase 16 search controller is missing the search-clear entry point.");
            AssertContains(searchControllerHeader, "void RebuildCurrentView", "Phase 16 search controller is missing the view-rebuild entry point.");
            AssertContains(searchControllerCpp, "VisitThreadDataPathAndDigestMatchingHashResults", "Phase 16 search controller does not yet route search matching through the shared HashResult search seam.");
            AssertContains(searchControllerCpp, "VisitThreadDataHashResults", "Phase 16 search controller does not yet rebuild list mode through the shared ThreadData HashResult seam.");
            AssertContains(searchControllerCpp, "UIBridgeMFC::AppendResultToHyperEdit", "Phase 16 search controller does not yet route result rendering through the shared MFC bridge seam.");

            AssertContains(dlgHeader, "#include \"FilesHashSearchController.h\"", "FilesHashDlg.h does not yet include the phase 16 search controller.");
            AssertContains(dlgHeader, "FilesHashSearchController m_hashSearchController;", "FilesHashDlg.h does not yet hold the phase 16 search controller member.");
            AssertDoesNotContain(dlgHeader, "BOOL m_bFind;", "FilesHashDlg.h still keeps the old inline search-mode flag after phase 16.");
            AssertDoesNotContain(dlgHeader, "CString m_strFindFile;", "FilesHashDlg.h still keeps the old inline search file filter after phase 16.");
            AssertDoesNotContain(dlgHeader, "CString m_strFindHash;", "FilesHashDlg.h still keeps the old inline search hash filter after phase 16.");
            AssertDoesNotContain(dlgHeader, "void ResultFind(", "FilesHashDlg.h still declares the old inline search renderer after phase 16.");
            AssertDoesNotContain(dlgHeader, "void ClearFind(", "FilesHashDlg.h still declares the old inline search clear helper after phase 16.");
            AssertDoesNotContain(dlgHeader, "void RefreshResult();", "FilesHashDlg.h still declares the old inline result-list refresh helper after phase 16.");

            AssertContains(dlgCppAndInitialization, "hashSearchController->Initialize(threadData, mainEdit, btnClr, btnFind, btnOpen, chkUppercase);", "FilesHashDlg.cpp does not yet initialize the phase 16 search controller.");
            AssertContains(dlgCppAndCommandAndLifecycle, "m_hashSearchController->BeginSearch(CString(), findDialog.GetFindHash(), clearVerifyButtonText)", "FilesHashDlg.cpp does not yet route search start through the phase 16 controller.");
            if (!dlgCppAndCommandAndLifecycle.Contains("m_hashSearchController->RebuildCurrentView();") &&
                !dlgCpp.Contains("m_hashResultViewController.RebuildCurrentViewPreservingScroll(m_hashSearchController);"))
            {
                throw new InvalidOperationException("FilesHashDlg.cpp does not yet route checkup/search refresh through the phase 16 controller.");
            }
            AssertContains(dlgCppAndCommandAndLifecycle, "m_hashSearchController->ClearSearch(clearButtonText);", "FilesHashDlg.cpp does not yet route search clear through the phase 16 controller.");
            AssertContains(dlgCppAndCommandAndLifecycle, "m_hashSearchController->IsActive()", "FilesHashDlg.cpp does not yet query search-mode state through the phase 16 controller.");
            AssertDoesNotContain(dlgCpp, "void CFilesHashDlg::ResultFind(", "FilesHashDlg.cpp still keeps the old inline search renderer after phase 16.");
            AssertDoesNotContain(dlgCpp, "void CFilesHashDlg::ClearFind(", "FilesHashDlg.cpp still keeps the old inline search clear helper after phase 16.");
            AssertDoesNotContain(dlgCpp, "void CFilesHashDlg::RefreshResult()", "FilesHashDlg.cpp still keeps the old inline result-list refresh helper after phase 16.");
            AssertDoesNotContain(dlgCpp, "m_bFind", "FilesHashDlg.cpp still uses the old inline search-mode flag after phase 16.");
            AssertDoesNotContain(dlgCpp, "m_strFindHash", "FilesHashDlg.cpp still uses the old inline search hash field after phase 16.");

            AssertContains(mfcProject, "source\\WinMFC\\FilesHashSearchController.cpp", "fileshash.vcxproj does not yet compile the dedicated phase 16 search controller.");
            AssertContains(mfcProject, "source\\WinMFC\\FilesHashSearchController.h", "fileshash.vcxproj does not yet include the dedicated phase 16 search controller header.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashSearchController.cpp", "fileshash.vcxproj.filters does not yet track the dedicated phase 16 search controller source.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashSearchController.h", "fileshash.vcxproj.filters does not yet track the dedicated phase 16 search controller header.");
        }, failures);

        Run("Phase 17 upgrades the legacy desktop hash-algorithm surface to runtime dynamic controls", () =>
        {
            string mfcControllerHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashAlgorithmSelectionController.h");
            string mfcController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashAlgorithmSelectionController.cpp");
            string dlgHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.h");
            string dlgCpp = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string initializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string mfcRc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\fileshash.rc");
            string dlgCppAndInitialization = dlgCpp + Environment.NewLine + initializationController;

            AssertContains(mfcControllerHeader, "#include <vector>", "Phase 17 controller does not yet depend on the dynamic checkbox collection type.");
            AssertContains(mfcControllerHeader, "struct HashAlgorithmCheckBox", "Phase 17 controller is missing the grouped runtime checkbox state.");
            AssertContains(mfcControllerHeader, "void CreateDynamicCheckBoxes();", "Phase 17 controller is missing the dynamic checkbox creation helper.");
            AssertContains(mfcControllerHeader, "void DestroyDynamicCheckBoxes();", "Phase 17 controller is missing the dynamic checkbox cleanup helper.");
            AssertContains(mfcControllerHeader, "CRect GetCheckBoxLayoutRect() const;", "Phase 17 controller is missing the runtime layout helper.");
            AssertContains(mfcControllerHeader, "std::vector<HashAlgorithmCheckBox> m_checkBoxes;", "Phase 17 controller does not yet store runtime-generated checkbox entries.");

            AssertContains(mfcController, "HASH_ALGORITHM_LAYOUT_SOURCE_IDS[]", "Phase 17 controller does not yet define the legacy checkbox layout-source ids.");
            AssertContains(mfcController, "CreateDynamicCheckBoxes();", "Phase 17 controller does not yet create runtime checkbox controls during initialization.");
            AssertContains(mfcController, "checkBoxEntry.checkBox->Create(", "Phase 17 controller does not yet materialize runtime checkbox windows.");
            AssertContains(mfcController, "GetHashAlgorithmDescriptorDisplayLabel(algorithmDescriptor)", "Phase 17 controller does not yet source checkbox labels from the hash-algorithm registry.");
            AssertContains(mfcController, "layoutControl->ShowWindow(SW_HIDE);", "Phase 17 controller does not yet hide the legacy fixed checkbox resources after using them as layout anchors.");
            AssertDoesNotContain(mfcController, "m_chkMd5", "Phase 17 controller still keeps fixed MD5 checkbox members after dynamicization.");
            AssertDoesNotContain(mfcController, "switch (digestType)", "Phase 17 controller still uses a fixed digest-type switch instead of runtime checkbox traversal.");

            AssertContains(dlgHeader, "FilesHashAlgorithmSelectionController m_hashAlgorithmSelectionController;", "FilesHashDlg.h does not yet keep the phase 17 dynamic checkbox controller.");
            AssertDoesNotContain(dlgHeader, "CButton m_chkMd5;", "FilesHashDlg.h still keeps the fixed MD5 checkbox member after phase 17.");
            AssertDoesNotContain(dlgHeader, "CButton m_chkSha1;", "FilesHashDlg.h still keeps the fixed SHA1 checkbox member after phase 17.");
            AssertDoesNotContain(dlgHeader, "CButton m_chkSha256;", "FilesHashDlg.h still keeps the fixed SHA256 checkbox member after phase 17.");
            AssertDoesNotContain(dlgHeader, "CButton m_chkSha512;", "FilesHashDlg.h still keeps the fixed SHA512 checkbox member after phase 17.");

            AssertContains(dlgCppAndInitialization, "hashAlgorithmSelectionController->Initialize(threadData, parentWnd);", "FilesHashDlg.cpp does not yet initialize the phase 17 controller through the dialog surface.");
            AssertDoesNotContain(dlgCpp, "DDX_Control(pDX, IDC_CHECK_MD5, m_chkMd5);", "FilesHashDlg.cpp still binds the fixed MD5 checkbox after phase 17.");
            AssertDoesNotContain(dlgCpp, "DDX_Control(pDX, IDC_CHECK_SHA1, m_chkSha1);", "FilesHashDlg.cpp still binds the fixed SHA1 checkbox after phase 17.");
            AssertDoesNotContain(dlgCpp, "DDX_Control(pDX, IDC_CHECK_SHA256, m_chkSha256);", "FilesHashDlg.cpp still binds the fixed SHA256 checkbox after phase 17.");
            AssertDoesNotContain(dlgCpp, "DDX_Control(pDX, IDC_CHECK_SHA512, m_chkSha512);", "FilesHashDlg.cpp still binds the fixed SHA512 checkbox after phase 17.");

            AssertContains(mfcRc, "IDC_CHECK_MD5", "MFC resources do not yet provide the legacy checkbox anchors required by the phase 17 dynamic controller.");
            AssertContains(mfcRc, "IDC_CHECK_SHA512", "MFC resources do not yet provide the legacy checkbox anchors required by the phase 17 dynamic controller.");
        }, failures);

        Run("Phase 18 splits ThreadData access into dedicated execution, input, and result seams", () =>
        {
            string threadAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataAccess.h");
            string threadExecutionAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataExecutionAccess.h");
            string threadInputAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataInputAccess.h");
            string threadResultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataResultAccess.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string mfcSearchController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSearchController.cpp");
            string mfcAlgorithmController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashAlgorithmSelectionController.cpp");

            AssertContains(threadAccess, "#include \"Common/ThreadDataExecutionAccess.h\"", "Phase 18 compatibility ThreadDataAccess shim does not yet layer on top of the execution seam.");
            AssertContains(threadAccess, "#include \"Common/ThreadDataInputAccess.h\"", "Phase 18 compatibility ThreadDataAccess shim does not yet layer on top of the input seam.");
            AssertContains(threadAccess, "#include \"Common/ThreadDataResultAccess.h\"", "Phase 18 compatibility ThreadDataAccess shim does not yet layer on top of the result seam.");
            AssertContains(threadAccess, "ResetThreadDataForNewSession(ThreadData& threadData)", "Phase 18 compatibility ThreadDataAccess shim does not yet keep the grouped session-reset helper.");
            AssertDoesNotContain(threadAccess, "AppendThreadDataInputFile(ThreadData& threadData", "Phase 18 compatibility ThreadDataAccess shim still owns input-file helpers after the seam split.");
            AssertDoesNotContain(threadAccess, "VisitThreadDataResults(const ThreadData& threadData", "Phase 18 compatibility ThreadDataAccess shim still owns result traversal after the seam split.");

            AssertContains(threadExecutionAccess, "SetThreadDataObserver(ThreadData& threadData, HashProgressSink *observer)", "Phase 18 execution seam does not yet own progress-sink wiring.");
            AssertContains(threadExecutionAccess, "SetThreadDataWorking(ThreadData& threadData, bool working)", "Phase 18 execution seam does not yet own working-state writes.");
            AssertContains(threadExecutionAccess, "SetThreadDataHashAlgorithmEnabled(ThreadData& threadData, ResultDigestType digestType, bool enabled)", "Phase 18 execution seam does not yet own algorithm enablement.");
            AssertContains(threadExecutionAccess, "GetThreadDataTotalSize(const ThreadData& threadData)", "Phase 18 execution seam does not yet own counted-size reads.");

            AssertContains(threadInputAccess, "GetThreadDataInputFiles(const ThreadData& threadData)", "Phase 18 input seam does not yet own input-file reads.");
            AssertContains(threadInputAccess, "AppendThreadDataInputFile(ThreadData& threadData, const sunjwbase::tstring& fullPath)", "Phase 18 input seam does not yet own input-file appends.");
            AssertContains(threadInputAccess, "ReplaceTrimmedThreadDataInputFiles(ThreadData& threadData, const TStrVector& fullPaths)", "Phase 18 input seam does not yet own trimmed input-file replacement.");
            AssertContains(threadInputAccess, "VisitThreadDataInputFiles(const ThreadData& threadData, TInputFileVisitor visitor)", "Phase 18 input seam does not yet own input-file traversal.");

            AssertContains(threadResultAccess, "GetThreadDataResults(const ThreadData& threadData)", "Phase 18 result seam does not yet own result-list reads.");
            AssertContains(threadResultAccess, "AppendThreadDataResult(ThreadData& threadData)", "Phase 18 result seam does not yet own result-list appends.");
            AssertContains(threadResultAccess, "VisitThreadDataResults(const ThreadData& threadData, TResultVisitor visitor)", "Phase 18 result seam does not yet own result traversal.");

            AssertContains(hashEngineInternal, "#include \"Common/ThreadDataExecutionAccess.h\"", "HashEngineInternal.h does not yet consume the phase 18 execution seam.");
            AssertContains(hashEngineInternal, "#include \"Common/ThreadDataInputAccess.h\"", "HashEngineInternal.h does not yet consume the phase 18 input seam.");
            AssertContains(hashEngineInternal, "#include \"Common/ThreadDataResultAccess.h\"", "HashEngineInternal.h does not yet consume the phase 18 result seam.");
            AssertDoesNotContain(hashEngineInternal, "#include \"Common/ThreadDataAccess.h\"", "HashEngineInternal.h still consumes the umbrella ThreadDataAccess header after the phase 18 seam split.");

            AssertContains(mfcSearchController, "#include \"Common/ThreadDataExecutionAccess.h\"", "MFC search controller does not yet consume the phase 18 execution seam.");
            AssertContains(mfcSearchController, "#include \"Common/ThreadDataResultAccess.h\"", "MFC search controller does not yet consume the phase 18 result seam.");
            AssertDoesNotContain(mfcSearchController, "#include \"Common/ThreadDataAccess.h\"", "MFC search controller still depends on the umbrella ThreadDataAccess header after phase 18.");

            AssertContains(mfcAlgorithmController, "#include \"Common/ThreadDataExecutionAccess.h\"", "MFC algorithm controller does not yet consume the phase 18 execution seam.");
            AssertDoesNotContain(mfcAlgorithmController, "#include \"Common/ThreadDataAccess.h\"", "MFC algorithm controller still depends on the umbrella ThreadDataAccess header after phase 18.");
        }, failures);

        Run("Phase 19 extracts shared managed hash-management helpers for CLR and UWP bridges", () =>
        {
            string managedHashMgmtAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ManagedHashMgmtAccess.h");
            string hashMgmtClr = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string hashMgmtUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");

            AssertContains(managedHashMgmtAccess, "TryConvertManagedHashAlgorithmDigestType(int digestTypeValue, ResultDigestType *digestType)", "Phase 19 is missing the shared managed digest-type conversion helper.");
            AssertContains(managedHashMgmtAccess, "CreateSupportedManagedHashAlgorithmDescriptors(", "Phase 19 is missing the shared managed algorithm-descriptor projection helper.");
            AssertContains(managedHashMgmtAccess, "descriptorNet->StableName =", "Phase 19 shared managed algorithm-descriptor helper does not yet expose stable names.");
            AssertContains(managedHashMgmtAccess, "descriptorNet->DisplayLabel =", "Phase 19 shared managed algorithm-descriptor helper does not yet expose display labels.");
            AssertContains(managedHashMgmtAccess, "SetManagedHashAlgorithmEnabledByDigestType(", "Phase 19 is missing the shared managed digest-type enable helper.");
            AssertContains(managedHashMgmtAccess, "GetManagedHashAlgorithmEnabledByDigestType(", "Phase 19 is missing the shared managed digest-type query helper.");
            AssertContains(managedHashMgmtAccess, "ReplaceThreadDataInputFilesFromManagedArray(", "Phase 19 is missing the shared managed input-file replacement helper.");
            AssertContains(managedHashMgmtAccess, "CreateProjectedManagedDigestMatchingResults(", "Phase 19 is missing the shared managed digest-search projection helper.");

            AssertContains(hashMgmtClr, "#include \"Common/ManagedHashMgmtAccess.h\"", "CLR HashMgmt implementation does not yet consume the phase 19 managed hash-management seam.");
            AssertContains(hashMgmtClr, "CreateSupportedManagedHashAlgorithmDescriptors<HashAlgorithmDescriptorNet^, cli::array<HashAlgorithmDescriptorNet^>^>", "CLR HashMgmt does not yet route descriptor projection through the shared managed helper.");
            AssertContains(hashMgmtClr, "::SetManagedHashAlgorithmEnabledByDigestType(*m_pThreadData, digestTypeValue, val);", "CLR HashMgmt does not yet route digest-type enablement through the shared managed helper.");
            AssertContains(hashMgmtClr, "::GetManagedHashAlgorithmEnabledByDigestType(*m_pThreadData, digestTypeValue);", "CLR HashMgmt does not yet route digest-type queries through the shared managed helper.");
            AssertContains(hashMgmtClr, "ReplaceThreadDataInputFilesFromManagedArray(*m_pThreadData, filePaths, ConvertManagedFilePathToTstr);", "CLR HashMgmt does not yet route managed file ingestion through the shared managed helper.");
            AssertContains(hashMgmtClr, "CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, cli::array<HashResultNet>^>(", "CLR HashMgmt does not yet route digest-search projection through the shared managed helper.");
            AssertDoesNotContain(hashMgmtClr, "#include \"Common/ResultDataSearch.h\"", "CLR HashMgmt still depends directly on the digest-search header after phase 19.");
            AssertDoesNotContain(hashMgmtClr, "#include \"Common/ResultDataProjection.h\"", "CLR HashMgmt still depends directly on the result-projection header after phase 19.");
            AssertDoesNotContain(hashMgmtClr, "#include \"Common/ThreadDataAccess.h\"", "CLR HashMgmt still depends directly on the umbrella ThreadDataAccess header after phase 19.");

            AssertContains(hashMgmtUwp, "#include \"Common/ManagedHashMgmtAccess.h\"", "UWP HashMgmt implementation does not yet consume the phase 19 managed hash-management seam.");
            AssertContains(hashMgmtUwp, "CreateSupportedManagedHashAlgorithmDescriptors<HashAlgorithmDescriptorNet^, Array<HashAlgorithmDescriptorNet^>^>", "UWP HashMgmt does not yet route descriptor projection through the shared managed helper.");
            AssertContains(hashMgmtUwp, "::SetManagedHashAlgorithmEnabledByDigestType(m_threadData, digestTypeValue, val);", "UWP HashMgmt does not yet route digest-type enablement through the shared managed helper.");
            AssertContains(hashMgmtUwp, "::GetManagedHashAlgorithmEnabledByDigestType(m_threadData, digestTypeValue);", "UWP HashMgmt does not yet route digest-type queries through the shared managed helper.");
            AssertContains(hashMgmtUwp, "ReplaceThreadDataInputFilesFromManagedArray(m_threadData, filePaths, ConvertManagedFilePathToTstr);", "UWP HashMgmt does not yet route managed file ingestion through the shared managed helper.");
            AssertContains(hashMgmtUwp, "CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, Array<HashResultNet>^>(", "UWP HashMgmt does not yet route digest-search projection through the shared managed helper.");
            AssertDoesNotContain(hashMgmtUwp, "#include \"Common/ResultDataSearch.h\"", "UWP HashMgmt still depends directly on the digest-search header after phase 19.");
            AssertDoesNotContain(hashMgmtUwp, "#include \"Common/ResultDataProjection.h\"", "UWP HashMgmt still depends directly on the result-projection header after phase 19.");
            AssertDoesNotContain(hashMgmtUwp, "#include \"Common/ThreadDataAccess.h\"", "UWP HashMgmt still depends directly on the umbrella ThreadDataAccess header after phase 19.");
        }, failures);

        Run("Phase 20 extracts the legacy desktop file-input flow into a dedicated controller", () =>
        {
            string inputControllerHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInputController.h");
            string inputController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInputController.cpp");
            string commandController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashCommandController.cpp");
            string messageController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashMessageController.cpp");
            string initializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string dlgHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.h");
            string dlgCpp = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string mfcProjectFilters = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj.filters");
            string dlgCppAndInitialization = dlgCpp + Environment.NewLine + initializationController;
            string dlgCppAndCommand = dlgCpp + Environment.NewLine + commandController;
            string dlgCppAndCommandAndMessage = string.Join(Environment.NewLine, dlgCpp, commandController, messageController);

            AssertContains(inputControllerHeader, "void LoadCommandLineFiles(LPTSTR filesCmdLine);", "Phase 20 input controller is missing the command-line ingestion seam.");
            AssertContains(inputControllerHeader, "BOOL LoadOpenFileDialogSelection(LPCTSTR fileFilter);", "Phase 20 input controller is missing the open-dialog ingestion seam.");
            AssertContains(inputControllerHeader, "BOOL LoadDroppedFiles(HDROP hDropInfo);", "Phase 20 input controller is missing the drag-drop ingestion seam.");
            AssertContains(inputControllerHeader, "BOOL LoadCopyDataFiles(const COPYDATASTRUCT* pCopyDataStruct);", "Phase 20 input controller is missing the WM_COPYDATA ingestion seam.");
            AssertContains(inputControllerHeader, "static TStrVector ParseFilesCmdLine(LPTSTR filesCmdLine);", "Phase 20 input controller is missing the command-line parser seam.");
            AssertContains(inputControllerHeader, "void ClearFilePaths();", "Phase 20 input controller is missing the grouped input-reset helper.");

            AssertContains(inputController, "#include \"Common/ThreadDataInputAccess.h\"", "Phase 20 input controller does not yet consume the dedicated ThreadData input seam.");
            AssertContains(inputController, "CommandLineToArgvW", "Phase 20 input controller does not yet own the hardened command-line parser.");
            AssertContains(inputController, "CopyDraggedPath", "Phase 20 input controller does not yet own long-path-safe drag/drop extraction.");
            AssertContains(inputController, "IsValidCopyDataString", "Phase 20 input controller does not yet own WM_COPYDATA validation.");
            AssertContains(inputController, "ReplaceThreadDataInputFiles(*m_threadData, parameters);", "Phase 20 input controller does not yet route command-line replacement through ThreadData input access.");
            AssertContains(inputController, "AppendThreadDataInputFile(*m_threadData, dlgOpen.GetNextPathName(pos).GetString());", "Phase 20 input controller does not yet route file-dialog appends through ThreadData input access.");
            AssertContains(inputController, "ReplaceTrimmedThreadDataInputFiles(*m_threadData, parameters);", "Phase 20 input controller does not yet route WM_COPYDATA replacement through ThreadData input access.");

            AssertContains(dlgHeader, "#include \"FilesHashInputController.h\"", "FilesHashDlg.h does not yet consume the phase 20 input controller.");
            AssertContains(dlgHeader, "FilesHashInputController m_hashInputController;", "FilesHashDlg.h does not yet keep the phase 20 input controller.");
            AssertDoesNotContain(dlgHeader, "TStrVector ParseFilesCmdLine(LPTSTR filesCmdLine);", "FilesHashDlg.h still declares the old inline command-line parser after phase 20.");
            AssertDoesNotContain(dlgHeader, "void ClearFilePaths();", "FilesHashDlg.h still declares the old inline input-reset helper after phase 20.");

            AssertContains(dlgCppAndInitialization, "hashInputController->Initialize(threadData, parentWnd);", "FilesHashDlg.cpp does not yet initialize the phase 20 input controller.");
            AssertContains(dlgCppAndInitialization, "hashInputController->LoadCommandLineFiles(filesCmdLine);", "FilesHashDlg.cpp does not yet route command-line ingestion through the phase 20 input controller.");
            AssertContains(dlgCppAndCommandAndMessage, "m_hashInputController->LoadDroppedFiles(hDropInfo);", "FilesHashDlg.cpp does not yet route drag-drop ingestion through the phase 20 input controller.");
            AssertContains(dlgCppAndCommandAndMessage, "m_hashInputController->LoadCopyDataFiles(pCopyDataStruct)", "FilesHashDlg.cpp does not yet route WM_COPYDATA ingestion through the phase 20 input controller.");
            AssertContains(dlgCppAndCommand, "m_hashInputController->LoadOpenFileDialogSelection(fileFilter)", "FilesHashDlg.cpp does not yet route open-dialog ingestion through the phase 20 input controller.");
            AssertDoesNotContain(dlgCpp, "TStrVector CFilesHashDlg::ParseFilesCmdLine(", "FilesHashDlg.cpp still keeps the old inline command-line parser after phase 20.");
            AssertDoesNotContain(dlgCpp, "void CFilesHashDlg::ClearFilePaths()", "FilesHashDlg.cpp still keeps the old inline input-reset helper after phase 20.");
            AssertDoesNotContain(dlgCpp, "IsValidCopyDataString(pCopyDataStruct)", "FilesHashDlg.cpp still keeps inline WM_COPYDATA validation after phase 20.");
            AssertDoesNotContain(dlgCpp, "CopyDraggedPath(hDropInfo", "FilesHashDlg.cpp still keeps inline drag-drop path extraction after phase 20.");

            AssertContains(mfcProject, "source\\WinMFC\\FilesHashInputController.cpp", "fileshash.vcxproj does not yet compile the phase 20 input controller.");
            AssertContains(mfcProject, "source\\WinMFC\\FilesHashInputController.h", "fileshash.vcxproj does not yet include the phase 20 input controller header.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashInputController.cpp", "fileshash.vcxproj.filters does not yet track the phase 20 input controller source.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashInputController.h", "fileshash.vcxproj.filters does not yet track the phase 20 input controller header.");
        }, failures);

        Run("Phase 21 extracts the legacy desktop hashing session flow into a dedicated controller", () =>
        {
            string sessionControllerHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSessionController.h");
            string sessionController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSessionController.cpp");
            string commandController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashCommandController.cpp");
            string initializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string lifecycleController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashLifecycleController.cpp");
            string dlgHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.h");
            string dlgCpp = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string mfcProjectFilters = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj.filters");
            string dlgCppAndInitialization = dlgCpp + Environment.NewLine + initializationController;
            string dlgCppAndCommandAndLifecycle = string.Join(Environment.NewLine, dlgCpp, commandController, lifecycleController);

            AssertContains(sessionControllerHeader, "BOOL PrepareHashStart(LPCTSTR noSelectionMessage);", "Phase 21 session controller is missing the pre-start validation seam.");
            AssertContains(sessionControllerHeader, "void StartHashThread();", "Phase 21 session controller is missing the thread-start seam.");
            AssertContains(sessionControllerHeader, "void StopWorkingThread();", "Phase 21 session controller is missing the stop-request seam.");
            AssertContains(sessionControllerHeader, "void SetControls(BOOL working, BOOL limited, LPCTSTR openButtonText, LPCTSTR stopButtonText);", "Phase 21 session controller is missing the grouped working-state UI seam.");
            AssertContains(sessionControllerHeader, "void PrepareDropTarget(CWnd* pWnd, BOOL bAccept);", "Phase 21 session controller is missing the grouped drop-target helper.");

            AssertContains(sessionController, "#include \"Common/HashEngine.h\"", "Phase 21 session controller does not yet own thread startup through HashEngine.h.");
            AssertContains(sessionController, "#include \"Common/ThreadDataExecutionAccess.h\"", "Phase 21 session controller does not yet consume the ThreadData execution seam.");
            AssertContains(sessionController, "m_hashAlgorithmSelectionController->SyncSelections();", "Phase 21 session controller does not yet own hash-algorithm selection sync.");
            AssertContains(sessionController, "m_hashAlgorithmSelectionController->ValidateSelection(noSelectionMessage);", "Phase 21 session controller does not yet own hash-algorithm validation.");
            AssertContains(sessionController, "_beginthreadex", "Phase 21 session controller does not yet own work-thread startup.");
            AssertContains(sessionController, "SetThreadDataStop(*m_threadData, true);", "Phase 21 session controller does not yet own stop-request signaling.");
            AssertContains(sessionController, "PrepareDropTarget(m_parentWnd, TRUE);", "Phase 21 session controller does not yet restore the dialog drop target.");
            AssertContains(sessionController, "PrepareDropTarget(m_mainEditDropTarget, TRUE);", "Phase 21 session controller does not yet restore the result-view drop target.");

            AssertContains(dlgHeader, "#include \"FilesHashSessionController.h\"", "FilesHashDlg.h does not yet consume the phase 21 session controller.");
            AssertContains(dlgHeader, "FilesHashSessionController m_hashSessionController;", "FilesHashDlg.h does not yet keep the phase 21 session controller.");
            AssertDoesNotContain(dlgHeader, "HANDLE m_hWorkThread;", "FilesHashDlg.h still keeps the old inline work-thread handle after phase 21.");
            AssertDoesNotContain(dlgHeader, "void StopWorkingThread();", "FilesHashDlg.h still declares the old inline stop helper after phase 21.");
            AssertDoesNotContain(dlgHeader, "void SetCtrls(BOOL working);", "FilesHashDlg.h still declares the old inline working-state helper after phase 21.");

            AssertContains(dlgCppAndInitialization, "hashSessionController->Initialize(threadData, parentWnd, mainEdit, btnOpen, btnClr, btnFind, btnContext, chkUppercase, hashAlgorithmSelectionController);", "FilesHashDlg.cpp does not yet initialize the phase 21 session controller.");
            AssertContains(dlgCppAndInitialization, "hashSessionController->SetControls(FALSE, limited, openButtonText, stopButtonText);", "FilesHashDlg.cpp does not yet route idle-state UI setup through the phase 21 session controller.");
            AssertContains(dlgCppAndCommandAndLifecycle, "m_hashSessionController->StopWorkingThread();", "FilesHashDlg.cpp does not yet route stop requests through the phase 21 session controller.");
            AssertContains(lifecycleController, "m_hashSessionController->PrepareHashStart(noSelectionMessage)", "FilesHashDlg.cpp does not yet route hash-start validation through the phase 21 session controller.");
            AssertContains(lifecycleController, "m_hashSessionController->StartHashThread();", "FilesHashDlg.cpp does not yet route work-thread startup through the phase 21 session controller.");
            AssertDoesNotContain(dlgCpp, "#include \"Common/HashEngine.h\"", "FilesHashDlg.cpp still depends directly on HashEngine.h after phase 21.");
            AssertDoesNotContain(dlgCpp, "m_hWorkThread", "FilesHashDlg.cpp still keeps the old inline work-thread handle after phase 21.");
            AssertDoesNotContain(dlgCpp, "void CFilesHashDlg::StopWorkingThread()", "FilesHashDlg.cpp still keeps the old inline stop helper after phase 21.");
            AssertDoesNotContain(dlgCpp, "void CFilesHashDlg::SetCtrls(BOOL working)", "FilesHashDlg.cpp still keeps the old inline working-state helper after phase 21.");
            AssertDoesNotContain(dlgCpp, "void PrepareDropTarget(CWnd* pWnd, BOOL bAccept)", "FilesHashDlg.cpp still keeps the old inline drop-target helper after phase 21.");

            AssertContains(mfcProject, "source\\WinMFC\\FilesHashSessionController.cpp", "fileshash.vcxproj does not yet compile the phase 21 session controller.");
            AssertContains(mfcProject, "source\\WinMFC\\FilesHashSessionController.h", "fileshash.vcxproj does not yet include the phase 21 session controller header.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashSessionController.cpp", "fileshash.vcxproj.filters does not yet track the phase 21 session controller source.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashSessionController.h", "fileshash.vcxproj.filters does not yet track the phase 21 session controller header.");
        }, failures);

        Run("Phase 22 extracts the legacy desktop context-menu flow into a dedicated controller", () =>
        {
            string contextControllerHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashContextMenuController.h");
            string contextController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashContextMenuController.cpp");
            string initializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string dlgHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.h");
            string dlgCpp = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string mfcProjectFilters = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj.filters");
            string dlgCppAndInitialization = dlgCpp + Environment.NewLine + initializationController;

            AssertContains(contextControllerHeader, "void RefreshButtonText(LPCTSTR addText, LPCTSTR removeText);", "Phase 22 context controller is missing the button-text sync seam.");
            AssertContains(contextControllerHeader, "BOOL HandleButtonClick(BOOL limited,", "Phase 22 context controller is missing the grouped click-handling seam.");
            AssertContains(contextControllerHeader, "BOOL TryElevateLimitedProcess() const;", "Phase 22 context controller is missing the limited-process elevation helper.");

            AssertContains(contextController, "#include \"WindowsUtils.h\"", "Phase 22 context controller no longer consumes WindowsUtils for context-menu actions.");
            AssertContains(contextController, "WindowsUtils::ContextMenuExisted()", "Phase 22 context controller does not yet own the context-menu existence check.");
            AssertContains(contextController, "WindowsUtils::ElevateProcess()", "Phase 22 context controller does not yet own the elevation path.");
            AssertContains(contextController, "WindowsUtils::RemoveContextMenu(); // Try to delete all items related to fHash", "Phase 22 context controller does not yet keep the pre-add cleanup path.");
            AssertContains(contextController, "WindowsUtils::AddContextMenu()", "Phase 22 context controller does not yet own context-menu creation.");
            AssertContains(contextController, "WindowsUtils::RemoveContextMenu()", "Phase 22 context controller does not yet own context-menu removal.");
            AssertContains(contextController, "WindowsComm::GetWindowsVersion(osvi, bOsVersionInfoEx)", "Phase 22 context controller does not yet gate elevation by Windows version.");

            AssertContains(dlgHeader, "#include \"FilesHashContextMenuController.h\"", "FilesHashDlg.h does not yet consume the phase 22 context controller.");
            AssertContains(dlgHeader, "FilesHashContextMenuController m_hashContextMenuController;", "FilesHashDlg.h does not yet keep the phase 22 context controller.");

            AssertContains(dlgCppAndInitialization, "hashContextMenuController->Initialize(btnContext, parentWnd->GetDlgItem(IDC_STATIC_ADDRESULT));", "FilesHashDlg.cpp does not yet initialize the phase 22 context controller.");
            AssertContains(dlgCppAndInitialization, "hashContextMenuController->RefreshButtonText(addContextText, removeContextText);", "FilesHashDlg.cpp does not yet route initial context-button text through the phase 22 controller.");
            AssertContains(dlgCppAndInitialization, "hashContextMenuController->ResetStatus();", "FilesHashDlg.cpp does not yet route initial context status clearing through the phase 22 controller.");
            AssertContains(dlgCpp, "m_hashContextMenuController.HandleButtonClick(", "FilesHashDlg.cpp does not yet route button clicks through the phase 22 context controller.");
            AssertDoesNotContain(dlgCpp, "WindowsUtils::ContextMenuExisted()", "FilesHashDlg.cpp still performs inline context-menu existence checks after phase 22.");
            AssertDoesNotContain(dlgCpp, "WindowsUtils::AddContextMenu()", "FilesHashDlg.cpp still performs inline context-menu creation after phase 22.");
            AssertDoesNotContain(dlgCpp, "WindowsUtils::RemoveContextMenu()", "FilesHashDlg.cpp still performs inline context-menu removal after phase 22.");
            AssertDoesNotContain(dlgCpp, "WindowsUtils::ElevateProcess()", "FilesHashDlg.cpp still performs inline elevation after phase 22.");
            AssertDoesNotContain(dlgCpp, "WindowsComm::GetWindowsVersion", "FilesHashDlg.cpp still performs inline Windows-version checks after phase 22.");

            AssertContains(mfcProject, "source\\WinMFC\\FilesHashContextMenuController.cpp", "fileshash.vcxproj does not yet compile the phase 22 context controller source.");
            AssertContains(mfcProject, "source\\WinMFC\\FilesHashContextMenuController.h", "fileshash.vcxproj does not yet include the phase 22 context controller header.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashContextMenuController.cpp", "fileshash.vcxproj.filters does not yet track the phase 22 context controller source.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashContextMenuController.h", "fileshash.vcxproj.filters does not yet track the phase 22 context controller header.");
        }, failures);

        Run("Phase 23 extracts the legacy desktop progress and timing flow into a dedicated controller", () =>
        {
            string progressControllerHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashProgressController.h");
            string progressController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashProgressController.cpp");
            string initializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string lifecycleController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashLifecycleController.cpp");
            string dlgHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.h");
            string dlgCpp = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string mfcProjectFilters = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj.filters");
            string dlgCppAndInitialization = dlgCpp + Environment.NewLine + initializationController;

            AssertContains(progressControllerHeader, "void PrepareAdvTaskbar();", "Phase 23 progress controller is missing the taskbar-preparation seam.");
            AssertContains(progressControllerHeader, "void StartTiming(LPCTSTR secondText);", "Phase 23 progress controller is missing the timer-start seam.");
            AssertContains(progressControllerHeader, "void AdvanceTimeTick(LPCTSTR secondText);", "Phase 23 progress controller is missing the timer-tick seam.");
            AssertContains(progressControllerHeader, "void FinishTiming(ULONGLONG totalSize);", "Phase 23 progress controller is missing the finish-speed seam.");
            AssertContains(progressControllerHeader, "void ResetAfterStop();", "Phase 23 progress controller is missing the stop-reset seam.");
            AssertContains(progressControllerHeader, "void SetWholeProgress(UINT pos);", "Phase 23 progress controller is missing the grouped whole-progress seam.");

            AssertContains(progressController, "CoCreateInstance(", "Phase 23 progress controller does not yet own taskbar setup.");
            AssertContains(progressController, "IID_ITaskbarList3", "Phase 23 progress controller does not yet own the taskbar interface binding.");
            AssertContains(progressController, "m_parentWnd->SetTimer(1, 100, NULL);", "Phase 23 progress controller does not yet own timer startup.");
            AssertContains(progressController, "m_parentWnd->KillTimer(m_timerId);", "Phase 23 progress controller does not yet own timer shutdown.");
            AssertContains(progressController, "m_taskbarList->SetProgressValue(", "Phase 23 progress controller does not yet own taskbar progress updates.");
            AssertContains(progressController, "SetSpeedText(", "Phase 23 progress controller does not yet centralize speed-label updates.");
            AssertContains(progressController, "SetTimeText(", "Phase 23 progress controller does not yet centralize time-label updates.");

            AssertContains(dlgHeader, "#include \"FilesHashProgressController.h\"", "FilesHashDlg.h does not yet consume the phase 23 progress controller.");
            AssertContains(dlgHeader, "FilesHashProgressController m_hashProgressController;", "FilesHashDlg.h does not yet keep the phase 23 progress controller.");
            AssertDoesNotContain(dlgHeader, "float m_calculateTime;", "FilesHashDlg.h still keeps the old inline timing state after phase 23.");
            AssertDoesNotContain(dlgHeader, "UINT_PTR m_timer;", "FilesHashDlg.h still keeps the old inline timer handle after phase 23.");
            AssertDoesNotContain(dlgHeader, "BOOL m_bAdvTaskbar;", "FilesHashDlg.h still keeps the old inline taskbar flag after phase 23.");
            AssertDoesNotContain(dlgHeader, "ITaskbarList3* pTl;", "FilesHashDlg.h still keeps the old inline taskbar interface after phase 23.");
            AssertDoesNotContain(dlgHeader, "void PrepareAdvTaskbar();", "FilesHashDlg.h still declares the old inline taskbar helper after phase 23.");
            AssertDoesNotContain(dlgHeader, "void SetWholeProgPos(UINT pos);", "FilesHashDlg.h still declares the old inline progress helper after phase 23.");
            AssertDoesNotContain(dlgHeader, "void CalcSpeed(ULONGLONG tsize);", "FilesHashDlg.h still declares the old inline speed helper after phase 23.");

            AssertContains(dlgCppAndInitialization, "hashProgressController->Initialize(parentWnd, progressCtrl);", "FilesHashDlg.cpp does not yet initialize the phase 23 progress controller.");
            AssertContains(lifecycleController, "m_hashProgressController->PrepareAdvTaskbar();", "FilesHashDlg.cpp does not yet route taskbar prep through the phase 23 progress controller.");
            AssertContains(lifecycleController, "m_hashProgressController->StartTiming(secondText);", "FilesHashDlg.cpp does not yet route timer startup through the phase 23 progress controller.");
            AssertContains(lifecycleController, "m_hashProgressController->AdvanceTimeTick(secondText);", "FilesHashDlg.cpp does not yet route timer ticks through the phase 23 progress controller.");
            AssertContains(lifecycleController, "m_hashProgressController->FinishTiming(GetThreadDataTotalSize(*m_threadData));", "FilesHashDlg.cpp does not yet route finish-speed updates through the phase 23 progress controller.");
            AssertContains(lifecycleController, "m_hashProgressController->ResetAfterStop();", "FilesHashDlg.cpp does not yet route stop resets through the phase 23 progress controller.");
            AssertContains(lifecycleController, "m_hashProgressController->SetWholeProgress((int)lParam);", "FilesHashDlg.cpp does not yet route progress callbacks through the phase 23 progress controller.");
            AssertDoesNotContain(dlgCpp, "void CFilesHashDlg::PrepareAdvTaskbar()", "FilesHashDlg.cpp still keeps the old inline taskbar helper after phase 23.");
            AssertDoesNotContain(dlgCpp, "void CFilesHashDlg::SetWholeProgPos(UINT pos)", "FilesHashDlg.cpp still keeps the old inline progress helper after phase 23.");
            AssertDoesNotContain(dlgCpp, "void CFilesHashDlg::CalcSpeed(ULONGLONG tsize)", "FilesHashDlg.cpp still keeps the old inline speed helper after phase 23.");

            AssertContains(mfcProject, "source\\WinMFC\\FilesHashProgressController.cpp", "fileshash.vcxproj does not yet compile the phase 23 progress controller source.");
            AssertContains(mfcProject, "source\\WinMFC\\FilesHashProgressController.h", "fileshash.vcxproj does not yet include the phase 23 progress controller header.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashProgressController.cpp", "fileshash.vcxproj.filters does not yet track the phase 23 progress controller source.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashProgressController.h", "fileshash.vcxproj.filters does not yet track the phase 23 progress controller header.");
        }, failures);

        Run("Phase 24 extracts the legacy desktop result-view and HyperEdit flow into a dedicated controller", () =>
        {
            string resultViewControllerHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashResultViewController.h");
            string resultViewController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashResultViewController.cpp");
            string commandController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashCommandController.cpp");
            string messageController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashMessageController.cpp");
            string initializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string lifecycleController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashLifecycleController.cpp");
            string dlgHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.h");
            string dlgCpp = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string mfcProjectFilters = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj.filters");
            string dlgCppAndInitialization = dlgCpp + Environment.NewLine + initializationController;
            string dlgCppAndCommandAndMessageAndLifecycle = string.Join(Environment.NewLine, dlgCpp, commandController, messageController, lifecycleController);

            AssertContains(resultViewControllerHeader, "void ShowInitialInfo(LPCTSTR initInfo);", "Phase 24 result-view controller is missing the initial-info seam.");
            AssertContains(resultViewControllerHeader, "void ClearResults(ThreadData& threadData);", "Phase 24 result-view controller is missing the clear-results seam.");
            AssertContains(resultViewControllerHeader, "void RefreshMainText(BOOL scrollToEnd = TRUE);", "Phase 24 result-view controller is missing the text-refresh seam.");
            AssertContains(resultViewControllerHeader, "void RebuildCurrentViewPreservingScroll(FilesHashSearchController& searchController);", "Phase 24 result-view controller is missing the scroll-preserving rebuild seam.");
            AssertContains(resultViewControllerHeader, "void ToggleUppercaseAndRebuild(CButton* chkUppercase, FilesHashSearchController& searchController);", "Phase 24 result-view controller is missing the uppercase-toggle seam.");
            AssertContains(resultViewControllerHeader, "void ShowHyperEditMenu(CWnd* ownerWnd);", "Phase 24 result-view controller is missing the HyperEdit menu seam.");
            AssertContains(resultViewControllerHeader, "void UpdatePopupMenu(CWnd* ownerWnd, CMenu* pPopupMenu);", "Phase 24 result-view controller is missing the popup-update seam.");
            AssertContains(resultViewControllerHeader, "void CopyLastHyperlink() const;", "Phase 24 result-view controller is missing the copy-hyperlink seam.");

            AssertContains(resultViewController, "#include \"WindowsUtils.h\"", "Phase 24 result-view controller does not yet own clipboard integration.");
            AssertContains(resultViewController, "#include \"resource.h\"", "Phase 24 result-view controller does not yet consume HyperEdit menu resources.");
            AssertContains(resultViewController, "ClearThreadDataResults(threadData);", "Phase 24 result-view controller does not yet own result clearing.");
            AssertContains(resultViewController, "searchController.RebuildCurrentView();", "Phase 24 result-view controller does not yet own the rebuilt-view refresh path.");
            AssertContains(resultViewController, "m_mainEdit->GetFirstVisibleLine()", "Phase 24 result-view controller does not yet own scroll preservation.");
            AssertContains(resultViewController, "menuHyperEdit.LoadMenu(IDR_MENU_HYPEREDIT);", "Phase 24 result-view controller does not yet own the HyperEdit menu load.");
            AssertContains(resultViewController, "WindowsUtils::CopyCString(m_mainEdit->GetLastHyperlink());", "Phase 24 result-view controller does not yet own copy-hyperlink handling.");
            AssertContains(resultViewController, "pCmdUI->SetText(copyText);", "Phase 24 result-view controller does not yet own menu-text updates.");

            AssertContains(dlgHeader, "#include \"FilesHashResultViewController.h\"", "FilesHashDlg.h does not yet consume the phase 24 result-view controller.");
            AssertContains(dlgHeader, "FilesHashResultViewController m_hashResultViewController;", "FilesHashDlg.h does not yet keep the phase 24 result-view controller.");
            AssertDoesNotContain(dlgHeader, "void RefreshMainText(BOOL bScrollToEnd = TRUE);", "FilesHashDlg.h still declares the old inline text-refresh helper after phase 24.");

            AssertContains(dlgCppAndInitialization, "hashResultViewController->Initialize(mainMutex, mainEdit);", "FilesHashDlg.cpp does not yet initialize the phase 24 result-view controller.");
            AssertContains(dlgCppAndInitialization, "hashResultViewController->ShowInitialInfo(initInfoText);", "FilesHashDlg.cpp does not yet route initial result text through the phase 24 result-view controller.");
            AssertContains(dlgCppAndCommandAndMessageAndLifecycle, "m_hashResultViewController->ClearResults(*m_threadData);", "FilesHashDlg.cpp does not yet route clear-results flow through the phase 24 result-view controller.");
            AssertContains(dlgCppAndCommandAndMessageAndLifecycle, "m_hashResultViewController->RefreshMainText();", "FilesHashDlg.cpp does not yet route text refresh through the phase 24 result-view controller.");
            AssertContains(dlgCppAndCommandAndMessageAndLifecycle, "m_hashResultViewController->RefreshMainText(FALSE);", "FilesHashDlg.cpp does not yet route non-scrolling refresh through the phase 24 result-view controller.");
            AssertContains(dlgCpp, "m_hashResultViewController.RebuildCurrentViewPreservingScroll(m_hashSearchController);", "FilesHashDlg.cpp does not yet route result rebuilding through the phase 24 result-view controller.");
            AssertContains(dlgCpp, "m_hashResultViewController.ToggleUppercaseAndRebuild(&m_chkUppercase, m_hashSearchController);", "FilesHashDlg.cpp does not yet route uppercase rebuild through the phase 24 result-view controller.");
            AssertContains(dlgCppAndCommandAndMessageAndLifecycle, "m_hashResultViewController->AppendLineBreakAndScrollEnd();", "FilesHashDlg.cpp does not yet route stop-output updates through the phase 24 result-view controller.");
            AssertContains(dlgCppAndCommandAndMessageAndLifecycle, "m_hashResultViewController->ShowHyperEditMenu(m_parentWnd);", "FilesHashDlg.cpp does not yet route HyperEdit popup display through the phase 24 result-view controller.");
            AssertContains(dlgCppAndCommandAndMessageAndLifecycle, "m_hashResultViewController->UpdatePopupMenu(m_parentWnd, pPopupMenu);", "FilesHashDlg.cpp does not yet route popup-menu state through the phase 24 result-view controller.");
            AssertContains(dlgCppAndCommandAndMessageAndLifecycle, "m_hashResultViewController->CopyLastHyperlink();", "FilesHashDlg.cpp does not yet route hyperlink copying through the phase 24 result-view controller.");
            AssertContains(dlgCppAndCommandAndMessageAndLifecycle, "m_hashResultViewController->UpdateCopyHashMenuText(pCmdUI, copyText);", "FilesHashDlg.cpp does not yet route menu-text updates through the phase 24 result-view controller.");
            AssertDoesNotContain(dlgCpp, "void CFilesHashDlg::RefreshMainText(", "FilesHashDlg.cpp still keeps the old inline text-refresh helper after phase 24.");
            AssertDoesNotContain(dlgCpp, "WindowsUtils::CopyCString(", "FilesHashDlg.cpp still performs inline copy-hyperlink handling after phase 24.");
            AssertDoesNotContain(dlgCpp, "menuHyperEdit.LoadMenu(IDR_MENU_HYPEREDIT);", "FilesHashDlg.cpp still performs inline HyperEdit menu loading after phase 24.");
            AssertDoesNotContain(dlgCpp, "GetFirstVisibleLine()", "FilesHashDlg.cpp still performs inline scroll-preserving rebuilds after phase 24.");

            AssertContains(mfcProject, "source\\WinMFC\\FilesHashResultViewController.cpp", "fileshash.vcxproj does not yet compile the phase 24 result-view controller source.");
            AssertContains(mfcProject, "source\\WinMFC\\FilesHashResultViewController.h", "fileshash.vcxproj does not yet include the phase 24 result-view controller header.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashResultViewController.cpp", "fileshash.vcxproj.filters does not yet track the phase 24 result-view controller source.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashResultViewController.h", "fileshash.vcxproj.filters does not yet track the phase 24 result-view controller header.");
        }, failures);

        Run("Phase 25 extracts the legacy desktop initialization flow into a dedicated controller", () =>
        {
            string initializationControllerHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.h");
            string initializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string dlgHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.h");
            string dlgCpp = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string mfcProjectFilters = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj.filters");

            AssertContains(initializationControllerHeader, "class FilesHashInitializationController", "Phase 25 initialization controller header is missing the controller type.");
            AssertContains(initializationControllerHeader, "void InitializeDialog(", "Phase 25 initialization controller header is missing the grouped initialization seam.");

            AssertContains(initializationController, "hashProgressController->PrepareAdvTaskbar();", "Phase 25 initialization controller does not yet own startup taskbar preparation.");
            AssertContains(initializationController, "SetDialogItemText(parentWnd, IDC_STATIC_SPEED, _T(\"\"));", "Phase 25 initialization controller does not yet own startup speed-label clearing.");
            AssertContains(initializationController, "*uiBridgeMFC = new UIBridgeMFC", "Phase 25 initialization controller does not yet own bridge creation.");
            AssertContains(initializationController, "SetThreadDataObserver(*threadData, *uiBridgeMFC);", "Phase 25 initialization controller does not yet own observer wiring.");
            AssertContains(initializationController, "ResetThreadDataForNewSession(*threadData);", "Phase 25 initialization controller does not yet own session reset.");
            AssertContains(initializationController, "hashResultViewController->ShowInitialInfo(initInfoText);", "Phase 25 initialization controller does not yet own initial result text.");
            AssertContains(initializationController, "hashContextMenuController->RefreshButtonText(addContextText, removeContextText);", "Phase 25 initialization controller does not yet own context-button text refresh.");
            AssertContains(initializationController, "hashSessionController->SetControls(FALSE, limited, openButtonText, stopButtonText);", "Phase 25 initialization controller does not yet own the initial control-state setup.");
            AssertContains(initializationController, "hashInputController->LoadCommandLineFiles(filesCmdLine);", "Phase 25 initialization controller does not yet own command-line file loading.");
            AssertContains(initializationController, "SetThreadDataWorking(*threadData, false);", "Phase 25 initialization controller does not yet own the initial working-state reset.");
            AssertContains(initializationController, "progressCtrl->SetRange(0, 99);", "Phase 25 initialization controller does not yet own startup progress-range setup.");
            AssertContains(initializationController, "hashAlgorithmSelectionController->ResetChecks();", "Phase 25 initialization controller does not yet own algorithm-checkbox reset.");
            AssertContains(initializationController, "hashAlgorithmSelectionController->SyncSelections();", "Phase 25 initialization controller does not yet own algorithm-selection synchronization.");
            AssertContains(initializationController, "if (HasThreadDataInputFiles(*threadData))", "Phase 25 initialization controller does not yet own the delayed command-line start gate.");
            AssertContains(initializationController, "parentWnd->SetTimer(4, 50, NULL);", "Phase 25 initialization controller does not yet own the delayed command-line timer.");

            AssertContains(dlgHeader, "#include \"FilesHashInitializationController.h\"", "FilesHashDlg.h does not yet consume the phase 25 initialization controller.");
            AssertContains(dlgHeader, "FilesHashInitializationController m_hashInitializationController;", "FilesHashDlg.h does not yet keep the phase 25 initialization controller.");

            AssertContains(dlgCpp, "m_hashInitializationController.InitializeDialog(", "FilesHashDlg.cpp does not yet route startup initialization through the phase 25 controller.");
            AssertDoesNotContain(dlgCpp, "m_hashInputController.LoadCommandLineFiles(theApp.m_lpCmdLine);", "FilesHashDlg.cpp still performs inline command-line loading after phase 25.");
            AssertDoesNotContain(dlgCpp, "m_progWhole.SetRange(0, 99);", "FilesHashDlg.cpp still performs inline startup progress-range setup after phase 25.");
            AssertDoesNotContain(dlgCpp, "m_hashAlgorithmSelectionController.ResetChecks();", "FilesHashDlg.cpp still performs inline startup algorithm reset after phase 25.");
            AssertDoesNotContain(dlgCpp, "m_hashContextMenuController.RefreshButtonText(GetStringByKey(MAINDLG_ADD_CONTEXT_MENU), GetStringByKey(MAINDLG_REMOVE_CONTEXT_MENU));", "FilesHashDlg.cpp still performs inline startup context-button text refresh after phase 25.");
            AssertDoesNotContain(dlgCpp, "m_hashResultViewController.ShowInitialInfo(GetStringByKey(MAINDLG_INITINFO));", "FilesHashDlg.cpp still performs inline startup initial-info rendering after phase 25.");
            AssertContains(mfcProject, "source\\WinMFC\\FilesHashInitializationController.cpp", "fileshash.vcxproj does not yet compile the phase 25 initialization controller source.");
            AssertContains(mfcProject, "source\\WinMFC\\FilesHashInitializationController.h", "fileshash.vcxproj does not yet include the phase 25 initialization controller header.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashInitializationController.cpp", "fileshash.vcxproj.filters does not yet track the phase 25 initialization controller source.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashInitializationController.h", "fileshash.vcxproj.filters does not yet track the phase 25 initialization controller header.");
        }, failures);

        Run("Phase 26 extracts the legacy desktop lifecycle flow into a dedicated controller", () =>
        {
            string lifecycleControllerHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashLifecycleController.h");
            string lifecycleController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashLifecycleController.cpp");
            string commandController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashCommandController.cpp");
            string messageController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashMessageController.cpp");
            string initializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string dlgHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.h");
            string dlgCpp = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string mfcProjectFilters = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj.filters");
            string dlgCppAndInitialization = dlgCpp + Environment.NewLine + initializationController;
            string dlgCppAndCommandAndMessage = string.Join(Environment.NewLine, dlgCpp, commandController, messageController);

            AssertContains(lifecycleControllerHeader, "class FilesHashLifecycleController", "Phase 26 lifecycle controller header is missing the controller type.");
            AssertContains(lifecycleControllerHeader, "void StartHashing(LPCTSTR clearButtonText, LPCTSTR secondText, LPCTSTR noSelectionMessage);", "Phase 26 lifecycle controller is missing the grouped hash-start seam.");
            AssertContains(lifecycleControllerHeader, "void HandleTimer(UINT_PTR nIDEvent, LPCTSTR secondText, LPCTSTR clearButtonText, LPCTSTR noSelectionMessage);", "Phase 26 lifecycle controller is missing the grouped timer seam.");
            AssertContains(lifecycleControllerHeader, "LRESULT HandleThreadMessage(WPARAM wParam, LPARAM lParam, BOOL limited, LPCTSTR openButtonText, LPCTSTR stopButtonText);", "Phase 26 lifecycle controller is missing the grouped thread-message seam.");
            AssertContains(lifecycleControllerHeader, "BOOL HandleClose();", "Phase 26 lifecycle controller is missing the close-handling seam.");

            AssertContains(lifecycleController, "#include \"Common/ThreadDataExecutionAccess.h\"", "Phase 26 lifecycle controller does not yet consume the ThreadData execution seam.");
            AssertContains(lifecycleController, "m_hashSearchController->ClearSearch(clearButtonText);", "Phase 26 lifecycle controller does not yet own search-reset before hashing.");
            AssertContains(lifecycleController, "m_hashProgressController->PrepareAdvTaskbar();", "Phase 26 lifecycle controller does not yet own taskbar preparation.");
            AssertContains(lifecycleController, "m_hashSessionController->PrepareHashStart(noSelectionMessage)", "Phase 26 lifecycle controller does not yet own hash-start validation.");
            AssertContains(lifecycleController, "m_hashSessionController->StartHashThread();", "Phase 26 lifecycle controller does not yet own work-thread startup.");
            AssertContains(lifecycleController, "m_parentWnd->KillTimer(4);", "Phase 26 lifecycle controller does not yet own delayed-start timer cleanup.");
            AssertContains(lifecycleController, "GetThreadDataTotalSize(*m_threadData)", "Phase 26 lifecycle controller does not yet own finish-time size lookup.");
            AssertContains(lifecycleController, "m_hashResultViewController->AppendLineBreakAndScrollEnd();", "Phase 26 lifecycle controller does not yet own stop-output updates.");
            AssertContains(lifecycleController, "delete *m_uiBridgeMFC;", "Phase 26 lifecycle controller does not yet own bridge cleanup during close.");
            AssertContains(lifecycleController, "m_parentWnd->PostMessage(WM_CLOSE);", "Phase 26 lifecycle controller does not yet own deferred close after worker shutdown.");

            AssertContains(dlgHeader, "#include \"FilesHashLifecycleController.h\"", "FilesHashDlg.h does not yet consume the phase 26 lifecycle controller.");
            AssertContains(dlgHeader, "FilesHashLifecycleController m_hashLifecycleController;", "FilesHashDlg.h does not yet keep the phase 26 lifecycle controller.");
            AssertDoesNotContain(dlgHeader, "BOOL m_waitingExit;", "FilesHashDlg.h still keeps the old inline waiting-exit flag after phase 26.");
            AssertDoesNotContain(dlgHeader, "void DoMD5();", "FilesHashDlg.h still declares the old inline hash-start helper after phase 26.");

            AssertContains(dlgCppAndInitialization, "hashLifecycleController->Initialize(threadData, parentWnd, btnClr, uiBridgeMFC, hashSearchController, hashSessionController, hashProgressController, hashResultViewController);", "FilesHashDlg.cpp does not yet initialize the phase 26 lifecycle controller.");
            AssertContains(dlgCppAndCommandAndMessage, "m_hashLifecycleController->StartHashing(clearButtonText, secondText, noSelectionMessage);", "FilesHashDlg.cpp does not yet route hash-start requests through the phase 26 lifecycle controller.");
            AssertContains(dlgCpp, "m_hashLifecycleController.HandleTimer(nIDEvent, GetStringByKey(SECOND_STRING), GetStringByKey(MAINDLG_CLEAR), GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM));", "FilesHashDlg.cpp does not yet route timer handling through the phase 26 lifecycle controller.");
            AssertContains(dlgCpp, "m_hashLifecycleController.HandleThreadMessage(wParam, lParam, m_bLimited, GetStringByKey(MAINDLG_OPEN), GetStringByKey(MAINDLG_STOP));", "FilesHashDlg.cpp does not yet route thread messages through the phase 26 lifecycle controller.");
            AssertContains(dlgCpp, "m_hashLifecycleController.HandleClose()", "FilesHashDlg.cpp does not yet route close handling through the phase 26 lifecycle controller.");
            AssertDoesNotContain(dlgCpp, "void CFilesHashDlg::DoMD5()", "FilesHashDlg.cpp still keeps the old inline hash-start helper after phase 26.");
            AssertDoesNotContain(dlgCpp, "m_waitingExit = TRUE;", "FilesHashDlg.cpp still keeps the old inline waiting-exit branch after phase 26.");
            AssertDoesNotContain(dlgCpp, "m_hashProgressController.FinishTiming(GetThreadDataTotalSize(m_thrdData));", "FilesHashDlg.cpp still performs inline finish-timing updates after phase 26.");

            AssertContains(mfcProject, "source\\WinMFC\\FilesHashLifecycleController.cpp", "fileshash.vcxproj does not yet compile the phase 26 lifecycle controller source.");
            AssertContains(mfcProject, "source\\WinMFC\\FilesHashLifecycleController.h", "fileshash.vcxproj does not yet include the phase 26 lifecycle controller header.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashLifecycleController.cpp", "fileshash.vcxproj.filters does not yet track the phase 26 lifecycle controller source.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashLifecycleController.h", "fileshash.vcxproj.filters does not yet track the phase 26 lifecycle controller header.");
        }, failures);

        Run("Phase 27 extracts the legacy desktop command-button flow into a dedicated controller", () =>
        {
            string commandControllerHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashCommandController.h");
            string commandController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashCommandController.cpp");
            string initializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string dlgHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.h");
            string dlgCpp = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string mfcProjectFilters = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj.filters");
            string dlgCppAndInitialization = dlgCpp + Environment.NewLine + initializationController;

            AssertContains(commandControllerHeader, "class FilesHashCommandController", "Phase 27 command controller header is missing the controller type.");
            AssertContains(commandControllerHeader, "void HandleOpenButtonClick(LPCTSTR fileFilter, LPCTSTR clearButtonText, LPCTSTR secondText, LPCTSTR noSelectionMessage);", "Phase 27 command controller is missing the open-button seam.");
            AssertContains(commandControllerHeader, "void HandleExitButtonClick() const;", "Phase 27 command controller is missing the exit-button seam.");
            AssertContains(commandControllerHeader, "void HandleAboutButtonClick() const;", "Phase 27 command controller is missing the about-button seam.");
            AssertContains(commandControllerHeader, "void HandleCleanButtonClick(LPCTSTR clearButtonText, LPCTSTR clearVerifyButtonText);", "Phase 27 command controller is missing the clean-button seam.");
            AssertContains(commandControllerHeader, "void HandleFindButtonClick(LPCTSTR clearVerifyButtonText) const;", "Phase 27 command controller is missing the find-button seam.");

            AssertContains(commandController, "#include \"AboutDlg.h\"", "Phase 27 command controller does not yet own the About dialog include.");
            AssertContains(commandController, "#include \"FindDlg.h\"", "Phase 27 command controller does not yet own the Find dialog include.");
            AssertContains(commandController, "#include \"Common/ThreadDataExecutionAccess.h\"", "Phase 27 command controller does not yet consume the execution seam.");
            AssertContains(commandController, "m_hashInputController->LoadOpenFileDialogSelection(fileFilter)", "Phase 27 command controller does not yet own open-dialog file loading.");
            AssertContains(commandController, "m_hashLifecycleController->StartHashing(clearButtonText, secondText, noSelectionMessage);", "Phase 27 command controller does not yet own open-button hash starts.");
            AssertContains(commandController, "m_hashSessionController->StopWorkingThread();", "Phase 27 command controller does not yet own open-button stop behavior.");
            AssertContains(commandController, "m_hashResultViewController->ClearResults(*m_threadData);", "Phase 27 command controller does not yet own clear-results dispatch.");
            AssertContains(commandController, "ClearProgressLabels();", "Phase 27 command controller does not yet own grouped progress-label clearing.");
            AssertContains(commandController, "m_hashSearchController->BeginSearch(CString(), findDialog.GetFindHash(), clearVerifyButtonText)", "Phase 27 command controller does not yet own find-dialog dispatch.");
            AssertContains(commandController, "m_parentWnd->PostMessage(WM_CLOSE);", "Phase 27 command controller does not yet own exit-button close dispatch.");

            AssertContains(dlgHeader, "#include \"FilesHashCommandController.h\"", "FilesHashDlg.h does not yet consume the phase 27 command controller.");
            AssertContains(dlgHeader, "FilesHashCommandController m_hashCommandController;", "FilesHashDlg.h does not yet keep the phase 27 command controller.");

            AssertContains(dlgCppAndInitialization, "hashCommandController->Initialize(threadData, parentWnd, btnClr, hashInputController, hashSearchController, hashSessionController, hashLifecycleController, hashProgressController, hashResultViewController);", "FilesHashDlg.cpp does not yet initialize the phase 27 command controller.");
            AssertContains(dlgCpp, "m_hashCommandController.HandleOpenButtonClick(filter, GetStringByKey(MAINDLG_CLEAR), GetStringByKey(SECOND_STRING), GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM));", "FilesHashDlg.cpp does not yet route open-button handling through the phase 27 command controller.");
            AssertContains(dlgCpp, "m_hashCommandController.HandleExitButtonClick();", "FilesHashDlg.cpp does not yet route exit-button handling through the phase 27 command controller.");
            AssertContains(dlgCpp, "m_hashCommandController.HandleAboutButtonClick();", "FilesHashDlg.cpp does not yet route about-button handling through the phase 27 command controller.");
            AssertContains(dlgCpp, "m_hashCommandController.HandleCleanButtonClick(GetStringByKey(MAINDLG_CLEAR), GetStringByKey(MAINDLG_CLEAR_VERIFY));", "FilesHashDlg.cpp does not yet route clean-button handling through the phase 27 command controller.");
            AssertContains(dlgCpp, "m_hashCommandController.HandleFindButtonClick(GetStringByKey(MAINDLG_CLEAR_VERIFY));", "FilesHashDlg.cpp does not yet route find-button handling through the phase 27 command controller.");

            AssertDoesNotContain(dlgCpp, "#include \"FindDlg.h\"", "FilesHashDlg.cpp still includes the old inline find-dialog header after phase 27.");
            AssertDoesNotContain(dlgCpp, "#include \"AboutDlg.h\"", "FilesHashDlg.cpp still includes the old inline about-dialog header after phase 27.");
            AssertDoesNotContain(dlgCpp, "CFindDlg Find;", "FilesHashDlg.cpp still performs inline find-dialog handling after phase 27.");
            AssertDoesNotContain(dlgCpp, "CAboutDlg About;", "FilesHashDlg.cpp still performs inline about-dialog handling after phase 27.");
            AssertDoesNotContain(dlgCpp, "m_btnClr.GetWindowText(strBtnText);", "FilesHashDlg.cpp still performs inline clean-button text branching after phase 27.");
            AssertDoesNotContain(dlgCpp, "m_hashInputController.LoadOpenFileDialogSelection(filter)", "FilesHashDlg.cpp still performs inline open-dialog file loading after phase 27.");

            AssertContains(mfcProject, "source\\WinMFC\\FilesHashCommandController.cpp", "fileshash.vcxproj does not yet compile the phase 27 command controller source.");
            AssertContains(mfcProject, "source\\WinMFC\\FilesHashCommandController.h", "fileshash.vcxproj does not yet include the phase 27 command controller header.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashCommandController.cpp", "fileshash.vcxproj.filters does not yet track the phase 27 command controller source.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashCommandController.h", "fileshash.vcxproj.filters does not yet track the phase 27 command controller header.");
        }, failures);

        Run("Phase 28 extracts the legacy desktop window-message flow into a dedicated controller", () =>
        {
            string messageControllerHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashMessageController.h");
            string messageController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashMessageController.cpp");
            string initializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string dlgHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.h");
            string dlgCpp = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string mfcProjectFilters = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj.filters");
            string dlgCppAndInitialization = dlgCpp + Environment.NewLine + initializationController;

            AssertContains(messageControllerHeader, "class FilesHashMessageController", "Phase 28 message controller header is missing the controller type.");
            AssertContains(messageControllerHeader, "BOOL HandlePaint(HICON icon) const;", "Phase 28 message controller is missing the paint seam.");
            AssertContains(messageControllerHeader, "HCURSOR GetDragCursor(HICON icon) const;", "Phase 28 message controller is missing the drag-cursor seam.");
            AssertContains(messageControllerHeader, "void HandleDropFiles(HDROP hDropInfo, LPCTSTR clearButtonText, LPCTSTR secondText, LPCTSTR noSelectionMessage) const;", "Phase 28 message controller is missing the drop-files seam.");
            AssertContains(messageControllerHeader, "BOOL HandleCopyData(const COPYDATASTRUCT* pCopyDataStruct, LPCTSTR clearButtonText, LPCTSTR secondText, LPCTSTR noSelectionMessage) const;", "Phase 28 message controller is missing the copy-data seam.");
            AssertContains(messageControllerHeader, "LRESULT HandleCustomMessage(WPARAM wParam) const;", "Phase 28 message controller is missing the custom-message seam.");
            AssertContains(messageControllerHeader, "void HandleInitMenuPopup(CMenu* pPopupMenu) const;", "Phase 28 message controller is missing the popup-menu seam.");
            AssertContains(messageControllerHeader, "void HandleCopyHash() const;", "Phase 28 message controller is missing the copy-hash seam.");
            AssertContains(messageControllerHeader, "void UpdateCopyHashMenuText(CCmdUI* pCmdUI, LPCTSTR copyText) const;", "Phase 28 message controller is missing the copy-menu-text seam.");

            AssertContains(messageController, "#include \"Common/ThreadDataExecutionAccess.h\"", "Phase 28 message controller does not yet consume the execution seam.");
            AssertContains(messageController, "m_parentWnd->IsIconic()", "Phase 28 message controller does not yet own iconic-paint gating.");
            AssertContains(messageController, "dc.DrawIcon(x, y, icon);", "Phase 28 message controller does not yet own icon rendering.");
            AssertContains(messageController, "m_parentWnd->DragAcceptFiles(FALSE);", "Phase 28 message controller does not yet own drop-target suspension during drag ingestion.");
            AssertContains(messageController, "m_hashInputController->LoadDroppedFiles(hDropInfo);", "Phase 28 message controller does not yet own drag-drop ingestion.");
            AssertContains(messageController, "m_parentWnd->SetForegroundWindow();", "Phase 28 message controller does not yet own foreground promotion for WM_COPYDATA.");
            AssertContains(messageController, "m_hashInputController->LoadCopyDataFiles(pCopyDataStruct)", "Phase 28 message controller does not yet own WM_COPYDATA ingestion.");
            AssertContains(messageController, "m_hashLifecycleController->StartHashing(clearButtonText, secondText, noSelectionMessage);", "Phase 28 message controller does not yet own non-button hash starts.");
            AssertContains(messageController, "m_hashResultViewController->ShowHyperEditMenu(m_parentWnd);", "Phase 28 message controller does not yet own HyperEdit popup display.");
            AssertContains(messageController, "m_hashResultViewController->UpdatePopupMenu(m_parentWnd, pPopupMenu);", "Phase 28 message controller does not yet own popup-menu updates.");
            AssertContains(messageController, "m_hashResultViewController->CopyLastHyperlink();", "Phase 28 message controller does not yet own hyperlink copying.");
            AssertContains(messageController, "m_hashResultViewController->UpdateCopyHashMenuText(pCmdUI, copyText);", "Phase 28 message controller does not yet own menu-text updates.");

            AssertContains(dlgHeader, "#include \"FilesHashMessageController.h\"", "FilesHashDlg.h does not yet consume the phase 28 message controller.");
            AssertContains(dlgHeader, "FilesHashMessageController m_hashMessageController;", "FilesHashDlg.h does not yet keep the phase 28 message controller.");

            AssertContains(dlgCppAndInitialization, "hashMessageController->Initialize(threadData, parentWnd, hashInputController, hashLifecycleController, hashResultViewController);", "FilesHashDlg.cpp does not yet initialize the phase 28 message controller.");
            AssertContains(dlgCpp, "if (m_hashMessageController.HandlePaint(m_hIcon))", "FilesHashDlg.cpp does not yet route icon paint through the phase 28 message controller.");
            AssertContains(dlgCpp, "return m_hashMessageController.GetDragCursor(m_hIcon);", "FilesHashDlg.cpp does not yet route drag-cursor queries through the phase 28 message controller.");
            AssertContains(dlgCpp, "m_hashMessageController.HandleDropFiles(hDropInfo, GetStringByKey(MAINDLG_CLEAR), GetStringByKey(SECOND_STRING), GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM));", "FilesHashDlg.cpp does not yet route WM_DROPFILES through the phase 28 message controller.");
            AssertContains(dlgCpp, "m_hashMessageController.HandleCopyData(pCopyDataStruct, GetStringByKey(MAINDLG_CLEAR), GetStringByKey(SECOND_STRING), GetStringByKey(MAINDLG_SELECT_HASH_ALGORITHM))", "FilesHashDlg.cpp does not yet route WM_COPYDATA through the phase 28 message controller.");
            AssertContains(dlgCpp, "return m_hashMessageController.HandleCustomMessage(wParam);", "FilesHashDlg.cpp does not yet route custom messages through the phase 28 message controller.");
            AssertContains(dlgCpp, "m_hashMessageController.HandleInitMenuPopup(pPopupMenu);", "FilesHashDlg.cpp does not yet route popup-menu updates through the phase 28 message controller.");
            AssertContains(dlgCpp, "m_hashMessageController.HandleCopyHash();", "FilesHashDlg.cpp does not yet route copy-hash commands through the phase 28 message controller.");
            AssertContains(dlgCpp, "m_hashMessageController.UpdateCopyHashMenuText(pCmdUI, GetStringByKey(MAINDLG_HYPEREDIT_MENU_COPY));", "FilesHashDlg.cpp does not yet route copy-menu text updates through the phase 28 message controller.");
            AssertDoesNotContain(dlgCpp, "SetForegroundWindow();", "FilesHashDlg.cpp still performs inline foreground promotion after phase 28.");
            AssertDoesNotContain(dlgCpp, "m_hashInputController.LoadDroppedFiles(hDropInfo);", "FilesHashDlg.cpp still performs inline drag-drop ingestion after phase 28.");
            AssertDoesNotContain(dlgCpp, "m_hashInputController.LoadCopyDataFiles(pCopyDataStruct)", "FilesHashDlg.cpp still performs inline WM_COPYDATA ingestion after phase 28.");
            AssertDoesNotContain(dlgCpp, "m_hashResultViewController.ShowHyperEditMenu(this);", "FilesHashDlg.cpp still performs inline HyperEdit popup display after phase 28.");
            AssertDoesNotContain(dlgCpp, "m_hashResultViewController.UpdatePopupMenu(this, pPopupMenu);", "FilesHashDlg.cpp still performs inline popup-menu updates after phase 28.");
            AssertDoesNotContain(dlgCpp, "m_hashResultViewController.CopyLastHyperlink();", "FilesHashDlg.cpp still performs inline hyperlink copying after phase 28.");

            AssertContains(mfcProject, "source\\WinMFC\\FilesHashMessageController.cpp", "fileshash.vcxproj does not yet compile the phase 28 message controller source.");
            AssertContains(mfcProject, "source\\WinMFC\\FilesHashMessageController.h", "fileshash.vcxproj does not yet include the phase 28 message controller header.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashMessageController.cpp", "fileshash.vcxproj.filters does not yet track the phase 28 message controller source.");
            AssertContains(mfcProjectFilters, "source\\WinMFC\\FilesHashMessageController.h", "fileshash.vcxproj.filters does not yet track the phase 28 message controller header.");
        }, failures);

        Run("Phase 29 splits digest access into dedicated metadata, state, and value seams", () =>
        {
            string digestAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h");
            string digestMetadataAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestMetadataAccess.h");
            string digestStateAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestStateAccess.h");
            string digestValueAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestValueAccess.h");
            string digestRender = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestRender.h");
            string resultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataAccess.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");

            AssertContains(digestAccess, "#include \"Common/ResultDigestMetadataAccess.h\"", "Phase 29 compatibility ResultDigestAccess shim does not yet layer on top of the metadata seam.");
            AssertContains(digestAccess, "#include \"Common/ResultDigestStateAccess.h\"", "Phase 29 compatibility ResultDigestAccess shim does not yet layer on top of the state seam.");
            AssertContains(digestAccess, "#include \"Common/ResultDigestValueAccess.h\"", "Phase 29 compatibility ResultDigestAccess shim does not yet layer on top of the value seam.");
            AssertDoesNotContain(digestAccess, "GetResultDigestState(const ResultData& result)", "Phase 29 compatibility ResultDigestAccess shim still owns digest-state helpers after the seam split.");
            AssertDoesNotContain(digestAccess, "GetResultDigest(const ResultData& result, ResultDigestType digestType)", "Phase 29 compatibility ResultDigestAccess shim still owns digest-value helpers after the seam split.");

            AssertContains(digestMetadataAccess, "typedef HashAlgorithmDescriptor ResultDigestMetadata;", "Phase 29 metadata seam does not yet bridge digest metadata onto the hash-algorithm descriptor.");
            AssertContains(digestMetadataAccess, "GetResultDigestCount()", "Phase 29 metadata seam does not yet own digest-count reads.");
            AssertContains(digestMetadataAccess, "GetResultDigestMetadataAt(int index)", "Phase 29 metadata seam does not yet own metadata lookup.");
            AssertContains(digestMetadataAccess, "VisitResultDigestMetadata(TResultDigestMetadataVisitor visitor)", "Phase 29 metadata seam does not yet own metadata iteration.");
            AssertContains(digestMetadataAccess, "VisitResultDigests(TResultDigestVisitor visitor)", "Phase 29 metadata seam does not yet own digest-type iteration.");

            AssertContains(digestStateAccess, "GetDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)", "Phase 29 state seam does not yet own reusable digest-storage reads.");
            AssertContains(digestStateAccess, "GetResultDigestState(const ResultData& result)", "Phase 29 state seam does not yet own digest-state reads.");
            AssertContains(digestStateAccess, "GetResultDigestStorage(const ResultData& result)", "Phase 29 state seam does not yet own digest-storage reads.");
            AssertDoesNotContain(digestStateAccess, "GetResultDigestCompatibilityFields(const ResultData& result)", "Phase 46 core digest state seam still exposes legacy compatibility-field reads.");
            AssertDoesNotContain(digestStateAccess, "GetCompatibilityResultDigest(const ResultData& result, ResultDigestType digestType)", "Phase 46 core digest state seam still exposes legacy compatibility-value reads.");

            AssertContains(digestValueAccess, "GetResultDigest(const ResultData& result, ResultDigestType digestType)", "Phase 29 value seam does not yet own digest reads.");
            AssertContains(digestValueAccess, "VisitResultDigestMetadataValues(const ResultData& result, TResultDigestMetadataValueVisitor visitor)", "Phase 29 value seam does not yet own metadata-value iteration.");
            AssertContains(digestValueAccess, "HasAnyResultDigests(const ResultData& result)", "Phase 29 value seam does not yet own aggregate digest presence checks.");
            AssertContains(digestValueAccess, "SetResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)", "Phase 29 value seam does not yet own digest writes.");
            AssertContains(digestValueAccess, "ResetResultDigests(ResultData& result)", "Phase 29 value seam does not yet own digest resets.");

            AssertContains(resultAccess, "#include \"Common/ResultDigestValueAccess.h\"", "ResultDataAccess does not yet consume the phase 29 digest value seam.");
            AssertDoesNotContain(resultAccess, "#include \"Common/ResultDigestAccess.h\"", "ResultDataAccess still depends on the umbrella ResultDigestAccess header after phase 29.");
            AssertContains(digestRender, "#include \"Common/ResultDigestValueAccess.h\"", "ResultDigestRender does not yet consume the phase 29 digest value seam.");
            AssertDoesNotContain(digestRender, "#include \"Common/ResultDigestAccess.h\"", "ResultDigestRender still depends on the umbrella ResultDigestAccess header after phase 29.");
            AssertContains(hashEngineInternal, "#include \"Common/ResultDigestStateAccess.h\"", "HashEngineInternal.h does not yet consume the phase 29 digest state seam.");
            AssertDoesNotContain(hashEngineInternal, "#include \"Common/ResultDigestAccess.h\"", "HashEngineInternal.h still depends on the umbrella ResultDigestAccess header after phase 29.");
        }, failures);

        Run("Phase 30 introduces an independent xUnit unit-test framework and runs it in CI", () =>
        {
            string unitTestProject = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\FHash.UnitTests.csproj");
            string unitTestContext = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\RepositoryTestContext.cs");
            string unitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\CommonSeamUnitTests.cs");
            string workflow = ReadRepoFile(repoRoot, @".github\workflows\windows-build.yml");

            AssertContains(unitTestProject, "<PackageReference Include=\"Microsoft.NET.Test.Sdk\"", "Phase 30 unit test project is missing Microsoft.NET.Test.Sdk.");
            AssertContains(unitTestProject, "<PackageReference Include=\"xunit\"", "Phase 30 unit test project is missing xUnit.");
            AssertContains(unitTestProject, "<PackageReference Include=\"xunit.runner.visualstudio\"", "Phase 30 unit test project is missing the xUnit VS runner.");
            AssertContains(unitTests, "[Fact]", "Phase 30 unit test project does not yet contain xUnit facts.");
            AssertContains(unitTests, "HashAlgorithmRegistry_DefinesStableCompatibilityOrder", "Phase 30 unit tests do not yet cover the hash-algorithm registry seam.");
            AssertContains(unitTests, "ResultDigestAccess_UmbrellaShimDependsOnDedicatedSeams", "Phase 30 unit tests do not yet cover the digest umbrella seam.");
            AssertContains(unitTests, "Workflow_RunsIndependentUnitTests_AndGatesNativeBuilds", "Phase 30 unit tests do not yet cover CI gating for the new unit framework.");
            AssertContains(unitTestContext, "FindRepoRoot()", "Phase 30 unit test helper is missing repository-root discovery.");
            AssertContains(workflow, "unit-tests:", "Workflow does not yet declare the phase 30 unit-tests job.");
            AssertContains(workflow, "dotnet restore unit-tests/FHash.UnitTests/FHash.UnitTests.csproj", "Workflow does not yet restore the phase 30 unit test project.");
            AssertContains(workflow, "dotnet test unit-tests/FHash.UnitTests/FHash.UnitTests.csproj --configuration Release --no-restore", "Workflow does not yet run the phase 30 unit test project.");
            AssertInOrder(
                workflow,
                [
                    "build-legacy-x64:",
                    "needs:",
                    "- security-regression",
                    "- unit-tests"
                ],
                "Workflow does not yet gate the legacy native build on both regression and unit-test jobs.");
        }, failures);

        Run("Phase 31 introduces stable hash request, result, and progress event contracts", () =>
        {
            string hashRequest = ReadRepoFile(repoRoot, @"trunk\source\Common\HashRequest.h");
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string hashResult = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResult.h");
            string progressEvent = ReadRepoFile(repoRoot, @"trunk\source\Common\ProgressEvent.h");
            string hashProgressSink = ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressSink.h");
            string hashEngineObserver = ReadRepoFile(repoRoot, @"trunk\source\Adapters\UiBridge\HashEngineObserver.h");
            string hashEngine = ReadHashEngineImplementation(repoRoot);

            AssertContains(hashRequest, "struct HashRequest", "Phase 31 does not yet define a stable HashRequest contract.");
            AssertContains(hashRequest, "TStrVector files;", "Phase 31 HashRequest does not yet own file inputs.");
            AssertContains(hashRequest, "std::vector<ResultDigestType> algorithms;", "Phase 31 HashRequest does not yet own algorithm selection.");
            AssertContains(hashRequest, "bool uppercaseDigest;", "Phase 31 HashRequest does not yet own uppercase output preference.");
            AssertContains(hashRequest, "CreateHashRequest(const ThreadData& threadData)", "Phase 31 does not yet project ThreadData into HashRequest.");
            AssertContains(hashRequest, "VisitHashRequestFiles(const HashRequest& request", "Phase 31 HashRequest does not yet own file iteration.");
            AssertContains(hashRequest, "VisitHashRequestAlgorithms(const HashRequest& request", "Phase 31 HashRequest does not yet own algorithm iteration.");

            AssertContains(global, "struct HashResult", "Phase 31 does not yet define a stable HashResult contract.");
            AssertDoesNotContain(hashResult, "const ResultData *sourceResult;", "Phase 42 HashResult still keeps the legacy compatibility link back to ResultData.");
            AssertContains(global, "std::vector<HashDigestResult> digests;", "Phase 31 HashResult does not yet own a digest collection.");
            AssertContains(hashResult, "ProjectHashResult(const ResultData& result)", "Phase 31 does not yet project ResultData into HashResult.");

            AssertContains(progressEvent, "enum ProgressEventType", "Phase 31 does not yet define a stable ProgressEventType surface.");
            AssertContains(progressEvent, "struct ProgressEvent", "Phase 31 does not yet define a stable ProgressEvent contract.");
            AssertDoesNotContain(progressEvent, "CreateFileHashReadyProgressEvent(const ResultData& result, bool uppercaseDigest)", "Phase 42 still keeps the legacy ResultData-based hash-ready progress event overload.");
            AssertContains(progressEvent, "CreateFileHashReadyProgressEvent(const HashResult& result, bool uppercaseDigest)", "Phase 35 does not yet expose hash-ready progress events directly from HashResult.");

            AssertContains(hashProgressSink, "class HashProgressSink", "Phase 31 does not yet define a neutral hash progress sink contract.");
            AssertContains(hashProgressSink, "virtual int progressMax() = 0;", "Phase 31 hash progress sink does not yet own progress max queries.");
            AssertContains(hashProgressSink, "virtual void onProgressEvent(const ProgressEvent& progressEvent) = 0;", "Phase 31 hash progress sink does not yet own semantic progress-event dispatch.");
            AssertContains(hashEngineObserver, "class HashEngineObserver: public HashProgressSink", "HashEngineObserver does not yet layer on top of the phase 31 hash progress sink.");
            AssertContains(hashEngineObserver, "virtual void onProgressEvent(const ProgressEvent& progressEvent)", "HashEngineObserver does not yet expose the phase 31 progress-event compatibility entry point.");
            AssertContains(hashEngineObserver, "onFileResultEvent(progressEvent.result, progressEvent.type, progressEvent.uppercaseDigest);", "HashEngineObserver does not yet route result progress events through HashResult.");
            AssertContains(hashEngineObserver, "onTotalProgressValue(progressEvent.value);", "HashEngineObserver does not yet bridge total-progress events through the neutral adapter callback.");

            AssertContains(hashEngine, "HashRequest request = CreateHashRequest(*thrdData);", "HashEngine does not yet start from the phase 31 HashRequest contract.");
            AssertContains(hashEngine, "VisitHashRequestFiles(request", "HashEngine does not yet iterate files through HashRequest.");
            AssertContains(hashEngine, "HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA256)", "HashEngine does not yet read selected algorithms through HashRequest.");
            AssertContains(hashEngine, "observer->onProgressEvent(CreatePreparingProgressEvent());", "HashEngine preparation does not yet emit semantic progress events.");
            AssertContains(hashEngine, "observer->onProgressEvent(CreateFileStartedProgressEvent(result));", "Phase 35 HashEngine does not yet emit file-started progress events through HashResult.");
            AssertContains(hashEngine, "observer->onProgressEvent(CreateFileHashReadyProgressEvent(result, uppercase));", "Phase 35 HashEngine does not yet emit hash-ready progress events through HashResult.");
            AssertContains(hashEngine, "observer->onProgressEvent(CreateCancelledProgressEvent());", "HashEngine does not yet emit cancellation progress events.");
            AssertContains(hashEngine, "observer->onProgressEvent(CreateCompletedProgressEvent());", "HashEngine does not yet emit completion progress events.");
        }, failures);

        Run("Phase 32 routes the WinUI native stack through fHashNativeCore instead of recompiling the core", () =>
        {
            string winUiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");
            string clrBridgeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\fHashClrBridge.vcxproj");
            string workflow = ReadRepoFile(repoRoot, @".github\workflows\windows-build.yml");

            AssertDoesNotContain(winUiNativeProject, @"..\..\trunk\source\Algorithms\MD5.cpp", "Phase 32 WinUI native project still recompiles MD5 instead of consuming fHashNativeCore.");
            AssertDoesNotContain(winUiNativeProject, @"..\..\trunk\source\Algorithms\SHA1.cpp", "Phase 32 WinUI native project still recompiles SHA1 instead of consuming fHashNativeCore.");
            AssertDoesNotContain(winUiNativeProject, @"..\..\trunk\source\Algorithms\sha256.cpp", "Phase 32 WinUI native project still recompiles SHA256 instead of consuming fHashNativeCore.");
            AssertDoesNotContain(winUiNativeProject, @"..\..\trunk\source\Algorithms\sha512.cpp", "Phase 32 WinUI native project still recompiles SHA512 instead of consuming fHashNativeCore.");
            AssertDoesNotContain(winUiNativeProject, @"..\..\trunk\source\Common\HashEngine.cpp", "Phase 32 WinUI native project still recompiles HashEngine instead of consuming fHashNativeCore.");
            AssertDoesNotContain(winUiNativeProject, @"..\..\trunk\source\Common\HashEnginePreparation.cpp", "Phase 32 WinUI native project still recompiles HashEnginePreparation instead of consuming fHashNativeCore.");
            AssertDoesNotContain(winUiNativeProject, @"..\..\trunk\source\Common\HashEngineResult.cpp", "Phase 32 WinUI native project still recompiles HashEngineResult instead of consuming fHashNativeCore.");
            AssertDoesNotContain(winUiNativeProject, @"..\..\trunk\source\Common\strhelper.cpp", "Phase 32 WinUI native project still recompiles strhelper instead of consuming fHashNativeCore.");
            AssertDoesNotContain(winUiNativeProject, @"..\..\trunk\source\OsUtils\OsFileWinApi.cpp", "Phase 32 WinUI native project still recompiles OsFileWinApi instead of consuming fHashNativeCore.");
            AssertDoesNotContain(winUiNativeProject, @"..\..\trunk\source\OsUtils\OsThreadWinApi.cpp", "Phase 32 WinUI native project still recompiles OsThreadWinApi instead of consuming fHashNativeCore.");
            AssertDoesNotContain(winUiNativeProject, @"..\..\trunk\source\WinCommon\WindowsComm.cpp", "Phase 32 WinUI native project still recompiles WindowsComm instead of consuming fHashNativeCore.");

            AssertContains(winUiNativeProject, @"..\..\trunk\source\WinCommon\AdvTaskbar.cpp", "Phase 32 WinUI native project no longer keeps its platform-specific AdvTaskbar layer.");
            AssertContains(winUiNativeProject, @"..\..\trunk\source\WinCommon\ClipboardHelper.cpp", "Phase 32 WinUI native project no longer keeps its platform-specific ClipboardHelper layer.");
            AssertContains(winUiNativeProject, @"..\..\trunk\source\WinCommon\FileVersionHelper.cpp", "Phase 32 WinUI native project no longer keeps its platform-specific FileVersionHelper layer.");

            AssertContains(clrBridgeProject, "fHashWUINative.lib;fHashNativeCore.lib;Version.lib;%(AdditionalDependencies)", "Phase 32 CLR bridge does not yet link both the WinUI platform layer and fHashNativeCore.");
            AssertContains(clrBridgeProject, @"$(ProjectDir)..\fHashNativeCore\$(Platform)\$(Configuration)\fHashNativeCore-md\", "Phase 32 CLR bridge does not yet search the CLR-compatible fHashNativeCore-md output directory.");
            AssertContains(clrBridgeProject, @"$(ProjectDir)..\fHashNativeCore\$(Platform)\$(Configuration)\fHashNativeCore\", "Phase 32 CLR bridge does not yet search the fHashNativeCore output directory.");

            AssertInOrder(
                workflow,
                [
                    "build-winui-bridge-x64:",
                    "& msbuild sub-proj/fHashNativeCore/fHashNativeCore.vcxproj /m /p:Configuration=Release /p:Platform=x64 /p:PlatformToolset=v143 /p:FHashDynamicRuntime=true",
                    "& msbuild sub-proj/fHashWUINative/fHashWUINative.vcxproj",
                    "& msbuild sub-proj/fHashClrBridge/fHashClrBridge.vcxproj /restore"
                ],
                "Phase 32 workflow does not yet build fHashNativeCore before the WinUI native layer and CLR bridge.");
        }, failures);

        Run("Phase 33 routes the core hashing entry through RunHashRequest", () =>
        {
            string hashExecutionContext = ReadRepoFile(repoRoot, @"trunk\source\Common\HashExecutionContext.h");
            string hashEngineHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.h");
            string hashEngine = ReadHashEngineImplementation(repoRoot);

            AssertContains(hashExecutionContext, "struct HashExecutionContext", "Phase 35 hash execution context header is missing the execution-context contract.");
            AssertContains(hashExecutionContext, "CreateHashExecutionContext(ThreadData& threadData)", "Phase 35 hash execution context does not yet project ThreadData into the execution context.");
            AssertContains(hashEngineHeader, "struct HashExecutionContext;", "Phase 33 HashEngine header does not yet forward declare HashExecutionContext.");
            AssertContains(hashEngineHeader, "int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request);", "Phase 33 HashEngine header does not yet expose the execution-context request entry.");
            AssertContains(hashEngine, "int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request)", "Phase 33 HashEngine implementation does not yet define the execution-context request entry.");
            AssertContains(hashEngine, "HashExecutionContext executionContext = CreateHashExecutionContext(*thrdData);", "Phase 35 HashThreadFunc does not yet project ThreadData into HashExecutionContext before dispatch.");
            AssertContains(hashEngine, "return RunHashRequest(&executionContext, request);", "Phase 33 HashThreadFunc does not yet delegate into RunHashRequest through HashExecutionContext.");
            AssertContains(hashEngine, "HashRequest request = CreateHashRequest(*thrdData);", "Phase 33 HashThreadFunc no longer projects ThreadData into HashRequest before dispatch.");
        }, failures);

        Run("Phase 34 narrows core hashing execution onto HashProgressSink while keeping HashEngineObserver as a compatibility adapter", () =>
        {
            string hashProgressSink = ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressSink.h");
            string hashEngineObserver = ReadRepoFile(repoRoot, @"trunk\source\Adapters\UiBridge\HashEngineObserver.h");
            string hashExecutionContext = ReadRepoFile(repoRoot, @"trunk\source\Common\HashExecutionContext.h");
            string hashEngineHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string hashEnginePreparation = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp");
            string hashEngineResult = string.Join("\r\n",
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp"));
            string hashEngine = ReadHashEngineImplementation(repoRoot);

            AssertContains(hashProgressSink, "class HashProgressSink", "Phase 34 hash progress sink header is missing the neutral sink seam.");
            AssertContains(hashProgressSink, "virtual int progressMax() = 0;", "Phase 34 hash progress sink does not yet own progress-max queries.");
            AssertContains(hashProgressSink, "virtual void onProgressEvent(const ProgressEvent& progressEvent) = 0;", "Phase 34 hash progress sink does not yet own semantic event dispatch.");

            AssertContains(hashEngineObserver, "#include \"Common/HashProgressSink.h\"", "Phase 34 HashEngineObserver does not yet layer on top of HashProgressSink.");
            AssertContains(hashEngineObserver, "class HashEngineObserver: public HashProgressSink", "Phase 34 HashEngineObserver is not yet narrowed into a compatibility adapter on top of HashProgressSink.");
            AssertContains(hashEngineObserver, "virtual int progressMax()", "Phase 34 HashEngineObserver no longer satisfies the progress-max sink contract.");
            AssertContains(hashEngineObserver, "virtual void onProgressEvent(const ProgressEvent& progressEvent)", "Phase 34 HashEngineObserver no longer satisfies the semantic progress-event sink contract.");
            AssertContains(hashExecutionContext, "HashProgressSink *progressSink;", "Phase 35 hash execution context does not yet carry the neutral progress sink.");
            AssertContains(hashExecutionContext, "HashJobState *jobState;", "Phase 35 hash execution context does not yet carry the grouped job-state seam.");
            AssertContains(hashExecutionContext, "HashCancellationState *cancellationState;", "Phase 35 hash execution context does not yet carry the grouped cancellation seam.");
            AssertContains(hashExecutionContext, "GetHashExecutionProgressSink(const HashExecutionContext& executionContext)", "Phase 35 hash execution context does not yet expose progress-sink reads.");
            AssertContains(hashExecutionContext, "ShouldStopHashExecution(const HashExecutionContext& executionContext)", "Phase 35 hash execution context does not yet expose cancellation reads.");
            AssertContains(hashExecutionContext, "AppendHashExecutionResult(HashExecutionContext& executionContext)", "Phase 35 hash execution context does not yet expose result publication.");

            AssertDoesNotContain(hashEngineHeader, "class HashProgressSink;", "Phase 35 HashEngine header still forward declares HashProgressSink after switching to HashExecutionContext.");
            AssertDoesNotContain(hashEngineHeader, "class HashEngineObserver;", "Phase 34 HashEngine header still directly depends on HashEngineObserver.");
            AssertDoesNotContain(hashEngineHeader, "HashProgressSink *observer", "Phase 35 HashEngine header still routes the core entry through the older progress-sink argument.");

            AssertContains(hashEngineInternal, "#include \"Common/HashProgressSink.h\"", "Phase 34 HashEngineInternal does not yet consume HashProgressSink.");
            AssertContains(hashEngineInternal, "#include \"Common/HashExecutionContext.h\"", "Phase 35 HashEngineInternal does not yet consume HashExecutionContext.");
            AssertDoesNotContain(hashEngineInternal, "#include \"Common/HashEngineObserver.h\"", "Phase 34 HashEngineInternal still directly depends on HashEngineObserver.");
            AssertContains(hashEngineInternal, "HashExecutionContext *executionContext", "Phase 35 HashEngineInternal does not yet route execution seams through HashExecutionContext.");

            AssertContains(hashEnginePreparation, "PrepareHashingWork(HashExecutionContext *executionContext, const HashRequest& request", "Phase 35 preparation seam does not yet consume HashExecutionContext.");
            AssertContains(hashEnginePreparation, "EmitPathResult(HashExecutionContext *executionContext, HashResult& result)", "Phase 35 preparation seam does not yet publish file-start events through HashExecutionContext.");
            AssertContains(hashEnginePreparation, "AppendHashExecutionResult(*executionContext)", "Phase 35 preparation seam does not yet publish results through HashExecutionContext.");

            AssertContains(hashEngineResult, "PrepareFileMetaResult(HashExecutionContext *executionContext, HashResult& result", "Phase 35 result seam does not yet consume HashExecutionContext.");
            AssertContains(hashEngineResult, "EmitHashResult(HashExecutionContext *executionContext, HashResult& result, bool uppercase)", "Phase 35 result seam does not yet publish hash results through HashExecutionContext.");
            AssertContains(hashEngineResult, "ReplaceHashExecutionCountedFileSize(*executionContext, fSizes[fileIndex], fsize);", "Phase 35 result seam does not yet route counted-size replacement through HashExecutionContext.");

            AssertContains(hashEngine, "HashExecutionContext *executionContext, uint64_t fsize", "Phase 35 engine progress updates do not yet route through HashExecutionContext.");
            AssertContains(hashEngine, "int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request)", "Phase 35 HashEngine does not yet narrow the request-driven core entry onto HashExecutionContext.");
            AssertContains(hashEngine, "ShouldStopHashExecution(*executionContext)", "Phase 35 HashEngine does not yet read stop state through HashExecutionContext.");
            AssertContains(hashEngine, "ResetHashExecutionTotalSize(*executionContext);", "Phase 35 HashEngine does not yet reset counted size through HashExecutionContext.");
        }, failures);

        Run("Phase 35 routes semantic result events through HashResult while narrowing adapter compatibility to event-oriented callbacks", () =>
        {
            string progressEvent = ReadRepoFile(repoRoot, @"trunk\source\Common\ProgressEvent.h");
            string hashEngineObserver = ReadRepoFile(repoRoot, @"trunk\source\Adapters\UiBridge\HashEngineObserver.h");
            string hashEnginePreparation = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp");
            string hashEngineResult = string.Join("\r\n",
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp"));

            AssertContains(progressEvent, "CreateResultProgressEvent(ProgressEventType eventType, const HashResult& result)", "Phase 35 does not yet expose ProgressEvent creation directly from HashResult.");
            AssertContains(progressEvent, "CreateFileStartedProgressEvent(const HashResult& result)", "Phase 35 does not yet expose file-started events directly from HashResult.");
            AssertContains(progressEvent, "CreateFileMetaReadyProgressEvent(const HashResult& result)", "Phase 35 does not yet expose file-meta events directly from HashResult.");
            AssertContains(progressEvent, "CreateFileHashReadyProgressEvent(const HashResult& result, bool uppercaseDigest)", "Phase 35 does not yet expose file-hash events directly from HashResult.");
            AssertContains(progressEvent, "CreateFileFailedProgressEvent(const HashResult& result)", "Phase 35 does not yet expose file-failed events directly from HashResult.");

            AssertContains(hashEngineObserver, "virtual void onFileResultEvent(const HashResult& result,", "Phase 35 HashEngineObserver does not yet expose a HashResult-based file-result event contract.");
            AssertContains(hashEngineObserver, "onFileResultEvent(progressEvent.result, progressEvent.type, progressEvent.uppercaseDigest);", "Phase 35 HashEngineObserver does not yet dispatch file-result events through HashResult.");
            AssertDoesNotContain(hashEngineObserver, "showFileHash(*progressEvent.result.sourceResult, progressEvent.uppercaseDigest);", "Phase 35 HashEngineObserver still depends on progressEvent.result.sourceResult for hash-ready dispatch.");
            AssertDoesNotContain(hashEngineObserver, "CreateCompatibilityResultData(result);", "Phase 35 HashEngineObserver still rebuilds ResultData instead of staying as a thin HashResult wrapper.");

            AssertContains(hashEnginePreparation, "CreateFileStartedProgressEvent(result)", "Phase 35 preparation seam does not yet emit file-start events through HashResult.");
            AssertContains(hashEngineResult, "CreateFileMetaReadyProgressEvent(result)", "Phase 35 result seam does not yet emit file-meta events through HashResult.");
            AssertContains(hashEngineResult, "CreateFileHashReadyProgressEvent(result, uppercase)", "Phase 35 result seam does not yet emit file-hash events through HashResult.");
            AssertContains(hashEngineResult, "CreateFileFailedProgressEvent(result)", "Phase 35 result seam does not yet emit file-failed events through HashResult.");
        }, failures);

        Run("Phase 36 makes HashResult the bridge-facing result contract across MFC and managed adapters", () =>
        {
            string observer = ReadRepoFile(repoRoot, @"trunk\source\Adapters\UiBridge\HashEngineObserver.h");
            string managedDispatch = ReadRepoFile(repoRoot, @"trunk\source\Common\ManagedBridgeDispatch.h");
            string hashResultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultProjection.h");
            string bridgeMfcHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.h");
            string bridgeMfcSource = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");
            string bridgeWuiHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.h");
            string bridgeWuiSource = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.h");
            string bridgeUwpSource = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(observer, "virtual void onFileResultEvent(const HashResult& result,", "Phase 36 observer seam does not yet expose a HashResult-based file-result event contract.");
            AssertContains(observer, "virtual int queryProgressMax() = 0;", "Phase 36 observer seam does not yet expose a neutral progress query contract.");

            AssertContains(hashResultProjection, "AssignHashResultCoreToNet", "Phase 36 does not yet project HashResult core fields directly to managed result DTOs.");
            AssertContains(hashResultProjection, "AssignHashResultDigestsToNet", "Phase 36 does not yet project HashResult digest fields directly to managed result DTOs.");
            AssertContains(hashResultProjection, "ProjectHashResultToNet(const HashResult& result, TStringConverter convertString)", "Phase 36 does not yet expose direct HashResult-to-managed projection.");

            AssertContains(managedDispatch, "#include \"Common/HashResultProjection.h\"", "Phase 36 managed bridge dispatch does not yet depend on HashResultProjection.");
            AssertContains(managedDispatch, "DispatchManagedBridgeResultByType(const HashResult& result", "Phase 36 managed bridge dispatch does not yet expose HashResult-based projection dispatch.");
            AssertContains(managedDispatch, "ProjectHashResultToNet<TResultDataNet, TResultStateNet>(result, convertString)", "Phase 36 managed bridge dispatch does not yet project HashResult directly.");

            AssertContains(bridgeMfcHeader, "virtual void onFileResultEvent(const HashResult& result,", "Phase 36 MFC bridge header does not yet accept HashResult file-result consumption.");

            AssertContains(bridgeWuiHeader, "virtual void onFileResultEvent(const HashResult& result,", "Phase 36 WinUI bridge header does not yet accept HashResult file-result consumption.");
            AssertContains(bridgeWuiHeader, "DispatchProjectedResultToDelegate(const HashResult& result", "Phase 36 WinUI bridge helper does not yet narrow to HashResult.");
            AssertContains(bridgeWuiSource, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase", "Phase 36 WinUI bridge does not yet project HashResult directly to managed delegates.");
            AssertContains(bridgeWuiSource, "GetManagedResultDispatchType(eventType)", "Phase 36 WinUI bridge does not yet route ProgressEventType through a neutral managed dispatch selector.");

            AssertContains(bridgeUwpHeader, "virtual void onFileResultEvent(const HashResult& result,", "Phase 36 UWP bridge header does not yet accept HashResult file-result consumption.");
            AssertContains(bridgeUwpHeader, "DispatchProjectedResultToDelegate(const HashResult& result", "Phase 36 UWP bridge helper does not yet narrow to HashResult.");
            AssertContains(bridgeUwpSource, "DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase", "Phase 36 UWP bridge does not yet project HashResult directly to managed delegates.");
            AssertContains(bridgeUwpSource, "GetManagedResultDispatchType(eventType)", "Phase 36 UWP bridge does not yet route ProgressEventType through a neutral managed dispatch selector.");
        }, failures);

        Run("Phase 37 routes managed digest-search projection through HashResult search and projection seams", () =>
        {
            string hashResultSearch = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultSearch.h");
            string hashResultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultProjection.h");
            string managedHashMgmtAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ManagedHashMgmtAccess.h");
            string hashMgmtClr = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string hashMgmtUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");

            AssertContains(hashResultSearch, "HashResultContainsDigest(const HashResult& result, const sunjwbase::tstring& digestText)", "Phase 37 does not yet expose HashResult digest-search matching.");
            AssertContains(hashResultSearch, "HashResultMatchesDigestText(const HashResult& result, const sunjwbase::tstring& digestText)", "Phase 37 does not yet expose HashResult digest-search predicate matching.");

            AssertContains(hashResultProjection, "#include \"Common/HashResultSearch.h\"", "Phase 37 HashResult projection seam does not yet layer on top of HashResultSearch.");
            AssertContains(hashResultProjection, "VisitProjectedHashResults(const HashResultList& resultList, TStringConverter convertString, TResultVisitor visitor)", "Phase 37 does not yet expose whole-list HashResult projection.");
            AssertContains(hashResultSearch, "CountMatchingHashResults(const HashResultList& resultList, THashResultPredicate predicate)", "Phase 45 does not yet expose HashResult match counting through the shared search seam.");
            AssertContains(hashResultProjection, "VisitProjectedMatchingHashResults(const HashResultList& resultList, THashResultPredicate predicate, TStringConverter convertString, TResultVisitor visitor)", "Phase 37 does not yet expose HashResult matching projection traversal.");
            AssertContains(hashResultProjection, "CreateProjectedMatchingHashResults(const HashResultList& resultList, THashResultPredicate predicate, TResultArrayFactory createResultArray, TStringConverter convertString, TResultArraySetter setProjectedResult)", "Phase 37 does not yet expose HashResult-based materialized projection.");
            AssertContains(hashResultProjection, "CreateProjectedDigestMatchingHashResults(const HashResultList& resultList, const sunjwbase::tstring& digestText, TResultArrayFactory createResultArray, TStringConverter convertString, TResultArraySetter setProjectedResult)", "Phase 37 does not yet expose HashResult-based digest-search materialization.");
            AssertContains(hashResultProjection, "VisitHashResults(resultList, [&](const HashResult& hashResult)", "Phase 45 HashResult projection seam does not yet reuse shared HashResult whole-list traversal.");
            AssertContains(hashResultProjection, "ProjectHashResultToNet<TResultDataNet, TResultStateNet>(hashResult, convertString)", "Phase 37 HashResult projection seam does not yet route materialized net projection through ProjectHashResultToNet.");

            AssertContains(managedHashMgmtAccess, "#include \"Common/HashResultProjection.h\"", "Phase 37 managed hash-management seam does not yet consume HashResultProjection.");
            AssertContains(managedHashMgmtAccess, "CreateProjectedDigestMatchingHashResults<THashResultNet, THashResultStateNet, TResultArray>(", "Phase 37 managed hash-management seam does not yet route digest-search projection through HashResultProjection.");
            AssertDoesNotContain(managedHashMgmtAccess, "#include \"Common/ResultDataProjection.h\"", "Phase 37 managed hash-management seam still depends directly on ResultDataProjection.");
        }, failures);

        Run("Phase 38 promotes managed query results onto HashResultNet as the primary managed query contract", () =>
        {
            string managedHashMgmtAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ManagedHashMgmtAccess.h");
            string clrHashResultNet = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashResultNet.h");
            string clrHashMgmtHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.h");
            string clrHashMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpHashResultNet = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashResultNet.h");
            string uwpHashMgmtHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.h");
            string uwpHashMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");
            string winUiPage = ReadRepoFile(repoRoot, @"trunk\source\WinUI\MainPage.xaml.cs");
            string winUwpPage = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\MainPage.xaml.cs");

            AssertContains(managedHashMgmtAccess, "static inline TResultArray CreateProjectedManagedDigestMatchingHashResults(", "Phase 38 managed hash-management seam does not yet expose HashResultNet-based digest search projection.");

            AssertContains(clrHashResultNet, "public enum class HashResultStateNet", "Phase 38 CLR bridge does not yet define HashResultStateNet.");
            AssertContains(clrHashResultNet, "public value struct HashResultNet", "Phase 38 CLR bridge does not yet define HashResultNet.");
            AssertContains(clrHashMgmtHeader, "cli::array<HashResultNet>^ FindHashResults(System::String^ sstrHashToFind);", "Phase 38 CLR HashMgmt does not yet expose FindHashResults.");
            AssertContains(clrHashMgmt, "CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, cli::array<HashResultNet>^>(", "Phase 38 CLR HashMgmt does not yet route queries through HashResultNet projection.");
            AssertDoesNotContain(clrHashMgmtHeader, "FindResult(System::String^ sstrHashToFind)", "Phase 38 CLR HashMgmt still exposes the legacy ResultDataNet find API.");
            AssertDoesNotContain(clrHashMgmt, "CreateCompatibilityResultDataNetArray(", "Phase 38 CLR HashMgmt still keeps the legacy ResultDataNet compatibility array helper.");

            AssertContains(uwpHashResultNet, "public enum class HashResultStateNet", "Phase 38 UWP bridge does not yet define HashResultStateNet.");
            AssertContains(uwpHashResultNet, "public value struct HashResultNet", "Phase 38 UWP bridge does not yet define HashResultNet.");
            AssertContains(uwpHashMgmtHeader, "Platform::Array<HashResultNet>^ FindHashResults(Platform::String^ pstrHashToFind);", "Phase 38 UWP HashMgmt does not yet expose FindHashResults.");
            AssertContains(uwpHashMgmt, "CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, Array<HashResultNet>^>(", "Phase 38 UWP HashMgmt does not yet route queries through HashResultNet projection.");
            AssertDoesNotContain(uwpHashMgmtHeader, "FindResult(Platform::String^ pstrHashToFind)", "Phase 38 UWP HashMgmt still exposes the legacy ResultDataNet find API.");
            AssertDoesNotContain(uwpHashMgmt, "CreateCompatibilityResultDataNetArray(", "Phase 38 UWP HashMgmt still keeps the legacy ResultDataNet compatibility array helper.");

            AssertContains(winUiPage, "HashResultNet[] hashResultNetArray = m_mainWindow.HashMgmt.FindHashResults(strHashToFind);", "Phase 38 WinUI page does not yet consume FindHashResults.");
            AssertContains(winUiPage, "private void ShowFindResult(string strHashToFind, HashResultNet[] hashResultNetArray)", "Phase 38 WinUI page does not yet switch the find-result surface to HashResultNet.");
            AssertContains(winUiPage, "AppendFileResultToTextMain(hashResult, m_uppercaseChecked);", "Phase 38 WinUI page does not yet render HashResultNet query results directly.");

            AssertContains(winUwpPage, "HashResultNet[] hashResultNetArray = m_hashMgmt.FindHashResults(strHashToFind);", "Phase 38 UWP page does not yet consume FindHashResults.");
            AssertContains(winUwpPage, "private void ShowFindResult(string strHashToFind, HashResultNet[] hashResultNetArray)", "Phase 38 UWP page does not yet switch the find-result surface to HashResultNet.");
            AssertContains(winUwpPage, "AppendFileResultToTextMain(hashResult, m_uppercaseChecked);", "Phase 38 UWP page does not yet render HashResultNet query results directly.");
        }, failures);

        Run("Phase 39 promotes managed realtime bridge delegates and page rendering onto HashResultNet", () =>
        {
            string clrDelegatesHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeDelegates.h");
            string clrDelegatesSource = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeDelegates.cpp");
            string uwpDelegateHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeDelegate.h");
            string uwpDelegateSource = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeDelegate.cpp");
            string bridgeWuiSource = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwpSource = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");
            string winUiPage = ReadRepoFile(repoRoot, @"trunk\source\WinUI\MainPage.xaml.cs");
            string winUwpPage = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\MainPage.xaml.cs");

            AssertContains(clrDelegatesHeader, "#include \"HashResultNet.h\"", "Phase 39 CLR delegate header does not yet consume HashResultNet.");
            AssertContains(clrDelegatesHeader, "public delegate void HashResultEventHandler(HashResultNet);", "Phase 39 CLR delegate header does not yet expose HashResultNet event handlers.");
            AssertContains(clrDelegatesHeader, "void ShowFileHash(HashResultNet hashResultNet, bool uppercase);", "Phase 39 CLR delegate header does not yet switch ShowFileHash to HashResultNet.");
            AssertContains(clrDelegatesSource, "void UIBridgeDelegates::ShowFileHash(HashResultNet hashResultNet, bool uppercase)", "Phase 39 CLR delegate implementation does not yet switch ShowFileHash to HashResultNet.");

            AssertContains(uwpDelegateHeader, "#include \"HashResultNet.h\"", "Phase 39 UWP delegate header does not yet consume HashResultNet.");
            AssertContains(uwpDelegateHeader, "public delegate void HashResultEventHandler(HashResultNet);", "Phase 39 UWP delegate header does not yet expose HashResultNet event handlers.");
            AssertContains(uwpDelegateHeader, "void ShowFileHash(HashResultNet hashResultNet, Platform::Boolean uppercase);", "Phase 39 UWP delegate header does not yet switch ShowFileHash to HashResultNet.");
            AssertContains(uwpDelegateSource, "void UIBridgeDelegate::ShowFileHash(HashResultNet hashResultNet, Boolean uppercase)", "Phase 39 UWP delegate implementation does not yet switch ShowFileHash to HashResultNet.");

            AssertContains(bridgeWuiSource, "m_uiBridgeDelegates->ShowFileHash(hashResultNet, hashUppercase);", "Phase 39 WinUI bridge does not yet forward realtime hash events as HashResultNet.");
            AssertContains(bridgeUwpSource, "m_uiBridgeDelegate->ShowFileHash(hashResultNet, hashUppercase);", "Phase 39 UWP bridge does not yet forward realtime hash events as HashResultNet.");

            AssertContains(winUiPage, "private void AppendFileResultToTextMain(HashResultNet hashResult, bool uppercase)", "Phase 39 WinUI page does not yet render realtime results directly from HashResultNet.");
            AssertContains(winUiPage, "private void UIBridgeHandlers_ShowFileHashHandler(HashResultNet hashResult, bool uppercase)", "Phase 39 WinUI page does not yet accept realtime HashResultNet hash events.");
            AssertDoesNotContain(winUiPage, "CreateCompatibilityResultData(", "Phase 39 WinUI page still rebuilds compatibility ResultDataNet for managed rendering.");

            AssertContains(winUwpPage, "private void AppendFileResultToTextMain(HashResultNet hashResult, bool uppercase)", "Phase 39 UWP page does not yet render realtime results directly from HashResultNet.");
            AssertContains(winUwpPage, "private void UIBridgeDelegate_ShowFileHashHandler(HashResultNet hashResult, bool uppercase)", "Phase 39 UWP page does not yet accept realtime HashResultNet hash events.");
            AssertDoesNotContain(winUwpPage, "CreateCompatibilityResultData(", "Phase 39 UWP page still rebuilds compatibility ResultDataNet for managed rendering.");
        }, failures);

        Run("Phase 40 removes managed ResultDataNet query compatibility wrappers in favor of HashResultNet-only query APIs", () =>
        {
            string clrHashMgmtHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.h");
            string clrHashMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpHashMgmtHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.h");
            string uwpHashMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");

            AssertContains(clrHashMgmtHeader, "cli::array<HashResultNet>^ FindHashResults(System::String^ sstrHashToFind);", "Phase 40 CLR HashMgmt no longer exposes the HashResultNet query API.");
            AssertDoesNotContain(clrHashMgmtHeader, "FindResult(System::String^ sstrHashToFind)", "Phase 40 CLR HashMgmt still exposes the legacy ResultDataNet query wrapper.");
            AssertDoesNotContain(clrHashMgmt, "CreateCompatibilityResultDataNetArray(", "Phase 40 CLR HashMgmt still keeps the legacy ResultDataNet compatibility array helper.");

            AssertContains(uwpHashMgmtHeader, "Platform::Array<HashResultNet>^ FindHashResults(Platform::String^ pstrHashToFind);", "Phase 40 UWP HashMgmt no longer exposes the HashResultNet query API.");
            AssertDoesNotContain(uwpHashMgmtHeader, "FindResult(Platform::String^ pstrHashToFind)", "Phase 40 UWP HashMgmt still exposes the legacy ResultDataNet query wrapper.");
            AssertDoesNotContain(uwpHashMgmt, "CreateCompatibilityResultDataNetArray(", "Phase 40 UWP HashMgmt still keeps the legacy ResultDataNet compatibility array helper.");
        }, failures);

        Run("Phase 41 extracts neutral managed projection primitives into ResultNetProjection and removes ResultData-based managed dispatch", () =>
        {
            string resultNetProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultNetProjection.h");
            string resultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataProjection.h");
            string hashResultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultProjection.h");
            string managedDispatch = ReadRepoFile(repoRoot, @"trunk\source\Common\ManagedBridgeDispatch.h");

            AssertContains(resultNetProjection, "static inline TResultStateNet ConvertResultStateToNet(ResultState resultState)", "Phase 41 ResultNetProjection does not yet own the shared ResultStateNet conversion helper.");
            AssertContains(resultNetProjection, "DispatchResultDigestValueByType(ResultDigestType digestType, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)", "Phase 41 ResultNetProjection does not yet own the shared digest-type dispatch helper.");
            AssertContains(resultNetProjection, "static inline TResultDataNet AssignResultDigestToNet(TResultDataNet resultDataNet, ResultDigestType digestType, TResultString digestValue)", "Phase 41 ResultNetProjection does not yet own the shared managed digest assignment helper.");
            AssertContains(resultProjection, "#include \"Common/ResultNetProjection.h\"", "Phase 41 ResultDataProjection does not yet depend on the neutral ResultNetProjection seam.");
            AssertContains(hashResultProjection, "#include \"Common/ResultNetProjection.h\"", "Phase 41 HashResultProjection does not yet depend on the neutral ResultNetProjection seam.");
            AssertDoesNotContain(hashResultProjection, "#include \"Common/ResultDataProjection.h\"", "Phase 41 HashResultProjection still depends on the legacy ResultDataProjection header.");
            AssertDoesNotContain(managedDispatch, "DispatchManagedBridgeResultByType(const ResultData& result", "Phase 41 ManagedBridgeDispatch still exposes the deprecated ResultData-based managed dispatch overload.");
            AssertContains(managedDispatch, "DispatchManagedBridgeResultByType(const HashResult& result", "Phase 41 ManagedBridgeDispatch does not yet expose the HashResult-only managed dispatch overload.");
        }, failures);

        Run("Phase 42 removes ResultData-based progress and observer compatibility overloads so HashResult stays a pure event contract", () =>
        {
            string hashResult = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResult.h");
            string progressEvent = ReadRepoFile(repoRoot, @"trunk\source\Common\ProgressEvent.h");
            string hashEngineObserver = ReadRepoFile(repoRoot, @"trunk\source\Adapters\UiBridge\HashEngineObserver.h");

            AssertDoesNotContain(hashResult, "const ResultData *sourceResult;", "Phase 42 HashResult still keeps the legacy ResultData back-pointer.");

            AssertDoesNotContain(progressEvent, "CreateResultProgressEvent(ProgressEventType eventType, const ResultData& result)", "Phase 42 ProgressEvent still keeps the legacy ResultData result overload.");
            AssertDoesNotContain(progressEvent, "CreateFileStartedProgressEvent(const ResultData& result)", "Phase 42 ProgressEvent still keeps the legacy ResultData file-start overload.");
            AssertDoesNotContain(progressEvent, "CreateFileMetaReadyProgressEvent(const ResultData& result)", "Phase 42 ProgressEvent still keeps the legacy ResultData file-meta overload.");
            AssertDoesNotContain(progressEvent, "CreateFileHashReadyProgressEvent(const ResultData& result, bool uppercaseDigest)", "Phase 42 ProgressEvent still keeps the legacy ResultData file-hash overload.");
            AssertDoesNotContain(progressEvent, "CreateFileFailedProgressEvent(const ResultData& result)", "Phase 42 ProgressEvent still keeps the legacy ResultData file-error overload.");

            AssertDoesNotContain(hashEngineObserver, "void onFileStarted(const ResultData& result)", "Phase 42 HashEngineObserver still keeps the legacy ResultData file-start wrapper.");
            AssertDoesNotContain(hashEngineObserver, "void onFileMetaReady(const ResultData& result)", "Phase 42 HashEngineObserver still keeps the legacy ResultData file-meta wrapper.");
            AssertDoesNotContain(hashEngineObserver, "void onFileHashReady(const ResultData& result, bool uppercase)", "Phase 42 HashEngineObserver still keeps the legacy ResultData file-hash wrapper.");
            AssertDoesNotContain(hashEngineObserver, "void onFileFailed(const ResultData& result)", "Phase 42 HashEngineObserver still keeps the legacy ResultData file-error wrapper.");
        }, failures);

        Run("Phase 43 makes the MFC bridge render HashResult directly without rebuilding compatibility ResultData", () =>
        {
            string hashResultRender = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultRender.h");
            string bridgeMfcHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.h");
            string bridgeMfcSource = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(hashResultRender, "GetHashResultSizeDisplayInfo(const HashResult& result)", "Phase 43 HashResultRender does not yet expose HashResult size formatting.");
            AssertContains(hashResultRender, "VisitRenderableHashResultMetaLines(const HashResult& result, TResultMetaLineVisitor visitor)", "Phase 43 HashResultRender does not yet expose HashResult meta traversal.");
            AssertContains(hashResultRender, "VisitHashResultDigestDisplayValues(const HashResult& result, bool uppercase, TResultDigestDisplayVisitor visitor)", "Phase 43 HashResultRender does not yet expose HashResult digest rendering traversal.");

            AssertContains(bridgeMfcHeader, "#include \"Common/HashResultRender.h\"", "Phase 43 MFC bridge header does not yet depend on HashResultRender.");
            AssertDoesNotContain(bridgeMfcHeader, "#include \"Common/HashResultCompatibility.h\"", "Phase 43 MFC bridge header still depends on HashResultCompatibility.");
            AssertContains(bridgeMfcHeader, "static void AppendResultToHyperEdit(const HashResult& result,", "Phase 43 MFC bridge header does not yet expose HashResult rendering helpers.");

            AssertContains(bridgeMfcSource, "case PROGRESS_EVENT_FILE_HASH_READY:", "Phase 43 MFC bridge realtime hash rendering does not yet route hash-ready events through the dedicated file-result event seam.");
            AssertContains(bridgeMfcSource, "AppendResultSectionAndRefresh(result, RESULT_RENDER_SECTION_HASH, uppercaseDigest);", "Phase 43 MFC bridge realtime hash rendering does not yet consume HashResult directly.");
            AssertContains(bridgeMfcSource, "VisitHashResultDigestDisplayValues(result, uppercase", "Phase 43 MFC bridge does not yet render digest values directly from HashResult.");
            AssertContains(bridgeMfcSource, "void UIBridgeMFC::AppendResultToHyperEdit(const HashResult& result,", "Phase 43 MFC bridge does not yet expose HashResult whole-result rendering.");
            AssertDoesNotContain(bridgeMfcSource, "CreateCompatibilityResultData(result);", "Phase 43 MFC bridge still rebuilds compatibility ResultData.");
        }, failures);

        Run("Phase 44 removes HashResultCompatibility and routes MFC search plus Mac bridge consumption directly through HashResult", () =>
        {
            string searchHeader = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSearchController.h");
            string searchSource = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSearchController.cpp");
            string bridgeMacHeader = ReadRepoFile(repoRoot, @"trunk\source\OSXUI\UIBridgeMacSwift.h");
            string bridgeMacSource = ReadRepoFile(repoRoot, @"trunk\source\OSXUI\UIBridgeMacSwift.mm");
            string hashBridgeMac = ReadRepoFile(repoRoot, @"trunk\source\OSXUI\HashBridge.mm");
            string compatibilityPath = Path.Combine(repoRoot, @"trunk\source\Common\HashResultCompatibility.h");

            if (File.Exists(compatibilityPath))
            {
                throw new InvalidOperationException("Phase 44 still keeps the deprecated HashResultCompatibility header.");
            }

            AssertContains(searchHeader, "void AppendResult(const HashResult& result);", "Phase 44 MFC search controller does not yet accept HashResult as its render subject.");
            AssertContains(searchSource, "VisitThreadDataHashResults(*m_threadData, [&](const HashResult& result)", "Phase 45 MFC search controller does not yet route history traversal through the shared HashResult visitor seam.");
            AssertContains(searchSource, "VisitThreadDataPathAndDigestMatchingHashResults(*m_threadData, tstrFileToFind, tstrHashToFind, [&](const HashResult& result)", "Phase 45 MFC search controller does not yet route search traversal through the shared HashResult visitor seam.");
            AssertContains(searchSource, "UIBridgeMFC::AppendResultToHyperEdit(result, GetThreadDataUppercase(*m_threadData), m_mainEdit);", "Phase 44 MFC search controller does not yet render search/history results directly from HashResult.");

            AssertDoesNotContain(bridgeMacHeader, "#include \"Common/HashResultCompatibility.h\"", "Phase 44 Mac bridge header still depends on HashResultCompatibility.");
            AssertContains(bridgeMacHeader, "static ResultDataSwift *ConvertHashResultToSwift(const HashResult& result);", "Phase 44 Mac bridge header does not yet expose direct HashResult-to-Swift projection.");
            AssertContains(bridgeMacSource, "ResultDataSwift *resultSwift = UIBridgeMacSwift::ConvertHashResultToSwift(result);", "Phase 44 Mac bridge realtime path does not yet project HashResult directly.");
            AssertDoesNotContain(bridgeMacSource, "CreateCompatibilityResultData(result);", "Phase 44 Mac bridge still rebuilds compatibility ResultData.");
            AssertContains(bridgeMacSource, "ResultDataSwift *UIBridgeMacSwift::ConvertHashResultToSwift(const HashResult& result)", "Phase 44 Mac bridge does not yet expose direct HashResult-to-Swift projection.");
            AssertContains(hashBridgeMac, "VisitThreadDataHashResults(*_thrdData, [&](const HashResult& result)", "Phase 45 Mac history bridge does not yet route stored results through the shared HashResult visitor seam.");
            AssertContains(hashBridgeMac, "ConvertHashResultToSwift(result);", "Phase 45 Mac history bridge does not yet consume shared HashResult traversal directly.");
        }, failures);

        Run("Phase 45 routes shared search and history traversal through HashResult visitor seams", () =>
        {
            string hashResultSearch = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultSearch.h");
            string hashResultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultProjection.h");
            string threadResultAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ThreadDataResultAccess.h");
            string managedHashMgmtAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ManagedHashMgmtAccess.h");
            string filesHashSearchController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSearchController.cpp");
            string hashBridgeMac = ReadRepoFile(repoRoot, @"trunk\source\OSXUI\HashBridge.mm");

            AssertContains(hashResultSearch, "VisitHashResults(const HashResultList& resultList, THashResultVisitor visitor)", "Phase 45 HashResultSearch does not yet expose whole-list HashResult traversal.");
            AssertContains(hashResultSearch, "VisitMatchingHashResults(const HashResultList& resultList, THashResultPredicate predicate, THashResultVisitor visitor)", "Phase 45 HashResultSearch does not yet expose shared HashResult matching traversal.");
            AssertContains(hashResultSearch, "VisitPathAndDigestMatchingHashResults(const HashResultList& resultList, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, THashResultVisitor visitor)", "Phase 45 HashResultSearch does not yet expose shared path+digest HashResult traversal.");
            AssertContains(hashResultSearch, "NormalizeHashResultPathSearchText(const sunjwbase::tstring& pathText)", "Phase 45 HashResultSearch does not yet own path normalization.");
            AssertContains(hashResultSearch, "NormalizeHashResultDigestSearchText(const sunjwbase::tstring& digestText)", "Phase 45 HashResultSearch does not yet own digest normalization.");

            AssertContains(hashResultProjection, "VisitHashResults(resultList, [&](const HashResult& hashResult)", "Phase 45 HashResultProjection does not yet reuse shared HashResult traversal.");
            AssertContains(hashResultProjection, "VisitMatchingHashResults(resultList, predicate, [&](const HashResult& hashResult)", "Phase 45 HashResultProjection does not yet reuse shared HashResult matching traversal.");

            AssertContains(threadResultAccess, "VisitThreadDataHashResults(const ThreadData& threadData, THashResultVisitor visitor)", "Phase 45 ThreadData result access does not yet expose HashResult traversal.");
            AssertContains(threadResultAccess, "VisitThreadDataPathAndDigestMatchingHashResults(const ThreadData& threadData, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, THashResultVisitor visitor)", "Phase 45 ThreadData result access does not yet expose HashResult path+digest traversal.");

            AssertContains(managedHashMgmtAccess, "NormalizeHashResultDigestSearchText(hashToFind)", "Phase 45 managed hash management does not yet normalize digest queries through HashResultSearch.");
            AssertContains(filesHashSearchController, "VisitThreadDataHashResults(*m_threadData, [&](const HashResult& result)", "Phase 45 MFC search controller does not yet route history traversal through ThreadData HashResult visitors.");
            AssertContains(filesHashSearchController, "VisitThreadDataPathAndDigestMatchingHashResults(*m_threadData, tstrFileToFind, tstrHashToFind, [&](const HashResult& result)", "Phase 45 MFC search controller does not yet route search traversal through ThreadData HashResult visitors.");
            AssertContains(hashBridgeMac, "VisitThreadDataHashResults(*_thrdData, [&](const HashResult& result)", "Phase 45 Mac history bridge does not yet route history traversal through ThreadData HashResult visitors.");
        }, failures);

        Run("Phase 46 demotes ResultData projection and digest-count helpers into thin compatibility shims over HashResult seams", () =>
        {
            string resultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataProjection.h");
            string resultSearch = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataSearch.h");

            AssertContains(resultProjection, "#include \"Common/HashResultProjection.h\"", "Phase 46 ResultDataProjection does not yet layer on top of HashResultProjection.");
            AssertContains(resultProjection, "AssignHashResultCoreToNet<TResultDataNet, TResultStateNet>(resultDataNet, ProjectHashResult(result), convertString);", "Phase 46 ResultDataProjection does not yet route core projection through HashResultProjection.");
            AssertContains(resultProjection, "AssignHashResultDigestsToNet(resultDataNet, ProjectHashResult(result), convertString);", "Phase 46 ResultDataProjection does not yet route digest projection through HashResultProjection.");
            AssertContains(resultProjection, "VisitProjectedHashResults<TResultDataNet, TResultStateNet>(resultList, convertString, visitor);", "Phase 46 ResultDataProjection whole-list traversal does not yet defer to HashResultProjection.");
            AssertContains(resultProjection, "VisitProjectedDigestMatchingHashResults<TResultDataNet, TResultStateNet>(resultList, digestText, convertString, visitor);", "Phase 46 ResultDataProjection digest traversal does not yet defer to HashResultProjection.");
            AssertContains(resultProjection, "CreateProjectedDigestMatchingHashResults<TResultDataNet, TResultStateNet, TResultArray>(resultList, digestText, createResultArray, convertString, setProjectedResult);", "Phase 46 ResultDataProjection digest materialization does not yet defer to HashResultProjection.");
            AssertContains(resultSearch, "return CountDigestMatchingHashResults(resultList, digestText);", "Phase 46 ResultDataSearch digest-count helper does not yet defer to HashResultSearch.");
        }, failures);

        Run("Phase 47 introduces a native C++ runtime test project and gates all native builds on it", () =>
        {
            string workflow = ReadRepoFile(repoRoot, @".github\workflows\windows-build.yml");
            string nativeRuntimeProject = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\FHash.NativeRuntimeTests.vcxproj");
            string nativeRuntimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
            string nativeRuntimeMain = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\NativeTestMain.cpp");
            string solution = ReadRepoFile(repoRoot, @"trunk\fileshash15.sln");
            string gitignore = ReadRepoFile(repoRoot, @".gitignore");

            AssertContains(nativeRuntimeProject, "<ProjectName>FHash.NativeRuntimeTests</ProjectName>", "Phase 47 native runtime test project does not yet exist.");
            AssertContains(nativeRuntimeProject, "<ConfigurationType>Application</ConfigurationType>", "Phase 47 native runtime test project is not a standalone executable.");
            AssertContains(nativeRuntimeProject, @"..\..\sub-proj\fHashNativeCore\fHashNativeCore.vcxproj", "Phase 47 native runtime tests do not yet reference fHashNativeCore.");
            AssertContains(nativeRuntimeProject, "Version.lib;%(AdditionalDependencies)", "Phase 47 native runtime test project does not yet link Version.lib for WindowsComm version helpers.");
            AssertContains(nativeRuntimeSource, "HashThreadFunc_ComputesExpectedDigestsForSingleFile", "Phase 47 native runtime tests do not yet cover the main HashThreadFunc runtime path.");
            AssertContains(nativeRuntimeSource, "HashThreadFunc_ProcessesMultipleFilesAndWholeProgress", "Phase 47 native runtime tests do not yet cover multi-file runtime progress and result storage.");
            AssertContains(nativeRuntimeSource, "HashThreadFunc_RespectsSelectedAlgorithms", "Phase 47 native runtime tests do not yet cover algorithm selection.");
            AssertContains(nativeRuntimeSource, "HashResultSearch_FindsMatchingRuntimeDigests", "Phase 47 native runtime tests do not yet cover runtime digest search.");
            AssertContains(nativeRuntimeSource, "HashResultSearch_MatchesPathAndDigestForRuntimeResults", "Phase 47 native runtime tests do not yet cover combined path+digest runtime search.");
            AssertContains(nativeRuntimeSource, "HashThreadFunc_ComputesExpectedDigestsForEmptyFile", "Phase 47 native runtime tests do not yet cover the empty-file digest vectors.");
            AssertContains(nativeRuntimeSource, "RunHashRequest_ReportsMissingFileAsErrorResult", "Phase 47 native runtime tests do not yet cover the missing-file error path.");
            AssertContains(nativeRuntimeSource, "RunHashRequest_ContinuesAfterOpenFileErrorInBatch", "Phase 47 native runtime tests do not yet cover mixed success+error file batches.");
            AssertContains(nativeRuntimeSource, "RunHashRequest_CancelsWhenStopRequestedBeforeStart", "Phase 47 native runtime tests do not yet cover cooperative cancellation.");
            AssertContains(nativeRuntimeSource, "RunHashRequest_PropagatesUppercasePreferenceInHashReadyEvent", "Phase 47 native runtime tests do not yet cover uppercase digest event propagation.");
            AssertContains(nativeRuntimeSource, "RunHashRequest_CancelsDuringFileProgressAndSkipsRemainingFiles", "Phase 47 native runtime tests do not yet cover mid-run cancellation.");
            AssertContains(nativeRuntimeMain, "All native runtime tests passed", "Phase 47 native runtime test main does not yet report aggregate success.");

            AssertContains(solution, "FHash.NativeRuntimeTests", "Phase 47 fileshash15.sln does not yet include the native runtime test project.");
            AssertContains(gitignore, "native-runtime-tests/**/x64/", "Phase 47 .gitignore does not yet ignore native runtime test build outputs.");

            AssertContains(workflow, "native-runtime-tests:", "Phase 47 workflow does not yet define a native-runtime-tests job.");
            AssertContains(workflow, "msbuild native-runtime-tests/FHash.NativeRuntimeTests/FHash.NativeRuntimeTests.vcxproj", "Phase 47 workflow does not yet build the native runtime test project.");
            AssertContains(workflow, @"native-runtime-tests\FHash.NativeRuntimeTests\x64\Release\FHash.NativeRuntimeTests.exe", "Phase 47 workflow does not yet execute the native runtime test binary.");
            AssertInOrder(
                workflow,
                new[]
                {
                    "build-legacy-x64:",
                    "needs:",
                    "- security-regression",
                    "- unit-tests",
                    "- native-runtime-tests"
                },
                "Phase 47 build-legacy-x64 is not yet gated by native-runtime-tests.");
        }, failures);

        Run("Phase 48 exercises the publish-release chain on branches while reserving GitHub releases for version tags", () =>
        {
            string workflow = ReadRepoFile(repoRoot, @".github\workflows\windows-build.yml");

            AssertContains(workflow, "publish-release:", "Phase 48 workflow does not yet define a publish-release job.");
            AssertContains(workflow, "if: github.event_name != 'pull_request'", "Phase 48 publish-release is not yet enabled for non-PR rehearsal runs.");
            AssertContains(workflow, "pattern: LHash-*", "Phase 48 publish-release does not yet download all packaged release artifacts.");
            AssertContains(workflow, "merge-multiple: true", "Phase 48 publish-release does not yet merge downloaded artifacts into a single release-assets directory.");
            AssertContains(workflow, "Stage release rehearsal bundle", "Phase 48 publish-release does not yet stage a rehearsal bundle.");
            AssertContains(workflow, "RELEASE_MANIFEST.txt", "Phase 48 publish-release does not yet emit a release manifest.");
            AssertContains(workflow, "LHash-release-rehearsal", "Phase 48 publish-release does not yet upload a release rehearsal artifact.");
            AssertContains(workflow, "release_mode=\"rehearsal\"", "Phase 48 publish-release rehearsal mode is not yet recorded.");
            AssertContains(workflow, "release_mode=\"tagged-release\"", "Phase 48 publish-release tag mode is not yet recorded.");
            AssertContains(workflow, "if: startsWith(github.ref, 'refs/tags/v')", "Phase 48 publish-release does not yet reserve GitHub release publishing for version tags.");
            AssertContains(workflow, "softprops/action-gh-release@v2", "Phase 48 publish-release does not yet invoke the GitHub release publisher.");
            AssertContains(workflow, "release-assets/LHash-legacy-x64-*.zip", "Phase 48 publish-release does not yet publish the packaged legacy zip.");
            AssertContains(workflow, "release-staging/RELEASE_MANIFEST.txt", "Phase 48 publish-release does not yet attach the release manifest.");
        }, failures);

        Run("Phase 49 promotes single-file hashing into a dedicated runner seam so HashEngine.cpp stays orchestration-focused", () =>
        {
            string hashEngine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string hashFileRunner = string.Join(
                "\r\n",
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileRunner.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp"));
            string hashScheduler = ReadRepoFile(repoRoot, @"trunk\source\Common\HashScheduler.cpp");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");

            AssertContains(hashEngine, "RunHashScheduler(executionContext, request, isSizeCaled, fSizes)", "Phase 49 HashEngine.cpp does not yet delegate orchestration to the extracted scheduler seam.");
            AssertDoesNotContain(hashEngine, "static bool ProcessOpenedFileHashing(", "Phase 49 HashEngine.cpp still owns the opened-file hashing loop.");
            AssertContains(hashFileRunner, "bool ProcessOpenedFileHashing(", "Phase 49 digest pipeline does not yet own the opened-file hashing loop.");
            AssertContains(hashFileRunner, "bool RunFileHashAttempt(", "Phase 49 HashFileRunner.cpp does not yet expose the single-file execution seam.");
            AssertContains(hashFileRunner, "YieldHashThread();", "Phase 49 HashFileRunner.cpp does not yet own per-file scheduler yielding.");
            AssertDoesNotContain(hashFileRunner, "FileExecutionState executionState = { 0 };", "Phase 49 HashFileRunner.cpp should reuse grouped execution state from HashEngine.cpp instead of creating a new local bundle.");
            AssertContains(hashScheduler, "FileExecutionState executionState = { 0 };", "Phase 49 HashScheduler.cpp does not yet preserve grouped file execution state for the runner seam.");
            AssertContains(hashScheduler, "VisitHashRequestFiles(request, [&](uint32_t fileIndex, const tstring& fullPath)", "Phase 49 HashScheduler.cpp does not yet own request-file iteration.");
            AssertContains(hashScheduler, "RunFileHashAttempt(executionContext, request, fileIndex, fullPath, isSizeCaled, fSizes,", "Phase 49 HashScheduler.cpp does not yet route single-file work into HashFileRunner.");
            AssertContains(hashEngineInternal, "bool RunFileHashAttempt(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const sunjwbase::tstring& fullPath, bool isSizeCaled, ULLongVector& fSizes,", "Phase 49 HashEngineInternal.h does not yet declare the single-file runner seam.");
            AssertContains(hashEngineInternal, "FileExecutionState *executionState", "Phase 49 HashEngineInternal.h does not yet route grouped file execution state into the runner seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileRunner.cpp", "Phase 49 desktop native core project does not yet compile HashFileRunner.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileRunner.cpp", "Phase 49 desktop native core filters do not yet expose HashFileRunner.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileRunner.cpp", "Phase 49 UWP native project does not yet compile HashFileRunner.cpp.");
        }, failures);

        Run("Phase 50 promotes request scheduling into a dedicated scheduler seam so HashEngine.cpp keeps only entry and lifecycle logic", () =>
        {
            string hashEngine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string hashScheduler = ReadRepoFile(repoRoot, @"trunk\source\Common\HashScheduler.cpp");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashEngine, "if (!RunHashScheduler(executionContext, request, isSizeCaled, fSizes))", "Phase 50 HashEngine.cpp does not yet route request scheduling through HashScheduler.");
            AssertDoesNotContain(hashEngine, "VisitHashRequestFiles(request, [&](uint32_t fileIndex, const tstring& fullPath)", "Phase 50 HashEngine.cpp still owns file iteration instead of delegating it to HashScheduler.");
            AssertDoesNotContain(hashEngine, "ThreadPool threadPool(5);", "Phase 50 HashEngine.cpp still owns the thread-pool scheduler instead of delegating it to HashScheduler.");

            AssertContains(hashScheduler, "bool RunHashScheduler(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, ULLongVector& fSizes)", "Phase 50 HashScheduler.cpp does not yet expose the dedicated request scheduler seam.");
            AssertContains(hashScheduler, "static bool ExecuteScheduledHashRequestFiles(", "Phase 50 HashScheduler.cpp does not yet isolate the scheduled file loop.");
            AssertContains(hashScheduler, "ThreadPool threadPool(5);", "Phase 50 HashScheduler.cpp does not yet own the fixed-size thread pool.");
            AssertContains(hashScheduler, "VisitHashRequestFiles(request, [&](uint32_t fileIndex, const tstring& fullPath)", "Phase 50 HashScheduler.cpp does not yet iterate files through HashRequest.");
            AssertContains(hashEngineInternal, "bool RunHashScheduler(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, ULLongVector& fSizes);", "Phase 50 HashEngineInternal.h does not yet declare the request scheduler seam.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashScheduler.cpp", "Phase 50 desktop native core project does not yet compile HashScheduler.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashScheduler.cpp", "Phase 50 desktop native core filters do not yet expose HashScheduler.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashScheduler.cpp", "Phase 50 UWP native project does not yet compile HashScheduler.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashScheduler.cpp", "Phase 50 UWP native filters do not yet expose HashScheduler.cpp.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashScheduler.cpp", "Phase 50 WinUI native project should keep consuming the shared native core instead of compiling HashScheduler.cpp directly.");
        }, failures);

        Run("Phase 51 promotes result publication into a dedicated publisher seam so digest finalization and event emission stop sharing one file", () =>
        {
            string hashEngineResult = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp");
            string hashResultPublisher = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp");
            string hashResultPublisherHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");

            AssertContains(hashEngineResult, "PrepareFileMetaResult(", "Phase 51 HashEngineResult.cpp should still own file metadata result preparation.");
            AssertContains(hashEngineResult, "FinalizeDigestStrings(", "Phase 51 HashEngineResult.cpp should still own digest finalization.");
            AssertDoesNotContain(hashEngineResult, "void CompleteSuccessfulFileHashing(", "Phase 51 HashEngineResult.cpp still owns successful file publication.");
            AssertDoesNotContain(hashEngineResult, "void EmitHashResult(", "Phase 51 HashEngineResult.cpp still owns hash-ready publication.");

            AssertContains(hashResultPublisherHeader, "struct FileExecutionState;", "Phase 51 HashResultPublisher.h does not yet forward declare the grouped file execution state.");
            AssertContains(hashResultPublisherHeader, "void CompleteSuccessfulFileHashing(", "Phase 51 HashResultPublisher.h does not yet expose the successful-file publisher seam.");
            AssertContains(hashResultPublisherHeader, "void EmitHashResult(", "Phase 51 HashResultPublisher.h does not yet expose the hash-ready publisher seam.");
            AssertContains(hashResultPublisher, "void UpdateWholeProgressAfterFile(", "Phase 51 HashResultPublisher.cpp does not yet own whole-progress publication.");
            AssertContains(hashResultPublisher, "void CompleteSuccessfulFileHashing(", "Phase 51 HashResultPublisher.cpp does not yet own the successful-file publisher seam.");
            AssertContains(hashResultPublisher, "void CompleteOpenedFileAttempt(", "Phase 51 HashResultPublisher.cpp does not yet own the opened-file completion publisher seam.");
            AssertContains(hashResultPublisher, "void EmitHashResult(", "Phase 51 HashResultPublisher.cpp does not yet own the hash-ready publisher seam.");
            AssertContains(hashResultPublisher, "void EmitErrorResult(", "Phase 51 HashResultPublisher.cpp does not yet own the error publisher seam.");
            AssertContains(hashResultPublisher, "void FinishFileProcessing(", "Phase 51 HashResultPublisher.cpp does not yet own the file-finished publisher seam.");

            AssertContains(hashEngineInternal, "#include \"Common/HashResultPublisher.h\"", "Phase 51 HashEngineInternal.h does not yet consume the publisher seam.");
            AssertDoesNotContain(hashEngineInternal, "void EmitHashResult(HashExecutionContext *executionContext, HashResult& result, bool uppercase);", "Phase 51 HashEngineInternal.h still directly declares the hash-ready publisher helper.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashResultPublisher.cpp", "Phase 51 desktop native core project does not yet compile HashResultPublisher.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashResultPublisher.cpp", "Phase 51 desktop native core filters do not yet expose HashResultPublisher.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashResultPublisher.cpp", "Phase 51 UWP native project does not yet compile HashResultPublisher.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashResultPublisher.cpp", "Phase 51 UWP native filters do not yet expose HashResultPublisher.cpp.");
        }, failures);

        Run("Phase 52 promotes opened-file digest update orchestration into a dedicated digest pipeline seam", () =>
        {
            string hashFileRunner = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileRunner.cpp");
            string hashDigestPipeline = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp");
            string hashDigestPipelineHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashFileRunner, "bool wasStopped = ProcessOpenedFileHashing(executionContext, request, result, fileIndex, isSizeCaled, fSizes, executionState", "Phase 52 HashFileRunner.cpp does not yet delegate opened-file digest updates through the digest pipeline seam.");
            AssertDoesNotContain(hashFileRunner, "future<void> taskSHA512Update", "Phase 52 HashFileRunner.cpp still owns SHA512 digest worker futures instead of delegating them to HashDigestPipeline.");

            AssertContains(hashDigestPipelineHeader, "uint64_t CalculateFileChunkIterations(uint64_t fsize);", "Phase 52 HashDigestPipeline.h does not yet expose the chunk-iteration helper seam.");
            AssertContains(hashDigestPipelineHeader, "bool ProcessOpenedFileHashing(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex,", "Phase 52 HashDigestPipeline.h does not yet expose the opened-file digest pipeline seam.");
            AssertContains(hashDigestPipeline, "class DataBuffer", "Phase 52 HashDigestPipeline.cpp does not yet own buffered digest update state.");
            AssertContains(hashDigestPipeline, "uint64_t CalculateFileChunkIterations(uint64_t fsize)", "Phase 52 HashDigestPipeline.cpp does not yet own chunk-iteration calculations.");
            AssertContains(hashDigestPipeline, "bool ProcessOpenedFileHashing(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex,", "Phase 52 HashDigestPipeline.cpp does not yet own opened-file digest update orchestration.");
            AssertContains(hashDigestPipeline, "future<void> taskSHA512Update", "Phase 52 HashDigestPipeline.cpp does not yet own SHA512 digest worker fan-out.");
            AssertContains(hashDigestPipeline, "future<void> taskSHA256Update", "Phase 52 HashDigestPipeline.cpp does not yet own SHA256 digest worker fan-out.");
            AssertContains(hashDigestPipeline, "future<void> taskSHA1Update", "Phase 52 HashDigestPipeline.cpp does not yet own SHA1 digest worker fan-out.");
            AssertContains(hashDigestPipeline, "future<void> taskMD5Update", "Phase 52 HashDigestPipeline.cpp does not yet own MD5 digest worker fan-out.");
            AssertContains(hashDigestPipeline, "observer->onProgressEvent(CreateFileProgressEvent(positionNew));", "Phase 52 HashDigestPipeline.cpp does not yet own per-file progress publication.");
            AssertContains(hashDigestPipeline, "observer->onProgressEvent(CreateTotalProgressEvent(progressState->positionWhole));", "Phase 52 HashDigestPipeline.cpp does not yet own total-progress publication.");

            AssertContains(hashEngineInternal, "#include \"Common/HashDigestPipeline.h\"", "Phase 52 HashEngineInternal.h does not yet consume the digest pipeline seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestPipeline.cpp", "Phase 52 desktop native core project does not yet compile HashDigestPipeline.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestPipeline.h", "Phase 52 desktop native core project does not yet include HashDigestPipeline.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestPipeline.cpp", "Phase 52 desktop native core filters do not yet expose HashDigestPipeline.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestPipeline.h", "Phase 52 desktop native core filters do not yet expose HashDigestPipeline.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestPipeline.cpp", "Phase 52 UWP native project does not yet compile HashDigestPipeline.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestPipeline.h", "Phase 52 UWP native project does not yet include HashDigestPipeline.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestPipeline.cpp", "Phase 52 UWP native filters do not yet expose HashDigestPipeline.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestPipeline.h", "Phase 52 UWP native filters do not yet expose HashDigestPipeline.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestPipeline.cpp", "Phase 52 WinUI native project should keep consuming the shared native core instead of compiling HashDigestPipeline.cpp directly.");
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

    private static string ReadResultDigestAccessSeams(string repoRoot)
    {
        return string.Join(
            "\r\n",
            ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestMetadataAccess.h"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestStateAccess.h"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestValueAccess.h"));
    }

    private static string ReadHashEngineImplementation(string repoRoot)
    {
        return string.Join(
            "\r\n",
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileRunner.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashScheduler.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp"));
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
