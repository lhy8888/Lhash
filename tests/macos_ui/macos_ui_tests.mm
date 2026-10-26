#import <Cocoa/Cocoa.h>
#import "AppController.h"

#include <chrono>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <fcntl.h>
#include <functional>
#include <stdexcept>
#include <string>
#include <unistd.h>

// The production destination reads the native dragging pasteboard; no fake
// filesystem or hashing service is substituted in this handler test.
@interface LHTestDrag : NSObject
@property(nonatomic, strong) NSPasteboard *draggingPasteboard;
@end
@implementation LHTestDrag
@end

namespace
{
unsigned cases = 0;
NSString *fixtureRoot;
NSUserDefaults *testDefaults;
NSDictionary *abcDigests;

void Require(bool condition, const char *message)
{
    if (!condition) throw std::runtime_error(message);
}

void Pump(double seconds = 0.01)
{
    const auto deadline = std::chrono::steady_clock::now() + std::chrono::duration<double>(seconds);
    do
    {
        @autoreleasepool
        {
            NSEvent *event = [NSApp nextEventMatchingMask:NSEventMaskAny
                untilDate:[NSDate dateWithTimeIntervalSinceNow:0.002]
                inMode:NSDefaultRunLoopMode dequeue:YES];
            if (event) [NSApp sendEvent:event];
            [[NSRunLoop currentRunLoop] runMode:NSDefaultRunLoopMode
                beforeDate:[NSDate dateWithTimeIntervalSinceNow:0.002]];
            [NSApp updateWindows];
        }
    } while (std::chrono::steady_clock::now() < deadline);
}

void Finish(LHAppController *controller, double timeout = 45)
{
    const auto deadline = std::chrono::steady_clock::now() + std::chrono::duration<double>(timeout);
    while (controller.isBusy && std::chrono::steady_clock::now() < deadline)
    {
        [controller pollSession];
        Pump();
    }
    [controller pollSession];
    Require(!controller.isBusy, "hash session failed to finish before timeout");
}

LHAppController *Controller()
{
    LHAppController *controller = [[LHAppController alloc] initWithDefaults:testDefaults];
    [controller showWindow:nil];
    Pump();
    Require(controller.window != nil && controller.window.isVisible, "native app window did not open");
    return controller;
}

NSString *File(NSString *name, NSData *data)
{
    NSString *path = [fixtureRoot stringByAppendingPathComponent:name];
    NSError *error = nil;
    Require([data writeToFile:path options:NSDataWritingAtomic error:&error], "could not create input fixture");
    return path;
}

NSString *AbcFile(NSString *name)
{
    return File(name, [@"abc" dataUsingEncoding:NSUTF8StringEncoding]);
}

void Start(LHAppController *controller, NSArray<NSString *> *paths)
{
    NSError *error = nil;
    Require([controller startPaths:paths error:&error], "controller refused valid file selection");
    Require(error == nil, "successful file selection returned an error");
}

void Select(LHAppController *controller, NSArray<NSString *> *ids)
{
    NSError *error = nil;
    Require([controller selectAlgorithmIDs:ids error:&error], "controller refused valid algorithms");
    Require(error == nil, "successful algorithm selection returned an error");
}

NSUInteger Occurrences(NSString *haystack, NSString *needle)
{
    NSUInteger count = 0, offset = 0;
    while (offset < haystack.length)
    {
        NSRange found = [haystack rangeOfString:needle options:0 range:NSMakeRange(offset, haystack.length - offset)];
        if (found.location == NSNotFound) break;
        ++count;
        offset = NSMaxRange(found);
    }
    return count;
}

NSString *ViewText(NSView *view)
{
    if ([view isKindOfClass:[NSTextField class]]) return [(NSTextField *)view stringValue];
    if ([view isKindOfClass:[NSTextView class]]) return [(NSTextView *)view string];
    NSMutableString *result = [NSMutableString string];
    for (NSView *child in view.subviews) [result appendString:ViewText(child)];
    return result;
}

void AssertTerminalRows(LHAppController *controller, NSInteger expected)
{
    NSTableView *table = controller.taskTableView;
    Require(table.numberOfRows == expected, "task rows were lost or duplicated");
    Require(table.numberOfColumns == 4, "task table must have File/Algorithms/Status/Progress columns");
    for (NSInteger row = 0; row < expected; ++row)
    {
        NSString *status = ViewText([table viewAtColumn:2 row:row makeIfNecessary:YES]);
        Require(status.length != 0, "terminal task row has no status");
        NSString *lower = status.lowercaseString;
        Require(![lower containsString:@"running"] && ![lower containsString:@"pending"] &&
            ![lower containsString:@"waiting"] && ![status containsString:@"运行中"] &&
            ![status containsString:@"等待中"], "finished worker left a nonterminal task row");
    }
}

void Screenshot(LHAppController *controller)
{
    const char *directory = std::getenv("LHASH_UI_ARTIFACT_DIR");
    if (!directory || !*directory) return;
    NSString *dir = [[NSString alloc] initWithUTF8String:directory];
    NSError *error = nil;
    Require([[NSFileManager defaultManager] createDirectoryAtPath:dir withIntermediateDirectories:YES
        attributes:nil error:&error], "could not create UI artifact directory");
    [controller.window displayIfNeeded];
    NSView *view = controller.window.contentView;
    NSBitmapImageRep *bitmap = [view bitmapImageRepForCachingDisplayInRect:view.bounds];
    Require(bitmap != nil, "native view screenshot allocation failed");
    [view cacheDisplayInRect:view.bounds toBitmapImageRep:bitmap];
    NSData *png = [bitmap representationUsingType:NSBitmapImageFileTypePNG properties:@{}];
    Require([png writeToFile:[dir stringByAppendingPathComponent:@"LHash-macos-ui.png"]
        options:NSDataWritingAtomic error:&error], "native view screenshot write failed");
}

void Test(const char *name, const std::function<void()>& body)
{
    @autoreleasepool
    {
        body();
        ++cases;
        std::printf("PASS macOS GUI: %s\n", name);
        std::fflush(stdout);
    }
}

void LayoutAndDefaults()
{
    LHAppController *controller = Controller();
    Require(controller.resultTextView != nil && !controller.resultTextView.isEditable &&
        controller.resultTextView.isSelectable, "result text must be read-only and selectable");
    Require(controller.taskTableView.numberOfColumns == 4, "Windows-compatible task columns missing");
    NSMutableArray<NSString *> *buttons = [NSMutableArray array];
    std::function<void(NSView *)> visit = [&](NSView *view)
    {
        if ([view isKindOfClass:[NSButton class]]) [buttons addObject:[(NSButton *)view title]];
        for (NSView *child in view.subviews) visit(child);
    };
    visit(controller.window.contentView);
    Require(buttons.count >= 6, "main window is missing the six primary commands");
    NSString *path = AbcFile(@"默认 摘要 😀.bin");
    Start(controller, @[path]);
    Finish(controller);
    AssertTerminalRows(controller, 1);
    NSString *text = controller.resultTextView.string.lowercaseString;
    Require([text containsString:path.lowercaseString], "Unicode path was not preserved in native result text");
    for (NSString *id in @[@"openssl-sha-256", @"openssl-sha-512", @"openssl-sha3-256"])
        Require([text containsString:[NSString stringWithFormat:@": %@\n", abcDigests[id]]],
            "default digest does not match independent reference vector");
    Require(![text containsString:abcDigests[@"md5"]], "MD5 unexpectedly enabled by default");
    Screenshot(controller);
    [controller close];
}

void DuplicatesAndVerification()
{
    LHAppController *controller = Controller();
    NSString *path = AbcFile(@"duplicate.bin");
    Start(controller, @[path, path]);
    Finish(controller);
    AssertTerminalRows(controller, 2);
    NSString *original = [controller.resultTextView.string copy];
    Require(Occurrences(original, path) == 2, "duplicate paths lost separate result records");
    NSError *error = nil;
    NSString *digest = abcDigests[@"openssl-sha-256"];
    NSString *query = [NSString stringWithFormat:@" \n%@\t", digest.uppercaseString];
    Require([controller verifyDigest:query error:&error] == 2 && error == nil,
        "verify must trim whitespace and match complete case-insensitive digest values");
    [controller clearVerification];
    Require([controller.resultTextView.string isEqualToString:original], "clear verification did not restore retained results");
    Require([controller verifyDigest:[digest substringToIndex:16] error:&error] == 0,
        "verification incorrectly accepted a digest prefix");
    [controller clearVerification];
    for (NSString *invalid in @[@"", @"a", @"not-a-digest", @"ab cd", @"１２"])
    {
        error = nil;
        Require([controller verifyDigest:invalid error:&error] == NSNotFound && error != nil,
            "invalid verification input did not report an error");
    }
    Start(controller, @[path]);
    Finish(controller);
    AssertTerminalRows(controller, 3);
    Require(Occurrences(controller.resultTextView.string, path) == 3,
        "a later session overwrote history or reused task identity");
    [controller clearResults];
    Require(controller.taskTableView.numberOfRows == 0, "clear did not clear task history");
    Require(![controller.resultTextView.string containsString:path], "clear left old result text");
    [controller close];
}

void AllAlgorithmsAndSettings()
{
    LHAppController *controller = Controller();
    NSArray<NSString *> *all = [[abcDigests allKeys] sortedArrayUsingSelector:@selector(compare:)];
    Require(all.count == 18, "reference fixture must cover all 18 algorithms");
    Select(controller, all);
    NSError *error = nil;
    Require(![controller selectAlgorithmIDs:@[] error:&error] && error != nil,
        "empty algorithm selection must be rejected");
    error = nil;
    Require(![controller selectAlgorithmIDs:@[@"made-up-algorithm"] error:&error] && error != nil,
        "unknown algorithm selection must be rejected");
    NSString *path = AbcFile(@"all-algorithms.bin");
    Start(controller, @[path]);
    Finish(controller);
    AssertTerminalRows(controller, 1);
    NSString *text = controller.resultTextView.string.lowercaseString;
    for (NSString *id in all)
        Require([text containsString:[NSString stringWithFormat:@": %@\n", abcDigests[id]]],
            "GUI algorithm digest differs from independent reference vector");
    [controller setUppercaseDigests:YES];
    Require([controller.resultTextView.string containsString:[abcDigests[@"openssl-sha-256"] uppercaseString]],
        "uppercase setting did not rerender retained digest values");
    Require([controller.resultTextView.string containsString:path], "uppercase setting incorrectly changed path case");
    [controller setUppercaseDigests:NO];
    Require([controller.resultTextView.string containsString:abcDigests[@"openssl-sha-256"]],
        "lowercase setting did not rerender retained digest values");
    Select(controller, @[@"openssl-sha-256"]);
    [controller close];
    LHAppController *restored = Controller();
    Start(restored, @[path]);
    Finish(restored);
    Require([restored.resultTextView.string.lowercaseString containsString:abcDigests[@"openssl-sha-256"]],
        "persisted algorithm selection lost SHA-256");
    Require(![restored.resultTextView.string.lowercaseString containsString:abcDigests[@"md5"]],
        "restored settings unexpectedly enabled MD5");
    [restored close];
}

void CopyAndExport()
{
    LHAppController *controller = Controller();
    Start(controller, @[AbcFile(@"导出 😀.bin")]);
    Finish(controller);
    NSString *text = [controller.resultTextView.string copy];
    [controller copyAll];
    Require([[[NSPasteboard generalPasteboard] stringForType:NSPasteboardTypeString] isEqualToString:text],
        "Copy All clipboard text differs from visible results");
    NSError *error = nil;
    NSURL *url = [NSURL fileURLWithPath:[fixtureRoot stringByAppendingPathComponent:@"导出.txt"]];
    Require([controller exportToURL:url error:&error] && error == nil, "UTF-8 result export failed");
    NSData *data = [NSData dataWithContentsOfURL:url];
    const unsigned char bom[] = {0xef, 0xbb, 0xbf};
    Require(data.length >= 3 && std::memcmp(data.bytes, bom, 3) == 0, "export lacks Windows-compatible UTF-8 BOM");
    NSData *body = [data subdataWithRange:NSMakeRange(3, data.length - 3)];
    Require([[[NSString alloc] initWithData:body encoding:NSUTF8StringEncoding] isEqualToString:text],
        "exported result bytes differ from visible text");
    error = nil;
    Require(![controller exportToURL:[NSURL fileURLWithPath:fixtureRoot isDirectory:YES] error:&error] && error != nil,
        "export failure was silently reported as success");
    Require([[NSData dataWithContentsOfURL:url] isEqualToData:data], "failed export changed previous successful export");
    [controller close];
}

void OrderedAppendAndSelection()
{
    LHAppController *controller = Controller();
    Select(controller, @[@"openssl-sha-256"]);
    NSString *first = AbcFile(@"first-record.bin");
    Start(controller, @[first]);
    Finish(controller);
    NSString *prefix = [controller.resultTextView.string copy];
    Require(prefix.length > 20, "first record did not render");
    const NSRange selection = NSMakeRange(3, 11);
    [controller.resultTextView setSelectedRange:selection];
    NSMutableArray<NSString *> *paths = [NSMutableArray array];
    for (NSUInteger index = 0; index < 128; ++index)
        [paths addObject:AbcFile([NSString stringWithFormat:@"ordered-%03lu-摘要.bin", (unsigned long)index])];
    Start(controller, paths);
    // A selected range exists before new result batches are consumed; restoring
    // that range must never make the next append overwrite/insert in old text.
    Finish(controller);
    NSString *text = controller.resultTextView.string;
    Require([text hasPrefix:prefix], "incremental append inserted into the old selected record");
    Require(NSEqualRanges(controller.resultTextView.selectedRange, selection),
        "incremental result refresh moved the user's selected range");
    NSUInteger cursor = prefix.length;
    for (NSString *path in paths)
    {
        NSRange found = [text rangeOfString:path options:0 range:NSMakeRange(cursor, text.length - cursor)];
        Require(found.location != NSNotFound, "result records are missing or out of input order");
        cursor = NSMaxRange(found);
    }
    AssertTerminalRows(controller, 129);
    [controller close];
}

void FolderAndFailures()
{
    LHAppController *controller = Controller();
    NSString *directory = [fixtureRoot stringByAppendingPathComponent:@"folder-tree"];
    NSString *nested = [directory stringByAppendingPathComponent:@"child"];
    NSError *error = nil;
    Require([[NSFileManager defaultManager] createDirectoryAtPath:nested withIntermediateDirectories:YES attributes:nil error:&error],
        "could not create folder fixture");
    NSString *first = AbcFile(@"folder-tree/one.bin");
    NSString *second = AbcFile(@"folder-tree/child/two.bin");
    Require(symlink(directory.fileSystemRepresentation, [[nested stringByAppendingPathComponent:@"cycle"] fileSystemRepresentation]) == 0,
        "could not create symbolic-link fixture");
    Start(controller, @[directory]);
    Finish(controller);
    AssertTerminalRows(controller, 2);
    Require([controller.resultTextView.string containsString:first] && [controller.resultTextView.string containsString:second],
        "folder scan did not recurse through regular files");
    [controller clearResults];
    NSString *missing = [fixtureRoot stringByAppendingPathComponent:@"missing.bin"];
    Start(controller, @[missing, first]);
    Finish(controller);
    AssertTerminalRows(controller, 2);
    Require([controller.resultTextView.string containsString:missing] &&
        [controller.resultTextView.string.lowercaseString containsString:abcDigests[@"openssl-sha-256"]],
        "failed input suppressed a later valid result");
    [controller close];
}

void DropAndDigestLink()
{
    LHAppController *controller = Controller();
    LHTestDrag *drag = [[LHTestDrag alloc] init];
    drag.draggingPasteboard = [NSPasteboard pasteboardWithUniqueName];
    NSString *path = AbcFile(@"Finder drop 摘要.bin");
    Require([drag.draggingPasteboard writeObjects:@[[NSURL fileURLWithPath:path]]],
        "could not write a native file-URL dragging pasteboard");
    NSView *target = controller.resultTextView;
    Require([target draggingEntered:(id<NSDraggingInfo>)drag] == NSDragOperationCopy,
        "native result view refused file-URL drag");
    Require([target performDragOperation:(id<NSDraggingInfo>)drag], "native file drop failed");
    Finish(controller);
    AssertTerminalRows(controller, 1);
    NSString *digest = abcDigests[@"openssl-sha-256"];
    NSRange location = [controller.resultTextView.string rangeOfString:digest options:NSCaseInsensitiveSearch];
    Require(location.location != NSNotFound, "dropped file did not produce digest");
    id link = [controller.resultTextView.textStorage attribute:NSLinkAttributeName atIndex:location.location effectiveRange:NULL];
    Require([link isKindOfClass:NSString.class], "digest text has no copy-link attribute");
    Require([controller textView:controller.resultTextView clickedOnLink:link atIndex:location.location],
        "digest link was handed to the external URL handler");
    Require([[[NSPasteboard.generalPasteboard stringForType:NSPasteboardTypeString] lowercaseString] isEqualToString:digest],
        "digest link did not copy the full checksum");
    [drag.draggingPasteboard clearContents];
    Require([drag.draggingPasteboard writeObjects:@[[NSURL URLWithString:@"https://example.invalid/file.bin"]]],
        "could not create a non-file URL pasteboard fixture");
    Require([target draggingEntered:(id<NSDraggingInfo>)drag] == NSDragOperationNone,
        "native file drop incorrectly accepted a network URL");
    [drag.draggingPasteboard releaseGlobally];
    [controller close];
}

NSString *LargeFile()
{
    NSString *path = [fixtureRoot stringByAppendingPathComponent:@"cancel-large.bin"];
    int fd = open(path.fileSystemRepresentation, O_WRONLY | O_CREAT | O_TRUNC, 0600);
    Require(fd >= 0, "could not create cancellation fixture");
    int result = ftruncate(fd, static_cast<off_t>(8ULL * 1024 * 1024 * 1024));
    close(fd);
    Require(result == 0, "could not size sparse cancellation fixture");
    return path;
}

void CancelAndClose()
{
    LHAppController *controller = Controller();
    NSString *large = LargeFile();
    NSString *later = AbcFile(@"after-cancel.bin");
    Start(controller, @[large, later]);
    Require(controller.isBusy, "started session did not enter busy state");
    const auto discoveryDeadline = std::chrono::steady_clock::now() + std::chrono::seconds(5);
    while (controller.isBusy && controller.taskTableView.numberOfRows < 2 &&
        std::chrono::steady_clock::now() < discoveryDeadline)
    {
        [controller pollSession];
        Pump();
    }
    Require(controller.isBusy && controller.taskTableView.numberOfRows == 2,
        "cancellation fixture did not reach the discovered active-file stage");
    NSError *error = nil;
    Require(![controller startPaths:@[later] error:&error], "busy controller accepted overlapping session");
    [controller cancel];
    Finish(controller, 10);
    AssertTerminalRows(controller, 2);
    Start(controller, @[later]);
    Finish(controller);
    AssertTerminalRows(controller, 3);
    Require([controller.resultTextView.string.lowercaseString containsString:abcDigests[@"openssl-sha-256"]],
        "new session failed after cancellation");
    [controller close];

    LHAppController *closing = Controller();
    Start(closing, @[large, later]);
    const auto before = std::chrono::steady_clock::now();
    Require([closing windowShouldClose:closing.window], "window close did not accept cancellation");
    [closing close];
    closing = nil;
    Require(std::chrono::duration<double>(std::chrono::steady_clock::now() - before).count() < 1,
        "window close synchronously waited for file I/O");
    Pump(0.5); // Deliver any queued callbacks after controller/window release.
}
}

