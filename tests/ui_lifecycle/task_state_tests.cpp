#include "stdafx.h"
#include "WinMFC/FilesHashTaskModel.h"
#include <iostream>
#include <stdexcept>

static void Require(bool condition, const char* message)
{
    if (!condition) throw std::runtime_error(message);
}

static const FilesHashTaskFinishText finishText = {
    _T("cancelled"), _T("aborted"), _T("not run: cancelled"), _T("not run: aborted")};

static FilesHashTaskUpdate Update(uint64_t session, uint32_t index, FilesHashTaskState state, int progress = 0)
{
    FilesHashTaskUpdate update;
    update.id = {session, index};
    update.path = _T("same-file.txt");
    update.algorithms = _T("SHA256");
    update.status = _T("status");
    update.state = state;
    update.progress = progress;
    return update;
}

static size_t Drain(FilesHashTaskUpdateQueue& queue, FilesHashTaskModel& model)
{
    std::vector<FilesHashTaskUpdate> updates;
    queue.Drain(updates);
    for (const auto& update : updates) model.Apply(update);
    return updates.size();
}

static void DuplicatePathsAndTerminalBoundaries()
{
    FilesHashTaskUpdateQueue queue;
    FilesHashTaskModel model;
    model.BeginSession(1, {_T("same-file.txt"), _T("same-file.txt")}, _T("SHA256"), _T("pending"));
    queue.Push(Update(1, 0, FILES_HASH_TASK_RUNNING));
    queue.Push(Update(1, 0, FILES_HASH_TASK_FAILED));
    queue.Push(Update(1, 1, FILES_HASH_TASK_RUNNING));
    queue.Push(Update(1, 1, FILES_HASH_TASK_COMPLETED, 100));
    Require(Drain(queue, model) == 2, "duplicate paths must retain one terminal update per input task");
    Require(model.Rows().size() == 2 && model.Rows()[0].state == FILES_HASH_TASK_FAILED &&
            model.Rows()[1].state == FILES_HASH_TASK_COMPLETED, "input positions must retain their own distinct outcomes");
    Require(model.FinishSession(1, FilesHashSessionOutcome::Completed, finishText), "all terminal rows are a consistent completed request");
}

static void ProgressCoalescingAndTerminalImmutability()
{
    FilesHashTaskUpdateQueue queue;
    FilesHashTaskModel model;
    model.BeginSession(1, {_T("same-file.txt")}, _T("SHA256"), _T("pending"));
    queue.Push(Update(1, 0, FILES_HASH_TASK_RUNNING, 10));
    queue.Push(Update(1, 0, FILES_HASH_TASK_RUNNING, 40));
    Require(Drain(queue, model) == 1 && model.Rows()[0].progress == 40, "same-task progress should coalesce");
    queue.Push(Update(1, 0, FILES_HASH_TASK_COMPLETED, 100));
    Require(!queue.Push(Update(1, 0, FILES_HASH_TASK_RUNNING, 50)), "queued terminal must not regress to progress");
    Require(!queue.Push(Update(1, 0, FILES_HASH_TASK_FAILED)), "queued first terminal must not be replaced");
    Drain(queue, model);
    queue.Push(Update(1, 0, FILES_HASH_TASK_RUNNING, 70)); // A later batch is also harmless.
    queue.Push(Update(1, 0, FILES_HASH_TASK_FAILED));
    Drain(queue, model);
    Require(model.Rows()[0].state == FILES_HASH_TASK_COMPLETED && model.Rows()[0].progress == 100,
            "consumed terminal row must not regress in a later batch");
    model.FinishSession(1, FilesHashSessionOutcome::Completed, finishText);
}

static void CancelAndAbortOnlyUnfinishedCurrentRows()
{
    for (const auto outcome : {FilesHashSessionOutcome::Cancelled, FilesHashSessionOutcome::Aborted}) {
        FilesHashTaskModel model;
        model.BeginSession(1, {_T("history")}, _T("MD5"), _T("pending"));
        auto history = Update(1, 0, FILES_HASH_TASK_COMPLETED, 100);
        history.algorithms = _T("MD5");
        model.Apply(history);
        model.FinishSession(1, FilesHashSessionOutcome::Completed, finishText);
        model.BeginSession(2, {_T("same"), _T("same"), _T("same"), _T("same")}, _T("SHA256"), _T("pending"));
        model.Apply(Update(2, 0, FILES_HASH_TASK_COMPLETED, 100));
        model.Apply(Update(2, 1, FILES_HASH_TASK_FAILED, 20));
        model.Apply(Update(2, 2, FILES_HASH_TASK_RUNNING, 42));
        Require(model.FinishSession(2, outcome, finishText), "cancel/abort finalization should settle remaining rows");
        const auto& rows = model.Rows();
        Require(rows.size() == 5 && rows[0].state == FILES_HASH_TASK_COMPLETED && rows[0].algorithms == _T("MD5") && rows[0].progress == 100,
                "history must retain its exact prior state");
        Require(rows[1].state == FILES_HASH_TASK_COMPLETED && rows[2].state == FILES_HASH_TASK_FAILED,
                "completed and failed current tasks must not be rewritten by session shutdown");
        Require(rows[3].state == (outcome == FilesHashSessionOutcome::Cancelled ? FILES_HASH_TASK_CANCELLED : FILES_HASH_TASK_ABORTED) &&
                rows[3].progress == 42 && rows[3].finishReason == outcome, "running row must retain progress and distinguish cancellation from abort");
        Require(rows[4].state == FILES_HASH_TASK_NOT_RUN && rows[4].finishReason == outcome &&
                rows[4].status == (outcome == FilesHashSessionOutcome::Cancelled ? finishText.notRunCancelled : finishText.notRunAborted),
                "pending row must be not-run and explain cancellation versus abort");
        Require(model.Apply(Update(2, 2, FILES_HASH_TASK_COMPLETED, 100)) == -1,
                "a late event must not resurrect a finalized session");
    }
}

