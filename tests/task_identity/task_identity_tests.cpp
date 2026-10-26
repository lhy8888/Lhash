#include "stdafx.h"

#include "Common/HashEngine.h"
#include "Domain/HashRequest.h"
#include "Runtime/HashExecutionContext.h"

#include <filesystem>
#include <fstream>
#include <iostream>
#include <random>
#include <stdexcept>
#include <string>
#include <vector>

namespace fs = std::filesystem;

namespace
{
    void Require(bool condition, const char* message)
    {
        if (!condition) throw std::runtime_error(message);
    }

    class Fixture
    {
    public:
        Fixture()
        {
            std::random_device random;
            for (unsigned attempt = 0; attempt < 100; ++attempt)
            {
                const auto candidate = fs::temp_directory_path() /
                    ("lhash-task-identity-" + std::to_string(random()) + "-" + std::to_string(random()));
                if (fs::create_directory(candidate)) { root = candidate; break; }
            }
            Require(!root.empty(), "Cannot create isolated fixture directory");
            std::ofstream output(root / "repeated.bin", std::ios::binary);
            const std::string data(8 * 1024 * 1024 + 17, 'a');
            output.write(data.data(), static_cast<std::streamsize>(data.size()));
            output.close();
            Require(!output.fail(), "Cannot write fixture");
        }
        ~Fixture()
        {
            std::error_code ignored;
            fs::remove_all(root, ignored);
        }
        sunjwbase::tstring Path(const char* name) const
        {
            return sunjwbase::strtotstrutf8((root / name).u8string());
        }
    private:
        fs::path root;
    };

    enum class StopMode { None, BeforeStart, CancelSecond, ThrowSecond };

    class EventRecorder : public HashProgressSink
    {
    public:
        EventRecorder(HashCancellationState& cancellation, StopMode mode)
            : cancellation_(cancellation), mode_(mode) {}

        int progressMax() override { return 100; }
        void onProgressEvent(const ProgressEvent& event) override
        {
            events.push_back(event);
            if (mode_ == StopMode::BeforeStart && event.type == PROGRESS_EVENT_JOB_PREPARING)
                cancellation_.stopRequested.store(true);
            if (event.type == PROGRESS_EVENT_FILE_PROGRESS && event.taskIndex == 1 && event.value > 0)
            {
                if (mode_ == StopMode::CancelSecond) cancellation_.stopRequested.store(true);
                if (mode_ == StopMode::ThrowSecond) throw std::runtime_error("injected worker callback failure");
            }
        }

        void CheckIdentity(const HashRequest& request) const
        {
            uint32_t active = kInvalidHashTaskIndex;
            for (const auto& event : events)
            {
                if (event.type == PROGRESS_EVENT_FILE_STARTED)
                {
                    Require(event.taskIndex < request.files.size(), "FILE_STARTED has no input index");
                    active = event.taskIndex;
                }
                switch (event.type)
                {
                case PROGRESS_EVENT_FILE_STARTED:
                case PROGRESS_EVENT_FILE_META_READY:
                case PROGRESS_EVENT_FILE_HASH_READY:
                case PROGRESS_EVENT_FILE_FAILED:
                    Require(event.result.taskIndex == event.taskIndex, "result/event identity mismatch");
                    Require(event.taskIndex < request.files.size() && event.result.path == request.files[event.taskIndex],
                        "result identity does not address its original input");
                    [[fallthrough]];
                case PROGRESS_EVENT_FILE_PROGRESS:
                case PROGRESS_EVENT_FILE_CALCULATED:
                case PROGRESS_EVENT_FILE_FINISHED:
                    Require(active != kInvalidHashTaskIndex && event.taskIndex == active,
                        "file event has an absent or stale task index");
                    break;
                default:
                    Require(event.taskIndex == kInvalidHashTaskIndex, "job/global event acquired a file identity");
                    break;
                }
            }
        }

        std::vector<uint32_t> Indices(ProgressEventType type) const
        {
            std::vector<uint32_t> indices;
            for (const auto& event : events) if (event.type == type) indices.push_back(event.taskIndex);
            return indices;
        }

        std::vector<ProgressEvent> events;
    private:
        HashCancellationState& cancellation_;
        StopMode mode_;
    };

    HashRequest MakeRequest(const Fixture& fixture, bool parallel, bool includeMissing)
    {
        HashRequest request;
        request.files = { fixture.Path("repeated.bin"),
            fixture.Path(includeMissing ? "missing.bin" : "repeated.bin"), fixture.Path("repeated.bin") };
        request.digestExecutionPolicy = parallel ? HASH_REQUEST_DIGEST_EXECUTION_POLICY_PARALLEL :
            HASH_REQUEST_DIGEST_EXECUTION_POLICY_SINGLE_THREADED;
#if defined(LHASH_WITH_OPENSSL3_VENDOR)
        AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr("openssl-sha-256"));
#else
        AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr("md5"));
