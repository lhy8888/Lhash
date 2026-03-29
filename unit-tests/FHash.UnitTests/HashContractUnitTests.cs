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
        string result = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashResult.h");
        string compatibility = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashResultCompatibility.h");
        string metadata = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\ResultDigestMetadataAccess.h");

        Assert.Contains("struct HashDigestResult", result, StringComparison.Ordinal);
        Assert.Contains("struct HashFileMeta", result, StringComparison.Ordinal);
        Assert.Contains("struct HashResult", result, StringComparison.Ordinal);
        Assert.Contains("const ResultData *sourceResult;", result, StringComparison.Ordinal);
        Assert.Contains("std::vector<HashDigestResult> digests;", result, StringComparison.Ordinal);
        Assert.Contains("ProjectHashResult(const ResultData& result)", result, StringComparison.Ordinal);
        Assert.Contains("PopulateCompatibilityResultData(ResultData& compatibilityResult, const HashResult& hashResult)", compatibility, StringComparison.Ordinal);
        Assert.Contains("CreateCompatibilityResultData(const HashResult& hashResult)", compatibility, StringComparison.Ordinal);
        Assert.Contains("VisitResultDigestMetadataValues(result", result, StringComparison.Ordinal);
        Assert.Contains("GetResultDigestMetadataStableName(const ResultDigestMetadata& digestMetadata)", metadata, StringComparison.Ordinal);
    }

    [Fact]
    public void ProgressEvent_DefinesSemanticLifecycleSurface_AndObserverCompatibilityDispatch()
    {
        string progressEvent = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\ProgressEvent.h");
        string progressSink = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashProgressSink.h");
        string observer = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEngineObserver.h");

        Assert.Contains("enum ProgressEventType", progressEvent, StringComparison.Ordinal);
        Assert.Contains("PROGRESS_EVENT_JOB_PREPARING", progressEvent, StringComparison.Ordinal);
        Assert.Contains("PROGRESS_EVENT_FILE_HASH_READY", progressEvent, StringComparison.Ordinal);
        Assert.Contains("CreateFileHashReadyProgressEvent(const ResultData& result, bool uppercaseDigest)", progressEvent, StringComparison.Ordinal);
        Assert.Contains("CreateFileHashReadyProgressEvent(const HashResult& result, bool uppercaseDigest)", progressEvent, StringComparison.Ordinal);
        Assert.Contains("CreateResultProgressEvent(ProgressEventType eventType, const HashResult& result)", progressEvent, StringComparison.Ordinal);

        Assert.Contains("class HashProgressSink", progressSink, StringComparison.Ordinal);
        Assert.Contains("virtual int progressMax() = 0;", progressSink, StringComparison.Ordinal);
        Assert.Contains("virtual void onProgressEvent(const ProgressEvent& progressEvent) = 0;", progressSink, StringComparison.Ordinal);

        Assert.Contains("class HashEngineObserver: public HashProgressSink", observer, StringComparison.Ordinal);
        Assert.Contains("virtual void onProgressEvent(const ProgressEvent& progressEvent)", observer, StringComparison.Ordinal);
        Assert.Contains("void onFileHashReady(const HashResult& result, bool uppercase)", observer, StringComparison.Ordinal);
        Assert.Contains("ResultData compatibilityResult = CreateCompatibilityResultData(result);", observer, StringComparison.Ordinal);
        Assert.Contains("onFileHashReady(progressEvent.result, progressEvent.uppercaseDigest);", observer, StringComparison.Ordinal);
        Assert.Contains("updateProgWhole(progressEvent.value);", observer, StringComparison.Ordinal);
    }

    [Fact]
    public void HashEngine_StartsFromHashRequest_AndEmitsProgressEvents()
    {
        string global = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\Global.h");
        string executionContext = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashExecutionContext.h");
        string threadExecutionAccess = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\ThreadDataExecutionAccess.h");
        string engineHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEngine.h");
        string engine = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEngine.cpp");
        string preparation = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEnginePreparation.cpp");
        string result = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEngineResult.cpp");
        string internalHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEngineInternal.h");

        Assert.Contains("class HashProgressSink;", global, StringComparison.Ordinal);
        Assert.Contains("HashProgressSink *observer;", global, StringComparison.Ordinal);
        Assert.Contains("struct HashExecutionContext", executionContext, StringComparison.Ordinal);
        Assert.Contains("CreateHashExecutionContext(ThreadData& threadData)", executionContext, StringComparison.Ordinal);
        Assert.Contains("GetHashExecutionProgressSink(const HashExecutionContext& executionContext)", executionContext, StringComparison.Ordinal);
        Assert.Contains("ShouldStopHashExecution(const HashExecutionContext& executionContext)", executionContext, StringComparison.Ordinal);
        Assert.Contains("AppendHashExecutionResult(HashExecutionContext& executionContext)", executionContext, StringComparison.Ordinal);
        Assert.Contains("SetThreadDataObserver(ThreadData& threadData, HashProgressSink *observer)", threadExecutionAccess, StringComparison.Ordinal);
        Assert.Contains("GetThreadDataObserver(const ThreadData& threadData)", threadExecutionAccess, StringComparison.Ordinal);
        Assert.Contains("struct HashExecutionContext;", engineHeader, StringComparison.Ordinal);
        Assert.Contains("int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request);", engineHeader, StringComparison.Ordinal);
        Assert.Contains("int RunHashRequest(HashExecutionContext *executionContext, const HashRequest& request)", engine, StringComparison.Ordinal);
        Assert.Contains("HashRequest request = CreateHashRequest(*thrdData);", engine, StringComparison.Ordinal);
        Assert.Contains("HashExecutionContext executionContext = CreateHashExecutionContext(*thrdData);", engine, StringComparison.Ordinal);
        Assert.Contains("return RunHashRequest(&executionContext, request);", engine, StringComparison.Ordinal);
        Assert.Contains("ULLongVector fSizes(GetHashRequestFileCount(request));", engine, StringComparison.Ordinal);
        Assert.Contains("VisitHashRequestFiles(request", engine, StringComparison.Ordinal);
        Assert.Contains("HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA256)", engine, StringComparison.Ordinal);
        Assert.Contains("ShouldStopHashExecution(*executionContext)", engine, StringComparison.Ordinal);
        Assert.Contains("ResetHashExecutionTotalSize(*executionContext);", engine, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileProgressEvent(positionNew));", engine, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateTotalProgressEvent(progressState->positionWhole));", engine, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateCancelledProgressEvent());", engine, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateCompletedProgressEvent());", engine, StringComparison.Ordinal);

        Assert.Contains("PrepareHashingWork(HashExecutionContext *executionContext, const HashRequest& request", preparation, StringComparison.Ordinal);
        Assert.Contains("AppendHashExecutionResult(*executionContext)", preparation, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreatePreparingProgressEvent());", preparation, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreatePreparationFinishedProgressEvent());", preparation, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileStartedProgressEvent(ProjectHashResult(result)));", preparation, StringComparison.Ordinal);

        Assert.Contains("PrepareFileMetaResult(HashExecutionContext *executionContext, ResultData& result", result, StringComparison.Ordinal);
        Assert.Contains("InitializeFileHashing(const HashRequest& request, HashExecutionContext *executionContext", result, StringComparison.Ordinal);
        Assert.Contains("FinalizeDigestStrings(const HashRequest& request", result, StringComparison.Ordinal);
        Assert.Contains("ReplaceHashExecutionCountedFileSize(*executionContext, fSizes[fileIndex], fsize);", result, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileHashReadyProgressEvent(ProjectHashResult(result), uppercase));", result, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileFailedProgressEvent(ProjectHashResult(result)));", result, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileFinishedProgressEvent());", result, StringComparison.Ordinal);

        Assert.Contains("#include \"Common/HashExecutionContext.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashProgressSink.h\"", internalHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("#include \"Common/HashEngineObserver.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashRequest.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("const HashRequest& request", internalHeader, StringComparison.Ordinal);
    }
}