static void PreparationCancellationAndInconsistentCompletion()
{
    FilesHashTaskModel model;
    model.BeginSession(1, {_T("a"), _T("b")}, _T("SHA256"), _T("pending"));
    model.FinishSession(1, FilesHashSessionOutcome::Cancelled, finishText);
    Require(model.Rows()[0].state == FILES_HASH_TASK_NOT_RUN && model.Rows()[1].state == FILES_HASH_TASK_NOT_RUN,
            "preparation cancellation must mark every unstarted task not-run");
    model.BeginSession(2, {_T("a"), _T("b"), _T("c")}, _T("SHA256"), _T("pending"));
    model.Apply(Update(2, 0, FILES_HASH_TASK_COMPLETED, 100));
    model.Apply(Update(2, 1, FILES_HASH_TASK_RUNNING, 15));
    Require(!model.FinishSession(2, FilesHashSessionOutcome::Completed, finishText),
            "normal completion with unfinished rows must be reported as inconsistent");
    Require(model.Rows()[2].state == FILES_HASH_TASK_COMPLETED && model.Rows()[3].state == FILES_HASH_TASK_ABORTED &&
            model.Rows()[4].state == FILES_HASH_TASK_NOT_RUN && model.Rows()[4].finishReason == FilesHashSessionOutcome::Aborted,
            "inconsistent completion must settle unfinished rows as aborted/not-run without inventing success");
    model.BeginSession(3, {_T("a")}, _T("SHA256"), _T("pending"));
    Require(model.Apply(Update(2, 1, FILES_HASH_TASK_COMPLETED)) == -1, "old-session update must not target the same index in a new session");
    Require(!model.FinishSession(2, FilesHashSessionOutcome::Cancelled, finishText), "old-session finalization must leave the current session intact");
    Require(model.Rows().back().state == FILES_HASH_TASK_PENDING, "current task must remain unchanged by stale updates");
    model.FinishSession(3, FilesHashSessionOutcome::Aborted, finishText);
}

static void UnknownIdentityNeverCreatesRows()
{
    FilesHashTaskModel model;
    FilesHashTaskUpdateQueue queue;
    model.BeginSession(1, {_T("same-file.txt")}, _T("SHA256"), _T("pending"));
    Require(!queue.Push(Update(0, 0, FILES_HASH_TASK_COMPLETED)), "session zero is invalid");
    Require(!queue.Push(Update(1, (std::numeric_limits<uint32_t>::max)(), FILES_HASH_TASK_COMPLETED)), "invalid input index must not enter queue");
    Require(model.Apply(Update(1, 1, FILES_HASH_TASK_COMPLETED)) == -1 &&
            model.Apply(Update(2, 0, FILES_HASH_TASK_COMPLETED)) == -1, "unknown identities must not fall back to path lookup");
    Require(model.Rows().size() == 1 && model.Rows()[0].state == FILES_HASH_TASK_PENDING,
            "unknown updates must never create or alter rows");
    queue.Push(Update(1, 0, FILES_HASH_TASK_COMPLETED));
    queue.Push(Update(2, 0, FILES_HASH_TASK_FAILED));
    Require(Drain(queue, model) == 2 && model.Rows()[0].state == FILES_HASH_TASK_COMPLETED,
            "queue keys must include session and model must ignore a different session");
    model.FinishSession(1, FilesHashSessionOutcome::Completed, finishText);
}

int main()
{
    try {
        DuplicatePathsAndTerminalBoundaries();
        ProgressCoalescingAndTerminalImmutability();
        CancelAndAbortOnlyUnfinishedCurrentRows();
        PreparationCancellationAndInconsistentCompletion();
        UnknownIdentityNeverCreatesRows();
        std::cout << "PASS: task identity, duplicate inputs, terminal queue/state monotonicity, cancellation/abort, history and stale events\n";
        return 0;
    } catch (const std::exception& error) {
        std::cerr << "FAIL: " << error.what() << '\n';
        return 1;
    }
}