#endif
        return request;
    }

    void CheckRepeatedPaths(const Fixture& fixture, bool parallel)
    {
        HashRequest request = MakeRequest(fixture, parallel, true);
        HashJobState state;
        HashResult history;
        history.path = sunjwbase::strtotstr("old-session");
        history.taskIndex = 77;
        state.results.push_back(history);
        HashCancellationState cancellation;
        EventRecorder recorder(cancellation, StopMode::None);
        HashExecutionContext context(&recorder, state, cancellation);
        Require(RunHashRequest(&context, request) == 0, "repeated-path request failed");
        recorder.CheckIdentity(request);
        Require(recorder.Indices(PROGRESS_EVENT_FILE_STARTED) == std::vector<uint32_t>({0, 1, 2}),
            "repeated paths lost their input identities");
        Require(recorder.Indices(PROGRESS_EVENT_FILE_HASH_READY) == std::vector<uint32_t>({0, 2}),
            "repeated-path success events were merged");
        Require(recorder.Indices(PROGRESS_EVENT_FILE_FAILED) == std::vector<uint32_t>({1}),
            "open failure has the wrong input identity");
        Require(recorder.Indices(PROGRESS_EVENT_FILE_FINISHED) == std::vector<uint32_t>({0, 1, 2}),
            "finished events lost their identity");
        Require(state.results.size() == 4 && state.results.front().taskIndex == 77,
            "existing result history changed");
        auto result = state.results.begin();
        for (uint32_t index = 0; index != 3; ++index)
            Require((++result)->taskIndex == index, "persisted result index is based on history instead of input");

        // A retained execution context and result history do not shift the next
        // request's index. The UI provides the separate session boundary.
        recorder.events.clear();
        request.files.resize(1);
        Require(RunHashRequest(&context, request) == 0, "second request failed");
        recorder.CheckIdentity(request);
        Require(recorder.Indices(PROGRESS_EVENT_FILE_STARTED) == std::vector<uint32_t>({0}) &&
            state.results.back().taskIndex == 0, "task index did not restart at the new request");
    }

    void CheckInterruptedPaths(const Fixture& fixture, bool parallel, StopMode mode)
    {
        HashRequest request = MakeRequest(fixture, parallel, false);
        HashJobState state;
        HashCancellationState cancellation;
        EventRecorder recorder(cancellation, mode);
        HashExecutionContext context(&recorder, state, cancellation);
        bool caught = false;
        try { Require(RunHashRequest(&context, request) == 0, "cancelled request failed"); }
        catch (const std::runtime_error& error)
        {
            if (std::string(error.what()) != "injected worker callback failure") throw;
            caught = true;
        }
        Require(caught == (mode == StopMode::ThrowSecond), "worker exception behavior changed");
        Require(!state.working.load(), "interrupted request left working set");
        recorder.CheckIdentity(request);
        const bool beforeStart = mode == StopMode::BeforeStart;
        Require(recorder.Indices(PROGRESS_EVENT_FILE_STARTED) ==
            (beforeStart ? std::vector<uint32_t>() : std::vector<uint32_t>({0, 1})),
            "an unstarted duplicate acquired a started event");
        Require(recorder.Indices(PROGRESS_EVENT_FILE_HASH_READY) ==
            (beforeStart ? std::vector<uint32_t>() : std::vector<uint32_t>({0})),
            "interrupted/unstarted duplicate published a completed digest");
        Require(recorder.Indices(PROGRESS_EVENT_JOB_COMPLETED).empty(), "interrupted job reported completion");
        Require(recorder.Indices(PROGRESS_EVENT_JOB_CANCELLED).size() == (caught ? 0u : 1u),
            "cancellation event behavior changed");
    }
}

int main()
{
    try
    {
        Fixture fixture;
        for (bool parallel : {false, true})
        {
            CheckRepeatedPaths(fixture, parallel);
            for (StopMode mode : {StopMode::BeforeStart, StopMode::CancelSecond, StopMode::ThrowSecond})
                CheckInterruptedPaths(fixture, parallel, mode);
        }
        std::cout << "PASS task identity: duplicate paths, errors, retained history, new requests, cancellation and exceptions; single + parallel\n";
        return 0;
    }
    catch (const std::exception& error)
    {
        std::cerr << "FAIL: " << error.what() << '\n';
        return 1;
    }
}
