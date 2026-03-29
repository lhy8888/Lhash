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
        string projection = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashResultProjection.h");
        string search = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashResultSearch.h");
        string metadata = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\ResultDigestMetadataAccess.h");

        Assert.Contains("struct HashDigestResult", result, StringComparison.Ordinal);
        Assert.Contains("struct HashFileMeta", result, StringComparison.Ordinal);
        Assert.Contains("struct HashResult", result, StringComparison.Ordinal);
        Assert.Contains("const ResultData *sourceResult;", result, StringComparison.Ordinal);
        Assert.Contains("std::vector<HashDigestResult> digests;", result, StringComparison.Ordinal);
        Assert.Contains("ProjectHashResult(const ResultData& result)", result, StringComparison.Ordinal);
        Assert.Contains("PopulateCompatibilityResultData(ResultData& compatibilityResult, const HashResult& hashResult)", compatibility, StringComparison.Ordinal);
        Assert.Contains("CreateCompatibilityResultData(const HashResult& hashResult)", compatibility, StringComparison.Ordinal);
        Assert.Contains("AssignHashResultCoreToNet", projection, StringComparison.Ordinal);
        Assert.Contains("AssignHashResultDigestsToNet", projection, StringComparison.Ordinal);
        Assert.Contains("ProjectHashResultToNet(const HashResult& result, TStringConverter convertString)", projection, StringComparison.Ordinal);
        Assert.Contains("VisitProjectedHashResults(const ResultList& resultList, TStringConverter convertString, TResultVisitor visitor)", projection, StringComparison.Ordinal);
        Assert.Contains("CreateProjectedDigestMatchingHashResults(const ResultList& resultList, const sunjwbase::tstring& digestText", projection, StringComparison.Ordinal);
        Assert.Contains("HashResultContainsDigest(const HashResult& result, const sunjwbase::tstring& digestText)", search, StringComparison.Ordinal);
        Assert.Contains("HashResultMatchesDigestText(const HashResult& result, const sunjwbase::tstring& digestText)", search, StringComparison.Ordinal);
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
        Assert.Contains("onFileStarted(ProjectHashResult(result));", observer, StringComparison.Ordinal);
        Assert.Contains("virtual void showFileName(const HashResult& result) = 0;", observer, StringComparison.Ordinal);
        Assert.Contains("onFileHashReady(progressEvent.result, progressEvent.uppercaseDigest);", observer, StringComparison.Ordinal);
        Assert.Contains("updateProgWhole(progressEvent.value);", observer, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateCompatibilityResultData(result);", observer, StringComparison.Ordinal);
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

    [Fact]
    public void BridgeConsumers_NowConsumeHashResultAcrossManagedAndCompatibilityAdapters()
    {
        string managedDispatch = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\ManagedBridgeDispatch.h");
        string managedHashMgmtAccess = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\ManagedHashMgmtAccess.h");
        string clrHashResultNet = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashClrBridge\HashResultNet.h");
        string clrMgmt = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashClrBridge\HashMgmtClr.cpp");
        string clrMgmtHeader = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashClrBridge\HashMgmtClr.h");
        string uwpHashResultNet = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashWinRtBridge\HashResultNet.h");
        string uwpMgmt = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashWinRtBridge\HashMgmt.cpp");
        string uwpMgmtHeader = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashWinRtBridge\HashMgmt.h");
        string mfcHeader = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\UIBridgeMFC.h");
        string mfcSource = RepositoryTestContext.ReadTextFile(@"trunk\source\WinMFC\UIBridgeMFC.cpp");
        string wuiHeader = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashClrBridge\UIBridgeWUI.h");
        string wuiSource = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashClrBridge\UIBridgeWUI.cpp");
        string uwpHeader = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashWinRtBridge\UIBridgeUwp.h");
        string uwpSource = RepositoryTestContext.ReadTextFile(@"sub-proj\fHashWinRtBridge\UIBridgeUwp.cpp");
        string winUiPage = RepositoryTestContext.ReadTextFile(@"trunk\source\WinUI\MainPage.xaml.cs");
        string winUwpPage = RepositoryTestContext.ReadTextFile(@"trunk\source\WinUWP\MainPage.xaml.cs");

        Assert.Contains("#include \"Common/HashResultProjection.h\"", managedDispatch, StringComparison.Ordinal);
        Assert.Contains("DispatchManagedBridgeResultByType(const HashResult& result", managedDispatch, StringComparison.Ordinal);
        Assert.Contains("ProjectHashResultToNet<TResultDataNet, TResultStateNet>(result, convertString)", managedDispatch, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashResultProjection.h\"", managedHashMgmtAccess, StringComparison.Ordinal);
        Assert.Contains("CreateProjectedDigestMatchingHashResults<THashResultNet, THashResultStateNet, TResultArray>(", managedHashMgmtAccess, StringComparison.Ordinal);
        Assert.Contains("static inline TResultArray CreateProjectedManagedDigestMatchingHashResults(", managedHashMgmtAccess, StringComparison.Ordinal);

        Assert.Contains("public enum class HashResultStateNet", clrHashResultNet, StringComparison.Ordinal);
        Assert.Contains("public value struct HashResultNet", clrHashResultNet, StringComparison.Ordinal);
        Assert.Contains("cli::array<HashResultNet>^ FindHashResults(System::String^ sstrHashToFind);", clrMgmtHeader, StringComparison.Ordinal);
        Assert.Contains("CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, cli::array<HashResultNet>^>(", clrMgmt, StringComparison.Ordinal);
        Assert.Contains("return CreateCompatibilityResultDataNetArray(FindHashResults(sstrHashToFind));", clrMgmt, StringComparison.Ordinal);

        Assert.Contains("public enum class HashResultStateNet", uwpHashResultNet, StringComparison.Ordinal);
        Assert.Contains("public value struct HashResultNet", uwpHashResultNet, StringComparison.Ordinal);
        Assert.Contains("Platform::Array<HashResultNet>^ FindHashResults(Platform::String^ pstrHashToFind);", uwpMgmtHeader, StringComparison.Ordinal);
        Assert.Contains("CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, Array<HashResultNet>^>(", uwpMgmt, StringComparison.Ordinal);
        Assert.Contains("return CreateCompatibilityResultDataNetArray(FindHashResults(pstrHashToFind));", uwpMgmt, StringComparison.Ordinal);

        Assert.Contains("virtual void showFileName(const HashResult& result);", mfcHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashResultCompatibility.h\"", mfcHeader, StringComparison.Ordinal);
        Assert.Contains("ResultData compatibilityResult = CreateCompatibilityResultData(result);", mfcSource, StringComparison.Ordinal);

        Assert.Contains("virtual void showFileName(const HashResult& result);", wuiHeader, StringComparison.Ordinal);
        Assert.Contains("DispatchProjectedResultToDelegate(const HashResult& result", wuiHeader, StringComparison.Ordinal);
        Assert.Contains("void UIBridgeWUI::showFileHash(const HashResult& result, bool uppercase)", wuiSource, StringComparison.Ordinal);
        Assert.Contains("DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase", wuiSource, StringComparison.Ordinal);

        Assert.Contains("virtual void showFileName(const HashResult& result);", uwpHeader, StringComparison.Ordinal);
        Assert.Contains("DispatchProjectedResultToDelegate(const HashResult& result", uwpHeader, StringComparison.Ordinal);
        Assert.Contains("void UIBridgeUwp::showFileHash(const HashResult& result, bool uppercase)", uwpSource, StringComparison.Ordinal);
        Assert.Contains("DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase", uwpSource, StringComparison.Ordinal);

        Assert.Contains("HashResultNet[] hashResultNetArray = m_mainWindow.HashMgmt.FindHashResults(strHashToFind);", winUiPage, StringComparison.Ordinal);
        Assert.Contains("private void ShowFindResult(string strHashToFind, HashResultNet[] hashResultNetArray)", winUiPage, StringComparison.Ordinal);
        Assert.Contains("AppendFileResultToTextMain(CreateCompatibilityResultData(hashResult), m_uppercaseChecked);", winUiPage, StringComparison.Ordinal);

        Assert.Contains("HashResultNet[] hashResultNetArray = m_hashMgmt.FindHashResults(strHashToFind);", winUwpPage, StringComparison.Ordinal);
        Assert.Contains("private void ShowFindResult(string strHashToFind, HashResultNet[] hashResultNetArray)", winUwpPage, StringComparison.Ordinal);
        Assert.Contains("AppendFileResultToTextMain(CreateCompatibilityResultData(hashResult), m_uppercaseChecked);", winUwpPage, StringComparison.Ordinal);
    }
}
