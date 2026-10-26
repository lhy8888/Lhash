#include "HashSession.h"

#include <algorithm>
#include <chrono>
#include <filesystem>
#include <fstream>
#include <functional>
#include <iostream>
#include <map>
#include <stdexcept>
#include <string>
#include <sys/stat.h>
#include <thread>
#include <unistd.h>

namespace fs = std::filesystem;
using namespace lhash::mac;
using Clock = std::chrono::steady_clock;

namespace
{
void Require(bool value, const std::string& message)
{
    if (!value) throw std::runtime_error(message);
}

struct TemporaryDirectory
{
    fs::path root;
    TemporaryDirectory()
    {
        std::string name = (fs::temp_directory_path() / "lhash-macos-session-XXXXXX").string();
        Require(mkdtemp(name.data()) != nullptr, "Cannot create fixture directory");
        root = name;
    }
    ~TemporaryDirectory() { std::error_code ignored; fs::remove_all(root, ignored); }
    fs::path Write(const std::string& name, const std::string& bytes = "abc")
    {
        const auto path = root / fs::u8path(name);
        fs::create_directories(path.parent_path());
        std::ofstream file(path, std::ios::binary);
        file << bytes;
        file.close();
        Require(!file.fail(), "Cannot write fixture");
        return path;
    }
};

struct Capture
{
    Update final;
    std::map<uint32_t, Task> tasks;
    std::vector<Result> results;
    std::vector<std::string> warnings;
    void Add(Update update)
    {
        for (auto& task : update.tasks) tasks[task.index] = std::move(task);
        for (auto& result : update.results) results.push_back(std::move(result));
        for (auto& warning : update.warnings) warnings.push_back(std::move(warning));
        final = std::move(update);
    }
};

SessionOptions Options(const std::vector<fs::path>& files, bool all = false)
{
    SessionOptions options;
    for (const auto& path : files) options.paths.push_back(path.u8string());
    for (const auto& algorithm : Algorithms())
        if (all || algorithm.enabledByDefault) options.algorithmIds.push_back(algorithm.id);
    return options;
}

Capture Finish(HashSession& session, std::function<void(const Update&)> observe = {})
{
    Capture capture;
    const auto deadline = Clock::now() + std::chrono::seconds(40);
    for (;;)
    {
        auto update = session.Poll();
        if (observe) observe(update);
        const bool busy = update.busy;
        capture.Add(std::move(update));
        if (!busy) break;
        Require(Clock::now() < deadline, "Session did not finish within 40 seconds");
        std::this_thread::sleep_for(std::chrono::milliseconds(1));
    }
    Require(!session.Busy(), "Busy remained true after final update");
    Require(capture.final.completed + capture.final.failed + capture.final.cancelled +
        capture.final.aborted + capture.final.notRun == capture.final.taskCount, "Unfinished task after worker exit");
    for (const auto& entry : capture.tasks)
        Require(entry.second.state != TaskState::Running && entry.second.state != TaskState::Pending, "Stale pending/running row");
    return capture;
}

void Start(HashSession& session, const SessionOptions& options)
{
    std::string error;
    Require(session.Start(options, error), "Cannot start session: " + error);
}

// Independent abc vectors shared in provenance with tests/cli/algorithm_vectors.json.
const std::map<std::string, std::string> expected = {
    {"md5", "900150983cd24fb0d6963f7d28e17f72"},
    {"sha1", "a9993e364706816aba3e25717850c26c9cd0d89d"},
    {"openssl-sha-256", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"},
    {"openssl-sha-384", "cb00753f45a35e8bb5a03d699ac65007272c32ab0eded1631a8b605a43ff5bed8086072ba1e7cc2358baeca134c825a7"},
    {"openssl-sha-512", "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f"},
    {"openssl-sha3-256", "3a985da74fe225b2045c172d6bd390bd855f086e3e9d525b46bfe24511431532"},
    {"openssl-sha3-384", "ec01498288516fc926459f58e2c6ad8df9b473cb0fc08c2596da7cf0e49be4b298d88cea927ac7f539f1edf228376d25"},
    {"openssl-sha3-512", "b751850b1a57168a5693cd924b6b096e08f621827444f70d884f5d0240d2712e10e116e9192af3c91a7ec57647e3934057340b4cf408d5a56592f8274eec53f0"},
    {"openssl-blake2b-512", "ba80a53f981c4d0d6a2797b69f12f6e94c212f14685ac4b74b12bb6fdbffa2d17d87c5392aab792dc252d5de4533cc9518d38aa8dbf1925ab92386edd4009923"},
    {"openssl-blake2s-256", "508c5e8c327c14e2e1a72ba34eeb452f37458b209ed63a294d999b4c86675982"},
    {"openssl-shake128-256", "5881092dd818bf5cf8a3ddb793fbcba74097d5c526a6d35f97b83351940f2cc8"},
    {"openssl-shake256-512", "483366601360a8771c6863080cc4114d8db44530f8f1e1ee4f94ea37e78b5739d5a15bef186a5386c75744c0527e1faa9f8726e462a12a4feb06bd8801e751e4"},
    {"blake3-256", "6437b3ac38465133ffb63b75273a8db548c558465d79db03fd359c6cd5bd9d85"},
    {"blake3-512", "6437b3ac38465133ffb63b75273a8db548c558465d79db03fd359c6cd5bd9d851fb250ae7393f5d02813b65d521a0d492d9ba09cf7ce7f4cffd900f23374bf0b"},
    {"blake3-xof", "6437b3ac38465133ffb63b75273a8db548c558465d79db03fd359c6cd5bd9d851fb250ae7393f5d02813b65d521a0d492d9ba09cf7ce7f4cffd900f23374bf0bc08a1fb0b38ed276181ccbd9f7b7edbddf9f86404ad7929605f6ffa3fb1ac87983105f013384f2f11d38879c985d47003804b905f0c38975e28d36804bb60d8c"},
    {"xxh3-64", "78af5f94892f3950"},
    {"xxh3-128", "06b05ab6733a618578af5f94892f3950"},
    {"crc32c", "364b3fb7"}
};

void CheckAlgorithms()
{
    const auto algorithms = Algorithms();
    Require(algorithms.size() == expected.size(), "GUI algorithm registry differs from Windows");
    size_t defaults = 0;
    for (const auto& algorithm : algorithms)
    {
        Require(expected.count(algorithm.id) == 1, "Unexpected algorithm: " + algorithm.id);
        if (algorithm.enabledByDefault)
        {
            Require(algorithm.id == "openssl-sha-256" || algorithm.id == "openssl-sha-512" || algorithm.id == "openssl-sha3-256", "Unexpected default");
            ++defaults;
        }
    }
    Require(defaults == 3, "Defaults differ from Windows");
}

void CheckDigests(const Result& result, bool uppercase)
{
    Require(result.error.empty() && result.size == 3 && result.digests.size() == expected.size(), "Incomplete result");
    for (const auto& digest : result.digests)
    {
        auto value = expected.at(digest.id);
        if (uppercase) for (char& c : value) if (c >= 'a' && c <= 'f') c = static_cast<char>(c - 'a' + 'A');
        Require(digest.value == value, "Incorrect digest/case: " + digest.id);
    }
}

void CheckDuplicatesAndSessions()
{
    TemporaryDirectory fixture;
    const auto file = fixture.Write("摘要 😀 file.bin");
    HashSession session;
    auto options = Options({file, file}, true);
    Start(session, options);
    auto first = Finish(session);
    Require(first.final.completed == 2 && first.results.size() == 2 && first.tasks.size() == 2, "Duplicate path lost a task/result");
    Require(first.results[0].index == 0 && first.results[1].index == 1, "Duplicate identity/order lost");
    Require(first.results[0].path == file.u8string(), "Unicode path changed");
    for (const auto& result : first.results) CheckDigests(result, false);
    Require(first.final.bytesProcessed == 6 && first.final.elapsedSeconds > 0, "Incorrect timing/bytes");
    auto drained = session.Poll();
    Require(drained.tasks.empty() && drained.results.empty(), "Poll repeated consumed updates");
    Require(drained.elapsedSeconds == first.final.elapsedSeconds, "Completed elapsed time keeps increasing");
    session.Cancel();
    Require(!session.Poll().cancelRequested, "Late cancel changed completed session");
    options.paths.resize(1);
    options.parallel = true;
    options.uppercase = true;
    Start(session, options);
    auto second = Finish(session);
    Require(second.final.sessionId > first.final.sessionId && second.final.completed == 1, "Session identity/counts leaked");
    Require(second.results.size() == 1 && second.results[0].index == 0, "Second session index was not reset");
    CheckDigests(second.results[0], true);
}

void CheckFailureContinuation()
{
    TemporaryDirectory fixture;
    const auto file = fixture.Write("abc");
    const auto link = fixture.root / "link";
    fs::create_symlink(file, link);
    const auto pipe = fixture.root / "pipe";
    Require(mkfifo(pipe.c_str(), 0600) == 0, "Cannot create FIFO");
    HashSession session;
    Start(session, Options({fixture.root / "missing", file, link, pipe, file}));
    const auto capture = Finish(session);
    Require(capture.final.failed == 3 && capture.final.completed == 2 && capture.results.size() == 5, "Failure continuation did not preserve every input");
    for (size_t i = 0; i < capture.results.size(); ++i)
    {
        const auto& result = capture.results[i];
        Require(result.index == i, "Results not in input order");
        const bool failed = i != 1 && i != 4;
        Require(failed ? (!result.error.empty() && result.digests.empty()) : result.error.empty(), "Failure result/digests inconsistent");
    }
}

void CheckFolderDiscovery()
{
    TemporaryDirectory fixture;
    const auto file = fixture.Write("folder/a");
    fixture.Write("folder/sub/摘要.bin");
    fixture.Write("outside/secret.bin");
    fs::create_directory_symlink(fixture.root / "outside", fixture.root / "folder" / "external");
    fs::create_directory_symlink(fixture.root / "folder", fixture.root / "folder" / "loop");
    Require(mkfifo((fixture.root / "folder" / "pipe").c_str(), 0600) == 0, "Cannot create FIFO");
    HashSession session;
    Start(session, Options({fixture.root / "folder", file}));
    const auto capture = Finish(session);
    Require(capture.final.completed == 3 && capture.final.failed == 0, "Folder traversal followed links/special entries or dropped explicit duplicate");
    Require(!capture.warnings.empty(), "Skipped entries not disclosed");
    for (const auto& result : capture.results) Require(result.path.find("secret") == std::string::npos, "Traversed symbolic directory link");
    fs::create_directory(fixture.root / "empty");
    Start(session, Options({fixture.root / "empty"}));
    const auto empty = Finish(session);
    Require(empty.final.taskCount == 0 && !empty.warnings.empty(), "Empty-folder outcome is not visible");
}

void CheckDepthLimit()
{
    TemporaryDirectory fixture;
    auto directory = fixture.root;
    for (size_t depth = 0; depth != 130; ++depth)
    {
        directory /= "d";
        fs::create_directory(directory);
    }
    std::ofstream(directory / "beyond") << "abc";
    HashSession session;
    Start(session, Options({fixture.root}));
    const auto capture = Finish(session);
    Require(capture.final.taskCount == 0 && !capture.warnings.empty(), "Depth limit ignored or silent");
}

void CheckFileLimitAndCoalescing()
{
    TemporaryDirectory fixture;
    for (size_t i = 0; i != 8193; ++i) fixture.Write(std::to_string(i), "");
    HashSession session;
    auto options = Options({fixture.root});
    options.algorithmIds = {"md5"};
    Start(session, options);
    // Deliberately defer all polling: terminal results must survive while the
    // task/progress queue remains bounded to one state per original input.
    const auto deadline = Clock::now() + std::chrono::seconds(40);
    while (session.Busy())
    {
        Require(Clock::now() < deadline, "Large batch timed out");
        std::this_thread::sleep_for(std::chrono::milliseconds(2));
    }
    const auto capture = Finish(session);
    Require(capture.tasks.size() == 8192 && capture.results.size() == 8192 && capture.final.completed == 8192, "File budget lost completions or was exceeded");
    Require(capture.final.tasks.size() == 8192 && !capture.warnings.empty(), "Queue was not coalesced or truncation was silent");
}

void CheckCancellation()
{
    TemporaryDirectory fixture;
    const auto large = fixture.Write("large");
    fs::resize_file(large, uint64_t{1} << 30);
    const auto next = fixture.Write("next");
    HashSession session;
    Start(session, Options({large, next}, true));
    bool requested = false;
    double lastElapsed = 0;
    const auto capture = Finish(session, [&](const Update& update)
    {
        Require(update.elapsedSeconds >= lastElapsed, "Monotonic timing decreased");
        lastElapsed = update.elapsedSeconds;
        for (const auto& task : update.tasks)
            if (task.state == TaskState::Running && task.progress > 0 && !requested)
            {
                std::string error;
                Require(!session.Start(Options({next}), error) && !error.empty(), "Overlapping session was accepted");
                session.Cancel();
                requested = true;
            }
    });
    Require(requested, "Fixture completed before cancellation was exercised");
    Require(capture.final.cancelled == 1 && capture.final.notRun == 1 && capture.final.cancelRequested, "Cancel did not terminate running and pending rows distinctly");
    Require(capture.results.empty() && capture.final.error.empty(), "Cancel published a partial digest or abnormal error");
    Start(session, Options({next}));
    Require(Finish(session).final.completed == 1, "Cancellation leaked into subsequent session");
}

void CheckInvalidInputs()
{
    HashSession session;
    std::string error;
    auto options = Options({fs::path("missing")});
    options.algorithmIds.clear();
    Require(!session.Start(options, error) && !error.empty(), "Empty algorithm selection accepted");
    options.algorithmIds = {"unavailable"};
    Require(!session.Start(options, error), "Unavailable algorithm accepted");
    options.algorithmIds = {"md5"};
    options.paths = {std::string("bad\0path", 8)};
    Require(!session.Start(options, error), "Embedded NUL accepted");
    options.paths = {std::string("bad\xff", 4)};
    Require(!session.Start(options, error), "Invalid UTF-8 accepted");
    options.paths.assign(8193, "missing");
    Require(!session.Start(options, error) && error.find("8192") != std::string::npos, "Oversized explicit batch accepted");
    Require(!session.Busy() && session.Poll().sessionId == 0, "Invalid inputs created a worker/session");
}
}

int main()
{
    try
    {
        CheckAlgorithms();
        CheckDuplicatesAndSessions();
        CheckFailureContinuation();
        CheckFolderDiscovery();
        CheckDepthLimit();
        CheckFileLimitAndCoalescing();
        CheckCancellation();
        CheckInvalidInputs();
        std::cout << "PASS macOS session: 18 independent abc digests, Unicode, case/policy, duplicate identity, sessions, errors/FIFO, folder links/limits, bounded drain, cancellation, monotonic timing, input validation\n";
        return 0;
    }
    catch (const std::exception& error)
    {
        std::cerr << "FAIL macOS session: " << error.what() << '\n';
        return 1;
    }
}
