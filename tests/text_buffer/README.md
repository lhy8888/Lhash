The production `ResultTextBuffer.h` is tested directly on all native runtime
platforms. The suite verifies append-only delivery over 8192 records, reset and
stale-batch ordering, preparing-snapshot restoration, wide text/link offsets,
visible link boundary lookup, and a producer that appends while a drained batch
is displayed without holding its mutex.

This suite does not emulate Windows edit controls. Native Windows GUI tests
verify the complete result text and clear/next-session behavior separately.
