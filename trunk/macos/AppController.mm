#import "AppController.h"
#include "HashSession.h"
#include <algorithm>
#include <array>
#include <memory>
#include <utility>
#include <vector>

using namespace lhash::mac;

namespace {
NSString *String(const std::string& value) {
    return [[NSString alloc] initWithBytes:value.data() length:value.size() encoding:NSUTF8StringEncoding] ?: @"�";
}
NSString *Visible(NSString *value) {
    // A filename must not be able to manufacture another result line.
    NSMutableString *out = [NSMutableString string];
    for (NSUInteger i = 0; i < value.length; ++i) {
        unichar c = [value characterAtIndex:i];
        if (c == '\n') [out appendString:@"\\n"];
        else if (c == '\r') [out appendString:@"\\r"];
        else if (c == '\t') [out appendString:@"\\t"];
        else if (c < 32 || c == 127 || c == 0x85 || c == 0x2028 || c == 0x2029 || c == 0x200e || c == 0x200f ||
            (c >= 0x202a && c <= 0x202e) || (c >= 0x2066 && c <= 0x2069))
            [out appendFormat:@"\\u%04x", c];
        else [out appendFormat:@"%C", c];
    }
    return out;
}
BOOL Fail(NSError **error, NSString *message) {
    if (error) *error = [NSError errorWithDomain:@"org.lhash.desktop" code:1
        userInfo:@{NSLocalizedDescriptionKey:message}];
    return NO;
}
NSArray<NSURL *> *DroppedURLs(id<NSDraggingInfo> sender) {
    return [sender.draggingPasteboard readObjectsForClasses:@[NSURL.class]
        options:@{NSPasteboardURLReadingFileURLsOnlyKey:@YES}];
}
}

@interface LHDropView : NSView
@property(nonatomic, weak) LHAppController *controller;
@end
@interface LHResultView : NSTextView
@property(nonatomic, weak) LHAppController *controller;
@end
@interface LHTaskTable : NSTableView
@property(nonatomic, weak) LHAppController *controller;
@end

@interface LHAppController ()
- (void)showError:(NSError *)error;
- (BOOL)acceptDrop:(id<NSDraggingInfo>)sender;
- (void)openFiles:(id)sender;
- (void)openFolder:(id)sender;
- (void)showVerify:(id)sender;
- (void)copyResults:(id)sender;
- (void)saveResults:(id)sender;
- (void)showSettings:(id)sender;
- (void)clearAction:(id)sender;
@end

@implementation LHDropView
- (NSDragOperation)draggingEntered:(id<NSDraggingInfo>)sender {
    return !self.controller.isBusy && DroppedURLs(sender).count ? NSDragOperationCopy : NSDragOperationNone;
}
- (BOOL)performDragOperation:(id<NSDraggingInfo>)sender { return [self.controller acceptDrop:sender]; }
@end
@implementation LHResultView
- (NSDragOperation)draggingEntered:(id<NSDraggingInfo>)sender {
    return !self.controller.isBusy && DroppedURLs(sender).count ? NSDragOperationCopy : NSDragOperationNone;
}
- (BOOL)performDragOperation:(id<NSDraggingInfo>)sender { return [self.controller acceptDrop:sender]; }
@end
@implementation LHTaskTable
- (NSDragOperation)draggingEntered:(id<NSDraggingInfo>)sender {
    return !self.controller.isBusy && DroppedURLs(sender).count ? NSDragOperationCopy : NSDragOperationNone;
}
- (BOOL)performDragOperation:(id<NSDraggingInfo>)sender { return [self.controller acceptDrop:sender]; }
@end

@implementation LHAppController {
    std::unique_ptr<HashSession> _session;
    std::vector<Result> _results;
    std::array<size_t, 7> _stateCounts{};
    NSUInteger _filterMatches;
    NSUserDefaults *_defaults;
    NSMutableArray<NSString *> *_algorithmIDs;
    NSMutableArray<NSMutableDictionary *> *_rows;
    NSMutableDictionary<NSString *, NSNumber *> *_rowIndex;
    NSMutableArray<NSString *> *_notices;
    NSTextView *_resultTextView;
    NSTableView *_taskTableView;
    NSButton *_openButton, *_folderButton, *_settingsButton, *_clearButton;
    NSTextField *_summary, *_timing;
    NSProgressIndicator *_progress;
    NSTimer *_timer;
    NSString *_activeAlgorithms, *_filter;
    BOOL _chinese, _uppercase, _closed;
}

- (NSString *)text:(NSString *)english chinese:(NSString *)chinese { return _chinese ? chinese : english; }