int main(int argc, char **argv)
{
    @autoreleasepool
    {
        @try
        {
            try
            {
                Require(argc == 2, "usage: lhash_macos_ui_tests algorithm_vectors.json");
                [NSApplication sharedApplication];
                [NSApp setActivationPolicy:NSApplicationActivationPolicyRegular];
                [NSApp finishLaunching];
                NSString *suite = [@"org.lhash.ui-tests." stringByAppendingString:NSUUID.UUID.UUIDString];
                testDefaults = [[NSUserDefaults alloc] initWithSuiteName:suite];
                [testDefaults setObject:@[@"en"] forKey:@"AppleLanguages"];
                fixtureRoot = [NSTemporaryDirectory() stringByAppendingPathComponent:suite];
                NSError *error = nil;
                Require([[NSFileManager defaultManager] createDirectoryAtPath:fixtureRoot withIntermediateDirectories:YES attributes:nil error:&error],
                    "could not create test directory");
                NSData *vectorData = [NSData dataWithContentsOfFile:[NSString stringWithUTF8String:argv[1]]];
                NSDictionary *vectors = [NSJSONSerialization JSONObjectWithData:vectorData options:0 error:&error];
                Require(vectors != nil && error == nil, "could not read independent algorithm vectors");
                for (NSDictionary *vector in vectors[@"vectors"])
                    if ([vector[@"name"] isEqualToString:@"abc"]) abcDigests = vector[@"digests"];
                Require(abcDigests.count == 18, "abc vector is incomplete");
                Test("native layout, Unicode, Windows default digests", LayoutAndDefaults);
                Test("duplicate task identity, exact verification, retained history, clear", DuplicatesAndVerification);
                Test("all 18 algorithms, settings validation/persistence, uppercase", AllAlgorithmsAndSettings);
                Test("real pasteboard, UTF-8 BOM export and output failure", CopyAndExport);
                Test("128 ordered appends preserve the real NSTextView selection", OrderedAppendAndSelection);
                Test("recursive folder selection, symlink cycle, per-file failure continuation", FolderAndFailures);
                Test("native file-URL drop handler and individual digest copy link", DropAndDigestLink);
                Test("cancel, subsequent session, responsive close and worker ownership", CancelAndClose);
                [testDefaults removePersistentDomainForName:suite];
                [[NSFileManager defaultManager] removeItemAtPath:fixtureRoot error:NULL];
                std::printf("PASS macOS GUI integration groups=%u\n", cases);
                return 0;
            }
            catch (const std::exception& error)
            {
                std::fprintf(stderr, "FAIL macOS GUI: %s\n", error.what());
                return 1;
            }
        }
        @catch (NSException *error)
        {
            std::fprintf(stderr, "FAIL macOS GUI Objective-C exception: %s: %s\n",
                error.name.UTF8String, error.reason.UTF8String);
            return 1;
        }
    }
}
