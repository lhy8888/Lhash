# Engine failure recovery

```sh
cmake -S tests/engine_failure -B build/engine-failure
cmake --build build/engine-failure --config Release
ctest --test-dir build/engine-failure -C Release --output-on-failure
```

The entry-point test compiles production `HashEngine.cpp` and lifecycle code,
injects preparation/scheduler failures at a narrow test boundary, and throws from
real progress-sink callbacks. It checks that every exception propagates and clears
`working`. The ThreadPool test compiles the production header and fails successive
C++ allocations during construction, including after workers have started. Each
failure must return to the caller and permit a fresh pool to complete work.
The production digest updater is also tested with a deliberately failed descriptor
copy after an earlier update has started. A barrier verifies that the earlier task
finishes before the allocation exception reaches the caller.

These tests complement the actual producer/consumer exception, short-read and
buffer-lifetime cases in `tests/digest_queue`, and the real-file engine tests.
CTest timeouts turn a cleanup deadlock into a test failure.