- (instancetype)initWithDefaults:(NSUserDefaults *)defaults {
    NSWindow *window = [[NSWindow alloc] initWithContentRect:NSMakeRect(0, 0, 1080, 760)
        styleMask:NSWindowStyleMaskTitled | NSWindowStyleMaskClosable | NSWindowStyleMaskMiniaturizable | NSWindowStyleMaskResizable
        backing:NSBackingStoreBuffered defer:NO];
    self = [super initWithWindow:window];
    if (!self) return nil;
    _session = std::make_unique<HashSession>();
    _defaults = defaults;
    _chinese = [NSLocale.preferredLanguages.firstObject hasPrefix:@"zh"];
    _uppercase = [defaults boolForKey:@"uppercaseDigests"];
    _algorithmIDs = [NSMutableArray array];
    id saved = [defaults objectForKey:@"algorithmIDs"];
    for (const auto& algorithm : Algorithms()) {
        NSString *key = String(algorithm.id);
        if ([saved isKindOfClass:NSArray.class] ? [saved containsObject:key] : algorithm.enabledByDefault)
            [_algorithmIDs addObject:key];
    }
    if (!_algorithmIDs.count)
        for (const auto& algorithm : Algorithms()) if (algorithm.enabledByDefault) [_algorithmIDs addObject:String(algorithm.id)];
    _rows = [NSMutableArray array];
    _rowIndex = [NSMutableDictionary dictionary];
    _notices = [NSMutableArray array];
    window.title = @"LHash";
    window.minSize = NSMakeSize(800, 600);
    window.releasedWhenClosed = NO;
    window.delegate = self;
    [window setFrameAutosaveName:@"LHashMainWindow"];
    LHDropView *content = [[LHDropView alloc] initWithFrame:NSMakeRect(0, 0, 1080, 760)];
    content.controller = self;
    [content registerForDraggedTypes:@[NSPasteboardTypeFileURL]];
    window.contentView = content;

    NSArray *titles = @[[self text:@"Open…" chinese:@"打开…"], [self text:@"Open Folder…" chinese:@"打开文件夹…"],
        [self text:@"Verify" chinese:@"验证"], [self text:@"Copy All" chinese:@"全部复制"],
        [self text:@"Export…" chinese:@"导出…"], [self text:@"Settings" chinese:@"设置"]];
    SEL actions[] = {@selector(openFiles:), @selector(openFolder:), @selector(showVerify:),
        @selector(copyResults:), @selector(saveResults:), @selector(showSettings:)};
    NSStackView *toolbar = [[NSStackView alloc] initWithFrame:NSMakeRect(16, 704, 1048, 40)];
    toolbar.orientation = NSUserInterfaceLayoutOrientationHorizontal;
    toolbar.distribution = NSStackViewDistributionFillEqually;
    toolbar.spacing = 12;
    toolbar.autoresizingMask = NSViewWidthSizable | NSViewMinYMargin;
    for (NSUInteger i = 0; i < titles.count; ++i) {
        NSButton *button = [NSButton buttonWithTitle:titles[i] target:self action:actions[i]];
        button.bezelStyle = NSBezelStyleRounded;
        button.controlSize = NSControlSizeLarge;
        button.accessibilityIdentifier = @[@"open", @"folder", @"verify", @"copy", @"export", @"settings"][i];
        [toolbar addArrangedSubview:button];
        if (i == 0) _openButton = button;
        if (i == 1) _folderButton = button;
        if (i == 5) _settingsButton = button;
    }
    [content addSubview:toolbar];

    NSScrollView *resultScroll = [[NSScrollView alloc] initWithFrame:NSMakeRect(16, 238, 1048, 452)];
    resultScroll.autoresizingMask = NSViewWidthSizable | NSViewHeightSizable;
    resultScroll.hasVerticalScroller = YES;
    resultScroll.hasHorizontalScroller = YES;
    resultScroll.borderType = NSBezelBorder;
    LHResultView *text = [[LHResultView alloc] initWithFrame:resultScroll.contentView.bounds];
    text.controller = self;
    text.editable = NO;
    text.selectable = YES;
    text.richText = NO;
    text.usesFindPanel = YES;
    text.font = [NSFont monospacedSystemFontOfSize:13 weight:NSFontWeightRegular];
    text.textColor = NSColor.textColor;
    text.backgroundColor = NSColor.textBackgroundColor;
    text.textContainerInset = NSMakeSize(8, 8);
    text.verticallyResizable = YES;
    text.horizontallyResizable = YES;
    text.minSize = NSMakeSize(0, resultScroll.contentSize.height);
    text.maxSize = NSMakeSize(CGFLOAT_MAX, CGFLOAT_MAX);
    text.textContainer.containerSize = NSMakeSize(CGFLOAT_MAX, CGFLOAT_MAX);
    text.textContainer.widthTracksTextView = NO;
    text.delegate = self;
    text.accessibilityIdentifier = @"results";
    [text registerForDraggedTypes:@[NSPasteboardTypeFileURL]];
    resultScroll.documentView = text;
    _resultTextView = text;
    [content addSubview:resultScroll];

    NSScrollView *taskScroll = [[NSScrollView alloc] initWithFrame:NSMakeRect(16, 78, 1048, 148)];
    taskScroll.autoresizingMask = NSViewWidthSizable;
    taskScroll.hasVerticalScroller = YES;
    taskScroll.hasHorizontalScroller = YES;
    taskScroll.borderType = NSBezelBorder;
    LHTaskTable *table = [[LHTaskTable alloc] initWithFrame:taskScroll.bounds];
    table.controller = self;
    table.rowHeight = 25;
    table.usesAlternatingRowBackgroundColors = YES;
    table.columnAutoresizingStyle = NSTableViewLastColumnOnlyAutoresizingStyle;
    table.dataSource = self;
    table.delegate = self;
    table.accessibilityIdentifier = @"tasks";
    NSArray *columnIDs = @[@"path", @"algorithms", @"status", @"progress"];
    NSArray *columnTitles = @[[self text:@"File" chinese:@"文件"], [self text:@"Algorithms" chinese:@"算法"],
        [self text:@"Status" chinese:@"状态"], [self text:@"Progress" chinese:@"进度"]];
    const CGFloat widths[] = {400, 310, 170, 120};
    for (NSUInteger i = 0; i < columnIDs.count; ++i) {
        NSTableColumn *column = [[NSTableColumn alloc] initWithIdentifier:columnIDs[i]];
        column.title = columnTitles[i]; column.width = widths[i]; column.minWidth = 75;
        [table addTableColumn:column];
    }
    [table registerForDraggedTypes:@[NSPasteboardTypeFileURL]];
    taskScroll.documentView = table;
    _taskTableView = table;
    [content addSubview:taskScroll];

    _summary = [NSTextField labelWithString:[self text:@"Open or drop files to calculate checksums." chinese:@"打开或拖入文件以计算校验和。"]];
    _summary.frame = NSMakeRect(16, 48, 640, 20);
    _summary.autoresizingMask = NSViewWidthSizable;
    _summary.lineBreakMode = NSLineBreakByTruncatingTail;
    _summary.accessibilityIdentifier = @"summary";
    [content addSubview:_summary];
    _timing = [NSTextField labelWithString:@""];
    _timing.frame = NSMakeRect(670, 48, 394, 20);
    _timing.autoresizingMask = NSViewMinXMargin;
    _timing.alignment = NSTextAlignmentRight;
    [content addSubview:_timing];
    _progress = [[NSProgressIndicator alloc] initWithFrame:NSMakeRect(16, 22, 904, 16)];
    _progress.autoresizingMask = NSViewWidthSizable;
    _progress.indeterminate = NO;
    _progress.minValue = 0; _progress.maxValue = 100;
    [content addSubview:_progress];
    _clearButton = [NSButton buttonWithTitle:[self text:@"Clear" chinese:@"清空"] target:self action:@selector(clearAction:)];
    _clearButton.frame = NSMakeRect(936, 12, 128, 32);
    _clearButton.autoresizingMask = NSViewMinXMargin;
    [content addSubview:_clearButton];
    __weak LHAppController *weakSelf = self;
    _timer = [NSTimer timerWithTimeInterval:0.1 repeats:YES block:^(NSTimer *timer) { [weakSelf pollSession]; }];
    [NSRunLoop.mainRunLoop addTimer:_timer forMode:NSRunLoopCommonModes];
    [window center];
    return self;
}

