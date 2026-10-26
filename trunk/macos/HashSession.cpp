#include "stdafx.h"
#include "HashSession.h"

#include "Common/CheckedArithmetic.h"
#include "Common/HashEngine.h"
#include "Common/HashInputPolicy.h"
#include "Domain/HashRequest.h"
#include "Runtime/HashExecutionContext.h"

#include <algorithm>
#include <array>
#include <atomic>
#include <cerrno>
#include <chrono>
#include <cstring>
#include <dirent.h>
#include <fcntl.h>
#include <limits>
#include <mutex>
#include <stdexcept>
#include <sys/stat.h>
#include <unistd.h>
#include <utility>

namespace lhash::mac
{
namespace
{
using Clock = std::chrono::steady_clock;

bool IsUtf8Path(const std::string& text)
{
    if (text.empty()) return false;
    for (size_t i = 0; i < text.size();)
    {
        const auto first = static_cast<unsigned char>(text[i++]);
        if (first == 0) return false;
        if (first < 0x80) continue;
        unsigned value = 0, minimum = 0;
        size_t remaining = 0;
        if (first >= 0xc2 && first <= 0xdf) { value = first & 0x1f; remaining = 1; minimum = 0x80; }
        else if (first >= 0xe0 && first <= 0xef) { value = first & 15; remaining = 2; minimum = 0x800; }
        else if (first >= 0xf0 && first <= 0xf4) { value = first & 7; remaining = 3; minimum = 0x10000; }
        else return false;
        if (remaining > text.size() - i) return false;
        while (remaining--)
        {
            const auto next = static_cast<unsigned char>(text[i++]);
            if ((next & 0xc0) != 0x80) return false;
            value = (value << 6) | (next & 63);
        }
        if (value < minimum || value > 0x10ffff || (value >= 0xd800 && value <= 0xdfff)) return false;
    }
    return true;
}

bool Terminal(TaskState state)
{
    return state != TaskState::Pending && state != TaskState::Running;
}

uint64_t ProcessedBytes(const Task& task)
{
    if (task.state == TaskState::Completed) return task.size;
    if (task.state == TaskState::Pending || task.state == TaskState::NotRun) return 0;
    const auto progress = static_cast<unsigned>(task.progress);
    return task.size / 100 * progress + task.size % 100 * progress / 100;
}

std::string Utf8(const sunjwbase::tstring& value) { return sunjwbase::tstrtostrutf8(value); }
}

namespace detail
{
struct SessionState
{
    std::mutex mutex;
    HashCancellationState cancellation;
    uint64_t id = 0;
    bool busy = true, scanning = true;
    int totalProgress = 0;
    Clock::time_point started = Clock::now(), finished;
    std::vector<Task> tasks;
    std::vector<bool> dirty;
    std::vector<size_t> changed;
    std::vector<Result> results;
    std::vector<std::string> warnings;
    size_t suppressedWarnings = 0;
    std::array<size_t, 7> counts{};
    uint64_t bytesProcessed = 0;
    std::string error;

    void Changed(size_t index)
    {
        if (!dirty[index])
        {
            changed.push_back(index);
            dirty[index] = true;
        }
    }

    void ChangeState(Task& task, TaskState state)
    {
        --counts[static_cast<size_t>(task.state)];
        ++counts[static_cast<size_t>(state)];
        task.state = state;
    }

    void Warn(const std::string& message)
    {
        std::lock_guard<std::mutex> lock(mutex);
        if (warnings.size() < 16) warnings.push_back(message);
        else ++suppressedWarnings;
    }

    bool AddTask(const std::string& path)
    {
        std::lock_guard<std::mutex> lock(mutex);
        if (tasks.size() >= kMaxHashFilesPerSession) return false;
        Task task;
        task.sessionId = id;
        task.index = static_cast<uint32_t>(tasks.size());
        task.path = path;
        tasks.push_back(std::move(task));
        dirty.push_back(false);
        ++counts[static_cast<size_t>(TaskState::Pending)];
        Changed(tasks.size() - 1);
        return true;
    }
};
}

namespace
{
using State = detail::SessionState;

// Directory handles remain relative to their opened parents. A directory that
// is replaced by a symbolic link cannot redirect traversal between stat/open.
class Directory
{
public:
    explicit Directory(int fd) : value_(fd < 0 ? nullptr : fdopendir(fd))
    {
        if (!value_ && fd >= 0) close(fd);
    }
    ~Directory() { if (value_) closedir(value_); }
    Directory(const Directory&) = delete;
    Directory& operator=(const Directory&) = delete;
    DIR* Get() const { return value_; }
private:
    DIR* value_;
};

class Scanner
{
public:
    explicit Scanner(State& state) : state_(state) { budget_.directories = 0; }

