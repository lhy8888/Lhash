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
            string legacyThreadData = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\LegacyThreadData.h");

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
            AssertDoesNotContain(global, "#include \"LegacyCompat/LegacyThreadData.h\"", "Global.h should not include legacy ThreadData contracts directly.");
            AssertDoesNotContain(global, "struct ThreadData;", "Global.h should no longer expose ThreadData forward declarations after boundary isolation.");
            AssertDoesNotContain(global, "HashEngineObserver *uiBridge;", "ThreadData still uses the UI-specific uiBridge field name in the phase-1 contract.");
            AssertDoesNotContain(global, "UIBridgeBase *uiBridge;", "ThreadData still directly depends on UIBridgeBase in the phase-1 contract.");
            AssertContains(global, "struct HashExecutionPreferenceState", "ThreadData baseline contract is missing the grouped execution-preference seam.");
            AssertContains(global, "struct HashCancellationState", "ThreadData baseline contract is missing the grouped cancellation seam.");
            AssertContains(global, "struct HashJobState", "ThreadData baseline contract is missing the grouped job-state seam.");
            AssertContains(global, "std::atomic<bool> working;", "ThreadData no longer carries the grouped working-state flag in the baseline contract.");
            AssertContains(global, "std::atomic<bool> stopRequested;", "ThreadData no longer carries the grouped stop flag in the baseline contract.");
            AssertContains(global, "bool uppercaseDigest;", "ThreadData no longer carries the grouped uppercase flag in the baseline contract.");
            AssertContains(global, "typedef HashResultList ResultList;", "Global.h no longer preserves the temporary ResultList compatibility alias while the legacy seams are being retired.");

            AssertContains(legacyThreadData, "struct ThreadDataInputState", "LegacyThreadData seam is missing the grouped input-state seam.");
            AssertContains(legacyThreadData, "struct ThreadDataExecutionState", "LegacyThreadData seam is missing the grouped execution-state seam.");
            AssertContains(legacyThreadData, "struct ThreadData", "LegacyThreadData seam is missing the root ThreadData contract.");
            AssertContains(legacyThreadData, "HashProgressSink *observer;", "ThreadData is not yet narrowed to a neutral HashProgressSink observer in the legacy seam.");
            AssertContains(legacyThreadData, "ThreadDataInputState inputState;", "ThreadData no longer carries the grouped input-state field in the legacy seam.");
            AssertContains(legacyThreadData, "ThreadDataExecutionState executionState;", "ThreadData no longer carries the grouped execution-state field in the legacy seam.");
            AssertContains(legacyThreadData, "uint32_t fileCount;", "ThreadData no longer carries nFiles in the legacy seam.");
            AssertContains(legacyThreadData, "TStrVector inputFiles;", "ThreadData no longer carries fullPaths in the legacy seam.");
            AssertContains(legacyThreadData, "HashExecutionPreferenceState preferences;", "ThreadData no longer carries the grouped execution-preference state field in the legacy seam.");
            AssertContains(legacyThreadData, "HashCancellationState cancellation;", "ThreadData no longer carries the grouped cancellation state field in the legacy seam.");
            AssertContains(legacyThreadData, "HashJobState jobState;", "ThreadData no longer carries the grouped job-state field in the legacy seam.");
            AssertDoesNotContain(legacyThreadData, "bool threadWorking;", "ThreadData still exposes the legacy threadWorking field name.");
            AssertDoesNotContain(legacyThreadData, "bool stop;", "ThreadData still exposes the legacy stop field name.");
            AssertDoesNotContain(legacyThreadData, "bool uppercase;", "ThreadData still exposes the legacy uppercase field name.");
            AssertDoesNotContain(legacyThreadData, "uint64_t totalSize;", "ThreadData still exposes the legacy totalSize field name.");
            AssertDoesNotContain(legacyThreadData, "uint32_t nFiles;", "ThreadData still exposes the legacy nFiles field name.");
            AssertDoesNotContain(legacyThreadData, "TStrVector fullPaths;", "ThreadData still exposes the legacy fullPaths field name.");
            AssertDoesNotContain(legacyThreadData, "ResultList resultList;", "ThreadData still exposes the legacy resultList field name.");
        }, failures);

        Run("Phase 1 keeps the core on HashProgressSink while moving adapter bridge semantics onto event-oriented callbacks", () =>
        {
            string observer = ReadRepoFile(repoRoot, @"trunk\source\Adapters\UiBridge\HashEngineObserver.h");
            string bridgeBase = ReadRepoFile(repoRoot, @"trunk\source\Adapters\UiBridge\HashEngineBridge.h");
            string legacyObserverPath = Path.Combine(repoRoot, @"trunk\source\Common\HashEngineObserver.h");
            string legacyBridgePath = Path.Combine(repoRoot, @"trunk\source\Common\HashEngineBridge.h");
            string legacyUiBridgeBasePath = Path.Combine(repoRoot, @"trunk\source\Common\UIBridgeBase.h");

            AssertContains(observer, "class HashProgressEventBridge", "Phase-1 observer seam is missing.");
            AssertContains(observer, "typedef HashProgressEventBridge HashEngineObserver;", "HashEngineObserver is not yet retained as a compatibility alias.");
            AssertContains(observer, "virtual void handleJobPreparingEvent() = 0;", "HashProgressEventBridge does not expose handleJobPreparingEvent.");
            AssertContains(observer, "virtual void handleJobPreparationFinishedEvent() = 0;", "HashProgressEventBridge does not expose handleJobPreparationFinishedEvent.");
            AssertContains(observer, "virtual void handleJobCancelledEvent() = 0;", "HashProgressEventBridge does not expose handleJobCancelledEvent.");
            AssertContains(observer, "virtual void handleJobCompletedEvent() = 0;", "HashProgressEventBridge does not expose handleJobCompletedEvent.");
            AssertContains(observer, "virtual void handleFileResultProgressEvent(const HashResult& result,", "HashProgressEventBridge does not yet expose a HashResult-based file-result event seam.");
            AssertContains(observer, "virtual int getProgressValueMax() = 0;", "HashProgressEventBridge does not expose getProgressValueMax.");
            AssertContains(observer, "virtual void handleFileProgressEvent(int value) = 0;", "HashProgressEventBridge does not expose handleFileProgressEvent.");
            AssertContains(observer, "virtual void handleTotalProgressEvent(int value) = 0;", "HashProgressEventBridge does not expose handleTotalProgressEvent.");
            AssertContains(observer, "virtual void handleFileCalculatedEvent() = 0;", "HashProgressEventBridge does not expose handleFileCalculatedEvent.");
            AssertContains(observer, "virtual void handleFileFinishedEvent() = 0;", "HashProgressEventBridge does not expose handleFileFinishedEvent.");
            AssertContains(observer, "handleFileResultProgressEvent(progressEvent.result, progressEvent.type, progressEvent.uppercaseDigest);", "HashProgressEventBridge does not yet route file-result lifecycle through the event-oriented callback.");
            AssertDoesNotContain(observer, "virtual void showFileName(const HashResult& result) = 0;", "HashEngineObserver still keeps the UI-specific showFileName contract.");
            AssertDoesNotContain(observer, "virtual void updateProgWhole(int value) = 0;", "HashEngineObserver still keeps the UI-specific updateProgWhole contract.");

            AssertContains(bridgeBase, "#include \"Adapters/UiBridge/HashEngineObserver.h\"", "HashEngineBridge does not yet layer directly on top of the shared adapter observer seam.");
            AssertContains(bridgeBase, "class HashUiBridgeAdapter: public HashProgressEventBridge", "HashUiBridgeAdapter is missing the neutral bridge seam on top of HashProgressEventBridge.");
            AssertContains(bridgeBase, "typedef HashUiBridgeAdapter HashEngineBridge;", "HashEngineBridge is not yet retained as a compatibility alias.");
            AssertContains(bridgeBase, "virtual void lockBridgeData() = 0;", "HashUiBridgeAdapter does not keep lockBridgeData on top of HashProgressEventBridge.");
            AssertContains(bridgeBase, "virtual void unlockBridgeData() = 0;", "HashUiBridgeAdapter does not keep unlockBridgeData on top of HashProgressEventBridge.");
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
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptWorkflow.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashSchedulerDispatch.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestSinglePass.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestUpdater.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressTracker.cpp"));
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
            AssertContains(fileRunner, "uint64_t CalculateFileChunkIterations(", "HashDigestQueue.cpp does not yet expose a tiny chunk-iteration helper.");
            AssertContains(engineImpl, "UpdateWholeProgressAfterFile(", "HashEngine implementation set does not yet expose a tiny whole-progress helper.");
            AssertContains(engineImpl, "PopulateDigestResult(", "HashEngine implementation set does not yet expose a tiny digest-population helper.");
            AssertContains(engineImpl, "typedef ResultDigestStorage FinalizedDigestBundle;", "HashEngine implementation set does not yet expose the finalized digest bundle introduced after phase 3.");
            AssertContainsAny(engineImpl,
                [
                    "GetFinalizedDigestValue(",
                    "GetFinalizedDigestValueById("
                ],
                "HashEngine implementation set does not yet expose the finalized-digest getter introduced after phase 3.");
            AssertContainsAny(engineImpl,
                [
                    "SetFinalizedDigestValue(",
                    "SetFinalizedDigestValueById("
                ],
                "HashEngine implementation set does not yet expose the finalized-digest setter introduced after phase 3.");
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
            AssertContains(engineImpl, "ThreadPool threadPool(GetHashSchedulerWorkerThreadCount(schedulerPlan));", "HashEngine implementation set no longer resolves scheduler worker-count through the scheduler-plan seam.");
            AssertContains(engineImpl, "ShouldPreScanHashRequestFileSizes(preparationPlan, request)", "HashEngine no longer performs the current small-batch pre-scan gate through the preparation-plan seam.");
            AssertContains(engineImpl, "VisitHashRequestFiles(request, [&](uint32_t fileIndex, const tstring& fullPath)", "HashEngine implementation set no longer routes input-file iteration through the HashRequest contract seam.");
            AssertContains(engineImpl, "AccumulatePreScannedFileSize(executionContext, request, fSizes, fileIndex);", "HashEngine no longer routes the small-batch pre-scan loop body through the tiny helper.");
            AssertContains(engine, "bool wasCancelled = false;", "HashEngine no longer tracks small-batch pre-scan cancellation through the local helper contract.");
            AssertContains(engine, "isSizeCaled = PrepareHashingWork(executionContext, request, GetHashJobPreparationPlan(executionPlan), fSizes, &wasCancelled);", "HashEngine no longer routes the preparation phase through the job preparation-plan seam.");
            AssertContains(engine, "if (wasCancelled)", "HashEngine no longer handles small-batch pre-scan cancellation via the helper result.");
            AssertContains(fileRunner, "YieldHashThread();", "HashFileRunner no longer routes per-file scheduler yielding through the tiny helper.");
            AssertContains(engineImpl, "std::vector<std::future<void>> digestUpdateTasks;", "HashEngine implementation set no longer fans out digest updates through generic worker-task vectors.");
            AssertContains(engineImpl, "digestUpdateTasks.push_back(threadPool->enqueue([&hashContexts, data, dataLen, operationDescriptor]()", "HashEngine implementation set no longer dispatches digest update tasks through operation descriptors.");
            AssertContains(engineImpl, "FileExecutionState executionState;", "HashEngine implementation set no longer creates the grouped file-execution state bundle.");
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
                    "uint64_t times = CalculateFileChunkIterations(fsize, preferredBufferLength);",
                    "ExecuteOpenedFileDigestUpdate(executionContext, digestRuntimePlan, fsize, isSizeCaled, executionState",
                    "return false;"
                ],
                "HashEngine opened-file processing helper no longer preserves the expected read-loop order.");
            AssertInOrder(fileRunner,
                [
                    "bool ProcessOpenedFileHashingSinglePass(",
                    "do",
                    "while (!isFileFinished && !executionState->fileAttemptState.readFailed);",
                    "return ShouldStopHashExecution(*executionContext);"
                ],
                "HashDigestSinglePass no longer preserves the expected single-thread read-loop order.");
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
                    "unsigned int bufferLength = NormalizeDigestDataBufferPreferredLength(preferredLength);",
                    "if (fileSize == 0)",
                    "return 1;",
                    "uint64_t adjustedFileSize = SaturatingAddUInt64(fileSize, static_cast<uint64_t>(bufferLength) - 1);",
                    "return adjustedFileSize / bufferLength;"
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
                    "return ExecuteHashPreparationWorkflow(executionContext, request, preparationPlan, fSizes, wasCancelled);"
                ],
                "HashEngine preparation helper no longer delegates orchestration through the preparation-workflow seam.");
            AssertInOrder(engineImpl,
                [
                    "ExecuteHashPreparationWorkflow(",
                    "observer->onProgressEvent(CreatePreparingProgressEvent());",
                    "bool isSizeCaled = TryPreScanSmallBatchFileSizes(executionContext, request, preparationPlan, fSizes, wasCancelled);",
                    "if (*wasCancelled)",
                    "observer->onProgressEvent(CreatePreparationFinishedProgressEvent());",
                    "return isSizeCaled;"
                ],
                "HashEngine preparation workflow seam no longer preserves the expected preparation order.");
            AssertInOrder(engineImpl,
                [
                    "TryPreScanSmallBatchFileSizes(",
                    "if (ShouldPreScanHashRequestFileSizes(preparationPlan, request))",
                    "RunHashPreScanVisitWorkflow(executionContext, request, fSizes, wasCancelled);",
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
                    "HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);",
                    "ExecuteCompletedHashingWorkflow(executionContext, observer);",
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
                    "fileAttemptState.osFile->close();",
                    "FinalizeDigestStrings(",
                    "UpdateWholeProgressAfterFile(",
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
                    "HashRuntime::OpenSslEvpHashContext openSslSha256;",
                    "HashRuntime::OpenSslEvpHashContext openSslSha512;"
                ],
                "HashEngine grouped file-hash context bundle no longer keeps the current algorithm contexts together.");
            AssertInOrder(engineImpl,
                [
                    "struct FileExecutionState",
                    "FileProgressState progressState;",
                    "FileAttemptState fileAttemptState;",
                    "FileHashContexts hashContexts;",
                    "HashJobExecutionPlan executionPlan;",
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
                    "isSizeCaled = PrepareHashingWork(executionContext, request, GetHashJobPreparationPlan(executionPlan), fSizes, &wasCancelled);",
                    "if (wasCancelled)",
                    "if (!RunHashScheduler(executionContext, request, executionPlan, isSizeCaled, fSizes))",
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
            string legacyThreadDataAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataAccess.h");
            string legacyThreadExecutionAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataExecutionAccess.h");
            string legacyThreadInputAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataInputAccess.h");
            string legacyThreadResultAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataResultAccess.h");
            string threadAccess = ReadLegacyThreadDataAccessSeams(repoRoot);
            string clrBridge = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpBridge = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");
            string mfcDialog = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            string mfcInitializationController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashInitializationController.cpp");
            string mfcResultViewController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashResultViewController.cpp");
            string mfcResultLifecycle = string.Join("\r\n", mfcDialog, mfcResultViewController);

            AssertFileMissing(repoRoot, @"trunk\source\Common\ThreadDataAccess.h", "Phase 5 Common ThreadDataAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\ThreadDataExecutionAccess.h", "Phase 5 Common ThreadDataExecutionAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\ThreadDataInputAccess.h", "Phase 5 Common ThreadDataInputAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\ThreadDataResultAccess.h", "Phase 5 Common ThreadDataResultAccess shim should be removed after the LegacyCompat boundary cleanup.");

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
            AssertContains(legacyThreadDataAccess, "ResetThreadDataInputFiles(threadData);", "ThreadDataAccess grouped reset helper does not yet reuse the input-file reset seam.");
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
            AssertContains(threadAccess, "GetMutableThreadDataHashJobState(threadData).countedSize = SaturatingAddUInt64(GetThreadDataTotalSize(threadData), sizeDelta);", "ThreadData access seams total-size increment helper does not yet use bounded uint64 accounting.");
            AssertContains(threadAccess, "GetMutableThreadDataHashJobState(threadData).countedSize = ReplaceSizedValueUInt64(GetThreadDataTotalSize(threadData), previousSize, currentSize);", "ThreadData access seams replacement-size helper does not yet use checked uint64 accounting.");
            AssertContains(threadAccess, "return GetThreadDataInputState(threadData).fileCount;", "ThreadData access seams file-count getter does not yet route through the grouped input-state seam.");
            AssertContains(threadAccess, "return GetThreadDataInputFiles(threadData)[fileIndex];", "ThreadData access seams grouped path getter does not yet route through the neutral ThreadData field name.");
            AssertContains(threadAccess, "return GetThreadDataHashJobState(threadData).results;", "ThreadData access seams grouped result-list getter does not yet route through the grouped job-state seam.");

            AssertContains(clrBridge, "#include \"LegacyCompat/ManagedHashMgmtAccess.h\"", "CLR bridge does not yet consume the LegacyCompat managed thread-data seam directly.");
            AssertContains(clrBridge, "SetThreadDataObserver(*m_pThreadData, m_pUiBridgeWUI);", "CLR bridge does not yet route observer assignment through ThreadDataAccess.");
            AssertContains(clrBridge, "ResetThreadDataForNewSession(*m_pThreadData);", "CLR bridge does not yet route Clear() through ThreadDataAccess.");
            AssertContains(clrBridge, "SetThreadDataStop(*m_pThreadData, val);", "CLR bridge does not yet route SetStop() through ThreadDataAccess.");
            AssertContains(clrBridge, "SetThreadDataUppercase(*m_pThreadData, val);", "CLR bridge does not yet route SetUppercase() through ThreadDataAccess.");
            AssertContains(clrBridge, "GetThreadDataTotalSize(*m_pThreadData);", "CLR bridge does not yet route GetTotalSize() through ThreadDataAccess.");
            AssertContains(clrBridge, "GetThreadDataResultCount(*m_pThreadData);", "CLR bridge does not yet route GetResultCount() through ThreadDataAccess.");
            AssertContains(clrBridge, "ReplaceThreadDataInputFilesFromManagedArray(*m_pThreadData, filePaths, ConvertManagedFilePathToTstr);", "CLR bridge does not yet route AddFiles() through the current compile-safe managed input-file helper.");

            AssertContains(uwpBridge, "#include \"LegacyCompat/ManagedHashMgmtAccess.h\"", "UWP bridge does not yet consume the LegacyCompat managed thread-data seam directly.");
            AssertContains(uwpBridge, "SetThreadDataObserver(m_threadData, m_spUiBridgeUwp.get());", "UWP bridge does not yet route observer assignment through ThreadDataAccess.");
            AssertContains(uwpBridge, "ResetThreadDataForNewSession(m_threadData);", "UWP bridge does not yet route Clear() through ThreadDataAccess.");
            AssertContains(uwpBridge, "SetThreadDataStop(m_threadData, val);", "UWP bridge does not yet route SetStop() through ThreadDataAccess.");
            AssertContains(uwpBridge, "SetThreadDataUppercase(m_threadData, val);", "UWP bridge does not yet route SetUppercase() through ThreadDataAccess.");
            AssertContains(uwpBridge, "GetThreadDataTotalSize(m_threadData);", "UWP bridge does not yet route GetTotalSize() through ThreadDataAccess.");
            AssertContains(uwpBridge, "ReplaceThreadDataInputFilesFromManagedArray(m_threadData, filePaths, ConvertManagedFilePathToTstr);", "UWP bridge does not yet route AddFiles() through the grouped managed input-file helper.");

            AssertContains(mfcDialog, "#include \"LegacyCompat/ThreadDataAccess.h\"", "MFC dialog does not yet consume the ThreadDataAccess seam from LegacyCompat.");
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
            string legacyDigestType = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ResultDigestTypeCompat.h");
            string hashAlgorithmRegistry = ReadHashAlgorithmRegistrySeams(repoRoot);
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

            AssertDoesNotContain(global, "enum ResultDigestType", "Global.h should no longer anchor the compatibility digest enum after the algorithm-id cleanup.");
            AssertContains(legacyDigestType, "enum ResultDigestType", "LegacyCompat should retain the digest enum for compatibility callers.");
            AssertContains(legacyDigestType, "RESULT_DIGEST_MD5", "LegacyCompat no longer exposes the MD5 digest slot.");
            AssertContains(legacyDigestType, "RESULT_DIGEST_SHA1", "LegacyCompat no longer exposes the SHA1 digest slot.");
            AssertContains(legacyDigestType, "RESULT_DIGEST_SHA256", "LegacyCompat no longer exposes the SHA256 digest slot.");
            AssertContains(legacyDigestType, "RESULT_DIGEST_SHA512", "LegacyCompat no longer exposes the SHA512 digest slot.");
            AssertContains(digestAccess, "GetResultDigestCount()", "ResultDigestAccess is missing the neutral digest count helper.");
            AssertContains(digestAccess, "GetResultDigestTypeAt(int index)", "ResultDigestAccess is missing the neutral digest order helper.");
            AssertContains(digestAccess, "GetResultDigestLabel(ResultDigestType digestType)", "ResultDigestAccess is missing the neutral digest label helper.");
            AssertContains(digestAccess, "GetResultDigest(const ResultData& result, ResultDigestType digestType)", "ResultDigestAccess is missing the neutral digest getter.");
            AssertContains(digestAccess, "TryGetMutableResultDigest(ResultData& result, ResultDigestType digestType, sunjwbase::tstring **digestValue)", "ResultDigestAccess is missing the explicit mutable digest lookup helper.");
            AssertContains(digestAccess, "SetResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)", "ResultDigestAccess is missing the neutral digest setter.");
            AssertContains(digestAccess, "ResultContainsDigest(const ResultData& result, const sunjwbase::tstring& digestText)", "ResultDigestAccess is missing the neutral digest search helper.");
            AssertContainsAny(digestAccess,
                [
                    "VisitResultDigests([&](ResultDigestType digestType)",
                    "VisitResultDigestIds([&](const HashAlgorithmId& algorithmId)"
                ],
                "ResultDigestAccess no longer routes digest search through the neutral digest iteration helper.");
            AssertContainsAny(digestAccess,
                [
                    "GetResultDigest(result, digestType).find(digestText)",
                    "GetResultDigestById(result, algorithmId).find(digestText)"
                ],
                "ResultDigestAccess no longer routes digest search through the neutral digest order helper.");

            AssertContains(engineImpl, "typedef ResultDigestStorage FinalizedDigestBundle;", "HashEngine does not yet centralize finalized digest strings through the finalized digest bundle.");
            AssertContainsAny(engineImpl,
                [
                    "VisitHashRequestAlgorithms(request, [&](ResultDigestType digestType)",
                    "VisitHashRequestAlgorithmIds(request, [&](const HashAlgorithmId& algorithmId)"
                ],
                "HashEngine no longer routes digest publishing through the request algorithm iteration helper.");
            AssertContainsAny(engineImpl,
                [
                    "GetFinalizedDigestValue(digestBundle, digestType)",
                    "GetFinalizedDigestValueById(digestBundle, algorithmId)"
                ],
                "HashEngine does not yet read finalized digest strings through the finalized digest seam.");
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
            AssertContains(resultNetProjection, "static inline TResultDataNet AssignResultDigestToNetById(TResultDataNet resultDataNet, const HashAlgorithmId& algorithmId, TResultString digestValue)", "ResultNetProjection does not yet expose the centralized ResultDataNet digest-assignment helper.");
            AssertContains(resultProjection, "template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter>", "ResultDataProjection does not yet expose the centralized ResultDataNet projection template.");
            AssertContains(resultProjection, "static inline TResultDataNet ProjectResultDataToNet(const ResultData& result, TStringConverter convertString)", "ResultDataProjection does not yet expose the centralized ResultDataNet projection helper.");
            AssertContains(resultProjection, "AssignHashResultDigestsToNet(resultDataNet, ProjectHashResult(result), convertString);", "ResultDataProjection does not yet route managed digest projection through HashResultProjection.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route HashResultNet projection through the centralized projection helper.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route HashResultNet projection through the dedicated managed bridge helper.");
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

            AssertContains(bridgeUwp, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route HashResultNet projection through the centralized projection helper.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route HashResultNet projection through the dedicated managed bridge helper.");
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
            AssertContains(digestAccess, "return TryGetMutableStoredResultDigest(result, digestType, digestValue);", "ResultDigestAccess does not yet expose explicit mutable access through the internal digest storage in phase 3.");
            AssertDoesNotContain(digestAccess, "GetMutableCompatibilityResultDigest(result, digestType) = digestValue;", "Digest writes still mirror back into removed compatibility fields.");

            AssertContainsAny(engineImpl,
                [
                    "const ResultDigestMetadata& digestMetadata = GetResultDigestMetadata(digestType);",
                    "TryGetResultDigestMetadataById(algorithmId, &digestMetadata)"
                ],
                "HashEngine no longer routes finalized digest metadata lookup through the digest metadata seam.");
            AssertContains(engineImpl, "result.digests.push_back(digestResult);", "HashEngine no longer routes finalized digest writes through the HashResult digest seam.");
            AssertContains(bridgeMfc, "VisitResultDigestDisplayValues(result, uppercase, [&](int index, const ResultDigestMetadata& digestMetadata, const ResultDigestDisplayInfo& digestDisplayInfo)", "MFC digest rendering no longer reads through the phase-5 formatted digest-display visitor seam.");
        }, failures);

        Run("Phase 3 resets digest storage through the neutral seam when a file result is created", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string engineImpl = ReadHashEngineImplementation(repoRoot);

            AssertContains(digestAccess, "ResetResultDigests(ResultData& result)", "ResultDigestAccess does not yet expose the digest-reset helper introduced in phase 3.");
            AssertContainsAny(digestAccess,
                [
                    "VisitResultDigests([&](ResultDigestType digestType)",
                    "VisitResultDigestIds([&](const HashAlgorithmId& algorithmId)"
                ],
                "ResultDigestAccess digest-reset helper does not yet iterate through the centralized visitor helper.");
            AssertContainsAny(digestAccess,
                [
                    "ClearStoredResultDigest(result, digestType);",
                    "ClearStoredResultDigestById(result, algorithmId);"
                ],
                "ResultDigestAccess digest-reset helper does not yet clear internal digest storage.");
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
            string hashAlgorithmRegistry = ReadHashAlgorithmRegistrySeams(repoRoot);
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(global, "std::vector<sunjwbase::tstring> values;", "ResultData internal digest storage is not yet routed through a registry-sized vector.");
            AssertContains(hashAlgorithmRegistry, "struct HashAlgorithmDescriptorRegistry", "HashAlgorithmRegistry does not yet wrap the descriptor table in a dedicated registry object.");
            AssertContains(hashAlgorithmRegistry, "GetHashAlgorithmDescriptorRegistry()", "HashAlgorithmRegistry does not yet expose the centralized descriptor-registry helper.");
            AssertContains(hashAlgorithmRegistry, "GetMutableHashAlgorithmDescriptorStorage()", "HashAlgorithmRegistry does not yet route descriptors through mutable registry storage.");
            AssertContains(hashAlgorithmRegistry, "RegisterHashAlgorithmDescriptor(const HashAlgorithmDescriptor& algorithmDescriptor)", "HashAlgorithmRegistry does not yet expose descriptor registration.");
            AssertContains(hashAlgorithmRegistry, "EnsureDefaultHashAlgorithmDescriptorsRegistered()", "HashAlgorithmRegistry does not yet isolate default algorithms behind a registration bootstrap seam.");
            AssertContains(digestAccess, "return GetRegisteredHashAlgorithmCount();", "ResultDigestAccess does not yet route digest count through the centralized registry-count helper.");
        }, failures);

        Run("Phase 3 routes direct internal digest-slot access through stored-digest helpers", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(digestAccess, "GetStoredResultDigest(const ResultData& result, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the const stored-digest helper introduced in phase 3.");
            AssertContains(digestAccess, "TryGetMutableStoredResultDigest(ResultData& result, ResultDigestType digestType, sunjwbase::tstring **digestValue)", "ResultDigestAccess does not yet expose the mutable stored-digest helper introduced in phase 3.");
            AssertContainsAny(digestAccess,
                [
                    "return GetDigestStorageValue(GetResultDigestStorage(result), digestType);",
                    "return GetStoredResultDigestById(result, GetHashAlgorithmId(digestType));"
                ],
                "ResultDigestAccess stored-digest helpers do not yet route into the dedicated internal digest storage struct.");
            AssertContains(digestAccess, "return GetStoredResultDigest(result, digestType);", "ResultDigestAccess getter does not yet read through the stored-digest helper.");
            AssertContains(digestAccess, "return TryGetMutableStoredResultDigest(result, digestType, digestValue);", "ResultDigestAccess mutable getter does not yet route through the stored-digest helper.");
            AssertContainsAny(digestAccess,
                [
                    "ClearDigestStorageValue(GetMutableResultDigestStorage(result), digestType);",
                    "ClearStoredResultDigestById(result, GetHashAlgorithmId(digestType));"
                ],
                "ResultDigestAccess digest-reset helper does not yet clear digests through the stored-digest helper.");
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
            string hashAlgorithmRegistry = ReadHashAlgorithmRegistrySeams(repoRoot);
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(hashAlgorithmRegistry, "struct HashAlgorithmDescriptor", "HashAlgorithmRegistry does not yet expose the centralized algorithm metadata struct introduced in phase 11.");
            AssertContains(digestAccess, "typedef HashAlgorithmDescriptor ResultDigestMetadata;", "ResultDigestAccess does not yet bridge digest metadata onto the centralized algorithm descriptor.");
            AssertContains(digestAccess, "GetResultDigestMetadataAt(int index)", "ResultDigestAccess does not yet expose the centralized digest metadata lookup helper introduced in phase 3.");
            AssertContains(digestAccess, "GetResultDigestMetadataSnapshot()", "ResultDigestAccess does not yet expose the centralized digest metadata snapshot helper introduced for stable traversal.");
            AssertContains(hashAlgorithmRegistry, "{ \"md5\", \"MD5 (Deprecated)\", true, false }", "HashAlgorithmRegistry metadata table does not yet map deprecated MD5.");
            AssertContains(hashAlgorithmRegistry, "{ \"sha1\", \"SHA1 (Deprecated)\", true, false }", "HashAlgorithmRegistry metadata table does not yet map deprecated SHA1.");
            AssertContains(hashAlgorithmRegistry, "{ \"openssl-sha-256\", \"SHA-256\", true, true }", "HashAlgorithmRegistry metadata table does not yet map OpenSSL SHA-256.");
            AssertContains(hashAlgorithmRegistry, "{ \"openssl-sha-512\", \"SHA-512\", true, true }", "HashAlgorithmRegistry metadata table does not yet map OpenSSL SHA-512.");
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
            AssertContainsAny(digestAccess,
                [
                    "return HasDigestStorageValue(GetResultDigestStorage(result), digestType);",
                    "return HasStoredResultDigestById(result, GetHashAlgorithmId(digestType));"
                ],
                "ResultDigestAccess stored-digest presence helper does not yet route through the stored-digest helper.");
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
            AssertContainsAny(digestAccess,
                [
                    "ClearStoredResultDigest(result, digestType);",
                    "ClearStoredResultDigestById(result, GetHashAlgorithmId(digestType));"
                ],
                "ResultDigestAccess reset helper does not yet route internal storage clearing through the dedicated helper.");
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
            AssertContains(digestAccess, "ResultDigestType digestType = GetResultDigestMetadataType(digestMetadata);", "ResultDigestAccess digest visitor helper does not yet route through the metadata type accessor.");
            AssertContains(digestAccess, "if (digestType == RESULT_DIGEST_UNKNOWN)", "ResultDigestAccess digest visitor helper does not yet guard legacy enum visitors against descriptor-only algorithms.");
            AssertContainsAny(digestAccess,
                [
                    "VisitResultDigests([&](ResultDigestType digestType)",
                    "VisitResultDigestIds([&](const HashAlgorithmId& algorithmId)"
                ],
                "ResultDigestAccess does not yet route digest iteration through the centralized visitor helper.");
            AssertContains(digestAccess, "return true;", "ResultDigestAccess digest visitor usage no longer preserves the current early-success semantics.");
        }, failures);

        Run("Phase 3 routes metadata iteration through a dedicated metadata visitor helper", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);

            AssertContains(digestAccess, "template<typename TResultDigestMetadataVisitor>", "ResultDigestAccess does not yet expose the metadata-visitor template introduced in phase 3.");
            AssertContains(digestAccess, "VisitResultDigestMetadata(TResultDigestMetadataVisitor visitor)", "ResultDigestAccess does not yet expose the centralized metadata visitor helper.");
            AssertContains(digestAccess, "std::vector<ResultDigestMetadata> digestMetadataSnapshot = GetResultDigestMetadataSnapshot();", "ResultDigestAccess metadata visitor helper does not yet bind traversal to a stable digest metadata snapshot.");
            AssertContains(digestAccess, "visitor(static_cast<int>(index), digestMetadataSnapshot[index])", "ResultDigestAccess metadata visitor helper does not yet route through the centralized digest metadata snapshot.");
            AssertContains(digestAccess, "VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)", "ResultDigestAccess does not yet route metadata iteration through the centralized metadata visitor helper.");
            AssertContains(digestAccess, "ResultDigestType digestType = GetResultDigestMetadataType(digestMetadata);", "ResultDigestAccess digest visitor does not yet route through the metadata visitor helper.");
            AssertContains(digestAccess, "return GetHashAlgorithmIndex(digestType);", "ResultDigestAccess digest-index helper does not yet route through the centralized registry seam.");
        }, failures);

        Run("Phase 3 routes digest-value iteration through a dedicated value visitor helper and reuses it in managed bridges", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string resultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDataProjection.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContainsAny(digestAccess,
                [
                    "template<typename TResultDigestValueVisitor>",
                    "template<typename TResultDigestMetadataValueVisitor>"
                ],
                "ResultDigestAccess does not yet expose the digest-value visitor template introduced in phase 3.");
            AssertContainsAny(digestAccess,
                [
                    "VisitResultDigestValues(const ResultData& result, TResultDigestValueVisitor visitor)",
                    "VisitResultDigestMetadataValues(const ResultData& result, TResultDigestMetadataValueVisitor visitor)"
                ],
                "ResultDigestAccess does not yet expose the centralized digest-value visitor helper.");
            AssertContainsAny(digestAccess,
                [
                    "return VisitResultDigests([&](ResultDigestType digestType)",
                    "return VisitResultDigestMetadata([&](int index, const ResultDigestMetadata& digestMetadata)"
                ],
                "ResultDigestAccess digest-value visitor helper does not yet route through the centralized digest visitor helper.");
            AssertContainsAny(digestAccess,
                [
                    "return visitor(digestType, GetResultDigest(result, digestType));",
                    "return visitor(index, digestMetadata, GetResultDigestById(result, algorithmId));"
                ],
                "ResultDigestAccess digest-value visitor helper does not yet feed values through the neutral digest seam.");

            AssertContains(resultProjection, "AssignHashResultDigestsToNet(resultDataNet, ProjectHashResult(result), convertString);", "ResultDataProjection does not yet consume digest values through HashResultProjection when projecting managed result data.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet consume digest values through the centralized HashResultNet projection helper.");
            AssertDoesNotContain(bridgeWui, "String^ digestValue = ConvertTstrToSystemString(GetResultDigest(result, digestType).c_str());", "WinUI bridge still performs inline digest lookup instead of consuming the digest-value visitor payload.");

            AssertContains(bridgeUwp, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet consume digest values through the centralized HashResultNet projection helper.");
            AssertDoesNotContain(bridgeUwp, "String^ digestValue = ConvertToPlatStr(GetResultDigest(result, digestType).c_str());", "UWP bridge still performs inline digest lookup instead of consuming the digest-value visitor payload.");
        }, failures);

        Run("Phase 3 promotes ResultDigestStorage into a reusable neutral storage seam and reuses it for finalized digest bundles", () =>
        {
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string engineImpl = ReadHashEngineImplementation(repoRoot);

            AssertContains(digestAccess, "GetDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the reusable digest-storage getter seam.");
            AssertContains(digestAccess, "TryGetMutableDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType, sunjwbase::tstring **digestValue)", "ResultDigestAccess does not yet expose the reusable mutable digest-storage seam.");
            AssertContains(digestAccess, "HasDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the reusable digest-storage presence seam.");
            AssertContains(digestAccess, "SetDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType, const sunjwbase::tstring& digestValue)", "ResultDigestAccess does not yet expose the reusable digest-storage write seam.");
            AssertContains(digestAccess, "ClearDigestStorageValue(ResultDigestStorage& digestStorage, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the reusable digest-storage clear seam.");
            AssertContains(digestAccess, "EnsureDigestStorageSize(digestStorage);", "ResultDigestAccess reusable digest-storage seam does not yet size storage from the registry before mutation.");
            AssertContains(digestAccess, "return digestStorage.values[digestIndex];", "ResultDigestAccess reusable digest-storage seam does not yet route through the centralized digest index.");
            AssertDoesNotContain(digestAccess, "GetInvalidDigestStorageScratch()", "ResultDigestAccess still relies on invalid digest scratch storage instead of explicit failure handling.");
            AssertContainsAny(digestAccess,
                [
                    "return GetDigestStorageValue(GetResultDigestStorage(result), digestType);",
                    "return GetStoredResultDigestById(result, GetHashAlgorithmId(digestType));"
                ],
                "ResultDigestAccess stored-digest getter does not yet reuse the neutral digest-storage seam.");
            AssertContainsAny(digestAccess,
                [
                    "return TryGetMutableDigestStorageValue(GetMutableResultDigestStorage(result), digestType, digestValue);",
                    "return TryGetMutableStoredResultDigestById(result, GetHashAlgorithmId(digestType), digestValue);"
                ],
                "ResultDigestAccess mutable stored-digest getter does not yet reuse the neutral digest-storage seam.");
            AssertContainsAny(digestAccess,
                [
                    "return HasDigestStorageValue(GetResultDigestStorage(result), digestType);",
                    "return HasStoredResultDigestById(result, GetHashAlgorithmId(digestType));"
                ],
                "ResultDigestAccess stored-digest presence helper does not yet reuse the neutral digest-storage seam.");
            AssertContainsAny(digestAccess,
                [
                    "SetDigestStorageValue(GetMutableResultDigestStorage(result), digestType, digestValue);",
                    "SetStoredResultDigestById(result, GetHashAlgorithmId(digestType), digestValue);"
                ],
                "ResultDigestAccess stored-digest setter does not yet reuse the neutral digest-storage seam.");
            AssertContainsAny(digestAccess,
                [
                    "ClearDigestStorageValue(GetMutableResultDigestStorage(result), digestType);",
                    "ClearStoredResultDigestById(result, GetHashAlgorithmId(digestType));"
                ],
                "ResultDigestAccess stored-digest clear helper does not yet reuse the neutral digest-storage seam.");

            AssertContains(engineImpl, "typedef ResultDigestStorage FinalizedDigestBundle;", "HashEngine does not yet reuse ResultDigestStorage as the finalized digest bundle.");
            AssertContainsAny(engineImpl,
                [
                    "GetDigestStorageValue(digestBundle, digestType)",
                    "GetDigestStorageValueById(digestBundle, algorithmId)"
                ],
                "HashEngine finalized-digest getter does not yet reuse the neutral digest-storage seam.");
            AssertContainsAny(engineImpl,
                [
                    "SetDigestStorageValue(digestBundle, digestType, digestValue);",
                    "SetDigestStorageValueById(digestBundle, algorithmId, digestValue);"
                ],
                "HashEngine finalized-digest setter does not yet reuse the neutral digest-storage seam.");
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
            AssertContainsAny(digestAccess,
                [
                    "return GetDigestStorageValue(GetResultDigestStorage(result), digestType);",
                    "return GetStoredResultDigestById(result, GetHashAlgorithmId(digestType));"
                ],
                "ResultDigestAccess stored-digest getter does not yet route through the result-digest-storage helper.");
            AssertContainsAny(digestAccess,
                [
                    "return TryGetMutableDigestStorageValue(GetMutableResultDigestStorage(result), digestType, digestValue);",
                    "return TryGetMutableStoredResultDigestById(result, GetHashAlgorithmId(digestType), digestValue);"
                ],
                "ResultDigestAccess mutable stored-digest getter does not yet route through the result-digest-storage helper.");
            AssertContainsAny(digestAccess,
                [
                    "return HasDigestStorageValue(GetResultDigestStorage(result), digestType);",
                    "return HasStoredResultDigestById(result, GetHashAlgorithmId(digestType));"
                ],
                "ResultDigestAccess stored-digest presence helper does not yet route through the result-digest-storage helper.");
            AssertContainsAny(digestAccess,
                [
                    "SetDigestStorageValue(GetMutableResultDigestStorage(result), digestType, digestValue);",
                    "SetStoredResultDigestById(result, GetHashAlgorithmId(digestType), digestValue);"
                ],
                "ResultDigestAccess stored-digest setter does not yet route through the result-digest-storage helper.");
            AssertContainsAny(digestAccess,
                [
                    "ClearDigestStorageValue(GetMutableResultDigestStorage(result), digestType);",
                    "ClearStoredResultDigestById(result, GetHashAlgorithmId(digestType));"
                ],
                "ResultDigestAccess stored-digest clear helper does not yet route through the result-digest-storage helper.");
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
            AssertContainsAny(digestAccess,
                [
                    "return visitor(index, digestMetadata, GetResultDigest(result, GetResultDigestMetadataType(digestMetadata)));",
                    "return visitor(index, digestMetadata, GetResultDigestById(result, algorithmId));"
                ],
                "ResultDigestAccess digest-metadata-value visitor does not yet feed value lookups through the metadata type accessor.");
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

            AssertContains(bridgeWui, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route non-digest reads through the centralized HashResultNet projection helper.");

            AssertContains(bridgeUwp, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route non-digest reads through the centralized HashResultNet projection helper.");

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
            AssertContains(engineImpl, "result.meta.version.clear();", "HashEngine does not yet clear eager file-version metadata from the hot path.");
            AssertDoesNotContain(engineImpl, "tstrFileVersion = ResolveHashFileVersion(osFile, path);", "HashEngine still resolves file versions synchronously inside the hot metadata path.");
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
            AssertContains(bridgeWui, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet read HashResultState through the centralized HashResultNet projection helper.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet read HashResultState through the centralized HashResultNet projection helper.");
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
            AssertContains(bridgeMfcHeader, "void RequestRefreshMainText()", "UIBridgeMFC does not yet expose the coalesced main-hyperedit refresh helper.");
            AssertContains(bridgeMfcHeader, "PostThreadInfoMessage(WP_REFRESH_TEXT);", "UIBridgeMFC main-hyperedit refresh-message helper does not yet route refresh notifications through the centralized thread-info seam.");
            AssertContains(bridgeMfcHeader, "InterlockedCompareExchange(&m_refreshPending, 1, 0) == 0", "UIBridgeMFC main-text refresh helper does not yet coalesce duplicate refresh requests.");
            AssertContains(bridgeMfcHeader, "void UpdateMainHyperEdit(TAppendAction appendAction, bool refreshAfterUpdate = false)", "UIBridgeMFC does not yet expose the generalized main-hyperedit update helper.");
            AssertContains(bridgeMfcHeader, "void AppendToMainHyperEditAndRefresh(TAppendAction appendAction)", "UIBridgeMFC does not yet expose the dedicated main-hyperedit refresh helper.");
            AssertContains(bridgeMfcHeader, "appendAction(m_mainHyperEdit);", "UIBridgeMFC main-hyperedit refresh helper does not yet forward the hyper-edit instance through the callback.");
            AssertContains(bridgeMfcHeader, "UpdateMainHyperEdit(appendAction, true);", "UIBridgeMFC main-hyperedit refresh helper does not yet compose through the generalized update helper.");
            AssertContains(bridgeMfcHeader, "RequestRefreshMainText();", "UIBridgeMFC main-hyperedit refresh helper does not yet centralize refresh notifications through the dedicated refresh-message helper.");
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

            AssertDoesNotContain(bridgeMfc, "lockBridgeData();\r\n\t{\r\n\t\tAppendFileNameToHyperEdit(result, m_mainHyperEdit);", "UIBridgeMFC still inlines the showFileName refresh flow instead of using the dedicated helper.");
            AssertDoesNotContain(bridgeMfc, "lockBridgeData();\r\n\t{\r\n\t\tAppendFileMetaToHyperEdit(result, m_mainHyperEdit);", "UIBridgeMFC still inlines the showFileMeta refresh flow instead of using the dedicated helper.");
            AssertDoesNotContain(bridgeMfc, "lockBridgeData();\r\n\t{\r\n\t\tAppendFileHashToHyperEdit(result, uppercase, m_mainHyperEdit);", "UIBridgeMFC still inlines the showFileHash refresh flow instead of using the dedicated helper.");
            AssertDoesNotContain(bridgeMfc, "lockBridgeData();\r\n\t{\r\n\t\tAppendFileErrToHyperEdit(result, m_mainHyperEdit);", "UIBridgeMFC still inlines the showFileErr refresh flow instead of using the dedicated helper.");
            AssertDoesNotContain(bridgeMfc, "::PostMessage(m_hWnd, WM_THREAD_INFO, WP_REFRESH_TEXT, 0);", "UIBridgeMFC still posts refresh notifications inline instead of using the dedicated refresh-message helper.");
            AssertDoesNotContain(bridgeMfc, "lockBridgeData();\r\n\t{\r\n\t\tm_tstrNoPreparing = m_mainHyperEdit->GetTextBuffer().GetBuffer();", "UIBridgeMFC still mutates the preparing buffer inline instead of using the generalized main-hyperedit update helper.");
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
            AssertContains(bridgeWui, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route showFile* methods through the common managed projection-dispatch helper.");
            AssertContains(bridgeWui, "m_hashUiEvents->PublishFileStarted(hashResultNet);", "WinUI bridge no longer forwards projected file-name results through the current delegate path.");
            AssertContains(bridgeWui, "m_hashUiEvents->PublishFileMetadata(hashResultNet);", "WinUI bridge no longer forwards projected file-meta results through the current delegate path.");
            AssertContains(bridgeWui, "m_hashUiEvents->PublishFileHash(hashResultNet, hashUppercase);", "WinUI bridge no longer forwards projected file-hash results through the current delegate path.");
            AssertContains(bridgeWui, "m_hashUiEvents->PublishFileError(hashResultNet);", "WinUI bridge no longer forwards projected file-error results through the current delegate path.");
            AssertDoesNotContain(bridgeWuiHeader, "void ProjectManagedResultAndDispatch(const ResultData& result, TResultHandler resultHandler)", "WinUI bridge still keeps the local managed projection-dispatch template instead of using the centralized seam.");
            AssertDoesNotContain(bridgeWui, "ResultDataNet resultDataNet = ConvertResultDataToNet(result);", "WinUI bridge still inlines projected result creation inside showFile* methods instead of using the dedicated helper.");

            AssertContains(bridgeUwpHeader, "#include \"Common/ManagedBridgeDispatch.h\"", "UWP bridge header does not yet include the common managed-bridge dispatch header.");
            AssertDoesNotContain(bridgeUwpHeader, "#include \"Common/ManagedBridgeHelpers.h\"", "UWP bridge header still depends on the deprecated managed-bridge helper header.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route showFile* methods through the common managed projection-dispatch helper.");
            AssertContains(bridgeUwp, "m_hashUiEvents->PublishFileStarted(hashResultNet);", "UWP bridge no longer forwards projected file-name results through the current delegate path.");
            AssertContains(bridgeUwp, "m_hashUiEvents->PublishFileMetadata(hashResultNet);", "UWP bridge no longer forwards projected file-meta results through the current delegate path.");
            AssertContains(bridgeUwp, "m_hashUiEvents->PublishFileHash(hashResultNet, hashUppercase);", "UWP bridge no longer forwards projected file-hash results through the current delegate path.");
            AssertContains(bridgeUwp, "m_hashUiEvents->PublishFileError(hashResultNet);", "UWP bridge no longer forwards projected file-error results through the current delegate path.");
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

            AssertContains(managedDispatch, "enum ManagedResultEventType", "Common managed-bridge dispatch header does not yet expose the dedicated managed result-dispatch type.");
            AssertContains(managedDispatch, "DispatchManagedResultEventByType(ManagedResultEventType eventType, TResultDataNet resultDataNet, bool uppercase", "Common managed-bridge dispatch header does not yet expose the centralized managed result-dispatch helper.");
            AssertDoesNotContain(managedDispatch, "DispatchManagedBridgeResultEventByType(const ResultData& result, ManagedResultEventType eventType, bool uppercase", "Common managed-bridge dispatch header still exposes the deprecated ResultData-based managed result-dispatch wrapper.");
            AssertContains(managedDispatch, "DispatchManagedResultEventByType(eventType, resultDataNet", "Common managed-bridge dispatch header does not yet compose managed result dispatch through the lower-level dispatch seam.");
            AssertContains(managedDispatch, "onFileStarted, onFileMetadata, onFileHash, onFileError", "Common managed-bridge dispatch header does not yet compose managed result dispatch through the lower-level dispatch seam.");
            AssertContains(bridgeWuiHeader, "#include \"Common/ManagedBridgeDispatch.h\"", "WinUI bridge header does not yet include the common managed-bridge dispatch header.");
            AssertDoesNotContain(bridgeWuiHeader, "#include \"Common/ManagedBridgeHelpers.h\"", "WinUI bridge header still includes the deprecated managed-bridge helper header.");
            AssertContains(bridgeWuiHeader, "void DispatchProjectedResultEvent(const HashResult& result, ManagedResultEventType eventType, bool uppercase = false);", "WinUI bridge does not yet expose the dedicated managed result-dispatch helper.");
            AssertContains(bridgeWui, "void UIBridgeWUI::DispatchProjectedResultEvent(const HashResult& result, ManagedResultEventType eventType, bool uppercase)", "WinUI bridge does not yet implement the dedicated managed result-dispatch helper.");
            AssertContains(bridgeWui, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge managed result-dispatch helper does not yet route delegate forwarding through the common managed-bridge helper.");
            AssertContains(bridgeWui, "void UIBridgeWUI::handleFileResultProgressEvent(const HashResult& result,", "WinUI bridge does not yet route file-result events through the dedicated managed result-dispatch helper.");
            AssertContains(bridgeWui, "DispatchProjectedResultEvent(result,", "WinUI bridge does not yet route file-result events through the dedicated managed result-dispatch helper.");
            AssertContains(bridgeWui, "GetManagedResultEventType(eventType)", "WinUI bridge does not yet derive managed dispatch from ProgressEventType.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::showFileName(const HashResult& result)", "WinUI bridge still exposes the old showFileName bridge method.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::showFileHash(const HashResult& result, bool uppercase)", "WinUI bridge still exposes the old showFileHash bridge method.");

            AssertContains(bridgeUwpHeader, "#include \"Common/ManagedBridgeDispatch.h\"", "UWP bridge header does not yet include the common managed-bridge dispatch header.");
            AssertDoesNotContain(bridgeUwpHeader, "#include \"Common/ManagedBridgeHelpers.h\"", "UWP bridge header still includes the deprecated managed-bridge helper header.");
            AssertContains(bridgeUwpHeader, "void DispatchProjectedResultEvent(const HashResult& result, ManagedResultEventType eventType, bool uppercase = false);", "UWP bridge does not yet expose the dedicated managed result-dispatch helper.");
            AssertContains(bridgeUwp, "void UIBridgeUwp::DispatchProjectedResultEvent(const HashResult& result, ManagedResultEventType eventType, bool uppercase)", "UWP bridge does not yet implement the dedicated managed result-dispatch helper.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "UWP bridge managed result-dispatch helper does not yet route delegate forwarding through the common managed-bridge helper.");
            AssertContains(bridgeUwp, "void UIBridgeUwp::handleFileResultProgressEvent(const HashResult& result,", "UWP bridge does not yet route file-result events through the dedicated managed result-dispatch helper.");
            AssertContains(bridgeUwp, "DispatchProjectedResultEvent(result,", "UWP bridge does not yet route file-result events through the dedicated managed result-dispatch helper.");
            AssertContains(bridgeUwp, "GetManagedResultEventType(eventType)", "UWP bridge does not yet derive managed dispatch from ProgressEventType.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::showFileName(const HashResult& result)", "UWP bridge still exposes the old showFileName bridge method.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::showFileHash(const HashResult& result, bool uppercase)", "UWP bridge still exposes the old showFileHash bridge method.");
        }, failures);

        Run("Phase 5 routes managed bridge delegate forwarding through dedicated bridge helpers", () =>
        {
            string managedDispatch = ReadRepoFile(repoRoot, @"trunk\source\Common\ManagedBridgeDispatch.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(managedDispatch, "DispatchManagedBridgeLifecycleEventByType(ManagedBridgeLifecycleEventType eventType, int value", "Common managed-bridge dispatch header does not yet expose the shared bridge-level delegate-action wrapper.");
            AssertContains(managedDispatch, "DispatchManagedBridgeQueryByType(ManagedBridgeQueryType queryType, TProgressValueMaxQuery queryProgressValueMax)", "Common managed-bridge dispatch header does not yet expose the shared bridge-level delegate-query wrapper.");
            AssertDoesNotContain(managedDispatch, "ProjectManagedBridgeResultAndDispatch", "Common managed-bridge dispatch header still keeps the redundant bridge-level projection wrapper.");
            AssertContains(bridgeWui, "DispatchManagedBridgeLifecycleEventByType(eventType, value, [&]()", "WinUI bridge does not yet route delegate actions through the common forwarding helper.");
            AssertContains(bridgeWui, "return DispatchManagedBridgeQueryByType<int>(queryType, [&]()", "WinUI bridge does not yet route delegate queries through the common forwarding helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::preparingCalc()\r\n{\r\n\tm_hashUiEvents->NotifyJobPreparing();\r\n}", "WinUI bridge still forwards NotifyJobPreparing inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::removeNotifyJobPreparing()\r\n{\r\n\tm_hashUiEvents->RemoveNotifyJobPreparing();\r\n}", "WinUI bridge still forwards RemoveNotifyJobPreparing inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::calcStop()\r\n{\r\n\tm_hashUiEvents->NotifyJobCancelled();\r\n}", "WinUI bridge still forwards NotifyJobCancelled inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::calcFinish()\r\n{\r\n\tm_hashUiEvents->NotifyJobCompleted();\r\n}", "WinUI bridge still forwards NotifyJobCompleted inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeWui, "int UIBridgeWUI::getProgMax()\r\n{\r\n\treturn m_hashUiEvents->GetProgressValueMax();\r\n}", "WinUI bridge still forwards GetProgressValueMax inline instead of using the dedicated delegate-query helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::updateProgWhole(int value)\r\n{\r\n\tm_hashUiEvents->PublishTotalProgress(value);\r\n}", "WinUI bridge still forwards PublishTotalProgress inline instead of using the dedicated delegate-action helper.");

            AssertContains(bridgeUwp, "DispatchManagedBridgeLifecycleEventByType(eventType, value, [&]()", "UWP bridge does not yet route delegate actions through the common forwarding helper.");
            AssertContains(bridgeUwp, "return DispatchManagedBridgeQueryByType<int>(queryType, [&]()", "UWP bridge does not yet route delegate queries through the common forwarding helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::preparingCalc()\r\n{\r\n\tm_hashUiEvents->NotifyJobPreparing();\r\n}", "UWP bridge still forwards NotifyJobPreparing inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::removeNotifyJobPreparing()\r\n{\r\n\tm_hashUiEvents->RemoveNotifyJobPreparing();\r\n}", "UWP bridge still forwards RemoveNotifyJobPreparing inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::calcStop()\r\n{\r\n\tm_hashUiEvents->NotifyJobCancelled();\r\n}", "UWP bridge still forwards NotifyJobCancelled inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::calcFinish()\r\n{\r\n\tm_hashUiEvents->NotifyJobCompleted();\r\n}", "UWP bridge still forwards NotifyJobCompleted inline instead of using the dedicated delegate-action helper.");
            AssertDoesNotContain(bridgeUwp, "int UIBridgeUwp::getProgMax()\r\n{\r\n\treturn m_hashUiEvents->GetProgressValueMax();\r\n}", "UWP bridge still forwards GetProgressValueMax inline instead of using the dedicated delegate-query helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::updateProgWhole(int value)\r\n{\r\n\tm_hashUiEvents->PublishTotalProgress(value);\r\n}", "UWP bridge still forwards PublishTotalProgress inline instead of using the dedicated delegate-action helper.");
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

            AssertContains(managedDispatch, "enum ManagedBridgeQueryType", "Common managed-bridge dispatch header does not yet expose the dedicated delegate-query type.");
            AssertContains(managedDispatch, "DispatchManagedQueryByType(ManagedBridgeQueryType queryType, TProgressValueMaxQuery queryProgressValueMax)", "Common managed-bridge dispatch header does not yet expose the centralized delegate-query helper.");
            AssertContains(managedDispatch, "DispatchManagedBridgeQueryByType(ManagedBridgeQueryType queryType, TProgressValueMaxQuery queryProgressValueMax)", "Common managed-bridge dispatch header does not yet expose the bridge-level delegate-query wrapper.");
            AssertContains(managedDispatch, "return DispatchManagedQueryByType(queryType, [&]()", "Common managed-bridge dispatch header does not yet compose delegate-query forwarding through the dispatch seam.");
            AssertDoesNotContain(managedDispatch, "return ForwardManagedDelegateQuery<TResult>([&]()", "Common managed-bridge dispatch header still keeps the redundant delegate-query forwarding wrapper in the bridge-level helper.");
            AssertContains(bridgeWuiHeader, "int DispatchBridgeQuery(ManagedBridgeQueryType queryType);", "WinUI bridge does not yet expose the dedicated delegate-query dispatch helper.");
            AssertContains(bridgeWui, "int UIBridgeWUI::DispatchBridgeQuery(ManagedBridgeQueryType queryType)", "WinUI bridge does not yet implement the dedicated delegate-query dispatch helper.");
            AssertContains(bridgeWui, "return DispatchManagedBridgeQueryByType<int>(queryType, [&]()", "WinUI bridge delegate-query dispatch helper does not yet route queries through the bridge-level helper.");
            AssertContains(bridgeWui, "return DispatchBridgeQuery(MANAGED_BRIDGE_QUERY_PROGRESS_VALUE_MAX);", "WinUI bridge does not yet route GetProgressValueMax through the dedicated delegate-query dispatch helper.");
            AssertDoesNotContain(bridgeWui, "int UIBridgeWUI::getProgMax()\r\n{\r\n\treturn DispatchDelegateQuery<int>([&]()", "WinUI bridge still keeps GetProgressValueMax's inline delegate-query lambda instead of routing through the dedicated query-type helper.");

            AssertContains(bridgeUwpHeader, "int DispatchBridgeQuery(ManagedBridgeQueryType queryType);", "UWP bridge does not yet expose the dedicated delegate-query dispatch helper.");
            AssertContains(bridgeUwp, "int UIBridgeUwp::DispatchBridgeQuery(ManagedBridgeQueryType queryType)", "UWP bridge does not yet implement the dedicated delegate-query dispatch helper.");
            AssertContains(bridgeUwp, "return DispatchManagedBridgeQueryByType<int>(queryType, [&]()", "UWP bridge delegate-query dispatch helper does not yet route queries through the bridge-level helper.");
            AssertContains(bridgeUwp, "return DispatchBridgeQuery(MANAGED_BRIDGE_QUERY_PROGRESS_VALUE_MAX);", "UWP bridge does not yet route GetProgressValueMax through the dedicated delegate-query dispatch helper.");
            AssertDoesNotContain(bridgeUwp, "int UIBridgeUwp::getProgMax()\r\n{\r\n\treturn DispatchDelegateQuery<int>([&]()", "UWP bridge still keeps GetProgressValueMax's inline delegate-query lambda instead of routing through the dedicated query-type helper.");
        }, failures);

        Run("Phase 5 routes managed bridge delegate actions through dedicated action-type dispatch helpers", () =>
        {
            string managedDispatch = ReadRepoFile(repoRoot, @"trunk\source\Common\ManagedBridgeDispatch.h");
            string bridgeWuiHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.h");
            string bridgeWui = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
            string bridgeUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.h");
            string bridgeUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");

            AssertContains(managedDispatch, "enum ManagedBridgeLifecycleEventType", "Common managed-bridge dispatch header does not yet expose the dedicated delegate-action type.");
            AssertContains(managedDispatch, "DispatchManagedLifecycleEventByType(ManagedBridgeLifecycleEventType eventType, int value", "Common managed-bridge dispatch header does not yet expose the centralized delegate-action helper.");
            AssertContains(managedDispatch, "DispatchManagedBridgeLifecycleEventByType(ManagedBridgeLifecycleEventType eventType, int value", "Common managed-bridge dispatch header does not yet expose the bridge-level delegate-action wrapper.");
            AssertContains(managedDispatch, "DispatchManagedLifecycleEventByType(eventType, value, [&]()", "Common managed-bridge dispatch header does not yet compose delegate-action forwarding through the dispatch seam.");
            AssertDoesNotContain(managedDispatch, "ForwardManagedDelegateAction([&]()", "Common managed-bridge dispatch header still keeps the redundant delegate-action forwarding wrapper in the bridge-level helper.");
            AssertContains(bridgeWuiHeader, "void DispatchBridgeLifecycleEvent(ManagedBridgeLifecycleEventType eventType, int value = 0);", "WinUI bridge does not yet expose the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeWui, "void UIBridgeWUI::DispatchBridgeLifecycleEvent(ManagedBridgeLifecycleEventType eventType, int value)", "WinUI bridge does not yet implement the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeWui, "DispatchManagedBridgeLifecycleEventByType(eventType, value, [&]()", "WinUI bridge delegate-action dispatch helper does not yet route actions through the bridge-level helper.");
            AssertContains(bridgeWui, "DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_PREPARING);", "WinUI bridge does not yet route NotifyJobPreparing through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeWui, "DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_PREPARATION_FINISHED);", "WinUI bridge does not yet route RemoveNotifyJobPreparing through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeWui, "DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_CANCELLED);", "WinUI bridge does not yet route NotifyJobCancelled through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeWui, "DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_COMPLETED);", "WinUI bridge does not yet route NotifyJobCompleted through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeWui, "DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_TOTAL_PROGRESS, value);", "WinUI bridge does not yet route PublishTotalProgress through the dedicated delegate-action dispatch helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::preparingCalc()\r\n{\r\n\tDispatchDelegateAction([&]()", "WinUI bridge still keeps NotifyJobPreparing's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::removeNotifyJobPreparing()\r\n{\r\n\tDispatchDelegateAction([&]()", "WinUI bridge still keeps RemoveNotifyJobPreparing's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::calcStop()\r\n{\r\n\tDispatchDelegateAction([&]()", "WinUI bridge still keeps NotifyJobCancelled's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::calcFinish()\r\n{\r\n\tDispatchDelegateAction([&]()", "WinUI bridge still keeps NotifyJobCompleted's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeWui, "void UIBridgeWUI::updateProgWhole(int value)\r\n{\r\n\tDispatchDelegateAction([&]()", "WinUI bridge still keeps PublishTotalProgress's inline delegate-action lambda instead of routing through the dedicated action-type helper.");

            AssertContains(bridgeUwpHeader, "void DispatchBridgeLifecycleEvent(ManagedBridgeLifecycleEventType eventType, int value = 0);", "UWP bridge does not yet expose the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeUwp, "void UIBridgeUwp::DispatchBridgeLifecycleEvent(ManagedBridgeLifecycleEventType eventType, int value)", "UWP bridge does not yet implement the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeUwp, "DispatchManagedBridgeLifecycleEventByType(eventType, value, [&]()", "UWP bridge delegate-action dispatch helper does not yet route actions through the bridge-level helper.");
            AssertContains(bridgeUwp, "DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_PREPARING);", "UWP bridge does not yet route NotifyJobPreparing through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeUwp, "DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_PREPARATION_FINISHED);", "UWP bridge does not yet route RemoveNotifyJobPreparing through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeUwp, "DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_CANCELLED);", "UWP bridge does not yet route NotifyJobCancelled through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeUwp, "DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_COMPLETED);", "UWP bridge does not yet route NotifyJobCompleted through the dedicated delegate-action dispatch helper.");
            AssertContains(bridgeUwp, "DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_TOTAL_PROGRESS, value);", "UWP bridge does not yet route PublishTotalProgress through the dedicated delegate-action dispatch helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::preparingCalc()\r\n{\r\n\tDispatchDelegateAction([&]()", "UWP bridge still keeps NotifyJobPreparing's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::removeNotifyJobPreparing()\r\n{\r\n\tDispatchDelegateAction([&]()", "UWP bridge still keeps RemoveNotifyJobPreparing's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::calcStop()\r\n{\r\n\tDispatchDelegateAction([&]()", "UWP bridge still keeps NotifyJobCancelled's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::calcFinish()\r\n{\r\n\tDispatchDelegateAction([&]()", "UWP bridge still keeps NotifyJobCompleted's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
            AssertDoesNotContain(bridgeUwp, "void UIBridgeUwp::updateProgWhole(int value)\r\n{\r\n\tDispatchDelegateAction([&]()", "UWP bridge still keeps PublishTotalProgress's inline delegate-action lambda instead of routing through the dedicated action-type helper.");
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
            AssertContains(bridgeWui, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "WinUI bridge does not yet route HashResultStateNet assignment through the centralized HashResultNet projection helper.");
            AssertDoesNotContain(bridgeWui, "switch (GetResultState(result))", "WinUI bridge still inlines ResultStateNet conversion instead of using the dedicated helper.");
            AssertDoesNotContain(bridgeWui, "static ResultStateNet ConvertResultStateToNet(ResultState resultState)", "WinUI bridge still keeps a local ResultStateNet conversion helper instead of using the centralized ResultDataAccess helper.");

            AssertContains(bridgeUwp, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)", "UWP bridge does not yet route HashResultStateNet assignment through the centralized HashResultNet projection helper.");
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
            AssertContains(resultNetProjection, "DispatchResultDigestValueById(const HashAlgorithmId& algorithmId, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)", "ResultNetProjection does not yet expose the grouped descriptor-id dispatch helper.");
            AssertContains(resultNetProjection, "switch (resultState)", "ResultNetProjection ResultStateNet conversion helper does not yet use the compile-safe explicit ResultState switch.");
            AssertContains(resultNetProjection, "return TResultStateNet::ResultPath;", "ResultNetProjection ResultStateNet conversion helper does not yet map RESULT_PATH through the compile-safe explicit switch.");
            AssertContains(resultNetProjection, "IsResultDigestStableNameById(const HashAlgorithmId& algorithmId, const char *stableName)", "ResultNetProjection digest assignment helper does not yet route descriptor-id checks through stable-name metadata.");
            AssertContains(resultNetProjection, "DispatchResultDigestValueById(algorithmId,", "ResultNetProjection digest assignment helper does not yet route assignments through the grouped digest dispatch helper.");
            AssertContains(resultNetProjection, "resultDataNet.MD5 = digestValue;", "ResultNetProjection digest assignment helper does not yet map MD5 through the grouped digest dispatch helper.");

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
            string hashAlgorithmRegistry = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashAlgorithmRegistryCore.h");

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
            string hashAlgorithmRegistry = ReadHashAlgorithmRegistrySeams(repoRoot);
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string digestRender = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestRender.h");
            string bridgeMfc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");

            AssertContains(hashAlgorithmRegistry, "bool requiresDigestOperations;", "HashAlgorithmRegistry does not yet expose the descriptor-level digest-operation requirement flag.");
            AssertDoesNotContain(hashAlgorithmRegistry, "ResultDigestType type;", "HashAlgorithmRegistry still keeps the legacy enum-backed descriptor identity in the core seam.");
            AssertDoesNotContain(hashAlgorithmRegistry, "compatibilityValueField", "HashAlgorithmRegistry still carries compatibility-field pointers in the core descriptor.");

            AssertContains(digestAccess, "ResultDigestType digestType = GetResultDigestMetadataType(digestMetadata);", "ResultDigestAccess digest visitor does not yet route through the neutral ResultDigestMetadata type accessor.");
            AssertContainsAny(digestAccess,
                [
                    "return visitor(index, digestMetadata, GetResultDigest(result, GetResultDigestMetadataType(digestMetadata)));",
                    "return visitor(index, digestMetadata, GetResultDigestById(result, algorithmId));"
                ],
                "ResultDigestAccess digest metadata-value visitor does not yet route through the neutral ResultDigestMetadata type accessor.");
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

            AssertContains(bridgeBase, "#include \"Adapters/UiBridge/HashEngineObserver.h\"", "HashUiBridgeAdapter does not yet layer directly on top of the adapter observer seam.");
            AssertContains(bridgeBase, "class HashUiBridgeAdapter: public HashProgressEventBridge", "HashUiBridgeAdapter is missing.");
            AssertContains(bridgeBase, "typedef HashUiBridgeAdapter HashEngineBridge;", "HashEngineBridge compatibility alias is missing.");
            if (File.Exists(compatibilityBridgePath))
            {
                failures.Add("UIBridgeBase still exists even though the shared adapter bridge seam should have replaced it.");
            }

            AssertContains(bridgeMfcHeader, "#include \"Adapters/UiBridge/HashEngineBridge.h\"", "MFC bridge header does not yet include the shared adapter HashUiBridgeAdapter seam.");
            AssertContains(bridgeMfcHeader, "class UIBridgeMFC: public HashUiBridgeAdapter", "MFC bridge does not yet inherit HashUiBridgeAdapter directly.");
            AssertDoesNotContain(bridgeMfcHeader, "#include \"Common/UIBridgeBase.h\"", "MFC bridge header still depends directly on the compatibility shim.");
            AssertDoesNotContain(bridgeMfcHeader, "class UIBridgeMFC: public UIBridgeBase", "MFC bridge still inherits the compatibility shim instead of HashEngineBridge.");

            AssertContains(bridgeWuiHeader, "#include \"Adapters/UiBridge/HashEngineBridge.h\"", "WinUI bridge header does not yet include the shared adapter HashUiBridgeAdapter seam.");
            AssertContains(bridgeWuiHeader, "class UIBridgeWUI : public HashUiBridgeAdapter", "WinUI bridge does not yet inherit HashUiBridgeAdapter directly.");
            AssertDoesNotContain(bridgeWuiHeader, "#include \"Common/UIBridgeBase.h\"", "WinUI bridge header still depends directly on the compatibility shim.");
            AssertDoesNotContain(bridgeWuiHeader, "class UIBridgeWUI : public UIBridgeBase", "WinUI bridge still inherits the compatibility shim instead of HashEngineBridge.");

            AssertContains(bridgeUwpHeader, "#include \"Adapters/UiBridge/HashEngineBridge.h\"", "UWP bridge header does not yet include the shared adapter HashUiBridgeAdapter seam.");
            AssertContains(bridgeUwpHeader, "class UIBridgeUwp : public HashUiBridgeAdapter", "UWP bridge does not yet inherit HashUiBridgeAdapter directly.");
            AssertDoesNotContain(bridgeUwpHeader, "#include \"Common/UIBridgeBase.h\"", "UWP bridge header still depends directly on the compatibility shim.");
            AssertDoesNotContain(bridgeUwpHeader, "class UIBridgeUwp : public UIBridgeBase", "UWP bridge still inherits the compatibility shim instead of HashEngineBridge.");

            AssertContains(bridgeMacHeader, "#include \"Adapters/UiBridge/HashEngineBridge.h\"", "macOS bridge header does not yet include the shared adapter HashUiBridgeAdapter seam.");
            AssertContains(bridgeMacHeader, "class UIBridgeMacSwift: public HashUiBridgeAdapter", "macOS bridge does not yet inherit HashUiBridgeAdapter directly.");
            AssertDoesNotContain(bridgeMacHeader, "#include \"Common/UIBridgeBase.h\"", "macOS bridge header still depends directly on the compatibility shim.");
            AssertDoesNotContain(bridgeMacHeader, "class UIBridgeMacSwift: public UIBridgeBase", "macOS bridge still inherits the compatibility shim instead of HashEngineBridge.");
        }, failures);

        Run("Phase 8 introduces thread-scoped hash algorithm selection while keeping the current four algorithms enabled by default", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string threadAccess = ReadLegacyThreadDataAccessSeams(repoRoot);
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string digestRender = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestRender.h");
            string engineImpl = ReadHashEngineImplementation(repoRoot);

            AssertContains(global, "struct HashAlgorithmSelectionState", "Global.h does not yet expose the grouped hash-algorithm selection state introduced in phase 8.");
            AssertContains(global, "std::vector<bool> enabled;", "Hash-algorithm selection state does not yet store the current enabled flags through a registry-sized vector.");
            AssertContains(global, "HashAlgorithmSelectionState hashAlgorithms;", "ThreadData execution state does not yet carry the hash-algorithm selection state.");

            AssertContainsAny(threadAccess,
                [
                    "#include \"Domain/HashAlgorithmRegistryCore.h\"",
                    "#include \"LegacyCompat/HashAlgorithmTypeCompat.h\""
                ],
                "ThreadData access seams do not yet include the hash-algorithm registry seam needed for algorithm selection.");
            AssertContains(threadAccess, "GetThreadDataHashAlgorithmSelectionState(const ThreadData& threadData)", "ThreadDataAccess does not yet expose the const hash-algorithm selection helper.");
            AssertContains(threadAccess, "GetMutableThreadDataHashAlgorithmSelectionState(ThreadData& threadData)", "ThreadDataAccess does not yet expose the mutable hash-algorithm selection helper.");
            AssertContains(threadAccess, "SetThreadDataHashAlgorithmEnabled(ThreadData& threadData, ResultDigestType digestType, bool enabled)", "ThreadDataAccess does not yet expose the hash-algorithm enable/disable helper.");
            AssertContains(threadAccess, "IsThreadDataHashAlgorithmEnabled(const ThreadData& threadData, ResultDigestType digestType)", "ThreadDataAccess does not yet expose the hash-algorithm enabled-state helper.");
            AssertContains(threadAccess, "VisitEnabledThreadDataHashAlgorithms(const ThreadData& threadData, THashAlgorithmVisitor visitor)", "ThreadDataAccess does not yet expose the enabled-hash-algorithm visitor seam.");
            AssertContains(threadAccess, "VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)", "ThreadDataAccess does not yet route algorithm iteration through the registry seam.");
            AssertContains(threadAccess, "ResetThreadDataHashAlgorithms(ThreadData& threadData)", "ThreadDataAccess does not yet expose the default hash-algorithm reset helper.");
            AssertContains(threadAccess, "ResetThreadDataHashAlgorithms(threadData);", "New ThreadData sessions do not yet reset hash algorithms to the default enabled set.");
            AssertContains(threadAccess, "hashAlgorithmSelectionState.enabled.assign(registeredAlgorithmCount, false);", "ThreadDataAccess does not yet initialize a new algorithm-selection vector through the registry-sized seam.");
            AssertContains(threadAccess, "hashAlgorithmSelectionState.enabled[static_cast<size_t>(index)] = IsHashAlgorithmDescriptorEnabledByDefault(algorithmDescriptor);", "ThreadDataAccess does not yet seed default algorithm selection from descriptor metadata.");

            AssertContains(engineImpl, "InitializeFileHashing(const HashRequest& request, HashExecutionContext *executionContext, FileHashContexts *hashContexts)", "HashEngine does not yet thread the algorithm-selection state into file-hashing initialization.");
            AssertContainsAny(engineImpl,
                [
                    "VisitHashRequestAlgorithms(request, [&](ResultDigestType digestType)",
                    "VisitHashRequestAlgorithmIds(request, [&](const HashAlgorithmId& algorithmId)"
                ],
                "HashEngine does not yet route digest initialization/finalization/publication through the HashRequest algorithm seam.");
            AssertContains(engineImpl, "FinalizeDigestStrings(request, executionState.hashContexts, executionState.digestBundle, &finalizeErrorText)", "HashEngine does not yet finalize digests through the request-scoped algorithm-selection seam.");
            AssertContains(engineImpl, "PopulateDigestResult(request, result, executionState.digestBundle);", "HashEngine does not yet publish digests through the request-scoped algorithm-selection seam.");
            AssertContains(engineImpl, "VisitDigestUpdateRequestOperations(digestUpdateRequest, [&](const HashDigestOperationDescriptor& operationDescriptor)", "HashEngine does not yet route digest updates through operation-descriptor iteration seams.");
            AssertContains(engineImpl, "if (!result.digests.empty())", "HashEngine does not yet suppress hash-result publication when no algorithms are enabled.");

            AssertContains(digestAccess, "HasResultDigest(const ResultData& result, ResultDigestType digestType)", "ResultDigestAccess does not yet expose the result-digest presence helper needed for selective rendering.");
            AssertContains(digestAccess, "HasAnyResultDigests(const ResultData& result)", "ResultDigestAccess does not yet expose the grouped result-digest presence helper.");
            AssertContainsAny(digestRender,
                [
                    "if (!HasResultDigest(result, GetResultDigestMetadataType(digestMetadata)))",
                    "if (!HasResultDigestById(result, algorithmId))"
                ],
                "ResultDigestRender formatted display visitor does not yet skip disabled or absent digest values.");
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

            AssertContains(hashMgmtClrHeader, "void ResetHashAlgorithms();", "CLR bridge does not yet expose the managed algorithm reset helper.");
            AssertContains(hashMgmtClr, "ResetThreadDataHashAlgorithms(*m_pThreadData);", "CLR bridge does not yet reset native algorithm selections from the managed seam.");
            AssertContains(hashMgmtClrHeader, "void SetHashAlgorithmEnabledById(System::String^ algorithmId, bool val);", "CLR bridge does not yet expose the descriptor-id algorithm enable helper.");
            AssertContains(hashMgmtClrHeader, "bool GetHashAlgorithmEnabledById(System::String^ algorithmId);", "CLR bridge does not yet expose the descriptor-id algorithm query helper.");
            AssertContains(hashMgmtClrHeader, "property System::String^ AlgorithmId;", "CLR bridge does not yet expose descriptor ids on managed algorithm descriptors.");
            AssertDoesNotContain(hashMgmtClrHeader, "public enum class HashAlgorithmTypeNet", "CLR bridge still exposes the removed managed hash-algorithm enum.");
            AssertDoesNotContain(hashMgmtClrHeader, "property int DigestType;", "CLR bridge still exposes removed digest-type descriptor payload.");
            AssertContains(hashMgmtClr, "::SetManagedHashAlgorithmEnabledById(*m_pThreadData, ConvertManagedAlgorithmIdToTstr(algorithmId), val);", "CLR bridge does not yet forward algorithm enablement through the shared managed id helper.");
            AssertContains(hashMgmtClr, "if (!HasEnabledThreadDataHashAlgorithms(*m_pThreadData))", "CLR bridge does not yet reject zero-algorithm hash starts.");

            AssertContains(hashMgmtUwpHeader, "void ResetHashAlgorithms();", "UWP bridge does not yet expose the managed algorithm reset helper.");
            AssertContains(hashMgmtUwp, "ResetThreadDataHashAlgorithms(m_threadData);", "UWP bridge does not yet reset native algorithm selections from the managed seam.");
            AssertContains(hashMgmtUwpHeader, "void SetHashAlgorithmEnabledById(Platform::String^ algorithmId, Platform::Boolean val);", "UWP bridge does not yet expose the descriptor-id algorithm enable helper.");
            AssertContains(hashMgmtUwpHeader, "Platform::Boolean GetHashAlgorithmEnabledById(Platform::String^ algorithmId);", "UWP bridge does not yet expose the descriptor-id algorithm query helper.");
            AssertContains(hashMgmtUwpHeader, "property Platform::String^ AlgorithmId;", "UWP bridge does not yet expose descriptor ids on managed algorithm descriptors.");
            AssertDoesNotContain(hashMgmtUwpHeader, "public enum class HashAlgorithmTypeNet", "UWP bridge still exposes the removed managed hash-algorithm enum.");
            AssertDoesNotContain(hashMgmtUwpHeader, "property int DigestType;", "UWP bridge still exposes removed digest-type descriptor payload.");
            AssertContains(hashMgmtUwp, "::SetManagedHashAlgorithmEnabledById(m_threadData, ConvertManagedAlgorithmIdToTstr(algorithmId), val);", "UWP bridge does not yet forward algorithm enablement through the shared managed id helper.");
            AssertContains(hashMgmtUwp, "if (!HasEnabledThreadDataHashAlgorithms(m_threadData))", "UWP bridge does not yet reject zero-algorithm hash starts.");

            AssertContains(winUiXaml, "StackPanelHashAlgorithms", "WinUI page does not yet expose the dynamic hash-algorithm container.");
            AssertContains(winUiPage, "KeyHashAlgorithmPrefix", "WinUI page does not yet persist dynamic hash-algorithm toggle state.");
            AssertContains(winUiPage, "UpdateHashAlgorithmStat(bool saveLocalSetting = true)", "WinUI page does not yet synchronize algorithm selections into HashMgmt.");
            AssertContains(winUiPage, "ValidateHashAlgorithmSelectionAsync()", "WinUI page does not yet validate algorithm selection before start.");
            AssertContains(winUiPage, "if (!await ValidateHashAlgorithmSelectionAsync())", "WinUI page does not yet block zero-algorithm starts.");
            AssertContains(winUiPage, "m_mainWindow.HashMgmt.ResetHashAlgorithms();", "WinUI page does not yet reset managed selections before reapplying the current checkbox state.");
            AssertContains(winUiPage, "LoadHashAlgorithmControls()", "WinUI page does not yet materialize dynamic hash-algorithm controls.");
            AssertContains(winUiPage, "m_mainWindow.HashMgmt.GetSupportedHashAlgorithms();", "WinUI page does not yet fetch supported algorithms from the managed seam.");
            AssertContains(winUiPage, "SetHashAlgorithmEnabledById(GetHashAlgorithmId(hashAlgorithm), hashAlgorithmEnabled);", "WinUI page does not yet forward dynamic hash-algorithm selections through descriptor ids.");
            AssertContains(winUiPage, "private static string GetHashAlgorithmId(HashAlgorithmDescriptorNet hashAlgorithm)", "WinUI page does not yet normalize dynamic hash-algorithm ids.");
            AssertContains(winUiPage, "private Dictionary<string, CheckBox> m_hashAlgorithmCheckBoxes = [];", "WinUI page does not yet key algorithm checkboxes by descriptor id.");
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
            AssertContains(uwpPage, "SetHashAlgorithmEnabledById(GetHashAlgorithmId(hashAlgorithm), hashAlgorithmEnabled);", "UWP page does not yet forward dynamic hash-algorithm selections through descriptor ids.");
            AssertContains(uwpPage, "private static string GetHashAlgorithmId(HashAlgorithmDescriptorNet hashAlgorithm)", "UWP page does not yet normalize dynamic hash-algorithm ids.");
            AssertContains(uwpPage, "private Dictionary<string, CheckBox> m_hashAlgorithmCheckBoxes = new Dictionary<string, CheckBox>();", "UWP page does not yet key algorithm checkboxes by descriptor id.");
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
            AssertDoesNotContain(mfcRc, "IDC_CHECK_SHA256", "MFC resources should no longer include the removed legacy SHA256 checkbox.");
            AssertDoesNotContain(mfcRc, "IDC_CHECK_SHA512", "MFC resources should no longer include the removed legacy SHA512 checkbox.");
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
            AssertContains(hashMgmtClr, "#include \"LegacyCompat/ManagedHashMgmtAccess.h\"", "CLR search bridge does not yet consume the shared managed hash-management seam after the phase 9 projection/search split.");
            AssertContains(hashMgmtUwp, "#include \"LegacyCompat/ManagedHashMgmtAccess.h\"", "UWP search bridge does not yet consume the shared managed hash-management seam after the phase 9 projection/search split.");
        }, failures);

        Run("Phase 10 splits HashEngine preparation, result-finalization, and result-publication helpers into dedicated implementation files", () =>
        {
            string engine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string fileRunner = string.Join(
                "\r\n",
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileRunner.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptWorkflow.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashSchedulerDispatch.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestSinglePass.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestUpdater.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressTracker.cpp"));
            string scheduler = ReadRepoFile(repoRoot, @"trunk\source\Common\HashScheduler.cpp");
            string engineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string hashThreadEntry = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\HashThreadEntry.cpp");
            string legacyHashThreadEntryProjection = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\HashThreadEntryProjection.h");
            string legacyHashThreadEntryRuntime = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\HashThreadEntryRuntime.h");
            string enginePreparation = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp");
            string engineResult = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp");
            string resultPublisher = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");

            AssertContains(engine, "#include \"Common/HashEngineInternal.h\"", "HashEngine.cpp does not yet consume the new internal HashEngine split seam.");
            AssertContains(fileRunner, "bool ProcessOpenedFileHashing(", "HashDigestPipeline.cpp no longer owns the opened-file read/update orchestration.");
            AssertDoesNotContain(engine, "int WINAPI HashThreadFunc(void *param)", "HashEngine.cpp should no longer own the thread orchestration entry point after the thread-entry split.");
            AssertContains(hashThreadEntry, "int WINAPI HashThreadFunc(void *param)", "HashThreadEntry.cpp does not yet own the thread orchestration entry point after the thread-entry split.");
            AssertContains(hashThreadEntry, "#include \"LegacyCompat/HashThreadEntryRuntime.h\"", "HashThreadEntry.cpp does not yet consume the legacy thread-entry runtime seam.");
            AssertDoesNotContain(hashThreadEntry, "#include \"Common/HashRequestProjection.h\"", "HashThreadEntry.cpp should not include HashRequestProjection directly after the thread-entry projection seam split.");
            AssertDoesNotContain(hashThreadEntry, "#include \"Common/ThreadDataExecutionAccess.h\"", "HashThreadEntry.cpp should not include ThreadData execution access directly after the thread-entry projection seam split.");
            AssertContains(hashThreadEntry, "return RunLegacyHashThread(param);", "HashThreadEntry.cpp should delegate thread execution to legacy runtime seam.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\HashThreadEntry.cpp", "Phase 10 Common HashThreadEntry.cpp should be removed after the LegacyCompat boundary cleanup.");
            AssertContains(legacyHashThreadEntryRuntime, "HashRequest request = CreateThreadDataHashRequest(*thrdData);", "Legacy HashThreadEntry runtime does not yet project ThreadData into HashRequest.");
            AssertContains(legacyHashThreadEntryRuntime, "HashExecutionContext executionContext = CreateThreadDataHashExecutionContext(*thrdData);", "Legacy HashThreadEntry runtime does not yet inject HashExecutionContext through the projection seam.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\HashThreadEntryProjection.h", "Phase 10 Common HashThreadEntryProjection shim should be removed after the LegacyCompat boundary cleanup.");
            AssertContains(legacyHashThreadEntryProjection, "#include \"LegacyCompat/HashRequestProjection.h\"", "Legacy HashThreadEntryProjection does not yet layer on top of HashRequestProjection.");
            AssertContains(legacyHashThreadEntryProjection, "#include \"LegacyCompat/ThreadDataExecutionAccess.h\"", "Legacy HashThreadEntryProjection does not yet layer on top of ThreadDataExecutionAccess.");
            AssertContains(legacyHashThreadEntryProjection, "CreateThreadDataHashExecutionContext(ThreadData& threadData)", "Legacy HashThreadEntryProjection does not yet expose the ThreadData-to-HashExecutionContext projection seam.");
            AssertContains(legacyHashThreadEntryProjection, "CreateThreadDataHashRequest(const ThreadData& threadData)", "Legacy HashThreadEntryProjection does not yet expose the ThreadData-to-HashRequest projection seam.");
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
            AssertDoesNotContain(engineResult, "void InitializeFileHashing(", "HashEngineResult.cpp still owns file-hashing initialization after lifecycle extraction.");
            AssertDoesNotContain(engineResult, "FinalizeDigestStrings(", "HashEngineResult.cpp still owns digest finalization after lifecycle extraction.");
            AssertContains(resultPublisher, "void CompleteSuccessfulFileHashing(", "HashResultPublisher.cpp does not yet own successful-file completion.");
            AssertContains(resultPublisher, "void CompleteFileAttempt(", "HashResultPublisher.cpp does not yet own file-attempt completion.");
            AssertContains(fileRunner, "bool RunFileHashAttempt(", "HashFileRunner.cpp does not yet own the single-file execution seam.");
            AssertContains(scheduler, "FileExecutionState executionState;", "HashScheduler.cpp does not yet preserve grouped file-execution state for the runner seam.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileRunner.cpp", "Desktop native core project does not yet compile HashFileRunner.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\LegacyCompat\HashThreadEntry.cpp", "Desktop native core project does not yet compile HashThreadEntry.cpp from LegacyCompat.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashEnginePreparation.cpp", "Desktop native core project does not yet compile HashEnginePreparation.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashEngineResult.cpp", "Desktop native core project does not yet compile HashEngineResult.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashResultPublisher.cpp", "Desktop native core project does not yet compile HashResultPublisher.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashEngineInternal.h", "Desktop native core project does not yet include HashEngineInternal.h.");
            AssertContains(nativeProject, "<SolutionDir Condition=\"'$(SolutionDir)'==''\">$(ProjectDir)..\\..\\trunk\\</SolutionDir>", "Desktop native core project is missing the standalone SolutionDir fallback required by the direct CI build.");
            AssertContains(nativeProject, "<FHashRuntimeSuffix Condition=\"'$(FHashDynamicRuntime)'=='true'\">-md</FHashRuntimeSuffix>", "Desktop native core project is missing the runtime-variant suffix required for CLR-compatible WinUI builds.");
            AssertContains(nativeProject, @"$(ProjectDir);$(ProjectDir)..\..\trunk\source\;$(SolutionDir)source\", "Desktop native core project is missing the standalone include-root fallback required by the direct CI build.");
            AssertContains(nativeProject, @"$(ProjectDir)..\..\third_party\blake3\1.8.4\c", "Desktop native core project is missing the fixed-version BLAKE3 vendor include root.");
            AssertContains(nativeProject, "<UseOfMfc Condition=\"'$(FHashDynamicRuntime)'=='true'\">Dynamic</UseOfMfc>", "Desktop native core project is missing the CLR-compatible shared-MFC override.");
            AssertContains(nativeProject, "<RuntimeLibrary Condition=\"'$(FHashDynamicRuntime)'=='true'\">MultiThreadedDLL</RuntimeLibrary>", "Desktop native core project is missing the CLR-compatible dynamic runtime override.");
            AssertContains(nativeProject, @"$(MSBuildProjectName)$(FHashRuntimeSuffix)", "Desktop native core project does not yet route output directories through the runtime-variant suffix.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileRunner.cpp", "Desktop native core filters do not yet expose HashFileRunner.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\LegacyCompat\HashThreadEntry.cpp", "Desktop native core filters do not yet expose HashThreadEntry.cpp from LegacyCompat.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashEnginePreparation.cpp", "Desktop native core filters do not yet expose HashEnginePreparation.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashEngineResult.cpp", "Desktop native core filters do not yet expose HashEngineResult.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashResultPublisher.cpp", "Desktop native core filters do not yet expose HashResultPublisher.cpp.");

            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashFileRunner.cpp", "WinUI native project still compiles HashFileRunner.cpp instead of consuming fHashNativeCore.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\LegacyCompat\HashThreadEntry.cpp", "WinUI native project still compiles HashThreadEntry.cpp instead of consuming fHashNativeCore.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashEnginePreparation.cpp", "WinUI native project still compiles HashEnginePreparation.cpp instead of consuming fHashNativeCore.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashEngineResult.cpp", "WinUI native project still compiles HashEngineResult.cpp instead of consuming fHashNativeCore.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileRunner.cpp", "UWP native project does not yet compile HashFileRunner.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\LegacyCompat\HashThreadEntry.cpp", "UWP native project does not yet compile HashThreadEntry.cpp from LegacyCompat.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashEnginePreparation.cpp", "UWP native project does not yet compile HashEnginePreparation.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashEngineResult.cpp", "UWP native project does not yet compile HashEngineResult.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashResultPublisher.cpp", "UWP native project does not yet compile HashResultPublisher.cpp.");
        }, failures);

        Run("Phase 11 introduces a dedicated hash-algorithm registry seam while keeping the current four built-in algorithms intact", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string hashAlgorithmRegistry = ReadHashAlgorithmRegistrySeams(repoRoot);
            string digestAccess = ReadResultDigestAccessSeams(repoRoot);
            string threadAccess = ReadLegacyThreadDataAccessSeams(repoRoot);

            AssertContains(global, "std::vector<bool> enabled;", "Global.h does not yet route algorithm-selection state through a registry-sized vector.");

            AssertContains(hashAlgorithmRegistry, "struct HashAlgorithmDescriptor", "HashAlgorithmRegistry does not yet expose the dedicated hash-algorithm descriptor.");
            AssertContains(hashAlgorithmRegistry, "bool requiresDigestOperations;", "HashAlgorithmRegistry does not yet expose the digest-operation requirement field.");
            AssertContains(hashAlgorithmRegistry, "const char *stableName;", "HashAlgorithmRegistry does not yet expose the stable algorithm name field.");
            AssertContains(hashAlgorithmRegistry, "const char *displayLabel;", "HashAlgorithmRegistry does not yet expose the display label field.");
            AssertContains(hashAlgorithmRegistry, "bool enabledByDefault;", "HashAlgorithmRegistry does not yet expose default-enabled metadata for registry descriptors.");
            AssertContains(hashAlgorithmRegistry, "GetRegisteredHashAlgorithmCount()", "HashAlgorithmRegistry does not yet expose the registry-count helper.");
            AssertContains(hashAlgorithmRegistry, "VisitRegisteredHashAlgorithms(THashAlgorithmVisitor visitor)", "HashAlgorithmRegistry does not yet expose the algorithm visitor seam.");
            AssertContains(hashAlgorithmRegistry, "{ \"md5\", \"MD5 (Deprecated)\", true, false }", "HashAlgorithmRegistry does not yet register deprecated MD5.");
            AssertContains(hashAlgorithmRegistry, "{ \"sha1\", \"SHA1 (Deprecated)\", true, false }", "HashAlgorithmRegistry does not yet register deprecated SHA1.");
            AssertContains(hashAlgorithmRegistry, "{ \"openssl-sha-256\", \"SHA-256\", true, true }", "HashAlgorithmRegistry does not yet register OpenSSL SHA-256.");
            AssertContains(hashAlgorithmRegistry, "{ \"openssl-sha-512\", \"SHA-512\", true, true }", "HashAlgorithmRegistry does not yet register OpenSSL SHA-512.");

            AssertContains(digestAccess, "#include \"Domain/HashAlgorithmRegistryCore.h\"", "ResultDigestAccess does not yet layer on top of the hash-algorithm registry seam.");
            AssertContains(digestAccess, "typedef HashAlgorithmDescriptor ResultDigestMetadata;", "ResultDigestAccess does not yet bridge digest metadata onto the new registry descriptor.");
            AssertContains(digestAccess, "return GetRegisteredHashAlgorithmCount();", "ResultDigestAccess does not yet route digest count through the registry seam.");
            AssertContains(digestAccess, "return GetHashAlgorithmDescriptorAt(index);", "ResultDigestAccess does not yet route metadata lookup through the registry seam.");
            AssertContains(digestAccess, "return GetHashAlgorithmDescriptor(digestType);", "ResultDigestAccess does not yet route type lookup through the registry seam.");
            AssertContains(digestAccess, "return GetHashAlgorithmTypeAt(index);", "ResultDigestAccess does not yet route digest order through the registry seam.");
            AssertContains(digestAccess, "return GetHashAlgorithmIndex(digestType);", "ResultDigestAccess does not yet route digest index lookup through the registry seam.");

            AssertContainsAny(threadAccess,
                [
                    "#include \"Domain/HashAlgorithmRegistryCore.h\"",
                    "#include \"LegacyCompat/HashAlgorithmTypeCompat.h\""
                ],
                "ThreadData access seams do not yet consume the hash-algorithm registry seam.");
            AssertContains(threadAccess, "TryGetHashAlgorithmIndexById(algorithmId, &algorithmIndex)", "ThreadData access seams do not yet route selection storage through the registry index seam.");
            AssertContains(threadAccess, "VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)", "ThreadData access seams do not yet route enabled-algorithm iteration through the registry seam.");
            AssertContains(threadAccess, "SetThreadDataHashAlgorithmEnabledById(", "ThreadData access seams do not yet reset algorithm selection through descriptor ids.");
            AssertContains(threadAccess, "IsHashAlgorithmDescriptorEnabledByDefault(algorithmDescriptor)", "ThreadData access seams do not yet route default selection through descriptor metadata.");
            AssertContains(threadAccess, "VisitEnabledThreadDataHashAlgorithmIds(const ThreadData& threadData, THashAlgorithmIdVisitor visitor)", "ThreadData access seams do not yet expose id-based enabled-algorithm traversal.");
        }, failures);

        Run("Phase 12 routes managed and XAML algorithm entry through dynamic registry-driven descriptors while keeping the legacy desktop checkbox surface intact", () =>
        {
            string hashAlgorithmRegistry = ReadHashAlgorithmRegistrySeams(repoRoot);
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
            AssertDoesNotContain(hashMgmtClrHeader, "void SetHashAlgorithmEnabledByDigestType(int digestType, bool val);", "CLR bridge still exposes the removed generic digest-type enable helper.");
            AssertDoesNotContain(hashMgmtClrHeader, "bool GetHashAlgorithmEnabledByDigestType(int digestType);", "CLR bridge still exposes the removed generic digest-type query helper.");
            AssertContains(hashMgmtClr, "#include \"LegacyCompat/ManagedHashMgmtAccess.h\"", "CLR bridge does not yet include the shared managed hash-management seam.");
            AssertContains(hashMgmtClr, "CreateSupportedHashAlgorithmDescriptors()", "CLR bridge does not yet materialize a dynamic managed algorithm descriptor list.");
            AssertContains(hashMgmtUwpHeader, "public ref class HashAlgorithmDescriptorNet sealed", "UWP bridge does not yet expose the managed algorithm descriptor.");
            AssertContains(hashMgmtUwpHeader, "Platform::Array<HashAlgorithmDescriptorNet^>^ GetSupportedHashAlgorithms();", "UWP bridge does not yet expose the supported-algorithm list helper.");
            AssertDoesNotContain(hashMgmtUwpHeader, "void SetHashAlgorithmEnabledByDigestType(int digestType, Platform::Boolean val);", "UWP bridge still exposes the removed generic digest-type enable helper.");
            AssertDoesNotContain(hashMgmtUwpHeader, "Platform::Boolean GetHashAlgorithmEnabledByDigestType(int digestType);", "UWP bridge still exposes the removed generic digest-type query helper.");
            AssertContains(hashMgmtUwp, "#include \"LegacyCompat/ManagedHashMgmtAccess.h\"", "UWP bridge does not yet include the shared managed hash-management seam.");
            AssertContains(hashMgmtUwp, "CreateSupportedHashAlgorithmDescriptors()", "UWP bridge does not yet materialize a dynamic managed algorithm descriptor list.");

            AssertContains(winUiXaml, "StackPanelHashAlgorithms", "WinUI page does not yet expose the dynamic algorithm container.");
            AssertContains(winUiPage, "HashAlgorithmDescriptorNet[] m_hashAlgorithms", "WinUI page does not yet store the managed algorithm descriptor list.");
            AssertContains(winUiPage, "Dictionary<string, CheckBox> m_hashAlgorithmCheckBoxes", "WinUI page does not yet track dynamic algorithm checkboxes by descriptor id.");
            AssertContains(winUiPage, "GetHashAlgorithmId(HashAlgorithmDescriptorNet hashAlgorithm)", "WinUI page does not yet normalize dynamic algorithm ids for checkbox tracking.");
            AssertContains(winUiPage, "GetHashAlgorithmSettingKey(HashAlgorithmDescriptorNet hashAlgorithm)", "WinUI page does not yet route algorithm persistence through stable-name keys.");
            AssertContains(winUiPage, "LoadHashAlgorithmControls()", "WinUI page does not yet materialize dynamic algorithm controls.");
            AssertContains(winUiPage, "StackPanelHashAlgorithms.Children.Add(checkBox);", "WinUI page does not yet append dynamic algorithm checkboxes.");
            AssertContains(winUiPage, "SetHashAlgorithmControlsEnabled(bool enabled)", "WinUI page does not yet centralize dynamic algorithm enable/disable state.");
            AssertDoesNotContain(winUiPage, "CheckBoxHashMd5", "WinUI page still hardcodes the MD5 checkbox after introducing dynamic algorithm controls.");

            AssertContains(uwpXaml, "StackPanelHashAlgorithms", "UWP page does not yet expose the dynamic algorithm container.");
            AssertContains(uwpPage, "HashAlgorithmDescriptorNet[] m_hashAlgorithms", "UWP page does not yet store the managed algorithm descriptor list.");
            AssertContains(uwpPage, "Dictionary<string, CheckBox> m_hashAlgorithmCheckBoxes", "UWP page does not yet track dynamic algorithm checkboxes by descriptor id.");
            AssertContains(uwpPage, "GetHashAlgorithmId(HashAlgorithmDescriptorNet hashAlgorithm)", "UWP page does not yet normalize dynamic algorithm ids for checkbox tracking.");
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
            AssertContains(mfcController, "#include \"LegacyCompat/ThreadDataExecutionAccess.h\"", "Phase 13 controller does not yet layer on top of the thread-data execution seam.");
            AssertContains(mfcController, "VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)", "Phase 13 controller does not yet route checkbox traversal through the registry seam.");
            AssertContains(mfcController, "SetThreadDataHashAlgorithmEnabledById(*m_threadData, algorithmId, (checkBox->GetCheck() != FALSE));", "Phase 13 controller does not yet route checkbox state into ThreadDataAccess.");
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

        Run("Phase 14 keeps the shared shell ExplorerCommand seam while archiving legacy shell-extension projects", () =>
        {
            string shellCore = ReadRepoFile(repoRoot, @"trunk\source\WinCommon\ShellExplorerCommandCore.h");
            string wuiShellVerb = ReadRepoFile(repoRoot, @"sub-proj\fHashWUIShellExt\ExplorerCommandVerb.cpp");
            string uwpShellVerb = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpShellExt\ExplorerCommandVerb.cpp");
            string wuiShellProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUIShellExt\fHashWUIShellExt.vcxproj");
            string uwpShellProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpShellExt\fHashUwpShellExt.vcxproj");
            string workflow = ReadRepoFile(repoRoot, @".github\workflows\windows-build.yml");
            string archiveReadme = ReadRepoFile(repoRoot, @"archive\README.md");

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

            AssertDoesNotContain(workflow, "build-wui-shell-ext-x64:", "Windows workflow still builds the archived WinUI shell extension job in phase 14.");
            AssertDoesNotContain(workflow, "build-uwp-shell-ext-x64:", "Windows workflow still builds the archived UWP shell extension job in phase 14.");
            AssertDoesNotContain(workflow, "msbuild sub-proj/fHashWUIShellExt/fHashWUIShellExt.vcxproj", "Windows workflow still builds the archived WinUI shell extension project in phase 14.");
            AssertDoesNotContain(workflow, "msbuild sub-proj/fHashUwpShellExt/fHashUwpShellExt.vcxproj", "Windows workflow still builds the archived UWP shell extension project in phase 14.");
            AssertContains(archiveReadme, "legacy-platforms/sub-proj/fHashWUIShellExt", "Archive documentation does not record the WinUI shell extension as an archived legacy platform.");
            AssertContains(archiveReadme, "legacy-platforms/sub-proj/fHashUwpShellExt", "Archive documentation does not record the UWP shell extension as an archived legacy platform.");
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
            AssertContains(mfcRc, "IDC_CHECK_SHA1", "MFC resources do not yet provide the legacy checkbox anchors required by the phase 17 dynamic controller.");
        }, failures);

        Run("Phase 18 splits ThreadData access into dedicated execution, input, and result seams", () =>
        {
            string legacyThreadAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataAccess.h");
            string legacyThreadExecutionAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataExecutionAccess.h");
            string legacyThreadInputAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataInputAccess.h");
            string legacyThreadResultAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataResultAccess.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string mfcSearchController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSearchController.cpp");
            string mfcAlgorithmController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashAlgorithmSelectionController.cpp");

            AssertFileMissing(repoRoot, @"trunk\source\Common\ThreadDataAccess.h", "Phase 18 Common ThreadDataAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\ThreadDataExecutionAccess.h", "Phase 18 Common ThreadDataExecutionAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\ThreadDataInputAccess.h", "Phase 18 Common ThreadDataInputAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\ThreadDataResultAccess.h", "Phase 18 Common ThreadDataResultAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertContains(legacyThreadAccess, "ResetThreadDataForNewSession(ThreadData& threadData)", "Phase 18 legacy ThreadDataAccess seam does not yet keep the grouped session-reset helper.");

            AssertContains(legacyThreadExecutionAccess, "SetThreadDataObserver(ThreadData& threadData, HashProgressSink *observer)", "Phase 18 execution seam does not yet own progress-sink wiring.");
            AssertContains(legacyThreadExecutionAccess, "SetThreadDataWorking(ThreadData& threadData, bool working)", "Phase 18 execution seam does not yet own working-state writes.");
            AssertContains(legacyThreadExecutionAccess, "SetThreadDataHashAlgorithmEnabled(ThreadData& threadData, ResultDigestType digestType, bool enabled)", "Phase 18 execution seam does not yet own algorithm enablement.");
            AssertContains(legacyThreadExecutionAccess, "GetThreadDataTotalSize(const ThreadData& threadData)", "Phase 18 execution seam does not yet own counted-size reads.");

            AssertContains(legacyThreadInputAccess, "GetThreadDataInputFiles(const ThreadData& threadData)", "Phase 18 input seam does not yet own input-file reads.");
            AssertContains(legacyThreadInputAccess, "AppendThreadDataInputFile(ThreadData& threadData, const sunjwbase::tstring& fullPath)", "Phase 18 input seam does not yet own input-file appends.");
            AssertContains(legacyThreadInputAccess, "ReplaceTrimmedThreadDataInputFiles(ThreadData& threadData, const TStrVector& fullPaths)", "Phase 18 input seam does not yet own trimmed input-file replacement.");
            AssertContains(legacyThreadInputAccess, "VisitThreadDataInputFiles(const ThreadData& threadData, TInputFileVisitor visitor)", "Phase 18 input seam does not yet own input-file traversal.");

            AssertContains(legacyThreadResultAccess, "GetThreadDataResults(const ThreadData& threadData)", "Phase 18 result seam does not yet own result-list reads.");
            AssertContains(legacyThreadResultAccess, "AppendThreadDataResult(ThreadData& threadData)", "Phase 18 result seam does not yet own result-list appends.");
            AssertContains(legacyThreadResultAccess, "VisitThreadDataResults(const ThreadData& threadData, TResultVisitor visitor)", "Phase 18 result seam does not yet own result traversal.");

            AssertDoesNotContain(hashEngineInternal, "#include \"Common/ThreadDataExecutionAccess.h\"", "HashEngineInternal.h should no longer consume the phase 18 execution seam after thread-entry decoupling.");
            AssertDoesNotContain(hashEngineInternal, "#include \"Common/ThreadDataInputAccess.h\"", "HashEngineInternal.h should no longer consume the phase 18 input seam after thread-entry decoupling.");
            AssertDoesNotContain(hashEngineInternal, "#include \"Common/ThreadDataResultAccess.h\"", "HashEngineInternal.h should no longer consume the phase 18 result seam after thread-entry decoupling.");
            AssertDoesNotContain(hashEngineInternal, "#include \"Common/ThreadDataAccess.h\"", "HashEngineInternal.h still consumes the umbrella ThreadDataAccess header after the phase 18 seam split.");

            AssertContains(mfcSearchController, "#include \"LegacyCompat/ThreadDataExecutionAccess.h\"", "MFC search controller does not yet consume the phase 18 execution seam.");
            AssertContains(mfcSearchController, "#include \"LegacyCompat/ThreadDataResultAccess.h\"", "MFC search controller does not yet consume the phase 18 result seam.");
            AssertDoesNotContain(mfcSearchController, "#include \"Common/ThreadDataAccess.h\"", "MFC search controller still depends on the umbrella ThreadDataAccess header after phase 18.");

            AssertContains(mfcAlgorithmController, "#include \"LegacyCompat/ThreadDataExecutionAccess.h\"", "MFC algorithm controller does not yet consume the phase 18 execution seam.");
            AssertDoesNotContain(mfcAlgorithmController, "#include \"Common/ThreadDataAccess.h\"", "MFC algorithm controller still depends on the umbrella ThreadDataAccess header after phase 18.");
        }, failures);

        Run("Phase 19 extracts shared managed hash-management helpers for CLR and UWP bridges", () =>
        {
            string legacyManagedHashMgmtAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ManagedHashMgmtAccess.h");
            string hashMgmtClrHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.h");
            string hashMgmtClr = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string hashMgmtUwpHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.h");
            string hashMgmtUwp = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");

            AssertFileMissing(repoRoot, @"trunk\source\Common\ManagedHashMgmtAccess.h", "Phase 19 Common ManagedHashMgmtAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertContains(legacyManagedHashMgmtAccess, "TryConvertManagedHashAlgorithmId(const sunjwbase::tstring& managedAlgorithmId, HashAlgorithmId *algorithmId)", "Phase 19 is missing the shared managed algorithm-id conversion helper.");
            AssertContains(legacyManagedHashMgmtAccess, "CreateSupportedManagedHashAlgorithmDescriptors(", "Phase 19 is missing the shared managed algorithm-descriptor projection helper.");
            AssertContains(legacyManagedHashMgmtAccess, "descriptorNet->AlgorithmId =", "Phase 19 shared managed algorithm-descriptor helper does not yet expose algorithm ids.");
            AssertContains(legacyManagedHashMgmtAccess, "descriptorNet->StableName =", "Phase 19 shared managed algorithm-descriptor helper does not yet expose stable names.");
            AssertContains(legacyManagedHashMgmtAccess, "descriptorNet->DisplayLabel =", "Phase 19 shared managed algorithm-descriptor helper does not yet expose display labels.");
            AssertContains(legacyManagedHashMgmtAccess, "SetManagedHashAlgorithmEnabledById(", "Phase 19 is missing the shared managed algorithm-id enable helper.");
            AssertContains(legacyManagedHashMgmtAccess, "GetManagedHashAlgorithmEnabledById(", "Phase 19 is missing the shared managed algorithm-id query helper.");
            AssertDoesNotContain(legacyManagedHashMgmtAccess, "TryConvertManagedHashAlgorithmDigestType(", "Phase 19 legacy managed hash helper still exposes the removed digest-type conversion helper.");
            AssertDoesNotContain(legacyManagedHashMgmtAccess, "SetManagedHashAlgorithmEnabledByDigestType(", "Phase 19 legacy managed hash helper still exposes the removed digest-type enable helper.");
            AssertDoesNotContain(legacyManagedHashMgmtAccess, "GetManagedHashAlgorithmEnabledByDigestType(", "Phase 19 legacy managed hash helper still exposes the removed digest-type query helper.");
            AssertContains(legacyManagedHashMgmtAccess, "ReplaceThreadDataInputFilesFromManagedArray(", "Phase 19 is missing the shared managed input-file replacement helper.");
            AssertContains(legacyManagedHashMgmtAccess, "CreateProjectedManagedDigestMatchingResults(", "Phase 19 is missing the shared managed digest-search projection helper.");

            AssertContains(hashMgmtClr, "#include \"LegacyCompat/ManagedHashMgmtAccess.h\"", "CLR HashMgmt implementation does not yet consume the phase 19 managed hash-management seam.");
            AssertContains(hashMgmtClr, "CreateSupportedManagedHashAlgorithmDescriptors<HashAlgorithmDescriptorNet^, cli::array<HashAlgorithmDescriptorNet^>^>", "CLR HashMgmt does not yet route descriptor projection through the shared managed helper.");
            AssertContains(hashMgmtClrHeader, "property System::String^ AlgorithmId;", "CLR HashMgmt descriptor surface does not yet expose algorithm ids.");
            AssertContains(hashMgmtClrHeader, "void SetHashAlgorithmEnabledById(System::String^ algorithmId, bool val);", "CLR HashMgmt does not yet expose algorithm-id enablement.");
            AssertContains(hashMgmtClrHeader, "bool GetHashAlgorithmEnabledById(System::String^ algorithmId);", "CLR HashMgmt does not yet expose algorithm-id queries.");
            AssertContains(hashMgmtClr, "::SetManagedHashAlgorithmEnabledById(*m_pThreadData, ConvertManagedAlgorithmIdToTstr(algorithmId), val);", "CLR HashMgmt does not yet route algorithm-id enablement through the shared managed helper.");
            AssertContains(hashMgmtClr, "return ::GetManagedHashAlgorithmEnabledById(*m_pThreadData, ConvertManagedAlgorithmIdToTstr(algorithmId));", "CLR HashMgmt does not yet route algorithm-id queries through the shared managed helper.");
            AssertDoesNotContain(hashMgmtClrHeader, "HashAlgorithmTypeNet", "CLR HashMgmt header still exposes the removed managed algorithm enum.");
            AssertDoesNotContain(hashMgmtClrHeader, "property int DigestType;", "CLR HashMgmt header still exposes removed digest-type payload.");
            AssertDoesNotContain(hashMgmtClr, "::SetManagedHashAlgorithmEnabledByDigestType(", "CLR HashMgmt still routes through the removed digest-type enable helper.");
            AssertDoesNotContain(hashMgmtClr, "::GetManagedHashAlgorithmEnabledByDigestType(", "CLR HashMgmt still routes through the removed digest-type query helper.");
            AssertContains(hashMgmtClr, "ReplaceThreadDataInputFilesFromManagedArray(*m_pThreadData, filePaths, ConvertManagedFilePathToTstr);", "CLR HashMgmt does not yet route managed file ingestion through the shared managed helper.");
            AssertContains(hashMgmtClr, "CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, cli::array<HashResultNet>^>(", "CLR HashMgmt does not yet route digest-search projection through the shared managed helper.");
            AssertDoesNotContain(hashMgmtClr, "#include \"Common/ResultDataSearch.h\"", "CLR HashMgmt still depends directly on the digest-search header after phase 19.");
            AssertDoesNotContain(hashMgmtClr, "#include \"Common/ResultDataProjection.h\"", "CLR HashMgmt still depends directly on the result-projection header after phase 19.");
            AssertDoesNotContain(hashMgmtClr, "#include \"Common/ThreadDataAccess.h\"", "CLR HashMgmt still depends directly on the umbrella ThreadDataAccess header after phase 19.");

            AssertContains(hashMgmtUwp, "#include \"LegacyCompat/ManagedHashMgmtAccess.h\"", "UWP HashMgmt implementation does not yet consume the phase 19 managed hash-management seam.");
            AssertContains(hashMgmtUwp, "CreateSupportedManagedHashAlgorithmDescriptors<HashAlgorithmDescriptorNet^, Array<HashAlgorithmDescriptorNet^>^>", "UWP HashMgmt does not yet route descriptor projection through the shared managed helper.");
            AssertContains(hashMgmtUwpHeader, "property Platform::String^ AlgorithmId;", "UWP HashMgmt descriptor surface does not yet expose algorithm ids.");
            AssertContains(hashMgmtUwpHeader, "void SetHashAlgorithmEnabledById(Platform::String^ algorithmId, Platform::Boolean val);", "UWP HashMgmt does not yet expose algorithm-id enablement.");
            AssertContains(hashMgmtUwpHeader, "Platform::Boolean GetHashAlgorithmEnabledById(Platform::String^ algorithmId);", "UWP HashMgmt does not yet expose algorithm-id queries.");
            AssertContains(hashMgmtUwp, "::SetManagedHashAlgorithmEnabledById(m_threadData, ConvertManagedAlgorithmIdToTstr(algorithmId), val);", "UWP HashMgmt does not yet route algorithm-id enablement through the shared managed helper.");
            AssertContains(hashMgmtUwp, "return ::GetManagedHashAlgorithmEnabledById(m_threadData, ConvertManagedAlgorithmIdToTstr(algorithmId));", "UWP HashMgmt does not yet route algorithm-id queries through the shared managed helper.");
            AssertDoesNotContain(hashMgmtUwpHeader, "HashAlgorithmTypeNet", "UWP HashMgmt header still exposes the removed managed algorithm enum.");
            AssertDoesNotContain(hashMgmtUwpHeader, "property int DigestType;", "UWP HashMgmt header still exposes removed digest-type payload.");
            AssertDoesNotContain(hashMgmtUwp, "::SetManagedHashAlgorithmEnabledByDigestType(", "UWP HashMgmt still routes through the removed digest-type enable helper.");
            AssertDoesNotContain(hashMgmtUwp, "::GetManagedHashAlgorithmEnabledByDigestType(", "UWP HashMgmt still routes through the removed digest-type query helper.");
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
            AssertContains(inputControllerHeader, "enum class FileLoadResult", "Phase 20 input controller is missing the unified file-load result enum.");
            AssertContains(inputControllerHeader, "struct FileLoadOutcome", "Phase 20 input controller is missing the structured file-load outcome.");
            AssertContains(inputControllerHeader, "struct FolderScanOutcome", "Phase 20 input controller is missing the folder-scan outcome seam.");
            AssertContains(inputControllerHeader, "FileLoadOutcome LoadOpenFileDialogSelection(LPCTSTR fileFilter);", "Phase 20 input controller is missing the open-dialog ingestion seam.");
            AssertContains(inputControllerHeader, "FileLoadOutcome LoadDroppedFiles(HDROP hDropInfo);", "Phase 20 input controller is missing the drag-drop ingestion seam.");
            AssertContains(inputControllerHeader, "FileLoadOutcome LoadCopyDataFiles(const COPYDATASTRUCT* pCopyDataStruct);", "Phase 20 input controller is missing the WM_COPYDATA ingestion seam.");
            AssertContains(inputControllerHeader, "static TStrVector ParseFilesCmdLine(LPTSTR filesCmdLine);", "Phase 20 input controller is missing the command-line parser seam.");
            AssertContains(inputControllerHeader, "void ClearFilePaths();", "Phase 20 input controller is missing the grouped input-reset helper.");

            AssertContains(inputController, "#include \"LegacyCompat/ThreadDataInputAccess.h\"", "Phase 20 input controller does not yet consume the dedicated ThreadData input seam.");
            AssertContains(inputController, "CommandLineToArgvW", "Phase 20 input controller does not yet own the hardened command-line parser.");
            AssertContains(inputController, "CopyDraggedPath", "Phase 20 input controller does not yet own long-path-safe drag/drop extraction.");
            AssertContains(inputController, "IsValidCopyDataString", "Phase 20 input controller does not yet own WM_COPYDATA validation.");
            AssertContains(inputController, "ReplaceThreadDataInputFiles(*m_threadData, parameters);", "Phase 20 input controller does not yet route command-line replacement through ThreadData input access.");
            AssertContains(inputController, "AppendThreadDataInputFile(*m_threadData, dlgOpen.GetNextPathName(pos).GetString());", "Phase 20 input controller does not yet route file-dialog appends through ThreadData input access.");
            AssertContains(inputController, "ReplaceTrimmedThreadDataInputFiles(*m_threadData, parameters);", "Phase 20 input controller does not yet route WM_COPYDATA replacement through ThreadData input access.");
            AssertContains(inputController, "kMaxHashFilesPerSession", "Phase 20 input controller does not yet use the unified per-session file limit constant.");
            AssertContains(inputController, "FileLoadResult::RejectedOverLimit", "Phase 20 input controller does not yet expose explicit over-limit outcomes.");

            AssertContains(dlgHeader, "#include \"FilesHashInputController.h\"", "FilesHashDlg.h does not yet consume the phase 20 input controller.");
            AssertContains(dlgHeader, "FilesHashInputController m_hashInputController;", "FilesHashDlg.h does not yet keep the phase 20 input controller.");
            AssertDoesNotContain(dlgHeader, "TStrVector ParseFilesCmdLine(LPTSTR filesCmdLine);", "FilesHashDlg.h still declares the old inline command-line parser after phase 20.");
            AssertDoesNotContain(dlgHeader, "void ClearFilePaths();", "FilesHashDlg.h still declares the old inline input-reset helper after phase 20.");

            AssertContains(dlgCppAndInitialization, "hashInputController->Initialize(threadData, parentWnd);", "FilesHashDlg.cpp does not yet initialize the phase 20 input controller.");
            AssertContains(dlgCppAndInitialization, "hashInputController->LoadCommandLineFiles(filesCmdLine);", "FilesHashDlg.cpp does not yet route command-line ingestion through the phase 20 input controller.");
            AssertContains(dlgCppAndCommandAndMessage, "m_hashInputController->LoadDroppedFiles(hDropInfo);", "FilesHashDlg.cpp does not yet route drag-drop ingestion through the phase 20 input controller.");
            AssertContains(dlgCppAndCommandAndMessage, "m_hashInputController->LoadCopyDataFiles(pCopyDataStruct)", "FilesHashDlg.cpp does not yet route WM_COPYDATA ingestion through the phase 20 input controller.");
            AssertContains(dlgCppAndCommand, "m_hashInputController->LoadOpenFileDialogSelection(fileFilter)", "FilesHashDlg.cpp does not yet route open-dialog ingestion through the phase 20 input controller.");
            AssertContains(dlgCppAndCommandAndMessage, "DispatchFileLoadOutcome(", "FilesHashDlg.cpp does not yet route structured file-load outcomes through the shared dispatcher.");
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

            AssertFileMissing(repoRoot, @"trunk\source\Common\HashThreadLaunch.h", "Phase 21 Common HashThreadLaunch shim should be removed after the LegacyCompat boundary cleanup.");

            AssertContains(sessionControllerHeader, "BOOL PrepareHashStart(LPCTSTR noSelectionMessage);", "Phase 21 session controller is missing the pre-start validation seam.");
            AssertContains(sessionControllerHeader, "void StartHashThread();", "Phase 21 session controller is missing the thread-start seam.");
            AssertContains(sessionControllerHeader, "void StopWorkingThread();", "Phase 21 session controller is missing the stop-request seam.");
            AssertContains(sessionControllerHeader, "void SetControls(BOOL working, BOOL limited, LPCTSTR openButtonText, LPCTSTR stopButtonText);", "Phase 21 session controller is missing the grouped working-state UI seam.");
            AssertContains(sessionControllerHeader, "void PrepareDropTarget(CWnd* pWnd, BOOL bAccept);", "Phase 21 session controller is missing the grouped drop-target helper.");

            AssertContains(sessionController, "#include \"LegacyCompat/HashThreadLaunch.h\"", "Phase 21 session controller does not yet consume the shared hash-thread launch seam.");
            AssertDoesNotContain(sessionController, "#include \"Common/HashThreadEntry.h\"", "Phase 21 session controller should consume HashThreadLaunch.h instead of directly including HashThreadEntry.h.");
            AssertContains(sessionController, "#include \"LegacyCompat/ThreadDataExecutionAccess.h\"", "Phase 21 session controller does not yet consume the ThreadData execution seam.");
            AssertContains(sessionController, "m_hashAlgorithmSelectionController->SyncSelections();", "Phase 21 session controller does not yet own hash-algorithm selection sync.");
            AssertContains(sessionController, "m_hashAlgorithmSelectionController->ValidateSelection(noSelectionMessage);", "Phase 21 session controller does not yet own hash-algorithm validation.");
            AssertContains(sessionController, "RestartHashWorkerThread(&m_hWorkThread, m_threadData, &thredID);", "Phase 21 session controller does not yet own work-thread restart through the shared launch seam.");
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
            AssertContains(contextController, "WindowsComm::IsWindowsVistaOrGreater()", "Phase 22 context controller does not yet gate elevation through the modern Windows-version helper.");

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
            AssertContains(progressController, "UpdateSummaryText();", "Phase 23 progress controller does not yet centralize status-summary refreshes.");
            AssertContains(progressController, "m_statusOverviewCtrl->SetWindowText(summary);", "Phase 23 progress controller does not yet centralize status summary rendering.");

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

            AssertContains(lifecycleController, "#include \"LegacyCompat/ThreadDataExecutionAccess.h\"", "Phase 26 lifecycle controller does not yet consume the ThreadData execution seam.");
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
            AssertContains(commandControllerHeader, "void HandleOpenButtonClick(", "Phase 27 command controller is missing the open-button seam.");
            AssertContains(commandControllerHeader, "void HandleExitButtonClick() const;", "Phase 27 command controller is missing the exit-button seam.");
            AssertContains(commandControllerHeader, "void HandleAboutButtonClick() const;", "Phase 27 command controller is missing the about-button seam.");
            AssertContains(commandControllerHeader, "void HandleCleanButtonClick(LPCTSTR clearButtonText, LPCTSTR clearVerifyButtonText);", "Phase 27 command controller is missing the clean-button seam.");
            AssertContains(commandControllerHeader, "void HandleFindButtonClick(LPCTSTR clearVerifyButtonText) const;", "Phase 27 command controller is missing the find-button seam.");

            AssertContains(commandController, "#include \"AboutDlg.h\"", "Phase 27 command controller does not yet own the About dialog include.");
            AssertContains(commandController, "#include \"FindDlg.h\"", "Phase 27 command controller does not yet own the Find dialog include.");
            AssertContains(commandController, "#include \"LegacyCompat/ThreadDataExecutionAccess.h\"", "Phase 27 command controller does not yet consume the execution seam.");
            AssertContains(commandController, "m_hashInputController->LoadOpenFileDialogSelection(fileFilter)", "Phase 27 command controller does not yet own open-dialog file loading.");
            AssertContains(commandController, "m_hashMessageController->DispatchFileLoadOutcome(", "Phase 27 command controller does not yet route file-load outcomes through the shared dispatcher.");
            AssertContains(commandController, "m_hashSessionController->StopWorkingThread();", "Phase 27 command controller does not yet own open-button stop behavior.");
            AssertContains(commandController, "m_hashResultViewController->ClearResults(*m_threadData);", "Phase 27 command controller does not yet own clear-results dispatch.");
            AssertContains(commandController, "ClearProgressLabels();", "Phase 27 command controller does not yet own grouped progress-label clearing.");
            AssertContains(commandController, "m_hashSearchController->BeginSearch(CString(), findDialog.GetFindHash(), clearVerifyButtonText)", "Phase 27 command controller does not yet own find-dialog dispatch.");
            AssertContains(commandController, "m_parentWnd->PostMessage(WM_CLOSE);", "Phase 27 command controller does not yet own exit-button close dispatch.");
            AssertContains(commandControllerHeader, "FilesHashMessageController* hashMessageController,", "Phase 27 command controller is missing the shared message-controller dependency.");

            AssertContains(dlgHeader, "#include \"FilesHashCommandController.h\"", "FilesHashDlg.h does not yet consume the phase 27 command controller.");
            AssertContains(dlgHeader, "FilesHashCommandController m_hashCommandController;", "FilesHashDlg.h does not yet keep the phase 27 command controller.");

            AssertContains(dlgCppAndInitialization, "hashCommandController->Initialize(threadData, parentWnd, btnClr, hashInputController, hashSearchController, hashSessionController, hashLifecycleController, hashMessageController, hashProgressController, hashResultViewController);", "FilesHashDlg.cpp does not yet initialize the phase 27 command controller.");
            AssertContains(dlgCpp, "m_hashCommandController.HandleOpenButtonClick(", "FilesHashDlg.cpp does not yet route open-button handling through the phase 27 command controller.");
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
            AssertContains(messageControllerHeader, "bool DispatchFileLoadOutcome(", "Phase 28 message controller is missing the shared file-load dispatch seam.");
            AssertContains(messageControllerHeader, "void HandleDropFiles(", "Phase 28 message controller is missing the drop-files seam.");
            AssertContains(messageControllerHeader, "BOOL HandleCopyData(", "Phase 28 message controller is missing the copy-data seam.");
            AssertContains(messageControllerHeader, "LRESULT HandleCustomMessage(WPARAM wParam) const;", "Phase 28 message controller is missing the custom-message seam.");
            AssertContains(messageControllerHeader, "void HandleInitMenuPopup(CMenu* pPopupMenu) const;", "Phase 28 message controller is missing the popup-menu seam.");
            AssertContains(messageControllerHeader, "void HandleCopyHash() const;", "Phase 28 message controller is missing the copy-hash seam.");
            AssertContains(messageControllerHeader, "void UpdateCopyHashMenuText(CCmdUI* pCmdUI, LPCTSTR copyText) const;", "Phase 28 message controller is missing the copy-menu-text seam.");

            AssertContains(messageController, "#include \"LegacyCompat/ThreadDataExecutionAccess.h\"", "Phase 28 message controller does not yet consume the execution seam.");
            AssertContains(messageController, "m_parentWnd->IsIconic()", "Phase 28 message controller does not yet own iconic-paint gating.");
            AssertContains(messageController, "dc.DrawIcon(x, y, icon);", "Phase 28 message controller does not yet own icon rendering.");
            AssertContains(messageController, "m_parentWnd->DragAcceptFiles(FALSE);", "Phase 28 message controller does not yet own drop-target suspension during drag ingestion.");
            AssertContains(messageController, "m_hashInputController->LoadDroppedFiles(hDropInfo);", "Phase 28 message controller does not yet own drag-drop ingestion.");
            AssertContains(messageController, "m_parentWnd->SetForegroundWindow();", "Phase 28 message controller does not yet own foreground promotion for WM_COPYDATA.");
            AssertContains(messageController, "m_hashInputController->LoadCopyDataFiles(pCopyDataStruct)", "Phase 28 message controller does not yet own WM_COPYDATA ingestion.");
            AssertContains(messageController, "DispatchFileLoadOutcome(", "Phase 28 message controller does not yet own non-button file-load dispatch.");
            AssertContains(messageController, "m_hashResultViewController->ShowHyperEditMenu(m_parentWnd);", "Phase 28 message controller does not yet own HyperEdit popup display.");
            AssertContains(messageController, "m_hashResultViewController->UpdatePopupMenu(m_parentWnd, pPopupMenu);", "Phase 28 message controller does not yet own popup-menu updates.");
            AssertContains(messageController, "m_hashResultViewController->CopyLastHyperlink();", "Phase 28 message controller does not yet own hyperlink copying.");
            AssertContains(messageController, "m_hashResultViewController->UpdateCopyHashMenuText(pCmdUI, copyText);", "Phase 28 message controller does not yet own menu-text updates.");

            AssertContains(dlgHeader, "#include \"FilesHashMessageController.h\"", "FilesHashDlg.h does not yet consume the phase 28 message controller.");
            AssertContains(dlgHeader, "FilesHashMessageController m_hashMessageController;", "FilesHashDlg.h does not yet keep the phase 28 message controller.");

            AssertContains(dlgCppAndInitialization, "hashMessageController->Initialize(threadData, parentWnd, hashInputController, hashLifecycleController, hashResultViewController);", "FilesHashDlg.cpp does not yet initialize the phase 28 message controller.");
            AssertContains(dlgCpp, "if (m_hashMessageController.HandlePaint(m_hIcon))", "FilesHashDlg.cpp does not yet route icon paint through the phase 28 message controller.");
            AssertContains(dlgCpp, "return m_hashMessageController.GetDragCursor(m_hIcon);", "FilesHashDlg.cpp does not yet route drag-cursor queries through the phase 28 message controller.");
            AssertContains(dlgCpp, "m_hashMessageController.HandleDropFiles(", "FilesHashDlg.cpp does not yet route WM_DROPFILES through the phase 28 message controller.");
            AssertContains(dlgCpp, "m_hashMessageController.HandleCopyData(", "FilesHashDlg.cpp does not yet route WM_COPYDATA through the phase 28 message controller.");
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
            AssertContains(digestMetadataAccess, "GetResultDigestMetadataSnapshot()", "Phase 29 metadata seam does not yet expose stable snapshot reads for digest metadata.");
            AssertContains(digestMetadataAccess, "VisitResultDigestMetadata(TResultDigestMetadataVisitor visitor)", "Phase 29 metadata seam does not yet own metadata iteration.");
            AssertContainsAny(digestMetadataAccess,
                [
                    "VisitResultDigests(TResultDigestVisitor visitor)",
                    "VisitResultDigestIds(TResultDigestIdVisitor visitor)"
                ],
                "Phase 29 metadata seam does not yet own digest iteration.");

            AssertContainsAny(digestStateAccess,
                [
                    "GetDigestStorageValue(const ResultDigestStorage& digestStorage, ResultDigestType digestType)",
                    "GetDigestStorageValueById(const ResultDigestStorage& digestStorage, const HashAlgorithmId& algorithmId)"
                ],
                "Phase 29 state seam does not yet own reusable digest-storage reads.");
            AssertContains(digestStateAccess, "GetResultDigestState(const ResultData& result)", "Phase 29 state seam does not yet own digest-state reads.");
            AssertContains(digestStateAccess, "GetResultDigestStorage(const ResultData& result)", "Phase 29 state seam does not yet own digest-storage reads.");
            AssertDoesNotContain(digestStateAccess, "GetResultDigestCompatibilityFields(const ResultData& result)", "Phase 46 core digest state seam still exposes legacy compatibility-field reads.");
            AssertDoesNotContain(digestStateAccess, "GetCompatibilityResultDigest(const ResultData& result, ResultDigestType digestType)", "Phase 46 core digest state seam still exposes legacy compatibility-value reads.");

            AssertContainsAny(digestValueAccess,
                [
                    "GetResultDigest(const ResultData& result, ResultDigestType digestType)",
                    "GetResultDigestById(const ResultData& result, const HashAlgorithmId& algorithmId)"
                ],
                "Phase 29 value seam does not yet own digest reads.");
            AssertContains(digestValueAccess, "VisitResultDigestMetadataValues(const ResultData& result, TResultDigestMetadataValueVisitor visitor)", "Phase 29 value seam does not yet own metadata-value iteration.");
            AssertContains(digestValueAccess, "HasAnyResultDigests(const ResultData& result)", "Phase 29 value seam does not yet own aggregate digest presence checks.");
            AssertContainsAny(digestValueAccess,
                [
                    "SetResultDigest(ResultData& result, ResultDigestType digestType, const sunjwbase::tstring& digestValue)",
                    "SetResultDigestById(ResultData& result, const HashAlgorithmId& algorithmId, const sunjwbase::tstring& digestValue)"
                ],
                "Phase 29 value seam does not yet own digest writes.");
            AssertContainsAny(digestValueAccess,
                [
                    "TryGetMutableResultDigest(ResultData& result, ResultDigestType digestType, sunjwbase::tstring **digestValue)",
                    "TryGetMutableResultDigestById(ResultData& result, const HashAlgorithmId& algorithmId, sunjwbase::tstring **digestValue)"
                ],
                "Phase 29 value seam does not yet own explicit mutable digest lookup.");
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
            AssertContains(unitTests, "Workflow_RunsIndependentUnitTests_And_PreparesNativeOpenSslVendor_Once", "Phase 30 unit tests do not yet cover CI gating for the new unit framework.");
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
            string hashRequest = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashRequest.h");
            string legacyHashRequestProjection = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\HashRequestProjection.h");
            string legacyHashRequestTypeCompat = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\HashRequestTypeCompat.h");
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string hashResult = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashResult.h");
            string hashResultShim = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResult.h");
            string progressEvent = ReadRepoFile(repoRoot, @"trunk\source\Domain\ProgressEvent.h");
            string progressEventShim = ReadRepoFile(repoRoot, @"trunk\source\Common\ProgressEvent.h");
            string hashProgressSink = ReadRepoFile(repoRoot, @"trunk\source\Runtime\HashProgressSink.h");
            string hashProgressSinkShim = ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressSink.h");
            string hashEngineObserver = ReadRepoFile(repoRoot, @"trunk\source\Adapters\UiBridge\HashEngineObserver.h");
            string hashEngine = ReadHashEngineImplementation(repoRoot);

            AssertFileMissing(repoRoot, @"trunk\source\Common\HashRequest.h", "Phase 90 Common HashRequest.h should be removed after direct Domain include adoption.");
            AssertContains(hashRequest, "struct HashRequest", "Phase 31 does not yet define a stable HashRequest contract.");
            AssertContains(hashRequest, "TStrVector files;", "Phase 31 HashRequest does not yet own file inputs.");
            AssertContains(hashRequest, "std::vector<HashAlgorithmId> algorithmIds;", "Phase 31 HashRequest does not yet own descriptor/id-based algorithm selection.");
            AssertContains(hashRequest, "assert(fileIndex < request.files.size());", "Phase 31 HashRequest file access does not yet assert checked file-index bounds.");
            AssertContains(hashRequest, "return request.files.at(fileIndex);", "Phase 31 HashRequest file access does not yet use checked indexing.");
            AssertDoesNotContain(hashRequest, "return request.files[fileIndex];", "Phase 31 HashRequest file access still uses unchecked operator[] indexing.");
            AssertDoesNotContain(hashRequest, "std::vector<ResultDigestType> algorithms;", "Phase 31 HashRequest still keeps the legacy digest-type algorithm list after descriptor/id promotion.");
            AssertContains(hashRequest, "bool uppercaseDigest;", "Phase 31 HashRequest does not yet own uppercase output preference.");
            AssertContains(hashRequest, "HashRequestDigestExecutionPolicy digestExecutionPolicy;", "Phase 31 HashRequest does not yet expose runtime digest execution policy.");
            AssertDoesNotContain(hashRequest, "CreateHashRequest(const ThreadData& threadData)", "Phase 31 HashRequest contract still depends directly on ThreadData projection.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\HashRequestProjection.h", "Phase 31 Common HashRequestProjection shim should be removed after the LegacyCompat boundary cleanup.");
            AssertContains(legacyHashRequestProjection, "CreateHashRequest(const ThreadData& threadData)", "Phase 31 does not yet project ThreadData into HashRequest through the legacy projection seam.");
            AssertContains(legacyHashRequestProjection, "#include \"LegacyCompat/HashRequestTypeCompat.h\"", "Phase 31 legacy HashRequest projection does not consume digest-type compatibility helpers.");
            AssertContains(legacyHashRequestProjection, "#include \"LegacyCompat/ThreadDataExecutionAccess.h\"", "Phase 31 legacy HashRequest projection does not yet consume thread-data execution access.");
            AssertContains(legacyHashRequestProjection, "#include \"LegacyCompat/ThreadDataInputAccess.h\"", "Phase 31 legacy HashRequest projection does not yet consume thread-data input access.");
            AssertContains(hashRequest, "AppendHashRequestAlgorithmId(HashRequest& request, const HashAlgorithmId& algorithmId)", "Phase 31 HashRequest does not yet expose descriptor/id append seams.");
            AssertContains(legacyHashRequestTypeCompat, "AppendHashRequestAlgorithm(HashRequest& request, ResultDigestType digestType)", "Phase 31 digest-type compatibility append seam should live in legacy compatibility layer.");
            AssertContains(hashRequest, "normalizedAlgorithmIds.reserve(request.algorithmIds.size());", "Phase 31 HashRequest normalization should reserve from descriptor/id algorithm inputs only.");
            AssertContains(hashRequest, "VisitHashRequestAlgorithmIds(const HashRequest& request", "Phase 31 HashRequest does not yet expose descriptor/id iteration seams.");
            AssertContains(hashRequest, "VisitHashRequestFiles(const HashRequest& request", "Phase 31 HashRequest does not yet own file iteration.");
            AssertContains(legacyHashRequestTypeCompat, "VisitHashRequestAlgorithms(const HashRequest& request", "Phase 31 digest-type compatibility algorithm iteration seam should live in legacy compatibility layer.");

            AssertContains(hashResultShim, "#include \"Domain/HashResult.h\"", "Phase 89 Common HashResult.h should now be a thin Domain shim.");
            AssertContains(global, "struct HashResult", "Phase 31 does not yet define a stable HashResult contract.");
            AssertDoesNotContain(hashResult, "const ResultData *sourceResult;", "Phase 42 HashResult still keeps the legacy compatibility link back to ResultData.");
            AssertContains(global, "std::vector<HashDigestResult> digests;", "Phase 31 HashResult does not yet own a digest collection.");
            AssertContains(hashResult, "ProjectHashResult(const ResultData& result)", "Phase 31 does not yet project ResultData into HashResult.");

            AssertContains(progressEventShim, "#include \"Domain/ProgressEvent.h\"", "Phase 89 Common ProgressEvent.h should now be a thin Domain shim.");
            AssertContains(progressEvent, "enum ProgressEventType", "Phase 31 does not yet define a stable ProgressEventType surface.");
            AssertContains(progressEvent, "struct ProgressEvent", "Phase 31 does not yet define a stable ProgressEvent contract.");
            AssertDoesNotContain(progressEvent, "CreateFileHashReadyProgressEvent(const ResultData& result, bool uppercaseDigest)", "Phase 42 still keeps the legacy ResultData-based hash-ready progress event overload.");
            AssertContains(progressEvent, "CreateFileHashReadyProgressEvent(const HashResult& result, bool uppercaseDigest)", "Phase 35 does not yet expose hash-ready progress events directly from HashResult.");

            AssertContains(hashProgressSinkShim, "#include \"Runtime/HashProgressSink.h\"", "Phase 90 Common HashProgressSink.h should now be a thin Runtime shim.");
            AssertContains(hashProgressSink, "class HashProgressSink", "Phase 31 does not yet define a neutral hash progress sink contract.");
            AssertContains(hashProgressSink, "virtual int progressMax() = 0;", "Phase 31 hash progress sink does not yet own progress max queries.");
            AssertContains(hashProgressSink, "virtual void onProgressEvent(const ProgressEvent& progressEvent) = 0;", "Phase 31 hash progress sink does not yet own semantic progress-event dispatch.");
            AssertContains(hashEngineObserver, "class HashProgressEventBridge: public HashProgressSink", "HashProgressEventBridge does not yet layer on top of the phase 31 hash progress sink.");
            AssertContains(hashEngineObserver, "typedef HashProgressEventBridge HashEngineObserver;", "HashEngineObserver compatibility alias is missing from the phase 31 adapter seam.");
            AssertContains(hashEngineObserver, "virtual void onProgressEvent(const ProgressEvent& progressEvent)", "HashEngineObserver does not yet expose the phase 31 progress-event compatibility entry point.");
            AssertContains(hashEngineObserver, "handleFileResultProgressEvent(progressEvent.result, progressEvent.type, progressEvent.uppercaseDigest);", "HashProgressEventBridge does not yet route result progress events through HashResult.");
            AssertContains(hashEngineObserver, "handleTotalProgressEvent(progressEvent.value);", "HashProgressEventBridge does not yet bridge total-progress events through the neutral adapter callback.");

            AssertContains(hashEngine, "HashRequest request = CreateThreadDataHashRequest(*thrdData);", "HashThread entry does not yet start from the phase 31 HashRequest contract through the projection seam.");
            AssertContains(hashEngine, "VisitHashRequestFiles(request", "HashEngine does not yet iterate files through HashRequest.");
            AssertContains(hashEngine, "VisitDigestUpdateRequestOperations(digestUpdateRequest, [&](const HashDigestOperationDescriptor& operationDescriptor)", "HashEngine does not yet route selected algorithms through digest-update operation descriptors.");
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
            string previewWorkflow = ReadRepoFile(repoRoot, @".github\workflows\winui-preview-build.yml");

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
                previewWorkflow,
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
            string hashExecutionContext = ReadRepoFile(repoRoot, @"trunk\source\Runtime\HashExecutionContext.h");
            string hashEngineHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.h");
            string hashThreadEntryHeader = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\HashThreadEntry.h");
            string hashEngine = ReadHashEngineImplementation(repoRoot);

            AssertFileMissing(repoRoot, @"trunk\source\Common\HashExecutionContext.h", "Phase 90 Common HashExecutionContext.h should be removed after direct Runtime include adoption.");
            AssertContains(hashExecutionContext, "struct HashExecutionContext", "Phase 35 hash execution context header is missing the execution-context contract.");
            AssertContains(hashExecutionContext, "CreateHashExecutionContext(HashProgressSink *progressSink, HashJobState& jobState, HashCancellationState& cancellationState)", "Phase 35 hash execution context does not yet expose explicit execution-context construction dependencies.");
            AssertDoesNotContain(hashExecutionContext, "CreateHashExecutionContext(ThreadData& threadData)", "Phase 35 hash execution context still directly depends on ThreadData.");
            AssertContains(hashEngineHeader, "struct HashExecutionContext;", "Phase 33 HashEngine header does not yet forward declare HashExecutionContext.");
            AssertContains(hashEngineHeader, "int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request);", "Phase 33 HashEngine header does not yet expose the execution-context request entry.");
            AssertDoesNotContain(hashEngineHeader, "int WINAPI HashThreadFunc(void *param);", "Phase 33 HashEngine header should no longer expose the thread-entry declaration after the thread-entry header split.");
            AssertContains(hashThreadEntryHeader, "int WINAPI HashThreadFunc(void *param);", "Phase 33 HashThreadEntry header does not yet expose the thread-entry declaration.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\HashThreadEntry.h", "Phase 33 Common HashThreadEntry.h should be removed after the LegacyCompat boundary cleanup.");
            AssertContains(hashEngine, "int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request)", "Phase 33 HashEngine implementation does not yet define the execution-context request entry.");
            AssertContains(hashEngine, "#include \"LegacyCompat/HashThreadEntryProjection.h\"", "Phase 33 HashThread entry does not yet consume the ThreadData projection seam.");
            AssertDoesNotContain(hashEngine, "#include \"Common/ThreadDataAccess.h\"", "Phase 33 HashEngine still consumes the broad ThreadDataAccess shim directly.");
            AssertContains(hashEngine, "HashExecutionContext executionContext = CreateThreadDataHashExecutionContext(*thrdData);", "Phase 35 HashThreadFunc does not yet construct HashExecutionContext through the thread-entry projection seam.");
            AssertContains(hashEngine, "HashRequest request = CreateThreadDataHashRequest(*thrdData);", "Phase 33 HashThreadFunc no longer projects ThreadData into HashRequest through the thread-entry projection seam.");
            AssertContains(hashEngine, "return RunHashRequest(&executionContext, request);", "Phase 33 HashThreadFunc does not yet delegate into RunHashRequest through HashExecutionContext.");
        }, failures);

        Run("Phase 34 narrows core hashing execution onto HashProgressSink while keeping HashEngineObserver as a compatibility adapter", () =>
        {
            string hashProgressSink = ReadRepoFile(repoRoot, @"trunk\source\Runtime\HashProgressSink.h");
            string hashProgressSinkShim = ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressSink.h");
            string hashEngineObserver = ReadRepoFile(repoRoot, @"trunk\source\Adapters\UiBridge\HashEngineObserver.h");
            string hashExecutionContext = ReadRepoFile(repoRoot, @"trunk\source\Runtime\HashExecutionContext.h");
            string hashEngineHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string hashEnginePreparation = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp");
            string hashFileResultWorkflow = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileResultWorkflow.cpp");
            string hashEngineResult = string.Join("\r\n",
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileSizeAccounting.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultEventWorkflow.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp"));
            string hashEngine = ReadHashEngineImplementation(repoRoot);

            AssertContains(hashProgressSinkShim, "#include \"Runtime/HashProgressSink.h\"", "Phase 90 Common HashProgressSink.h should now be a thin Runtime shim.");
            AssertContains(hashProgressSink, "#include \"Domain/ProgressEvent.h\"", "Phase 90 HashProgressSink should now consume ProgressEvent through the Runtime contract.");
            AssertContains(hashProgressSink, "class HashProgressSink", "Phase 34 hash progress sink header is missing the neutral sink seam.");
            AssertContains(hashProgressSink, "virtual int progressMax() = 0;", "Phase 34 hash progress sink does not yet own progress-max queries.");
            AssertContains(hashProgressSink, "virtual void onProgressEvent(const ProgressEvent& progressEvent) = 0;", "Phase 34 hash progress sink does not yet own semantic event dispatch.");

            AssertContains(hashEngineObserver, "#include \"Runtime/HashProgressSink.h\"", "Phase 90 HashProgressEventBridge should now layer directly on top of the Runtime HashProgressSink seam.");
            AssertContains(hashEngineObserver, "class HashProgressEventBridge: public HashProgressSink", "Phase 34 HashProgressEventBridge is not yet narrowed into a compatibility adapter on top of HashProgressSink.");
            AssertContains(hashEngineObserver, "typedef HashProgressEventBridge HashEngineObserver;", "Phase 34 HashEngineObserver compatibility alias is missing.");
            AssertContains(hashEngineObserver, "virtual int progressMax()", "Phase 34 HashProgressEventBridge no longer satisfies the progress-max sink contract.");
            AssertContains(hashEngineObserver, "virtual void onProgressEvent(const ProgressEvent& progressEvent)", "Phase 34 HashProgressEventBridge no longer satisfies the semantic progress-event sink contract.");
            AssertContains(hashExecutionContext, "HashProgressSink& progressSinkObserver;", "Phase 35 hash execution context does not yet carry the neutral progress sink observer.");
            AssertContains(hashExecutionContext, "HashJobState& jobState;", "Phase 35 hash execution context does not yet carry the grouped job-state seam.");
            AssertContains(hashExecutionContext, "HashCancellationState& cancellationState;", "Phase 35 hash execution context does not yet carry the grouped cancellation seam.");
            AssertContains(hashExecutionContext, "HashExecutionContext(HashProgressSink *sink, HashJobState& state, HashCancellationState& cancellation)", "Phase 35 hash execution context does not yet require explicit state dependencies.");
            AssertContains(hashExecutionContext, "GetHashExecutionProgressSink(const HashExecutionContext& executionContext)", "Phase 35 hash execution context does not yet expose progress-sink reads.");
            AssertContains(hashExecutionContext, "ShouldStopHashExecution(const HashExecutionContext& executionContext)", "Phase 35 hash execution context does not yet expose cancellation reads.");
            AssertContains(hashExecutionContext, "AppendHashExecutionResult(HashExecutionContext& executionContext)", "Phase 35 hash execution context does not yet expose result publication.");

            AssertDoesNotContain(hashEngineHeader, "class HashProgressSink;", "Phase 35 HashEngine header still forward declares HashProgressSink after switching to HashExecutionContext.");
            AssertDoesNotContain(hashEngineHeader, "class HashEngineObserver;", "Phase 34 HashEngine header still directly depends on HashEngineObserver.");
            AssertDoesNotContain(hashEngineHeader, "HashProgressSink *observer", "Phase 35 HashEngine header still routes the core entry through the older progress-sink argument.");

            AssertContains(hashEngineInternal, "#include \"Runtime/HashProgressSink.h\"", "Phase 90 HashEngineInternal should now consume the Runtime HashProgressSink seam.");
            AssertContains(hashEngineInternal, "#include \"Runtime/HashExecutionContext.h\"", "Phase 90 HashEngineInternal should now consume HashExecutionContext through the Runtime seam directly.");
            AssertDoesNotContain(hashEngineInternal, "#include \"Common/HashEngineObserver.h\"", "Phase 34 HashEngineInternal still directly depends on HashEngineObserver.");
            AssertContains(hashEngineInternal, "HashExecutionContext *executionContext", "Phase 35 HashEngineInternal does not yet route execution seams through HashExecutionContext.");

            AssertContains(hashEnginePreparation, "PrepareHashingWork(HashExecutionContext *executionContext, const HashRequest& request", "Phase 35 preparation seam does not yet consume HashExecutionContext.");
            AssertContains(hashEnginePreparation, "EmitPathResult(HashExecutionContext *executionContext, HashResult& result)", "Phase 35 preparation seam does not yet publish file-start events through HashExecutionContext.");
            AssertContains(hashFileResultWorkflow, "AppendHashExecutionResult(*executionContext)", "Phase 35 file-result workflow seam does not yet publish results through HashExecutionContext.");

            AssertContains(hashEngineResult, "PrepareFileMetaResult(HashExecutionContext *executionContext, HashResult& result", "Phase 35 result seam does not yet consume HashExecutionContext.");
            AssertContains(hashEngineResult, "EmitHashResult(HashExecutionContext *executionContext, HashResult& result, bool uppercase)", "Phase 35 result seam does not yet publish hash results through HashExecutionContext.");
            AssertContains(hashEngineResult, "ReplaceHashExecutionCountedFileSize(*executionContext, fSizes[fileIndex], fsize);", "Phase 35 result seam does not yet route counted-size replacement through HashExecutionContext.");

            AssertContains(hashEngine, "UpdateHashExecutionProgress(HashExecutionContext *executionContext", "Phase 35 engine progress updates do not yet route through HashExecutionContext.");
            AssertContains(hashEngine, "int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request)", "Phase 35 HashEngine does not yet narrow the request-driven core entry onto HashExecutionContext.");
            AssertContains(hashEngine, "ShouldStopHashExecution(*executionContext)", "Phase 35 HashEngine does not yet read stop state through HashExecutionContext.");
            AssertContains(hashEngine, "ResetHashExecutionTotalSize(*executionContext);", "Phase 35 HashEngine does not yet reset counted size through HashExecutionContext.");
        }, failures);

        Run("Phase 35 routes semantic result events through HashResult while narrowing adapter compatibility to event-oriented callbacks", () =>
        {
            string progressEvent = ReadRepoFile(repoRoot, @"trunk\source\Domain\ProgressEvent.h");
            string hashEngineObserver = ReadRepoFile(repoRoot, @"trunk\source\Adapters\UiBridge\HashEngineObserver.h");
            string hashEnginePreparation = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp");
            string hashFileResultWorkflow = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileResultWorkflow.cpp");
            string hashEngineResult = string.Join("\r\n",
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileSizeAccounting.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultEventWorkflow.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp"));

            AssertContains(progressEvent, "CreateResultProgressEvent(ProgressEventType eventType, const HashResult& result)", "Phase 35 does not yet expose ProgressEvent creation directly from HashResult.");
            AssertContains(progressEvent, "CreateFileStartedProgressEvent(const HashResult& result)", "Phase 35 does not yet expose file-started events directly from HashResult.");
            AssertContains(progressEvent, "CreateFileMetaReadyProgressEvent(const HashResult& result)", "Phase 35 does not yet expose file-meta events directly from HashResult.");
            AssertContains(progressEvent, "CreateFileHashReadyProgressEvent(const HashResult& result, bool uppercaseDigest)", "Phase 35 does not yet expose file-hash events directly from HashResult.");
            AssertContains(progressEvent, "CreateFileFailedProgressEvent(const HashResult& result)", "Phase 35 does not yet expose file-failed events directly from HashResult.");

            AssertContains(hashEngineObserver, "virtual void handleFileResultProgressEvent(const HashResult& result,", "Phase 35 HashProgressEventBridge does not yet expose a HashResult-based file-result event contract.");
            AssertContains(hashEngineObserver, "handleFileResultProgressEvent(progressEvent.result, progressEvent.type, progressEvent.uppercaseDigest);", "Phase 35 HashProgressEventBridge does not yet dispatch file-result events through HashResult.");
            AssertDoesNotContain(hashEngineObserver, "showFileHash(*progressEvent.result.sourceResult, progressEvent.uppercaseDigest);", "Phase 35 HashEngineObserver still depends on progressEvent.result.sourceResult for hash-ready dispatch.");
            AssertDoesNotContain(hashEngineObserver, "CreateCompatibilityResultData(result);", "Phase 35 HashEngineObserver still rebuilds ResultData instead of staying as a thin HashResult wrapper.");

            AssertContains(hashFileResultWorkflow, "CreateFileStartedProgressEvent(result)", "Phase 35 file-result workflow seam does not yet emit file-start events through HashResult.");
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

            AssertContains(observer, "virtual void handleFileResultProgressEvent(const HashResult& result,", "Phase 36 observer seam does not yet expose a HashResult-based file-result event contract.");
            AssertContains(observer, "virtual int getProgressValueMax() = 0;", "Phase 36 observer seam does not yet expose a neutral progress query contract.");

            AssertContains(hashResultProjection, "AssignHashResultCoreToNet", "Phase 36 does not yet project HashResult core fields directly to managed result DTOs.");
            AssertContains(hashResultProjection, "AssignHashResultDigestsToNet", "Phase 36 does not yet project HashResult digest fields directly to managed result DTOs.");
            AssertContains(hashResultProjection, "ProjectHashResultToNet(const HashResult& result, TStringConverter convertString)", "Phase 36 does not yet expose direct HashResult-to-managed projection.");

            AssertContains(managedDispatch, "#include \"Common/HashResultProjection.h\"", "Phase 36 managed bridge dispatch does not yet depend on HashResultProjection.");
            AssertContains(managedDispatch, "DispatchManagedBridgeResultEventByType(const HashResult& result", "Phase 36 managed bridge dispatch does not yet expose HashResult-based projection dispatch.");
            AssertContains(managedDispatch, "ProjectHashResultToNet<TResultDataNet, TResultStateNet>(result, convertString)", "Phase 36 managed bridge dispatch does not yet project HashResult directly.");

            AssertContains(bridgeMfcHeader, "virtual void handleFileResultProgressEvent(const HashResult& result,", "Phase 36 MFC bridge header does not yet accept HashResult file-result consumption.");

            AssertContains(bridgeWuiHeader, "virtual void handleFileResultProgressEvent(const HashResult& result,", "Phase 36 WinUI bridge header does not yet accept HashResult file-result consumption.");
            AssertContains(bridgeWuiHeader, "DispatchProjectedResultEvent(const HashResult& result", "Phase 36 WinUI bridge helper does not yet narrow to HashResult.");
            AssertContains(bridgeWuiSource, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase", "Phase 36 WinUI bridge does not yet project HashResult directly to managed delegates.");
            AssertContains(bridgeWuiSource, "GetManagedResultEventType(eventType)", "Phase 36 WinUI bridge does not yet route ProgressEventType through a neutral managed dispatch selector.");

            AssertContains(bridgeUwpHeader, "virtual void handleFileResultProgressEvent(const HashResult& result,", "Phase 36 UWP bridge header does not yet accept HashResult file-result consumption.");
            AssertContains(bridgeUwpHeader, "DispatchProjectedResultEvent(const HashResult& result", "Phase 36 UWP bridge helper does not yet narrow to HashResult.");
            AssertContains(bridgeUwpSource, "DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase", "Phase 36 UWP bridge does not yet project HashResult directly to managed delegates.");
            AssertContains(bridgeUwpSource, "GetManagedResultEventType(eventType)", "Phase 36 UWP bridge does not yet route ProgressEventType through a neutral managed dispatch selector.");
        }, failures);

        Run("Phase 37 routes managed digest-search projection through HashResult search and projection seams", () =>
        {
            string hashResultSearch = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultSearch.h");
            string hashResultProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultProjection.h");
            string legacyManagedHashMgmtAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ManagedHashMgmtAccess.h");
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

            AssertFileMissing(repoRoot, @"trunk\source\Common\ManagedHashMgmtAccess.h", "Phase 37 Common ManagedHashMgmtAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertContains(legacyManagedHashMgmtAccess, "#include \"Common/HashResultProjection.h\"", "Phase 37 managed hash-management seam does not yet consume HashResultProjection.");
            AssertContains(legacyManagedHashMgmtAccess, "CreateProjectedDigestMatchingHashResults<THashResultNet, THashResultStateNet, TResultArray>(", "Phase 37 managed hash-management seam does not yet route digest-search projection through HashResultProjection.");
            AssertDoesNotContain(legacyManagedHashMgmtAccess, "#include \"Common/ResultDataProjection.h\"", "Phase 37 managed hash-management seam still depends directly on ResultDataProjection.");
        }, failures);

        Run("Phase 38 promotes managed query results onto HashResultNet as the primary managed query contract", () =>
        {
            string legacyManagedHashMgmtAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ManagedHashMgmtAccess.h");
            string clrHashResultNet = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashResultNet.h");
            string clrHashMgmtHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.h");
            string clrHashMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
            string uwpHashResultNet = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashResultNet.h");
            string uwpHashMgmtHeader = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.h");
            string uwpHashMgmt = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\HashMgmt.cpp");
            string winUiPage = ReadRepoFile(repoRoot, @"trunk\source\WinUI\MainPage.xaml.cs");
            string winUwpPage = ReadRepoFile(repoRoot, @"trunk\source\WinUWP\MainPage.xaml.cs");

            AssertFileMissing(repoRoot, @"trunk\source\Common\ManagedHashMgmtAccess.h", "Phase 38 Common ManagedHashMgmtAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertContains(legacyManagedHashMgmtAccess, "static inline TResultArray CreateProjectedManagedDigestMatchingHashResults(", "Phase 38 managed hash-management seam does not yet expose HashResultNet-based digest search projection.");

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
            AssertContains(clrDelegatesHeader, "void PublishFileHash(HashResultNet hashResultNet, bool uppercase);", "Phase 39 CLR delegate header does not yet switch ShowFileHash to HashResultNet.");
            AssertContains(clrDelegatesSource, "void UIBridgeDelegates::PublishFileHash(HashResultNet hashResultNet, bool uppercase)", "Phase 39 CLR delegate implementation does not yet switch ShowFileHash to HashResultNet.");

            AssertContains(uwpDelegateHeader, "#include \"HashResultNet.h\"", "Phase 39 UWP delegate header does not yet consume HashResultNet.");
            AssertContains(uwpDelegateHeader, "public delegate void HashResultEventHandler(HashResultNet);", "Phase 39 UWP delegate header does not yet expose HashResultNet event handlers.");
            AssertContains(uwpDelegateHeader, "void PublishFileHash(HashResultNet hashResultNet, Platform::Boolean uppercase);", "Phase 39 UWP delegate header does not yet switch ShowFileHash to HashResultNet.");
            AssertContains(uwpDelegateSource, "void UIBridgeDelegate::PublishFileHash(HashResultNet hashResultNet, Boolean uppercase)", "Phase 39 UWP delegate implementation does not yet switch ShowFileHash to HashResultNet.");

            AssertContains(bridgeWuiSource, "m_hashUiEvents->PublishFileHash(hashResultNet, hashUppercase);", "Phase 39 WinUI bridge does not yet forward realtime hash events as HashResultNet.");
            AssertContains(bridgeUwpSource, "m_hashUiEvents->PublishFileHash(hashResultNet, hashUppercase);", "Phase 39 UWP bridge does not yet forward realtime hash events as HashResultNet.");

            AssertContains(winUiPage, "private void AppendFileResultToTextMain(HashResultNet hashResult, bool uppercase)", "Phase 39 WinUI page does not yet render realtime results directly from HashResultNet.");
            AssertContains(winUiPage, "private void HashUiEvents_FileHashHandler(HashResultNet hashResult, bool uppercase)", "Phase 39 WinUI page does not yet accept realtime HashResultNet hash events.");
            AssertDoesNotContain(winUiPage, "CreateCompatibilityResultData(", "Phase 39 WinUI page still rebuilds compatibility ResultDataNet for managed rendering.");

            AssertContains(winUwpPage, "private void AppendFileResultToTextMain(HashResultNet hashResult, bool uppercase)", "Phase 39 UWP page does not yet render realtime results directly from HashResultNet.");
            AssertContains(winUwpPage, "private void HashUiEvents_FileHashHandler(HashResultNet hashResult, bool uppercase)", "Phase 39 UWP page does not yet accept realtime HashResultNet hash events.");
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
            AssertContains(resultNetProjection, "DispatchResultDigestValueById(const HashAlgorithmId& algorithmId, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)", "Phase 41 ResultNetProjection does not yet own the shared descriptor-id dispatch helper.");
            AssertContains(resultNetProjection, "static inline TResultDataNet AssignResultDigestToNetById(TResultDataNet resultDataNet, const HashAlgorithmId& algorithmId, TResultString digestValue)", "Phase 41 ResultNetProjection does not yet own the shared managed digest assignment helper.");
            AssertDoesNotContain(resultNetProjection, "DispatchResultDigestValueByType(ResultDigestType digestType, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)", "Phase 41 ResultNetProjection still exposes the removed digest-type dispatch helper.");
            AssertDoesNotContain(resultNetProjection, "AssignResultDigestToNet(TResultDataNet resultDataNet, ResultDigestType digestType, TResultString digestValue)", "Phase 41 ResultNetProjection still exposes the removed digest-type digest-assignment helper.");
            AssertContains(resultProjection, "#include \"Common/ResultNetProjection.h\"", "Phase 41 ResultDataProjection does not yet depend on the neutral ResultNetProjection seam.");
            AssertContains(hashResultProjection, "#include \"Common/ResultNetProjection.h\"", "Phase 41 HashResultProjection does not yet depend on the neutral ResultNetProjection seam.");
            AssertDoesNotContain(hashResultProjection, "#include \"Common/ResultDataProjection.h\"", "Phase 41 HashResultProjection still depends on the legacy ResultDataProjection header.");
            AssertDoesNotContain(managedDispatch, "DispatchManagedBridgeResultEventByType(const ResultData& result", "Phase 41 ManagedBridgeDispatch still exposes the deprecated ResultData-based managed dispatch overload.");
            AssertContains(managedDispatch, "DispatchManagedBridgeResultEventByType(const HashResult& result", "Phase 41 ManagedBridgeDispatch does not yet expose the HashResult-only managed dispatch overload.");
        }, failures);

        Run("Phase 42 removes ResultData-based progress and observer compatibility overloads so HashResult stays a pure event contract", () =>
        {
            string hashResult = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashResult.h");
            string progressEvent = ReadRepoFile(repoRoot, @"trunk\source\Domain\ProgressEvent.h");
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
            string legacyThreadResultAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataResultAccess.h");
            string legacyManagedHashMgmtAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ManagedHashMgmtAccess.h");
            string filesHashSearchController = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashSearchController.cpp");
            string hashBridgeMac = ReadRepoFile(repoRoot, @"trunk\source\OSXUI\HashBridge.mm");

            AssertContains(hashResultSearch, "VisitHashResults(const HashResultList& resultList, THashResultVisitor visitor)", "Phase 45 HashResultSearch does not yet expose whole-list HashResult traversal.");
            AssertContains(hashResultSearch, "VisitMatchingHashResults(const HashResultList& resultList, THashResultPredicate predicate, THashResultVisitor visitor)", "Phase 45 HashResultSearch does not yet expose shared HashResult matching traversal.");
            AssertContains(hashResultSearch, "VisitPathAndDigestMatchingHashResults(const HashResultList& resultList, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, THashResultVisitor visitor)", "Phase 45 HashResultSearch does not yet expose shared path+digest HashResult traversal.");
            AssertContains(hashResultSearch, "NormalizeHashResultPathSearchText(const sunjwbase::tstring& pathText)", "Phase 45 HashResultSearch does not yet own path normalization.");
            AssertContains(hashResultSearch, "NormalizeHashResultDigestSearchText(const sunjwbase::tstring& digestText)", "Phase 45 HashResultSearch does not yet own digest normalization.");

            AssertContains(hashResultProjection, "VisitHashResults(resultList, [&](const HashResult& hashResult)", "Phase 45 HashResultProjection does not yet reuse shared HashResult traversal.");
            AssertContains(hashResultProjection, "VisitMatchingHashResults(resultList, predicate, [&](const HashResult& hashResult)", "Phase 45 HashResultProjection does not yet reuse shared HashResult matching traversal.");

            AssertFileMissing(repoRoot, @"trunk\source\Common\ThreadDataResultAccess.h", "Phase 45 Common ThreadDataResultAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertContains(legacyThreadResultAccess, "VisitThreadDataHashResults(const ThreadData& threadData, THashResultVisitor visitor)", "Phase 45 ThreadData result access does not yet expose HashResult traversal.");
            AssertContains(legacyThreadResultAccess, "VisitThreadDataPathAndDigestMatchingHashResults(const ThreadData& threadData, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, THashResultVisitor visitor)", "Phase 45 ThreadData result access does not yet expose HashResult path+digest traversal.");

            AssertFileMissing(repoRoot, @"trunk\source\Common\ManagedHashMgmtAccess.h", "Phase 45 Common ManagedHashMgmtAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertContains(legacyManagedHashMgmtAccess, "NormalizeHashResultDigestSearchText(hashToFind)", "Phase 45 managed hash management does not yet normalize digest queries through HashResultSearch.");
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

        Run("Phase 47 introduces a native C++ runtime test project and caches the vendored OpenSSL package for parallel native builds", () =>
        {
            string workflow = ReadRepoFile(repoRoot, @".github\workflows\windows-build.yml");
            string previewWorkflow = ReadRepoFile(repoRoot, @".github\workflows\winui-preview-build.yml");
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
            AssertContains(nativeRuntimeSource, "RunHashRequest_IgnoresUnknownAndDuplicateAlgorithmsInRequest", "Phase 47 native runtime tests do not yet cover unknown/duplicate request algorithm sanitization.");
            AssertContains(nativeRuntimeSource, "ThreadDataExecutionAccess_IgnoresUnknownAlgorithmSelection", "Phase 47 native runtime tests do not yet cover unknown algorithm selection hardening.");
            AssertContains(nativeRuntimeSource, "RunHashRequest_CancelsWhenStopRequestedBeforeStart", "Phase 47 native runtime tests do not yet cover cooperative cancellation.");
            AssertContains(nativeRuntimeSource, "RunHashRequest_PropagatesUppercasePreferenceInHashReadyEvent", "Phase 47 native runtime tests do not yet cover uppercase digest event propagation.");
            AssertContains(nativeRuntimeSource, "RunHashRequest_CancelsDuringFileProgressAndSkipsRemainingFiles", "Phase 47 native runtime tests do not yet cover mid-run cancellation.");
            AssertContains(nativeRuntimeSource, "static HashExecutionContext CreateExecutionContext(CapturingProgressSink& progressSink, HashJobState& jobState, HashCancellationState& cancellationState)", "Phase 47 native runtime tests do not yet expose an execution-context helper aligned with the explicit-context constructor seam.");
            AssertContains(nativeRuntimeSource, "return HashExecutionContext(&progressSink, jobState, cancellationState);", "Phase 47 native runtime tests do not yet construct HashExecutionContext through explicit state dependencies.");
            AssertDoesNotContain(nativeRuntimeSource, "HashExecutionContext executionContext;", "Phase 47 native runtime tests still rely on default HashExecutionContext construction.");
            AssertDoesNotContain(nativeRuntimeSource, "executionContext.jobState = &jobState;", "Phase 47 native runtime tests still mutate job-state pointers directly.");
            AssertDoesNotContain(nativeRuntimeSource, "executionContext.cancellationState = &cancellationState;", "Phase 47 native runtime tests still mutate cancellation-state pointers directly.");
            AssertDoesNotContain(nativeRuntimeSource, "executionContext.progressSink = &progressSink;", "Phase 47 native runtime tests still mutate progress-sink pointers directly.");
            AssertContains(nativeRuntimeMain, "All native runtime tests passed", "Phase 47 native runtime test main does not yet report aggregate success.");

            AssertContains(solution, "FHash.NativeRuntimeTests", "Phase 47 fileshash15.sln does not yet include the native runtime test project.");
            AssertContains(gitignore, "native-runtime-tests/**/x64/", "Phase 47 .gitignore does not yet ignore native runtime test build outputs.");

            AssertContains(workflow, "native-runtime-tests:", "Phase 47 workflow does not yet define a native-runtime-tests job.");
            AssertContains(workflow, "msbuild native-runtime-tests/FHash.NativeRuntimeTests/FHash.NativeRuntimeTests.vcxproj", "Phase 47 workflow does not yet build the native runtime test project.");
            AssertContains(workflow, @"native-runtime-tests\FHash.NativeRuntimeTests\x64\Release\FHash.NativeRuntimeTests.exe", "Phase 47 workflow does not yet execute the native runtime test binary.");
            AssertContains(workflow, "prepare-openssl-vendor-x64:", "Phase 47 workflow does not yet define the shared OpenSSL vendor preparation job.");
            AssertContains(workflow, "Restore cached OpenSSL vendor x64", "Phase 47 workflow does not yet restore the shared OpenSSL vendor cache.");
            AssertContains(workflow, "actions/cache@v4", "Phase 47 workflow does not yet cache the shared OpenSSL vendor build.");
            AssertContains(workflow, "name: FHash-openssl-vendor-x64", "Phase 47 workflow does not yet upload the shared OpenSSL vendor artifact.");
            AssertContains(workflow, "Download OpenSSL vendor x64 artifact", "Phase 47 workflow does not yet download the shared OpenSSL vendor artifact in downstream native jobs.");
            AssertDoesNotContain(workflow, "prepare-openssl-vendor-x64:\r\n    needs:", "Phase 47 shared OpenSSL vendor preparation should start independently instead of waiting for managed gates.");
            AssertInOrder(
                workflow,
                new[]
                {
                    "build-legacy-x64:",
                    "needs:",
                    "- security-regression",
                    "- unit-tests",
                    "- prepare-openssl-vendor-x64"
                },
                "Phase 47 build-legacy-x64 does not yet depend on the shared OpenSSL vendor artifact.");
        }, failures);

        Run("Phase 48 exercises the publish-release chain on manual dispatch while reserving GitHub releases for version tags", () =>
        {
            string workflow = ReadRepoFile(repoRoot, @".github\workflows\windows-build.yml");

            AssertContains(workflow, "publish-release:", "Phase 48 workflow does not yet define a publish-release job.");
            AssertContains(workflow, "if: github.event_name == 'workflow_dispatch' || startsWith(github.ref, 'refs/tags/v')", "Phase 48 publish-release is not yet limited to manual rehearsal runs and version tags.");
            AssertContains(workflow, "pattern: LHash-legacy-*", "Phase 48 publish-release does not yet target the lightweight native release artifacts.");
            AssertContains(workflow, "merge-multiple: true", "Phase 48 publish-release does not yet merge downloaded artifacts into a single release-assets directory.");
            AssertContains(workflow, "Stage release rehearsal bundle", "Phase 48 publish-release does not yet stage a rehearsal bundle.");
            AssertContains(workflow, "RELEASE_MANIFEST.txt", "Phase 48 publish-release does not yet emit a release manifest.");
            AssertContains(workflow, "LHash-release-rehearsal", "Phase 48 publish-release does not yet upload a release rehearsal artifact.");
            AssertContains(workflow, "release_mode=\"rehearsal\"", "Phase 48 publish-release rehearsal mode is not yet recorded.");
            AssertContains(workflow, "release_mode=\"tagged-release\"", "Phase 48 publish-release tag mode is not yet recorded.");
            AssertContains(workflow, "if: startsWith(github.ref, 'refs/tags/v')", "Phase 48 publish-release does not yet reserve GitHub release publishing for version tags.");
            AssertContains(workflow, "softprops/action-gh-release@v2", "Phase 48 publish-release does not yet invoke the GitHub release publisher.");
            AssertContains(workflow, "release-assets/LHash-legacy-x64-*.zip", "Phase 48 publish-release does not yet publish the lightweight native release zip.");
            AssertContains(workflow, "release-staging/RELEASE_MANIFEST.txt", "Phase 48 publish-release does not yet attach the release manifest.");
            AssertDoesNotContain(workflow, "artifacts/winui-x64-*", "Phase 48 WinUI artifact upload still includes the duplicated uncompressed publish directory.");
        }, failures);

        Run("Phase 49 promotes single-file hashing into a dedicated runner seam so HashEngine.cpp stays orchestration-focused", () =>
        {
            string hashEngine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string hashFileRunner = string.Join(
                "\r\n",
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileRunner.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptWorkflow.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestUpdater.cpp"),
                ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressTracker.cpp"));
            string hashScheduler = ReadRepoFile(repoRoot, @"trunk\source\Common\HashScheduler.cpp");
            string hashSchedulerDispatch = ReadRepoFile(repoRoot, @"trunk\source\Common\HashSchedulerDispatch.cpp");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");

            AssertContains(hashEngine, "RunHashScheduler(executionContext, request, executionPlan, isSizeCaled, fSizes)", "Phase 49 HashEngine.cpp does not yet delegate orchestration to the extracted scheduler seam.");
            AssertDoesNotContain(hashEngine, "static bool ProcessOpenedFileHashing(", "Phase 49 HashEngine.cpp still owns the opened-file hashing loop.");
            AssertContains(hashFileRunner, "bool ProcessOpenedFileHashing(", "Phase 49 digest pipeline does not yet own the opened-file hashing loop.");
            AssertContains(hashFileRunner, "bool RunFileHashAttempt(", "Phase 49 HashFileRunner.cpp does not yet expose the single-file execution seam.");
            AssertContains(hashFileRunner, "YieldHashThread();", "Phase 49 HashFileRunner.cpp does not yet own per-file scheduler yielding.");
            AssertDoesNotContain(hashFileRunner, "FileExecutionState executionState = { 0 };", "Phase 49 HashFileRunner.cpp should reuse grouped execution state from HashEngine.cpp instead of creating a new local bundle.");
            AssertContains(hashScheduler, "FileExecutionState executionState;", "Phase 49 HashScheduler.cpp does not yet preserve grouped file execution state for the runner seam.");
            AssertContains(hashSchedulerDispatch, "VisitHashRequestFiles(request, [&](uint32_t fileIndex, const sunjwbase::tstring& fullPath)", "Phase 49 HashSchedulerDispatch.cpp does not yet own request-file iteration.");
            AssertContains(hashSchedulerDispatch, "RunFileHashAttempt(executionContext, request, fileIndex, fullPath, isSizeCaled, fSizes,", "Phase 49 HashSchedulerDispatch.cpp does not yet route single-file work into HashFileRunner.");
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
            string hashSchedulerDispatch = ReadRepoFile(repoRoot, @"trunk\source\Common\HashSchedulerDispatch.cpp");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashEngine, "if (!RunHashScheduler(executionContext, request, executionPlan, isSizeCaled, fSizes))", "Phase 50 HashEngine.cpp does not yet route request scheduling through HashScheduler.");
            AssertDoesNotContain(hashEngine, "VisitHashRequestFiles(request, [&](uint32_t fileIndex, const tstring& fullPath)", "Phase 50 HashEngine.cpp still owns file iteration instead of delegating it to HashScheduler.");
            AssertDoesNotContain(hashEngine, "ThreadPool threadPool(5);", "Phase 50 HashEngine.cpp still owns the thread-pool scheduler instead of delegating it to HashScheduler.");

            AssertContains(hashScheduler, "bool RunHashScheduler(HashExecutionContext *executionContext, const HashRequest& request, const HashJobExecutionPlan& executionPlan, bool isSizeCaled, ULLongVector& fSizes)", "Phase 50 HashScheduler.cpp does not yet expose the dedicated request scheduler seam.");
            AssertContains(hashScheduler, "ExecuteScheduledHashRequestFiles(executionContext, request, isSizeCaled, fSizes, &executionState", "Phase 50 HashScheduler.cpp does not yet delegate the scheduled file loop through HashSchedulerDispatch.");
            AssertContains(hashSchedulerDispatch, "bool ExecuteScheduledHashRequestFiles(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState", "Phase 50 HashSchedulerDispatch.cpp does not yet isolate the scheduled file loop.");
            AssertContains(hashScheduler, "ThreadPool threadPool(", "Phase 50 HashScheduler.cpp does not yet own the scheduler thread-pool lifecycle.");
            AssertContains(hashSchedulerDispatch, "VisitHashRequestFiles(request, [&](uint32_t fileIndex, const sunjwbase::tstring& fullPath)", "Phase 50 HashSchedulerDispatch.cpp does not yet iterate files through HashRequest.");
            AssertContains(hashEngineInternal, "bool RunHashScheduler(HashExecutionContext *executionContext, const HashRequest& request, const HashJobExecutionPlan& executionPlan, bool isSizeCaled, ULLongVector& fSizes);", "Phase 50 HashEngineInternal.h does not yet declare the request scheduler seam.");

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
            AssertDoesNotContain(hashEngineResult, "FinalizeDigestStrings(", "Phase 51 HashEngineResult.cpp should no longer own digest finalization after lifecycle extraction.");
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
            string hashFileAttemptWorkflow = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptWorkflow.cpp");
            string hashDigestExecution = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp");
            string hashDigestQueue = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp");
            string hashDigestQueueHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.h");
            string hashDigestPipeline = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp");
            string hashDigestPipelineHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.h");
            string hashDigestSinglePass = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestSinglePass.cpp");
            string hashDigestSinglePassHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestSinglePass.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashFileRunner, "ExecuteFileHashAttemptWorkflow(executionContext, request, fileIndex, fullPath, isSizeCaled, fSizes, executionState", "Phase 52 HashFileRunner.cpp does not yet delegate file-attempt execution through HashFileAttemptWorkflow.");
            AssertContains(hashFileAttemptWorkflow, "bool wasStopped = ProcessOpenedFileHashing(executionContext, request, result, fileIndex, isSizeCaled, fSizes, executionState", "Phase 52 HashFileAttemptWorkflow.cpp does not yet delegate opened-file digest updates through the digest pipeline seam.");
            AssertDoesNotContain(hashFileRunner, "future<void> taskSHA512Update", "Phase 52 HashFileRunner.cpp still owns SHA512 digest worker futures instead of delegating them to HashDigestPipeline.");

            AssertContains(hashDigestQueueHeader, "class DigestDataBuffer", "Phase 52 HashDigestQueue.h does not yet expose queue buffer state.");
            AssertContains(hashDigestQueueHeader, "uint64_t CalculateFileChunkIterations(uint64_t fileSize, unsigned int preferredLength);", "Phase 52 HashDigestQueue.h does not yet expose chunk-iteration calculation.");
            AssertContains(hashDigestQueueHeader, "bool ProcessOpenedFileHashingParallel(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fileSize, bool isSizeCaled,", "Phase 52 HashDigestQueue.h does not yet expose parallel digest queue processing.");
            AssertContains(hashDigestQueue, "DigestDataBuffer::DigestDataBuffer(unsigned int preferredLength)", "Phase 52 HashDigestQueue.cpp does not yet own buffered digest update state.");
            AssertContains(hashDigestQueue, "uint64_t CalculateFileChunkIterations(uint64_t fileSize, unsigned int preferredLength)", "Phase 52 HashDigestQueue.cpp does not yet own chunk-iteration calculations.");
            AssertContains(hashDigestQueue, "bool ProcessOpenedFileHashingParallel(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fileSize, bool isSizeCaled,", "Phase 52 HashDigestQueue.cpp does not yet own parallel opened-file digest queue processing.");

            AssertContains(hashDigestPipelineHeader, "uint64_t CalculateFileChunkIterations(uint64_t fsize, unsigned int preferredBufferLength);", "Phase 52 HashDigestPipeline.h does not yet expose the chunk-iteration helper seam.");
            AssertContains(hashDigestPipelineHeader, "bool ProcessOpenedFileHashing(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex,", "Phase 52 HashDigestPipeline.h does not yet expose the opened-file digest pipeline seam.");
            AssertContains(hashDigestSinglePassHeader, "bool ProcessOpenedFileHashingSinglePass(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fsize, bool isSizeCaled,", "Phase 52 HashDigestSinglePass.h does not yet expose the single-thread digest processing seam.");
            AssertContains(hashDigestPipeline, "bool ProcessOpenedFileHashing(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex,", "Phase 52 HashDigestPipeline.cpp does not yet own opened-file digest update orchestration.");
            AssertContains(hashDigestPipeline, "HashDigestRuntimePlan digestRuntimePlan = CreateHashDigestRuntimePlan(executionState->executionPlan);", "Phase 52 HashDigestPipeline.cpp does not yet initialize digest runtime planning through the job execution-plan seam.");
            AssertContains(hashDigestPipeline, "unsigned int preferredBufferLength = GetHashDigestRuntimePreferredBufferLength(digestRuntimePlan);", "Phase 52 HashDigestPipeline.cpp does not yet consume digest runtime preferred-buffer planning.");
            AssertContains(hashDigestExecution, "bool ExecuteOpenedFileDigestUpdate(HashExecutionContext *executionContext, const HashDigestRuntimePlan& digestRuntimePlan, uint64_t fsize, bool isSizeCaled,", "Phase 52 HashDigestExecution.cpp does not yet expose digest execution dispatch.");
            AssertContains(hashDigestExecution, "const DigestUpdateRequest& digestUpdateRequest = GetHashDigestRuntimeUpdateRequest(digestRuntimePlan);", "Phase 52 HashDigestExecution.cpp does not yet resolve digest update selection through the runtime-plan seam.");
            AssertContains(hashDigestExecution, "HashDigestExecutionMode digestExecutionMode = GetHashDigestRuntimeExecutionMode(digestRuntimePlan);", "Phase 52 HashDigestExecution.cpp does not yet resolve digest execution mode through the runtime-plan seam.");
            AssertContains(hashDigestExecution, "const HashDigestQueuePlan& digestQueuePlan = GetHashDigestRuntimeQueuePlan(digestRuntimePlan);", "Phase 52 HashDigestExecution.cpp does not yet resolve digest queue planning through the runtime-plan seam.");
            AssertContains(hashDigestQueue, "UpdateDigestContextsParallel(digestUpdateRequest", "Phase 52 HashDigestQueue.cpp does not yet delegate parallel digest updates to the digest updater seam.");
            AssertContains(hashDigestPipeline, "ExecuteOpenedFileDigestUpdate(executionContext, digestRuntimePlan, fsize, isSizeCaled, executionState", "Phase 52 HashDigestPipeline.cpp does not yet delegate digest execution dispatch to HashDigestExecution.");
            AssertContains(hashDigestSinglePass, "UpdateDigestContextsSequential(digestUpdateRequest", "Phase 52 HashDigestSinglePass.cpp does not yet delegate sequential digest updates to the digest updater seam.");
            AssertContains(hashDigestQueue, "UpdateHashExecutionProgress(executionContext, fileSize, isSizeCaled, ptrDataBufCalc->datalen, &executionState->progressState);", "Phase 52 HashDigestQueue.cpp does not yet route per-buffer progress updates through the progress tracker seam.");
            AssertContains(hashDigestSinglePass, "UpdateHashExecutionProgress(executionContext, fsize, isSizeCaled, databuf.datalen, &executionState->progressState);", "Phase 52 HashDigestSinglePass.cpp does not yet route single-thread progress updates through the progress tracker seam.");

            AssertContains(hashEngineInternal, "#include \"Common/HashDigestQueue.h\"", "Phase 52 HashEngineInternal.h does not yet consume the digest queue seam.");
            AssertContains(hashEngineInternal, "#include \"Common/HashDigestPipeline.h\"", "Phase 52 HashEngineInternal.h does not yet consume the digest pipeline seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestQueue.cpp", "Phase 52 desktop native core project does not yet compile HashDigestQueue.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestQueue.h", "Phase 52 desktop native core project does not yet include HashDigestQueue.h.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestPipeline.cpp", "Phase 52 desktop native core project does not yet compile HashDigestPipeline.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestPipeline.h", "Phase 52 desktop native core project does not yet include HashDigestPipeline.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestQueue.cpp", "Phase 52 desktop native core filters do not yet expose HashDigestQueue.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestQueue.h", "Phase 52 desktop native core filters do not yet expose HashDigestQueue.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestPipeline.cpp", "Phase 52 desktop native core filters do not yet expose HashDigestPipeline.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestPipeline.h", "Phase 52 desktop native core filters do not yet expose HashDigestPipeline.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestQueue.cpp", "Phase 52 UWP native project does not yet compile HashDigestQueue.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestQueue.h", "Phase 52 UWP native project does not yet include HashDigestQueue.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestPipeline.cpp", "Phase 52 UWP native project does not yet compile HashDigestPipeline.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestPipeline.h", "Phase 52 UWP native project does not yet include HashDigestPipeline.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestQueue.cpp", "Phase 52 UWP native filters do not yet expose HashDigestQueue.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestQueue.h", "Phase 52 UWP native filters do not yet expose HashDigestQueue.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestPipeline.cpp", "Phase 52 UWP native filters do not yet expose HashDigestPipeline.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestPipeline.h", "Phase 52 UWP native filters do not yet expose HashDigestPipeline.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestQueue.cpp", "Phase 52 WinUI native project should keep consuming the shared native core instead of compiling HashDigestQueue.cpp directly.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestPipeline.cpp", "Phase 52 WinUI native project should keep consuming the shared native core instead of compiling HashDigestPipeline.cpp directly.");
        }, failures);

        Run("Phase 53 promotes digest update fan-out and algorithm selection into a dedicated updater seam", () =>
        {
            string hashDigestUpdater = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestUpdater.cpp");
            string hashDigestUpdaterHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestUpdater.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashDigestUpdaterHeader, "struct DigestUpdateRequest", "Phase 53 HashDigestUpdater.h does not yet expose the digest update request seam.");
            AssertContains(hashDigestUpdaterHeader, "DigestUpdateRequest CreateDigestUpdateRequest(const HashRequest& request);", "Phase 53 HashDigestUpdater.h does not yet expose digest update request creation.");
            AssertContains(hashDigestUpdaterHeader, "void UpdateDigestContextsSequential(const DigestUpdateRequest& digestUpdateRequest", "Phase 53 HashDigestUpdater.h does not yet expose sequential digest updates.");
            AssertContains(hashDigestUpdaterHeader, "void UpdateDigestContextsParallel(const DigestUpdateRequest& digestUpdateRequest", "Phase 53 HashDigestUpdater.h does not yet expose parallel digest updates.");
            AssertContains(hashDigestUpdaterHeader, "std::vector<HashDigestOperationDescriptor> operationDescriptors;", "Phase 53 HashDigestUpdater.h does not yet expose operation-descriptor planning.");
            AssertContains(hashDigestUpdaterHeader, "VisitDigestUpdateRequestOperations(const DigestUpdateRequest& digestUpdateRequest", "Phase 53 HashDigestUpdater.h does not yet expose operation-descriptor iteration.");

            AssertContains(hashDigestUpdater, "DigestUpdateRequest CreateDigestUpdateRequest(const HashRequest& request)", "Phase 53 HashDigestUpdater.cpp does not yet own digest update request creation.");
            AssertContains(hashDigestUpdater, "VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)", "Phase 53 HashDigestUpdater.cpp does not yet own registry-driven request-algorithm iteration.");
            AssertContains(hashDigestUpdater, "const std::vector<HashAlgorithmId> normalizedAlgorithmIds = GetHashRequestNormalizedAlgorithmIds(request);", "Phase 53 HashDigestUpdater.cpp does not yet precompute normalized descriptor/id request algorithms.");
            AssertContains(hashDigestUpdater, "if (!IsRequestedDigestAlgorithmId(normalizedAlgorithmIds, algorithmId))", "Phase 53 HashDigestUpdater.cpp does not yet filter update plans through descriptor/id request algorithms.");
            AssertContains(hashDigestUpdater, "digestUpdateRequest.operationDescriptors.push_back(operationDescriptor);", "Phase 53 HashDigestUpdater.cpp does not yet preserve operation descriptors in digest update requests.");
            AssertContains(hashDigestUpdater, "void UpdateDigestContextsParallel(const DigestUpdateRequest& digestUpdateRequest", "Phase 53 HashDigestUpdater.cpp does not yet own parallel digest fan-out.");
            AssertContains(hashDigestUpdater, "std::vector<std::future<void>> digestUpdateTasks;", "Phase 53 HashDigestUpdater.cpp does not yet own generic digest worker fan-out.");
            AssertContains(hashDigestUpdater, "digestUpdateTasks.push_back(threadPool->enqueue([&hashContexts, data, dataLen, operationDescriptor]()", "Phase 53 HashDigestUpdater.cpp does not yet dispatch digest workers through operation descriptors.");
            AssertContains(hashDigestUpdater, "void UpdateDigestContextsSequential(const DigestUpdateRequest& digestUpdateRequest", "Phase 53 HashDigestUpdater.cpp does not yet own sequential digest updates.");

            AssertContains(hashEngineInternal, "#include \"Common/HashDigestUpdater.h\"", "Phase 53 HashEngineInternal.h does not yet consume the digest updater seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestUpdater.cpp", "Phase 53 desktop native core project does not yet compile HashDigestUpdater.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestUpdater.h", "Phase 53 desktop native core project does not yet include HashDigestUpdater.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestUpdater.cpp", "Phase 53 desktop native core filters do not yet expose HashDigestUpdater.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestUpdater.h", "Phase 53 desktop native core filters do not yet expose HashDigestUpdater.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestUpdater.cpp", "Phase 53 UWP native project does not yet compile HashDigestUpdater.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestUpdater.h", "Phase 53 UWP native project does not yet include HashDigestUpdater.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestUpdater.cpp", "Phase 53 UWP native filters do not yet expose HashDigestUpdater.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestUpdater.h", "Phase 53 UWP native filters do not yet expose HashDigestUpdater.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestUpdater.cpp", "Phase 53 WinUI native project should keep consuming the shared native core instead of compiling HashDigestUpdater.cpp directly.");
        }, failures);

        Run("Phase 54 promotes progress computation and event publication into a dedicated progress tracker seam", () =>
        {
            string hashDigestQueue = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp");
            string hashDigestPipeline = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp");
            string hashDigestExecution = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp");
            string hashDigestSinglePass = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestSinglePass.cpp");
            string hashProgressTracker = ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressTracker.cpp");
            string hashProgressTrackerHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressTracker.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashProgressTrackerHeader, "void UpdateHashExecutionProgress(HashExecutionContext *executionContext, uint64_t fileSize, bool isSizeCaled, unsigned int dataLen,", "Phase 54 HashProgressTracker.h does not yet expose the progress tracker update seam.");
            AssertContains(hashProgressTracker, "void UpdateHashExecutionProgress(HashExecutionContext *executionContext, uint64_t fileSize, bool isSizeCaled, unsigned int dataLen,", "Phase 54 HashProgressTracker.cpp does not yet own progress update orchestration.");
            AssertContains(hashProgressTracker, "observer->onProgressEvent(CreateFileProgressEvent(positionNew));", "Phase 54 HashProgressTracker.cpp does not yet own per-file progress publication.");
            AssertContains(hashProgressTracker, "observer->onProgressEvent(CreateTotalProgressEvent(progressState->positionWhole));", "Phase 54 HashProgressTracker.cpp does not yet own whole-job progress publication.");

            AssertContains(hashDigestQueue, "UpdateHashExecutionProgress(executionContext, fileSize, isSizeCaled, ptrDataBufCalc->datalen, &executionState->progressState);", "Phase 54 HashDigestQueue.cpp does not yet delegate queued buffer progress updates to HashProgressTracker.");
            AssertContains(hashDigestPipeline, "ExecuteOpenedFileDigestUpdate(executionContext, digestRuntimePlan, fsize, isSizeCaled, executionState", "Phase 54 HashDigestPipeline.cpp does not yet delegate digest execution through HashDigestExecution.");
            AssertContains(hashDigestExecution, "ProcessOpenedFileHashingSinglePass(executionContext, digestUpdateRequest, fsize, isSizeCaled, preferredBufferLength, executionState);", "Phase 54 HashDigestExecution.cpp does not yet delegate single-thread digest processing through HashDigestSinglePass.");
            AssertContains(hashDigestSinglePass, "UpdateHashExecutionProgress(executionContext, fsize, isSizeCaled, databuf.datalen, &executionState->progressState);", "Phase 54 HashDigestSinglePass.cpp does not yet delegate single-thread progress updates to HashProgressTracker.");
            AssertDoesNotContain(hashDigestPipeline, "observer->onProgressEvent(CreateFileProgressEvent(positionNew));", "Phase 54 HashDigestPipeline.cpp still publishes per-file progress directly instead of routing through HashProgressTracker.");

            AssertContains(hashEngineInternal, "#include \"Common/HashProgressTracker.h\"", "Phase 54 HashEngineInternal.h does not yet consume the HashProgressTracker seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashProgressTracker.cpp", "Phase 54 desktop native core project does not yet compile HashProgressTracker.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashProgressTracker.h", "Phase 54 desktop native core project does not yet include HashProgressTracker.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashProgressTracker.cpp", "Phase 54 desktop native core filters do not yet expose HashProgressTracker.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashProgressTracker.h", "Phase 54 desktop native core filters do not yet expose HashProgressTracker.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashProgressTracker.cpp", "Phase 54 UWP native project does not yet compile HashProgressTracker.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashProgressTracker.h", "Phase 54 UWP native project does not yet include HashProgressTracker.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashProgressTracker.cpp", "Phase 54 UWP native filters do not yet expose HashProgressTracker.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashProgressTracker.h", "Phase 54 UWP native filters do not yet expose HashProgressTracker.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashProgressTracker.cpp", "Phase 54 WinUI native project should keep consuming the shared native core instead of compiling HashProgressTracker.cpp directly.");
        }, failures);

        Run("Phase 55 promotes digest queue buffering and producer-consumer coordination into a dedicated queue seam", () =>
        {
            string hashDigestPipeline = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp");
            string hashDigestExecution = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp");
            string hashDigestQueue = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp");
            string hashDigestQueueHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashDigestQueueHeader, "class DigestDataBuffer", "Phase 55 HashDigestQueue.h does not yet expose queue buffer state.");
            AssertContains(hashDigestQueueHeader, "bool ReadDigestDataBuffer(FileExecutionState *executionState, DigestDataBuffer& dataBuffer);", "Phase 55 HashDigestQueue.h does not yet expose digest chunk reads.");
            AssertContains(hashDigestQueueHeader, "bool ProcessOpenedFileHashingParallel(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fileSize, bool isSizeCaled,", "Phase 55 HashDigestQueue.h does not yet expose parallel producer-consumer processing.");
            AssertContains(hashDigestQueueHeader, "const HashDigestQueuePlan& digestQueuePlan", "Phase 55 HashDigestQueue.h does not yet expose queue planning controls for parallel producer-consumer processing.");
            AssertContains(hashDigestQueue, "bool ReadDigestDataBuffer(FileExecutionState *executionState, DigestDataBuffer& dataBuffer)", "Phase 55 HashDigestQueue.cpp does not yet own digest chunk reading.");
            AssertContains(hashDigestQueue, "queue<unique_ptr<DigestDataBuffer>> queueDataBuffer;", "Phase 55 HashDigestQueue.cpp does not yet own queued digest buffers.");
            AssertContains(hashDigestQueue, "condition_variable cvFile;", "Phase 55 HashDigestQueue.cpp does not yet own producer-consumer file queue signaling.");
            AssertContains(hashDigestQueue, "condition_variable cvCalc;", "Phase 55 HashDigestQueue.cpp does not yet own producer-consumer calculator queue signaling.");
            AssertContains(hashDigestQueue, "UpdateDigestContextsParallel(digestUpdateRequest", "Phase 55 HashDigestQueue.cpp does not yet own queued digest updates.");
            AssertContains(hashDigestQueue, "UpdateHashExecutionProgress(executionContext, fileSize, isSizeCaled, ptrDataBufCalc->datalen, &executionState->progressState);", "Phase 55 HashDigestQueue.cpp does not yet own queued progress publication.");
            AssertContains(hashDigestQueue, "queueDataBuffer.size() < GetHashDigestQueueMaxBufferedChunkCount(digestQueuePlan)", "Phase 55 HashDigestQueue.cpp does not yet route queue throttling through queue-plan controls.");

            AssertContains(hashDigestExecution, "ProcessOpenedFileHashingParallel(executionContext, digestUpdateRequest, fsize, isSizeCaled, preferredBufferLength, digestQueuePlan, executionState, threadPool);", "Phase 55 HashDigestExecution.cpp does not yet delegate producer-consumer queue orchestration to HashDigestQueue.");
            AssertDoesNotContain(hashDigestPipeline, "queue<unique_ptr<DigestDataBuffer>> queueDataBuffer;", "Phase 55 HashDigestPipeline.cpp still owns digest queue buffers instead of delegating them to HashDigestQueue.");

            AssertContains(hashEngineInternal, "#include \"Common/HashDigestQueue.h\"", "Phase 55 HashEngineInternal.h does not yet consume the digest queue seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestQueue.cpp", "Phase 55 desktop native core project does not yet compile HashDigestQueue.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestQueue.h", "Phase 55 desktop native core project does not yet include HashDigestQueue.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestQueue.cpp", "Phase 55 desktop native core filters do not yet expose HashDigestQueue.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestQueue.h", "Phase 55 desktop native core filters do not yet expose HashDigestQueue.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestQueue.cpp", "Phase 55 UWP native project does not yet compile HashDigestQueue.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestQueue.h", "Phase 55 UWP native project does not yet include HashDigestQueue.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestQueue.cpp", "Phase 55 UWP native filters do not yet expose HashDigestQueue.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestQueue.h", "Phase 55 UWP native filters do not yet expose HashDigestQueue.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestQueue.cpp", "Phase 55 WinUI native project should keep consuming the shared native core instead of compiling HashDigestQueue.cpp directly.");
        }, failures);

        Run("Phase 56 promotes single-thread digest loop orchestration into a dedicated single-pass seam", () =>
        {
            string hashDigestPipeline = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp");
            string hashDigestExecution = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp");
            string hashDigestSinglePass = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestSinglePass.cpp");
            string hashDigestSinglePassHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestSinglePass.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashDigestSinglePassHeader, "bool ProcessOpenedFileHashingSinglePass(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fsize, bool isSizeCaled,", "Phase 56 HashDigestSinglePass.h does not yet expose single-thread digest processing.");
            AssertContains(hashDigestSinglePass, "bool ProcessOpenedFileHashingSinglePass(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fsize, bool isSizeCaled,", "Phase 56 HashDigestSinglePass.cpp does not yet own single-thread digest processing.");
            AssertContains(hashDigestSinglePass, "DigestDataBuffer databuf(preferredBufferLength);", "Phase 56 HashDigestSinglePass.cpp does not yet own single-thread digest buffering.");
            AssertContains(hashDigestSinglePass, "if (ReadDigestDataBuffer(executionState, databuf))", "Phase 56 HashDigestSinglePass.cpp does not yet own single-thread digest reads.");
            AssertContains(hashDigestSinglePass, "UpdateDigestContextsSequential(digestUpdateRequest", "Phase 56 HashDigestSinglePass.cpp does not yet own single-thread digest updates.");
            AssertContains(hashDigestSinglePass, "UpdateHashExecutionProgress(executionContext, fsize, isSizeCaled, databuf.datalen, &executionState->progressState);", "Phase 56 HashDigestSinglePass.cpp does not yet own single-thread progress publication.");

            AssertContains(hashDigestExecution, "ProcessOpenedFileHashingSinglePass(executionContext, digestUpdateRequest, fsize, isSizeCaled, preferredBufferLength, executionState);", "Phase 56 HashDigestExecution.cpp does not yet delegate single-thread digest orchestration to HashDigestSinglePass.");
            AssertDoesNotContain(hashDigestPipeline, "DigestDataBuffer databuf;", "Phase 56 HashDigestPipeline.cpp still owns single-thread digest buffering instead of delegating it to HashDigestSinglePass.");

            AssertContains(hashEngineInternal, "#include \"Common/HashDigestSinglePass.h\"", "Phase 56 HashEngineInternal.h does not yet consume the HashDigestSinglePass seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestSinglePass.cpp", "Phase 56 desktop native core project does not yet compile HashDigestSinglePass.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestSinglePass.h", "Phase 56 desktop native core project does not yet include HashDigestSinglePass.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestSinglePass.cpp", "Phase 56 desktop native core filters do not yet expose HashDigestSinglePass.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestSinglePass.h", "Phase 56 desktop native core filters do not yet expose HashDigestSinglePass.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestSinglePass.cpp", "Phase 56 UWP native project does not yet compile HashDigestSinglePass.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestSinglePass.h", "Phase 56 UWP native project does not yet include HashDigestSinglePass.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestSinglePass.cpp", "Phase 56 UWP native filters do not yet expose HashDigestSinglePass.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestSinglePass.h", "Phase 56 UWP native filters do not yet expose HashDigestSinglePass.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestSinglePass.cpp", "Phase 56 WinUI native project should keep consuming the shared native core instead of compiling HashDigestSinglePass.cpp directly.");
        }, failures);

        Run("Phase 57 promotes digest execution strategy dispatch into a dedicated execution seam", () =>
        {
            string hashDigestPipeline = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp");
            string hashDigestExecution = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp");
            string hashDigestExecutionHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashDigestExecutionHeader, "bool ExecuteOpenedFileDigestUpdate(HashExecutionContext *executionContext, const HashDigestRuntimePlan& digestRuntimePlan, uint64_t fsize, bool isSizeCaled,", "Phase 57 HashDigestExecution.h does not yet expose digest execution strategy dispatch.");
            AssertContains(hashDigestExecution, "const HashDigestQueuePlan& digestQueuePlan = GetHashDigestRuntimeQueuePlan(digestRuntimePlan);", "Phase 57 HashDigestExecution.cpp does not yet expose planned queue controls in digest execution strategy dispatch.");
            AssertContains(hashDigestExecution, "bool ExecuteOpenedFileDigestUpdate(HashExecutionContext *executionContext, const HashDigestRuntimePlan& digestRuntimePlan, uint64_t fsize, bool isSizeCaled,", "Phase 57 HashDigestExecution.cpp does not yet own digest execution strategy dispatch.");
            AssertContains(hashDigestExecution, "if (IsParallelHashDigestExecutionMode(digestExecutionMode))", "Phase 57 HashDigestExecution.cpp does not yet route strategy selection through the execution-mode seam.");
            AssertContains(hashDigestExecution, "ProcessOpenedFileHashingParallel(executionContext, digestUpdateRequest, fsize, isSizeCaled, preferredBufferLength, digestQueuePlan, executionState, threadPool);", "Phase 57 HashDigestExecution.cpp does not yet dispatch to parallel digest execution.");
            AssertContains(hashDigestExecution, "ProcessOpenedFileHashingSinglePass(executionContext, digestUpdateRequest, fsize, isSizeCaled, preferredBufferLength, executionState);", "Phase 57 HashDigestExecution.cpp does not yet dispatch to single-pass digest execution.");
            AssertContains(hashDigestPipeline, "ExecuteOpenedFileDigestUpdate(executionContext, digestRuntimePlan, fsize, isSizeCaled, executionState", "Phase 57 HashDigestPipeline.cpp does not yet delegate digest execution strategy dispatch.");

            AssertContains(hashEngineInternal, "#include \"Common/HashDigestExecution.h\"", "Phase 57 HashEngineInternal.h does not yet consume the HashDigestExecution seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestExecution.cpp", "Phase 57 desktop native core project does not yet compile HashDigestExecution.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestExecution.h", "Phase 57 desktop native core project does not yet include HashDigestExecution.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestExecution.cpp", "Phase 57 desktop native core filters do not yet expose HashDigestExecution.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestExecution.h", "Phase 57 desktop native core filters do not yet expose HashDigestExecution.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestExecution.cpp", "Phase 57 UWP native project does not yet compile HashDigestExecution.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestExecution.h", "Phase 57 UWP native project does not yet include HashDigestExecution.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestExecution.cpp", "Phase 57 UWP native filters do not yet expose HashDigestExecution.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestExecution.h", "Phase 57 UWP native filters do not yet expose HashDigestExecution.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestExecution.cpp", "Phase 57 WinUI native project should keep consuming the shared native core instead of compiling HashDigestExecution.cpp directly.");
        }, failures);

        Run("Phase 58 promotes per-file attempt lifecycle into a dedicated workflow seam", () =>
        {
            string hashFileRunner = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileRunner.cpp");
            string hashFileAttemptWorkflow = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptWorkflow.cpp");
            string hashFileAttemptWorkflowHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptWorkflow.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashFileAttemptWorkflowHeader, "bool ExecuteFileHashAttemptWorkflow(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const sunjwbase::tstring& fullPath,", "Phase 58 HashFileAttemptWorkflow.h does not yet expose per-file attempt workflow dispatch.");
            AssertContains(hashFileAttemptWorkflow, "bool ExecuteFileHashAttemptWorkflow(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const sunjwbase::tstring& fullPath,", "Phase 58 HashFileAttemptWorkflow.cpp does not yet own per-file attempt workflow dispatch.");
            AssertContains(hashFileAttemptWorkflow, "YieldHashThread();", "Phase 58 HashFileAttemptWorkflow.cpp does not yet own per-file thread yielding.");
            AssertContains(hashFileAttemptWorkflow, "InitializeFileAttemptState(path, &osFile, &executionState->fileAttemptState);", "Phase 58 HashFileAttemptWorkflow.cpp does not yet own file-attempt initialization.");
            AssertContains(hashFileAttemptWorkflow, "bool wasStopped = ProcessOpenedFileHashing(executionContext, request, result, fileIndex, isSizeCaled, fSizes, executionState", "Phase 58 HashFileAttemptWorkflow.cpp does not yet own opened-file digest execution.");
            AssertContains(hashFileAttemptWorkflow, "CompleteFileAttempt(executionContext, request, result, fileIndex, isSizeCaled, *executionState);", "Phase 58 HashFileAttemptWorkflow.cpp does not yet own file-attempt completion.");
            AssertContains(hashFileRunner, "ExecuteFileHashAttemptWorkflow(executionContext, request, fileIndex, fullPath, isSizeCaled, fSizes, executionState", "Phase 58 HashFileRunner.cpp does not yet delegate per-file attempt workflow execution.");

            AssertContains(hashEngineInternal, "#include \"Common/HashFileAttemptWorkflow.h\"", "Phase 58 HashEngineInternal.h does not yet consume the HashFileAttemptWorkflow seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileAttemptWorkflow.cpp", "Phase 58 desktop native core project does not yet compile HashFileAttemptWorkflow.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileAttemptWorkflow.h", "Phase 58 desktop native core project does not yet include HashFileAttemptWorkflow.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileAttemptWorkflow.cpp", "Phase 58 desktop native core filters do not yet expose HashFileAttemptWorkflow.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileAttemptWorkflow.h", "Phase 58 desktop native core filters do not yet expose HashFileAttemptWorkflow.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileAttemptWorkflow.cpp", "Phase 58 UWP native project does not yet compile HashFileAttemptWorkflow.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileAttemptWorkflow.h", "Phase 58 UWP native project does not yet include HashFileAttemptWorkflow.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashFileAttemptWorkflow.cpp", "Phase 58 UWP native filters do not yet expose HashFileAttemptWorkflow.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashFileAttemptWorkflow.h", "Phase 58 UWP native filters do not yet expose HashFileAttemptWorkflow.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashFileAttemptWorkflow.cpp", "Phase 58 WinUI native project should keep consuming the shared native core instead of compiling HashFileAttemptWorkflow.cpp directly.");
        }, failures);

        Run("Phase 59 promotes scheduler file-dispatch traversal into a dedicated scheduler-dispatch seam", () =>
        {
            string hashScheduler = ReadRepoFile(repoRoot, @"trunk\source\Common\HashScheduler.cpp");
            string hashSchedulerDispatch = ReadRepoFile(repoRoot, @"trunk\source\Common\HashSchedulerDispatch.cpp");
            string hashSchedulerDispatchHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashSchedulerDispatch.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashSchedulerDispatchHeader, "bool ExecuteScheduledHashRequestFiles(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState", "Phase 59 HashSchedulerDispatch.h does not yet expose scheduler file-dispatch traversal.");
            AssertContains(hashSchedulerDispatch, "bool ExecuteScheduledHashRequestFiles(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState", "Phase 59 HashSchedulerDispatch.cpp does not yet own scheduler file-dispatch traversal.");
            AssertContains(hashSchedulerDispatch, "VisitHashRequestFiles(request, [&](uint32_t fileIndex, const sunjwbase::tstring& fullPath)", "Phase 59 HashSchedulerDispatch.cpp does not yet own file traversal.");
            AssertContains(hashSchedulerDispatch, "RunFileHashAttempt(executionContext, request, fileIndex, fullPath, isSizeCaled, fSizes,", "Phase 59 HashSchedulerDispatch.cpp does not yet dispatch per-file attempts through the runner seam.");
            AssertContains(hashScheduler, "ExecuteScheduledHashRequestFiles(executionContext, request, isSizeCaled, fSizes, &executionState", "Phase 59 HashScheduler.cpp does not yet delegate file-dispatch traversal through HashSchedulerDispatch.");

            AssertContains(hashEngineInternal, "#include \"Common/HashSchedulerDispatch.h\"", "Phase 59 HashEngineInternal.h does not yet consume the HashSchedulerDispatch seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashSchedulerDispatch.cpp", "Phase 59 desktop native core project does not yet compile HashSchedulerDispatch.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashSchedulerDispatch.h", "Phase 59 desktop native core project does not yet include HashSchedulerDispatch.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashSchedulerDispatch.cpp", "Phase 59 desktop native core filters do not yet expose HashSchedulerDispatch.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashSchedulerDispatch.h", "Phase 59 desktop native core filters do not yet expose HashSchedulerDispatch.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashSchedulerDispatch.cpp", "Phase 59 UWP native project does not yet compile HashSchedulerDispatch.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashSchedulerDispatch.h", "Phase 59 UWP native project does not yet include HashSchedulerDispatch.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashSchedulerDispatch.cpp", "Phase 59 UWP native filters do not yet expose HashSchedulerDispatch.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashSchedulerDispatch.h", "Phase 59 UWP native filters do not yet expose HashSchedulerDispatch.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashSchedulerDispatch.cpp", "Phase 59 WinUI native project should keep consuming the shared native core instead of compiling HashSchedulerDispatch.cpp directly.");
        }, failures);

        Run("Phase 60 promotes algorithm-selection planning into a dedicated job execution-plan seam", () =>
        {
            string hashEngine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string hashScheduler = ReadRepoFile(repoRoot, @"trunk\source\Common\HashScheduler.cpp");
            string hashDigestPipeline = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp");
            string hashDigestExecution = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp");
            string hashJobExecutionPlan = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.cpp");
            string hashJobExecutionPlanHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashJobExecutionPlanHeader, "struct HashJobExecutionPlan", "Phase 60 HashJobExecutionPlan.h does not yet define the job execution plan contract.");
            AssertContains(hashJobExecutionPlanHeader, "DigestUpdateRequest digestUpdateRequest;", "Phase 60 HashJobExecutionPlan.h does not yet expose digest update selection in the job execution plan.");
            AssertContains(hashJobExecutionPlanHeader, "HashDigestExecutionMode digestExecutionMode;", "Phase 60 HashJobExecutionPlan.h does not yet expose digest execution-mode selection in the job execution plan.");
            AssertContains(hashJobExecutionPlanHeader, "HashDigestQueuePlan digestQueuePlan;", "Phase 60 HashJobExecutionPlan.h does not yet expose digest queue planning in the job execution plan.");
            AssertContains(hashJobExecutionPlanHeader, "void InitializeHashJobExecutionPlan(const HashRequest& request, HashJobExecutionPlan *executionPlan);", "Phase 60 HashJobExecutionPlan.h does not yet expose job execution-plan initialization.");
            AssertContains(hashJobExecutionPlanHeader, "const DigestUpdateRequest& GetHashJobDigestUpdateRequest(const HashJobExecutionPlan& executionPlan);", "Phase 60 HashJobExecutionPlan.h does not yet expose digest request access.");
            AssertContains(hashJobExecutionPlanHeader, "HashDigestExecutionMode GetHashJobDigestExecutionMode(const HashJobExecutionPlan& executionPlan);", "Phase 60 HashJobExecutionPlan.h does not yet expose digest execution-mode access.");
            AssertContains(hashJobExecutionPlanHeader, "const HashDigestQueuePlan& GetHashJobDigestQueuePlan(const HashJobExecutionPlan& executionPlan);", "Phase 60 HashJobExecutionPlan.h does not yet expose digest queue planning access.");
            AssertContains(hashJobExecutionPlan, "void InitializeHashJobExecutionPlan(const HashRequest& request, HashJobExecutionPlan *executionPlan)", "Phase 60 HashJobExecutionPlan.cpp does not yet own job execution-plan initialization.");
            AssertContains(hashJobExecutionPlan, "executionPlan->digestUpdateRequest = CreateDigestUpdateRequest(request);", "Phase 60 HashJobExecutionPlan.cpp does not yet own digest request initialization.");
            AssertContains(hashJobExecutionPlan, "executionPlan->digestExecutionMode = ResolveHashDigestExecutionMode(request);", "Phase 60 HashJobExecutionPlan.cpp does not yet own digest execution-mode initialization.");
            AssertContains(hashJobExecutionPlan, "executionPlan->digestQueuePlan = CreateHashDigestQueuePlan(request, executionPlan->digestExecutionMode);", "Phase 60 HashJobExecutionPlan.cpp does not yet own digest queue-plan initialization.");
            AssertContains(hashJobExecutionPlan, "const DigestUpdateRequest& GetHashJobDigestUpdateRequest(const HashJobExecutionPlan& executionPlan)", "Phase 60 HashJobExecutionPlan.cpp does not yet own digest request retrieval.");
            AssertContains(hashJobExecutionPlan, "return executionPlan.digestUpdateRequest;", "Phase 60 HashJobExecutionPlan.cpp does not yet return the planned digest request.");
            AssertContains(hashJobExecutionPlan, "HashDigestExecutionMode GetHashJobDigestExecutionMode(const HashJobExecutionPlan& executionPlan)", "Phase 60 HashJobExecutionPlan.cpp does not yet own digest execution-mode retrieval.");
            AssertContains(hashJobExecutionPlan, "return executionPlan.digestExecutionMode;", "Phase 60 HashJobExecutionPlan.cpp does not yet return the planned digest execution mode.");
            AssertContains(hashJobExecutionPlan, "const HashDigestQueuePlan& GetHashJobDigestQueuePlan(const HashJobExecutionPlan& executionPlan)", "Phase 60 HashJobExecutionPlan.cpp does not yet own digest queue-plan retrieval.");
            AssertContains(hashJobExecutionPlan, "return executionPlan.digestQueuePlan;", "Phase 60 HashJobExecutionPlan.cpp does not yet return the planned digest queue-plan.");

            AssertContains(hashEngine, "InitializeHashJobExecutionPlan(request, &executionPlan);", "Phase 60 HashEngine.cpp does not yet initialize the shared job execution plan once per request.");
            AssertContains(hashScheduler, "executionState.executionPlan = executionPlan;", "Phase 60 HashScheduler.cpp does not yet consume the shared job execution plan from HashEngine.");
            AssertContains(hashDigestPipeline, "CreateHashDigestRuntimePlan(executionState->executionPlan)", "Phase 60 HashDigestPipeline.cpp does not yet consume the planned digest controls through the runtime-plan seam.");
            AssertContains(hashDigestExecution, "GetHashDigestRuntimeUpdateRequest(digestRuntimePlan)", "Phase 60 HashDigestExecution.cpp does not yet consume the planned digest request from file execution state through runtime planning.");
            AssertContains(hashDigestExecution, "GetHashDigestRuntimeExecutionMode(digestRuntimePlan)", "Phase 60 HashDigestExecution.cpp does not yet consume the planned digest execution mode from file execution state through runtime planning.");
            AssertContains(hashDigestExecution, "GetHashDigestRuntimeQueuePlan(digestRuntimePlan)", "Phase 60 HashDigestExecution.cpp does not yet consume the planned digest queue controls from file execution state through runtime planning.");

            AssertContains(hashEngineInternal, "#include \"Common/HashJobExecutionPlan.h\"", "Phase 60 HashEngineInternal.h does not yet consume the HashJobExecutionPlan seam.");
            AssertContains(hashEngineInternal, "HashJobExecutionPlan executionPlan;", "Phase 60 HashEngineInternal.h does not yet keep job execution-plan state in grouped file execution state.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashJobExecutionPlan.cpp", "Phase 60 desktop native core project does not yet compile HashJobExecutionPlan.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashJobExecutionPlan.h", "Phase 60 desktop native core project does not yet include HashJobExecutionPlan.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashJobExecutionPlan.cpp", "Phase 60 desktop native core filters do not yet expose HashJobExecutionPlan.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashJobExecutionPlan.h", "Phase 60 desktop native core filters do not yet expose HashJobExecutionPlan.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashJobExecutionPlan.cpp", "Phase 60 UWP native project does not yet compile HashJobExecutionPlan.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashJobExecutionPlan.h", "Phase 60 UWP native project does not yet include HashJobExecutionPlan.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashJobExecutionPlan.cpp", "Phase 60 UWP native filters do not yet expose HashJobExecutionPlan.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashJobExecutionPlan.h", "Phase 60 UWP native filters do not yet expose HashJobExecutionPlan.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashJobExecutionPlan.cpp", "Phase 60 WinUI native project should keep consuming the shared native core instead of compiling HashJobExecutionPlan.cpp directly.");
        }, failures);

        Run("Phase 61 promotes file-version resolution into a dedicated resolver seam", () =>
        {
            string hashEngineResult = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp");
            string hashFileVersionResolver = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileVersionResolver.cpp");
            string hashFileVersionResolverHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileVersionResolver.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashFileVersionResolverHeader, "sunjwbase::tstring ResolveHashFileVersion(sunjwbase::OsFile& osFile, const TCHAR *path);", "Phase 61 HashFileVersionResolver.h does not yet expose file-version resolution.");
            AssertContains(hashFileVersionResolver, "sunjwbase::tstring ResolveHashFileVersion(sunjwbase::OsFile& osFile, const TCHAR *path)", "Phase 61 HashFileVersionResolver.cpp does not yet own file-version resolution.");
            AssertContains(hashFileVersionResolver, "WindowsComm::FileVersionHelper fvHelper(osFile);", "Phase 61 HashFileVersionResolver.cpp does not yet preserve WinUI/UWP file-version probing.");
            AssertContains(hashFileVersionResolver, "return WindowsComm::GetExeFileVersion((TCHAR *)path);", "Phase 61 HashFileVersionResolver.cpp does not yet preserve desktop file-version probing.");
            AssertContains(hashEngineResult, "result.meta.version.clear();", "Phase 61 HashEngineResult.cpp does not yet clear eager version metadata from the hot hashing path.");
            AssertDoesNotContain(hashEngineResult, "ResolveHashFileVersion(osFile, path);", "Phase 61 HashEngineResult.cpp still performs synchronous file-version probing inside the hot metadata path.");
            AssertDoesNotContain(hashEngineResult, "WindowsComm::FileVersionHelper fvHelper(osFile);", "Phase 61 HashEngineResult.cpp should stop carrying WinUI/UWP file-version probing details.");
            AssertDoesNotContain(hashEngineResult, "WindowsComm::GetExeFileVersion((TCHAR *)path);", "Phase 61 HashEngineResult.cpp should stop carrying desktop file-version probing details.");

            AssertContains(hashEngineInternal, "#include \"Common/HashFileVersionResolver.h\"", "Phase 61 HashEngineInternal.h does not yet consume the HashFileVersionResolver seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileVersionResolver.cpp", "Phase 61 desktop native core project does not yet compile HashFileVersionResolver.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileVersionResolver.h", "Phase 61 desktop native core project does not yet include HashFileVersionResolver.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileVersionResolver.cpp", "Phase 61 desktop native core filters do not yet expose HashFileVersionResolver.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileVersionResolver.h", "Phase 61 desktop native core filters do not yet expose HashFileVersionResolver.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileVersionResolver.cpp", "Phase 61 UWP native project does not yet compile HashFileVersionResolver.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileVersionResolver.h", "Phase 61 UWP native project does not yet include HashFileVersionResolver.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashFileVersionResolver.cpp", "Phase 61 UWP native filters do not yet expose HashFileVersionResolver.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashFileVersionResolver.h", "Phase 61 UWP native filters do not yet expose HashFileVersionResolver.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashFileVersionResolver.cpp", "Phase 61 WinUI native project should keep consuming the shared native core instead of compiling HashFileVersionResolver.cpp directly.");
        }, failures);

        Run("Phase 62 promotes digest execution-mode resolution into a dedicated mode seam", () =>
        {
            string hashDigestExecutionMode = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecutionMode.cpp");
            string hashDigestExecutionModeHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecutionMode.h");
            string hashDigestExecution = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp");
            string hashDigestExecutionHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.h");
            string hashDigestPipeline = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp");
            string hashJobExecutionPlan = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.cpp");
            string hashJobExecutionPlanHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashDigestExecutionModeHeader, "enum HashDigestExecutionMode", "Phase 62 HashDigestExecutionMode.h does not yet expose digest execution modes.");
            AssertContains(hashDigestExecutionModeHeader, "HASH_DIGEST_EXECUTION_MODE_SINGLE_PASS", "Phase 62 HashDigestExecutionMode.h does not yet expose single-pass mode.");
            AssertContains(hashDigestExecutionModeHeader, "HASH_DIGEST_EXECUTION_MODE_PARALLEL", "Phase 62 HashDigestExecutionMode.h does not yet expose parallel mode.");
            AssertContains(hashDigestExecutionModeHeader, "HashDigestExecutionMode ResolveHashDigestExecutionMode(const HashRequest& request);", "Phase 62 HashDigestExecutionMode.h does not yet expose execution-mode resolution.");
            AssertContains(hashDigestExecutionModeHeader, "bool IsParallelHashDigestExecutionMode(HashDigestExecutionMode executionMode);", "Phase 62 HashDigestExecutionMode.h does not yet expose execution-mode querying.");
            AssertContains(hashDigestExecutionMode, "HashDigestExecutionMode ResolveHashDigestExecutionMode(const HashRequest& request)", "Phase 62 HashDigestExecutionMode.cpp does not yet own execution-mode resolution.");
            AssertContains(hashDigestExecutionMode, "return HASH_DIGEST_EXECUTION_MODE_PARALLEL;", "Phase 62 HashDigestExecutionMode.cpp does not yet preserve the parallel baseline mode.");
            AssertContains(hashDigestExecutionMode, "return HASH_DIGEST_EXECUTION_MODE_SINGLE_PASS;", "Phase 62 HashDigestExecutionMode.cpp does not yet preserve the single-pass fallback mode.");
            AssertContains(hashDigestExecutionMode, "bool IsParallelHashDigestExecutionMode(HashDigestExecutionMode executionMode)", "Phase 62 HashDigestExecutionMode.cpp does not yet own execution-mode querying.");

            AssertContains(hashJobExecutionPlanHeader, "HashDigestExecutionMode digestExecutionMode;", "Phase 62 HashJobExecutionPlan.h does not yet store the planned digest execution mode.");
            AssertContains(hashJobExecutionPlanHeader, "HashDigestExecutionMode GetHashJobDigestExecutionMode(const HashJobExecutionPlan& executionPlan);", "Phase 62 HashJobExecutionPlan.h does not yet expose execution-mode access.");
            AssertContains(hashJobExecutionPlan, "executionPlan->digestExecutionMode = ResolveHashDigestExecutionMode(request);", "Phase 62 HashJobExecutionPlan.cpp does not yet initialize execution mode.");
            AssertContains(hashJobExecutionPlan, "HashDigestExecutionMode GetHashJobDigestExecutionMode(const HashJobExecutionPlan& executionPlan)", "Phase 62 HashJobExecutionPlan.cpp does not yet own execution-mode retrieval.");
            AssertContains(hashJobExecutionPlan, "return executionPlan.digestExecutionMode;", "Phase 62 HashJobExecutionPlan.cpp does not yet return the planned execution mode.");

            AssertContains(hashDigestPipeline, "HashDigestRuntimePlan digestRuntimePlan = CreateHashDigestRuntimePlan(executionState->executionPlan);", "Phase 62 HashDigestPipeline.cpp does not yet consume planned execution mode through runtime planning.");
            AssertContains(hashDigestExecutionHeader, "const HashDigestRuntimePlan& digestRuntimePlan", "Phase 62 HashDigestExecution.h does not yet expose execution mode in digest dispatch.");
            AssertContains(hashDigestExecution, "HashDigestExecutionMode digestExecutionMode = GetHashDigestRuntimeExecutionMode(digestRuntimePlan);", "Phase 62 HashDigestExecution.cpp does not yet route execution through the execution-mode seam.");
            AssertContains(hashDigestExecution, "if (IsParallelHashDigestExecutionMode(digestExecutionMode))", "Phase 62 HashDigestExecution.cpp does not yet route execution through the execution-mode seam.");
            AssertContains(hashDigestExecution, "return ProcessOpenedFileHashingSinglePass(executionContext, digestUpdateRequest, fsize, isSizeCaled, preferredBufferLength, executionState);", "Phase 62 HashDigestExecution.cpp does not yet preserve single-pass fallback dispatch.");

            AssertContains(hashEngineInternal, "#include \"Common/HashDigestExecutionMode.h\"", "Phase 62 HashEngineInternal.h does not yet consume the HashDigestExecutionMode seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestExecutionMode.cpp", "Phase 62 desktop native core project does not yet compile HashDigestExecutionMode.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestExecutionMode.h", "Phase 62 desktop native core project does not yet include HashDigestExecutionMode.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestExecutionMode.cpp", "Phase 62 desktop native core filters do not yet expose HashDigestExecutionMode.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestExecutionMode.h", "Phase 62 desktop native core filters do not yet expose HashDigestExecutionMode.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestExecutionMode.cpp", "Phase 62 UWP native project does not yet compile HashDigestExecutionMode.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestExecutionMode.h", "Phase 62 UWP native project does not yet include HashDigestExecutionMode.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestExecutionMode.cpp", "Phase 62 UWP native filters do not yet expose HashDigestExecutionMode.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestExecutionMode.h", "Phase 62 UWP native filters do not yet expose HashDigestExecutionMode.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestExecutionMode.cpp", "Phase 62 WinUI native project should keep consuming the shared native core instead of compiling HashDigestExecutionMode.cpp directly.");
        }, failures);

        Run("Phase 63 promotes digest queue planning into a dedicated queue-plan seam", () =>
        {
            string hashDigestQueuePlan = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueuePlan.cpp");
            string hashDigestQueuePlanHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueuePlan.h");
            string hashDigestQueue = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp");
            string hashDigestQueueHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.h");
            string hashDigestExecution = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp");
            string hashDigestExecutionHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.h");
            string hashDigestPipeline = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp");
            string hashJobExecutionPlan = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.cpp");
            string hashJobExecutionPlanHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashDigestQueuePlanHeader, "struct HashDigestQueuePlan", "Phase 63 HashDigestQueuePlan.h does not yet expose the queue-plan contract.");
            AssertContains(hashDigestQueuePlanHeader, "size_t maxBufferedChunkCount;", "Phase 63 HashDigestQueuePlan.h does not yet expose queue buffering controls.");
            AssertContains(hashDigestQueuePlanHeader, "HashDigestQueuePlan CreateHashDigestQueuePlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode);", "Phase 63 HashDigestQueuePlan.h does not yet expose queue-plan initialization.");
            AssertContains(hashDigestQueuePlanHeader, "size_t GetHashDigestQueueMaxBufferedChunkCount(const HashDigestQueuePlan& digestQueuePlan);", "Phase 63 HashDigestQueuePlan.h does not yet expose queue-plan querying.");
            AssertContains(hashDigestQueuePlan, "HashDigestQueuePlan CreateHashDigestQueuePlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode)", "Phase 63 HashDigestQueuePlan.cpp does not yet own queue-plan initialization.");
            AssertContains(hashDigestQueuePlan, "digestQueuePlan.maxBufferedChunkCount = 4;", "Phase 63 HashDigestQueuePlan.cpp does not yet preserve the baseline parallel queue depth.");
            AssertContains(hashDigestQueuePlan, "size_t GetHashDigestQueueMaxBufferedChunkCount(const HashDigestQueuePlan& digestQueuePlan)", "Phase 63 HashDigestQueuePlan.cpp does not yet own queue-plan querying.");

            AssertContains(hashJobExecutionPlanHeader, "HashDigestQueuePlan digestQueuePlan;", "Phase 63 HashJobExecutionPlan.h does not yet carry queue planning state.");
            AssertContains(hashJobExecutionPlanHeader, "const HashDigestQueuePlan& GetHashJobDigestQueuePlan(const HashJobExecutionPlan& executionPlan);", "Phase 63 HashJobExecutionPlan.h does not yet expose queue-plan access.");
            AssertContains(hashJobExecutionPlan, "executionPlan->digestQueuePlan = CreateHashDigestQueuePlan(request, executionPlan->digestExecutionMode);", "Phase 63 HashJobExecutionPlan.cpp does not yet initialize queue planning.");
            AssertContains(hashJobExecutionPlan, "const HashDigestQueuePlan& GetHashJobDigestQueuePlan(const HashJobExecutionPlan& executionPlan)", "Phase 63 HashJobExecutionPlan.cpp does not yet own queue-plan retrieval.");
            AssertContains(hashJobExecutionPlan, "return executionPlan.digestQueuePlan;", "Phase 63 HashJobExecutionPlan.cpp does not yet return the planned queue controls.");

            AssertContains(hashDigestPipeline, "CreateHashDigestRuntimePlan(executionState->executionPlan);", "Phase 63 HashDigestPipeline.cpp does not yet consume queue planning through runtime planning.");
            AssertContains(hashDigestExecutionHeader, "const HashDigestRuntimePlan& digestRuntimePlan", "Phase 63 HashDigestExecution.h does not yet route queue planning into digest execution dispatch.");
            AssertContains(hashDigestExecution, "const HashDigestQueuePlan& digestQueuePlan = GetHashDigestRuntimeQueuePlan(digestRuntimePlan);", "Phase 63 HashDigestExecution.cpp does not yet route queue planning into parallel digest execution.");
            AssertContains(hashDigestQueueHeader, "const HashDigestQueuePlan& digestQueuePlan", "Phase 63 HashDigestQueue.h does not yet expose queue-plan controls for parallel queue processing.");
            AssertContains(hashDigestQueue, "queueDataBuffer.size() < GetHashDigestQueueMaxBufferedChunkCount(digestQueuePlan)", "Phase 63 HashDigestQueue.cpp does not yet use queue-plan buffering controls.");

            AssertContains(hashEngineInternal, "#include \"Common/HashDigestQueuePlan.h\"", "Phase 63 HashEngineInternal.h does not yet consume the HashDigestQueuePlan seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestQueuePlan.cpp", "Phase 63 desktop native core project does not yet compile HashDigestQueuePlan.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestQueuePlan.h", "Phase 63 desktop native core project does not yet include HashDigestQueuePlan.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestQueuePlan.cpp", "Phase 63 desktop native core filters do not yet expose HashDigestQueuePlan.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestQueuePlan.h", "Phase 63 desktop native core filters do not yet expose HashDigestQueuePlan.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestQueuePlan.cpp", "Phase 63 UWP native project does not yet compile HashDigestQueuePlan.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestQueuePlan.h", "Phase 63 UWP native project does not yet include HashDigestQueuePlan.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestQueuePlan.cpp", "Phase 63 UWP native filters do not yet expose HashDigestQueuePlan.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestQueuePlan.h", "Phase 63 UWP native filters do not yet expose HashDigestQueuePlan.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestQueuePlan.cpp", "Phase 63 WinUI native project should keep consuming the shared native core instead of compiling HashDigestQueuePlan.cpp directly.");
        }, failures);

        Run("Phase 64 promotes scheduler worker planning into a dedicated scheduler-plan seam", () =>
        {
            string hashSchedulerPlan = ReadRepoFile(repoRoot, @"trunk\source\Common\HashSchedulerPlan.cpp");
            string hashSchedulerPlanHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashSchedulerPlan.h");
            string hashScheduler = ReadRepoFile(repoRoot, @"trunk\source\Common\HashScheduler.cpp");
            string hashJobExecutionPlan = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.cpp");
            string hashJobExecutionPlanHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashSchedulerPlanHeader, "struct HashSchedulerPlan", "Phase 64 HashSchedulerPlan.h does not yet expose scheduler plan state.");
            AssertContains(hashSchedulerPlanHeader, "size_t workerThreadCount;", "Phase 64 HashSchedulerPlan.h does not yet expose scheduler worker-thread controls.");
            AssertContains(hashSchedulerPlanHeader, "HashSchedulerPlan CreateHashSchedulerPlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode);", "Phase 64 HashSchedulerPlan.h does not yet expose scheduler-plan initialization.");
            AssertContains(hashSchedulerPlanHeader, "size_t GetHashSchedulerWorkerThreadCount(const HashSchedulerPlan& schedulerPlan);", "Phase 64 HashSchedulerPlan.h does not yet expose scheduler worker-count querying.");
            AssertContains(hashSchedulerPlan, "HashSchedulerPlan CreateHashSchedulerPlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode)", "Phase 64 HashSchedulerPlan.cpp does not yet own scheduler-plan initialization.");
            AssertContains(hashSchedulerPlan, "schedulerPlan.workerThreadCount = 5;", "Phase 64 HashSchedulerPlan.cpp does not yet preserve the baseline parallel worker count.");
            AssertContains(hashSchedulerPlan, "size_t GetHashSchedulerWorkerThreadCount(const HashSchedulerPlan& schedulerPlan)", "Phase 64 HashSchedulerPlan.cpp does not yet own scheduler worker-count querying.");

            AssertContains(hashJobExecutionPlanHeader, "HashSchedulerPlan schedulerPlan;", "Phase 64 HashJobExecutionPlan.h does not yet carry scheduler planning state.");
            AssertContains(hashJobExecutionPlanHeader, "const HashSchedulerPlan& GetHashJobSchedulerPlan(const HashJobExecutionPlan& executionPlan);", "Phase 64 HashJobExecutionPlan.h does not yet expose scheduler-plan access.");
            AssertContains(hashJobExecutionPlan, "executionPlan->schedulerPlan = CreateHashSchedulerPlan(request, executionPlan->digestExecutionMode);", "Phase 64 HashJobExecutionPlan.cpp does not yet initialize scheduler planning.");
            AssertContains(hashJobExecutionPlan, "const HashSchedulerPlan& GetHashJobSchedulerPlan(const HashJobExecutionPlan& executionPlan)", "Phase 64 HashJobExecutionPlan.cpp does not yet own scheduler-plan retrieval.");
            AssertContains(hashJobExecutionPlan, "return executionPlan.schedulerPlan;", "Phase 64 HashJobExecutionPlan.cpp does not yet return the planned scheduler controls.");

            AssertContains(hashScheduler, "const HashSchedulerPlan& schedulerPlan = GetHashJobSchedulerPlan(executionState.executionPlan);", "Phase 64 HashScheduler.cpp does not yet consume scheduler planning from the job execution plan.");
            AssertContains(hashScheduler, "ThreadPool threadPool(GetHashSchedulerWorkerThreadCount(schedulerPlan));", "Phase 64 HashScheduler.cpp does not yet route thread-pool sizing through scheduler planning.");
            AssertDoesNotContain(hashScheduler, "ThreadPool threadPool(5);", "Phase 64 HashScheduler.cpp should no longer hard-code thread-pool size.");

            AssertContains(hashEngineInternal, "#include \"Common/HashSchedulerPlan.h\"", "Phase 64 HashEngineInternal.h does not yet consume the HashSchedulerPlan seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashSchedulerPlan.cpp", "Phase 64 desktop native core project does not yet compile HashSchedulerPlan.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashSchedulerPlan.h", "Phase 64 desktop native core project does not yet include HashSchedulerPlan.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashSchedulerPlan.cpp", "Phase 64 desktop native core filters do not yet expose HashSchedulerPlan.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashSchedulerPlan.h", "Phase 64 desktop native core filters do not yet expose HashSchedulerPlan.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashSchedulerPlan.cpp", "Phase 64 UWP native project does not yet compile HashSchedulerPlan.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashSchedulerPlan.h", "Phase 64 UWP native project does not yet include HashSchedulerPlan.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashSchedulerPlan.cpp", "Phase 64 UWP native filters do not yet expose HashSchedulerPlan.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashSchedulerPlan.h", "Phase 64 UWP native filters do not yet expose HashSchedulerPlan.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashSchedulerPlan.cpp", "Phase 64 WinUI native project should keep consuming the shared native core instead of compiling HashSchedulerPlan.cpp directly.");
        }, failures);

        Run("Phase 65 promotes preparation planning into a dedicated preparation-plan seam and reuses one shared execution plan across preparation and scheduling", () =>
        {
            string hashPreparationPlan = ReadRepoFile(repoRoot, @"trunk\source\Common\HashPreparationPlan.cpp");
            string hashPreparationPlanHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashPreparationPlan.h");
            string hashEngine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string hashEnginePreparation = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp");
            string hashScheduler = ReadRepoFile(repoRoot, @"trunk\source\Common\HashScheduler.cpp");
            string hashJobExecutionPlan = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.cpp");
            string hashJobExecutionPlanHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashPreparationPlanHeader, "struct HashPreparationPlan", "Phase 65 HashPreparationPlan.h does not yet expose preparation planning state.");
            AssertContains(hashPreparationPlanHeader, "size_t preScanFileCountThreshold;", "Phase 65 HashPreparationPlan.h does not yet expose pre-scan threshold controls.");
            AssertContains(hashPreparationPlanHeader, "HashPreparationPlan CreateHashPreparationPlan(const HashRequest& request);", "Phase 65 HashPreparationPlan.h does not yet expose preparation-plan initialization.");
            AssertContains(hashPreparationPlanHeader, "bool ShouldPreScanHashRequestFileSizes(const HashPreparationPlan& preparationPlan, const HashRequest& request);", "Phase 65 HashPreparationPlan.h does not yet expose pre-scan gating queries.");
            AssertContains(hashPreparationPlan, "HashPreparationPlan CreateHashPreparationPlan(const HashRequest& request)", "Phase 65 HashPreparationPlan.cpp does not yet own preparation-plan initialization.");
            AssertContains(hashPreparationPlan, "preparationPlan.preScanFileCountThreshold = 200;", "Phase 65 HashPreparationPlan.cpp does not yet preserve the baseline pre-scan threshold.");
            AssertContains(hashPreparationPlan, "bool ShouldPreScanHashRequestFileSizes(const HashPreparationPlan& preparationPlan, const HashRequest& request)", "Phase 65 HashPreparationPlan.cpp does not yet own pre-scan gating queries.");

            AssertContains(hashJobExecutionPlanHeader, "HashPreparationPlan preparationPlan;", "Phase 65 HashJobExecutionPlan.h does not yet carry preparation planning state.");
            AssertContains(hashJobExecutionPlanHeader, "const HashPreparationPlan& GetHashJobPreparationPlan(const HashJobExecutionPlan& executionPlan);", "Phase 65 HashJobExecutionPlan.h does not yet expose preparation-plan access.");
            AssertContains(hashJobExecutionPlan, "executionPlan->preparationPlan = CreateHashPreparationPlan(request);", "Phase 65 HashJobExecutionPlan.cpp does not yet initialize preparation planning.");
            AssertContains(hashJobExecutionPlan, "const HashPreparationPlan& GetHashJobPreparationPlan(const HashJobExecutionPlan& executionPlan)", "Phase 65 HashJobExecutionPlan.cpp does not yet own preparation-plan retrieval.");
            AssertContains(hashJobExecutionPlan, "return executionPlan.preparationPlan;", "Phase 65 HashJobExecutionPlan.cpp does not yet return planned preparation controls.");

            AssertContains(hashEngine, "HashJobExecutionPlan executionPlan = {};", "Phase 65 HashEngine.cpp does not yet allocate a shared execution plan per request.");
            AssertContains(hashEngine, "InitializeHashJobExecutionPlan(request, &executionPlan);", "Phase 65 HashEngine.cpp does not yet initialize the shared execution plan.");
            AssertContains(hashEngine, "PrepareHashingWork(executionContext, request, GetHashJobPreparationPlan(executionPlan), fSizes, &wasCancelled)", "Phase 65 HashEngine.cpp does not yet consume preparation planning through the shared execution plan.");
            AssertContains(hashEngine, "RunHashScheduler(executionContext, request, executionPlan, isSizeCaled, fSizes)", "Phase 65 HashEngine.cpp does not yet pass the shared execution plan into scheduling.");
            AssertContains(hashEnginePreparation, "TryPreScanSmallBatchFileSizes(HashExecutionContext *executionContext, const HashRequest& request, const HashPreparationPlan& preparationPlan", "Phase 65 HashEnginePreparation.cpp does not yet consume preparation planning in pre-scan gating.");
            AssertContains(hashEnginePreparation, "PrepareHashingWork(HashExecutionContext *executionContext, const HashRequest& request, const HashPreparationPlan& preparationPlan", "Phase 65 HashEnginePreparation.cpp does not yet consume preparation planning in preparation flow.");
            AssertContains(hashEnginePreparation, "ShouldPreScanHashRequestFileSizes(preparationPlan, request)", "Phase 65 HashEnginePreparation.cpp does not yet route pre-scan gating through preparation-plan seams.");
            AssertDoesNotContain(hashEnginePreparation, "if (GetHashRequestFileCount(request) < 200)", "Phase 65 HashEnginePreparation.cpp should no longer hard-code pre-scan thresholds.");
            AssertContains(hashScheduler, "executionState.executionPlan = executionPlan;", "Phase 65 HashScheduler.cpp does not yet consume the shared execution plan from HashEngine.");

            AssertContains(hashEngineInternal, "#include \"Common/HashPreparationPlan.h\"", "Phase 65 HashEngineInternal.h does not yet consume the HashPreparationPlan seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashPreparationPlan.cpp", "Phase 65 desktop native core project does not yet compile HashPreparationPlan.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashPreparationPlan.h", "Phase 65 desktop native core project does not yet include HashPreparationPlan.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashPreparationPlan.cpp", "Phase 65 desktop native core filters do not yet expose HashPreparationPlan.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashPreparationPlan.h", "Phase 65 desktop native core filters do not yet expose HashPreparationPlan.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashPreparationPlan.cpp", "Phase 65 UWP native project does not yet compile HashPreparationPlan.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashPreparationPlan.h", "Phase 65 UWP native project does not yet include HashPreparationPlan.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashPreparationPlan.cpp", "Phase 65 UWP native filters do not yet expose HashPreparationPlan.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashPreparationPlan.h", "Phase 65 UWP native filters do not yet expose HashPreparationPlan.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashPreparationPlan.cpp", "Phase 65 WinUI native project should keep consuming the shared native core instead of compiling HashPreparationPlan.cpp directly.");
        }, failures);

        Run("Phase 66 promotes digest buffer sizing into a dedicated digest-buffer plan seam", () =>
        {
            string hashDigestBufferPlan = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestBufferPlan.cpp");
            string hashDigestBufferPlanHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestBufferPlan.h");
            string hashDigestPipeline = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp");
            string hashDigestQueue = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp");
            string hashDigestQueueHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.h");
            string hashJobExecutionPlan = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.cpp");
            string hashJobExecutionPlanHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashDigestBufferPlanHeader, "struct HashDigestBufferPlan", "Phase 66 HashDigestBufferPlan.h does not yet expose digest buffer planning state.");
            AssertContains(hashDigestBufferPlanHeader, "unsigned int preferredBufferLength;", "Phase 66 HashDigestBufferPlan.h does not yet expose preferred digest buffer length.");
            AssertContains(hashDigestBufferPlanHeader, "kDefaultHashBufferLength = 1u * 1024u * 1024u;", "Phase 66 HashDigestBufferPlan.h does not yet expose the named default digest-buffer size.");
            AssertContains(hashDigestBufferPlanHeader, "HashDigestBufferPlan CreateDefaultHashDigestBufferPlan();", "Phase 66 HashDigestBufferPlan.h does not yet expose the default digest-buffer plan factory.");
            AssertContains(hashDigestBufferPlanHeader, "unsigned int GetHashDigestBufferPreferredLength(const HashDigestBufferPlan& digestBufferPlan);", "Phase 66 HashDigestBufferPlan.h does not yet expose digest-buffer plan querying.");
            AssertContains(hashDigestBufferPlan, "HashDigestBufferPlan CreateDefaultHashDigestBufferPlan()", "Phase 66 HashDigestBufferPlan.cpp does not yet own the default digest-buffer plan initialization.");
            AssertContains(hashDigestBufferPlan, "digestBufferPlan.preferredBufferLength = kDefaultHashBufferLength;", "Phase 66 HashDigestBufferPlan.cpp does not yet preserve the baseline preferred digest-buffer size through the named constant.");
            AssertContains(hashDigestBufferPlan, "unsigned int GetHashDigestBufferPreferredLength(const HashDigestBufferPlan& digestBufferPlan)", "Phase 66 HashDigestBufferPlan.cpp does not yet own digest-buffer plan querying.");

            AssertContains(hashJobExecutionPlanHeader, "HashDigestBufferPlan digestBufferPlan;", "Phase 66 HashJobExecutionPlan.h does not yet carry digest-buffer planning state.");
            AssertContains(hashJobExecutionPlanHeader, "const HashDigestBufferPlan& GetHashJobDigestBufferPlan(const HashJobExecutionPlan& executionPlan);", "Phase 66 HashJobExecutionPlan.h does not yet expose digest-buffer plan access.");
            AssertContains(hashJobExecutionPlan, "executionPlan->digestBufferPlan = CreateDefaultHashDigestBufferPlan();", "Phase 66 HashJobExecutionPlan.cpp does not yet initialize digest-buffer planning through the explicit default factory.");
            AssertContains(hashJobExecutionPlan, "const HashDigestBufferPlan& GetHashJobDigestBufferPlan(const HashJobExecutionPlan& executionPlan)", "Phase 66 HashJobExecutionPlan.cpp does not yet own digest-buffer plan retrieval.");
            AssertContains(hashJobExecutionPlan, "return executionPlan.digestBufferPlan;", "Phase 66 HashJobExecutionPlan.cpp does not yet return planned digest-buffer controls.");

            AssertContains(hashDigestPipeline, "HashDigestRuntimePlan digestRuntimePlan = CreateHashDigestRuntimePlan(executionState->executionPlan);", "Phase 66 HashDigestPipeline.cpp does not yet consume digest-buffer planning through runtime planning.");
            AssertContains(hashDigestPipeline, "unsigned int preferredBufferLength = GetHashDigestRuntimePreferredBufferLength(digestRuntimePlan);", "Phase 66 HashDigestPipeline.cpp does not yet apply digest-buffer planning.");
            AssertContains(hashDigestQueueHeader, "unsigned int NormalizeDigestDataBufferPreferredLength(unsigned int preferredLength);", "Phase 66 HashDigestQueue.h does not yet expose digest-buffer sizing controls.");
            AssertContains(hashDigestQueue, "unsigned int NormalizeDigestDataBufferPreferredLength(unsigned int preferredLength)", "Phase 66 HashDigestQueue.cpp does not yet own digest-buffer sizing controls.");

            AssertContains(hashEngineInternal, "#include \"Common/HashDigestBufferPlan.h\"", "Phase 66 HashEngineInternal.h does not yet consume the HashDigestBufferPlan seam.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestBufferPlan.cpp", "Phase 66 desktop native core project does not yet compile HashDigestBufferPlan.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestBufferPlan.h", "Phase 66 desktop native core project does not yet include HashDigestBufferPlan.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestBufferPlan.cpp", "Phase 66 desktop native core filters do not yet expose HashDigestBufferPlan.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestBufferPlan.h", "Phase 66 desktop native core filters do not yet expose HashDigestBufferPlan.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestBufferPlan.cpp", "Phase 66 UWP native project does not yet compile HashDigestBufferPlan.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestBufferPlan.h", "Phase 66 UWP native project does not yet include HashDigestBufferPlan.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestBufferPlan.cpp", "Phase 66 UWP native filters do not yet expose HashDigestBufferPlan.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestBufferPlan.h", "Phase 66 UWP native filters do not yet expose HashDigestBufferPlan.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestBufferPlan.cpp", "Phase 66 WinUI native project should keep consuming the shared native core instead of compiling HashDigestBufferPlan.cpp directly.");
        }, failures);

        Run("Phase 67 removes global digest-buffer state and routes buffer length explicitly through digest execution", () =>
        {
            string hashDigestQueue = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp");
            string hashDigestQueueHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.h");
            string hashDigestPipeline = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp");
            string hashDigestExecution = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp");
            string hashDigestExecutionHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.h");
            string hashDigestSinglePass = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestSinglePass.cpp");
            string hashDigestSinglePassHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestSinglePass.h");

            AssertContains(hashDigestQueueHeader, "explicit DigestDataBuffer(unsigned int preferredLength);", "Phase 67 HashDigestQueue.h does not yet expose explicit per-buffer length construction.");
            AssertContains(hashDigestQueueHeader, "unsigned int capacity;", "Phase 67 HashDigestQueue.h does not yet expose per-buffer capacity.");
            AssertDoesNotContain(hashDigestQueueHeader, "static unsigned int preflen;", "Phase 67 HashDigestQueue.h should no longer expose global static buffer length.");
            AssertContains(hashDigestQueueHeader, "uint64_t CalculateFileChunkIterations(uint64_t fileSize, unsigned int preferredLength);", "Phase 67 HashDigestQueue.h does not yet expose explicit chunk-iteration sizing.");
            AssertContains(hashDigestQueue, "DigestDataBuffer::DigestDataBuffer(unsigned int preferredLength):datalen(0), capacity(NormalizeDigestDataBufferPreferredLength(preferredLength)), data(NULL)", "Phase 67 HashDigestQueue.cpp does not yet initialize per-buffer capacity.");
            AssertContains(hashDigestQueue, "int64_t readRet = executionState->fileAttemptState.osFile->read(dataBuffer.data, dataBuffer.capacity);", "Phase 67 HashDigestQueue.cpp does not yet read using per-buffer capacity.");
        AssertContains(hashDigestQueue, "isFileFinished.store(ptrDataBufFile->datalen < ptrDataBufFile->capacity);", "Phase 67 HashDigestQueue.cpp does not yet route completion checks through per-buffer capacity.");
            AssertDoesNotContain(hashDigestQueue, "DigestDataBuffer::preflen", "Phase 67 HashDigestQueue.cpp should no longer rely on global static digest-buffer length.");
            AssertDoesNotContain(hashDigestQueue, "SetDigestDataBufferPreferredLength(", "Phase 67 HashDigestQueue.cpp should no longer expose global digest-buffer mutation helpers.");

            AssertContains(hashDigestPipeline, "uint64_t times = CalculateFileChunkIterations(fsize, preferredBufferLength);", "Phase 67 HashDigestPipeline.cpp does not yet route chunk-iteration sizing through explicit buffer length.");
            AssertContains(hashDigestPipeline, "ExecuteOpenedFileDigestUpdate(executionContext, digestRuntimePlan, fsize, isSizeCaled, executionState", "Phase 67 HashDigestPipeline.cpp does not yet route explicit buffer length into digest execution dispatch.");

            AssertContains(hashDigestExecutionHeader, "const HashDigestRuntimePlan& digestRuntimePlan", "Phase 67 HashDigestExecution.h does not yet expose digest runtime planning in digest execution dispatch.");
            AssertContains(hashDigestExecution, "ProcessOpenedFileHashingParallel(executionContext, digestUpdateRequest, fsize, isSizeCaled, preferredBufferLength, digestQueuePlan, executionState, threadPool);", "Phase 67 HashDigestExecution.cpp does not yet route explicit buffer sizing into parallel digest execution.");
            AssertContains(hashDigestExecution, "ProcessOpenedFileHashingSinglePass(executionContext, digestUpdateRequest, fsize, isSizeCaled, preferredBufferLength, executionState);", "Phase 67 HashDigestExecution.cpp does not yet route explicit buffer sizing into single-pass digest execution.");

            AssertContains(hashDigestSinglePassHeader, "bool ProcessOpenedFileHashingSinglePass(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fsize, bool isSizeCaled, unsigned int preferredBufferLength,", "Phase 67 HashDigestSinglePass.h does not yet expose explicit buffer sizing.");
            AssertContains(hashDigestSinglePass, "DigestDataBuffer databuf(preferredBufferLength);", "Phase 67 HashDigestSinglePass.cpp does not yet construct single-pass buffers from explicit buffer sizing.");
            AssertContains(hashDigestSinglePass, "isFileFinished = (databuf.datalen < databuf.capacity);", "Phase 67 HashDigestSinglePass.cpp does not yet route completion checks through per-buffer capacity.");
        }, failures);

        Run("Phase 68 promotes digest runtime and completion lifecycle into dedicated seams", () =>
        {
            string hashDigestRuntimePlan = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestRuntimePlan.cpp");
            string hashDigestRuntimePlanHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestRuntimePlan.h");
            string hashDigestCompletion = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestCompletion.cpp");
            string hashDigestCompletionHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestCompletion.h");
            string hashDigestPipeline = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp");
            string hashDigestExecution = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp");
            string hashDigestExecutionHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashDigestRuntimePlanHeader, "struct HashDigestRuntimePlan", "Phase 68 HashDigestRuntimePlan.h does not yet expose the digest runtime-plan contract.");
            AssertContains(hashDigestRuntimePlanHeader, "const DigestUpdateRequest& digestUpdateRequest;", "Phase 68 HashDigestRuntimePlan.h does not yet expose digest request state.");
            AssertContains(hashDigestRuntimePlanHeader, "const HashDigestQueuePlan& digestQueuePlan;", "Phase 68 HashDigestRuntimePlan.h does not yet expose queue-plan state.");
            AssertContains(hashDigestRuntimePlanHeader, "HashDigestRuntimePlan(const DigestUpdateRequest& updateRequest, HashDigestExecutionMode executionMode, unsigned int bufferLength, const HashDigestQueuePlan& queuePlan)", "Phase 68 HashDigestRuntimePlan.h does not yet require explicit runtime-plan dependencies.");
            AssertContains(hashDigestRuntimePlanHeader, "HashDigestRuntimePlan CreateHashDigestRuntimePlan(const HashJobExecutionPlan& executionPlan);", "Phase 68 HashDigestRuntimePlan.h does not yet expose runtime-plan creation.");
            AssertContains(hashDigestRuntimePlanHeader, "const DigestUpdateRequest& GetHashDigestRuntimeUpdateRequest(const HashDigestRuntimePlan& digestRuntimePlan);", "Phase 68 HashDigestRuntimePlan.h does not yet expose digest-request querying.");
            AssertContains(hashDigestRuntimePlanHeader, "const HashDigestQueuePlan& GetHashDigestRuntimeQueuePlan(const HashDigestRuntimePlan& digestRuntimePlan);", "Phase 68 HashDigestRuntimePlan.h does not yet expose queue-plan querying.");
            AssertContains(hashDigestRuntimePlan, "HashDigestRuntimePlan CreateHashDigestRuntimePlan(const HashJobExecutionPlan& executionPlan)", "Phase 68 HashDigestRuntimePlan.cpp does not yet own runtime-plan creation.");
            AssertContains(hashDigestRuntimePlan, "return HashDigestRuntimePlan(", "Phase 68 HashDigestRuntimePlan.cpp does not yet construct runtime plans through the explicit constructor seam.");
            AssertContains(hashDigestRuntimePlan, "GetHashJobDigestUpdateRequest(executionPlan),", "Phase 68 HashDigestRuntimePlan.cpp does not yet preserve digest-request planning.");
            AssertContains(hashDigestRuntimePlan, "GetHashJobDigestQueuePlan(executionPlan));", "Phase 68 HashDigestRuntimePlan.cpp does not yet preserve queue-plan planning.");
            AssertContains(hashDigestRuntimePlan, "GetHashDigestBufferPreferredLength(GetHashJobDigestBufferPlan(executionPlan))", "Phase 68 HashDigestRuntimePlan.cpp does not yet preserve digest-buffer planning.");

            AssertContains(hashDigestCompletionHeader, "bool CompleteOpenedFileDigestExecution(HashExecutionContext *executionContext, FileExecutionState *executionState, bool wasStopped);", "Phase 68 HashDigestCompletion.h does not yet expose digest completion lifecycle.");
            AssertContains(hashDigestCompletion, "bool CompleteOpenedFileDigestExecution(HashExecutionContext *executionContext, FileExecutionState *executionState, bool wasStopped)", "Phase 68 HashDigestCompletion.cpp does not yet own digest completion lifecycle.");
            AssertContains(hashDigestCompletion, "if (ShouldStopHashExecution(*executionContext))", "Phase 68 HashDigestCompletion.cpp does not yet preserve stop-check completion gating.");

            AssertContains(hashDigestPipeline, "HashDigestRuntimePlan digestRuntimePlan = CreateHashDigestRuntimePlan(executionState->executionPlan);", "Phase 68 HashDigestPipeline.cpp does not yet initialize digest runtime planning.");
            AssertContains(hashDigestPipeline, "if (CompleteOpenedFileDigestExecution(executionContext, executionState, wasStopped))", "Phase 68 HashDigestPipeline.cpp does not yet delegate post-execution completion lifecycle.");
            AssertContains(hashDigestExecutionHeader, "const HashDigestRuntimePlan& digestRuntimePlan", "Phase 68 HashDigestExecution.h does not yet expose runtime-plan dispatch.");
            AssertContains(hashDigestExecution, "GetHashDigestRuntimePreferredBufferLength(digestRuntimePlan);", "Phase 68 HashDigestExecution.cpp does not yet resolve explicit buffer length through runtime planning.");

            AssertContains(hashEngineInternal, "#include \"Common/HashDigestRuntimePlan.h\"", "Phase 68 HashEngineInternal.h does not yet consume HashDigestRuntimePlan.");
            AssertContains(hashEngineInternal, "#include \"Common/HashDigestCompletion.h\"", "Phase 68 HashEngineInternal.h does not yet consume HashDigestCompletion.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestRuntimePlan.cpp", "Phase 68 desktop native core project does not yet compile HashDigestRuntimePlan.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestRuntimePlan.h", "Phase 68 desktop native core project does not yet include HashDigestRuntimePlan.h.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestCompletion.cpp", "Phase 68 desktop native core project does not yet compile HashDigestCompletion.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestCompletion.h", "Phase 68 desktop native core project does not yet include HashDigestCompletion.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestRuntimePlan.cpp", "Phase 68 desktop native core filters do not yet expose HashDigestRuntimePlan.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestRuntimePlan.h", "Phase 68 desktop native core filters do not yet expose HashDigestRuntimePlan.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestCompletion.cpp", "Phase 68 desktop native core filters do not yet expose HashDigestCompletion.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestCompletion.h", "Phase 68 desktop native core filters do not yet expose HashDigestCompletion.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestRuntimePlan.cpp", "Phase 68 UWP native project does not yet compile HashDigestRuntimePlan.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestRuntimePlan.h", "Phase 68 UWP native project does not yet include HashDigestRuntimePlan.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestCompletion.cpp", "Phase 68 UWP native project does not yet compile HashDigestCompletion.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestCompletion.h", "Phase 68 UWP native project does not yet include HashDigestCompletion.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestRuntimePlan.cpp", "Phase 68 UWP native filters do not yet expose HashDigestRuntimePlan.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestRuntimePlan.h", "Phase 68 UWP native filters do not yet expose HashDigestRuntimePlan.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestCompletion.cpp", "Phase 68 UWP native filters do not yet expose HashDigestCompletion.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestCompletion.h", "Phase 68 UWP native filters do not yet expose HashDigestCompletion.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestRuntimePlan.cpp", "Phase 68 WinUI native project should keep consuming the shared native core instead of compiling HashDigestRuntimePlan.cpp directly.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestCompletion.cpp", "Phase 68 WinUI native project should keep consuming the shared native core instead of compiling HashDigestCompletion.cpp directly.");
        }, failures);

        Run("Phase 69 promotes digest context lifecycle and digest finalization into a dedicated lifecycle seam", () =>
        {
            string hashDigestLifecycle = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestLifecycle.cpp");
            string hashDigestLifecycleHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestLifecycle.h");
            string hashDigestContextOps = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestContextOps.cpp");
            string hashDigestContextOpsHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestContextOps.h");
            string hashDigestOperationRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestOperationRegistry.cpp");
            string hashDigestOperationRegistryHeader = ReadHashDigestOperationRegistrySeams(repoRoot);
            string hashEngineResult = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp");
            string hashResultPublisher = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp");
            string hashSuccessfulFileCompletionWorkflow = ReadRepoFile(repoRoot, @"trunk\source\Common\HashSuccessfulFileCompletionWorkflow.cpp");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashDigestLifecycleHeader, "struct FileHashContexts;", "Phase 69 HashDigestLifecycle.h does not yet expose digest lifecycle context seams.");
            AssertContains(hashDigestLifecycleHeader, "void InitializeFileHashing(const HashRequest& request, HashExecutionContext *executionContext, FileHashContexts *hashContexts);", "Phase 69 HashDigestLifecycle.h does not yet expose hash-context initialization.");
            AssertContainsAny(hashDigestLifecycleHeader,
                [
                    "const sunjwbase::tstring& GetFinalizedDigestValue(const ResultDigestStorage& digestBundle, ResultDigestType digestType);",
                    "const sunjwbase::tstring& GetFinalizedDigestValueById(const ResultDigestStorage& digestBundle, const HashAlgorithmId& algorithmId);"
                ],
                "Phase 69 HashDigestLifecycle.h does not yet expose finalized digest getters.");
            AssertContainsAny(hashDigestLifecycleHeader,
                [
                    "void SetFinalizedDigestValue(ResultDigestStorage& digestBundle, ResultDigestType digestType, const sunjwbase::tstring& digestValue);",
                    "void SetFinalizedDigestValueById(ResultDigestStorage& digestBundle, const HashAlgorithmId& algorithmId, const sunjwbase::tstring& digestValue);"
                ],
                "Phase 69 HashDigestLifecycle.h does not yet expose finalized digest setters.");
            AssertContains(hashDigestLifecycleHeader, "void PopulateDigestResult(const HashRequest& request, HashResult& result, const ResultDigestStorage& digestBundle);", "Phase 69 HashDigestLifecycle.h does not yet expose digest projection.");
            AssertContains(hashDigestLifecycleHeader, "bool FinalizeDigestStrings(const HashRequest& request, FileHashContexts& hashContexts, ResultDigestStorage& digestBundle, sunjwbase::tstring *errorText);", "Phase 69 HashDigestLifecycle.h does not yet expose digest finalization.");

            AssertContains(hashDigestLifecycle, "void InitializeFileHashing(const HashRequest& request, HashExecutionContext *executionContext, FileHashContexts *hashContexts)", "Phase 69 HashDigestLifecycle.cpp does not yet own hash-context initialization.");
            AssertContains(hashDigestLifecycle, "bool FinalizeDigestStrings(const HashRequest& request, FileHashContexts& hashContexts, ResultDigestStorage& digestBundle, sunjwbase::tstring *errorText)", "Phase 69 HashDigestLifecycle.cpp does not yet own digest finalization.");
            AssertContains(hashDigestLifecycle, "void PopulateDigestResult(const HashRequest& request, HashResult& result, const ResultDigestStorage& digestBundle)", "Phase 69 HashDigestLifecycle.cpp does not yet own digest projection.");
            AssertContainsAny(hashDigestLifecycle,
                [
                    "InitializeHashDigestContext(hashContexts, digestType);",
                    "InitializeHashDigestContextById(hashContexts, algorithmId);"
                ],
                "Phase 69 HashDigestLifecycle.cpp does not yet delegate initialization through hash-digest context seams.");
            AssertContainsAny(hashDigestLifecycle,
                [
                    "FinalizeHashDigestContext(hashContexts, digestType, digestBundle, errorText)",
                    "FinalizeHashDigestContextById(hashContexts, algorithmId, digestBundle, errorText)"
                ],
                "Phase 69 HashDigestLifecycle.cpp does not yet delegate finalization through hash-digest context seams.");
            AssertContainsAny(hashDigestContextOpsHeader,
                [
                    "void InitializeHashDigestContext(FileHashContexts *hashContexts, ResultDigestType digestType);",
                    "void InitializeHashDigestContextById(FileHashContexts *hashContexts, const HashAlgorithmId& algorithmId);"
                ],
                "Phase 69 HashDigestContextOps.h does not yet expose digest-context initialization.");
            AssertContainsAny(hashDigestContextOpsHeader,
                [
                    "bool FinalizeHashDigestContext(FileHashContexts& hashContexts, ResultDigestType digestType, ResultDigestStorage& digestBundle, sunjwbase::tstring *errorText);",
                    "bool FinalizeHashDigestContextById(FileHashContexts& hashContexts, const HashAlgorithmId& algorithmId, ResultDigestStorage& digestBundle, sunjwbase::tstring *errorText);"
                ],
                "Phase 69 HashDigestContextOps.h does not yet expose digest-context finalization.");
            AssertContainsAny(hashDigestContextOps,
                [
                    "void InitializeHashDigestContext(FileHashContexts *hashContexts, ResultDigestType digestType)",
                    "void InitializeHashDigestContextById(FileHashContexts *hashContexts, const HashAlgorithmId& algorithmId)"
                ],
                "Phase 69 HashDigestContextOps.cpp does not yet own digest-context initialization.");
            AssertContainsAny(hashDigestContextOps,
                [
                    "bool FinalizeHashDigestContext(FileHashContexts& hashContexts, ResultDigestType digestType, ResultDigestStorage& digestBundle, sunjwbase::tstring *errorText)",
                    "bool FinalizeHashDigestContextById(FileHashContexts& hashContexts, const HashAlgorithmId& algorithmId, ResultDigestStorage& digestBundle, sunjwbase::tstring *errorText)"
                ],
                "Phase 69 HashDigestContextOps.cpp does not yet own digest-context finalization.");
            AssertContains(hashDigestContextOps, "TryGetHashAlgorithmDescriptorById(algorithmId, &algorithmDescriptor)", "Phase 69 HashDigestContextOps.cpp does not yet resolve descriptor metadata before finalization.");
            AssertContains(hashDigestContextOps, "!DoesHashAlgorithmDescriptorRequireDigestOperations(algorithmDescriptor)", "Phase 69 HashDigestContextOps.cpp does not yet skip descriptor-only algorithms during finalization.");
            AssertContains(hashDigestOperationRegistryHeader, "struct HashDigestOperationDescriptor", "Phase 69 HashDigestOperationRegistry.h does not yet expose digest operation descriptors.");
            AssertContainsAny(hashDigestOperationRegistryHeader,
                [
                    "bool TryGetHashDigestOperationDescriptor(ResultDigestType digestType, HashDigestOperationDescriptor *operationDescriptor);",
                    "static inline bool TryGetHashDigestOperationDescriptor(ResultDigestType digestType, HashDigestOperationDescriptor *operationDescriptor)"
                ],
                "Phase 69 HashDigestOperationRegistry.h does not yet expose digest operation lookup.");
            AssertContains(hashDigestOperationRegistryHeader, "bool TryGetHashDigestOperationDescriptorById(const HashAlgorithmId& algorithmId, HashDigestOperationDescriptor *operationDescriptor);", "Phase 69 HashDigestOperationRegistry.h does not yet expose descriptor/id operation lookup.");
            AssertContains(hashDigestOperationRegistry, "MD5Final(&hashContexts.mdContext);", "Phase 69 HashDigestOperationRegistry.cpp does not yet preserve MD5 finalization.");
            AssertContains(hashDigestOperationRegistry, "hashContexts.sha1.Final();", "Phase 69 HashDigestOperationRegistry.cpp does not yet preserve SHA1 finalization.");
            AssertContains(hashDigestOperationRegistry, "FinalizeOpenSslSha256DigestContext", "Phase 69 HashDigestOperationRegistry.cpp does not yet preserve SHA-256 finalization through the OpenSSL provider.");
            AssertContains(hashDigestOperationRegistry, "FinalizeOpenSslSha512DigestContext", "Phase 69 HashDigestOperationRegistry.cpp does not yet preserve SHA-512 finalization through the OpenSSL provider.");

            AssertContains(hashEngineResult, "uint64_t PrepareFileMetaResult(", "Phase 69 HashEngineResult.cpp should keep owning file metadata projection.");
            AssertDoesNotContain(hashEngineResult, "void InitializeFileHashing(", "Phase 69 HashEngineResult.cpp should no longer own hash-context initialization.");
            AssertDoesNotContain(hashEngineResult, "FinalizeDigestStrings(", "Phase 69 HashEngineResult.cpp should no longer own digest finalization.");

            AssertContains(string.Join("\r\n", hashResultPublisher, hashSuccessfulFileCompletionWorkflow), "FinalizeDigestStrings(request, executionState.hashContexts, executionState.digestBundle, &finalizeErrorText)", "Phase 69 result publication flow does not yet consume digest finalization through lifecycle seams.");
            AssertContains(string.Join("\r\n", hashResultPublisher, hashSuccessfulFileCompletionWorkflow), "PopulateDigestResult(request, result, executionState.digestBundle);", "Phase 69 result publication flow does not yet consume digest projection through lifecycle seams.");

            AssertContains(hashEngineInternal, "#include \"Common/HashDigestLifecycle.h\"", "Phase 69 HashEngineInternal.h does not yet consume HashDigestLifecycle.");
            AssertContains(hashEngineInternal, "#include \"Common/HashDigestContextOps.h\"", "Phase 69 HashEngineInternal.h does not yet consume HashDigestContextOps.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestLifecycle.cpp", "Phase 69 desktop native core project does not yet compile HashDigestLifecycle.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestLifecycle.h", "Phase 69 desktop native core project does not yet include HashDigestLifecycle.h.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestContextOps.cpp", "Phase 69 desktop native core project does not yet compile HashDigestContextOps.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestContextOps.h", "Phase 69 desktop native core project does not yet include HashDigestContextOps.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestLifecycle.cpp", "Phase 69 desktop native core filters do not yet expose HashDigestLifecycle.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestLifecycle.h", "Phase 69 desktop native core filters do not yet expose HashDigestLifecycle.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestContextOps.cpp", "Phase 69 desktop native core filters do not yet expose HashDigestContextOps.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestContextOps.h", "Phase 69 desktop native core filters do not yet expose HashDigestContextOps.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestLifecycle.cpp", "Phase 69 UWP native project does not yet compile HashDigestLifecycle.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestLifecycle.h", "Phase 69 UWP native project does not yet include HashDigestLifecycle.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestContextOps.cpp", "Phase 69 UWP native project does not yet compile HashDigestContextOps.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestContextOps.h", "Phase 69 UWP native project does not yet include HashDigestContextOps.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestLifecycle.cpp", "Phase 69 UWP native filters do not yet expose HashDigestLifecycle.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestLifecycle.h", "Phase 69 UWP native filters do not yet expose HashDigestLifecycle.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestContextOps.cpp", "Phase 69 UWP native filters do not yet expose HashDigestContextOps.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestContextOps.h", "Phase 69 UWP native filters do not yet expose HashDigestContextOps.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestLifecycle.cpp", "Phase 69 WinUI native project should keep consuming the shared native core instead of compiling HashDigestLifecycle.cpp directly.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestContextOps.cpp", "Phase 69 WinUI native project should keep consuming the shared native core instead of compiling HashDigestContextOps.cpp directly.");
        }, failures);

        Run("Phase 70 promotes per-algorithm digest context initialization and finalization into dedicated context-ops seams", () =>
        {
            string hashDigestLifecycle = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestLifecycle.cpp");
            string hashDigestContextOps = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestContextOps.cpp");
            string hashDigestContextOpsHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestContextOps.h");
            string hashDigestOperationRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestOperationRegistry.cpp");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContainsAny(hashDigestContextOpsHeader,
                [
                    "void InitializeHashDigestContext(FileHashContexts *hashContexts, ResultDigestType digestType);",
                    "void InitializeHashDigestContextById(FileHashContexts *hashContexts, const HashAlgorithmId& algorithmId);"
                ],
                "Phase 70 HashDigestContextOps.h does not yet expose per-algorithm initialization.");
            AssertContainsAny(hashDigestContextOpsHeader,
                [
                    "bool FinalizeHashDigestContext(FileHashContexts& hashContexts, ResultDigestType digestType, ResultDigestStorage& digestBundle, sunjwbase::tstring *errorText);",
                    "bool FinalizeHashDigestContextById(FileHashContexts& hashContexts, const HashAlgorithmId& algorithmId, ResultDigestStorage& digestBundle, sunjwbase::tstring *errorText);"
                ],
                "Phase 70 HashDigestContextOps.h does not yet expose per-algorithm finalization.");

            AssertContainsAny(hashDigestContextOps,
                [
                    "TryGetHashDigestOperationDescriptor(digestType, &operationDescriptor)",
                    "TryGetHashDigestOperationDescriptorById(algorithmId, &operationDescriptor)"
                ],
                "Phase 70 HashDigestContextOps.cpp does not yet route context operations through operation-registry lookup.");
            AssertContains(hashDigestOperationRegistry, "MD5Init(&hashContexts->mdContext);", "Phase 70 HashDigestOperationRegistry.cpp does not yet preserve standard MD5 init.");
            AssertDoesNotContain(hashDigestOperationRegistry, "MD5Init(&hashContexts->mdContext, 0);", "Phase 70 HashDigestOperationRegistry.cpp still routes standard MD5 through the seeded init signature.");
            AssertContains(hashDigestOperationRegistry, "hashContexts->sha1.Reset();", "Phase 70 HashDigestOperationRegistry.cpp does not yet preserve SHA1 init.");
            AssertContains(hashDigestOperationRegistry, "InitializeOpenSslSha256DigestContext", "Phase 70 HashDigestOperationRegistry.cpp does not yet preserve SHA-256 init through the OpenSSL provider.");
            AssertContains(hashDigestOperationRegistry, "InitializeOpenSslSha512DigestContext", "Phase 70 HashDigestOperationRegistry.cpp does not yet preserve SHA-512 init through the OpenSSL provider.");

            AssertContainsAny(hashDigestLifecycle,
                [
                    "InitializeHashDigestContext(hashContexts, digestType);",
                    "InitializeHashDigestContextById(hashContexts, algorithmId);"
                ],
                "Phase 70 HashDigestLifecycle.cpp does not yet consume per-algorithm initialization seams.");
            AssertContainsAny(hashDigestLifecycle,
                [
                    "FinalizeHashDigestContext(hashContexts, digestType, digestBundle, errorText)",
                    "FinalizeHashDigestContextById(hashContexts, algorithmId, digestBundle, errorText)"
                ],
                "Phase 70 HashDigestLifecycle.cpp does not yet consume per-algorithm finalization seams.");
            AssertDoesNotContain(hashDigestLifecycle, "switch (digestType)", "Phase 70 HashDigestLifecycle.cpp should no longer own algorithm-specific branching.");

            AssertContains(hashEngineInternal, "#include \"Common/HashDigestContextOps.h\"", "Phase 70 HashEngineInternal.h does not yet consume HashDigestContextOps.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestContextOps.cpp", "Phase 70 desktop native core project does not yet compile HashDigestContextOps.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestContextOps.h", "Phase 70 desktop native core project does not yet include HashDigestContextOps.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestContextOps.cpp", "Phase 70 desktop native core filters do not yet expose HashDigestContextOps.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestContextOps.h", "Phase 70 desktop native core filters do not yet expose HashDigestContextOps.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestContextOps.cpp", "Phase 70 UWP native project does not yet compile HashDigestContextOps.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestContextOps.h", "Phase 70 UWP native project does not yet include HashDigestContextOps.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestContextOps.cpp", "Phase 70 UWP native filters do not yet expose HashDigestContextOps.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestContextOps.h", "Phase 70 UWP native filters do not yet expose HashDigestContextOps.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestContextOps.cpp", "Phase 70 WinUI native project should keep consuming the shared native core instead of compiling HashDigestContextOps.cpp directly.");
        }, failures);

        Run("Phase 71 promotes file-size accounting into a dedicated file-size seam", () =>
        {
            string hashEngineResult = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp");
            string hashFileSizeAccounting = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileSizeAccounting.cpp");
            string hashFileSizeAccountingHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileSizeAccounting.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashFileSizeAccountingHeader, "uint64_t TrackHashResolvedFileSize(HashExecutionContext *executionContext, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, HashResult& result, uint64_t fsize);", "Phase 71 HashFileSizeAccounting.h does not yet expose the direct tracked-size seam.");
            AssertContains(hashFileSizeAccountingHeader, "uint64_t ResolveHashFileSizeAndTrack(HashExecutionContext *executionContext, sunjwbase::OsFile& osFile, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, HashResult& result);", "Phase 71 HashFileSizeAccounting.h does not yet expose file-size accounting seams.");
            AssertContains(hashFileSizeAccounting, "uint64_t TrackHashResolvedFileSize(HashExecutionContext *executionContext, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, HashResult& result, uint64_t fsize)", "Phase 71 HashFileSizeAccounting.cpp does not yet expose direct tracked-size accounting.");
            AssertContains(hashFileSizeAccounting, "uint64_t ResolveHashFileSizeAndTrack(HashExecutionContext *executionContext, sunjwbase::OsFile& osFile, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, HashResult& result)", "Phase 71 HashFileSizeAccounting.cpp does not yet own file-size accounting.");
            AssertContains(hashFileSizeAccounting, "result.meta.size = fsize;", "Phase 71 HashFileSizeAccounting.cpp does not yet project file-size metadata.");
            AssertContains(hashFileSizeAccounting, "AddHashExecutionTotalSize(*executionContext, fsize);", "Phase 71 HashFileSizeAccounting.cpp does not yet preserve uncounted-size accumulation.");
            AssertContains(hashFileSizeAccounting, "ReplaceHashExecutionCountedFileSize(*executionContext, fSizes[fileIndex], fsize);", "Phase 71 HashFileSizeAccounting.cpp does not yet preserve counted-size replacement.");
            AssertContains(hashFileSizeAccounting, "fSizes[fileIndex] = fsize;", "Phase 71 HashFileSizeAccounting.cpp does not yet preserve counted-size cache updates.");
            AssertContains(hashFileSizeAccounting, "return TrackHashResolvedFileSize(executionContext, isSizeCaled, fSizes, fileIndex, result, osFile.getLength());", "Phase 71 HashFileSizeAccounting.cpp does not yet preserve OsFile length retrieval through the tracked-size seam.");
            AssertContains(hashEngineResult, "result.meta.modifiedDate = osFile.getModifiedTimeFormat();", "Phase 71 HashEngineResult.cpp does not yet project file metadata from the opened handle.");
            AssertContains(hashEngineResult, "fsize = ResolveHashFileSizeAndTrack(executionContext, osFile, isSizeCaled, fSizes, fileIndex, result);", "Phase 71 HashEngineResult.cpp does not yet delegate fallback file-size accounting.");
            AssertDoesNotContain(hashEngineResult, "TryResolveWindowsPathFileMeta(path, &resolvedMeta)", "Phase 71 HashEngineResult.cpp should no longer use path-based metadata projection.");
            AssertDoesNotContain(hashEngineResult, "fsize = TrackHashResolvedFileSize(executionContext, isSizeCaled, fSizes, fileIndex, result, resolvedMeta.size);", "Phase 71 HashEngineResult.cpp should no longer reuse tracked-size accounting through path-based metadata.");
            AssertDoesNotContain(hashEngineResult, "uint64_t fsize = osFile.getLength();", "Phase 71 HashEngineResult.cpp should no longer inline file-size retrieval.");
            AssertContains(hashEngineInternal, "#include \"Common/HashFileSizeAccounting.h\"", "Phase 71 HashEngineInternal.h does not yet consume HashFileSizeAccounting.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileSizeAccounting.cpp", "Phase 71 desktop native core project does not yet compile HashFileSizeAccounting.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileSizeAccounting.h", "Phase 71 desktop native core project does not yet include HashFileSizeAccounting.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileSizeAccounting.cpp", "Phase 71 desktop native core filters do not yet expose HashFileSizeAccounting.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileSizeAccounting.h", "Phase 71 desktop native core filters do not yet expose HashFileSizeAccounting.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileSizeAccounting.cpp", "Phase 71 UWP native project does not yet compile HashFileSizeAccounting.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileSizeAccounting.h", "Phase 71 UWP native project does not yet include HashFileSizeAccounting.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashFileSizeAccounting.cpp", "Phase 71 UWP native filters do not yet expose HashFileSizeAccounting.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashFileSizeAccounting.h", "Phase 71 UWP native filters do not yet expose HashFileSizeAccounting.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashFileSizeAccounting.cpp", "Phase 71 WinUI native project should keep consuming the shared native core instead of compiling HashFileSizeAccounting.cpp directly.");
        }, failures);

        Run("Phase 72 promotes file-attempt state operations into a dedicated state-ops seam", () =>
        {
            string hashEnginePreparation = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp");
            string hashFileResultWorkflow = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileResultWorkflow.cpp");
            string hashFileAttemptStateOps = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptStateOps.cpp");
            string hashFileAttemptStateOpsHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptStateOps.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashFileAttemptStateOpsHeader, "struct FileAttemptState;", "Phase 72 HashFileAttemptStateOps.h does not yet expose FileAttemptState forward declarations.");
            AssertContains(hashFileAttemptStateOpsHeader, "struct FileProgressState;", "Phase 72 HashFileAttemptStateOps.h does not yet expose FileProgressState forward declarations.");
            AssertContains(hashFileAttemptStateOpsHeader, "void InitializeFileAttemptState(const TCHAR *path, sunjwbase::OsFile *osFile, FileAttemptState *fileAttemptState);", "Phase 72 HashFileAttemptStateOps.h does not yet expose file-attempt initialization.");
            AssertContains(hashFileAttemptStateOpsHeader, "bool OpenFileForHashing(FileAttemptState *fileAttemptState, void *openErrorBuffer);", "Phase 72 HashFileAttemptStateOps.h does not yet expose file-open state transitions.");
            AssertContains(hashFileAttemptStateOpsHeader, "void ResetFileProgressState(FileProgressState *progressState);", "Phase 72 HashFileAttemptStateOps.h does not yet expose file-progress reset operations.");

            AssertContains(hashFileAttemptStateOps, "void InitializeFileAttemptState(const TCHAR *path, sunjwbase::OsFile *osFile, FileAttemptState *fileAttemptState)", "Phase 72 HashFileAttemptStateOps.cpp does not yet own file-attempt initialization.");
            AssertContains(hashFileAttemptStateOps, "fileAttemptState->path = path;", "Phase 72 HashFileAttemptStateOps.cpp does not yet preserve path assignment.");
            AssertContains(hashFileAttemptStateOps, "fileAttemptState->osFile = osFile;", "Phase 72 HashFileAttemptStateOps.cpp does not yet preserve file-handle assignment.");
            AssertContains(hashFileAttemptStateOps, "fileAttemptState->fileVersion.clear();", "Phase 72 HashFileAttemptStateOps.cpp does not yet preserve version reset.");
            AssertContains(hashFileAttemptStateOps, "fileAttemptState->isFileOpened = fileAttemptState->osFile->openReadScan(openErrorBuffer);", "Phase 72 HashFileAttemptStateOps.cpp does not yet preserve openReadScan routing.");
            AssertContains(hashFileAttemptStateOps, "progressState->finishedSize = 0;", "Phase 72 HashFileAttemptStateOps.cpp does not yet preserve finished-size reset.");
            AssertContains(hashFileAttemptStateOps, "progressState->position = 0;", "Phase 72 HashFileAttemptStateOps.cpp does not yet preserve position reset.");
            AssertContains(hashFileResultWorkflow, "ResetFileProgressState(&executionState->progressState);", "Phase 72 file-result workflow seam does not yet consume the state-ops progress reset seam.");
            AssertDoesNotContain(hashEnginePreparation, "ResetFileProgressState(&executionState->progressState);", "Phase 72 HashEnginePreparation.cpp should no longer inline state-ops progress reset after workflow extraction.");
            AssertContains(hashEngineInternal, "#include \"Common/HashFileAttemptStateOps.h\"", "Phase 72 HashEngineInternal.h does not yet consume HashFileAttemptStateOps.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileAttemptStateOps.cpp", "Phase 72 desktop native core project does not yet compile HashFileAttemptStateOps.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileAttemptStateOps.h", "Phase 72 desktop native core project does not yet include HashFileAttemptStateOps.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileAttemptStateOps.cpp", "Phase 72 desktop native core filters do not yet expose HashFileAttemptStateOps.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileAttemptStateOps.h", "Phase 72 desktop native core filters do not yet expose HashFileAttemptStateOps.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileAttemptStateOps.cpp", "Phase 72 UWP native project does not yet compile HashFileAttemptStateOps.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileAttemptStateOps.h", "Phase 72 UWP native project does not yet include HashFileAttemptStateOps.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashFileAttemptStateOps.cpp", "Phase 72 UWP native filters do not yet expose HashFileAttemptStateOps.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashFileAttemptStateOps.h", "Phase 72 UWP native filters do not yet expose HashFileAttemptStateOps.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashFileAttemptStateOps.cpp", "Phase 72 WinUI native project should keep consuming the shared native core instead of compiling HashFileAttemptStateOps.cpp directly.");
        }, failures);

        Run("Phase 73 promotes pre-scan size probing into a dedicated probe seam", () =>
        {
            string hashEnginePreparation = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp");
            string hashPreScanSizeProbe = ReadRepoFile(repoRoot, @"trunk\source\Common\HashPreScanSizeProbe.cpp");
            string hashPreScanSizeProbeHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashPreScanSizeProbe.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashPreScanSizeProbeHeader, "uint64_t ResolveHashPreScannedFileSize(const TCHAR *path);", "Phase 73 HashPreScanSizeProbe.h does not yet expose pre-scan size probing.");
            AssertContains(hashPreScanSizeProbe, "uint64_t ResolveHashPreScannedFileSize(const TCHAR *path)", "Phase 73 HashPreScanSizeProbe.cpp does not yet own pre-scan size probing.");
            AssertContains(hashPreScanSizeProbe, "sunjwbase::OsFile osFile(path);", "Phase 73 HashPreScanSizeProbe.cpp does not yet preserve pre-scan file-open handling.");
            AssertContains(hashPreScanSizeProbe, "if (osFile.openRead())", "Phase 73 HashPreScanSizeProbe.cpp does not yet preserve pre-scan open gating.");
            AssertContains(hashPreScanSizeProbe, "fSize = osFile.getLength();", "Phase 73 HashPreScanSizeProbe.cpp does not yet preserve pre-scan length reads.");
            AssertContains(hashEnginePreparation, "uint64_t fSize = ResolveHashPreScannedFileSize(path);", "Phase 73 HashEnginePreparation.cpp does not yet delegate pre-scan size probing.");
            AssertDoesNotContain(hashEnginePreparation, "OsFile osFile(path);", "Phase 73 HashEnginePreparation.cpp should no longer own pre-scan file-open details.");
            AssertContains(hashEngineInternal, "#include \"Common/HashPreScanSizeProbe.h\"", "Phase 73 HashEngineInternal.h does not yet consume HashPreScanSizeProbe.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashPreScanSizeProbe.cpp", "Phase 73 desktop native core project does not yet compile HashPreScanSizeProbe.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashPreScanSizeProbe.h", "Phase 73 desktop native core project does not yet include HashPreScanSizeProbe.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashPreScanSizeProbe.cpp", "Phase 73 desktop native core filters do not yet expose HashPreScanSizeProbe.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashPreScanSizeProbe.h", "Phase 73 desktop native core filters do not yet expose HashPreScanSizeProbe.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashPreScanSizeProbe.cpp", "Phase 73 UWP native project does not yet compile HashPreScanSizeProbe.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashPreScanSizeProbe.h", "Phase 73 UWP native project does not yet include HashPreScanSizeProbe.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashPreScanSizeProbe.cpp", "Phase 73 UWP native filters do not yet expose HashPreScanSizeProbe.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashPreScanSizeProbe.h", "Phase 73 UWP native filters do not yet expose HashPreScanSizeProbe.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashPreScanSizeProbe.cpp", "Phase 73 WinUI native project should keep consuming the shared native core instead of compiling HashPreScanSizeProbe.cpp directly.");
        }, failures);

        Run("Phase 74 promotes pre-scan size accounting into a dedicated accounting seam", () =>
        {
            string hashEnginePreparation = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp");
            string hashPreScanSizeAccounting = ReadRepoFile(repoRoot, @"trunk\source\Common\HashPreScanSizeAccounting.cpp");
            string hashPreScanSizeAccountingHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashPreScanSizeAccounting.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashPreScanSizeAccountingHeader, "uint64_t TrackHashPreScannedFileSize(HashExecutionContext *executionContext, ULLongVector& fSizes, uint32_t fileIndex, uint64_t fSize);", "Phase 74 HashPreScanSizeAccounting.h does not yet expose pre-scan size accounting.");
            AssertContains(hashPreScanSizeAccounting, "uint64_t TrackHashPreScannedFileSize(HashExecutionContext *executionContext, ULLongVector& fSizes, uint32_t fileIndex, uint64_t fSize)", "Phase 74 HashPreScanSizeAccounting.cpp does not yet own pre-scan size accounting.");
            AssertContains(hashPreScanSizeAccounting, "fSizes[fileIndex] = fSize;", "Phase 74 HashPreScanSizeAccounting.cpp does not yet preserve pre-scan size cache writes.");
            AssertContains(hashPreScanSizeAccounting, "AddHashExecutionTotalSize(*executionContext, fSize);", "Phase 74 HashPreScanSizeAccounting.cpp does not yet preserve pre-scan total-size accumulation.");
            AssertContains(hashEnginePreparation, "TrackHashPreScannedFileSize(executionContext, fSizes, fileIndex, fSize);", "Phase 74 HashEnginePreparation.cpp does not yet delegate pre-scan size accounting.");
            AssertDoesNotContain(hashEnginePreparation, "fSizes[fileIndex] = fSize;", "Phase 74 HashEnginePreparation.cpp should no longer own pre-scan cache writes.");
            AssertDoesNotContain(hashEnginePreparation, "AddHashExecutionTotalSize(*executionContext, fSize);", "Phase 74 HashEnginePreparation.cpp should no longer own pre-scan total-size accumulation.");
            AssertContains(hashEngineInternal, "#include \"Common/HashPreScanSizeAccounting.h\"", "Phase 74 HashEngineInternal.h does not yet consume HashPreScanSizeAccounting.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashPreScanSizeAccounting.cpp", "Phase 74 desktop native core project does not yet compile HashPreScanSizeAccounting.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashPreScanSizeAccounting.h", "Phase 74 desktop native core project does not yet include HashPreScanSizeAccounting.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashPreScanSizeAccounting.cpp", "Phase 74 desktop native core filters do not yet expose HashPreScanSizeAccounting.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashPreScanSizeAccounting.h", "Phase 74 desktop native core filters do not yet expose HashPreScanSizeAccounting.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashPreScanSizeAccounting.cpp", "Phase 74 UWP native project does not yet compile HashPreScanSizeAccounting.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashPreScanSizeAccounting.h", "Phase 74 UWP native project does not yet include HashPreScanSizeAccounting.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashPreScanSizeAccounting.cpp", "Phase 74 UWP native filters do not yet expose HashPreScanSizeAccounting.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashPreScanSizeAccounting.h", "Phase 74 UWP native filters do not yet expose HashPreScanSizeAccounting.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashPreScanSizeAccounting.cpp", "Phase 74 WinUI native project should keep consuming the shared native core instead of compiling HashPreScanSizeAccounting.cpp directly.");
        }, failures);

        Run("Phase 75 promotes pre-scan visit and cancellation flow into a dedicated pre-scan workflow seam", () =>
        {
            string hashEnginePreparation = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp");
            string hashPreScanWorkflow = ReadRepoFile(repoRoot, @"trunk\source\Common\HashPreScanWorkflow.cpp");
            string hashPreScanWorkflowHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashPreScanWorkflow.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashPreScanWorkflowHeader, "void RunHashPreScanVisitWorkflow(HashExecutionContext *executionContext, const HashRequest& request, ULLongVector& fSizes, bool *wasCancelled);", "Phase 75 HashPreScanWorkflow.h does not yet expose pre-scan visit workflow orchestration.");
            AssertContains(hashPreScanWorkflow, "void RunHashPreScanVisitWorkflow(HashExecutionContext *executionContext, const HashRequest& request, ULLongVector& fSizes, bool *wasCancelled)", "Phase 75 HashPreScanWorkflow.cpp does not yet own pre-scan visit workflow orchestration.");
            AssertContains(hashPreScanWorkflow, "VisitHashRequestFiles(request, [&](uint32_t fileIndex, const tstring& fullPath)", "Phase 75 HashPreScanWorkflow.cpp does not yet preserve pre-scan request traversal.");
            AssertContains(hashPreScanWorkflow, "if (ShouldStopHashExecution(*executionContext))", "Phase 75 HashPreScanWorkflow.cpp does not yet preserve pre-scan cancellation checks.");
            AssertContains(hashPreScanWorkflow, "*wasCancelled = true;", "Phase 75 HashPreScanWorkflow.cpp does not yet preserve pre-scan cancellation signaling.");
            AssertContains(hashPreScanWorkflow, "AccumulatePreScannedFileSize(executionContext, request, fSizes, fileIndex);", "Phase 75 HashPreScanWorkflow.cpp does not yet preserve pre-scan accumulation dispatch.");
            AssertContains(hashEnginePreparation, "RunHashPreScanVisitWorkflow(executionContext, request, fSizes, wasCancelled);", "Phase 75 HashEnginePreparation.cpp does not yet delegate pre-scan traversal workflow.");
            AssertDoesNotContain(hashEnginePreparation, "VisitHashRequestFiles(request, [&](uint32_t fileIndex, const tstring& fullPath)", "Phase 75 HashEnginePreparation.cpp should no longer inline pre-scan visit traversal.");
            AssertContains(hashEngineInternal, "#include \"Common/HashPreScanWorkflow.h\"", "Phase 75 HashEngineInternal.h does not yet consume HashPreScanWorkflow.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashPreScanWorkflow.cpp", "Phase 75 desktop native core project does not yet compile HashPreScanWorkflow.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashPreScanWorkflow.h", "Phase 75 desktop native core project does not yet include HashPreScanWorkflow.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashPreScanWorkflow.cpp", "Phase 75 desktop native core filters do not yet expose HashPreScanWorkflow.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashPreScanWorkflow.h", "Phase 75 desktop native core filters do not yet expose HashPreScanWorkflow.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashPreScanWorkflow.cpp", "Phase 75 UWP native project does not yet compile HashPreScanWorkflow.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashPreScanWorkflow.h", "Phase 75 UWP native project does not yet include HashPreScanWorkflow.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashPreScanWorkflow.cpp", "Phase 75 UWP native filters do not yet expose HashPreScanWorkflow.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashPreScanWorkflow.h", "Phase 75 UWP native filters do not yet expose HashPreScanWorkflow.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashPreScanWorkflow.cpp", "Phase 75 WinUI native project should keep consuming the shared native core instead of compiling HashPreScanWorkflow.cpp directly.");
        }, failures);

        Run("Phase 76 promotes preparation lifecycle orchestration into a dedicated preparation-workflow seam", () =>
        {
            string hashEnginePreparation = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp");
            string hashPreparationWorkflow = ReadRepoFile(repoRoot, @"trunk\source\Common\HashPreparationWorkflow.cpp");
            string hashPreparationWorkflowHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashPreparationWorkflow.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashPreparationWorkflowHeader, "bool ExecuteHashPreparationWorkflow(HashExecutionContext *executionContext, const HashRequest& request, const HashPreparationPlan& preparationPlan, ULLongVector& fSizes, bool *wasCancelled);", "Phase 76 HashPreparationWorkflow.h does not yet expose preparation lifecycle orchestration.");
            AssertContains(hashPreparationWorkflow, "bool ExecuteHashPreparationWorkflow(HashExecutionContext *executionContext, const HashRequest& request, const HashPreparationPlan& preparationPlan, ULLongVector& fSizes, bool *wasCancelled)", "Phase 76 HashPreparationWorkflow.cpp does not yet own preparation lifecycle orchestration.");
            AssertContains(hashPreparationWorkflow, "observer->onProgressEvent(CreatePreparingProgressEvent());", "Phase 76 HashPreparationWorkflow.cpp does not yet preserve preparation-start publication.");
            AssertContains(hashPreparationWorkflow, "bool isSizeCaled = TryPreScanSmallBatchFileSizes(executionContext, request, preparationPlan, fSizes, wasCancelled);", "Phase 76 HashPreparationWorkflow.cpp does not yet preserve pre-scan dispatch.");
            AssertContains(hashPreparationWorkflow, "if (*wasCancelled)", "Phase 76 HashPreparationWorkflow.cpp does not yet preserve cancellation short-circuiting.");
            AssertContains(hashPreparationWorkflow, "observer->onProgressEvent(CreatePreparationFinishedProgressEvent());", "Phase 76 HashPreparationWorkflow.cpp does not yet preserve preparation-finished publication.");
            AssertContains(hashEnginePreparation, "return ExecuteHashPreparationWorkflow(executionContext, request, preparationPlan, fSizes, wasCancelled);", "Phase 76 HashEnginePreparation.cpp does not yet delegate preparation lifecycle orchestration.");
            AssertDoesNotContain(hashEnginePreparation, "observer->onProgressEvent(CreatePreparingProgressEvent());", "Phase 76 HashEnginePreparation.cpp should no longer inline preparation-start publication.");
            AssertDoesNotContain(hashEnginePreparation, "observer->onProgressEvent(CreatePreparationFinishedProgressEvent());", "Phase 76 HashEnginePreparation.cpp should no longer inline preparation-finished publication.");
            AssertContains(hashEngineInternal, "#include \"Common/HashPreparationWorkflow.h\"", "Phase 76 HashEngineInternal.h does not yet consume HashPreparationWorkflow.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashPreparationWorkflow.cpp", "Phase 76 desktop native core project does not yet compile HashPreparationWorkflow.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashPreparationWorkflow.h", "Phase 76 desktop native core project does not yet include HashPreparationWorkflow.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashPreparationWorkflow.cpp", "Phase 76 desktop native core filters do not yet expose HashPreparationWorkflow.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashPreparationWorkflow.h", "Phase 76 desktop native core filters do not yet expose HashPreparationWorkflow.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashPreparationWorkflow.cpp", "Phase 76 UWP native project does not yet compile HashPreparationWorkflow.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashPreparationWorkflow.h", "Phase 76 UWP native project does not yet include HashPreparationWorkflow.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashPreparationWorkflow.cpp", "Phase 76 UWP native filters do not yet expose HashPreparationWorkflow.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashPreparationWorkflow.h", "Phase 76 UWP native filters do not yet expose HashPreparationWorkflow.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashPreparationWorkflow.cpp", "Phase 76 WinUI native project should keep consuming the shared native core instead of compiling HashPreparationWorkflow.cpp directly.");
        }, failures);

        Run("Phase 77 promotes file-result startup into a dedicated file-result workflow seam", () =>
        {
            string hashEnginePreparation = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp");
            string hashFileResultWorkflow = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileResultWorkflow.cpp");
            string hashFileResultWorkflowHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileResultWorkflow.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashFileResultWorkflowHeader, "void PublishFilePathResult(HashExecutionContext *executionContext, HashResult& result);", "Phase 77 HashFileResultWorkflow.h does not yet expose path-result publication.");
            AssertContains(hashFileResultWorkflowHeader, "HashResult& ExecuteFileResultBeginWorkflow(HashExecutionContext *executionContext, const sunjwbase::tstring& path);", "Phase 77 HashFileResultWorkflow.h does not yet expose file-result begin workflow.");
            AssertContains(hashFileResultWorkflowHeader, "HashResult& ExecuteFileHashAttemptBeginWorkflow(HashExecutionContext *executionContext, const sunjwbase::tstring& path, FileExecutionState *executionState, const TCHAR **resultPath);", "Phase 77 HashFileResultWorkflow.h does not yet expose file-attempt begin workflow.");
            AssertContains(hashFileResultWorkflow, "void PublishFilePathResult(HashExecutionContext *executionContext, HashResult& result)", "Phase 77 HashFileResultWorkflow.cpp does not yet own path-result publication.");
            AssertContains(hashFileResultWorkflow, "HashResult& ExecuteFileResultBeginWorkflow(HashExecutionContext *executionContext, const sunjwbase::tstring& path)", "Phase 77 HashFileResultWorkflow.cpp does not yet own file-result begin workflow.");
            AssertContains(hashFileResultWorkflow, "HashResult& ExecuteFileHashAttemptBeginWorkflow(HashExecutionContext *executionContext, const sunjwbase::tstring& path, FileExecutionState *executionState, const TCHAR **resultPath)", "Phase 77 HashFileResultWorkflow.cpp does not yet own file-attempt begin workflow.");
            AssertContains(hashFileResultWorkflow, "AppendHashExecutionResult(*executionContext)", "Phase 77 HashFileResultWorkflow.cpp does not yet preserve result-list append flow.");
            AssertContains(hashFileResultWorkflow, "EmitPathResult(executionContext, result);", "Phase 77 HashFileResultWorkflow.cpp does not yet preserve path-result emission.");
            AssertContains(hashFileResultWorkflow, "ResetFileProgressState(&executionState->progressState);", "Phase 77 HashFileResultWorkflow.cpp does not yet preserve progress-state reset.");

            AssertContains(hashEnginePreparation, "PublishFilePathResult(executionContext, result);", "Phase 77 HashEnginePreparation.cpp does not yet delegate path-result publication.");
            AssertContains(hashEnginePreparation, "return ExecuteFileResultBeginWorkflow(executionContext, path);", "Phase 77 HashEnginePreparation.cpp does not yet delegate file-result begin workflow.");
            AssertContains(hashEnginePreparation, "return ExecuteFileHashAttemptBeginWorkflow(executionContext, path, executionState, resultPath);", "Phase 77 HashEnginePreparation.cpp does not yet delegate file-attempt begin workflow.");
            AssertDoesNotContain(hashEnginePreparation, "AppendHashExecutionResult(*executionContext)", "Phase 77 HashEnginePreparation.cpp should no longer inline result-list append flow.");
            AssertDoesNotContain(hashEnginePreparation, "ResetFileProgressState(&executionState->progressState);", "Phase 77 HashEnginePreparation.cpp should no longer inline progress reset flow.");
            AssertContains(hashEngineInternal, "#include \"Common/HashFileResultWorkflow.h\"", "Phase 77 HashEngineInternal.h does not yet consume HashFileResultWorkflow.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileResultWorkflow.cpp", "Phase 77 desktop native core project does not yet compile HashFileResultWorkflow.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileResultWorkflow.h", "Phase 77 desktop native core project does not yet include HashFileResultWorkflow.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileResultWorkflow.cpp", "Phase 77 desktop native core filters do not yet expose HashFileResultWorkflow.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileResultWorkflow.h", "Phase 77 desktop native core filters do not yet expose HashFileResultWorkflow.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileResultWorkflow.cpp", "Phase 77 UWP native project does not yet compile HashFileResultWorkflow.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileResultWorkflow.h", "Phase 77 UWP native project does not yet include HashFileResultWorkflow.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashFileResultWorkflow.cpp", "Phase 77 UWP native filters do not yet expose HashFileResultWorkflow.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashFileResultWorkflow.h", "Phase 77 UWP native filters do not yet expose HashFileResultWorkflow.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashFileResultWorkflow.cpp", "Phase 77 WinUI native project should keep consuming the shared native core instead of compiling HashFileResultWorkflow.cpp directly.");
        }, failures);

        Run("Phase 78 promotes file-attempt completion branching into a dedicated completion-workflow seam", () =>
        {
            string hashResultPublisher = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp");
            string hashFileAttemptCompletionWorkflow = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptCompletionWorkflow.cpp");
            string hashFileAttemptCompletionWorkflowHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptCompletionWorkflow.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashFileAttemptCompletionWorkflowHeader, "void ExecuteOpenedFileAttemptCompletionWorkflow(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,", "Phase 78 HashFileAttemptCompletionWorkflow.h does not yet expose opened-file completion workflow.");
            AssertContains(hashFileAttemptCompletionWorkflowHeader, "void ExecuteFileAttemptCompletionWorkflow(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,", "Phase 78 HashFileAttemptCompletionWorkflow.h does not yet expose file-attempt completion workflow.");
            AssertContains(hashFileAttemptCompletionWorkflow, "void ExecuteOpenedFileAttemptCompletionWorkflow(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,", "Phase 78 HashFileAttemptCompletionWorkflow.cpp does not yet own opened-file completion branching.");
            AssertContains(hashFileAttemptCompletionWorkflow, "if (executionState.fileAttemptState.readFailed)", "Phase 78 HashFileAttemptCompletionWorkflow.cpp does not yet preserve read-failed branching.");
            AssertContains(hashFileAttemptCompletionWorkflow, "CompleteSuccessfulFileHashing(executionContext, request, result, fileIndex, isSizeCaled, executionState);", "Phase 78 HashFileAttemptCompletionWorkflow.cpp does not yet preserve successful completion dispatch.");
            AssertContains(hashFileAttemptCompletionWorkflow, "FinishFileProcessing(executionContext);", "Phase 78 HashFileAttemptCompletionWorkflow.cpp does not yet preserve file-finished dispatch.");
            AssertContains(hashFileAttemptCompletionWorkflow, "void ExecuteFileAttemptCompletionWorkflow(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,", "Phase 78 HashFileAttemptCompletionWorkflow.cpp does not yet own file-attempt completion branching.");
            AssertContains(hashFileAttemptCompletionWorkflow, "if (executionState.fileAttemptState.isFileOpened)", "Phase 78 HashFileAttemptCompletionWorkflow.cpp does not yet preserve open-file branching.");
            AssertContains(hashFileAttemptCompletionWorkflow, "CompleteOpenedFileAttempt(executionContext, request, result, fileIndex, isSizeCaled, executionState);", "Phase 78 HashFileAttemptCompletionWorkflow.cpp does not yet preserve opened-file completion dispatch.");
            AssertContains(hashFileAttemptCompletionWorkflow, "EmitOpenFileError(executionContext, result, executionState.fileAttemptState.openErrorText);", "Phase 78 HashFileAttemptCompletionWorkflow.cpp does not yet preserve open-file error dispatch.");

            AssertContains(hashResultPublisher, "ExecuteOpenedFileAttemptCompletionWorkflow(executionContext, request, result, fileIndex, isSizeCaled, executionState);", "Phase 78 HashResultPublisher.cpp does not yet delegate opened-file completion branching.");
            AssertContains(hashResultPublisher, "ExecuteFileAttemptCompletionWorkflow(executionContext, request, result, fileIndex, isSizeCaled, executionState);", "Phase 78 HashResultPublisher.cpp does not yet delegate file-attempt completion branching.");
            AssertDoesNotContain(hashResultPublisher, "if (executionState.fileAttemptState.readFailed)", "Phase 78 HashResultPublisher.cpp should no longer inline read-failed branching.");
            AssertDoesNotContain(hashResultPublisher, "if (executionState.fileAttemptState.isFileOpened)", "Phase 78 HashResultPublisher.cpp should no longer inline opened-file branching.");
            AssertContains(hashEngineInternal, "#include \"Common/HashFileAttemptCompletionWorkflow.h\"", "Phase 78 HashEngineInternal.h does not yet consume HashFileAttemptCompletionWorkflow.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileAttemptCompletionWorkflow.cpp", "Phase 78 desktop native core project does not yet compile HashFileAttemptCompletionWorkflow.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashFileAttemptCompletionWorkflow.h", "Phase 78 desktop native core project does not yet include HashFileAttemptCompletionWorkflow.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileAttemptCompletionWorkflow.cpp", "Phase 78 desktop native core filters do not yet expose HashFileAttemptCompletionWorkflow.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashFileAttemptCompletionWorkflow.h", "Phase 78 desktop native core filters do not yet expose HashFileAttemptCompletionWorkflow.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileAttemptCompletionWorkflow.cpp", "Phase 78 UWP native project does not yet compile HashFileAttemptCompletionWorkflow.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashFileAttemptCompletionWorkflow.h", "Phase 78 UWP native project does not yet include HashFileAttemptCompletionWorkflow.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashFileAttemptCompletionWorkflow.cpp", "Phase 78 UWP native filters do not yet expose HashFileAttemptCompletionWorkflow.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashFileAttemptCompletionWorkflow.h", "Phase 78 UWP native filters do not yet expose HashFileAttemptCompletionWorkflow.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashFileAttemptCompletionWorkflow.cpp", "Phase 78 WinUI native project should keep consuming the shared native core instead of compiling HashFileAttemptCompletionWorkflow.cpp directly.");
        }, failures);

        Run("Phase 79 promotes successful-file completion into a dedicated successful-completion workflow seam", () =>
        {
            string hashResultPublisher = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp");
            string hashSuccessfulFileCompletionWorkflow = ReadRepoFile(repoRoot, @"trunk\source\Common\HashSuccessfulFileCompletionWorkflow.cpp");
            string hashSuccessfulFileCompletionWorkflowHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashSuccessfulFileCompletionWorkflow.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashSuccessfulFileCompletionWorkflowHeader, "void PublishWholeProgressAfterFile(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, uint32_t fileIndex);", "Phase 79 HashSuccessfulFileCompletionWorkflow.h does not yet expose whole-progress publication.");
            AssertContains(hashSuccessfulFileCompletionWorkflowHeader, "void ExecuteSuccessfulFileHashingWorkflow(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,", "Phase 79 HashSuccessfulFileCompletionWorkflow.h does not yet expose successful-file completion workflow.");
            AssertContains(hashSuccessfulFileCompletionWorkflow, "void PublishWholeProgressAfterFile(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, uint32_t fileIndex)", "Phase 79 HashSuccessfulFileCompletionWorkflow.cpp does not yet own whole-progress publication.");
            AssertContains(hashSuccessfulFileCompletionWorkflow, "void ExecuteSuccessfulFileHashingWorkflow(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,", "Phase 79 HashSuccessfulFileCompletionWorkflow.cpp does not yet own successful-file completion workflow.");
            AssertContains(hashSuccessfulFileCompletionWorkflow, "observer->onProgressEvent(CreateFileCalculatedProgressEvent());", "Phase 79 HashSuccessfulFileCompletionWorkflow.cpp does not yet preserve file-calculated publication.");
            AssertContains(hashSuccessfulFileCompletionWorkflow, "FinalizeDigestStrings(request, executionState.hashContexts, executionState.digestBundle, &finalizeErrorText)", "Phase 79 HashSuccessfulFileCompletionWorkflow.cpp does not yet preserve digest finalization.");
            AssertContains(hashSuccessfulFileCompletionWorkflow, "EmitErrorMessageResult(executionContext, result, finalizeErrorText);", "Phase 79 HashSuccessfulFileCompletionWorkflow.cpp does not yet surface digest-finalization failures.");
            AssertContains(hashSuccessfulFileCompletionWorkflow, "UpdateWholeProgressAfterFile(executionContext, request, isSizeCaled, fileIndex);", "Phase 79 HashSuccessfulFileCompletionWorkflow.cpp does not yet preserve whole-progress dispatch.");
            AssertContains(hashSuccessfulFileCompletionWorkflow, "executionState.fileAttemptState.osFile->close();", "Phase 79 HashSuccessfulFileCompletionWorkflow.cpp does not yet preserve file-close sequencing.");
            AssertContains(hashSuccessfulFileCompletionWorkflow, "PopulateDigestResult(request, result, executionState.digestBundle);", "Phase 79 HashSuccessfulFileCompletionWorkflow.cpp does not yet preserve digest projection.");
            AssertContains(hashSuccessfulFileCompletionWorkflow, "EmitHashResult(executionContext, result, GetHashRequestUppercaseDigest(request));", "Phase 79 HashSuccessfulFileCompletionWorkflow.cpp does not yet preserve hash-result publication.");

            AssertContains(hashResultPublisher, "PublishWholeProgressAfterFile(executionContext, request, isSizeCaled, fileIndex);", "Phase 79 HashResultPublisher.cpp does not yet delegate whole-progress publication.");
            AssertContains(hashResultPublisher, "ExecuteSuccessfulFileHashingWorkflow(executionContext, request, result, fileIndex, isSizeCaled, executionState);", "Phase 79 HashResultPublisher.cpp does not yet delegate successful-file completion workflow.");
            AssertDoesNotContain(hashResultPublisher, "observer->onProgressEvent(CreateFileCalculatedProgressEvent());", "Phase 79 HashResultPublisher.cpp should no longer inline file-calculated publication.");
            AssertDoesNotContain(hashResultPublisher, "FinalizeDigestStrings(request, executionState.hashContexts, executionState.digestBundle, &finalizeErrorText)", "Phase 79 HashResultPublisher.cpp should no longer inline digest finalization.");
            AssertDoesNotContain(hashResultPublisher, "PopulateDigestResult(request, result, executionState.digestBundle);", "Phase 79 HashResultPublisher.cpp should no longer inline digest projection.");
            AssertContains(hashEngineInternal, "#include \"Common/HashSuccessfulFileCompletionWorkflow.h\"", "Phase 79 HashEngineInternal.h does not yet consume HashSuccessfulFileCompletionWorkflow.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashSuccessfulFileCompletionWorkflow.cpp", "Phase 79 desktop native core project does not yet compile HashSuccessfulFileCompletionWorkflow.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashSuccessfulFileCompletionWorkflow.h", "Phase 79 desktop native core project does not yet include HashSuccessfulFileCompletionWorkflow.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashSuccessfulFileCompletionWorkflow.cpp", "Phase 79 desktop native core filters do not yet expose HashSuccessfulFileCompletionWorkflow.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashSuccessfulFileCompletionWorkflow.h", "Phase 79 desktop native core filters do not yet expose HashSuccessfulFileCompletionWorkflow.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashSuccessfulFileCompletionWorkflow.cpp", "Phase 79 UWP native project does not yet compile HashSuccessfulFileCompletionWorkflow.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashSuccessfulFileCompletionWorkflow.h", "Phase 79 UWP native project does not yet include HashSuccessfulFileCompletionWorkflow.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashSuccessfulFileCompletionWorkflow.cpp", "Phase 79 UWP native filters do not yet expose HashSuccessfulFileCompletionWorkflow.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashSuccessfulFileCompletionWorkflow.h", "Phase 79 UWP native filters do not yet expose HashSuccessfulFileCompletionWorkflow.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashSuccessfulFileCompletionWorkflow.cpp", "Phase 79 WinUI native project should keep consuming the shared native core instead of compiling HashSuccessfulFileCompletionWorkflow.cpp directly.");
        }, failures);

        Run("Phase 80 extracts error-message result emission into a dedicated error-result workflow seam", () =>
        {
            string hashResultPublisher = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp");
            string hashErrorResultWorkflow = ReadRepoFile(repoRoot, @"trunk\source\Common\HashErrorResultWorkflow.cpp");
            string hashErrorResultWorkflowHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashErrorResultWorkflow.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashErrorResultWorkflowHeader, "void PublishErrorMessageResult(HashExecutionContext *executionContext, HashResult& result, const sunjwbase::tstring& errorText);", "Phase 80 HashErrorResultWorkflow.h does not yet expose error-message publication.");
            AssertContains(hashErrorResultWorkflow, "void PublishErrorMessageResult(HashExecutionContext *executionContext, HashResult& result, const sunjwbase::tstring& errorText)", "Phase 80 HashErrorResultWorkflow.cpp does not yet own error-message publication.");
            AssertContains(hashErrorResultWorkflow, "result.error = errorText;", "Phase 80 HashErrorResultWorkflow.cpp does not yet preserve error message assignment.");
            AssertContains(hashErrorResultWorkflow, "EmitErrorResult(executionContext, result);", "Phase 80 HashErrorResultWorkflow.cpp does not yet preserve failed-result publication.");

            AssertContains(hashResultPublisher, "PublishErrorMessageResult(executionContext, result, errorText);", "Phase 80 HashResultPublisher.cpp does not yet delegate error-message publication.");
            AssertContains(hashResultPublisher, "EmitErrorMessageResult(executionContext, result, sunjwbase::tstring(errorText));", "Phase 80 HashResultPublisher.cpp does not yet route open-file errors through the shared error-message helper.");
            AssertContains(hashResultPublisher, "EmitErrorMessageResult(executionContext, result, sunjwbase::strtotstr(std::string(\"Failed to read file while hashing.\")));", "Phase 80 HashResultPublisher.cpp does not yet route read-file errors through the shared error-message helper.");
            AssertDoesNotContain(hashResultPublisher, "result.error = errorText;", "Phase 80 HashResultPublisher.cpp should no longer inline error assignment.");
            AssertContains(hashEngineInternal, "#include \"Common/HashErrorResultWorkflow.h\"", "Phase 80 HashEngineInternal.h does not yet consume HashErrorResultWorkflow.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashErrorResultWorkflow.cpp", "Phase 80 desktop native core project does not yet compile HashErrorResultWorkflow.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashErrorResultWorkflow.h", "Phase 80 desktop native core project does not yet include HashErrorResultWorkflow.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashErrorResultWorkflow.cpp", "Phase 80 desktop native core filters do not yet expose HashErrorResultWorkflow.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashErrorResultWorkflow.h", "Phase 80 desktop native core filters do not yet expose HashErrorResultWorkflow.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashErrorResultWorkflow.cpp", "Phase 80 UWP native project does not yet compile HashErrorResultWorkflow.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashErrorResultWorkflow.h", "Phase 80 UWP native project does not yet include HashErrorResultWorkflow.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashErrorResultWorkflow.cpp", "Phase 80 UWP native filters do not yet expose HashErrorResultWorkflow.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashErrorResultWorkflow.h", "Phase 80 UWP native filters do not yet expose HashErrorResultWorkflow.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashErrorResultWorkflow.cpp", "Phase 80 WinUI native project should keep consuming the shared native core instead of compiling HashErrorResultWorkflow.cpp directly.");
        }, failures);

        Run("Phase 81 extracts semantic result-event publication into a dedicated result-event workflow seam", () =>
        {
            string hashResultPublisher = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp");
            string hashResultEventWorkflow = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultEventWorkflow.cpp");
            string hashResultEventWorkflowHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultEventWorkflow.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashResultEventWorkflowHeader, "void PublishMetaResultEvent(HashExecutionContext *executionContext, HashResult& result);", "Phase 81 HashResultEventWorkflow.h does not yet expose meta-result publication.");
            AssertContains(hashResultEventWorkflowHeader, "void PublishHashResultEvent(HashExecutionContext *executionContext, HashResult& result, bool uppercase);", "Phase 81 HashResultEventWorkflow.h does not yet expose hash-result publication.");
            AssertContains(hashResultEventWorkflowHeader, "void PublishErrorResultEvent(HashExecutionContext *executionContext, HashResult& result);", "Phase 81 HashResultEventWorkflow.h does not yet expose error-result publication.");
            AssertContains(hashResultEventWorkflowHeader, "void PublishFileFinishedEvent(HashExecutionContext *executionContext);", "Phase 81 HashResultEventWorkflow.h does not yet expose file-finished publication.");
            AssertContains(hashResultEventWorkflow, "void PublishMetaResultEvent(HashExecutionContext *executionContext, HashResult& result)", "Phase 81 HashResultEventWorkflow.cpp does not yet own meta-result publication.");
            AssertContains(hashResultEventWorkflow, "void PublishHashResultEvent(HashExecutionContext *executionContext, HashResult& result, bool uppercase)", "Phase 81 HashResultEventWorkflow.cpp does not yet own hash-result publication.");
            AssertContains(hashResultEventWorkflow, "void PublishErrorResultEvent(HashExecutionContext *executionContext, HashResult& result)", "Phase 81 HashResultEventWorkflow.cpp does not yet own error-result publication.");
            AssertContains(hashResultEventWorkflow, "void PublishFileFinishedEvent(HashExecutionContext *executionContext)", "Phase 81 HashResultEventWorkflow.cpp does not yet own file-finished publication.");
            AssertContains(hashResultEventWorkflow, "result.state = RESULT_META;", "Phase 81 HashResultEventWorkflow.cpp does not yet preserve meta-result state publication.");
            AssertContains(hashResultEventWorkflow, "result.state = RESULT_ALL;", "Phase 81 HashResultEventWorkflow.cpp does not yet preserve hash-result state publication.");
            AssertContains(hashResultEventWorkflow, "result.state = RESULT_ERROR;", "Phase 81 HashResultEventWorkflow.cpp does not yet preserve error-result state publication.");
            AssertContains(hashResultEventWorkflow, "observer->onProgressEvent(CreateFileMetaReadyProgressEvent(result));", "Phase 81 HashResultEventWorkflow.cpp does not yet preserve meta-result event publication.");
            AssertContains(hashResultEventWorkflow, "observer->onProgressEvent(CreateFileHashReadyProgressEvent(result, uppercase));", "Phase 81 HashResultEventWorkflow.cpp does not yet preserve hash-result event publication.");
            AssertContains(hashResultEventWorkflow, "observer->onProgressEvent(CreateFileFailedProgressEvent(result));", "Phase 81 HashResultEventWorkflow.cpp does not yet preserve failed-result event publication.");
            AssertContains(hashResultEventWorkflow, "observer->onProgressEvent(CreateFileFinishedProgressEvent());", "Phase 81 HashResultEventWorkflow.cpp does not yet preserve file-finished event publication.");

            AssertContains(hashResultPublisher, "PublishMetaResultEvent(executionContext, result);", "Phase 81 HashResultPublisher.cpp does not yet delegate meta-result publication.");
            AssertContains(hashResultPublisher, "PublishHashResultEvent(executionContext, result, uppercase);", "Phase 81 HashResultPublisher.cpp does not yet delegate hash-result publication.");
            AssertContains(hashResultPublisher, "PublishErrorResultEvent(executionContext, result);", "Phase 81 HashResultPublisher.cpp does not yet delegate error-result publication.");
            AssertContains(hashResultPublisher, "PublishFileFinishedEvent(executionContext);", "Phase 81 HashResultPublisher.cpp does not yet delegate file-finished publication.");
            AssertDoesNotContain(hashResultPublisher, "observer->onProgressEvent(CreateFileMetaReadyProgressEvent(result));", "Phase 81 HashResultPublisher.cpp should no longer inline meta-result publication.");
            AssertDoesNotContain(hashResultPublisher, "observer->onProgressEvent(CreateFileHashReadyProgressEvent(result, uppercase));", "Phase 81 HashResultPublisher.cpp should no longer inline hash-result publication.");
            AssertDoesNotContain(hashResultPublisher, "observer->onProgressEvent(CreateFileFailedProgressEvent(result));", "Phase 81 HashResultPublisher.cpp should no longer inline failed-result publication.");
            AssertDoesNotContain(hashResultPublisher, "observer->onProgressEvent(CreateFileFinishedProgressEvent());", "Phase 81 HashResultPublisher.cpp should no longer inline file-finished publication.");
            AssertContains(hashEngineInternal, "#include \"Common/HashResultEventWorkflow.h\"", "Phase 81 HashEngineInternal.h does not yet consume HashResultEventWorkflow.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashResultEventWorkflow.cpp", "Phase 81 desktop native core project does not yet compile HashResultEventWorkflow.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashResultEventWorkflow.h", "Phase 81 desktop native core project does not yet include HashResultEventWorkflow.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashResultEventWorkflow.cpp", "Phase 81 desktop native core filters do not yet expose HashResultEventWorkflow.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashResultEventWorkflow.h", "Phase 81 desktop native core filters do not yet expose HashResultEventWorkflow.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashResultEventWorkflow.cpp", "Phase 81 UWP native project does not yet compile HashResultEventWorkflow.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashResultEventWorkflow.h", "Phase 81 UWP native project does not yet include HashResultEventWorkflow.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashResultEventWorkflow.cpp", "Phase 81 UWP native filters do not yet expose HashResultEventWorkflow.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashResultEventWorkflow.h", "Phase 81 UWP native filters do not yet expose HashResultEventWorkflow.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashResultEventWorkflow.cpp", "Phase 81 WinUI native project should keep consuming the shared native core instead of compiling HashResultEventWorkflow.cpp directly.");
        }, failures);

        Run("Phase 82 extracts hash-job terminal lifecycle publication into a dedicated job-lifecycle workflow seam", () =>
        {
            string hashEngine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string hashJobLifecycleWorkflow = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobLifecycleWorkflow.cpp");
            string hashJobLifecycleWorkflowHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobLifecycleWorkflow.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertContains(hashJobLifecycleWorkflowHeader, "void ExecuteCancelledHashingWorkflow(HashExecutionContext *executionContext, HashProgressSink *observer);", "Phase 82 HashJobLifecycleWorkflow.h does not yet expose cancellation lifecycle publication.");
            AssertContains(hashJobLifecycleWorkflowHeader, "void ExecuteCompletedHashingWorkflow(HashExecutionContext *executionContext, HashProgressSink *observer);", "Phase 82 HashJobLifecycleWorkflow.h does not yet expose completion lifecycle publication.");
            AssertContains(hashJobLifecycleWorkflow, "void ExecuteCancelledHashingWorkflow(HashExecutionContext *executionContext, HashProgressSink *observer)", "Phase 82 HashJobLifecycleWorkflow.cpp does not yet own cancellation lifecycle publication.");
            AssertContains(hashJobLifecycleWorkflow, "void ExecuteCompletedHashingWorkflow(HashExecutionContext *executionContext, HashProgressSink *observer)", "Phase 82 HashJobLifecycleWorkflow.cpp does not yet own completion lifecycle publication.");
            AssertContains(hashJobLifecycleWorkflow, "observer->onProgressEvent(CreateCancelledProgressEvent());", "Phase 82 HashJobLifecycleWorkflow.cpp does not yet preserve cancelled-event publication.");
            AssertContains(hashJobLifecycleWorkflow, "observer->onProgressEvent(CreateCompletedProgressEvent());", "Phase 82 HashJobLifecycleWorkflow.cpp does not yet preserve completed-event publication.");
            AssertContains(hashJobLifecycleWorkflow, "SetHashExecutionWorking(*executionContext, false);", "Phase 82 HashJobLifecycleWorkflow.cpp does not yet preserve job-working teardown.");

            AssertContains(hashEngine, "ExecuteCancelledHashingWorkflow(executionContext, observer);", "Phase 82 HashEngine.cpp does not yet delegate cancellation lifecycle publication.");
            AssertContains(hashEngine, "ExecuteCompletedHashingWorkflow(executionContext, observer);", "Phase 82 HashEngine.cpp does not yet delegate completion lifecycle publication.");
            AssertDoesNotContain(hashEngine, "observer->onProgressEvent(CreateCancelledProgressEvent());", "Phase 82 HashEngine.cpp should no longer inline cancelled-event publication.");
            AssertDoesNotContain(hashEngine, "observer->onProgressEvent(CreateCompletedProgressEvent());", "Phase 82 HashEngine.cpp should no longer inline completed-event publication.");
            AssertContains(hashEngineInternal, "#include \"Common/HashJobLifecycleWorkflow.h\"", "Phase 82 HashEngineInternal.h does not yet consume HashJobLifecycleWorkflow.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashJobLifecycleWorkflow.cpp", "Phase 82 desktop native core project does not yet compile HashJobLifecycleWorkflow.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashJobLifecycleWorkflow.h", "Phase 82 desktop native core project does not yet include HashJobLifecycleWorkflow.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashJobLifecycleWorkflow.cpp", "Phase 82 desktop native core filters do not yet expose HashJobLifecycleWorkflow.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashJobLifecycleWorkflow.h", "Phase 82 desktop native core filters do not yet expose HashJobLifecycleWorkflow.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashJobLifecycleWorkflow.cpp", "Phase 82 UWP native project does not yet compile HashJobLifecycleWorkflow.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashJobLifecycleWorkflow.h", "Phase 82 UWP native project does not yet include HashJobLifecycleWorkflow.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashJobLifecycleWorkflow.cpp", "Phase 82 UWP native filters do not yet expose HashJobLifecycleWorkflow.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashJobLifecycleWorkflow.h", "Phase 82 UWP native filters do not yet expose HashJobLifecycleWorkflow.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashJobLifecycleWorkflow.cpp", "Phase 82 WinUI native project should keep consuming the shared native core instead of compiling HashJobLifecycleWorkflow.cpp directly.");
        }, failures);

        Run("Phase 83 promotes digest dispatch to algorithm-list seams while keeping legacy compatibility paths", () =>
        {
            string hashEngine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");
            string hashDigestUpdater = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestUpdater.cpp");
            string hashDigestUpdaterHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestUpdater.h");
            string hashDigestContextOps = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestContextOps.cpp");
            string hashDigestContextOpsHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestContextOps.h");
            string hashDigestOperationRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestOperationRegistry.cpp");
            string hashDigestOperationRegistryHeader = ReadHashDigestOperationRegistrySeams(repoRoot);
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string hashDigestRuntimePlan = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestRuntimePlan.cpp");
            string hashDigestExecution = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp");
            string hashDigestQueue = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp");
            string hashDigestSinglePass = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestSinglePass.cpp");
            string hashJobExecutionPlan = ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.cpp");
            string nativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string nativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj.filters");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string uwpNativeFilters = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj.filters");
            string wuiNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWUINative\fHashWUINative.vcxproj");

            AssertDoesNotContain(hashDigestUpdaterHeader, "std::vector<ResultDigestType> algorithms;", "Phase 83 HashDigestUpdater.h still carries legacy algorithm-list state in digest update requests.");
            AssertContains(hashDigestUpdaterHeader, "std::vector<HashDigestOperationDescriptor> operationDescriptors;", "Phase 83 HashDigestUpdater.h does not yet preserve operation-descriptor state in digest update requests.");
            AssertDoesNotContain(hashDigestUpdaterHeader, "VisitDigestUpdateRequestAlgorithms(const DigestUpdateRequest& digestUpdateRequest", "Phase 83 HashDigestUpdater.h still exposes legacy digest-update algorithm iteration.");
            AssertContains(hashDigestUpdaterHeader, "VisitDigestUpdateRequestOperations(const DigestUpdateRequest& digestUpdateRequest", "Phase 83 HashDigestUpdater.h does not yet expose digest-update operation iteration.");
            AssertContains(hashDigestUpdater, "VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)", "Phase 83 HashDigestUpdater.cpp does not yet project request algorithms through the registry seam.");
            AssertContains(hashDigestUpdater, "const std::vector<HashAlgorithmId> normalizedAlgorithmIds = GetHashRequestNormalizedAlgorithmIds(request);", "Phase 83 HashDigestUpdater.cpp does not yet precompute normalized descriptor/id request algorithms.");
            AssertContains(hashDigestUpdater, "if (!IsRequestedDigestAlgorithmId(normalizedAlgorithmIds, algorithmId))", "Phase 83 HashDigestUpdater.cpp does not yet filter digest update plans by descriptor/id request algorithms.");
            AssertContains(hashDigestUpdater, "if (!IsHashDigestOperationRegistryConsistent())", "Phase 83 HashDigestUpdater.cpp does not yet gate digest update planning on registry consistency.");
            AssertContains(hashDigestUpdater, "TryResolveDigestUpdateOperationDescriptor(algorithmId, &operationDescriptor)", "Phase 83 HashDigestUpdater.cpp does not yet route operation resolution through the strict descriptor/id helper.");
            AssertContains(hashDigestUpdater, "TryGetHashDigestOperationDescriptorById(algorithmId, operationDescriptor)", "Phase 83 HashDigestUpdater.cpp does not yet resolve operations by descriptor/id.");
            AssertDoesNotContain(hashDigestUpdater, "digestUpdateRequest.algorithms.push_back(digestType);", "Phase 83 HashDigestUpdater.cpp should no longer persist legacy algorithm-list slots.");
            AssertContains(hashDigestUpdater, "digestUpdateRequest.operationDescriptors.push_back(operationDescriptor);", "Phase 83 HashDigestUpdater.cpp does not yet preserve request-driven digest operation descriptors.");
            AssertContains(hashDigestUpdater, "VisitDigestUpdateRequestOperations(digestUpdateRequest, [&](const HashDigestOperationDescriptor& operationDescriptor)", "Phase 83 HashDigestUpdater.cpp does not yet route updates through digest-update operation iteration.");
            AssertContains(hashDigestUpdater, "std::vector<std::future<void>> digestUpdateTasks;", "Phase 83 HashDigestUpdater.cpp does not yet preserve generic operation-task fan-out for algorithm-list updates.");
            AssertDoesNotContain(hashDigestUpdater, "DigestUpdateRequest digestUpdateRequest = { 0 };", "Phase 83 HashDigestUpdater.cpp still uses legacy scalar brace initialization that breaks std::vector-based digest requests on MSVC.");
            AssertDoesNotContain(hashDigestRuntimePlan, "static const DigestUpdateRequest emptyDigestUpdateRequest = { 0 };", "Phase 83 HashDigestRuntimePlan.cpp still uses legacy scalar brace initialization for digest requests on MSVC.");
            AssertDoesNotContain(hashDigestRuntimePlan, "static const HashDigestQueuePlan fallbackQueuePlan = { 1 };", "Phase 83 HashDigestRuntimePlan.cpp still uses legacy fallback queue plan initialization.");
            AssertDoesNotContain(hashEngine, "HashJobExecutionPlan executionPlan = { 0 };", "Phase 83 HashEngine.cpp still uses legacy scalar brace initialization for execution plans on MSVC.");

            AssertContainsAny(hashDigestContextOpsHeader,
                [
                    "void InitializeHashDigestContext(FileHashContexts *hashContexts, ResultDigestType digestType);",
                    "void InitializeHashDigestContextById(FileHashContexts *hashContexts, const HashAlgorithmId& algorithmId);"
                ],
                "Phase 83 HashDigestContextOps.h no longer exposes digest-context initialization seam.");
            AssertContainsAny(hashDigestContextOpsHeader,
                [
                    "bool FinalizeHashDigestContext(FileHashContexts& hashContexts, ResultDigestType digestType, ResultDigestStorage& digestBundle, sunjwbase::tstring *errorText);",
                    "bool FinalizeHashDigestContextById(FileHashContexts& hashContexts, const HashAlgorithmId& algorithmId, ResultDigestStorage& digestBundle, sunjwbase::tstring *errorText);"
                ],
                "Phase 83 HashDigestContextOps.h no longer exposes digest-context finalization seam.");
            AssertContainsAny(hashDigestContextOps,
                [
                    "TryGetHashDigestOperationDescriptor(digestType, &operationDescriptor)",
                    "TryGetHashDigestOperationDescriptorById(algorithmId, &operationDescriptor)"
                ],
                "Phase 83 HashDigestContextOps.cpp does not yet route context operations through registry lookup.");
            AssertContains(hashDigestOperationRegistryHeader, "struct HashDigestOperationDescriptor", "Phase 83 HashDigestOperationRegistry.h does not yet expose digest operation descriptors.");
            AssertContains(hashDigestOperationRegistryHeader, "RegisterHashDigestOperationDescriptor(const HashDigestOperationDescriptor& operationDescriptor);", "Phase 83 HashDigestOperationRegistry.h does not yet expose operation-descriptor registration.");
            AssertContainsAny(hashDigestOperationRegistryHeader,
                [
                    "TryGetHashDigestOperationDescriptor(ResultDigestType digestType, HashDigestOperationDescriptor *operationDescriptor);",
                    "TryGetHashDigestOperationDescriptor(ResultDigestType digestType, HashDigestOperationDescriptor *operationDescriptor)"
                ],
                "Phase 83 HashDigestOperationRegistry.h does not yet expose digest-type descriptor lookup.");
            AssertContains(hashDigestOperationRegistryHeader, "TryGetHashDigestOperationDescriptorById(const HashAlgorithmId& algorithmId, HashDigestOperationDescriptor *operationDescriptor);", "Phase 83 HashDigestOperationRegistry.h does not yet expose descriptor/id lookup.");
            AssertContains(hashDigestOperationRegistry, "GetMutableHashDigestOperationDescriptorStorage()", "Phase 83 HashDigestOperationRegistry.cpp does not yet centralize operation descriptors behind dedicated storage.");
            AssertContains(hashDigestOperationRegistry, "RegisterHashDigestOperationDescriptorUnlocked({", "Phase 83 HashDigestOperationRegistry.cpp does not yet register default operation descriptors through the registration seam.");
            AssertContains(hashDigestOperationRegistry, "UpdateOpenSslSha256DigestContext", "Phase 83 HashDigestOperationRegistry.cpp does not yet expose SHA-256 update delegation through the OpenSSL provider.");
            AssertContains(hashEngineInternal, "#include \"Common/HashDigestOperationRegistry.h\"", "Phase 83 HashEngineInternal.h does not yet consume HashDigestOperationRegistry.");

            AssertContains(hashJobExecutionPlan, "executionPlan->digestUpdateRequest = CreateDigestUpdateRequest(request);", "Phase 83 HashJobExecutionPlan.cpp does not yet consume request-driven digest update plans.");
            AssertContains(hashDigestRuntimePlan, "GetHashJobDigestUpdateRequest(executionPlan),", "Phase 83 HashDigestRuntimePlan.cpp does not yet forward request-driven digest update plans into runtime execution.");
            AssertContains(hashDigestExecution, "const DigestUpdateRequest& digestUpdateRequest = GetHashDigestRuntimeUpdateRequest(digestRuntimePlan);", "Phase 83 HashDigestExecution.cpp does not yet consume digest update request plans.");
            AssertContains(hashDigestQueue, "UpdateDigestContextsParallel(digestUpdateRequest", "Phase 83 HashDigestQueue.cpp does not yet consume digest-update request plans in parallel mode.");
            AssertContains(hashDigestSinglePass, "UpdateDigestContextsSequential(digestUpdateRequest", "Phase 83 HashDigestSinglePass.cpp does not yet consume digest-update request plans in single-pass mode.");

            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestOperationRegistry.cpp", "Phase 83 desktop native core project does not yet compile HashDigestOperationRegistry.cpp.");
            AssertContains(nativeProject, @"..\..\trunk\source\Common\HashDigestOperationRegistry.h", "Phase 83 desktop native core project does not yet include HashDigestOperationRegistry.h.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestOperationRegistry.cpp", "Phase 83 desktop native core filters do not yet expose HashDigestOperationRegistry.cpp.");
            AssertContains(nativeFilters, @"..\..\trunk\source\Common\HashDigestOperationRegistry.h", "Phase 83 desktop native core filters do not yet expose HashDigestOperationRegistry.h.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestOperationRegistry.cpp", "Phase 83 UWP native project does not yet compile HashDigestOperationRegistry.cpp.");
            AssertContains(uwpNativeProject, @"..\..\trunk\source\Common\HashDigestOperationRegistry.h", "Phase 83 UWP native project does not yet include HashDigestOperationRegistry.h.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestOperationRegistry.cpp", "Phase 83 UWP native filters do not yet expose HashDigestOperationRegistry.cpp.");
            AssertContains(uwpNativeFilters, @"..\..\trunk\source\Common\HashDigestOperationRegistry.h", "Phase 83 UWP native filters do not yet expose HashDigestOperationRegistry.h.");
            AssertDoesNotContain(wuiNativeProject, @"..\..\trunk\source\Common\HashDigestOperationRegistry.cpp", "Phase 83 WinUI native project should keep consuming the shared native core instead of compiling HashDigestOperationRegistry.cpp directly.");
        }, failures);

        Run("Phase 84 hardens algorithm-registry lookup seams against implicit MD5 fallback and unknown digest leakage", () =>
        {
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string legacyDigestType = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ResultDigestTypeCompat.h");
            string hashAlgorithmRegistry = ReadHashAlgorithmRegistrySeams(repoRoot);
            string hashRequest = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashRequest.h");
            string legacyThreadExecutionAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataExecutionAccess.h");
            string digestMetadataAccess = ReadResultDigestAccessSeams(repoRoot);
            string digestStateAccess = ReadResultDigestAccessSeams(repoRoot);
            string resultNetProjection = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultNetProjection.h");

            AssertDoesNotContain(global, "RESULT_DIGEST_UNKNOWN = -1", "Phase 84 Global.h should no longer own the compatibility digest sentinel.");
            AssertContains(legacyDigestType, "RESULT_DIGEST_UNKNOWN = -1", "Phase 84 LegacyCompat does not yet expose the unknown digest sentinel.");
            AssertContains(global, "sunjwbase::tstring algorithmId;", "Phase 84 HashDigestResult does not yet expose descriptor/id identity storage.");
            AssertDoesNotContain(global, ": type(RESULT_DIGEST_UNKNOWN)", "Phase 84 HashDigestResult should not carry legacy digest-type default initialization.");

            AssertContains(hashAlgorithmRegistry, "GetUnknownHashAlgorithmDescriptor()", "Phase 84 HashAlgorithmRegistry does not yet expose the unknown descriptor fallback seam.");
            AssertContains(hashAlgorithmRegistry, "TryGetHashAlgorithmIndex(ResultDigestType digestType, int *algorithmIndex)", "Phase 84 HashAlgorithmRegistry does not yet expose safe algorithm-index lookup.");
        AssertContains(hashAlgorithmRegistry, "TryGetHashAlgorithmDescriptor(ResultDigestType digestType, HashAlgorithmDescriptor *algorithmDescriptor)", "Phase 84 HashAlgorithmRegistry does not yet expose safe descriptor lookup.");
            AssertDoesNotContain(hashAlgorithmRegistry, "return algorithmDescriptors[0];", "Phase 84 HashAlgorithmRegistry should no longer implicitly fall back to MD5 for invalid indices.");
            AssertContains(hashAlgorithmRegistry, "int algorithmIndex = -1;", "Phase 84 HashAlgorithmRegistry does not yet initialize unresolved algorithm index to -1.");

            AssertContains(hashRequest, "#include \"Domain/HashAlgorithmRegistryCore.h\"", "Phase 90 HashRequest should now consume the hash-algorithm registry seam directly from Domain.");
            AssertContains(hashRequest, "std::find(normalizedAlgorithmIds.begin(), normalizedAlgorithmIds.end(), normalizedAlgorithmId)", "Phase 84 HashRequest algorithm traversal does not yet deduplicate descriptor/id algorithm selections.");
            AssertContains(hashRequest, "if (!IsRegisteredHashAlgorithmId(normalizedAlgorithmId))", "Phase 84 HashRequest algorithm traversal does not yet ignore unregistered algorithms.");

            AssertFileMissing(repoRoot, @"trunk\source\Common\ThreadDataExecutionAccess.h", "Phase 84 Common ThreadDataExecutionAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertContains(legacyThreadExecutionAccess, "TryGetHashAlgorithmIndexById(algorithmId, &algorithmIndex)", "Phase 84 ThreadData execution access does not yet route selection lookup through the safe index seam.");
            AssertDoesNotContain(legacyThreadExecutionAccess, "enabled[GetHashAlgorithmIndex(digestType)]", "Phase 84 ThreadData execution access still indexes selection arrays through unsafe direct digest-index conversion.");

            AssertContains(digestMetadataAccess, "TryGetResultDigestIndex(ResultDigestType digestType, int *index)", "Phase 84 ResultDigestMetadataAccess does not yet expose safe digest-index lookup.");
        AssertContains(digestMetadataAccess, "TryGetResultDigestMetadata(ResultDigestType digestType, ResultDigestMetadata *digestMetadata)", "Phase 84 ResultDigestMetadataAccess does not yet expose safe digest-metadata lookup.");

            AssertContains(digestStateAccess, "TryResolveDigestStorageIndex(ResultDigestType digestType, size_t *digestIndex)", "Phase 84 ResultDigestStateAccess does not yet expose safe digest-storage index resolution.");
            AssertContains(digestStateAccess, "TryGetMutableDigestStorageValueById(ResultDigestStorage& digestStorage, const HashAlgorithmId& algorithmId, sunjwbase::tstring **digestValue)", "Phase 84 ResultDigestStateAccess does not yet expose explicit mutable digest-storage failure handling.");
            AssertDoesNotContain(digestStateAccess, "GetInvalidDigestStorageScratch()", "Phase 84 ResultDigestStateAccess still exposes inert scratch storage for invalid digest writes.");
            AssertContainsAny(digestStateAccess,
                [
                    "if (!TryResolveDigestStorageIndex(digestType, &digestIndex))",
                    "if (!TryResolveDigestStorageIndexById(algorithmId, &digestIndex))"
                ],
                "Phase 84 ResultDigestStateAccess does not yet guard digest storage lookups.");

            AssertContains(resultNetProjection, "TryGetHashAlgorithmDescriptorById(algorithmId, &algorithmDescriptor)", "Phase 84 ResultNetProjection does not yet guard stable-name mapping with safe descriptor-id lookup.");
            AssertDoesNotContain(resultNetProjection, "TryGetHashAlgorithmDescriptor(digestType, &algorithmDescriptor)", "Phase 84 ResultNetProjection still routes through the removed digest-type descriptor lookup.");
        }, failures);

        Run("Phase 85 aligns digest-operation registry coverage with algorithm metadata and request planning order", () =>
        {
            string hashDigestOperationRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestOperationRegistry.cpp");
            string hashDigestOperationRegistryHeader = ReadHashDigestOperationRegistrySeams(repoRoot);
            string hashDigestUpdater = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestUpdater.cpp");
            string nativeRuntimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
            string nativeRuntimeUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\NativeRuntimeFrameworkUnitTests.cs");

            AssertContains(hashDigestOperationRegistryHeader, "bool IsHashDigestOperationDescriptorComplete(const HashDigestOperationDescriptor& operationDescriptor);", "Phase 85 HashDigestOperationRegistry.h does not yet expose descriptor-completeness validation.");
            AssertContainsAny(hashDigestOperationRegistryHeader,
                [
                    "bool IsHashDigestOperationDescriptorSupported(ResultDigestType digestType);",
                    "static inline bool IsHashDigestOperationDescriptorSupported(ResultDigestType digestType)"
                ],
                "Phase 85 HashDigestOperationRegistry.h does not yet expose per-digest descriptor support checks.");
            AssertContains(hashDigestOperationRegistryHeader, "bool IsHashDigestOperationDescriptorSupportedById(const HashAlgorithmId& algorithmId);", "Phase 85 HashDigestOperationRegistry.h does not yet expose descriptor/id support checks.");
            AssertContains(hashDigestOperationRegistryHeader, "bool IsHashDigestOperationRegistryConsistent();", "Phase 85 HashDigestOperationRegistry.h does not yet expose registry-consistency checks.");
            AssertContains(hashDigestOperationRegistry, "if (operationDescriptor != NULL)", "Phase 85 HashDigestOperationRegistry.cpp does not yet tolerate null descriptor output pointers.");
            AssertContains(hashDigestOperationRegistry, "GetMutableHashDigestOperationDescriptorStorage()", "Phase 85 HashDigestOperationRegistry.cpp does not yet cache descriptor snapshots through dedicated operation storage.");
            AssertContains(hashDigestOperationRegistry, "RegisterHashDigestOperationDescriptor(const HashDigestOperationDescriptor& operationDescriptor)", "Phase 85 HashDigestOperationRegistry.cpp does not yet expose descriptor registration.");
            AssertContains(hashDigestOperationRegistry, "IsHashDigestOperationDescriptorComplete(operationDescriptor);", "Phase 85 HashDigestOperationRegistry.cpp does not yet validate descriptor completeness.");

            AssertContains(hashDigestUpdater, "VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& algorithmDescriptor)", "Phase 85 HashDigestUpdater.cpp does not yet iterate digest planning in registry order.");
            AssertContains(hashDigestUpdater, "const std::vector<HashAlgorithmId> normalizedAlgorithmIds = GetHashRequestNormalizedAlgorithmIds(request);", "Phase 85 HashDigestUpdater.cpp does not yet precompute normalized descriptor/id request algorithms.");
            AssertContains(hashDigestUpdater, "if (!IsRequestedDigestAlgorithmId(normalizedAlgorithmIds, algorithmId))", "Phase 85 HashDigestUpdater.cpp does not yet filter registry order by descriptor/id request algorithms.");
            AssertContains(hashDigestUpdater, "if (!IsHashDigestOperationRegistryConsistent())", "Phase 85 HashDigestUpdater.cpp does not yet gate plan creation on operation-registry consistency.");
            AssertContains(hashDigestUpdater, "TryResolveDigestUpdateOperationDescriptor(algorithmId, &operationDescriptor)", "Phase 85 HashDigestUpdater.cpp does not yet resolve operations through the strict descriptor/id helper.");

            AssertContains(nativeRuntimeSource, "HashDigestOperationRegistry_StaysConsistentWithAlgorithmRegistry", "Phase 85 native runtime tests do not yet cover registry consistency.");
            AssertContains(nativeRuntimeSource, "HashEngineInternal::IsHashDigestOperationRegistryConsistent()", "Phase 85 native runtime tests do not yet assert operation-registry consistency.");
            AssertContains(nativeRuntimeSource, "HashDigestOperationRegistry_BuildsDescriptorSnapshotFromAlgorithmRegistry", "Phase 85 native runtime tests do not yet cover descriptor snapshot generation from the algorithm registry.");
            AssertContains(nativeRuntimeSource, "HashDigestOperationRegistry_AllowsNullDescriptorProbeForKnownDigests", "Phase 85 native runtime tests do not yet cover null descriptor probes for digest operations.");
            AssertContains(nativeRuntimeSource, "HashDigestOperationRegistry_ValidatesDescriptorCompletenessAndUnknownSupport", "Phase 85 native runtime tests do not yet cover descriptor completeness and unknown digest support.");
            AssertContains(nativeRuntimeSource, "HashDigestUpdater_CreatesRegistryOrderedOperationsForSelectedAlgorithms", "Phase 85 native runtime tests do not yet cover registry-ordered digest updater planning.");
            AssertContains(nativeRuntimeUnitTests, "HashDigestOperationRegistry_StaysConsistentWithAlgorithmRegistry", "Phase 85 managed unit tests do not yet gate the new native runtime consistency scenario.");
            AssertContains(nativeRuntimeUnitTests, "HashDigestOperationRegistry_BuildsDescriptorSnapshotFromAlgorithmRegistry", "Phase 85 managed unit tests do not yet gate descriptor snapshot coverage.");
            AssertContains(nativeRuntimeUnitTests, "HashDigestUpdater_CreatesRegistryOrderedOperationsForSelectedAlgorithms", "Phase 85 managed unit tests do not yet gate registry-ordered digest updater runtime coverage.");
        }, failures);

        Run("Phase 86 derives digest-operation snapshots from the algorithm registry seam", () =>
        {
            string hashDigestOperationRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestOperationRegistry.cpp");
            string nativeRuntimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
            string nativeRuntimeUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\NativeRuntimeFrameworkUnitTests.cs");
            string hashContractUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\HashContractUnitTests.cs");

            AssertContains(hashDigestOperationRegistry, "GetMutableHashDigestOperationDescriptorStorage()", "Phase 86 HashDigestOperationRegistry.cpp does not yet expose dedicated descriptor storage.");
            AssertContains(hashDigestOperationRegistry, "RegisterHashDigestOperationDescriptor(const HashDigestOperationDescriptor& operationDescriptor)", "Phase 86 HashDigestOperationRegistry.cpp does not yet expose descriptor registration through a dedicated seam.");
            AssertContains(hashDigestOperationRegistry, "EnsureDefaultHashDigestOperationDescriptorsRegistered()", "Phase 86 HashDigestOperationRegistry.cpp does not yet bootstrap defaults through registration.");
            AssertContains(hashDigestOperationRegistry, "RegisterHashDigestOperationDescriptorUnlocked({", "Phase 86 HashDigestOperationRegistry.cpp does not yet materialize default descriptors through registration.");
            AssertContains(hashDigestOperationRegistry, "BuildRegistryOrderedHashDigestOperationDescriptorSnapshot(", "Phase 86 HashDigestOperationRegistry.cpp does not yet rebuild descriptor snapshots in algorithm-registry order.");
            AssertContains(hashDigestOperationRegistry, "GetRegisteredHashAlgorithmDescriptors()", "Phase 86 HashDigestOperationRegistry.cpp does not yet derive descriptor snapshots from the algorithm registry seam.");
            AssertDoesNotContain(hashDigestOperationRegistry, "static const HashDigestOperationDescriptor operationDescriptors[]", "Phase 86 HashDigestOperationRegistry.cpp still hardcodes a fixed operation-descriptor table.");

            AssertContains(nativeRuntimeSource, "HashDigestOperationRegistry_BuildsDescriptorSnapshotFromAlgorithmRegistry", "Phase 86 native runtime tests do not yet cover descriptor snapshot generation.");
            AssertContains(nativeRuntimeUnitTests, "HashDigestOperationRegistry_BuildsDescriptorSnapshotFromAlgorithmRegistry", "Phase 86 managed unit tests do not yet gate descriptor snapshot generation coverage.");
            AssertContains(hashContractUnitTests, "GetMutableHashDigestOperationDescriptorStorage()", "Phase 86 contract unit tests do not yet assert descriptor snapshots use dedicated registry storage.");
        }, failures);

        Run("Phase 87 isolates ThreadData into legacy seams and unifies native toolsets on v143", () =>
        {
            string winUiProject = ReadRepoFile(repoRoot, @"trunk\source\WinUI\fHashWUI.csproj");
            AssertContains(winUiProject, "<PackageReference Include=\"Microsoft.WindowsAppSDK\" Version=\"1.8.260317003\" />", "Phase 87 WinUI project does not yet pin to a stable WindowsAppSDK version.");
            AssertDoesNotContain(winUiProject, "2.0.0-experimental", "Phase 87 WinUI project still depends on experimental WindowsAppSDK packages.");

            string[] vcxProjects = Directory.GetFiles(repoRoot, "*.vcxproj", SearchOption.AllDirectories);
            if (vcxProjects.Length == 0)
            {
                failures.Add("Phase 87 could not find any vcxproj files to validate native toolset unification.");
            }

            for (int projectIndex = 0; projectIndex < vcxProjects.Length; ++projectIndex)
            {
                string projectPath = vcxProjects[projectIndex];
                string projectContents = File.ReadAllText(projectPath, Encoding.UTF8);
                string relativeProjectPath = Path.GetRelativePath(repoRoot, projectPath).Replace('/', '\\');

                if (projectContents.Contains("<PlatformToolset>v141</PlatformToolset>", StringComparison.Ordinal) ||
                    projectContents.Contains("<PlatformToolset>v145</PlatformToolset>", StringComparison.Ordinal))
                {
                    failures.Add($"Phase 87 native toolset unification failed: {relativeProjectPath} still uses a legacy PlatformToolset.");
                }

                if (!projectContents.Contains("<PlatformToolset>v143</PlatformToolset>", StringComparison.Ordinal))
                {
                    failures.Add($"Phase 87 native toolset unification failed: {relativeProjectPath} does not contain a v143 PlatformToolset entry.");
                }
            }

            AssertFileMissing(repoRoot, @"trunk\source\Common\ThreadDataAccess.h", "Phase 87 Common ThreadDataAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\ThreadDataExecutionAccess.h", "Phase 87 Common ThreadDataExecutionAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\ThreadDataInputAccess.h", "Phase 87 Common ThreadDataInputAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\ThreadDataResultAccess.h", "Phase 87 Common ThreadDataResultAccess shim should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\HashRequestProjection.h", "Phase 87 Common HashRequestProjection shim should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\HashThreadEntryProjection.h", "Phase 87 Common HashThreadEntryProjection shim should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\HashThreadEntry.h", "Phase 87 Common HashThreadEntry.h should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\HashThreadEntry.cpp", "Phase 87 Common HashThreadEntry.cpp should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\HashThreadLaunch.h", "Phase 87 Common HashThreadLaunch shim should be removed after the LegacyCompat boundary cleanup.");
            AssertFileMissing(repoRoot, @"trunk\source\Common\ManagedHashMgmtAccess.h", "Phase 87 Common ManagedHashMgmtAccess shim should be removed after the LegacyCompat boundary cleanup.");

            HashSet<string> allowedThreadDataFiles = [];

            string commonRoot = Path.Combine(repoRoot, @"trunk\source\Common");
            string[] commonFiles = Directory.GetFiles(commonRoot, "*.*", SearchOption.AllDirectories);
            for (int fileIndex = 0; fileIndex < commonFiles.Length; ++fileIndex)
            {
                string commonFile = commonFiles[fileIndex];
                if (!commonFile.EndsWith(".h", StringComparison.OrdinalIgnoreCase) &&
                    !commonFile.EndsWith(".cpp", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string contents = File.ReadAllText(commonFile, Encoding.UTF8);
                if (!contents.Contains("ThreadData", StringComparison.Ordinal))
                {
                    continue;
                }

                string relativePath = Path.GetRelativePath(repoRoot, commonFile).Replace('/', '\\');
                if (allowedThreadDataFiles.Contains(relativePath))
                {
                    continue;
                }

                failures.Add($"Phase 87 ThreadData isolation failed: {relativePath} still references ThreadData outside the legacy seam whitelist.");
            }
        }, failures);

        Run("Phase 88 promotes descriptor-id index lookup to the primary request-selection seam", () =>
        {
            string hashAlgorithmRegistry = ReadHashAlgorithmRegistrySeams(repoRoot);
            string hashRequest = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashRequest.h");
            string nativeRuntimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
            string nativeRuntimeUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\NativeRuntimeFrameworkUnitTests.cs");
            string hashContractUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\HashContractUnitTests.cs");

            AssertContains(hashAlgorithmRegistry, "GetHashAlgorithmIndexById(const HashAlgorithmId& algorithmId)", "Phase 88 HashAlgorithmRegistry.h does not yet expose descriptor-id index lookup.");
            AssertContains(hashAlgorithmRegistry, "TryGetHashAlgorithmIndexById(const HashAlgorithmId& algorithmId, int *algorithmIndex)", "Phase 88 HashAlgorithmRegistry.h does not yet expose descriptor-id index probe helpers.");
            AssertContains(hashAlgorithmRegistry, "bool requiresDigestOperations;", "Phase 88 HashAlgorithmRegistry.h does not yet expose descriptor-level digest-operation requirements.");
            AssertContains(hashAlgorithmRegistry, "DoesHashAlgorithmDescriptorRequireDigestOperations(const HashAlgorithmDescriptor& algorithmDescriptor)", "Phase 88 HashAlgorithmRegistry.h does not yet expose the descriptor-operation requirement seam.");
            AssertContains(hashRequest, "TryGetHashAlgorithmIndexById(normalizedAlgorithmIds[algorithmIndex], &registeredIndex)", "Phase 88 HashRequest selection state does not yet resolve registry indices by descriptor id.");
            AssertDoesNotContain(hashRequest, "TryGetHashAlgorithmDescriptorById(normalizedAlgorithmIds[algorithmIndex], &algorithmDescriptor)", "Phase 88 HashRequest selection state still resolves registry indices by descriptor type fallback.");

            AssertContains(nativeRuntimeSource, "HashRequest_SelectionStateResolvesByAlgorithmIdForUnknownDigestTypes", "Phase 88 native runtime tests do not yet cover descriptor-id selection for unknown digest identities.");
            AssertContains(nativeRuntimeUnitTests, "HashRequest_SelectionStateResolvesByAlgorithmIdForUnknownDigestTypes", "Phase 88 managed tests do not yet gate descriptor-id selection runtime coverage.");
            AssertContains(hashContractUnitTests, "HashAlgorithmRegistry_UsesDescriptorIdLookupAsPrimarySelectionSeam", "Phase 88 contract tests do not yet gate descriptor-id selection seams.");
        }, failures);

        Run("Phase 89 keeps new algorithm extensibility on descriptor-id seams instead of fixed digest slots", () =>
        {
            string registryCore = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashAlgorithmRegistryCore.h");
            string hashRequest = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashRequest.h");
            string hashResult = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashResult.h");
            string digestMetadataAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestMetadataAccess.h");
            string digestValueAccess = ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestValueAccess.h");
            string global = ReadRepoFile(repoRoot, @"trunk\source\Common\Global.h");
            string nativeRuntimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
            string extensibilityUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\HashExtensibilityRegressionUnitTests.cs");

            AssertContains(registryCore, "RegisterHashAlgorithmDescriptor(const HashAlgorithmDescriptor& algorithmDescriptor)", "Phase 89 HashAlgorithmRegistryCore.h does not yet expose descriptor registration for new algorithms.");
            AssertContains(registryCore, "ClearHashAlgorithmDescriptorsForTesting()", "Phase 89 HashAlgorithmRegistryCore.h does not yet expose registry reset for extension tests.");
            AssertContains(registryCore, "ResetHashAlgorithmDescriptorsToDefaultsForTesting()", "Phase 89 HashAlgorithmRegistryCore.h does not yet expose default-registry reset for extension tests.");
            AssertContains(hashRequest, "std::vector<HashAlgorithmId> algorithmIds;", "Phase 89 HashRequest does not yet keep algorithm ids as the core extensibility surface.");
            AssertContains(hashRequest, "GetHashRequestNormalizedAlgorithmIds(const HashRequest& request)", "Phase 89 HashRequest does not yet normalize descriptor/id selections.");
            AssertContains(hashRequest, "TryGetHashAlgorithmIndexById(normalizedAlgorithmIds[algorithmIndex], &registeredIndex)", "Phase 89 HashRequest does not yet resolve selection state by descriptor id.");
            AssertContains(digestMetadataAccess, "GetResultDigestMetadataById(const HashAlgorithmId& algorithmId)", "Phase 89 ResultDigestMetadataAccess does not yet expose descriptor/id metadata lookup.");
            AssertContains(digestValueAccess, "SetResultDigestById(ResultData& result, const HashAlgorithmId& algorithmId, const sunjwbase::tstring& digestValue)", "Phase 89 ResultDigestValueAccess does not yet expose descriptor/id digest writes.");
            AssertContains(hashResult, "ProjectHashResult(const ResultData& result)", "Phase 89 HashResult projection does not yet remain the runtime projection seam.");
            AssertContains(hashResult, "digestResult.algorithmId = GetHashAlgorithmDescriptorId(digestMetadata);", "Phase 89 HashResult projection does not yet preserve descriptor/id identity.");
            AssertContains(global, "std::vector<HashDigestResult> digests;", "Phase 89 Global.h does not yet expose digest collections for extensible results.");
            AssertDoesNotContain(global, "sunjwbase::tstring md5;", "Phase 89 Global.h should not reintroduce a fixed MD5 field.");
            AssertDoesNotContain(global, "sunjwbase::tstring sha1;", "Phase 89 Global.h should not reintroduce a fixed SHA1 field.");
            AssertDoesNotContain(global, "sunjwbase::tstring sha256;", "Phase 89 Global.h should not reintroduce a fixed SHA256 field.");
            AssertDoesNotContain(global, "sunjwbase::tstring sha512;", "Phase 89 Global.h should not reintroduce a fixed SHA512 field.");

            AssertContains(nativeRuntimeSource, "HashAlgorithmRegistry_SupportsDescriptorIdRegistrationAndReset", "Phase 89 native runtime tests do not yet cover descriptor registration for new algorithms.");
            AssertContains(nativeRuntimeSource, "HashRequest_SelectionStateResolvesByAlgorithmIdForUnknownDigestTypes", "Phase 89 native runtime tests do not yet cover selection state extension by descriptor id.");
            AssertContains(nativeRuntimeSource, "HashResult_ProjectsRegistryExtendedDigestValuesWithoutFixedSlots", "Phase 89 native runtime tests do not yet cover result projection for registry-extended digests.");
            AssertContains(nativeRuntimeSource, "\"blake3\"", "Phase 89 native runtime tests do not yet cover a descriptor/id-only BLAKE3 registration path.");
            AssertContains(nativeRuntimeSource, "\"sha3-256\"", "Phase 89 native runtime tests do not yet cover a descriptor/id-only SHA3-256 selection path.");
            AssertContains(nativeRuntimeSource, "\"xxh3\"", "Phase 89 native runtime tests do not yet cover a descriptor/id-only XXH3 result-projection path.");
            AssertContains(nativeRuntimeSource, "SetResultDigestById(resultData, NormalizeHashAlgorithmId(sunjwbase::strtotstr(std::string(\"xxh3\"))), sunjwbase::strtotstr(std::string(\"CAFEBABE\")))", "Phase 89 native runtime tests do not yet write custom digest values through descriptor ids.");
            AssertContains(nativeRuntimeSource, "HashResult result = ProjectHashResult(resultData);", "Phase 89 native runtime tests do not yet project custom digest results through HashResult.");

            AssertContains(extensibilityUnitTests, "NativeRuntimeTests_CoverDescriptorRegistrationRequestSelectionAndResultProjectionForNewAlgorithms", "Phase 89 unit-test coverage does not yet gate the native extensibility scenarios.");
            AssertContains(extensibilityUnitTests, "CoreDescriptorIdSeams_AllowNewAlgorithmsWithoutAddingFixedDigestFields", "Phase 89 unit-test coverage does not yet gate the core extensibility seams.");
        }, failures);

        Run("Phase 90 keeps defensive security hardening on reparse rejection, checked arithmetic, RAII handles, throttled UI, and native mitigations", () =>
        {
            string osFilePosixDarwin = ReadRepoFile(repoRoot, @"trunk\source\OsUtils\OsFilePosixDarwin.cpp");
            string osFileWinApi = ReadRepoFile(repoRoot, @"trunk\source\OsUtils\OsFileWinApi.cpp");
            string osFileWinUwp = ReadRepoFile(repoRoot, @"trunk\source\OsUtils\OsFileWinUwp.cpp");
            string checkedArithmetic = ReadRepoFile(repoRoot, @"trunk\source\Common\CheckedArithmetic.h");
            string threadAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataExecutionAccess.h");
            string progressTracker = ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressTracker.cpp");
            string md5 = ReadRepoFile(repoRoot, @"trunk\source\Algorithms\MD5.cpp");
            string sha1 = ReadRepoFile(repoRoot, @"trunk\source\Algorithms\SHA1.cpp");
            string strhelper = ReadRepoFile(repoRoot, @"trunk\source\Common\strhelper.cpp");
            string uiBridge = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\UIBridgeMFC.cpp");
            string handleGuard = ReadRepoFile(repoRoot, @"trunk\source\WinCommon\WinHandleGuard.h");
            string shellCore = ReadRepoFile(repoRoot, @"trunk\source\WinCommon\ShellExplorerCommandCore.h");
            string windowsUtils = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\WindowsUtils.cpp");
            string runtimeTests = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
            string securityUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\SecurityHardeningUnitTests.cs");
            string nativeSecurityTargets = ReadRepoFile(repoRoot, @"NativeSecurity.targets");
            string legacyProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string nativeCoreProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");

            AssertContains(osFilePosixDarwin, "if (IsOpenModeCreate(posixFlag))", "Phase 90 POSIX Darwin file handling does not yet branch on O_CREAT before opening files.");
            AssertContains(osFilePosixDarwin, "*fd = ::open(strFilePath.c_str(), posixFlag, S_IRUSR | S_IWUSR | S_IRGRP | S_IROTH);", "Phase 90 POSIX Darwin file handling does not yet pass an explicit mode_t when O_CREAT is used.");
            AssertContains(osFilePosixDarwin, "static bool TryGetCurrentFileStatus(int *fd, const std::string& filePath, struct stat *fileStatus)", "Phase 90 POSIX Darwin metadata reads do not yet centralize on the current-file status helper.");
            AssertContains(osFilePosixDarwin, "if (fstat(*fd, &st) != 0)", "Phase 90 POSIX Darwin file handling does not yet validate the opened descriptor with fstat.");
            AssertContains(osFilePosixDarwin, "if (!IsRegularFile(st))", "Phase 90 POSIX Darwin file handling does not yet reject non-regular descriptors after open.");
            AssertContains(osFilePosixDarwin, "if (TryGetCurrentFileStatus(fd, strFilePath, &st))", "Phase 90 POSIX Darwin metadata reads do not yet reuse the current-file status helper.");
            AssertContains(osFilePosixDarwin, "if (fd == NULL || *fd == -1)", "Phase 90 POSIX Darwin file operations do not yet self-guard invalid file descriptors.");
            AssertDoesNotContain(osFilePosixDarwin, "if ((statRet = stat(strFilePath.c_str(), &st)) == 0", "Phase 90 POSIX Darwin file handling regressed to a stat-before-open TOCTOU gate.");
            AssertDoesNotContain(osFilePosixDarwin, "if (stat(strFilePath.c_str(), &st) == 0)", "Phase 90 POSIX Darwin metadata reads regressed to path-based stat lookups.");
            AssertDoesNotContain(osFilePosixDarwin, "Open first, we don't check here.", "Phase 90 POSIX Darwin file operations regressed to unchecked library-boundary assumptions.");
            AssertContains(osFileWinApi, "FILE_ATTRIBUTE_REPARSE_POINT", "Phase 90 Win32 file handling does not yet reject reparse points.");
            AssertContains(osFileWinApi, "Refusing to hash a symbolic link, junction, mount point, or other reparse point.", "Phase 90 Win32 file handling does not yet surface the reparse-point refusal message.");
            AssertContains(osFileWinApi, "HasReparsePointInPathHierarchy", "Phase 90 Win32 file handling does not yet walk ancestor path segments for reparse-point checks.");
            AssertContains(osFileWinApi, "if (!TryLongPathFix(_filePath, &fixedPath, pFileExc))", "Phase 90 Win32 file handling does not canonicalize long paths before opening files.");
            AssertContains(osFileWinApi, "if (TryRejectReparsePointPath(fixedPath, pFileExc))", "Phase 90 Win32 file handling does not reject reparse-point paths after long-path canonicalization.");
            AssertContains(osFileWinApi, "FILE_FLAG_OPEN_REPARSE_POINT", "Phase 90 Win32 file handling does not yet open the leaf object with reparse-point awareness.");
            AssertContains(osFileWinApi, "GetFileInformationByHandleEx(", "Phase 90 Win32 file handling does not yet validate opened handle attributes.");
            AssertContains(osFileWinApi, "GetFinalPathNameByHandle(", "Phase 90 Win32 file handling does not yet revalidate the resolved final path after open.");
            AssertContains(osFileWinApi, "kWindowsMaxExtendedPath = 32767", "Phase 90 Win32 file handling does not yet bound extended paths to the Windows 32767-character limit.");
            AssertContains(osFileWinApi, "ERROR_FILENAME_EXCED_RANGE", "Phase 90 Win32 file handling does not yet report explicit extended-path overflow errors.");
            AssertContains(osFileWinUwp, "FILE_ATTRIBUTE_REPARSE_POINT", "Phase 90 UWP file handling does not yet reject reparse points.");
            AssertContains(osFileWinUwp, "HasReparsePointInPathHierarchy", "Phase 90 UWP file handling does not yet walk ancestor path segments for reparse-point checks.");

            AssertContains(checkedArithmetic, "SaturatingAddUInt64", "Phase 90 checked arithmetic helpers do not yet expose saturating adds.");
            AssertContains(checkedArithmetic, "ReplaceSizedValueUInt64", "Phase 90 checked arithmetic helpers do not yet expose bounded replacement math.");
            AssertContains(progressTracker, "CalculateBoundedProgressValue", "Phase 90 progress tracking does not yet use bounded progress math.");
            AssertContains(threadAccess, "SaturatingAddUInt64(GetThreadDataTotalSize(threadData), sizeDelta)", "Phase 90 thread-data accounting does not yet use saturating adds.");
            AssertContains(threadAccess, "ReplaceSizedValueUInt64(GetThreadDataTotalSize(threadData), previousSize, currentSize)", "Phase 90 thread-data accounting does not yet use bounded replacement math.");

            AssertContains(handleGuard, "typedef UniqueHandleBase<HANDLE, HandleCloseTraits> UniqueWinHandle;", "Phase 90 WinHandleGuard does not yet expose UniqueWinHandle.");
            AssertContains(shellCore, "WinHandleGuard::UniqueWinHandle threadHandle(pInfo.hThread);", "Phase 90 shell-core launch path does not yet wrap the thread handle in RAII.");
            AssertContains(shellCore, "0, 0, FALSE,", "Phase 90 shell-core launch path still inherits parent handles.");
            AssertDoesNotContain(shellCore, "0, 0, TRUE,", "Phase 90 shell-core launch path regressed to inheriting parent handles.");
            AssertContains(windowsUtils, "WinHandleGuard::UniqueModuleHandle hModule", "Phase 90 WindowsUtils does not yet wrap loaded modules in RAII.");
            AssertContains(windowsUtils, "return LoadLibraryEx(pszDllPath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);", "Phase 90 WindowsUtils still falls back to bare LoadLibrary for shell-extension DLL loading.");
            AssertDoesNotContain(windowsUtils, "return LoadLibrary(pszDllPath);", "Phase 90 WindowsUtils regressed to bare LoadLibrary for shell-extension DLL loading.");
            AssertContains(windowsUtils, "bool IsAcceptableContextMenuDeleteResult(LONG deleteResult)", "Phase 90 WindowsUtils does not yet normalize context-menu delete results explicitly.");
            AssertContains(windowsUtils, "deleteResult == ERROR_SUCCESS || deleteResult == ERROR_FILE_NOT_FOUND", "Phase 90 WindowsUtils does not yet treat missing context-menu keys as acceptable cleanup.");
            AssertDoesNotContain(windowsUtils, "lResult &= keyShell.RecurseDeleteKey", "Phase 90 WindowsUtils regressed to folding Win32 delete error codes with bitwise operations.");
            AssertDoesNotContain(windowsUtils, "lResult &= keyShellEx.RecurseDeleteKey", "Phase 90 WindowsUtils regressed to folding shell-extension delete error codes with bitwise operations.");
            AssertContains(windowsUtils, "GetNativeSystemInfo(&systemInfo);", "Phase 90 WindowsUtils does not yet use the native system architecture API for 64-bit detection.");
            AssertContains(windowsUtils, "case PROCESSOR_ARCHITECTURE_ARM64:", "Phase 90 WindowsUtils does not yet treat ARM64 as a 64-bit Windows architecture.");
            AssertDoesNotContain(windowsUtils, "QueryStringValue(lpszArchKeyName", "Phase 90 WindowsUtils regressed to registry-based processor architecture probing.");

            AssertContains(md5, "static const unsigned char PADDING[64]", "Phase 90 MD5 padding should now be treated as immutable shared algorithm state.");
            AssertDoesNotContain(sha1, "static unsigned char workspace[64];", "Phase 90 SHA1 transform still shares mutable static workspace across threads.");
            AssertDoesNotContain(sha1, "HashFile(", "Phase 90 SHA1 still keeps the legacy direct file-hashing helper instead of relying on the shared OsFile pipeline.");
            AssertDoesNotContain(sha1, "fopen(", "Phase 90 SHA1 still opens files directly with stdio.");
            AssertDoesNotContain(sha1, "fread(", "Phase 90 SHA1 still reads files directly with stdio.");
            AssertDoesNotContain(sha1, "ferror(", "Phase 90 SHA1 still owns stdio error handling.");
            AssertDoesNotContain(sha1, "ferror(", "Phase 90 SHA1 still owns stdio read-failure handling instead of relying on the shared file pipeline.");
            AssertDoesNotContain(sha1, "uint32_t ulFileSize", "Phase 90 SHA1 file hashing still relies on a 32-bit total file-size accumulator.");
            AssertDoesNotContain(sha1, "ftell(fIn)", "Phase 90 SHA1 file hashing still uses ftell-driven chunk planning.");
            AssertDoesNotContain(sha1, "fseek(fIn, 0, SEEK_END)", "Phase 90 SHA1 file hashing still seeks to the end of the file to precompute chunk counts.");
            AssertContains(strhelper, "std::wstring_convert<std::codecvt_utf8<wchar_t>>", "Phase 90 POSIX string helpers do not yet use explicit UTF-8 wide-string conversion.");
            AssertContains(strhelper, "size_t iconvResult = iconv(cd, inleft > 0 ? &in : NULL, &inleft, &out, &outleft);", "Phase 90 POSIX string helpers do not yet validate iconv return values explicitly.");
            AssertContains(strhelper, "if (errno == E2BIG)", "Phase 90 POSIX string helpers do not yet grow iconv output buffers safely.");
            AssertDoesNotContain(strhelper, "setlocale(LC_ALL", "Phase 90 POSIX string helpers still mutate the global process locale.");
            AssertDoesNotContain(strhelper, "wcstombs(", "Phase 90 POSIX string helpers still rely on wcstombs-driven locale conversions.");
            AssertDoesNotContain(strhelper, "mbstowcs(", "Phase 90 POSIX string helpers still rely on mbstowcs-driven locale conversions.");
            AssertDoesNotContain(strhelper, "\"UTF-8\", \"ASCII\"", "Phase 90 POSIX UTF-8 helpers still route through an ASCII bridge conversion.");
            AssertDoesNotContain(strhelper, "\"ASCII\", \"UTF-8\"", "Phase 90 POSIX UTF-8 helpers still route through an ASCII bridge conversion.");
            AssertContains(uiBridge, "ShouldPostProgressValue(m_totalProgressDispatchState, value)", "Phase 90 UI progress dispatch does not yet throttle total-progress updates.");
            AssertContains(uiBridge, "kUiProgressDispatchIntervalMs = 80", "Phase 90 UI progress dispatch does not yet enforce the refresh interval.");
            AssertContains(runtimeTests, "HashThreadFunc_ProducesConsistentDigestsAcrossConcurrentRuns", "Phase 90 native runtime tests do not yet cover concurrent digest consistency.");

            AssertContains(nativeSecurityTargets, "<BufferSecurityCheck>true</BufferSecurityCheck>", "Phase 90 native security targets do not yet enable /GS.");
            AssertContains(nativeSecurityTargets, "<SDLCheck>true</SDLCheck>", "Phase 90 native security targets do not yet enable /sdl.");
            AssertContains(nativeSecurityTargets, "<FHashEnableControlFlowGuard>true</FHashEnableControlFlowGuard>", "Phase 90 native security targets do not yet enable CFG by default.");
            AssertContains(nativeSecurityTargets, "<FHashEnableControlFlowGuard Condition=\"'$(CLRSupport)'!=''\">false</FHashEnableControlFlowGuard>", "Phase 90 native security targets do not yet exempt managed CLR bridge projects from CFG.");
            AssertContains(nativeSecurityTargets, "<ControlFlowGuard Condition=\"'$(FHashEnableControlFlowGuard)'=='true'\">Guard</ControlFlowGuard>", "Phase 90 native security targets do not yet enable CFG for eligible native projects.");
            AssertContains(nativeSecurityTargets, "<RandomizedBaseAddress>true</RandomizedBaseAddress>", "Phase 90 native security targets do not yet enable ASLR.");
            AssertContains(nativeSecurityTargets, "<HighEntropyVA>true</HighEntropyVA>", "Phase 90 native security targets do not yet enable high-entropy VA for x64.");
            AssertContains(nativeSecurityTargets, "<DataExecutionPrevention>true</DataExecutionPrevention>", "Phase 90 native security targets do not yet enable DEP.");
            AssertContains(legacyProject, "NativeSecurity.targets", "Phase 90 legacy native project does not yet import the shared security targets.");
            AssertContains(nativeCoreProject, "NativeSecurity.targets", "Phase 90 native core project does not yet import the shared security targets.");

            AssertContains(securityUnitTests, "FileOpenPaths_RejectReparsePoints_AndReuseOpenedHandleMetadata", "Phase 90 unit tests do not yet gate reparse-point rejection.");
            AssertContains(securityUnitTests, "HandleOwnership_UsesRaiiAcrossWorkerAndShellPaths", "Phase 90 unit tests do not yet gate HANDLE RAII adoption.");
            AssertContains(securityUnitTests, "RuntimeUses_CheckedArithmetic_ForSizesAndProgress", "Phase 90 unit tests do not yet gate checked arithmetic usage.");
            AssertContains(securityUnitTests, "DigestExecution_RemainsThreadLocal_AndUiProgress_IsThrottled", "Phase 90 unit tests do not yet gate thread-local digest execution and throttled UI progress.");
        }, failures);

        Run("Phase 91 vendors fixed-version official BLAKE3 C code behind provider and descriptor seams", () =>
        {
            string registryCore = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashAlgorithmRegistryCore.h");
            string threadAccess = ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataExecutionAccess.h");
            string providerHeader = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\BLAKE3HashProvider.h");
            string providerImplementation = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\BLAKE3HashProvider.cpp");
            string internalHeader = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string digestRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestOperationRegistry.cpp");
            string nativeCoreProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string upstreamNote = ReadRepoFile(repoRoot, @"third_party\blake3\1.8.4\README.LHash.md");
            string upstreamHeader = ReadRepoFile(repoRoot, @"third_party\blake3\1.8.4\c\blake3.h");
            string nativeRuntimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
            string extensibilityUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\HashExtensibilityRegressionUnitTests.cs");

            AssertContains(registryCore, "{ \"blake3-256\", \"BLAKE3-256\", true, false }", "Phase 91 registry does not yet register BLAKE3-256 as a fixed descriptor variant.");
            AssertContains(registryCore, "{ \"blake3-512\", \"BLAKE3-512\", true, false }", "Phase 91 registry does not yet register BLAKE3-512 as a fixed descriptor variant.");
            AssertContains(registryCore, "{ \"blake3-xof\", \"BLAKE3 XOF\", true, false }", "Phase 91 registry does not yet register BLAKE3 XOF as a fixed descriptor variant.");
            AssertContains(registryCore, "bool enabledByDefault;", "Phase 91 registry does not yet expose default-enabled metadata for new algorithms.");
            AssertContains(threadAccess, "IsHashAlgorithmDescriptorEnabledByDefault(algorithmDescriptor)", "Phase 91 thread-data defaults do not yet honor per-descriptor default selection.");

            AssertContains(providerHeader, "BLAKE3_256_OUTPUT_BYTES = BLAKE3_OUT_LEN", "Phase 91 BLAKE3 provider does not yet expose the 256-bit output profile.");
            AssertContains(providerHeader, "BLAKE3_512_OUTPUT_BYTES = 64", "Phase 91 BLAKE3 provider does not yet expose the 512-bit output profile.");
            AssertContains(providerHeader, "BLAKE3_XOF_OUTPUT_BYTES = 128", "Phase 91 BLAKE3 provider does not yet expose the XOF output profile.");
            AssertContains(providerImplementation, "blake3_hasher_finalize(&hasher", "Phase 91 BLAKE3 provider does not yet finalize through the official C API.");
            AssertContains(internalHeader, "blake3_hasher blake3_256;", "Phase 91 runtime context does not yet carry a dedicated BLAKE3-256 state.");
            AssertContains(internalHeader, "blake3_hasher blake3_512;", "Phase 91 runtime context does not yet carry a dedicated BLAKE3-512 state.");
            AssertContains(internalHeader, "blake3_hasher blake3Xof;", "Phase 91 runtime context does not yet carry a dedicated BLAKE3 XOF state.");
            AssertContains(digestRegistry, "GetBlake3_256AlgorithmId()", "Phase 91 digest-operation registry does not yet register BLAKE3-256.");
            AssertContains(digestRegistry, "GetBlake3_512AlgorithmId()", "Phase 91 digest-operation registry does not yet register BLAKE3-512.");
            AssertContains(digestRegistry, "GetBlake3XofAlgorithmId()", "Phase 91 digest-operation registry does not yet register BLAKE3 XOF.");

            AssertContains(nativeCoreProject, @"third_party\blake3\1.8.4\c", "Phase 91 native core project does not yet include the fixed-version BLAKE3 vendor path.");
            AssertContains(nativeCoreProject, @"Runtime\Hash\BLAKE3HashProvider.cpp", "Phase 91 native core project does not yet compile the BLAKE3 provider.");
            AssertContains(uwpNativeProject, @"third_party\blake3\1.8.4\c", "Phase 91 UWP native project does not yet include the fixed-version BLAKE3 vendor path.");

            AssertContains(upstreamNote, "Upstream tag: 1.8.4", "Phase 91 third-party note does not yet pin the imported BLAKE3 tag.");
            AssertContains(upstreamNote, "b97a24f8754819755ef78d8016c0391c65c943c5", "Phase 91 third-party note does not yet pin the imported BLAKE3 commit.");
            AssertContains(upstreamHeader, "BLAKE3_VERSION_STRING \"1.8.4\"", "Phase 91 vendored BLAKE3 header is not the expected fixed upstream version.");

            AssertContains(nativeRuntimeSource, "HashThreadFunc_ComputesOfficialBlake3DigestsForKnownVector", "Phase 91 native runtime tests do not yet cover official BLAKE3 vectors.");
            AssertContains(nativeRuntimeSource, "\"blake3-256\"", "Phase 91 native runtime tests do not yet request BLAKE3-256 by descriptor id.");
            AssertContains(nativeRuntimeSource, "\"blake3-512\"", "Phase 91 native runtime tests do not yet request BLAKE3-512 by descriptor id.");
            AssertContains(nativeRuntimeSource, "\"blake3-xof\"", "Phase 91 native runtime tests do not yet request BLAKE3 XOF by descriptor id.");
            AssertContains(nativeRuntimeSource, "E1BE4D7A8AB5560AA4199EEA339849BA8E293D55CA0A81006726D184519E647F", "Phase 91 native runtime tests do not yet assert the official BLAKE3 vector.");
            AssertContains(extensibilityUnitTests, "Blake3Integration_VendorsOfficialFixedVersion_AndAddsThreeDescriptorVariants", "Phase 91 unit tests do not yet gate the fixed-version BLAKE3 integration.");
        }, failures);
        Run("Phase 92 strengthens BLAKE3 runtime coverage and keeps the progress sink non-owning", () =>
        {
            string executionContext = ReadRepoFile(repoRoot, @"trunk\source\Runtime\HashExecutionContext.h");
            string nativeRuntimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
            string securityUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\SecurityHardeningUnitTests.cs");
            string hashContractTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\HashContractUnitTests.cs");
            string nativeRuntimeUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\NativeRuntimeFrameworkUnitTests.cs");
            string securityRegression = ReadRepoFile(repoRoot, @"security-tests\SecurityRegression\Program.cs");
            string nativeCoreProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");

            AssertContains(executionContext, "class NullHashProgressSink : public HashProgressSink", "Phase 92 execution context does not yet expose the null-object progress sink.");
            AssertContains(executionContext, "HashProgressSink& progressSinkObserver;", "Phase 92 execution context does not yet model the sink as a non-owning observer reference.");
            AssertContains(executionContext, "sink != NULL ? *sink : GetNullHashProgressSink()", "Phase 92 execution context does not yet provide a null-safe sink fallback.");
            AssertDoesNotContain(executionContext, "HashProgressSink *progressSink;", "Phase 92 execution context regressed to a raw stored sink pointer.");

            AssertContains(nativeRuntimeSource, "RunHashRequest_Blake3UppercaseFlagRemainsDeterministicAcrossVariants", "Phase 92 native runtime tests do not yet cover uppercase BLAKE3 behavior.");
            AssertContains(nativeRuntimeSource, "RunHashRequest_Blake3UnknownIdsAreIgnoredAndKnownVariantsStayOrdered", "Phase 92 native runtime tests do not yet cover BLAKE3 id normalization behavior.");
            AssertContains(nativeRuntimeSource, "HashThreadFunc_Blake3VariantsRemainStableAcrossConcurrentRuns", "Phase 92 native runtime tests do not yet cover concurrent BLAKE3 stability.");
            AssertContains(nativeRuntimeSource, "CreateAlgorithmId(\"blake3-1024\")", "Phase 92 native runtime tests do not yet probe unsupported BLAKE3 ids.");

            AssertContains(nativeCoreProject, @"blake3_sse2.c", "Phase 92 desktop native core does not yet compile the BLAKE3 SSE2 translation unit.");
            AssertContains(nativeCoreProject, @"blake3_sse41.c", "Phase 92 desktop native core does not yet compile the BLAKE3 SSE4.1 translation unit.");
            AssertContains(nativeCoreProject, @"blake3_avx2.c", "Phase 92 desktop native core does not yet compile the BLAKE3 AVX2 translation unit.");
            AssertContains(nativeCoreProject, @"blake3_avx512.c", "Phase 92 desktop native core does not yet compile the BLAKE3 AVX512 translation unit.");
            AssertContains(nativeCoreProject, "FHashBlake3SimdProfile", "Phase 92 desktop native core does not yet expose a benchmark-selectable BLAKE3 SIMD profile.");
            AssertContains(nativeCoreProject, "Condition=\"'$(FHashBlake3SimdProfile)'=='portable'\">BLAKE3_USE_NEON=0;BLAKE3_NO_SSE2;BLAKE3_NO_SSE41;BLAKE3_NO_AVX2;BLAKE3_NO_AVX512;%(PreprocessorDefinitions)</PreprocessorDefinitions>", "Phase 92 desktop native core does not yet expose a portable BLAKE3 benchmark control.");
            AssertContains(nativeCoreProject, "ExcludedFromBuild Condition=\"'$(FHashBlake3SimdProfile)'=='portable'\"", "Phase 92 desktop native core does not yet allow benchmark builds to disable BLAKE3 SIMD translation units.");
            AssertContains(nativeCoreProject, "Condition=\"'$(Platform)'=='Win32'\">/arch:SSE2", "Phase 92 desktop native core does not yet enable the Win32 SSE2 BLAKE3 translation unit.");
            AssertContains(nativeCoreProject, "Condition=\"'$(Platform)'=='Win32'\">/arch:AVX", "Phase 92 desktop native core does not yet enable the Win32 SSE4.1-compatible BLAKE3 translation unit.");
            AssertContains(nativeCoreProject, "/arch:AVX2", "Phase 92 desktop native core does not yet enable AVX2 for the dedicated BLAKE3 translation unit.");
            AssertContains(nativeCoreProject, "/arch:AVX512", "Phase 92 desktop native core does not yet enable AVX512 for the dedicated BLAKE3 translation unit.");
            AssertContains(uwpNativeProject, @"blake3_sse2.c", "Phase 92 UWP native core does not yet compile the BLAKE3 SSE2 translation unit.");
            AssertContains(uwpNativeProject, @"blake3_sse41.c", "Phase 92 UWP native core does not yet compile the BLAKE3 SSE4.1 translation unit.");
            AssertContains(uwpNativeProject, @"blake3_avx2.c", "Phase 92 UWP native core does not yet compile the BLAKE3 AVX2 translation unit.");
            AssertContains(uwpNativeProject, @"blake3_avx512.c", "Phase 92 UWP native core does not yet compile the BLAKE3 AVX512 translation unit.");
            AssertContains(uwpNativeProject, @"blake3_neon.c", "Phase 92 UWP native core does not yet compile the BLAKE3 ARM64 NEON translation unit.");
            AssertContains(uwpNativeProject, "Condition=\"'$(Platform)'=='Win32'\">/arch:SSE2", "Phase 92 UWP native core does not yet enable the Win32 SSE2 BLAKE3 translation unit.");
            AssertContains(uwpNativeProject, "/arch:AVX2", "Phase 92 UWP native core does not yet enable AVX2 for the dedicated BLAKE3 translation unit.");
            AssertContains(uwpNativeProject, "/arch:AVX512", "Phase 92 UWP native core does not yet enable AVX512 for the dedicated BLAKE3 translation unit.");
            AssertContains(uwpNativeProject, "Condition=\"'$(Platform)'=='ARM64'\">BLAKE3_USE_NEON=1;BLAKE3_NO_SSE2;BLAKE3_NO_SSE41;BLAKE3_NO_AVX2;BLAKE3_NO_AVX512;%(PreprocessorDefinitions)</PreprocessorDefinitions>", "Phase 92 UWP native core does not yet enable the ARM64 NEON BLAKE3 path.");

            AssertContains(securityUnitTests, "Blake3Integration_UsesOfficialProviderProfiles_AndCoversUppercaseOrderingAndConcurrency", "Phase 92 unit tests do not yet gate the deeper BLAKE3 behavior coverage.");
            AssertContains(securityUnitTests, "HashExecutionContext_UsesANonOwningSinkReferenceWithNullFallback", "Phase 92 unit tests do not yet gate the execution-context sink lifetime seam.");
            AssertContains(hashContractTests, "HashExecutionContext_ModelsProgressSinkAsANonOwningObserverSeam", "Phase 92 contract tests do not yet describe the non-owning sink seam.");
            AssertContains(nativeRuntimeUnitTests, "RunHashRequest_Blake3UppercaseFlagRemainsDeterministicAcrossVariants", "Phase 92 native-runtime framework tests do not yet require uppercase BLAKE3 coverage.");
            AssertContains(nativeRuntimeUnitTests, "HashThreadFunc_Blake3VariantsRemainStableAcrossConcurrentRuns", "Phase 92 native-runtime framework tests do not yet require concurrent BLAKE3 coverage.");
            AssertContains(securityRegression, "BLAKE3 provider and descriptor variants stay covered by hardening gates", "Phase 92 security regression does not yet gate BLAKE3 coverage.");
        }, failures);

        Run("Phase 93 adds runnable filesystem attack harnesses for nested junctions and sharing violations", () =>
        {
            string securityRegression = ReadRepoFile(repoRoot, @"security-tests\SecurityRegression\Program.cs");
            string securityHarness = ReadRepoFile(repoRoot, @"security-tests\SecurityRegression\WindowsSecurityRuntimeHarness.cs");
            string nativeSecurityRuntime = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineSecurityRuntimeTests.cpp");
            string nativeRuntimeProject = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\FHash.NativeRuntimeTests.vcxproj");
            string nativeRuntimeUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\NativeRuntimeFrameworkUnitTests.cs");
            string securityUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\SecurityHardeningUnitTests.cs");

            AssertContains(securityRegression, "Windows junction attack harness reproduces ancestor reparse-point traversal", "Phase 93 security regression does not yet run the junction attack harness.");
            AssertContains(securityRegression, "Windows hash-style open harness reproduces sharing violations for locked files", "Phase 93 security regression does not yet run the sharing-violation harness.");
            AssertContains(securityHarness, "CreateDirectoryJunction", "Phase 93 security harness does not yet create directory junctions.");
            AssertContains(securityHarness, "leafAttributes.HasFlag(FileAttributes.ReparsePoint)", "Phase 93 security harness does not yet demonstrate the leaf-vs-ancestor reparse-point distinction.");
            AssertContains(securityHarness, "OpenFileForHashStyleRead", "Phase 93 security harness does not yet reproduce the hash-style file-open path.");
            AssertContains(securityHarness, "ERROR_SHARING_VIOLATION = 32", "Phase 93 security harness does not yet validate sharing-violation behavior.");

            AssertContains(nativeSecurityRuntime, "OsFile_RejectsLeafPathsNestedUnderDirectoryJunctions", "Phase 93 native runtime tests do not yet cover nested junction paths.");
            AssertContains(nativeSecurityRuntime, "OsFile_ReportsSharingViolationsForLockedFiles", "Phase 93 native runtime tests do not yet cover locked-file sharing violations.");
            AssertContains(nativeSecurityRuntime, "mklink /J", "Phase 93 native runtime tests do not yet create directory junctions.");
            AssertContains(nativeSecurityRuntime, "FILE_ATTRIBUTE_REPARSE_POINT", "Phase 93 native runtime tests do not yet assert reparse-point attributes.");
            AssertContains(nativeSecurityRuntime, "sunjwbase::OsFile::ERR_MSG_BUFFER_LEN", "Phase 93 native runtime tests do not yet qualify OsFile buffer constants through the OsFile namespace.");
            AssertContains(nativeRuntimeProject, "HashEngineSecurityRuntimeTests.cpp", "Phase 93 native runtime project does not yet compile the dedicated security runtime tests.");

            AssertContains(nativeRuntimeUnitTests, "HashEngineSecurityRuntimeTests.cpp", "Phase 93 unit tests do not yet gate the dedicated native security runtime source file.");
            AssertContains(securityUnitTests, "TestJunctionAncestorAttackSurface", "Phase 93 unit tests do not yet gate the managed junction attack harness.");
            AssertContains(securityUnitTests, "TestHashStyleOpenSharingViolation", "Phase 93 unit tests do not yet gate the managed sharing-violation harness.");
        }, failures);

        Run("Phase 94 adds a controlled native benchmark workflow before changing BLAKE3 SIMD shipping decisions", () =>
        {
            string benchmarkWorkflow = ReadRepoFile(repoRoot, @".github\workflows\native-benchmarks.yml");
            string benchmarkProject = ReadRepoFile(repoRoot, @"native-benchmarks\FHash.NativeBenchmarks\FHash.NativeBenchmarks.vcxproj");
            string benchmarkSource = ReadRepoFile(repoRoot, @"native-benchmarks\FHash.NativeBenchmarks\HashEngineBenchmarks.cpp");
            string benchmarkUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\BenchmarkFrameworkUnitTests.cs");
            string nativeCoreProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string benchmarkDoc = ReadRepoFile(repoRoot, @"docs\NATIVE_BENCHMARKS.md");

            AssertContains(benchmarkWorkflow, "name: Native Benchmarks", "Phase 94 does not yet define a dedicated native benchmark workflow.");
            AssertContains(benchmarkWorkflow, "workflow_dispatch:", "Phase 94 native benchmark workflow should remain manually triggered.");
            AssertDoesNotContain(benchmarkWorkflow, "push:", "Phase 94 native benchmark workflow should not auto-run on every push.");
            AssertContains(benchmarkWorkflow, "/p:FHashBlake3SimdProfile=portable", "Phase 94 does not yet build a portable BLAKE3 benchmark control.");
            AssertContains(benchmarkWorkflow, "/p:FHashBlake3SimdProfile=current", "Phase 94 does not yet build the current BLAKE3 benchmark configuration.");
            AssertContains(benchmarkWorkflow, "windows-11-arm", "Phase 94 does not yet schedule a dedicated ARM64 benchmark runner.");
            AssertContains(benchmarkWorkflow, "benchmark_platform: ARM64", "Phase 94 does not yet benchmark the ARM64 platform.");
            AssertContains(benchmarkWorkflow, "benchmark_platform: Win32", "Phase 94 does not yet benchmark the Win32 platform.");
            AssertContains(benchmarkWorkflow, "benchmark_platform: x64", "Phase 94 does not yet benchmark the x64 platform.");
            AssertContains(benchmarkWorkflow, "native-benchmarks-portable.csv", "Phase 94 does not yet persist the portable benchmark results.");
            AssertContains(benchmarkWorkflow, "native-benchmarks-current.csv", "Phase 94 does not yet persist the current benchmark results.");
            AssertContains(benchmarkWorkflow, "artifact_suffix: arm64", "Phase 94 does not yet define an ARM64 benchmark artifact suffix.");
            AssertContains(benchmarkWorkflow, "artifact_suffix: win32", "Phase 94 does not yet define a Win32 benchmark artifact suffix.");
            AssertContains(benchmarkWorkflow, "artifact_suffix: x64", "Phase 94 does not yet define an x64 benchmark artifact suffix.");
            AssertContains(benchmarkWorkflow, "name: FHash-native-benchmarks-${{ matrix.artifact_suffix }}", "Phase 94 does not yet publish per-platform benchmark artifacts.");
            AssertContains(benchmarkProject, "<ProjectName>FHash.NativeBenchmarks</ProjectName>", "Phase 94 does not yet introduce a standalone native benchmark project.");
            AssertContains(benchmarkProject, @"..\..\sub-proj\fHashNativeCore\fHashNativeCore.vcxproj", "Phase 94 native benchmarks do not yet link against the desktop native core.");
            AssertContains(benchmarkProject, "Include=\"Debug|ARM64\"", "Phase 94 native benchmark project does not yet define an ARM64 debug configuration.");
            AssertContains(benchmarkProject, "Include=\"Release|ARM64\"", "Phase 94 native benchmark project does not yet define an ARM64 release configuration.");
            AssertContains(nativeCoreProject, "FHashBlake3SimdProfile", "Phase 94 desktop native core does not yet expose a benchmark-selectable BLAKE3 SIMD profile.");
            AssertContains(nativeCoreProject, "ExcludedFromBuild Condition=\"'$(FHashBlake3SimdProfile)'=='portable'\"", "Phase 94 desktop native core does not yet allow benchmark builds to disable BLAKE3 SIMD translation units.");
            AssertContains(nativeCoreProject, @"..\..\third_party\blake3\1.8.4\c\blake3_neon.c", "Phase 94 desktop native core does not yet compile the ARM64 NEON BLAKE3 translation unit.");
            AssertContains(nativeCoreProject, "Condition=\"'$(FHashBlake3SimdProfile)'!='portable' and '$(Platform)'=='ARM64'\">BLAKE3_USE_NEON=1;BLAKE3_NO_SSE2;BLAKE3_NO_SSE41;BLAKE3_NO_AVX2;BLAKE3_NO_AVX512;%(PreprocessorDefinitions)</PreprocessorDefinitions>", "Phase 94 desktop native core does not yet configure the ARM64 NEON BLAKE3 path.");
            AssertContains(benchmarkSource, "small-single-64k", "Phase 94 native benchmarks do not yet cover the small-file scenario.");
            AssertContains(benchmarkSource, "many-small-256x64k", "Phase 94 native benchmarks do not yet cover the many-file scenario.");
            AssertContains(benchmarkSource, "large-single-128m", "Phase 94 native benchmarks do not yet cover the large-file scenario.");
            AssertContains(benchmarkSource, "\"openssl-sha-256\"", "Phase 94 native benchmarks do not yet cover a single-algorithm SHA-256 control.");
            AssertContains(benchmarkSource, "\"blake3-256\"", "Phase 94 native benchmarks do not yet cover a single-algorithm BLAKE3 control.");
            AssertContains(benchmarkSource, "\"classic-4\"", "Phase 94 native benchmarks do not yet cover the legacy multi-algorithm combination.");
            AssertContains(benchmarkSource, "\"hybrid-4\"", "Phase 94 native benchmarks do not yet cover a mixed BLAKE3 multi-algorithm combination.");
            AssertContains(benchmarkUnitTests, "NativeBenchmarkWorkflow_ComparesCurrentAgainstPortableBLAKE3ProfilesAcrossDesktopPlatforms", "Phase 94 unit tests do not yet guard the benchmark workflow and control profile.");
            AssertContains(benchmarkDoc, "Decision rules", "Phase 94 documentation does not yet explain how benchmark results should drive SIMD decisions.");
            AssertContains(benchmarkDoc, "windows-11-arm", "Phase 94 documentation does not yet explain the dedicated ARM64 runner.");
            AssertContains(benchmarkDoc, "Do not use one platform's benchmark to justify another platform's SIMD changes", "Phase 94 documentation does not yet keep per-platform SIMD decisions behind dedicated measurements.");
        }, failures);

        Run("Phase 95 vendors xxHash3 and google CRC32C through runtime providers and fixed-version regression gates", () =>
        {
            string registryCore = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashAlgorithmRegistryCore.h");
            string hashEngineInternal = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h");
            string digestRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestOperationRegistry.cpp");
            string xxh3ProviderHeader = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\XXHash3HashProvider.h");
            string xxh3ProviderImplementation = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\XXHash3HashProvider.cpp");
            string crc32cProviderHeader = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\CRC32CHashProvider.h");
            string crc32cProviderImplementation = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\CRC32CHashProvider.cpp");
            string nativeCoreProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string uwpNativeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpNative\fHashUwpNative.vcxproj");
            string nativeRuntimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
            string extensibilityUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\HashExtensibilityRegressionUnitTests.cs");
            string securityUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\SecurityHardeningUnitTests.cs");
            string nativeRuntimeUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\NativeRuntimeFrameworkUnitTests.cs");
            string securityRegression = ReadRepoFile(repoRoot, @"security-tests\SecurityRegression\Program.cs");
            string xxhashNote = ReadRepoFile(repoRoot, @"third_party\xxhash\0.8.3\README.LHash.md");
            string crc32cNote = ReadRepoFile(repoRoot, @"third_party\crc32c\1.1.2\README.LHash.md");
            string crc32cArm64Check = ReadRepoFile(repoRoot, @"third_party\crc32c\1.1.2\src\crc32c_arm64_check.h");

            AssertContains(registryCore, "{ \"xxh3-64\", \"XXH3-64\", true, false }", "Phase 95 registry does not yet expose the XXH3-64 descriptor.");
            AssertContains(registryCore, "{ \"xxh3-128\", \"XXH3-128\", true, false }", "Phase 95 registry does not yet expose the XXH3-128 descriptor.");
            AssertContains(registryCore, "{ \"crc32c\", \"CRC32C\", true, false }", "Phase 95 registry does not yet expose the CRC32C descriptor.");
            AssertContains(hashEngineInternal, "XXH3_state_t xxh3_64;", "Phase 95 hash-engine context bundle does not yet carry the XXH3-64 state.");
            AssertContains(hashEngineInternal, "XXH3_state_t xxh3_128;", "Phase 95 hash-engine context bundle does not yet carry the XXH3-128 state.");
            AssertContains(hashEngineInternal, "uint32_t crc32c;", "Phase 95 hash-engine context bundle does not yet carry the CRC32C state.");
            AssertContains(digestRegistry, "GetXXH3_64AlgorithmId()", "Phase 95 digest operation registry does not yet define the XXH3-64 algorithm id seam.");
            AssertContains(digestRegistry, "GetXXH3_128AlgorithmId()", "Phase 95 digest operation registry does not yet define the XXH3-128 algorithm id seam.");
            AssertContains(digestRegistry, "GetCRC32CAlgorithmId()", "Phase 95 digest operation registry does not yet define the CRC32C algorithm id seam.");
            AssertContains(digestRegistry, "RegisterHashDigestOperationDescriptorUnlocked({", "Phase 95 digest operation registry no longer registers runtime digest descriptors.");

            AssertContains(xxh3ProviderHeader, "XXH3_64_OUTPUT_BYTES = sizeof(XXH64_hash_t)", "Phase 95 XXH3 provider header does not yet expose the 64-bit profile.");
            AssertContains(xxh3ProviderImplementation, "XXH3_64bits_reset", "Phase 95 XXH3 provider does not yet initialize through the official xxHash API.");
            AssertContains(xxh3ProviderImplementation, "XXH128_canonicalFromHash", "Phase 95 XXH3 provider does not yet canonicalize the 128-bit output.");
            AssertContains(crc32cProviderHeader, "CRC32C_OUTPUT_BYTES = sizeof(uint32_t)", "Phase 95 CRC32C provider header does not yet expose the 32-bit profile.");
            AssertContains(crc32cProviderImplementation, "crc32c_extend", "Phase 95 CRC32C provider does not yet update through the official google/crc32c API.");

            AssertContains(nativeCoreProject, @"third_party\xxhash\0.8.3\xxhash.c", "Phase 95 desktop native core does not yet compile the vendored xxHash source snapshot.");
            AssertContains(nativeCoreProject, @"third_party\crc32c\1.1.2\src\crc32c.cc", "Phase 95 desktop native core does not yet compile the vendored CRC32C source snapshot.");
            AssertContains(nativeCoreProject, @"third_party\crc32c\1.1.2\src\crc32c_arm64.cc", "Phase 95 desktop native core does not yet compile the ARM64 CRC32C path.");
            AssertContains(uwpNativeProject, @"third_party\xxhash\0.8.3\xxhash.c", "Phase 95 UWP native core does not yet compile the vendored xxHash source snapshot.");
            AssertContains(uwpNativeProject, @"third_party\crc32c\1.1.2\src\crc32c_arm64.cc", "Phase 95 UWP native core does not yet compile the ARM64 CRC32C path.");

            AssertContains(xxhashNote, "Upstream tag: v0.8.3", "Phase 95 xxHash vendor note does not yet pin the official upstream tag.");
            AssertContains(xxhashNote, "e626a72bc2321cd320e953a0ccf1584cad60f363", "Phase 95 xxHash vendor note does not yet pin the official upstream commit.");
            AssertContains(crc32cNote, "Upstream tag: 1.1.2", "Phase 95 CRC32C vendor note does not yet pin the official upstream tag.");
            AssertContains(crc32cNote, "02e65f4fd3065d27b2e29324800ca6d04df16126", "Phase 95 CRC32C vendor note does not yet pin the official upstream commit.");
            AssertContains(crc32cArm64Check, "PF_ARM_V8_CRC32_INSTRUCTIONS_AVAILABLE", "Phase 95 vendored CRC32C ARM64 runtime check does not yet probe Windows CRC32 instructions.");

            AssertContains(nativeRuntimeSource, "HashThreadFunc_ComputesOfficialXXH3DigestsForKnownVector", "Phase 95 native runtime coverage does not yet include the official XXH3 vector.");
            AssertContains(nativeRuntimeSource, "HashThreadFunc_ComputesOfficialCRC32CDigestForKnownVector", "Phase 95 native runtime coverage does not yet include the official CRC32C vector.");
            AssertContains(nativeRuntimeSource, "RunHashRequest_XXH3AndCRC32CUnknownIdsAreIgnoredAndKnownVariantsStayOrdered", "Phase 95 native runtime coverage does not yet include XXH3/CRC32C request normalization behavior.");
            AssertContains(nativeRuntimeSource, "HashThreadFunc_XXH3AndCRC32CRemainStableAcrossConcurrentRuns", "Phase 95 native runtime coverage does not yet include concurrent XXH3/CRC32C stability.");
            AssertContains(extensibilityUnitTests, "XXH3AndCRC32CIntegration_VendorsOfficialFixedVersions_AndAddsRuntimeCoverage", "Phase 95 unit tests do not yet gate the xxHash3/CRC32C extensibility seam.");
            AssertContains(securityUnitTests, "XXH3AndCRC32CIntegration_UsesOfficialProviders_AndCoversVectorsOrderingAndConcurrency", "Phase 95 unit tests do not yet gate the xxHash3/CRC32C hardening seam.");
            AssertContains(nativeRuntimeUnitTests, "HashThreadFunc_ComputesOfficialXXH3DigestsForKnownVector", "Phase 95 native-runtime framework tests do not yet require the official XXH3 vector coverage.");
            AssertContains(nativeRuntimeUnitTests, "HashThreadFunc_ComputesOfficialCRC32CDigestForKnownVector", "Phase 95 native-runtime framework tests do not yet require the official CRC32C vector coverage.");
            AssertContains(securityRegression, "XXH3 and CRC32C providers stay covered by hardening gates", "Phase 95 security regression does not yet gate the xxHash3/CRC32C coverage.");
        }, failures);

        Run("Phase 96 expands xxHash3 and CRC32C from presence checks into stronger runtime vector and concurrent multi-file coverage", () =>
        {
            string nativeRuntimeSource = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\HashEngineRuntimeTests.cpp");
            string extensibilityUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\HashExtensibilityRegressionUnitTests.cs");
            string securityUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\SecurityHardeningUnitTests.cs");
            string nativeRuntimeUnitTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\NativeRuntimeFrameworkUnitTests.cs");
            string securityRegression = ReadRepoFile(repoRoot, @"security-tests\SecurityRegression\Program.cs");

            AssertContains(nativeRuntimeSource, "HashThreadFunc_ComputesOfficialCRC32CBoundaryDigestsForKnownVectors", "Phase 96 native runtime coverage does not yet sweep official CRC32C boundary vectors.");
            AssertContains(nativeRuntimeSource, "RunHashRequest_XXH3AndCRC32CMultiFileConcurrentMatchesSingleRun", "Phase 96 native runtime coverage does not yet compare multi-file concurrent XXH3/CRC32C runs against a baseline.");
            AssertContains(nativeRuntimeSource, "8A9136AA", "Phase 96 native runtime coverage does not yet preserve the CRC32C zero-input vector.");
            AssertContains(nativeRuntimeSource, "62A8AB43", "Phase 96 native runtime coverage does not yet preserve the CRC32C all-0xFF vector.");
            AssertContains(nativeRuntimeSource, "113FDB5C", "Phase 96 native runtime coverage does not yet preserve the CRC32C descending-input vector.");
            AssertContains(nativeRuntimeSource, "D9963A56", "Phase 96 native runtime coverage does not yet preserve the CRC32C iSCSI vector.");

            AssertContains(extensibilityUnitTests, "HashThreadFunc_ComputesOfficialCRC32CBoundaryDigestsForKnownVectors", "Phase 96 extensibility tests do not yet gate the stronger CRC32C boundary vectors.");
            AssertContains(extensibilityUnitTests, "RunHashRequest_XXH3AndCRC32CMultiFileConcurrentMatchesSingleRun", "Phase 96 extensibility tests do not yet gate the stronger XXH3/CRC32C concurrent multi-file comparison.");
            AssertContains(securityUnitTests, "HashThreadFunc_ComputesOfficialCRC32CBoundaryDigestsForKnownVectors", "Phase 96 hardening tests do not yet gate the stronger CRC32C boundary vectors.");
            AssertContains(securityUnitTests, "RunHashRequest_XXH3AndCRC32CMultiFileConcurrentMatchesSingleRun", "Phase 96 hardening tests do not yet gate the stronger XXH3/CRC32C concurrent multi-file comparison.");
            AssertContains(nativeRuntimeUnitTests, "HashThreadFunc_ComputesOfficialCRC32CBoundaryDigestsForKnownVectors", "Phase 96 native-runtime framework tests do not yet require the stronger CRC32C boundary vectors.");
            AssertContains(nativeRuntimeUnitTests, "RunHashRequest_XXH3AndCRC32CMultiFileConcurrentMatchesSingleRun", "Phase 96 native-runtime framework tests do not yet require the stronger XXH3/CRC32C concurrent multi-file comparison.");
            AssertContains(securityRegression, "HashThreadFunc_ComputesOfficialCRC32CBoundaryDigestsForKnownVectors", "Phase 96 security regression does not yet track the stronger CRC32C boundary vectors.");
            AssertContains(securityRegression, "RunHashRequest_XXH3AndCRC32CMultiFileConcurrentMatchesSingleRun", "Phase 96 security regression does not yet track the stronger XXH3/CRC32C concurrent multi-file comparison.");
        }, failures);

        Run("Phase 97 switches native compiler inputs and resource code pages onto a shared UTF-8 toolchain", () =>
        {
            string nativeUtf8Targets = ReadRepoFile(repoRoot, @"NativeUtf8.targets");
            string legacyProject = ReadRepoFile(repoRoot, @"trunk\fileshash.vcxproj");
            string nativeCoreProject = ReadRepoFile(repoRoot, @"sub-proj\fHashNativeCore\fHashNativeCore.vcxproj");
            string runtimeTestsProject = ReadRepoFile(repoRoot, @"native-runtime-tests\FHash.NativeRuntimeTests\FHash.NativeRuntimeTests.vcxproj");
            string benchmarkProject = ReadRepoFile(repoRoot, @"native-benchmarks\FHash.NativeBenchmarks\FHash.NativeBenchmarks.vcxproj");
            string shellProject = ReadRepoFile(repoRoot, @"sub-proj\fHashShlExt\fHashShlExt.vcxproj");
            string securityRegression = ReadRepoFile(repoRoot, @"security-tests\SecurityRegression\Program.cs");
            string releaseMetadataTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\ReleaseMetadataUnitTests.cs");
            string mfcRc = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\fileshash.rc");
            string mfcRc2 = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\res\fileshash.rc2");
            string shellRc = ReadRepoFile(repoRoot, @"sub-proj\fHashShlExt\fHashShlExt.rc");

            AssertContains(nativeUtf8Targets, "<AdditionalOptions>/utf-8 %(AdditionalOptions)</AdditionalOptions>", "Phase 97 shared UTF-8 targets do not yet enable /utf-8.");
            AssertContains(nativeUtf8Targets, "<AdditionalOptions>/c65001 %(AdditionalOptions)</AdditionalOptions>", "Phase 97 shared UTF-8 targets do not yet enable /c65001.");

            foreach (string projectContents in new[] { legacyProject, nativeCoreProject, runtimeTestsProject, benchmarkProject, shellProject })
            {
                AssertContains(projectContents, "NativeUtf8.targets", "Phase 97 native project does not yet import the shared UTF-8 targets.");
                AssertDoesNotContain(projectContents, "/source-charset:.936", "Phase 97 native project still forces the old 936 source charset.");
                AssertDoesNotContain(projectContents, "/execution-charset:.936", "Phase 97 native project still forces the old 936 execution charset.");
                AssertDoesNotContain(projectContents, "/c936", "Phase 97 native project still forces the old 936 resource code page.");
            }

            AssertContains(mfcRc, "#pragma code_page(65001)", "Phase 97 legacy resource chain does not yet use UTF-8 code pages.");
            AssertContains(mfcRc2, "BLOCK \"080404b0\"", "Phase 97 legacy version resource block is not yet migrated to Unicode metadata.");
            AssertContains(mfcRc2, "VALUE \"Translation\", 0x804, 1200", "Phase 97 legacy version resource translation is not yet migrated to Unicode metadata.");
            AssertContains(shellRc, "#pragma code_page(65001)", "Phase 97 shell-extension resource chain does not yet use UTF-8 code pages.");
            AssertContains(shellRc, "VALUE \"Translation\", 0x804, 1200", "Phase 97 shell-extension translation metadata is not yet Unicode.");

            AssertContains(securityRegression, "strictUtf8.GetString(bytes)", "Phase 97 security regression reader does not yet prefer strict UTF-8 decoding before 936 fallback.");
            AssertContains(releaseMetadataTests, "NativeProjects_AndResourceChain_Are_Migrating_To_Utf8", "Phase 97 unit coverage does not yet gate the UTF-8 migration seams.");
        }, failures);

        Run("Phase 98 adds a fixed-version OpenSSL EVP family as the only active SHA-256/SHA-512 path", () =>
        {
            string registryCore = ReadRepoFile(repoRoot, @"trunk\source\Domain\HashAlgorithmRegistryCore.h");
            string digestRegistry = ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestOperationRegistry.cpp");
            string providerImplementation = ReadRepoFile(repoRoot, @"trunk\source\Runtime\Hash\OpenSslEvpHashProvider.cpp");
            string vendorTargets = ReadRepoFile(repoRoot, @"NativeOpenSslVendor.targets");
            string clrBridgeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashClrBridge\fHashClrBridge.vcxproj");
            string uwpBridgeProject = ReadRepoFile(repoRoot, @"sub-proj\fHashWinRtBridge\fHashWinRtBridge.vcxproj");
            string workflow = ReadRepoFile(repoRoot, @".github\workflows\windows-build.yml");
            string previewWorkflow = ReadRepoFile(repoRoot, @".github\workflows\winui-preview-build.yml");
            string readme = ReadRepoFile(repoRoot, @"README.md");
            string changelog = ReadRepoFile(repoRoot, @"CHANGELOG.md");
            string changelogZh = ReadRepoFile(repoRoot, @"CHANGELOG.zh-CN.md");
            string releaseMetadataTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\ReleaseMetadataUnitTests.cs");
            string extensibilityTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\HashExtensibilityRegressionUnitTests.cs");
            string nativeRuntimeFrameworkTests = ReadRepoFile(repoRoot, @"unit-tests\FHash.UnitTests\NativeRuntimeFrameworkUnitTests.cs");
            string licenseException = ReadRepoFile(repoRoot, @"LICENSE-OPENSSL-EXCEPTION.md");

            AssertContains(registryCore, "{ \"md5\", \"MD5 (Deprecated)\", true, false }", "Phase 98 is missing the deprecated MD5 compatibility descriptor.");
            AssertContains(registryCore, "{ \"sha1\", \"SHA1 (Deprecated)\", true, false }", "Phase 98 is missing the deprecated SHA1 compatibility descriptor.");
            AssertDoesNotContain(registryCore, "{ \"sha256\", \"SHA256 (Legacy)\", true, false }", "Phase 98 should have fully removed the legacy SHA256 descriptor.");
            AssertDoesNotContain(registryCore, "{ \"sha512\", \"SHA512 (Legacy)\", true, false }", "Phase 98 should have fully removed the legacy SHA512 descriptor.");
            AssertContains(registryCore, "{ \"openssl-sha-256\", \"SHA-256\", true, true }", "Phase 98 is missing the OpenSSL SHA-256 descriptor or default enablement.");
            AssertContains(registryCore, "{ \"openssl-sha-384\", \"SHA-384\", true, false }", "Phase 98 is missing the OpenSSL SHA-384 descriptor.");
            AssertContains(registryCore, "{ \"openssl-sha-512\", \"SHA-512\", true, true }", "Phase 98 is missing the OpenSSL SHA-512 descriptor or default enablement.");
            AssertContains(registryCore, "{ \"openssl-sha3-256\", \"SHA3-256\", true, true }", "Phase 98 is missing the OpenSSL SHA3-256 descriptor or default enablement.");
            AssertContains(registryCore, "{ \"openssl-sha3-384\", \"SHA3-384\", true, false }", "Phase 98 is missing the OpenSSL SHA3-384 descriptor.");
            AssertContains(registryCore, "{ \"openssl-sha3-512\", \"SHA3-512\", true, false }", "Phase 98 is missing the OpenSSL SHA3-512 descriptor.");
            AssertContains(registryCore, "{ \"openssl-blake2b-512\", \"BLAKE2b-512\", true, false }", "Phase 98 is missing the OpenSSL BLAKE2b-512 descriptor.");
            AssertContains(registryCore, "{ \"openssl-blake2s-256\", \"BLAKE2s-256\", true, false }", "Phase 98 is missing the OpenSSL BLAKE2s-256 descriptor.");
            AssertContains(registryCore, "{ \"openssl-shake128-256\", \"SHAKE128-256\", true, false }", "Phase 98 is missing the OpenSSL SHAKE128-256 descriptor.");
            AssertContains(registryCore, "{ \"openssl-shake256-512\", \"SHAKE256-512\", true, false }", "Phase 98 is missing the OpenSSL SHAKE256-512 descriptor.");
            AssertContains(digestRegistry, "InitializeOpenSslSha256DigestContext", "Phase 98 is missing the OpenSSL SHA-256 digest registration hook.");
            AssertContains(digestRegistry, "InitializeOpenSslSha384DigestContext", "Phase 98 is missing the OpenSSL SHA-384 digest registration hook.");
            AssertContains(digestRegistry, "InitializeOpenSslSha3_384DigestContext", "Phase 98 is missing the OpenSSL SHA3-384 digest registration hook.");
            AssertContains(digestRegistry, "InitializeOpenSslBlake2b_512DigestContext", "Phase 98 is missing the OpenSSL BLAKE2b-512 digest registration hook.");
            AssertContains(digestRegistry, "InitializeOpenSslBlake2s_256DigestContext", "Phase 98 is missing the OpenSSL BLAKE2s-256 digest registration hook.");
            AssertContains(digestRegistry, "InitializeOpenSslShake256_512DigestContext", "Phase 98 is missing the OpenSSL SHAKE256-512 digest registration hook.");
            AssertContains(providerImplementation, "EVP_MD_fetch", "Phase 98 OpenSSL provider no longer uses EVP fetch semantics.");
            AssertContains(providerImplementation, "GetCachedDigestImplementation", "Phase 98 OpenSSL provider no longer caches EVP digest definitions across file contexts.");
            AssertContains(providerImplementation, "EVP_DigestUpdate(hashContext.mdContext, data, dataLen) != 1", "Phase 98 OpenSSL provider does not yet treat EVP_DigestUpdate failure as a sticky error.");
            AssertContains(providerImplementation, "EVP_DigestFinalXOF", "Phase 98 OpenSSL provider no longer uses EVP XOF finalization.");
            AssertContains(providerImplementation, "OSSL_DIGEST_PARAM_SIZE", "Phase 98 OpenSSL provider no longer configures truncated digest output through OSSL params.");
            AssertContains(providerImplementation, "ConfigureOpenSslEvpFailureInjection", "Phase 98 OpenSSL provider does not yet expose failure injection hooks for runtime error-path coverage.");
            AssertContains(vendorTargets, "FHASH_WITH_OPENSSL3_VENDOR=1", "Phase 98 shared OpenSSL vendor targets no longer define the OpenSSL vendor flag.");
            AssertContains(clrBridgeProject, @"<FHashOpenSslAdditionalDependencies Condition=""'$(FHashOpenSslInstallRoot)'!=''"">$(FHashOpenSslLibDir)\libcrypto.lib;WS2_32.lib;GDI32.lib;ADVAPI32.lib;CRYPT32.lib;USER32.lib;</FHashOpenSslAdditionalDependencies>", "Phase 98 CLR bridge does not yet define explicit OpenSSL bridge-link dependencies.");
            AssertContains(clrBridgeProject, @"$(FHashOpenSslAdditionalDependencies)fHashWUINative.lib;fHashNativeCore.lib;Version.lib;%(AdditionalDependencies)", "Phase 98 CLR bridge does not yet inject explicit OpenSSL bridge-link dependencies into the active link configuration.");
            AssertContains(uwpBridgeProject, @"<FHashOpenSslAdditionalDependencies Condition=""'$(FHashOpenSslInstallRoot)'!=''"">$(FHashOpenSslLibDir)\libcrypto.lib;WS2_32.lib;GDI32.lib;ADVAPI32.lib;CRYPT32.lib;USER32.lib;</FHashOpenSslAdditionalDependencies>", "Phase 98 WinRT bridge does not yet define explicit OpenSSL bridge-link dependencies.");
            AssertContains(uwpBridgeProject, @"$(FHashOpenSslAdditionalDependencies)fHashUwpNative.lib;Version.lib;%(AdditionalDependencies)", "Phase 98 WinRT bridge does not yet inject explicit OpenSSL bridge-link dependencies into the active link configuration.");
            AssertContains(workflow, "build_openssl_vendor.ps1", "Phase 98 Windows build workflow no longer builds the vendored OpenSSL package.");
            AssertContains(workflow, "prepare-openssl-vendor-x64:", "Phase 98 Windows build workflow does not yet prepare the shared OpenSSL vendor artifact once.");
            AssertContains(workflow, "Restore cached OpenSSL vendor x64", "Phase 98 Windows build workflow does not yet restore the shared OpenSSL vendor cache.");
            AssertContains(workflow, "actions/cache@v4", "Phase 98 Windows build workflow does not yet cache the shared OpenSSL vendor build.");
            AssertContains(workflow, "name: FHash-openssl-vendor-x64", "Phase 98 Windows build workflow does not yet upload the shared OpenSSL vendor artifact.");
            AssertContains(workflow, "Download OpenSSL vendor x64 artifact", "Phase 98 Windows build workflow does not yet reuse the shared OpenSSL vendor artifact downstream.");
            AssertContains(workflow, "FHashOpenSslInstallRoot", "Phase 98 Windows build workflow no longer passes the OpenSSL install root.");
            AssertDoesNotContain(workflow, "build-winui-bridge-x64:", "Phase 98 Windows build workflow should no longer keep the WinUI preview build in the mainline pipeline.");
            AssertContains(previewWorkflow, "workflow_dispatch:", "Phase 98 dedicated WinUI preview workflow should stay manual-only.");
            AssertContains(previewWorkflow, "$env:FHashOpenSslInstallRoot = $openSslRoot", "Phase 98 WinUI preview workflow does not yet export the OpenSSL install root into the WinUI preview environment.");
            AssertContains(previewWorkflow, "$env:OPENSSL_VENDOR_INSTALL_ROOT = $openSslRoot", "Phase 98 WinUI preview workflow does not yet export the shared OpenSSL vendor root into the WinUI preview environment.");
            AssertContains(previewWorkflow, "LHash-winui-preview-x64", "Phase 98 WinUI preview workflow no longer publishes the preview artifact.");
        AssertContains(licenseException, "OpenSSL Linking Exception", "Phase 98 no longer carries the OpenSSL linking exception note.");
            AssertContains(readme, "GPL-2.0-only with an OpenSSL linking exception", "Phase 98 README no longer documents the OpenSSL licensing exception.");
            AssertContains(readme, "SHA-256", "Phase 98 README no longer documents the OpenSSL SHA-2 family.");
            AssertContains(readme, "SHA-384", "Phase 98 README no longer documents the OpenSSL SHA-384 family.");
            AssertContains(readme, "BLAKE2b-512", "Phase 98 README no longer documents the supported OpenSSL BLAKE2b variant.");
            AssertContains(changelog, "OpenSSL 3 EVP", "Phase 98 changelog no longer records the OpenSSL EVP family.");
            AssertContains(changelogZh, "OpenSSL 3 EVP", "Phase 98 Chinese changelog no longer records the OpenSSL EVP family.");
            AssertContains(releaseMetadataTests, "OpenSslVendorPipeline_AndLinkingException_Are_WiredIntoTheMaintainedBuild", "Phase 98 release metadata coverage no longer guards the OpenSSL vendor pipeline.");
            AssertContains(extensibilityTests, "OpenSslEvpIntegration_VendorsOfficialFixedVersion_AndOwnsTheOnlyActiveSha256AndSha512Ids", "Phase 98 extensibility coverage no longer guards the OpenSSL-only SHA-256/SHA-512 seam.");
            AssertContains(nativeRuntimeFrameworkTests, "RunHashRequest_OpenSslDigestUpdateFailureProducesExplicitFileError", "Phase 98 runtime coverage no longer guards OpenSSL EVP update-failure propagation.");
            AssertContains(nativeRuntimeFrameworkTests, "RunHashRequest_OpenSslDigestFinalizeFailureProducesExplicitFileError", "Phase 98 runtime coverage no longer guards OpenSSL EVP finalize-failure propagation.");
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
        string path = ResolveRepoPath(repoRoot, relativePath);
        return File.ReadAllText(path, DetectEncoding(path));
    }

    private static string ResolveRepoPath(string repoRoot, string relativePath)
    {
        string livePath = Path.Combine(repoRoot, relativePath);
        if (File.Exists(livePath) || Directory.Exists(livePath))
        {
            return livePath;
        }

        foreach ((string livePrefix, string archivePrefix) in GetArchivePathMappings())
        {
            if (!relativePath.StartsWith(livePrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string archivedRelativePath = archivePrefix + relativePath.Substring(livePrefix.Length);
            string archivedPath = Path.Combine(repoRoot, archivedRelativePath);
            if (File.Exists(archivedPath) || Directory.Exists(archivedPath))
            {
                return archivedPath;
            }
        }

        return livePath;
    }

    private static IEnumerable<(string LivePrefix, string ArchivePrefix)> GetArchivePathMappings()
    {
        yield return (@"trunk\fHashWUIWap\", @"archive\legacy-platforms\trunk\fHashWUIWap\");
        yield return (@"trunk\fHashUwpWap\", @"archive\legacy-platforms\trunk\fHashUwpWap\");
        yield return (@"trunk\source\WinUWP\", @"archive\legacy-platforms\trunk\source\WinUWP\");
        yield return (@"trunk\source\OSXUI\", @"archive\legacy-platforms\trunk\source\OSXUI\");
        yield return (@"sub-proj\fHashWinRtBridge\", @"archive\legacy-platforms\sub-proj\fHashWinRtBridge\");
        yield return (@"sub-proj\fHashUwpNative\", @"archive\legacy-platforms\sub-proj\fHashUwpNative\");
        yield return (@"sub-proj\fHashUwpShellExt\", @"archive\legacy-platforms\sub-proj\fHashUwpShellExt\");
        yield return (@"sub-proj\fHashWUIShellExt\", @"archive\legacy-platforms\sub-proj\fHashWUIShellExt\");
    }

    private static string ReadResultDigestAccessSeams(string repoRoot)
    {
        return string.Join(
            "\r\n",
            ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestAccess.h"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestMetadataAccess.h"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestStateAccess.h"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\ResultDigestValueAccess.h"),
            ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ResultDigestTypeMetadataCompat.h"),
            ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ResultDigestTypeStateCompat.h"),
            ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ResultDigestTypeValueCompat.h"));
    }

    private static string ReadLegacyThreadDataAccessSeams(string repoRoot)
    {
        return string.Join(
            "\r\n",
            ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataAccess.h"),
            ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataExecutionAccess.h"),
            ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataInputAccess.h"),
            ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\ThreadDataResultAccess.h"));
    }

    private static string ReadHashAlgorithmRegistrySeams(string repoRoot)
    {
        return string.Join(
            "\r\n",
            ReadRepoFile(repoRoot, @"trunk\source\Domain\HashAlgorithmRegistryCore.h"),
            ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\HashAlgorithmTypeCompat.h"));
    }

    private static string ReadHashDigestOperationRegistrySeams(string repoRoot)
    {
        return string.Join(
            "\r\n",
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestOperationRegistry.h"),
            ReadRepoFile(repoRoot, @"trunk\source\Runtime\HashDigestOperationRegistryRuntime.h"),
            ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\HashDigestOperationTypeCompat.h"));
    }

    private static string ReadHashEngineImplementation(string repoRoot)
    {
        return string.Join(
            "\r\n",
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\HashThreadEntry.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\HashThreadEntryRuntime.h"),
            ReadRepoFile(repoRoot, @"trunk\source\LegacyCompat\HashThreadEntryProjection.h"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineInternal.h"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileRunner.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptWorkflow.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileResultWorkflow.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptStateOps.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashSchedulerDispatch.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobExecutionPlan.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashJobLifecycleWorkflow.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestExecution.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestContextOps.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestOperationRegistry.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestLifecycle.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestQueue.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestPipeline.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestSinglePass.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashDigestUpdater.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileSizeAccounting.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashProgressTracker.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashScheduler.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashPreScanSizeProbe.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashPreScanSizeAccounting.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashPreScanWorkflow.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashPreparationWorkflow.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashEnginePreparation.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngineResult.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultPublisher.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashResultEventWorkflow.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashErrorResultWorkflow.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashSuccessfulFileCompletionWorkflow.cpp"),
            ReadRepoFile(repoRoot, @"trunk\source\Common\HashFileAttemptCompletionWorkflow.cpp"));
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

        try
        {
            Encoding strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            _ = strictUtf8.GetString(bytes);
            return strictUtf8;
        }
        catch (ArgumentException)
        {
            // Fall through to the legacy code-page reader for historical files
            // that have not yet been migrated.
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

    private static void AssertContainsAny(string content, IReadOnlyList<string> expectedFragments, string failureMessage)
    {
        foreach (string expected in expectedFragments)
        {
            if (content.Contains(expected, StringComparison.Ordinal))
            {
                return;
            }
        }

        throw new InvalidOperationException(failureMessage);
    }

    private static void AssertDoesNotContain(string content, string expected, string failureMessage)
    {
        if (content.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(failureMessage);
        }
    }

    private static void AssertFileMissing(string repoRoot, string relativePath, string failureMessage)
    {
        if (File.Exists(Path.Combine(repoRoot, relativePath)))
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