- (void)dealloc { [_timer invalidate]; }
- (NSTextView *)resultTextView { return _resultTextView; }
- (NSTableView *)taskTableView { return _taskTableView; }
- (BOOL)isBusy { return _session && _session->Busy(); }
- (void)showError:(NSError *)error {
    if (!error || _closed) return;
    NSAlert *alert = [NSAlert alertWithError:error];
    [alert beginSheetModalForWindow:self.window completionHandler:nil];
}

- (BOOL)selectAlgorithmIDs:(NSArray<NSString *> *)ids error:(NSError **)error {
    if (self.isBusy) return Fail(error, [self text:@"Stop the current session before changing algorithms." chinese:@"请先停止当前任务，再更改算法。"]);
    if (!ids.count) return Fail(error, [self text:@"Select at least one algorithm." chinese:@"请至少选择一种算法。"]);
    NSMutableArray *selected = [NSMutableArray array];
    for (NSString *key in ids) {
        bool found = false;
        for (const auto& algorithm : Algorithms()) if ([key isEqualToString:String(algorithm.id)]) { found = true; break; }
        if (!found) return Fail(error, [self text:@"Unknown algorithm." chinese:@"未知算法。"]);
        if (![selected containsObject:key]) [selected addObject:key];
    }
    _algorithmIDs = selected;
    [_defaults setObject:selected forKey:@"algorithmIDs"];
    return YES;
}

