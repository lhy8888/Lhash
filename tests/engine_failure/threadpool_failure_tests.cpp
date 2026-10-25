#include "Common/ThreadPool.h"
#include <cstdlib>
#include <iostream>
#include <new>

// Fail allocations only on the constructing thread. Worker activity therefore
// cannot accidentally consume the injection point. No production test hook is
// needed: reserve and std::thread callable storage use ordinary C++ allocation.
static thread_local int allocationsBeforeFailure = -1;

void* operator new(std::size_t size)
{
    if (allocationsBeforeFailure == 0)
    {
        allocationsBeforeFailure = -1;
        throw std::bad_alloc();
    }
    if (allocationsBeforeFailure > 0)
        --allocationsBeforeFailure;
    if (void* memory = std::malloc(size ? size : 1))
        return memory;
    throw std::bad_alloc();
}
void operator delete(void* memory) noexcept { std::free(memory); }
void operator delete(void* memory, std::size_t) noexcept { std::free(memory); }

int main()
{
    try
    {
        for (int failurePoint = 0; failurePoint != 6; ++failurePoint)
        {
            bool caught = false;
            allocationsBeforeFailure = failurePoint;
            try
            {
                ThreadPool pool(5);
            }
            catch (const std::bad_alloc&)
            {
                caught = true;
            }
            allocationsBeforeFailure = -1;
            if (!caught)
                throw std::runtime_error("constructor allocation failure was not reached");
            ThreadPool recovered(2);
            if (recovered.enqueue([] { return 42; }).get() != 42)
                throw std::runtime_error("pool recovery failed");
        }
        std::cout << "PASS constructor_allocation_failures=6 recovery_jobs=6\n";
        return 0;
    }
    catch (const std::exception& error)
    {
        allocationsBeforeFailure = -1;
        std::cerr << "FAIL: " << error.what() << '\n';
        return 1;
    }
}