    void Collect(const std::vector<std::string>& inputs)
    {
        for (const auto& path : inputs)
        {
            if (Stopped()) break;
            struct stat info{};
            // Explicit missing/special/link inputs are retained for the shared
            // engine to report as ordinary failed tasks rather than disappearing.
            if (lstat(path.c_str(), &info) == 0 && S_ISDIR(info.st_mode))
            {
                if (!budget_.QueueDirectory(0)) { Limited(); continue; }
                Walk(open(path.c_str(), O_RDONLY | O_DIRECTORY | O_NOFOLLOW | O_CLOEXEC), path, 0);
            }
            else Add(path);
        }
        if (skippedLinks_) state_.Warn("Symbolic links were skipped while scanning folders.");
        if (skippedSpecial_) state_.Warn("Non-regular files were skipped while scanning folders.");
        if (skippedEncoding_) state_.Warn("Files whose names are not valid UTF-8 were skipped.");
    }

private:
    bool Stopped() const { return full_ || exhausted_ || state_.cancellation.stopRequested.load(); }
    void Limited()
    {
        if (!limited_)
        {
            state_.Warn("Folder scan reached its depth or directory limit; the file list is incomplete.");
            limited_ = true;
        }
    }
    void Add(const std::string& path)
    {
        if (!IsUtf8Path(path)) { skippedEncoding_ = true; return; }
        if (!state_.AddTask(path))
        {
            full_ = true;
            state_.Warn("Folder scan reached the 8192-file limit; the file list is incomplete.");
        }
    }
    void Walk(int fd, const std::string& path, size_t depth)
    {
        const int openError = errno;
        Directory directory(fd);
        if (!directory.Get())
        {
            state_.Warn("Cannot read folder: " + path + ": " + std::strerror(fd < 0 ? openError : errno));
            return;
        }
        while (!Stopped())
        {
            errno = 0;
            dirent* entry = readdir(directory.Get());
            if (!entry)
            {
                if (errno != 0) state_.Warn("Folder enumeration failed: " + path + ": " + std::strerror(errno));
                return;
            }
            const std::string name(entry->d_name);
            if (name == "." || name == "..") continue;
            if (!budget_.VisitEntry())
            {
                exhausted_ = true;
                state_.Warn("Folder scan reached the 65536-entry limit; the file list is incomplete.");
                return;
            }
            if (Stopped()) return;
            const std::string child = path + (path.back() == '/' ? "" : "/") + name;
            struct stat info{};
            if (fstatat(dirfd(directory.Get()), name.c_str(), &info, AT_SYMLINK_NOFOLLOW) != 0)
            {
                state_.Warn("Cannot inspect folder entry: " + child + ": " + std::strerror(errno));
                continue;
            }
            if (S_ISLNK(info.st_mode)) { skippedLinks_ = true; continue; }
            if (S_ISDIR(info.st_mode))
            {
                if (!budget_.QueueDirectory(depth + 1)) { Limited(); continue; }
                Walk(openat(dirfd(directory.Get()), name.c_str(), O_RDONLY | O_DIRECTORY | O_NOFOLLOW | O_CLOEXEC), child, depth + 1);
            }
            else if (S_ISREG(info.st_mode)) Add(child);
            else skippedSpecial_ = true;
        }
    }
    State& state_;
    HashFolderScanBudget budget_;
    bool full_ = false, exhausted_ = false, limited_ = false;
    bool skippedLinks_ = false, skippedSpecial_ = false, skippedEncoding_ = false;
};

class Sink final : public HashProgressSink
{
public:
    explicit Sink(State& state) : state_(state) {}
    int progressMax() override { return 100; }
    void onProgressEvent(const ProgressEvent& event) override
    {
        std::lock_guard<std::mutex> lock(state_.mutex);
        if (event.type == PROGRESS_EVENT_TOTAL_PROGRESS)
        {
            state_.totalProgress = std::clamp(event.value, 0, 100);
            return;
        }
        if (event.taskIndex >= state_.tasks.size()) return;
        auto& task = state_.tasks[event.taskIndex];
        if (Terminal(task.state)) return;
        const auto previousBytes = ProcessedBytes(task);
        switch (event.type)
        {
        case PROGRESS_EVENT_FILE_STARTED:
            state_.ChangeState(task, TaskState::Running);
            break;
        case PROGRESS_EVENT_FILE_META_READY:
            task.size = event.result.meta.size;
            break;
        case PROGRESS_EVENT_FILE_PROGRESS:
            task.progress = std::clamp(event.value, 0, 100);
            break;
        case PROGRESS_EVENT_FILE_HASH_READY:
        case PROGRESS_EVENT_FILE_FAILED:
        {
            const bool failed = event.type == PROGRESS_EVENT_FILE_FAILED;
            Result result;
            result.sessionId = state_.id;
            result.index = task.index;
            result.path = task.path;
            result.size = task.size = event.result.meta.size;
            result.modifiedDate = Utf8(event.result.meta.modifiedDate);
            result.error = task.error = Utf8(event.result.error);
            if (!failed)
            {
                for (const auto& digest : event.result.digests)
                {
                    auto value = Utf8(digest.value);
                    for (auto& c : value)
                    {
                        if (event.uppercaseDigest && c >= 'a' && c <= 'f') c = static_cast<char>(c - 'a' + 'A');
                        if (!event.uppercaseDigest && c >= 'A' && c <= 'F') c = static_cast<char>(c - 'A' + 'a');
                    }
                    result.digests.push_back({Utf8(digest.algorithmId), Utf8(digest.displayLabel), std::move(value)});
                }
            }
            state_.results.push_back(std::move(result));
            task.progress = failed ? task.progress : 100;
            state_.ChangeState(task, failed ? TaskState::Failed : TaskState::Completed);
            break;
        }
        default: return;
        }
        state_.bytesProcessed = ReplaceSizedValueUInt64(state_.bytesProcessed, previousBytes, ProcessedBytes(task));
        state_.Changed(task.index);
    }
private:
    State& state_;
};

void Execute(const std::shared_ptr<State>& state, const SessionOptions& options) noexcept
{
    bool aborted = false;
    try
    {
        Scanner(*state).Collect(options.paths);
        HashRequest request;
        {
            std::lock_guard<std::mutex> lock(state->mutex);
            state->scanning = false;
            for (const auto& task : state->tasks) request.files.push_back(sunjwbase::strtotstrutf8(task.path));
        }
        if (request.files.empty() && !state->cancellation.stopRequested.load())
            state->Warn("No regular files were found in the selected folders.");
        for (const auto& id : options.algorithmIds) AppendHashRequestAlgorithmId(request, sunjwbase::strtotstrutf8(id));
        request.uppercaseDigest = options.uppercase;
        if (options.parallel) request.digestExecutionPolicy = HASH_REQUEST_DIGEST_EXECUTION_POLICY_PARALLEL;
        if (!request.files.empty() && !state->cancellation.stopRequested.load())
        {
            Sink sink(*state);
            HashJobState job;
            HashExecutionContext context(&sink, job, state->cancellation);
            aborted = RunHashRequest(&context, request) != 0;
        }
    }
    catch (const std::exception& ex)
    {
        aborted = true;
        try { std::lock_guard<std::mutex> lock(state->mutex); state->error = ex.what(); } catch (...) {}
    }
    catch (...)
    {
        aborted = true;
        try { std::lock_guard<std::mutex> lock(state->mutex); state->error = "The hashing worker stopped unexpectedly."; } catch (...) {}
    }
    // No UI callbacks, handles or ownership are captured by the worker. Even a
    // blocked filesystem call may safely finish after its window has closed.
    std::lock_guard<std::mutex> lock(state->mutex);
    const bool cancelled = state->cancellation.stopRequested.load();
    for (auto& task : state->tasks)
    {
        if (Terminal(task.state)) continue;
        state->ChangeState(task, task.state == TaskState::Pending ? TaskState::NotRun :
            cancelled ? TaskState::Cancelled : TaskState::Aborted);
        try { state->Changed(task.index); } catch (...) {}
        if (!cancelled) aborted = true;
    }
    if (aborted && state->error.empty())
    {
        try { state->error = "The hashing session ended before all files completed."; } catch (...) {}
    }
    state->scanning = false;
    state->finished = Clock::now();
    state->busy = false;
}
}

std::vector<Algorithm> Algorithms()
{
    std::vector<Algorithm> algorithms;
    for (const auto& descriptor : GetRegisteredHashAlgorithmDescriptors())
        algorithms.push_back({Utf8(GetHashAlgorithmDescriptorId(descriptor)), descriptor.displayLabel, descriptor.enabledByDefault});
    return algorithms;
}

HashSession::~HashSession()
{
    Cancel();
    if (worker_.joinable())
    {
        // Do not make a close request wait for an uninterruptible network or
        // filesystem call. Shared state outlives a detached worker; no UI access.
        if (Busy()) worker_.detach();
        else worker_.join();
    }
}

bool HashSession::Start(const SessionOptions& options, std::string& error)
{
    error.clear();
    if (Busy()) { error = "A session is already running."; return false; }
    if (options.paths.empty()) { error = "Choose at least one file or folder."; return false; }
    if (options.paths.size() > kMaxHashFilesPerSession) { error = "A session accepts at most 8192 inputs."; return false; }
    if (options.algorithmIds.empty()) { error = "Select at least one algorithm."; return false; }
    for (const auto& path : options.paths)
        if (!IsUtf8Path(path)) { error = "Paths must be nonempty, valid UTF-8 without embedded nulls."; return false; }
    for (const auto& id : options.algorithmIds)
        if (!IsUtf8Path(id) || !IsRegisteredHashAlgorithmId(sunjwbase::strtotstrutf8(id)))
        { error = "An unavailable algorithm was selected."; return false; }
    try
    {
        if (worker_.joinable()) worker_.join();
        auto state = std::make_shared<State>();
        state->id = ++nextSession_;
        // Bounded capacities make callback bookkeeping allocation-free after
        // discovery, including the terminal cleanup on cancellation/exception.
        state->tasks.reserve(kMaxHashFilesPerSession);
        state->dirty.reserve(kMaxHashFilesPerSession);
        state->changed.reserve(kMaxHashFilesPerSession);
        state->results.reserve(kMaxHashFilesPerSession);
        worker_ = std::thread(Execute, state, options);
        state_ = std::move(state);
        return true;
    }
    catch (const std::exception& ex) { error = ex.what(); return false; }
}

void HashSession::Cancel()
{
    if (state_ && Busy()) state_->cancellation.stopRequested.store(true);
}

bool HashSession::Busy() const
{
    if (!state_) return false;
    std::lock_guard<std::mutex> lock(state_->mutex);
    return state_->busy;
}

Update HashSession::Poll()
{
    Update update;
    if (!state_) return update;
    std::lock_guard<std::mutex> lock(state_->mutex);
    update.sessionId = state_->id;
    update.busy = state_->busy;
    update.scanning = state_->scanning;
    update.cancelRequested = state_->cancellation.stopRequested.load();
    update.totalProgress = state_->totalProgress;
    update.elapsedSeconds = std::chrono::duration<double>((state_->busy ? Clock::now() : state_->finished) - state_->started).count();
    update.bytesProcessed = state_->bytesProcessed;
    update.taskCount = state_->tasks.size();
    update.completed = state_->counts[static_cast<size_t>(TaskState::Completed)];
    update.failed = state_->counts[static_cast<size_t>(TaskState::Failed)];
    update.cancelled = state_->counts[static_cast<size_t>(TaskState::Cancelled)];
    update.notRun = state_->counts[static_cast<size_t>(TaskState::NotRun)];
    update.aborted = state_->counts[static_cast<size_t>(TaskState::Aborted)];
    update.error.swap(state_->error);
    for (size_t index : state_->changed)
    {
        update.tasks.push_back(state_->tasks[index]);
        state_->dirty[index] = false;
    }
    state_->changed.clear();
    update.results.swap(state_->results);
    update.warnings.swap(state_->warnings);
    if (state_->suppressedWarnings)
    {
        update.warnings.push_back(std::to_string(state_->suppressedWarnings) + " additional folder warnings were omitted.");
        state_->suppressedWarnings = 0;
    }
    return update;
}
}