- (BOOL)startPaths:(NSArray<NSString *> *)paths error:(NSError **)error {
    if (_closed) return Fail(error, @"The window is closed.");
    [self pollSession]; // Drain the previous session before it can be replaced.
    SessionOptions options;
    for (NSString *path in paths) {
        NSData *utf8 = [path dataUsingEncoding:NSUTF8StringEncoding allowLossyConversion:NO];
        if (!utf8 || !utf8.length || [path rangeOfString:[NSString stringWithFormat:@"%C", (unichar)0]].location != NSNotFound)
            return Fail(error, [self text:@"Invalid file path." chinese:@"无效的文件路径。"]);
        options.paths.emplace_back(static_cast<const char *>(utf8.bytes), utf8.length);
    }
    for (NSString *key in _algorithmIDs) options.algorithmIds.emplace_back(key.UTF8String);
    options.uppercase = _uppercase;
    std::string message;
    if (!_session->Start(options, message)) return Fail(error, String(message));
    NSMutableArray *labels = [NSMutableArray array];
    for (const auto& algorithm : Algorithms()) if ([_algorithmIDs containsObject:String(algorithm.id)]) [labels addObject:String(algorithm.label)];
    _activeAlgorithms = [labels componentsJoinedByString:@", "];
    [self clearVerification];
    [self pollSession];
    return YES;
}
- (void)cancel { if (_session) _session->Cancel(); [self pollSession]; }

