#include "Common/HashEngineInternal.h"
#include "Common/ThreadPool.h"

#include <future>
#include <iostream>
#include <stdexcept>
#include <string>
#include <vector>

using namespace HashEngineInternal;

static void Require(bool condition, const std::string& message)
{
    if (!condition)
        throw std::runtime_error(message);
}

static void CheckFile(ThreadPool& pool, size_t size, unsigned int chunkSize, size_t buffers,
    bool yieldRead = false, bool yieldUpdate = false)
{
    MockFile file;
    file.length = size;
    file.yieldAfterRead = yieldRead;
    FileExecutionState state;
    state.fileAttemptState.osFile = &file;
    state.hashContexts.yieldAfterUpdate = yieldUpdate;
    HashExecutionContext context;
    const bool stopped = ProcessOpenedFileHashingParallel(&context, {}, size, false,
        chunkSize, {buffers}, &state, &pool);
    Require(!stopped && !state.fileAttemptState.readFailed, "unexpected cancellation/read failure");
    Require(state.hashContexts.bytes == size,
        "lost/repeated bytes: expected=" + std::to_string(size) +
        " consumed=" + std::to_string(state.hashContexts.bytes));
    Require(state.hashContexts.contentMatches, "chunk contents or ordering changed");
    Require(state.progressState.bytes == size, "progress differs from bytes hashed");
    Require(file.offset == size, "input was not completely read");
}

static void CheckReadFailure(ThreadPool& pool, size_t failAfter)
{
    MockFile file;
    file.length = 4096;
    file.failAfter = failAfter;
    FileExecutionState state;
    state.fileAttemptState.osFile = &file;
    HashExecutionContext context;
    const bool stopped = ProcessOpenedFileHashingParallel(&context, {}, file.length, false,
        64, {4}, &state, &pool);
    Require(!stopped && state.fileAttemptState.readFailed, "read failure was not propagated");
    Require(state.hashContexts.bytes == failAfter && state.progressState.bytes == failAfter,
        "queued bytes were lost before read failure");
    Require(state.hashContexts.contentMatches, "read failure corrupted queued contents");
}

static void CheckCancellation(ThreadPool& pool, bool beforeStart)
{
    MockFile file;
    file.length = 4096;
    FileExecutionState state;
    state.fileAttemptState.osFile = &file;
    HashExecutionContext context;
    context.stop.store(beforeStart);
    state.hashContexts.cancelContext = &context;
    state.hashContexts.cancelAfter = 64;
    const bool stopped = ProcessOpenedFileHashingParallel(&context, {}, file.length, false,
        64, {2}, &state, &pool);
    Require(stopped && !state.fileAttemptState.readFailed, "cancellation was not propagated");
    Require(state.hashContexts.contentMatches, "cancellation corrupted consumed contents");
    Require(state.progressState.bytes == state.hashContexts.bytes, "cancelled progress is inconsistent");
    if (beforeStart)
        Require(file.offset == 0 && state.hashContexts.bytes == 0, "pre-cancelled job read input");
    else
        Require(state.hashContexts.bytes >= 64 && state.hashContexts.bytes < file.length,
            "in-flight cancellation did not stop the job");
}

int main()
{
    try
    {
        ThreadPool pool(8);
        size_t boundaryCases = 0;
        for (const unsigned int chunk : {1u, 64u, 4096u})
        {
            for (const size_t buffers : {size_t{1}, size_t{2}, size_t{4}, size_t{8}})
            {
                for (const size_t size : {size_t{0}, size_t{1}, size_t{chunk - 1},
                    size_t{chunk}, size_t{chunk + 1}, size_t{2 * chunk + 17}, size_t{5 * chunk}})
                {
                    CheckFile(pool, size, chunk, buffers, true, true);
                    ++boundaryCases;
                }
            }
        }
        // Repeated small files exercise the empty-queue / partial-final-chunk race.
        // This supplements the exact byte checks; it is a stress test, not a proof
        // that a scheduling bug cannot occur on another machine.
        constexpr size_t stressIterations = 100000;
        for (size_t i = 0; i < stressIterations; ++i)
            CheckFile(pool, 145, 64, 4);

        for (const size_t failAfter : {size_t{0}, size_t{64}, size_t{128}})
            CheckReadFailure(pool, failAfter);
        CheckCancellation(pool, true);
        CheckCancellation(pool, false);
        CheckFile(pool, 145, 64, 4); // A cancelled job must not poison pool reuse.

        std::vector<std::future<void>> jobs;
        for (size_t job = 0; job < 4; ++job)
        {
            jobs.push_back(std::async(std::launch::async, [&pool, job]
            {
                for (size_t iteration = 0; iteration < 300; ++iteration)
                    CheckFile(pool, 8192 + job + 1, 1024, job + 1, true, true);
            }));
        }
        for (auto& job : jobs)
            job.get();

        std::cout << "PASS boundary_cases=" << boundaryCases
            << " final_chunk_stress=" << stressIterations
            << " read_failures=3 cancellations=2 pool_reuse=1 concurrent_jobs=1200\n";
        return 0;
    }
    catch (const std::exception& error)
    {
        std::cerr << "FAIL: " << error.what() << '\n';
        return 1;
    }
}
