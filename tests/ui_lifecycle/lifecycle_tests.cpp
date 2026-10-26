#include "stdafx.h"
#include "Adapters/MfcBridge/HashWorkerSessionLifecycle.h"
#include "Adapters/MfcBridge/HashThreadLaunchContext.h"
#include "Common/HashResultSearch.h"
#include "WinCommon/ComApartmentScope.h"
#include <condition_variable>
#include <deque>
#include <iostream>
#include <mutex>
#include <stdexcept>
#include <thread>

static void Require(bool condition, const char* message)
{
    if (!condition) throw std::runtime_error(message);
}

struct FakeComApi
{
    static inline std::int32_t result = 0;
    static inline int initializations = 0;
    static inline int uninitializations = 0;
    static inline int references = 0;
    static std::int32_t Initialize()
    {
        ++initializations;
        if (result >= 0) ++references;
        return result;
    }
    static void Uninitialize() { ++uninitializations; --references; }
};

static void TestComApartmentClientLifetime()
{
    // This simulates the dialog's taskbar client, whose Release crashed in CI
    // after DoModal. Its entire lifetime must stay inside the apartment scope.
    bool clientReleasedWhileInitialized = false;
    struct Client {
        bool& validRelease;
        ~Client() { validRelease = FakeComApi::references > 0; }
    };
    for (const std::int32_t result : {0, 1, static_cast<std::int32_t>(0x80010106u)}) {
        FakeComApi::result = result;
        FakeComApi::initializations = FakeComApi::uninitializations = FakeComApi::references = 0;
        const int existingReferences = result == 1 ? 1 : 0;
        FakeComApi::references = existingReferences;
        bool called = false;
        clientReleasedWhileInitialized = false;
        const bool ran = RunInComApartment<FakeComApi>([&] {
            called = true;
            Client dialog{clientReleasedWhileInitialized};
            Require(FakeComApi::references == existingReferences + 1, "COM clients must be constructed inside an initialized apartment");
        });
        Require(FakeComApi::initializations == 1, "COM initialization must happen exactly once");
        Require(ran == (result >= 0) && called == ran, "COM initialization failure must not construct clients");
        Require(FakeComApi::uninitializations == (ran ? 1 : 0) && FakeComApi::references == existingReferences,
                "S_OK/S_FALSE must balance once; failed initialization must never uninitialize");
        Require(clientReleasedWhileInitialized == ran, "COM clients must be released before apartment shutdown");
    }
    FakeComApi::result = 0;
    FakeComApi::uninitializations = 0;
    clientReleasedWhileInitialized = false;
    bool caught = false;
    try {
        RunInComApartment<FakeComApi>([&] {
            Client dialog{clientReleasedWhileInitialized};
            throw std::runtime_error("injected dialog construction/run failure");
        });
    } catch (const std::runtime_error&) { caught = true; }
    Require(caught && clientReleasedWhileInitialized && FakeComApi::uninitializations == 1 && FakeComApi::references == 0,
            "exception unwinding must release clients before balancing COM initialization");
}

static void TestLaunchFailureAndRetry()
{
    ThreadData state;
    const auto start = HashWorkerTiming::TimePoint{} + std::chrono::seconds(100);
    state.executionState.timing.Start(start);
    state.executionState.timing.Finish(start + std::chrono::seconds(2));
    bool busyBeforeLaunch = false;
    Require(!LaunchThreadDataWorker(state, [&] {
        busyBeforeLaunch = IsThreadDataWorking(state);
        return false; // injected _beginthreadex failure
    }), "failed launch must report failure");
    Require(busyBeforeLaunch && !IsThreadDataWorking(state), "launch failure must release a pre-acquired lease");
    Require(state.executionState.timing.ElapsedSeconds() == 0, "failed launch must not keep the preceding duration");
    Require(!LaunchThreadDataWorker(state, []() -> bool { throw std::bad_alloc(); }), "launch exception must report failure");
    Require(!IsThreadDataWorking(state), "launch exception must restore idle state");
    Require(LaunchThreadDataWorker(state, [] { return true; }), "retry after failure must succeed");
    state.executionState.timing.Start(start);
    state.executionState.timing.Finish(start + std::chrono::seconds(3));
    Require(!LaunchThreadDataWorker(state, [] { return true; }), "duplicate launch must preserve the active worker");
    Require(state.executionState.timing.ElapsedSeconds() == 3, "rejected duplicate launch must not reset active worker timing");
    Require(CollectThreadDataWorker(state, [] { return true; }), "joined retry must release ownership");
    Require(!IsThreadDataWorking(state), "joined retry must be idle");
}