- (NSString *)statusForTask:(const Task&)task {
    switch (task.state) {
        case TaskState::Pending: return [self text:@"Pending" chinese:@"等待中"];
        case TaskState::Running: return [self text:@"Running" chinese:@"运行中"];
        case TaskState::Completed: return [self text:@"Completed" chinese:@"已完成"];
        case TaskState::Failed: return [self text:@"Failed" chinese:@"失败"];
        case TaskState::Cancelled: return [self text:@"Cancelled" chinese:@"已取消"];
        case TaskState::NotRun: return [self text:@"Not run" chinese:@"未执行"];
        case TaskState::Aborted: return [self text:@"Aborted" chinese:@"异常中止"];
    }
    return @"";
}
- (BOOL)resultMatches:(const Result&)result {
    if (!_filter.length) return YES;
    for (const auto& digest : result.digests)
        if ([String(digest.value) caseInsensitiveCompare:_filter] == NSOrderedSame) return YES;
    return NO;
}
- (NSAttributedString *)renderResult:(const Result&)result {
    NSMutableAttributedString *text = [[NSMutableAttributedString alloc] init];
    NSDictionary *normal = @{NSFontAttributeName:_resultTextView.font, NSForegroundColorAttributeName:NSColor.textColor};
    NSString *heading = [NSString stringWithFormat:@"%@: %@\n%@: %llu %@ (%@)\n%@: %@\n",
        [self text:@"File" chinese:@"文件名"], Visible(String(result.path)),
        [self text:@"Size" chinese:@"文件大小"], (unsigned long long)result.size,
        [self text:@"bytes" chinese:@"字节"], [NSByteCountFormatter stringFromByteCount:(long long)std::min<uint64_t>(result.size, INT64_MAX) countStyle:NSByteCountFormatterCountStyleFile],
        [self text:@"Modified" chinese:@"修改日期"], Visible(String(result.modifiedDate))];
    [text appendAttributedString:[[NSAttributedString alloc] initWithString:heading attributes:normal]];
    if (!result.error.empty()) {
        [text appendAttributedString:[[NSAttributedString alloc] initWithString:
            [NSString stringWithFormat:@"%@: %@\n", [self text:@"Error" chinese:@"错误"], Visible(String(result.error))]
            attributes:@{NSFontAttributeName:_resultTextView.font, NSForegroundColorAttributeName:NSColor.systemRedColor}]];
    }
    for (const auto& digest : result.digests) {
        [text appendAttributedString:[[NSAttributedString alloc] initWithString:[String(digest.label) stringByAppendingString:@": "] attributes:normal]];
        NSString *value = _uppercase ? String(digest.value).uppercaseString : String(digest.value).lowercaseString;
        NSMutableDictionary *link = [normal mutableCopy];
        link[NSLinkAttributeName] = value;
        link[NSUnderlineStyleAttributeName] = @(NSUnderlineStyleSingle);
        [text appendAttributedString:[[NSAttributedString alloc] initWithString:value attributes:link]];
        [text appendAttributedString:[[NSAttributedString alloc] initWithString:@"\n" attributes:normal]];
    }
    [text appendAttributedString:[[NSAttributedString alloc] initWithString:@"\n" attributes:normal]];
    return text;
}
- (void)appendText:(NSAttributedString *)text {
    if (!text.length) return;
    const NSRange selection = _resultTextView.selectedRange;
    NSClipView *clip = _resultTextView.enclosingScrollView.contentView;
    const NSPoint origin = clip.bounds.origin;
    const BOOL followTail = selection.length == 0 && NSMaxY(clip.bounds) >= NSHeight(_resultTextView.bounds) - 4;
    [_resultTextView.textStorage beginEditing];
    [_resultTextView.textStorage appendAttributedString:text];
    [_resultTextView.textStorage endEditing];
    [_resultTextView setSelectedRange:selection];
    if (followTail) [_resultTextView scrollRangeToVisible:NSMakeRange(_resultTextView.string.length, 0)];
    else { [clip scrollToPoint:origin]; [_resultTextView.enclosingScrollView reflectScrolledClipView:clip]; }
}
- (void)rebuildText {
    NSMutableAttributedString *text = [[NSMutableAttributedString alloc] init];
    for (const auto& result : _results) if ([self resultMatches:result]) [text appendAttributedString:[self renderResult:result]];
    for (NSString *notice in _notices)
        [text appendAttributedString:[[NSAttributedString alloc] initWithString:notice attributes:
            @{NSFontAttributeName:_resultTextView.font, NSForegroundColorAttributeName:NSColor.secondaryLabelColor}]];
    const NSRange old = _resultTextView.selectedRange;
    [_resultTextView.textStorage setAttributedString:text];
    const NSUInteger start = std::min(old.location, text.length);
    [_resultTextView setSelectedRange:NSMakeRange(start, std::min(old.length, text.length - start))];
}
- (void)pollSession {
    if (!_session || _closed) return;
    auto update = _session->Poll();
    NSMutableIndexSet *changed = [NSMutableIndexSet indexSet];
    BOOL added = NO;
    for (const auto& task : update.tasks) {
        NSString *key = [NSString stringWithFormat:@"%llu:%u", (unsigned long long)task.sessionId, task.index];
        NSNumber *index = _rowIndex[key];
        NSMutableDictionary *row;
        if (!index) {
            index = @(_rows.count); _rowIndex[key] = index;
            row = [NSMutableDictionary dictionary]; [_rows addObject:row]; added = YES;
            row[@"state"] = @(static_cast<size_t>(TaskState::Pending));
            ++_stateCounts[static_cast<size_t>(TaskState::Pending)];
        } else row = _rows[index.unsignedIntegerValue];
        --_stateCounts[[row[@"state"] unsignedIntegerValue]];
        row[@"state"] = @(static_cast<size_t>(task.state));
        ++_stateCounts[static_cast<size_t>(task.state)];
        row[@"path"] = Visible(String(task.path).lastPathComponent);
        row[@"fullPath"] = Visible(String(task.path));
        row[@"algorithms"] = _activeAlgorithms ?: @"";
        row[@"status"] = [self statusForTask:task];
        row[@"error"] = Visible(String(task.error));
        row[@"progress"] = [NSString stringWithFormat:@"%d%%", task.progress];
        [changed addIndex:index.unsignedIntegerValue];
    }
    if (added) [_taskTableView noteNumberOfRowsChanged];
    if (changed.count) [_taskTableView reloadDataForRowIndexes:changed columnIndexes:[NSIndexSet indexSetWithIndexesInRange:NSMakeRange(0, 4)]];
    NSMutableAttributedString *pending = [[NSMutableAttributedString alloc] init];
    for (auto& result : update.results) {
        if ([self resultMatches:result]) {
            [pending appendAttributedString:[self renderResult:result]];
            if (_filter.length) ++_filterMatches;
        }
        _results.push_back(std::move(result));
    }
    if (!update.error.empty()) update.warnings.push_back(update.error);
    for (const auto& warning : update.warnings) {
        NSString *notice = [NSString stringWithFormat:@"%@: %@\n\n", [self text:@"Notice" chinese:@"提示"], Visible(String(warning))];
        [_notices addObject:notice];
        [pending appendAttributedString:[[NSAttributedString alloc] initWithString:notice attributes:
            @{NSFontAttributeName:_resultTextView.font, NSForegroundColorAttributeName:NSColor.secondaryLabelColor}]];
    }
    [self appendText:pending];
    _openButton.title = update.busy ? (update.cancelRequested ? [self text:@"Stopping…" chinese:@"正在停止…"] : [self text:@"Stop" chinese:@"停止"])
        : [self text:@"Open…" chinese:@"打开…"];
    _openButton.enabled = !update.cancelRequested || !update.busy;
    _folderButton.enabled = !update.busy;
    _settingsButton.enabled = !update.busy;
    _clearButton.enabled = !update.busy;
    _progress.indeterminate = update.scanning;
    if (update.scanning) [_progress startAnimation:nil]; else { [_progress stopAnimation:nil]; _progress.doubleValue = update.totalProgress; }
    if (update.sessionId) {
        NSString *phase = update.scanning ? [self text:@"Scanning folder…" chinese:@"正在扫描文件夹…"] :
            (update.busy ? [self text:@"Calculating" chinese:@"正在计算"] : [self text:@"Finished" chinese:@"已结束"]);
        if (!_filter.length)
            _summary.stringValue = [NSString stringWithFormat:@"%@ · %@ %zu/%zu · %@ %zu · %@ %zu", phase,
                [self text:@"Completed" chinese:@"完成"], _stateCounts[static_cast<size_t>(TaskState::Completed)], (size_t)_rows.count,
                [self text:@"Failed" chinese:@"失败"], _stateCounts[static_cast<size_t>(TaskState::Failed)],
                [self text:@"Unfinished" chinese:@"取消/未执行"], _stateCounts[static_cast<size_t>(TaskState::Cancelled)] +
                    _stateCounts[static_cast<size_t>(TaskState::NotRun)] + _stateCounts[static_cast<size_t>(TaskState::Aborted)]);
        else
            _summary.stringValue = [NSString stringWithFormat:[self text:@"Matching files: %lu" chinese:@"匹配文件数：%lu"], (unsigned long)_filterMatches];
        double speed = update.elapsedSeconds > 0 ? update.bytesProcessed / update.elapsedSeconds / (1024.0 * 1024.0) : 0;
        _timing.stringValue = [NSString stringWithFormat:@"%.2f s · %.2f MiB/s", update.elapsedSeconds, speed];
    }
}

