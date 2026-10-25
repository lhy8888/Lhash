# Digest queue regression

Build and run on Linux, macOS, or Windows:

```sh
cmake -S tests/digest_queue -B build/digest-queue
cmake --build build/digest-queue --config Release
ctest --test-dir build/digest-queue -C Release --output-on-failure
```

This target compiles the actual `HashDigestQueue.cpp` and `ThreadPool.h`. Narrow
file-read and digest/progress callback doubles let it test queue behavior without
a platform filesystem backend or OpenSSL. It verifies exact byte order, complete
input consumption, progress, empty files, exact/partial chunk boundaries, bounded
queues, cancellation, read failures, pool reuse, and concurrent jobs. It does not
replace the real-file engine or OpenSSL digest-vector suites.

Short positive reads must be consumed until a zero-byte EOF. Injected producer,
digest and progress exceptions must wake both sides and propagate; a barrier test
also verifies that producer failure waits for an in-flight consumer before its
buffer can be released.

The 100,000-iteration partial-final-chunk stress covers an observed regression in
which EOF became visible before the final chunk was queued. As with any scheduler
stress test, a passing run is not a proof of absence of races. CTest applies a
120-second timeout so deadlocks fail CI rather than hanging indefinitely.
