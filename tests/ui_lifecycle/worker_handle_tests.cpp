#include "stdafx.h"
#include "Adapters/MfcBridge/HashThreadLaunch.h"
#include "Adapters/MfcBridge/HashWorkerSessionLifecycle.h"
#include <iostream>
#include <stdexcept>

static HANDLE entered;
static HANDLE releaseWorker;
static HWND messageWindow;
static const UINT terminalMessage = WM_APP + 1;
static const UINT joinedMessage = WM_APP + 2;
int WINAPI HashThreadFunc(void* param)
{
	std::unique_ptr<HashThreadLaunchContext> context(static_cast<HashThreadLaunchContext*>(param));
	ThreadData& state = *context->threadData;
    SetThreadDataWorking(state, false); // simulates engine's terminal callback window
    PostMessage(messageWindow, terminalMessage, 0, 0);
    SetEvent(entered);
    return WaitForSingleObject(releaseWorker, 5000) == WAIT_OBJECT_0 ? 0 : 1;
}

int main()
{
    WinHandleGuard::UniqueWinHandle enteredOwner(CreateEvent(NULL, TRUE, FALSE, NULL));
    WinHandleGuard::UniqueWinHandle releaseOwner(CreateEvent(NULL, TRUE, FALSE, NULL));
    if (!enteredOwner.isValid() || !releaseOwner.isValid()) return 1;
    entered = enteredOwner.get();
    releaseWorker = releaseOwner.get();
    messageWindow = CreateWindowEx(0, _T("STATIC"), _T("LHash lifecycle test"), 0, 0, 0, 0, 0,
                                  HWND_MESSAGE, NULL, GetModuleHandle(NULL), NULL);
    if (messageWindow == NULL) return 1;
    ThreadData state;
    WinHandleGuard::UniqueWinHandle worker;
    struct Cleanup {
        WinHandleGuard::UniqueWinHandle& worker;
        ~Cleanup() {
            SetEvent(releaseWorker);
            if (worker.isValid()) WaitForSingleObject(worker.get(), INFINITE);
            DestroyWindow(messageWindow);
        }
    } cleanup{worker};
    unsigned int id = 0;
    if (!LaunchThreadDataWorker(state, [&] { return RestartHashWorkerThread(&worker, &state, &id) != NULL; })) return 2;
    if (WaitForSingleObject(entered, 5000) != WAIT_OBJECT_0) return 3;
    const HANDLE original = worker.get();
    const bool restartRejected = RestartHashWorkerThread(&worker, &state, &id) == NULL && worker.get() == original;
    const bool stillBusy = IsThreadDataWorking(state);
    const bool prematureCollect = CollectThreadDataWorker(state, [&] {
        return WaitForSingleObject(worker.get(), 0) == WAIT_OBJECT_0;
    });
    SetThreadDataStop(state, true);
    SetEvent(releaseWorker);
    const bool exited = WaitForSingleObject(worker.get(), 5000) == WAIT_OBJECT_0;
    const bool queued = state.executionState.workerSession.QueueFinalization(exited, [&] {
        return PostMessage(messageWindow, joinedMessage, 0, 0) != FALSE;
    });
    MSG message = {};
    const bool terminalFirst = PeekMessage(&message, messageWindow, terminalMessage, joinedMessage, PM_REMOVE) && message.message == terminalMessage;
    const bool busyWithTerminalPending = IsThreadDataWorking(state);
    const bool sentinelSecond = PeekMessage(&message, messageWindow, terminalMessage, joinedMessage, PM_REMOVE) && message.message == joinedMessage;
    state.executionState.workerSession.FinalizationHandled();
    const bool collected = exited && sentinelSecond && CollectThreadDataWorker(state, [&] {
        CloseHashWorkerThreadHandle(&worker);
        return true;
    });
    if (!restartRejected || !stillBusy || prematureCollect || !queued || !terminalFirst || !busyWithTerminalPending || !collected || IsThreadDataWorking(state)) return 4;
    std::cout << "PASS: real Windows handle, live restart rejection, queued terminal before finalization\n";
    return 0;
}
