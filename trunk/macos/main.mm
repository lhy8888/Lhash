#import <Cocoa/Cocoa.h>
#import "AppController.h"

@interface LHApplicationDelegate : NSObject <NSApplicationDelegate>
@property(nonatomic, strong) LHAppController *controller;
@end

@implementation LHApplicationDelegate
- (void)applicationDidFinishLaunching:(NSNotification *)notification {
    self.controller = [[LHAppController alloc] initWithDefaults:NSUserDefaults.standardUserDefaults];
    BOOL chinese = [NSLocale.preferredLanguages.firstObject hasPrefix:@"zh"];
    NSMenu *bar = [[NSMenu alloc] init];
    NSMenuItem *application = [[NSMenuItem alloc] init];
    [bar addItem:application];
    NSMenu *appMenu = [[NSMenu alloc] initWithTitle:@"LHash"];
    application.submenu = appMenu;
    [appMenu addItemWithTitle:chinese ? @"关于 LHash" : @"About LHash" action:@selector(orderFrontStandardAboutPanel:) keyEquivalent:@""];
    [appMenu addItem:NSMenuItem.separatorItem];
    [appMenu addItemWithTitle:chinese ? @"隐藏 LHash" : @"Hide LHash" action:@selector(hide:) keyEquivalent:@"h"];
    [appMenu addItemWithTitle:chinese ? @"退出 LHash" : @"Quit LHash" action:@selector(terminate:) keyEquivalent:@"q"];
    NSMenuItem *fileItem = [[NSMenuItem alloc] init]; [bar addItem:fileItem];
    NSMenu *file = [[NSMenu alloc] initWithTitle:chinese ? @"文件" : @"File"]; fileItem.submenu = file;
    NSMenuItem *open = [file addItemWithTitle:chinese ? @"打开…" : @"Open…" action:NSSelectorFromString(@"openFiles:") keyEquivalent:@"o"]; open.target = self.controller;
    NSMenuItem *folder = [file addItemWithTitle:chinese ? @"打开文件夹…" : @"Open Folder…" action:NSSelectorFromString(@"openFolder:") keyEquivalent:@"o"]; folder.target = self.controller;
    folder.keyEquivalentModifierMask = NSEventModifierFlagCommand | NSEventModifierFlagShift;
    NSMenuItem *save = [file addItemWithTitle:chinese ? @"导出…" : @"Export…" action:NSSelectorFromString(@"saveResults:") keyEquivalent:@"s"]; save.target = self.controller;
    [file addItemWithTitle:chinese ? @"关闭" : @"Close" action:@selector(performClose:) keyEquivalent:@"w"];
    NSMenuItem *editItem = [[NSMenuItem alloc] init]; [bar addItem:editItem];
    NSMenu *edit = [[NSMenu alloc] initWithTitle:chinese ? @"编辑" : @"Edit"]; editItem.submenu = edit;
    [edit addItemWithTitle:chinese ? @"剪切" : @"Cut" action:@selector(cut:) keyEquivalent:@"x"];
    [edit addItemWithTitle:chinese ? @"复制" : @"Copy" action:@selector(copy:) keyEquivalent:@"c"];
    [edit addItemWithTitle:chinese ? @"粘贴" : @"Paste" action:@selector(paste:) keyEquivalent:@"v"];
    [edit addItemWithTitle:chinese ? @"全选" : @"Select All" action:@selector(selectAll:) keyEquivalent:@"a"];
    NSMenuItem *verify = [edit addItemWithTitle:chinese ? @"验证…" : @"Verify…" action:NSSelectorFromString(@"showVerify:") keyEquivalent:@"f"]; verify.target = self.controller;
    NSApp.mainMenu = bar;
    [self.controller showWindow:nil];
    [NSApp activateIgnoringOtherApps:YES];
}
- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)sender { return YES; }
- (NSApplicationTerminateReply)applicationShouldTerminate:(NSApplication *)sender {
    if (self.controller) [self.controller windowShouldClose:self.controller.window];
    return NSTerminateNow;
}
- (void)application:(NSApplication *)sender openFiles:(NSArray<NSString *> *)filenames {
    NSError *error = nil;
    BOOL success = [self.controller startPaths:filenames error:&error];
    if (error) [[NSAlert alertWithError:error] beginSheetModalForWindow:self.controller.window completionHandler:nil];
    [sender replyToOpenOrPrint:success ? NSApplicationDelegateReplySuccess : NSApplicationDelegateReplyFailure];
}
@end

int main(int argc, const char **argv) {
    @autoreleasepool {
        [NSApplication sharedApplication];
        [NSApp setActivationPolicy:NSApplicationActivationPolicyRegular];
        __attribute__((objc_precise_lifetime)) LHApplicationDelegate *delegate = [[LHApplicationDelegate alloc] init];
        NSApp.delegate = delegate;
        [NSApp run];
    }
    return 0;
}
