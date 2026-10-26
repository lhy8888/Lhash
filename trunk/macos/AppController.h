#import <Cocoa/Cocoa.h>

// All methods are main-thread-only. Background workers own no AppKit objects.
@interface LHAppController : NSWindowController <NSWindowDelegate, NSTableViewDataSource,
    NSTableViewDelegate, NSTextViewDelegate>
- (instancetype)initWithDefaults:(NSUserDefaults *)defaults;
@property(nonatomic, readonly) NSTextView *resultTextView;
@property(nonatomic, readonly) NSTableView *taskTableView;
@property(nonatomic, readonly, getter=isBusy) BOOL busy;
- (BOOL)startPaths:(NSArray<NSString *> *)paths error:(NSError **)error;
- (void)cancel;
- (void)pollSession;
- (NSUInteger)verifyDigest:(NSString *)digest error:(NSError **)error;
- (void)clearVerification;
- (void)clearResults;
- (BOOL)selectAlgorithmIDs:(NSArray<NSString *> *)ids error:(NSError **)error;
- (void)setUppercaseDigests:(BOOL)uppercase;
- (void)copyAll;
- (BOOL)exportToURL:(NSURL *)url error:(NSError **)error;
@end