- (NSInteger)numberOfRowsInTableView:(NSTableView *)tableView { return static_cast<NSInteger>(_rows.count); }
- (NSView *)tableView:(NSTableView *)tableView viewForTableColumn:(NSTableColumn *)column row:(NSInteger)rowIndex {
    NSTableCellView *cell = [tableView makeViewWithIdentifier:column.identifier owner:self];
    if (!cell) {
        cell = [[NSTableCellView alloc] initWithFrame:NSMakeRect(0, 0, column.width, 25)];
        cell.identifier = column.identifier;
        NSTextField *label = [NSTextField labelWithString:@""];
        label.frame = NSMakeRect(5, 3, column.width - 10, 20);
        label.autoresizingMask = NSViewWidthSizable;
        label.lineBreakMode = NSLineBreakByTruncatingMiddle;
        cell.textField = label; [cell addSubview:label];
    }
    NSDictionary *row = _rows[static_cast<NSUInteger>(rowIndex)];
    cell.textField.stringValue = row[column.identifier] ?: @"";
    cell.toolTip = [column.identifier isEqualToString:@"status"] && [row[@"error"] length] ? row[@"error"] : row[@"fullPath"];
    return cell;
}
- (BOOL)textView:(NSTextView *)textView clickedOnLink:(id)link atIndex:(NSUInteger)charIndex {
    if (![link isKindOfClass:NSString.class]) return YES;
    [NSPasteboard.generalPasteboard clearContents];
    if (![NSPasteboard.generalPasteboard setString:link forType:NSPasteboardTypeString])
        [self clipboardFailed];
    return YES; // Digests are copied locally, never opened as URLs.
}

