namespace FHash.UnitTests;

public sealed class HashContractUnitTests
{
    [Fact]
    public void HashRequest_DefinesStableFileAlgorithmAndUppercaseContract()
    {
        string request = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashRequest.h");

        Assert.Contains("struct HashRequest", request, StringComparison.Ordinal);
        Assert.Contains("TStrVector files;", request, StringComparison.Ordinal);
        Assert.Contains("std::vector<ResultDigestType> algorithms;", request, StringComparison.Ordinal);
        Assert.Contains("bool uppercaseDigest;", request, StringComparison.Ordinal);
        Assert.Contains("VisitHashRequestFiles(const HashRequest& request", request, StringComparison.Ordinal);
        Assert.Contains("VisitHashRequestAlgorithms(const HashRequest& request", request, StringComparison.Ordinal);
        Assert.Contains("HasHashRequestAlgorithm(const HashRequest& request, ResultDigestType digestType)", request, StringComparison.Ordinal);
        Assert.Contains("CreateHashRequest(const ThreadData& threadData)", request, StringComparison.Ordinal);
    }

    [Fact]
    public void HashResult_ProjectsStableCoreAndDigestContract()
    {
        string global = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\Global.h");
        string result = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashResult.h");
        string projection = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashResultProjection.h");
        string resultNetProjection = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\ResultNetProjection.h");
        string search = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashResultSearch.h");
        string metadata = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\ResultDigestMetadataAccess.h");

        Assert.Contains("enum ResultDigestType", global, StringComparison.Ordinal);
        Assert.Contains("struct HashDigestResult", global, StringComparison.Ordinal);
        Assert.Contains("struct HashFileMeta", global, StringComparison.Ordinal);
        Assert.Contains("struct HashResult", global, StringComparison.Ordinal);
        Assert.Contains("std::vector<sunjwbase::tstring> values;", global, StringComparison.Ordinal);
        Assert.Contains("std::vector<bool> enabled;", global, StringComparison.Ordinal);
        Assert.DoesNotContain("struct ResultDigestCompatibilityFields", global, StringComparison.Ordinal);
        Assert.DoesNotContain("const ResultData *sourceResult;", result, StringComparison.Ordinal);
        Assert.Contains("std::vector<HashDigestResult> digests;", global, StringComparison.Ordinal);
        Assert.Contains("const HashResult& ProjectHashResult(const HashResult& result)", result, StringComparison.Ordinal);
        Assert.Contains("ProjectHashResult(const ResultData& result)", result, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/ResultNetProjection.h\"", projection, StringComparison.Ordinal);
        Assert.DoesNotContain("#include \"Common/ResultDataProjection.h\"", projection, StringComparison.Ordinal);
        Assert.Contains("AssignHashResultCoreToNet", projection, StringComparison.Ordinal);
        Assert.Contains("AssignHashResultDigestsToNet", projection, StringComparison.Ordinal);
        Assert.Contains("ProjectHashResultToNet(const HashResult& result, TStringConverter convertString)", projection, StringComparison.Ordinal);
        Assert.Contains("AssignResultDigestToNet(TResultDataNet resultDataNet, ResultDigestType digestType, TResultString digestValue)", resultNetProjection, StringComparison.Ordinal);
        Assert.Contains("ConvertResultStateToNet(ResultState resultState)", resultNetProjection, StringComparison.Ordinal);
        Assert.Contains("NormalizeHashResultPathSearchText(const sunjwbase::tstring& pathText)", search, StringComparison.Ordinal);
        Assert.Contains("NormalizeHashResultDigestSearchText(const sunjwbase::tstring& digestText)", search, StringComparison.Ordinal);
        Assert.Contains("VisitProjectedHashResults(const HashResultList& resultList, TStringConverter convertString, TResultVisitor visitor)", projection, StringComparison.Ordinal);
        Assert.Contains("VisitHashResults(resultList, [&](const HashResult& hashResult)", projection, StringComparison.Ordinal);
        Assert.Contains("VisitMatchingHashResults(resultList, predicate, [&](const HashResult& hashResult)", projection, StringComparison.Ordinal);
        Assert.Contains("CountMatchingHashResults(resultList, predicate)", projection, StringComparison.Ordinal);
        Assert.Contains("CreateProjectedDigestMatchingHashResults(const HashResultList& resultList, const sunjwbase::tstring& digestText", projection, StringComparison.Ordinal);
        Assert.Contains("HashResultContainsDigest(const HashResult& result, const sunjwbase::tstring& digestText)", search, StringComparison.Ordinal);
        Assert.Contains("HashResultMatchesDigestText(const HashResult& result, const sunjwbase::tstring& digestText)", search, StringComparison.Ordinal);
        Assert.Contains("HashResultMatchesPathText(const HashResult& result, const sunjwbase::tstring& pathText)", search, StringComparison.Ordinal);
        Assert.Contains("HashResultMatchesPathAndDigestText(const HashResult& result, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText)", search, StringComparison.Ordinal);
        Assert.Contains("VisitPathAndDigestMatchingHashResults(const HashResultList& resultList, const sunjwbase::tstring& pathText, const sunjwbase::tstring& digestText, THashResultVisitor visitor)", search, StringComparison.Ordinal);
        Assert.Contains("VisitResultDigestMetadataValues(result", result, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestMetadataStableName(const ResultDigestMetadata& digestMetadata)", metadata, StringComparison.Ordinal);
    }

    [Fact]
    public void ProgressEvent_DefinesSemanticLifecycleSurface_AndObserverCompatibilityDispatch()
    {
        string progressEvent = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\ProgressEvent.h");
        string progressSink = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashProgressSink.h");
        string observer = RepositoryTestContext.ReadTextFile(@"trunk\source\Adapters\UiBridge\HashEngineObserver.h");
        string legacyObserverPath = Path.Combine(RepositoryTestContext.RepoRoot, @"trunk\source\Common\HashEngineObserver.h");
        string legacyBridgePath = Path.Combine(RepositoryTestContext.RepoRoot, @"trunk\source\Common\HashEngineBridge.h");
        string legacyUiBridgeBasePath = Path.Combine(RepositoryTestContext.RepoRoot, @"trunk\source\Common\UIBridgeBase.h");

        Assert.Contains("enum ProgressEventType", progressEvent, StringComparison.Ordinal);
        Assert.Contains("PROGRESS_EVENT_JOB_PREPARING", progressEvent, StringComparison.Ordinal);
        Assert.Contains("PROGRESS_EVENT_FILE_HASH_READY", progressEvent, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateResultProgressEvent(ProgressEventType eventType, const ResultData& result)", progressEvent, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateFileStartedProgressEvent(const ResultData& result)", progressEvent, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateFileMetaReadyProgressEvent(const ResultData& result)", progressEvent, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateFileHashReadyProgressEvent(const ResultData& result, bool uppercaseDigest)", progressEvent, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateFileFailedProgressEvent(const ResultData& result)", progressEvent, StringComparison.Ordinal);
        Assert.Contains("CreateFileHashReadyProgressEvent(const HashResult& result, bool uppercaseDigest)", progressEvent, StringComparison.Ordinal);
        Assert.Contains("CreateResultProgressEvent(ProgressEventType eventType, const HashResult& result)", progressEvent, StringComparison.Ordinal);

        Assert.Contains("class HashProgressSink", progressSink, StringComparison.Ordinal);
        Assert.Contains("virtual int progressMax() = 0;", progressSink, StringComparison.Ordinal);
        Assert.Contains("virtual void onProgressEvent(const ProgressEvent& progressEvent) = 0;", progressSink, StringComparison.Ordinal);

        Assert.Contains("class HashEngineObserver: public HashProgressSink", observer, StringComparison.Ordinal);
        Assert.Contains("virtual void onProgressEvent(const ProgressEvent& progressEvent)", observer, StringComparison.Ordinal);
        Assert.Contains("virtual void onJobPreparing() = 0;", observer, StringComparison.Ordinal);
        Assert.Contains("virtual void onJobPreparationFinished() = 0;", observer, StringComparison.Ordinal);
        Assert.Contains("virtual void onJobCancelled() = 0;", observer, StringComparison.Ordinal);
        Assert.Contains("virtual void onJobCompleted() = 0;", observer, StringComparison.Ordinal);
        Assert.Contains("virtual void onFileResultEvent(const HashResult& result,", observer, StringComparison.Ordinal);
        Assert.Contains("virtual int queryProgressMax() = 0;", observer, StringComparison.Ordinal);
        Assert.Contains("virtual void onTotalProgressValue(int value) = 0;", observer, StringComparison.Ordinal);
        Assert.Contains("onFileResultEvent(progressEvent.result, progressEvent.type, progressEvent.uppercaseDigest);", observer, StringComparison.Ordinal);
        Assert.Contains("onTotalProgressValue(progressEvent.value);", observer, StringComparison.Ordinal);
        Assert.DoesNotContain("virtual void showFileName(const HashResult& result) = 0;", observer, StringComparison.Ordinal);
        Assert.DoesNotContain("virtual void updateProgWhole(int value) = 0;", observer, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateCompatibilityResultData(result);", observer, StringComparison.Ordinal);
        Assert.False(File.Exists(legacyObserverPath));
        Assert.False(File.Exists(legacyBridgePath));
        Assert.False(File.Exists(legacyUiBridgeBasePath));
    }

    [Fact]
    public void HashEngine_StartsFromHashRequest_AndEmitsProgressEvents()
    {
        string global = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\Global.h");
        string executionContext = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashExecutionContext.h");
        string threadExecutionAccess = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\ThreadDataExecutionAccess.h");
        string engineHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEngine.h");
        string engine = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEngine.cpp");
        string fileRunner = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashFileRunner.cpp");
        string digestQueue = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestQueue.cpp");
        string digestPipeline = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestPipeline.cpp");
        string digestExecution = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestExecution.cpp");
        string digestExecutionHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestExecution.h");
        string digestRuntimePlan = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestRuntimePlan.cpp");
        string digestRuntimePlanHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestRuntimePlan.h");
        string digestCompletion = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestCompletion.cpp");
        string digestCompletionHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestCompletion.h");
        string digestExecutionMode = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestExecutionMode.cpp");
        string digestExecutionModeHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestExecutionMode.h");
        string digestBufferPlan = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestBufferPlan.cpp");
        string digestBufferPlanHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestBufferPlan.h");
        string digestQueueHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestQueue.h");
        string digestQueuePlan = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestQueuePlan.cpp");
        string digestQueuePlanHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestQueuePlan.h");
        string digestSinglePass = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestSinglePass.cpp");
        string digestUpdater = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashDigestUpdater.cpp");
        string fileAttemptWorkflow = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashFileAttemptWorkflow.cpp");
        string jobExecutionPlan = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashJobExecutionPlan.cpp");
        string jobExecutionPlanHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashJobExecutionPlan.h");
        string progressTracker = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashProgressTracker.cpp");
        string scheduler = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashScheduler.cpp");
        string schedulerPlan = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashSchedulerPlan.cpp");
        string schedulerPlanHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashSchedulerPlan.h");
        string schedulerDispatch = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashSchedulerDispatch.cpp");
        string preparation = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEnginePreparation.cpp");
        string preparationPlan = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashPreparationPlan.cpp");
        string preparationPlanHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashPreparationPlan.h");
        string result = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEngineResult.cpp");
        string fileVersionResolver = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashFileVersionResolver.cpp");
        string fileVersionResolverHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashFileVersionResolver.h");
        string publisher = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashResultPublisher.cpp");
        string internalHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEngineInternal.h");

        Assert.Contains("class HashProgressSink;", global, StringComparison.Ordinal);
        Assert.Contains("HashProgressSink *observer;", global, StringComparison.Ordinal);
        Assert.Contains("struct HashExecutionPreferenceState", global, StringComparison.Ordinal);
        Assert.Contains("struct HashCancellationState", global, StringComparison.Ordinal);
        Assert.Contains("struct HashJobState", global, StringComparison.Ordinal);
        Assert.Contains("std::atomic<bool> stopRequested;", global, StringComparison.Ordinal);
        Assert.Contains("std::atomic<bool> working;", global, StringComparison.Ordinal);
        Assert.Contains("HashJobState jobState;", global, StringComparison.Ordinal);
        Assert.Contains("std::vector<bool> enabled;", global, StringComparison.Ordinal);
        Assert.Contains("struct HashExecutionContext", executionContext, StringComparison.Ordinal);
        Assert.Contains("CreateHashExecutionContext(ThreadData& threadData)", executionContext, StringComparison.Ordinal);
        Assert.Contains("GetHashExecutionProgressSink(const HashExecutionContext& executionContext)", executionContext, StringComparison.Ordinal);
        Assert.Contains("ShouldStopHashExecution(const HashExecutionContext& executionContext)", executionContext, StringComparison.Ordinal);
        Assert.Contains("AppendHashExecutionResult(HashExecutionContext& executionContext)", executionContext, StringComparison.Ordinal);
        Assert.Contains("HashJobState *jobState;", executionContext, StringComparison.Ordinal);
        Assert.Contains("HashCancellationState *cancellationState;", executionContext, StringComparison.Ordinal);
        Assert.Contains("SetThreadDataObserver(ThreadData& threadData, HashProgressSink *observer)", threadExecutionAccess, StringComparison.Ordinal);
        Assert.Contains("GetThreadDataObserver(const ThreadData& threadData)", threadExecutionAccess, StringComparison.Ordinal);
        Assert.Contains("GetThreadDataHashExecutionPreferenceState(const ThreadData& threadData)", threadExecutionAccess, StringComparison.Ordinal);
        Assert.Contains("GetThreadDataHashCancellationState(const ThreadData& threadData)", threadExecutionAccess, StringComparison.Ordinal);
        Assert.Contains("GetThreadDataHashJobState(const ThreadData& threadData)", threadExecutionAccess, StringComparison.Ordinal);
        Assert.Contains("struct HashExecutionContext;", engineHeader, StringComparison.Ordinal);
        Assert.Contains("int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request);", engineHeader, StringComparison.Ordinal);
        Assert.Contains("int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request)", engine, StringComparison.Ordinal);
        Assert.Contains("HashRequest request = CreateHashRequest(*thrdData);", engine, StringComparison.Ordinal);
        Assert.Contains("HashExecutionContext executionContext = CreateHashExecutionContext(*thrdData);", engine, StringComparison.Ordinal);
        Assert.Contains("return RunHashRequest(&executionContext, request);", engine, StringComparison.Ordinal);
        Assert.Contains("InitializeHashJobExecutionPlan(request, &executionPlan);", engine, StringComparison.Ordinal);
        Assert.Contains("ULLongVector fSizes(GetHashRequestFileCount(request));", engine, StringComparison.Ordinal);
        Assert.Contains("RunHashScheduler(executionContext, request, executionPlan, isSizeCaled, fSizes)", engine, StringComparison.Ordinal);
        Assert.Contains("ResetHashExecutionTotalSize(*executionContext);", engine, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateCancelledProgressEvent());", engine, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateCompletedProgressEvent());", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("FileExecutionState executionState = { 0 };", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("static bool ProcessOpenedFileHashing(", engine, StringComparison.Ordinal);

        Assert.Contains("bool RunHashScheduler(HashExecutionContext *executionContext, const HashRequest& request, const HashJobExecutionPlan& executionPlan, bool isSizeCaled, ULLongVector& fSizes)", scheduler, StringComparison.Ordinal);
        Assert.Contains("executionState.executionPlan = executionPlan;", scheduler, StringComparison.Ordinal);
        Assert.Contains("ExecuteScheduledHashRequestFiles(executionContext, request, isSizeCaled, fSizes, &executionState", scheduler, StringComparison.Ordinal);
        Assert.Contains("FileExecutionState executionState = { 0 };", scheduler, StringComparison.Ordinal);
        Assert.Contains("const HashSchedulerPlan& schedulerPlan = GetHashJobSchedulerPlan(executionState.executionPlan);", scheduler, StringComparison.Ordinal);
        Assert.Contains("ThreadPool threadPool(GetHashSchedulerWorkerThreadCount(schedulerPlan));", scheduler, StringComparison.Ordinal);
        Assert.Contains("bool ExecuteScheduledHashRequestFiles(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, ULLongVector& fSizes, FileExecutionState *executionState", schedulerDispatch, StringComparison.Ordinal);
        Assert.Contains("VisitHashRequestFiles(request", schedulerDispatch, StringComparison.Ordinal);
        Assert.Contains("RunFileHashAttempt(executionContext, request, fileIndex, fullPath, isSizeCaled, fSizes", schedulerDispatch, StringComparison.Ordinal);

        Assert.Contains("bool RunFileHashAttempt(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const tstring& fullPath", fileRunner, StringComparison.Ordinal);
        Assert.Contains("ExecuteFileHashAttemptWorkflow(executionContext, request, fileIndex, fullPath, isSizeCaled, fSizes, executionState", fileRunner, StringComparison.Ordinal);
        Assert.DoesNotContain("FileExecutionState executionState = { 0 };", fileRunner, StringComparison.Ordinal);
        Assert.Contains("bool ExecuteFileHashAttemptWorkflow(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const sunjwbase::tstring& fullPath", fileAttemptWorkflow, StringComparison.Ordinal);
        Assert.Contains("YieldHashThread();", fileAttemptWorkflow, StringComparison.Ordinal);
        Assert.Contains("ShouldStopHashExecution(*executionContext)", fileAttemptWorkflow, StringComparison.Ordinal);
        Assert.Contains("bool wasStopped = ProcessOpenedFileHashing(executionContext, request, result, fileIndex, isSizeCaled, fSizes, executionState", fileAttemptWorkflow, StringComparison.Ordinal);
        Assert.Contains("CompleteFileAttempt(executionContext, request, result, fileIndex, isSizeCaled, *executionState);", fileAttemptWorkflow, StringComparison.Ordinal);
        Assert.Contains("bool ProcessOpenedFileHashing(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex,", digestPipeline, StringComparison.Ordinal);
        Assert.Contains("HashDigestRuntimePlan digestRuntimePlan = CreateHashDigestRuntimePlan(executionState->executionPlan);", digestPipeline, StringComparison.Ordinal);
        Assert.Contains("unsigned int preferredBufferLength = GetHashDigestRuntimePreferredBufferLength(digestRuntimePlan);", digestPipeline, StringComparison.Ordinal);
        Assert.Contains("bool ExecuteOpenedFileDigestUpdate(HashExecutionContext *executionContext, const HashDigestRuntimePlan& digestRuntimePlan, uint64_t fsize, bool isSizeCaled,", digestExecutionHeader, StringComparison.Ordinal);
        Assert.Contains("bool ExecuteOpenedFileDigestUpdate(HashExecutionContext *executionContext, const HashDigestRuntimePlan& digestRuntimePlan, uint64_t fsize, bool isSizeCaled,", digestExecution, StringComparison.Ordinal);
        Assert.Contains("const DigestUpdateRequest& digestUpdateRequest = GetHashDigestRuntimeUpdateRequest(digestRuntimePlan);", digestExecution, StringComparison.Ordinal);
        Assert.Contains("const HashDigestQueuePlan& digestQueuePlan = GetHashDigestRuntimeQueuePlan(digestRuntimePlan);", digestExecution, StringComparison.Ordinal);
        Assert.Contains("if (IsParallelHashDigestExecutionMode(digestExecutionMode))", digestExecution, StringComparison.Ordinal);
        Assert.Contains("bool ProcessOpenedFileHashingParallel(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fileSize, bool isSizeCaled,", digestQueue, StringComparison.Ordinal);
        Assert.Contains("const HashDigestQueuePlan& digestQueuePlan", digestQueue, StringComparison.Ordinal);
        Assert.Contains("bool ProcessOpenedFileHashingSinglePass(HashExecutionContext *executionContext, const DigestUpdateRequest& digestUpdateRequest, uint64_t fsize, bool isSizeCaled,", digestSinglePass, StringComparison.Ordinal);
        Assert.Contains("bool ReadDigestDataBuffer(FileExecutionState *executionState, DigestDataBuffer& dataBuffer)", digestQueue, StringComparison.Ordinal);
        Assert.Contains("unsigned int NormalizeDigestDataBufferPreferredLength(unsigned int preferredLength);", digestQueueHeader, StringComparison.Ordinal);
        Assert.Contains("unsigned int NormalizeDigestDataBufferPreferredLength(unsigned int preferredLength)", digestQueue, StringComparison.Ordinal);
        Assert.Contains("uint64_t CalculateFileChunkIterations(uint64_t fileSize, unsigned int preferredLength)", digestQueue, StringComparison.Ordinal);
        Assert.DoesNotContain("static unsigned int preflen;", digestQueueHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("void SetDigestDataBufferPreferredLength(unsigned int preferredLength);", digestQueueHeader, StringComparison.Ordinal);
        Assert.Contains("UpdateDigestContextsParallel(digestUpdateRequest", digestQueue, StringComparison.Ordinal);
        Assert.Contains("UpdateDigestContextsParallel(digestUpdateRequest", digestQueue, StringComparison.Ordinal);
        Assert.Contains("queueDataBuffer.size() < GetHashDigestQueueMaxBufferedChunkCount(digestQueuePlan)", digestQueue, StringComparison.Ordinal);
        Assert.Contains("ExecuteOpenedFileDigestUpdate(executionContext, digestRuntimePlan, fsize, isSizeCaled, executionState", digestPipeline, StringComparison.Ordinal);
        Assert.Contains("UpdateDigestContextsSequential(digestUpdateRequest", digestSinglePass, StringComparison.Ordinal);
        Assert.Contains("DigestUpdateRequest CreateDigestUpdateRequest(const HashRequest& request)", digestUpdater, StringComparison.Ordinal);
        Assert.Contains("enum HashDigestExecutionMode", digestExecutionModeHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestExecutionMode ResolveHashDigestExecutionMode(const HashRequest& request);", digestExecutionModeHeader, StringComparison.Ordinal);
        Assert.Contains("bool IsParallelHashDigestExecutionMode(HashDigestExecutionMode executionMode);", digestExecutionModeHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestExecutionMode ResolveHashDigestExecutionMode(const HashRequest& request)", digestExecutionMode, StringComparison.Ordinal);
        Assert.Contains("return HASH_DIGEST_EXECUTION_MODE_PARALLEL;", digestExecutionMode, StringComparison.Ordinal);
        Assert.Contains("struct HashDigestBufferPlan", digestBufferPlanHeader, StringComparison.Ordinal);
        Assert.Contains("unsigned int preferredBufferLength;", digestBufferPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestBufferPlan CreateHashDigestBufferPlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode);", digestBufferPlanHeader, StringComparison.Ordinal);
        Assert.Contains("unsigned int GetHashDigestBufferPreferredLength(const HashDigestBufferPlan& digestBufferPlan);", digestBufferPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestBufferPlan CreateHashDigestBufferPlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode)", digestBufferPlan, StringComparison.Ordinal);
        Assert.Contains("digestBufferPlan.preferredBufferLength = 1048576;", digestBufferPlan, StringComparison.Ordinal);
        Assert.Contains("unsigned int GetHashDigestBufferPreferredLength(const HashDigestBufferPlan& digestBufferPlan)", digestBufferPlan, StringComparison.Ordinal);
        Assert.Contains("struct HashDigestQueuePlan", digestQueuePlanHeader, StringComparison.Ordinal);
        Assert.Contains("size_t maxBufferedChunkCount;", digestQueuePlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestQueuePlan CreateHashDigestQueuePlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode);", digestQueuePlanHeader, StringComparison.Ordinal);
        Assert.Contains("size_t GetHashDigestQueueMaxBufferedChunkCount(const HashDigestQueuePlan& digestQueuePlan);", digestQueuePlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestQueuePlan CreateHashDigestQueuePlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode)", digestQueuePlan, StringComparison.Ordinal);
        Assert.Contains("digestQueuePlan.maxBufferedChunkCount = 4;", digestQueuePlan, StringComparison.Ordinal);
        Assert.Contains("size_t GetHashDigestQueueMaxBufferedChunkCount(const HashDigestQueuePlan& digestQueuePlan)", digestQueuePlan, StringComparison.Ordinal);
        Assert.Contains("struct HashJobExecutionPlan", jobExecutionPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestExecutionMode digestExecutionMode;", jobExecutionPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestBufferPlan digestBufferPlan;", jobExecutionPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestQueuePlan digestQueuePlan;", jobExecutionPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashPreparationPlan preparationPlan;", jobExecutionPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashSchedulerPlan schedulerPlan;", jobExecutionPlanHeader, StringComparison.Ordinal);
        Assert.Contains("void InitializeHashJobExecutionPlan(const HashRequest& request, HashJobExecutionPlan *executionPlan);", jobExecutionPlanHeader, StringComparison.Ordinal);
        Assert.Contains("const DigestUpdateRequest& GetHashJobDigestUpdateRequest(const HashJobExecutionPlan& executionPlan);", jobExecutionPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestExecutionMode GetHashJobDigestExecutionMode(const HashJobExecutionPlan& executionPlan);", jobExecutionPlanHeader, StringComparison.Ordinal);
        Assert.Contains("const HashDigestBufferPlan& GetHashJobDigestBufferPlan(const HashJobExecutionPlan& executionPlan);", jobExecutionPlanHeader, StringComparison.Ordinal);
        Assert.Contains("const HashDigestQueuePlan& GetHashJobDigestQueuePlan(const HashJobExecutionPlan& executionPlan);", jobExecutionPlanHeader, StringComparison.Ordinal);
        Assert.Contains("const HashPreparationPlan& GetHashJobPreparationPlan(const HashJobExecutionPlan& executionPlan);", jobExecutionPlanHeader, StringComparison.Ordinal);
        Assert.Contains("const HashSchedulerPlan& GetHashJobSchedulerPlan(const HashJobExecutionPlan& executionPlan);", jobExecutionPlanHeader, StringComparison.Ordinal);
        Assert.Contains("executionPlan->digestUpdateRequest = CreateDigestUpdateRequest(request);", jobExecutionPlan, StringComparison.Ordinal);
        Assert.Contains("executionPlan->digestExecutionMode = ResolveHashDigestExecutionMode(request);", jobExecutionPlan, StringComparison.Ordinal);
        Assert.Contains("executionPlan->digestBufferPlan = CreateHashDigestBufferPlan(request, executionPlan->digestExecutionMode);", jobExecutionPlan, StringComparison.Ordinal);
        Assert.Contains("executionPlan->digestQueuePlan = CreateHashDigestQueuePlan(request, executionPlan->digestExecutionMode);", jobExecutionPlan, StringComparison.Ordinal);
        Assert.Contains("executionPlan->preparationPlan = CreateHashPreparationPlan(request);", jobExecutionPlan, StringComparison.Ordinal);
        Assert.Contains("executionPlan->schedulerPlan = CreateHashSchedulerPlan(request, executionPlan->digestExecutionMode);", jobExecutionPlan, StringComparison.Ordinal);
        Assert.Contains("return executionPlan.digestUpdateRequest;", jobExecutionPlan, StringComparison.Ordinal);
        Assert.Contains("return executionPlan.digestExecutionMode;", jobExecutionPlan, StringComparison.Ordinal);
        Assert.Contains("return executionPlan.digestBufferPlan;", jobExecutionPlan, StringComparison.Ordinal);
        Assert.Contains("return executionPlan.digestQueuePlan;", jobExecutionPlan, StringComparison.Ordinal);
        Assert.Contains("return executionPlan.preparationPlan;", jobExecutionPlan, StringComparison.Ordinal);
        Assert.Contains("return executionPlan.schedulerPlan;", jobExecutionPlan, StringComparison.Ordinal);
        Assert.Contains("struct HashPreparationPlan", preparationPlanHeader, StringComparison.Ordinal);
        Assert.Contains("size_t preScanFileCountThreshold;", preparationPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashPreparationPlan CreateHashPreparationPlan(const HashRequest& request);", preparationPlanHeader, StringComparison.Ordinal);
        Assert.Contains("bool ShouldPreScanHashRequestFileSizes(const HashPreparationPlan& preparationPlan, const HashRequest& request);", preparationPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashPreparationPlan CreateHashPreparationPlan(const HashRequest& request)", preparationPlan, StringComparison.Ordinal);
        Assert.Contains("preparationPlan.preScanFileCountThreshold = 200;", preparationPlan, StringComparison.Ordinal);
        Assert.Contains("bool ShouldPreScanHashRequestFileSizes(const HashPreparationPlan& preparationPlan, const HashRequest& request)", preparationPlan, StringComparison.Ordinal);
        Assert.Contains("struct HashSchedulerPlan", schedulerPlanHeader, StringComparison.Ordinal);
        Assert.Contains("size_t workerThreadCount;", schedulerPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashSchedulerPlan CreateHashSchedulerPlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode);", schedulerPlanHeader, StringComparison.Ordinal);
        Assert.Contains("size_t GetHashSchedulerWorkerThreadCount(const HashSchedulerPlan& schedulerPlan);", schedulerPlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashSchedulerPlan CreateHashSchedulerPlan(const HashRequest& request, HashDigestExecutionMode digestExecutionMode)", schedulerPlan, StringComparison.Ordinal);
        Assert.Contains("schedulerPlan.workerThreadCount = 5;", schedulerPlan, StringComparison.Ordinal);
        Assert.Contains("size_t GetHashSchedulerWorkerThreadCount(const HashSchedulerPlan& schedulerPlan)", schedulerPlan, StringComparison.Ordinal);
        Assert.Contains("CreateHashDigestRuntimePlan(executionState->executionPlan)", digestPipeline, StringComparison.Ordinal);
        Assert.Contains("GetHashDigestRuntimeUpdateRequest(digestRuntimePlan)", digestExecution, StringComparison.Ordinal);
        Assert.Contains("GetHashDigestRuntimeExecutionMode(digestRuntimePlan)", digestExecution, StringComparison.Ordinal);
        Assert.Contains("GetHashDigestRuntimePreferredBufferLength(digestRuntimePlan)", digestExecution, StringComparison.Ordinal);
        Assert.Contains("GetHashDigestRuntimeQueuePlan(digestRuntimePlan)", digestExecution, StringComparison.Ordinal);
        Assert.Contains("HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA256)", digestUpdater, StringComparison.Ordinal);
        Assert.Contains("void UpdateDigestContextsParallel(const DigestUpdateRequest& digestUpdateRequest", digestUpdater, StringComparison.Ordinal);
        Assert.Contains("future<void> taskSHA512Update", digestUpdater, StringComparison.Ordinal);
        Assert.Contains("future<void> taskSHA256Update", digestUpdater, StringComparison.Ordinal);
        Assert.Contains("future<void> taskSHA1Update", digestUpdater, StringComparison.Ordinal);
        Assert.Contains("future<void> taskMD5Update", digestUpdater, StringComparison.Ordinal);
        Assert.Contains("void UpdateHashExecutionProgress(HashExecutionContext *executionContext, uint64_t fileSize, bool isSizeCaled, unsigned int dataLen,", progressTracker, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileProgressEvent(positionNew));", progressTracker, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateTotalProgressEvent(progressState->positionWhole));", progressTracker, StringComparison.Ordinal);
        Assert.Contains("UpdateHashExecutionProgress(executionContext, fileSize, isSizeCaled, ptrDataBufCalc->datalen, &executionState->progressState);", digestQueue, StringComparison.Ordinal);
        Assert.Contains("UpdateHashExecutionProgress(executionContext, fsize, isSizeCaled, databuf.datalen, &executionState->progressState);", digestSinglePass, StringComparison.Ordinal);

        Assert.Contains("PrepareHashingWork(HashExecutionContext *executionContext, const HashRequest& request, const HashPreparationPlan& preparationPlan", preparation, StringComparison.Ordinal);
        Assert.Contains("TryPreScanSmallBatchFileSizes(HashExecutionContext *executionContext, const HashRequest& request, const HashPreparationPlan& preparationPlan", preparation, StringComparison.Ordinal);
        Assert.Contains("ShouldPreScanHashRequestFileSizes(preparationPlan, request)", preparation, StringComparison.Ordinal);
        Assert.Contains("AppendHashExecutionResult(*executionContext)", preparation, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreatePreparingProgressEvent());", preparation, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreatePreparationFinishedProgressEvent());", preparation, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileStartedProgressEvent(result));", preparation, StringComparison.Ordinal);

        Assert.Contains("PrepareFileMetaResult(HashExecutionContext *executionContext, HashResult& result", result, StringComparison.Ordinal);
        Assert.Contains("InitializeFileHashing(const HashRequest& request, HashExecutionContext *executionContext", result, StringComparison.Ordinal);
        Assert.Contains("FinalizeDigestStrings(const HashRequest& request", result, StringComparison.Ordinal);
        Assert.Contains("ReplaceHashExecutionCountedFileSize(*executionContext, fSizes[fileIndex], fsize);", result, StringComparison.Ordinal);
        Assert.Contains("tstrFileVersion = ResolveHashFileVersion(osFile, path);", result, StringComparison.Ordinal);
        Assert.DoesNotContain("void CompleteSuccessfulFileHashing(", result, StringComparison.Ordinal);
        Assert.DoesNotContain("void CompleteOpenedFileAttempt(", result, StringComparison.Ordinal);
        Assert.DoesNotContain("void EmitHashResult(", result, StringComparison.Ordinal);
        Assert.DoesNotContain("void EmitErrorResult(", result, StringComparison.Ordinal);
        Assert.Contains("sunjwbase::tstring ResolveHashFileVersion(sunjwbase::OsFile& osFile, const TCHAR *path);", fileVersionResolverHeader, StringComparison.Ordinal);
        Assert.Contains("sunjwbase::tstring ResolveHashFileVersion(sunjwbase::OsFile& osFile, const TCHAR *path)", fileVersionResolver, StringComparison.Ordinal);
        Assert.Contains("WindowsComm::FileVersionHelper fvHelper(osFile);", fileVersionResolver, StringComparison.Ordinal);
        Assert.Contains("return WindowsComm::GetExeFileVersion((TCHAR *)path);", fileVersionResolver, StringComparison.Ordinal);
        Assert.Contains("struct HashDigestRuntimePlan", digestRuntimePlanHeader, StringComparison.Ordinal);
        Assert.Contains("HashDigestRuntimePlan CreateHashDigestRuntimePlan(const HashJobExecutionPlan& executionPlan);", digestRuntimePlanHeader, StringComparison.Ordinal);
        Assert.Contains("const HashDigestQueuePlan *digestQueuePlan;", digestRuntimePlanHeader, StringComparison.Ordinal);
        Assert.Contains("digestRuntimePlan.digestQueuePlan = &GetHashJobDigestQueuePlan(executionPlan);", digestRuntimePlan, StringComparison.Ordinal);
        Assert.Contains("digestRuntimePlan.preferredBufferLength = GetHashDigestBufferPreferredLength(GetHashJobDigestBufferPlan(executionPlan));", digestRuntimePlan, StringComparison.Ordinal);
        Assert.Contains("bool CompleteOpenedFileDigestExecution(HashExecutionContext *executionContext, FileExecutionState *executionState, bool wasStopped);", digestCompletionHeader, StringComparison.Ordinal);
        Assert.Contains("bool CompleteOpenedFileDigestExecution(HashExecutionContext *executionContext, FileExecutionState *executionState, bool wasStopped)", digestCompletion, StringComparison.Ordinal);
        Assert.Contains("if (CompleteOpenedFileDigestExecution(executionContext, executionState, wasStopped))", digestPipeline, StringComparison.Ordinal);
        Assert.Contains("if (ShouldStopHashExecution(*executionContext))", digestCompletion, StringComparison.Ordinal);

        Assert.Contains("void UpdateWholeProgressAfterFile(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, uint32_t fileIndex)", publisher, StringComparison.Ordinal);
        Assert.Contains("void CompleteSuccessfulFileHashing(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,", publisher, StringComparison.Ordinal);
        Assert.Contains("void CompleteOpenedFileAttempt(HashExecutionContext *executionContext, const HashRequest& request, HashResult& result, uint32_t fileIndex, bool isSizeCaled,", publisher, StringComparison.Ordinal);
        Assert.Contains("void EmitHashResult(HashExecutionContext *executionContext, HashResult& result, bool uppercase)", publisher, StringComparison.Ordinal);
        Assert.Contains("void EmitErrorResult(HashExecutionContext *executionContext, HashResult& result)", publisher, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileHashReadyProgressEvent(result, uppercase));", publisher, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileFailedProgressEvent(result));", publisher, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileFinishedProgressEvent());", publisher, StringComparison.Ordinal);

        Assert.Contains("#include \"Common/HashDigestExecution.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashDigestExecutionMode.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashDigestBufferPlan.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashDigestCompletion.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashDigestQueue.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashDigestQueuePlan.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashDigestRuntimePlan.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashDigestPipeline.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashDigestSinglePass.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashDigestUpdater.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashFileAttemptWorkflow.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashFileVersionResolver.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashJobExecutionPlan.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashPreparationPlan.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashProgressTracker.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashResultPublisher.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashExecutionContext.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashProgressSink.h\"", internalHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("#include \"Common/HashEngineObserver.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashRequest.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashSchedulerDispatch.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashSchedulerPlan.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("const HashRequest& request", internalHeader, StringComparison.Ordinal);
        Assert.Contains("bool RunHashScheduler(HashExecutionContext *executionContext, const HashRequest& request, const HashJobExecutionPlan& executionPlan, bool isSizeCaled, ULLongVector& fSizes);", internalHeader, StringComparison.Ordinal);
        Assert.Contains("bool RunFileHashAttempt(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const sunjwbase::tstring& fullPath, bool isSizeCaled, ULLongVector& fSizes,", internalHeader, StringComparison.Ordinal);
        Assert.Contains("FileExecutionState *executionState", internalHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("void EmitHashResult(HashExecutionContext *executionContext, HashResult& result, bool uppercase);", internalHeader, StringComparison.Ordinal);
    }

    [Fact]
    public void BridgeConsumers_NowConsumeHashResultAcrossManagedAndRealtimeAdapters()
    {
        string managedDispatch = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\ManagedBridgeDispatch.h");
        string managedHashMgmtAccess = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\ManagedHashMgmtAccess.h");
        string clrHashResultNet = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashClrBridge\HashResultNet.h");
        string clrMgmt = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
        string clrMgmtHeader = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashClrBridge\HashMgmtClr.h");
        string clrDelegatesHeader = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashClrBridge\UIBridgeDelegates.h");
        string clrDelegatesSource = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashClrBridge\UIBridgeDelegates.cpp");
        string uwpHashResultNet = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashWinRtBridge\HashResultNet.h");
        string uwpMgmt = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashWinRtBridge\HashMgmt.cpp");
        string uwpMgmtHeader = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashWinRtBridge\HashMgmt.h");
        string uwpDelegateHeader = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashWinRtBridge\UIBridgeDelegate.h");
        string uwpDelegateSource = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashWinRtBridge\UIBridgeDelegate.cpp");
        string mfcHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\UIBridgeMFC.h");
        string mfcSource = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\UIBridgeMFC.cpp");
        string wuiHeader = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashClrBridge\UIBridgeWUI.h");
        string wuiSource = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
        string uwpHeader = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashWinRtBridge\UIBridgeUwp.h");
        string uwpSource = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");
        string macHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\OSXUI\UIBridgeMacSwift.h");
        string macSource = RepositoryTestContext.ReadTextFile(@"trunk\source\OSXUI\UIBridgeMacSwift.mm");
        string hashBridgeMac = RepositoryTestContext.ReadTextFile(@"trunk\source\OSXUI\HashBridge.mm");
        string searchHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\FilesHashSearchController.h");
        string searchSource = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\FilesHashSearchController.cpp");
        string winUiPage = RepositoryTestContext.ReadTextFile(@"trunk\source\WinUI\MainPage.xaml.cs");
        string winUwpPage = RepositoryTestContext.ReadTextFile(@"trunk\source\WinUWP\MainPage.xaml.cs");

        Assert.Contains("#include \"Common/HashResultProjection.h\"", managedDispatch, StringComparison.Ordinal);
        Assert.Contains("DispatchManagedBridgeResultByType(const HashResult& result", managedDispatch, StringComparison.Ordinal);
        Assert.Contains("ProjectHashResultToNet<TResultDataNet, TResultStateNet>(result, convertString)", managedDispatch, StringComparison.Ordinal);
        Assert.DoesNotContain("DispatchManagedBridgeResultByType(const ResultData& result", managedDispatch, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashResultProjection.h\"", managedHashMgmtAccess, StringComparison.Ordinal);
        Assert.Contains("CreateProjectedDigestMatchingHashResults<THashResultNet, THashResultStateNet, TResultArray>(", managedHashMgmtAccess, StringComparison.Ordinal);
        Assert.Contains("static inline TResultArray CreateProjectedManagedDigestMatchingHashResults(", managedHashMgmtAccess, StringComparison.Ordinal);

        Assert.Contains("public enum class HashResultStateNet", clrHashResultNet, StringComparison.Ordinal);
        Assert.Contains("public value struct HashResultNet", clrHashResultNet, StringComparison.Ordinal);
        Assert.Contains("cli::array<HashResultNet>^ FindHashResults(System::String^ sstrHashToFind);", clrMgmtHeader, StringComparison.Ordinal);
        Assert.Contains("CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, cli::array<HashResultNet>^>(", clrMgmt, StringComparison.Ordinal);
        Assert.DoesNotContain("FindResult(System::String^ sstrHashToFind)", clrMgmtHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateCompatibilityResultDataNetArray(", clrMgmt, StringComparison.Ordinal);
        Assert.Contains("#include \"HashResultNet.h\"", clrDelegatesHeader, StringComparison.Ordinal);
        Assert.Contains("public delegate void HashResultEventHandler(HashResultNet);", clrDelegatesHeader, StringComparison.Ordinal);
        Assert.Contains("void ShowFileHash(HashResultNet hashResultNet, bool uppercase);", clrDelegatesHeader, StringComparison.Ordinal);
        Assert.Contains("event HashResultHashEventHandler^ ShowFileHashHandler;", clrDelegatesHeader, StringComparison.Ordinal);
        Assert.Contains("void UIBridgeDelegates::ShowFileHash(HashResultNet hashResultNet, bool uppercase)", clrDelegatesSource, StringComparison.Ordinal);
        Assert.Contains("ShowFileHashHandler(hashResultNet, uppercase);", clrDelegatesSource, StringComparison.Ordinal);

        Assert.Contains("public enum class HashResultStateNet", uwpHashResultNet, StringComparison.Ordinal);
        Assert.Contains("public value struct HashResultNet", uwpHashResultNet, StringComparison.Ordinal);
        Assert.Contains("Platform::Array<HashResultNet>^ FindHashResults(Platform::String^ pstrHashToFind);", uwpMgmtHeader, StringComparison.Ordinal);
        Assert.Contains("CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, Array<HashResultNet>^>(", uwpMgmt, StringComparison.Ordinal);
        Assert.DoesNotContain("FindResult(Platform::String^ pstrHashToFind)", uwpMgmtHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateCompatibilityResultDataNetArray(", uwpMgmt, StringComparison.Ordinal);
        Assert.Contains("#include \"HashResultNet.h\"", uwpDelegateHeader, StringComparison.Ordinal);
        Assert.Contains("public delegate void HashResultEventHandler(HashResultNet);", uwpDelegateHeader, StringComparison.Ordinal);
        Assert.Contains("void ShowFileHash(HashResultNet hashResultNet, Platform::Boolean uppercase);", uwpDelegateHeader, StringComparison.Ordinal);
        Assert.Contains("event HashResultHashEventHandler^ ShowFileHashHandler;", uwpDelegateHeader, StringComparison.Ordinal);
        Assert.Contains("void UIBridgeDelegate::ShowFileHash(HashResultNet hashResultNet, Boolean uppercase)", uwpDelegateSource, StringComparison.Ordinal);
        Assert.Contains("ShowFileHashHandler(hashResultNet, uppercase);", uwpDelegateSource, StringComparison.Ordinal);

        Assert.Contains("virtual void onFileResultEvent(const HashResult& result,", mfcHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashResultRender.h\"", mfcHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("#include \"Common/HashResultCompatibility.h\"", mfcHeader, StringComparison.Ordinal);
        Assert.Contains("void UIBridgeMFC::onFileResultEvent(const HashResult& result,", mfcSource, StringComparison.Ordinal);
        Assert.Contains("case PROGRESS_EVENT_FILE_HASH_READY:", mfcSource, StringComparison.Ordinal);
        Assert.Contains("AppendResultSectionAndRefresh(result, RESULT_RENDER_SECTION_HASH, uppercaseDigest);", mfcSource, StringComparison.Ordinal);
        Assert.Contains("VisitHashResultDigestDisplayValues(result, uppercase", mfcSource, StringComparison.Ordinal);
        Assert.Contains("void UIBridgeMFC::AppendResultToHyperEdit(const HashResult& result,", mfcSource, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateCompatibilityResultData(result);", mfcSource, StringComparison.Ordinal);
        Assert.Contains("void AppendResult(const HashResult& result);", searchHeader, StringComparison.Ordinal);
        Assert.Contains("VisitThreadDataHashResults(*m_threadData, [&](const HashResult& result)", searchSource, StringComparison.Ordinal);
        Assert.Contains("VisitThreadDataPathAndDigestMatchingHashResults(*m_threadData, tstrFileToFind, tstrHashToFind, [&](const HashResult& result)", searchSource, StringComparison.Ordinal);
        Assert.Contains("UIBridgeMFC::AppendResultToHyperEdit(result, GetThreadDataUppercase(*m_threadData), m_mainEdit);", searchSource, StringComparison.Ordinal);

        Assert.Contains("virtual void onFileResultEvent(const HashResult& result,", wuiHeader, StringComparison.Ordinal);
        Assert.Contains("DispatchProjectedResultToDelegate(const HashResult& result", wuiHeader, StringComparison.Ordinal);
        Assert.Contains("void UIBridgeWUI::onFileResultEvent(const HashResult& result,", wuiSource, StringComparison.Ordinal);
        Assert.Contains("DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase", wuiSource, StringComparison.Ordinal);
        Assert.Contains("GetManagedResultDispatchType(eventType)", wuiSource, StringComparison.Ordinal);
        Assert.Contains("m_uiBridgeDelegates->ShowFileHash(hashResultNet, hashUppercase);", wuiSource, StringComparison.Ordinal);

        Assert.Contains("virtual void onFileResultEvent(const HashResult& result,", uwpHeader, StringComparison.Ordinal);
        Assert.Contains("DispatchProjectedResultToDelegate(const HashResult& result", uwpHeader, StringComparison.Ordinal);
        Assert.Contains("void UIBridgeUwp::onFileResultEvent(const HashResult& result,", uwpSource, StringComparison.Ordinal);
        Assert.Contains("DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase", uwpSource, StringComparison.Ordinal);
        Assert.Contains("GetManagedResultDispatchType(eventType)", uwpSource, StringComparison.Ordinal);
        Assert.Contains("m_uiBridgeDelegate->ShowFileHash(hashResultNet, hashUppercase);", uwpSource, StringComparison.Ordinal);

        Assert.DoesNotContain("#include \"Common/HashResultCompatibility.h\"", macHeader, StringComparison.Ordinal);
        Assert.Contains("static ResultDataSwift *ConvertHashResultToSwift(const HashResult& result);", macHeader, StringComparison.Ordinal);
        Assert.Contains("virtual void onFileResultEvent(const HashResult& result,", macHeader, StringComparison.Ordinal);
        Assert.Contains("ResultDataSwift *resultSwift = UIBridgeMacSwift::ConvertHashResultToSwift(result);", macSource, StringComparison.Ordinal);
        Assert.Contains("void UIBridgeMacSwift::onFileResultEvent(const HashResult& result,", macSource, StringComparison.Ordinal);
        Assert.Contains("case PROGRESS_EVENT_FILE_HASH_READY:", macSource, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateCompatibilityResultData(result);", macSource, StringComparison.Ordinal);
        Assert.Contains("ResultDataSwift *UIBridgeMacSwift::ConvertHashResultToSwift(const HashResult& result)", macSource, StringComparison.Ordinal);
        Assert.Contains("VisitThreadDataHashResults(*_thrdData, [&](const HashResult& result)", hashBridgeMac, StringComparison.Ordinal);
        Assert.Contains("ConvertHashResultToSwift(result);", hashBridgeMac, StringComparison.Ordinal);

        Assert.Contains("HashResultNet[] hashResultNetArray = m_mainWindow.HashMgmt.FindHashResults(strHashToFind);", winUiPage, StringComparison.Ordinal);
        Assert.Contains("private void AppendFileResultToTextMain(HashResultNet hashResult, bool uppercase)", winUiPage, StringComparison.Ordinal);
        Assert.Contains("private void ShowFindResult(string strHashToFind, HashResultNet[] hashResultNetArray)", winUiPage, StringComparison.Ordinal);
        Assert.Contains("AppendFileResultToTextMain(hashResult, m_uppercaseChecked);", winUiPage, StringComparison.Ordinal);
        Assert.Contains("private void UIBridgeHandlers_ShowFileHashHandler(HashResultNet hashResult, bool uppercase)", winUiPage, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateCompatibilityResultData(", winUiPage, StringComparison.Ordinal);

        Assert.Contains("HashResultNet[] hashResultNetArray = m_hashMgmt.FindHashResults(strHashToFind);", winUwpPage, StringComparison.Ordinal);
        Assert.Contains("private void AppendFileResultToTextMain(HashResultNet hashResult, bool uppercase)", winUwpPage, StringComparison.Ordinal);
        Assert.Contains("private void ShowFindResult(string strHashToFind, HashResultNet[] hashResultNetArray)", winUwpPage, StringComparison.Ordinal);
        Assert.Contains("AppendFileResultToTextMain(hashResult, m_uppercaseChecked);", winUwpPage, StringComparison.Ordinal);
        Assert.Contains("private void UIBridgeDelegate_ShowFileHashHandler(HashResultNet hashResult, bool uppercase)", winUwpPage, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateCompatibilityResultData(", winUwpPage, StringComparison.Ordinal);
    }
}