static void TestCallbackIsNotThreadExit()
{
    ThreadData state;
    std::mutex mutex;
    std::condition_variable changed;
    bool inTerminalCallback = false;
    bool releaseCallback = false;
    std::thread worker;
    Require(LaunchThreadDataWorker(state, [&] {
        Require(IsThreadDataWorking(state), "launch must close the UI entry window before thread creation");
        worker = std::thread([&] {
            SetThreadDataWorking(state, true);
            // Engine completion/cancellation may clear this before a callback
            // returns. The production UI ownership gate must remain active.
            SetThreadDataWorking(state, false);
            std::unique_lock<std::mutex> lock(mutex);
            inTerminalCallback = true;
            changed.notify_all();
            changed.wait(lock, [&] { return releaseCallback; });
        });
        return true;
    }), "real worker launch failed");
    {
        std::unique_lock<std::mutex> lock(mutex);
        changed.wait(lock, [&] { return inTerminalCallback; });
    }
    const bool stayedBusy = IsThreadDataWorking(state);
    bool duplicateLaunchCalled = false;
    const bool duplicateStarted = LaunchThreadDataWorker(state, [&] { duplicateLaunchCalled = true; return true; });
    const bool prematureCollect = CollectThreadDataWorker(state, [] { return false; });
    SetThreadDataStop(state, true); // Close/ESC request cancellation, not ownership release.
    const bool cancelStayedBusy = IsThreadDataWorking(state) && ShouldStopThreadData(state);
    {
        std::lock_guard<std::mutex> lock(mutex);
        releaseCallback = true;
    }
    changed.notify_all();
    const bool collected = CollectThreadDataWorker(state, [&] { worker.join(); return true; });
    Require(stayedBusy && cancelStayedBusy, "terminal callback/cancellation must not expose idle state");
    Require(!duplicateStarted && !duplicateLaunchCalled, "active worker must reject duplicate start before launcher runs");
    Require(!prematureCollect && collected && !IsThreadDataWorking(state), "only actual join may release ownership");
    Require(LaunchThreadDataWorker(state, [] { return true; }), "new session should start after join");
    Require(!ShouldStopThreadData(state), "new session must reset cancellation");
    Require(CollectThreadDataWorker(state, [] { return true; }), "new session should collect");
}

static void TestFullDigestVerification()
{
    HashResult result;
    result.path = _T("abc.txt");
    HashDigestResult digest;
    digest.algorithmId = _T("openssl-sha-256");
    digest.value = _T("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD");
    result.digests.push_back(digest);
    Require(HashResultMatchesDigestText(result, digest.value), "full SHA256 must verify");
    Require(HashResultMatchesDigestText(result, NormalizeHashResultDigestSearchText(_T("  ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad\r\n"))), "case/outer whitespace should normalize");
    for (const auto& partial : {sunjwbase::tstring(_T("A")), sunjwbase::tstring(_T("BA")), digest.value.substr(0, 32), digest.value.substr(17, 16), digest.value.substr(0, 63)})
        Require(!HashResultMatchesDigestText(result, partial), "partial digests must not verify");
    Require(!HashResultMatchesDigestText(result, digest.value + _T("A")), "overlong digest must not verify");
    Require(!HashResultMatchesDigestText(result, sunjwbase::tstring(64, 'F')), "wrong full digest must not verify");
    Require(!HashResultMatchesDigestText(result, _T("")), "empty input must not verify");
    digest.value = _T("ZZ");
    result.digests.assign(1, digest);
    Require(!HashResultMatchesDigestText(result, _T("ZZ")), "nonhex must not verify even if stored text matches");
}

static void TestFinalizationQueueAndSnapshot()
{
    ThreadData state;
    state.inputState.inputFiles.push_back(_T("original.txt"));
    state.executionState.preferences.uppercaseDigest = false;
    HashThreadLaunchContext snapshot(state);
    state.inputState.inputFiles[0] = _T("changed.txt");
    state.executionState.preferences.uppercaseDigest = true;
    Require(snapshot.request.files[0] == _T("original.txt") && !snapshot.request.uppercaseDigest,
            "worker request must be copied before launch, not read later from mutable UI input");

    Require(LaunchThreadDataWorker(state, [] { return true; }), "queue scenario must start");
    std::deque<int> messages{1, 2}; // terminal + last refresh still waiting when timer is handled
    bool calledBeforeExit = false;
    Require(!state.executionState.workerSession.QueueFinalization(false, [&] { calledBeforeExit = true; return true; })
            && !calledBeforeExit, "a running worker must not enqueue finalization");
    Require(!state.executionState.workerSession.QueueFinalization(true, [] { return false; }), "failed post must be retryable");
    Require(state.executionState.workerSession.QueueFinalization(true, [&] { messages.push_back(3); return true; }),
            "signalled worker must append a finalization sentinel");
    Require(!state.executionState.workerSession.QueueFinalization(true, [&] { messages.push_back(3); return true; }),
            "timer must not append duplicate sentinels");
    Require(IsThreadDataWorking(state), "queued sentinel must retain ownership");
    int terminal = 0;
    while (!messages.empty()) {
        const int message = messages.front();
        messages.pop_front();
        if (message == 1) terminal = message;
        else if (message == 3) {
            Require(terminal == 1 && messages.empty(), "all prior notifications must drain before idle is exposed");
            state.executionState.workerSession.FinalizationHandled();
            Require(CollectThreadDataWorker(state, [] { return true; }), "sentinel must collect exited worker");
        }
    }
    Require(!IsThreadDataWorking(state), "finalized worker should be idle");
    Require(LaunchThreadDataWorker(state, [] { return true; }), "next session should start after sentinel");
    Require(state.executionState.workerSession.QueueFinalization(true, [] { return true; }), "new session must reset sentinel state");
    Require(CollectThreadDataWorker(state, [] { return true; }), "next session must collect");
}

int main()
{
    try {
        TestLaunchFailureAndRetry();
        TestCallbackIsNotThreadExit();
        TestFullDigestVerification();
        TestFinalizationQueueAndSnapshot();
        TestComApartmentClientLifetime();
        std::cout << "PASS: launch failure/retry, real-thread ownership barrier, strict digest verification, queued finalization, immutable request, COM client lifetime and failures\n";
        return 0;
    } catch (const std::exception& error) {
        std::cerr << "FAIL: " << error.what() << '\n';
        return 1;
    }
}
