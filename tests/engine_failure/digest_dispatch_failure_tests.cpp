#include "stdafx.h"
#include "Common/HashEngineInternal.h"
#include "Common/ThreadPool.h"
#include <atomic>
#include <chrono>
#include <cstdlib>
#include <iostream>
#include <new>

using namespace HashEngineInternal;

// The test calls the production updater directly with two descriptors. Registry
// resolution is outside this test's boundary and is not used by the direct call.
namespace HashEngineInternal
{
    bool IsHashDigestOperationRegistryConsistent() { return true; }
    bool TryGetHashDigestOperationDescriptorById(const HashAlgorithmId&, HashDigestOperationDescriptor*) { return false; }
    bool IsHashDigestOperationDescriptorComplete(const HashDigestOperationDescriptor&) { return true; }
}

static thread_local bool failLargeDescriptorCopy = false;
static std::shared_future<void>* firstUpdateStarted = nullptr;
static std::atomic<bool> injectionReached(false);

void* operator new(std::size_t size)
{
    if (failLargeDescriptorCopy && size >= 4096)
    {
        failLargeDescriptorCopy = false;
        // Ensure an earlier dispatched task is still using its buffer/context
        // when the second descriptor's capture fails to allocate.
        firstUpdateStarted->wait();
        injectionReached.store(true);
        throw std::bad_alloc();
    }
    if (void* memory = std::malloc(size ? size : 1))
        return memory;
    throw std::bad_alloc();
}
void operator delete(void* memory) noexcept { std::free(memory); }
void operator delete(void* memory, std::size_t) noexcept { std::free(memory); }

static void BlockingUpdate(FileHashContexts& contexts, unsigned char* data, unsigned int size)
{
    contexts.started->set_value();
    contexts.release->wait();
    if (size != 1 || data[0] != 42)
        throw std::runtime_error("in-flight buffer changed");
    ++contexts.completed;
}
static void UnreachableUpdate(FileHashContexts&, unsigned char*, unsigned int)
{
    throw std::runtime_error("second update unexpectedly ran");
}

int main()
{
    try
    {
        ThreadPool pool(2);
        std::promise<void> started;
        auto startedFuture = started.get_future().share();
        firstUpdateStarted = &startedFuture;
        std::promise<void> release;
        auto releaseFuture = release.get_future().share();
        FileHashContexts contexts;
        contexts.started = &started;
        contexts.release = &releaseFuture;
        DigestUpdateRequest request;
        request.operationDescriptors.push_back({_T("first"), nullptr, &BlockingUpdate, nullptr});
        request.operationDescriptors.push_back({HashAlgorithmId(8192, _T('x')), nullptr, &UnreachableUpdate, nullptr});
        unsigned char data[] = {42};
        auto dispatch = std::async(std::launch::async, [&]
        {
            failLargeDescriptorCopy = true;
            try
            {
                UpdateDigestContextsParallel(request, contexts, data, 1, &pool);
            }
            catch (const std::bad_alloc&)
            {
                failLargeDescriptorCopy = false;
                return true;
            }
            failLargeDescriptorCopy = false;
            return false;
        });
        const auto deadline = std::chrono::steady_clock::now() + std::chrono::seconds(5);
        while (!injectionReached.load() && std::chrono::steady_clock::now() < deadline)
            std::this_thread::yield();
        const bool waited = dispatch.wait_for(std::chrono::milliseconds(30)) == std::future_status::timeout;
        release.set_value();
        const bool propagated = dispatch.get();
        if (!injectionReached.load() || !waited || !propagated || contexts.completed != 1)
            throw std::runtime_error("dispatch failure did not drain existing updates before propagating");
        std::cout << "PASS dispatch_failure_waits_for_inflight_update=1\n";
        return 0;
    }
    catch (const std::exception& error)
    {
        std::cerr << "FAIL: " << error.what() << '\n';
        return 1;
    }
}
