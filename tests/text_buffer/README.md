The production `ResultTextBuffer.h` is tested directly on all native runtime
platforms. The suite verifies append-only delivery over 8192 records, reset and
stale-batch ordering, preparing-snapshot restoration, wide text/link offsets,
visible link boundary lookup, and a producer that appends while a drained batch
is displayed without holding its mutex.

The portable suite does not emulate Windows edit controls. Native Windows GUI
tests verify the actual application separately.

# Native Windows control regression

`lhash_native_text_control_tests` runs on Windows x64/ARM64 in the full runtime
suite. Its generated source contains the current production `ShowTextUpdate`
method. A thin CEdit adapter sends real USER32 messages to a read-only multiline
EDIT control; it does not emulate caret or replacement behavior.

The negative control reproduces the old `SetSel(-1,-1)` insertion at the current
caret. Each production flush compares native text, the display cache and every
link offset, with collapsed/nonempty/all selections, two file records, Unicode,
indices above 64 KiB, scrolling, clearing and restarting. It verifies the data
used by the custom overlay; it is not a full MFC painter or pixel test. Native
MFC application tests additionally check ordered records while selecting text.

`native-control-extraction.json` in the build directory records the extracted
method/source hashes and original line number. On Linux, generation and syntax
of the Python extractor can be checked, but the native executable requires
Windows.
