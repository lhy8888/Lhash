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
        string scheduler = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashScheduler.cpp");
        string preparation = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEnginePreparation.cpp");
        string result = RepositoryTestContext.ReadTextFile(@"trunk\source\Common\HashEngineResult.cpp");
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
        Assert.Contains("ULLongVector fSizes(GetHashRequestFileCount(request));", engine, StringComparison.Ordinal);
        Assert.Contains("RunHashScheduler(executionContext, request, isSizeCaled, fSizes)", engine, StringComparison.Ordinal);
        Assert.Contains("ResetHashExecutionTotalSize(*executionContext);", engine, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateCancelledProgressEvent());", engine, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateCompletedProgressEvent());", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("FileExecutionState executionState = { 0 };", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("static bool ProcessOpenedFileHashing(", engine, StringComparison.Ordinal);

        Assert.Contains("bool RunHashScheduler(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, ULLongVector& fSizes)", scheduler, StringComparison.Ordinal);
        Assert.Contains("VisitHashRequestFiles(request", scheduler, StringComparison.Ordinal);
        Assert.Contains("RunFileHashAttempt(executionContext, request, fileIndex, fullPath, isSizeCaled, fSizes", scheduler, StringComparison.Ordinal);
        Assert.Contains("FileExecutionState executionState = { 0 };", scheduler, StringComparison.Ordinal);
        Assert.Contains("ThreadPool threadPool(5);", scheduler, StringComparison.Ordinal);

        Assert.Contains("bool RunFileHashAttempt(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const tstring& fullPath", fileRunner, StringComparison.Ordinal);
        Assert.Contains("static bool ProcessOpenedFileHashing(", fileRunner, StringComparison.Ordinal);
        Assert.Contains("YieldHashThread();", fileRunner, StringComparison.Ordinal);
        Assert.DoesNotContain("FileExecutionState executionState = { 0 };", fileRunner, StringComparison.Ordinal);
        Assert.Contains("HasHashRequestAlgorithm(request, RESULT_DIGEST_SHA256)", fileRunner, StringComparison.Ordinal);
        Assert.Contains("ShouldStopHashExecution(*executionContext)", fileRunner, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileProgressEvent(positionNew));", fileRunner, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateTotalProgressEvent(progressState->positionWhole));", fileRunner, StringComparison.Ordinal);

        Assert.Contains("PrepareHashingWork(HashExecutionContext *executionContext, const HashRequest& request", preparation, StringComparison.Ordinal);
        Assert.Contains("AppendHashExecutionResult(*executionContext)", preparation, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreatePreparingProgressEvent());", preparation, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreatePreparationFinishedProgressEvent());", preparation, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileStartedProgressEvent(result));", preparation, StringComparison.Ordinal);

        Assert.Contains("PrepareFileMetaResult(HashExecutionContext *executionContext, HashResult& result", result, StringComparison.Ordinal);
        Assert.Contains("InitializeFileHashing(const HashRequest& request, HashExecutionContext *executionContext", result, StringComparison.Ordinal);
        Assert.Contains("FinalizeDigestStrings(const HashRequest& request", result, StringComparison.Ordinal);
        Assert.Contains("ReplaceHashExecutionCountedFileSize(*executionContext, fSizes[fileIndex], fsize);", result, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileHashReadyProgressEvent(result, uppercase));", result, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileFailedProgressEvent(result));", result, StringComparison.Ordinal);
        Assert.Contains("observer->onProgressEvent(CreateFileFinishedProgressEvent());", result, StringComparison.Ordinal);

        Assert.Contains("#include \"Common/HashExecutionContext.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashProgressSink.h\"", internalHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("#include \"Common/HashEngineObserver.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("#include \"Common/HashRequest.h\"", internalHeader, StringComparison.Ordinal);
        Assert.Contains("const HashRequest& request", internalHeader, StringComparison.Ordinal);
        Assert.Contains("bool RunHashScheduler(HashExecutionContext *executionContext, const HashRequest& request, bool isSizeCaled, ULLongVector& fSizes);", internalHeader, StringComparison.Ordinal);
        Assert.Contains("bool RunFileHashAttempt(HashExecutionContext *executionContext, const HashRequest& request, uint32_t fileIndex, const sunjwbase::tstring& fullPath, bool isSizeCaled, ULLongVector& fSizes,", internalHeader, StringComparison.Ordinal);
        Assert.Contains("FileExecutionState *executionState", internalHeader, StringComparison.Ordinal);
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
