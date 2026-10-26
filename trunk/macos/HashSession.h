#pragma once

#include <cstddef>
#include <cstdint>
#include <memory>
#include <string>
#include <thread>
#include <vector>

namespace lhash::mac
{
struct Algorithm
{
    std::string id, label;
    bool enabledByDefault = false;
};

std::vector<Algorithm> Algorithms();

struct SessionOptions
{
    // Local UTF-8 paths. Folders are expanded on the worker, without following
    // symbolic links. Explicit repeated file inputs remain separate tasks.
    std::vector<std::string> paths, algorithmIds;
    bool uppercase = false;
    // False uses the engine's automatic policy; true requests parallel digests.
    bool parallel = false;
};

enum class TaskState { Pending, Running, Completed, Failed, Cancelled, NotRun, Aborted };

struct Task
{
    uint64_t sessionId = 0;
    uint32_t index = 0;
    std::string path;
    TaskState state = TaskState::Pending;
    int progress = 0;
    uint64_t size = 0;
    std::string error;
};

struct Digest { std::string id, label, value; };

struct Result
{
    uint64_t sessionId = 0;
    uint32_t index = 0;
    std::string path;
    uint64_t size = 0;
    std::string modifiedDate, error;
    std::vector<Digest> digests;
};

struct Update
{
    uint64_t sessionId = 0;
    bool busy = false, scanning = false, cancelRequested = false;
    int totalProgress = 0;
    double elapsedSeconds = 0;
    // Completed bytes plus estimates from active/interrupted task progress.
    uint64_t bytesProcessed = 0;
    size_t taskCount = 0, completed = 0, failed = 0, cancelled = 0, notRun = 0, aborted = 0;
    // Only changed tasks; at most one update per task per Poll. Task identities
    // are (sessionId,index), never paths. The UI owns its retained history.
    std::vector<Task> tasks;
    // New complete records, never coalesced. Poll transfers their ownership.
    std::vector<Result> results;
    std::vector<std::string> warnings;
    // A worker error is delivered once, like warnings and result records.
    std::string error;
};

namespace detail { struct SessionState; }

class HashSession
{
public:
    HashSession() = default;
    ~HashSession();
    HashSession(const HashSession&) = delete;
    HashSession& operator=(const HashSession&) = delete;

    // Start, Cancel and Poll are called by the UI thread. Drain the final Poll
    // before starting another session. Start performs no filesystem operations.
    bool Start(const SessionOptions& options, std::string& error);
    void Cancel();
    bool Busy() const;
    Update Poll();

private:
    std::shared_ptr<detail::SessionState> state_;
    std::thread worker_;
    uint64_t nextSession_ = 0;
};
}