- (NSUInteger)verifyDigest:(NSString *)digest error:(NSError **)error {
    NSString *normalized = [digest stringByTrimmingCharactersInSet:NSCharacterSet.whitespaceAndNewlineCharacterSet];
    NSCharacterSet *hex = [NSCharacterSet characterSetWithCharactersInString:@"0123456789abcdefABCDEF"];
    if (!normalized.length || normalized.length % 2 || [normalized rangeOfCharacterFromSet:hex.invertedSet].location != NSNotFound) {
        Fail(error, [self text:@"Enter a complete hexadecimal checksum." chinese:@"请输入完整的十六进制校验和。"]);
        return NSNotFound;
    }
    _filter = normalized;
    NSUInteger matches = 0;
    for (const auto& result : _results) if ([self resultMatches:result]) ++matches;
    _filterMatches = matches;
    [self rebuildText];
    _clearButton.title = [self text:@"Clear Verification" chinese:@"清除验证"];
    _summary.stringValue = [NSString stringWithFormat:[self text:@"Matching files: %lu" chinese:@"匹配文件数：%lu"], (unsigned long)matches];
    return matches;
}
- (void)clearVerification {
    if (_filter.length) { _filter = nil; _filterMatches = 0; [self rebuildText]; }
    _clearButton.title = [self text:@"Clear" chinese:@"清空"];
}
- (void)clearResults {
    if (self.isBusy) return;
    [self pollSession];
    _results.clear(); [_rows removeAllObjects]; [_rowIndex removeAllObjects]; [_notices removeAllObjects];
    _filter = nil;
    _filterMatches = 0;
    _stateCounts.fill(0);
    // Replace an idle session so later timer ticks cannot restore old counters.
    _session = std::make_unique<HashSession>();
    [_resultTextView setString:@""]; [_taskTableView reloadData];
    _progress.doubleValue = 0; _timing.stringValue = @"";
    _summary.stringValue = [self text:@"Open or drop files to calculate checksums." chinese:@"打开或拖入文件以计算校验和。"];
    _clearButton.title = [self text:@"Clear" chinese:@"清空"];
}
- (void)setUppercaseDigests:(BOOL)uppercase {
    _uppercase = uppercase; [_defaults setBool:uppercase forKey:@"uppercaseDigests"]; [self rebuildText];
}
- (void)copyAll {
    [NSPasteboard.generalPasteboard clearContents];
    if (![NSPasteboard.generalPasteboard setString:_resultTextView.string forType:NSPasteboardTypeString])
        [self clipboardFailed];
}
- (void)clipboardFailed {
    NSError *error = nil;
    Fail(&error, [self text:@"Could not copy to the clipboard. Please try again." chinese:@"无法复制到剪贴板，请重试。"]);
    [self showError:error];
}
- (BOOL)exportToURL:(NSURL *)url error:(NSError **)error {
    NSData *body = [_resultTextView.string dataUsingEncoding:NSUTF8StringEncoding allowLossyConversion:NO];
    if (!body || !url.isFileURL) return Fail(error, [self text:@"Cannot encode or save these results." chinese:@"无法编码或保存这些结果。"]);
    const unsigned char bom[] = {0xef, 0xbb, 0xbf};
    NSMutableData *data = [NSMutableData dataWithBytes:bom length:sizeof(bom)];
    [data appendData:body];
    return [data writeToURL:url options:NSDataWritingAtomic error:error];
}

