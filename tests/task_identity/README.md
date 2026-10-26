This target links the production `lhash_core` from the root CMake build. It
checks that every file event and retained result carries the original request's
input index, including repeated paths, open failures, new requests sharing old
result history, cancellation before startup and during the second duplicate,
and an exception in that duplicate's progress callback. Both single and
parallel policies run; interrupted and unstarted inputs must not publish a
completed digest. Vendor builds exercise real OpenSSL SHA256; stub builds use
MD5 for the same event flow. UI session IDs and task-row terminalization are
tested separately in `tests/ui_lifecycle`.
