#include "Adapters/MfcBridge/HashWorkerTiming.h"
#include <atomic>
#include <cmath>
#include <iostream>
#include <stdexcept>
#include <thread>

static void Require(bool condition, const char* message)
{
    if (!condition) throw std::runtime_error(message);
}

static void RequireSeconds(double actual, double expected, const char* message)
{
    Require(std::abs(actual - expected) < 0.0000001, message);
}

static void MissingTicksAndDelayedUiCompletion()
{
    using namespace std::chrono;
    HashWorkerTiming timing;
    const auto start = HashWorkerTiming::TimePoint{} + seconds(100);
    RequireSeconds(timing.ElapsedSeconds(start), 0, "unstarted timing must display zero");
    timing.Start(start);
    RequireSeconds(timing.ElapsedSeconds(start + milliseconds(600)), 0.6, "one late tick must reflect actual elapsed time");
    RequireSeconds(timing.ElapsedSeconds(start + milliseconds(1200)), 1.2, "two late ticks must not be counted as 0.2 seconds");
    timing.Finish(start + milliseconds(1250));
    RequireSeconds(timing.ElapsedSeconds(start + hours(1)), 1.25, "UI completion delays must not change the worker duration");
    timing.Finish(start + hours(2));
    RequireSeconds(timing.ElapsedSeconds(start + hours(3)), 1.25, "duplicate completion must preserve the first worker end");
    timing.Reset();
    RequireSeconds(timing.ElapsedSeconds(start + hours(3)), 0, "reset must clear a finished duration");
    timing.Finish(start + hours(3));
    RequireSeconds(timing.ElapsedSeconds(start + hours(4)), 0, "finish without a start must not create a measurement");
    timing.Start(start + hours(5));
    timing.Finish(start + hours(5) + microseconds(250));
    RequireSeconds(timing.ElapsedSeconds(start + hours(6)), 0.00025,
                   "a worker that finishes before the first UI tick must still have a duration");
    timing.Start(start);
    RequireSeconds(timing.ElapsedSeconds(start - seconds(1)), 0, "a defensive pre-start sample must not return negative elapsed time");
}

static void ScopeExitAndExceptionsFreezeTime()
{
    HashWorkerTiming timing;
    try {
        HashWorkerTimingScope scope(timing);
        throw std::runtime_error("injected worker failure");
    } catch (const std::runtime_error&) {}
    const double completed = timing.ElapsedSeconds();
    Require(completed >= 0, "exceptional work must leave a valid elapsed duration");
    RequireSeconds(timing.ElapsedSeconds(HashWorkerTiming::Clock::now() + std::chrono::hours(1)), completed,
                   "worker exception must finish timing before the UI observes the exit");
    timing.Reset();
    {
        HashWorkerTimingScope scope(timing);
    }
    const double shortCompleted = timing.ElapsedSeconds();
    RequireSeconds(timing.ElapsedSeconds(HashWorkerTiming::Clock::now() + std::chrono::hours(1)), shortCompleted,
                   "normal scope exit must freeze even an extremely short worker");
}

static void ConcurrentWorkerBoundariesAndUiReads()
{
    using namespace std::chrono;
    HashWorkerTiming timing;
    const auto start = HashWorkerTiming::TimePoint{} + seconds(100);
    std::atomic<bool> done{false};
    std::atomic<bool> invalid{false};
    std::atomic<unsigned int> reads{0};
    std::thread reader([&] {
        while (!done.load()) {
            const double elapsed = timing.ElapsedSeconds(start + seconds(900));
            // Reads may observe idle, running, or completed, never a torn pair.
            if (elapsed != 0 && elapsed != 900 && elapsed != 2) invalid.store(true);
            ++reads;
        }
    });
    while (reads.load() == 0) std::this_thread::yield();
    for (int index = 0; index < 10000; ++index) {
        timing.Reset();
        timing.Start(start);
        timing.Finish(start + seconds(2));
    }
    done.store(true);
    reader.join();
    Require(!invalid.load(), "UI reads must observe coherent worker boundaries");
    RequireSeconds(timing.ElapsedSeconds(start + hours(24)), 2, "final completed duration must remain stable after joining");
}

int main()
{
    try {
        MissingTicksAndDelayedUiCompletion();
        ScopeExitAndExceptionsFreezeTime();
        ConcurrentWorkerBoundariesAndUiReads();
        std::cout << "PASS: steady-clock worker timing, missing ticks, delayed UI, short jobs, reset, exception and concurrent reads\n";
        return 0;
    } catch (const std::exception& error) {
        std::cerr << "FAIL: " << error.what() << '\n';
        return 1;
    }
}