- (void)openFiles:(id)sender {
    if (self.isBusy) { [self cancel]; return; }
    NSOpenPanel *panel = NSOpenPanel.openPanel;
    panel.canChooseFiles = YES; panel.canChooseDirectories = NO; panel.allowsMultipleSelection = YES;
    panel.resolvesAliases = NO;
    [panel beginSheetModalForWindow:self.window completionHandler:^(NSModalResponse response) {
        if (response != NSModalResponseOK) return;
        NSMutableArray *paths = [NSMutableArray array]; for (NSURL *url in panel.URLs) [paths addObject:url.path];
        NSError *error; if (![self startPaths:paths error:&error]) [self showError:error];
    }];
}
- (void)openFolder:(id)sender {
    if (self.isBusy) return;
    NSOpenPanel *panel = NSOpenPanel.openPanel;
    panel.canChooseFiles = NO; panel.canChooseDirectories = YES; panel.allowsMultipleSelection = NO;
    panel.resolvesAliases = NO;
    [panel beginSheetModalForWindow:self.window completionHandler:^(NSModalResponse response) {
        if (response != NSModalResponseOK) return;
        NSError *error; if (![self startPaths:@[panel.URL.path] error:&error]) [self showError:error];
    }];
}
- (BOOL)acceptDrop:(id<NSDraggingInfo>)sender {
    if (self.isBusy) return NO;
    NSMutableArray *paths = [NSMutableArray array]; for (NSURL *url in DroppedURLs(sender)) [paths addObject:url.path];
    NSError *error; BOOL accepted = [self startPaths:paths error:&error];
    if (!accepted) [self showError:error];
    return accepted;
}
- (void)showVerify:(id)sender {
    NSAlert *alert = [[NSAlert alloc] init];
    alert.messageText = [self text:@"Verify checksum" chinese:@"验证校验和"];
    alert.informativeText = [self text:@"Paste the complete expected digest. Matching files will be shown." chinese:@"粘贴完整的预期校验和，将显示匹配的文件。"];
    [alert addButtonWithTitle:[self text:@"Verify" chinese:@"验证"]];
    [alert addButtonWithTitle:[self text:@"Cancel" chinese:@"取消"]];
    NSTextField *field = [[NSTextField alloc] initWithFrame:NSMakeRect(0, 0, 520, 28)];
    field.stringValue = _filter ?: @"";
    alert.accessoryView = field;
    [alert beginSheetModalForWindow:self.window completionHandler:^(NSModalResponse response) {
        if (response != NSAlertFirstButtonReturn) return;
        NSError *error; if ([self verifyDigest:field.stringValue error:&error] == NSNotFound) [self showError:error];
    }];
    [alert.window makeFirstResponder:field];
}
- (void)copyResults:(id)sender { [self copyAll]; }
- (void)saveResults:(id)sender {
    NSSavePanel *panel = NSSavePanel.savePanel; panel.nameFieldStringValue = @"LHash.txt";
    [panel beginSheetModalForWindow:self.window completionHandler:^(NSModalResponse response) {
        if (response != NSModalResponseOK) return;
        NSError *error; if (![self exportToURL:panel.URL error:&error]) [self showError:error];
    }];
}
- (void)clearAction:(id)sender { if (_filter.length) [self clearVerification]; else [self clearResults]; }
- (void)toggleUppercase:(id)sender { [self setUppercaseDigests:!_uppercase]; }
- (void)toggleAlgorithm:(NSMenuItem *)sender {
    NSMutableArray *selected = [_algorithmIDs mutableCopy];
    if ([selected containsObject:sender.representedObject]) [selected removeObject:sender.representedObject]; else [selected addObject:sender.representedObject];
    NSError *error; if (![self selectAlgorithmIDs:selected error:&error]) [self showError:error];
}
- (void)useDefaultAlgorithms:(id)sender {
    NSMutableArray *ids = [NSMutableArray array];
    for (const auto& algorithm : Algorithms()) if (algorithm.enabledByDefault) [ids addObject:String(algorithm.id)];
    [self selectAlgorithmIDs:ids error:nullptr];
}
- (void)useAllAlgorithms:(id)sender {
    NSMutableArray *ids = [NSMutableArray array]; for (const auto& algorithm : Algorithms()) [ids addObject:String(algorithm.id)];
    [self selectAlgorithmIDs:ids error:nullptr];
}
- (void)showSettings:(id)sender {
    if (self.isBusy) return;
    NSMenu *menu = [[NSMenu alloc] initWithTitle:[self text:@"Settings" chinese:@"设置"]];
    NSMenuItem *upper = [menu addItemWithTitle:[self text:@"Uppercase digests" chinese:@"大写校验和"] action:@selector(toggleUppercase:) keyEquivalent:@""];
    upper.target = self; upper.state = _uppercase ? NSControlStateValueOn : NSControlStateValueOff;
    NSMenuItem *algorithmItem = [menu addItemWithTitle:[self text:@"Algorithms" chinese:@"算法"] action:NULL keyEquivalent:@""];
    NSMenu *algorithms = [[NSMenu alloc] initWithTitle:algorithmItem.title]; algorithmItem.submenu = algorithms;
    for (const auto& algorithm : Algorithms()) {
        NSMenuItem *item = [algorithms addItemWithTitle:String(algorithm.label) action:@selector(toggleAlgorithm:) keyEquivalent:@""];
        item.target = self; item.representedObject = String(algorithm.id);
        item.state = [_algorithmIDs containsObject:item.representedObject] ? NSControlStateValueOn : NSControlStateValueOff;
    }
    [algorithms addItem:NSMenuItem.separatorItem];
    NSMenuItem *defaults = [algorithms addItemWithTitle:[self text:@"Restore defaults" chinese:@"恢复默认"] action:@selector(useDefaultAlgorithms:) keyEquivalent:@""]; defaults.target = self;
    NSMenuItem *all = [algorithms addItemWithTitle:[self text:@"Select all" chinese:@"全部选择"] action:@selector(useAllAlgorithms:) keyEquivalent:@""]; all.target = self;
    [menu addItem:NSMenuItem.separatorItem];
    NSMenuItem *clear = [menu addItemWithTitle:[self text:@"Clear results / verification" chinese:@"清空结果 / 清除验证"] action:@selector(clearAction:) keyEquivalent:@""]; clear.target = self;
    NSMenuItem *about = [menu addItemWithTitle:[self text:@"About LHash" chinese:@"关于 LHash"] action:@selector(orderFrontStandardAboutPanel:) keyEquivalent:@""]; about.target = NSApp;
    [menu popUpMenuPositioningItem:nil atLocation:NSMakePoint(0, NSHeight(_settingsButton.bounds)) inView:_settingsButton];
}
- (BOOL)windowShouldClose:(NSWindow *)sender {
    _closed = YES; [_timer invalidate];
    if (_session) _session->Cancel();
    // A stalled filesystem syscall owns only the worker's shared state. Closing
    // never waits on the AppKit thread and the worker cannot message this window.
    _session.reset();
    return YES;
}
- (void)windowWillClose:(NSNotification *)notification {
    // NSWindowController.close also reaches here when called programmatically.
    [self windowShouldClose:self.window];
}
@end
