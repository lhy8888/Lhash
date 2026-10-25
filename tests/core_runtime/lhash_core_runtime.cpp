#include "stdafx.h"

#include "Common/HashEngine.h"
#include "Domain/HashRequest.h"
#include "Runtime/HashExecutionContext.h"

#include <algorithm>
#include <filesystem>
#include <fstream>
#include <functional>
#include <future>
#include <iostream>
#include <mutex>
#include <random>
#include <stdexcept>
#include <string>
#include <vector>

namespace fs = std::filesystem;

namespace
{
    void Require(bool condition, const std::string& message)
    {
        if (!condition)
            throw std::runtime_error(message);
    }

    sunjwbase::tstring NativePath(const fs::path& path)
    {
        return sunjwbase::strtotstrutf8(path.u8string());
    }

    class TempDirectory
    {
    public:
        TempDirectory()
        {
            std::random_device random;
            for (unsigned attempt = 0; attempt != 100; ++attempt)
            {
                const auto candidate = fs::temp_directory_path() /
                    ("lhash-core-runtime-" + std::to_string(random()) + "-" + std::to_string(random()));
                if (fs::create_directory(candidate))
                {
                    root_ = candidate;
                    return;
                }
            }
            throw std::runtime_error("Cannot create an isolated temporary directory");
        }

        ~TempDirectory()
        {
            std::error_code ignored;
            fs::remove_all(root_, ignored);
        }

        fs::path Path(const std::string& name) const { return root_ / fs::u8path(name); }

        fs::path Write(const std::string& name, const std::string& content) const
        {
            const auto path = Path(name);
            std::ofstream output(path, std::ios::binary | std::ios::trunc);
            Require(output.is_open(), "Cannot create fixture " + name);
            output.write(content.data(), static_cast<std::streamsize>(content.size()));
            output.close();
            Require(!output.fail(), "Cannot write all fixture bytes for " + name);
            return path;
        }

    private:
        fs::path root_;
    };

    class ProgressCapture : public HashProgressSink
    {
    public:
        int progressMax() override { return 100; }

        void onProgressEvent(const ProgressEvent& event) override
        {
            std::lock_guard<std::mutex> lock(mutex_);
            events_.push_back(event);
            if (cancelOnProgress_ != nullptr && event.type == PROGRESS_EVENT_FILE_PROGRESS && event.value > 0)
                cancelOnProgress_->stopRequested.store(true);
        }

        void CancelOnProgress(HashCancellationState& cancellation) { cancelOnProgress_ = &cancellation; }

        std::vector<ProgressEvent> Events() const
        {
            std::lock_guard<std::mutex> lock(mutex_);
            return events_;
        }

        size_t Count(ProgressEventType type) const
        {
            const auto events = Events();
            return static_cast<size_t>(std::count_if(events.begin(), events.end(),
                [type](const ProgressEvent& event) { return event.type == type; }));
        }

    private:
        mutable std::mutex mutex_;
        std::vector<ProgressEvent> events_;
        HashCancellationState* cancelOnProgress_ = nullptr;
    };

    HashRequest Request(const std::vector<fs::path>& paths,
        const std::vector<std::string>& algorithms = { "md5", "sha1" },
        HashRequestDigestExecutionPolicy policy = HASH_REQUEST_DIGEST_EXECUTION_POLICY_AUTO)
    {
        HashRequest request;
        for (const auto& path : paths)
            request.files.push_back(NativePath(path));
        for (const auto& algorithm : algorithms)
            AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr(algorithm));
        request.digestExecutionPolicy = policy;
        return request;
    }

    HashJobState Execute(const HashRequest& request, ProgressCapture& sink, HashCancellationState& cancellation)
    {
        HashJobState state;
        HashExecutionContext context(&sink, state, cancellation);
        Require(RunHashRequest(&context, request) == 0, "Unexpected engine return code");
        Require(!state.working.load(), "Engine left the working flag set after returning");
        return state;
    }

    void ExpectDigest(const HashResult& result, const char* algorithm, const char* expected)
    {
        const auto id = sunjwbase::strtotstr(std::string(algorithm));
        const auto found = std::find_if(result.digests.begin(), result.digests.end(),
            [&id](const HashDigestResult& digest) { return digest.algorithmId == id; });
        Require(found != result.digests.end(), std::string("Missing digest: ") + algorithm);
        const auto actual = sunjwbase::tstrtostr(found->value);
        Require(actual == expected, std::string(algorithm) + " mismatch: expected " + expected + ", actual " + actual);
    }

    void ExpectCompleted(const HashJobState& state, const ProgressCapture& sink, size_t fileCount)
    {
        Require(state.results.size() == fileCount, "Unexpected file-result count");
        Require(sink.Count(PROGRESS_EVENT_JOB_COMPLETED) == 1, "Expected exactly one completion event");
        Require(sink.Count(PROGRESS_EVENT_JOB_CANCELLED) == 0, "Successful request emitted cancellation");
        Require(sink.Count(PROGRESS_EVENT_FILE_FINISHED) == fileCount, "Unexpected file-finished count");
    }

    void ExpectSuccessfulFile(const HashResult& result, const fs::path& path, size_t bytes, size_t digestCount)
    {
        Require(result.state == RESULT_ALL, "File did not complete successfully: " + sunjwbase::tstrtostrutf8(result.error));
        Require(result.path == NativePath(path), "Result path differs from input path");
        Require(result.meta.size == bytes, "Incorrect file size in result metadata");
        Require(result.error.empty(), "Successful result contains an error");
        Require(result.digests.size() == digestCount, "Unexpected selected-digest count");
    }

    std::string Pattern(size_t size)
    {
        std::string bytes(size, '\0');
        for (size_t index = 0; index != size; ++index)
            bytes[index] = static_cast<char>(index & 255);
        return bytes;
    }

    struct BoundaryVector { size_t bytes; const char* md5; const char* sha1; };

    // Independent oracle: Python hashlib over bytes(index & 255 for index in range(size)).
    // The 4 MiB and 8 MiB boundaries exercise the engine's current read-buffer size.
    const BoundaryVector kBoundaries[] = {
        { 0, "D41D8CD98F00B204E9800998ECF8427E", "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709" },
        { 1, "93B885ADFE0DA089CDF634904FD59F71", "5BA93C9DB0CFF93F52B521D7420E43F6EDA2784F" },
        { 63, "48A6295221902E8E0938F773A7185E72", "6D942DA0C4392B123528F2905C713A3CE28364BD" },
        { 64, "B2D3F56BC197FD985D5965079B5E7148", "C6138D514FFA2135BFCE0ED0B8FAC65669917EC7" },
        { 65, "8BD7053801C768420FAF816FADBA971C", "69BD728AD6E13CD76FF19751FDE427B00E395746" },
        { 4095, "4F059904FB7CB387D3903DCFD9DD2A7C", "4B892EF6A50B100EB11F74E3F2DAAEFD22417FCA" },
        { 4096, "2BCD3C4DE20C918E19FAB5C36249C70D", "E9DDED8C84614E894501965AF60C2525794A8C7D" },
        { 4097, "70410AAD262CD11E63AE854804C8024B", "C627E736EFD8BB0DFF1778335C9C79CB1F27E396" },
        { 4194303, "1AA96EA6F006D6AF685953833FA8AA88", "48680F80204633EB6A775A539AFC9F6AEB627DF2" },
        { 4194304, "631B2C76267E568CCB221193AB23E134", "D697024ED93FF625330D050391ADE99CD5CBDDAD" },
        { 4194321, "404969C1FED41EC92079E17D41E61741", "F089A31397CE3ED99A63FF61BD869DA37A473B77" },
        { 8388625, "CF00E27D6858B77F1A86D421E453EA11", "FD7BFA7C82BE27C5593FC32241C293B9DA050EE9" }
    };

    void CheckBoundary(const TempDirectory& temporary, const BoundaryVector& vector,
        HashRequestDigestExecutionPolicy policy)
    {
        const auto path = temporary.Write("boundary.bin", Pattern(vector.bytes));
        ProgressCapture sink;
        HashCancellationState cancellation;
        const auto state = Execute(Request({ path }, { "md5", "sha1" }, policy), sink, cancellation);
        ExpectCompleted(state, sink, 1);
        ExpectSuccessfulFile(state.results.front(), path, vector.bytes, 2);
        ExpectDigest(state.results.front(), "md5", vector.md5);
        ExpectDigest(state.results.front(), "sha1", vector.sha1);
        Require(state.countedSize.load() == vector.bytes, "Incorrect total byte count");
        Require(sink.Count(PROGRESS_EVENT_FILE_HASH_READY) == 1, "Digest result was not published exactly once");
    }

    void KnownAnswers(const TempDirectory& temporary)
    {
        const auto empty = temporary.Write("empty.bin", "");
        const auto abc = temporary.Write("abc.txt", "abc");
        const auto crc = temporary.Write("crc.txt", "123456789");
        ProgressCapture sink;
        HashCancellationState cancellation;
        auto request = Request({ empty, abc, crc }, { "md5", "sha1", "crc32c", "blake3-256" });
        request.uppercaseDigest = true;
        const auto state = Execute(request, sink, cancellation);
        ExpectCompleted(state, sink, 3);
        auto result = state.results.begin();
        ExpectSuccessfulFile(*result, empty, 0, 4);
        ExpectDigest(*result, "md5", "D41D8CD98F00B204E9800998ECF8427E");
        ExpectDigest(*result, "sha1", "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709");
        ExpectDigest(*result, "crc32c", "00000000");
        ExpectDigest(*result, "blake3-256", "AF1349B9F5F9A1A6A0404DEA36DCC9499BCB25C9ADC112B7CC9A93CAE41F3262");
        ++result;
        ExpectSuccessfulFile(*result, abc, 3, 4);
        ExpectDigest(*result, "md5", "900150983CD24FB0D6963F7D28E17F72");
        ExpectDigest(*result, "sha1", "A9993E364706816ABA3E25717850C26C9CD0D89D");
        ExpectDigest(*result, "crc32c", "364B3FB7");
        ExpectDigest(*result, "blake3-256", "6437B3AC38465133FFB63B75273A8DB548C558465D79DB03FD359C6CD5BD9D85");
        ++result;
        ExpectSuccessfulFile(*result, crc, 9, 4);
        ExpectDigest(*result, "crc32c", "E3069283");
        for (const auto& event : sink.Events())
            if (event.type == PROGRESS_EVENT_FILE_HASH_READY)
                Require(event.uppercaseDigest, "Digest event lost the uppercase preference");
    }

    void UnicodeAndDuplicatePaths(const TempDirectory& temporary)
    {
        const auto path = temporary.Write(u8"校验 文件 résumé 🔎.txt", "abc");
        ProgressCapture sink;
        HashCancellationState cancellation;
        const auto state = Execute(Request({ path, path }), sink, cancellation);
        ExpectCompleted(state, sink, 2);
        for (const auto& result : state.results)
        {
            ExpectSuccessfulFile(result, path, 3, 2);
            ExpectDigest(result, "md5", "900150983CD24FB0D6963F7D28E17F72");
            ExpectDigest(result, "sha1", "A9993E364706816ABA3E25717850C26C9CD0D89D");
        }
        Require(state.countedSize.load() == 6, "Duplicate file inputs must each contribute their size");
    }

    void ErrorThenSuccess(const TempDirectory& temporary)
    {
        const auto missing = temporary.Path("does-not-exist.bin");
        const auto directory = temporary.Path("directory");
        Require(fs::create_directory(directory), "Cannot create directory fixture");
        const auto regular = temporary.Write("after-errors.txt", "abc");
        ProgressCapture sink;
        HashCancellationState cancellation;
        const auto state = Execute(Request({ missing, directory, regular }), sink, cancellation);
        ExpectCompleted(state, sink, 3);
        auto result = state.results.begin();
        for (const auto& path : { missing, directory })
        {
            Require(result->path == NativePath(path), "Error results lost input order");
            Require(result->state == RESULT_ERROR, "Invalid file must report an error");
            Require(!result->error.empty(), "File error must have an explanation");
            Require(result->digests.empty(), "Failed file published a digest");
            ++result;
        }
        ExpectSuccessfulFile(*result, regular, 3, 2);
        ExpectDigest(*result, "md5", "900150983CD24FB0D6963F7D28E17F72");
        Require(sink.Count(PROGRESS_EVENT_FILE_FAILED) == 2, "Expected two file-failed events");
        Require(sink.Count(PROGRESS_EVENT_FILE_HASH_READY) == 1, "Only the regular file may publish a digest");
    }

    void PreCancelled(const TempDirectory& temporary)
    {
        ProgressCapture sink;
        HashCancellationState cancellation;
        cancellation.stopRequested.store(true);
        const auto state = Execute(Request({ temporary.Path("must-not-open.bin") }), sink, cancellation);
        Require(state.results.empty(), "Pre-cancelled request attempted a file");
        Require(sink.Count(PROGRESS_EVENT_JOB_CANCELLED) == 1, "Pre-cancelled request must emit cancellation");
        Require(sink.Count(PROGRESS_EVENT_JOB_COMPLETED) == 0, "Pre-cancelled request emitted completion");
    }

    void CancelDuringRead(const TempDirectory& temporary, HashRequestDigestExecutionPolicy policy)
    {
        // More than one 4 MiB engine buffer makes this a mid-file cancellation.
        const auto first = temporary.Write("cancel-large.bin", Pattern(16 * 1024 * 1024 + 17));
        const auto second = temporary.Write("cancel-next.txt", "abc");
        ProgressCapture sink;
        HashCancellationState cancellation;
        sink.CancelOnProgress(cancellation);
        const auto state = Execute(Request({ first, second }, { "md5", "sha1" }, policy), sink, cancellation);
        Require(cancellation.stopRequested.load(), "Progress callback did not request cancellation");
        Require(sink.Count(PROGRESS_EVENT_JOB_CANCELLED) == 1, "Read cancellation event missing");
        Require(sink.Count(PROGRESS_EVENT_JOB_COMPLETED) == 0, "Cancelled job emitted completion");
        Require(sink.Count(PROGRESS_EVENT_FILE_STARTED) == 1, "Cancellation did not skip the next file");
        Require(sink.Count(PROGRESS_EVENT_FILE_HASH_READY) == 0, "Cancelled file published a partial digest");
        Require(state.results.size() == 1, "Cancellation appended later file results");
        Require(state.results.front().state != RESULT_ALL, "Cancelled file was marked complete");
        Require(state.results.front().digests.empty(), "Cancelled result retained partial digests");
    }

    void ConcurrentRequests(const TempDirectory& temporary)
    {
        const auto file = temporary.Write("concurrent.txt", "abc");
        std::vector<std::future<void>> workers;
        for (unsigned worker = 0; worker != 4; ++worker)
        {
            workers.push_back(std::async(std::launch::async, [file, worker]
            {
                for (unsigned iteration = 0; iteration != 8; ++iteration)
                {
                    ProgressCapture sink;
                    HashCancellationState cancellation;
                    const auto policy = worker % 2 == 0 ? HASH_REQUEST_DIGEST_EXECUTION_POLICY_SINGLE_THREADED :
                        HASH_REQUEST_DIGEST_EXECUTION_POLICY_PARALLEL;
                    const auto state = Execute(Request({ file }, { "md5", "sha1", "blake3-256" }, policy), sink, cancellation);
                    ExpectCompleted(state, sink, 1);
                    ExpectSuccessfulFile(state.results.front(), file, 3, 3);
                    ExpectDigest(state.results.front(), "md5", "900150983CD24FB0D6963F7D28E17F72");
                    ExpectDigest(state.results.front(), "sha1", "A9993E364706816ABA3E25717850C26C9CD0D89D");
                    ExpectDigest(state.results.front(), "blake3-256", "6437B3AC38465133FFB63B75273A8DB548C558465D79DB03FD359C6CD5BD9D85");
                }
            }));
        }
        for (auto& worker : workers)
            worker.get();
    }
}

int main()
{
    unsigned passed = 0;
    unsigned failed = 0;
    const auto run = [&](const std::string& name, const std::function<void()>& test)
    {
        try
        {
            test();
            ++passed;
            std::cout << "[pass] " << name << '\n';
        }
        catch (const std::exception& error)
        {
            ++failed;
            std::cerr << "[fail] " << name << ": " << error.what() << '\n';
        }
    };

    try
    {
        const TempDirectory temporary;
        run("standard digest vectors and published preference", [&] { KnownAnswers(temporary); });
        const HashRequestDigestExecutionPolicy policies[] = {
            HASH_REQUEST_DIGEST_EXECUTION_POLICY_AUTO,
            HASH_REQUEST_DIGEST_EXECUTION_POLICY_SINGLE_THREADED,
            HASH_REQUEST_DIGEST_EXECUTION_POLICY_PARALLEL
        };
        for (auto policy : policies)
            for (const auto& vector : kBoundaries)
                run("binary bytes=" + std::to_string(vector.bytes) + " policy=" + std::to_string(policy),
                    [&] { CheckBoundary(temporary, vector, policy); });
        run("Unicode, spaces and duplicate file paths", [&] { UnicodeAndDuplicatePaths(temporary); });
        run("missing file and directory errors preserve later success", [&] { ErrorThenSuccess(temporary); });
        run("cancellation before starting", [&] { PreCancelled(temporary); });
        run("cancellation while reading single-pass", [&] {
            CancelDuringRead(temporary, HASH_REQUEST_DIGEST_EXECUTION_POLICY_SINGLE_THREADED);
        });
        run("cancellation while reading parallel", [&] {
            CancelDuringRead(temporary, HASH_REQUEST_DIGEST_EXECUTION_POLICY_PARALLEL);
        });
        run("32 independent requests on four concurrent callers", [&] { ConcurrentRequests(temporary); });
    }
    catch (const std::exception& error)
    {
        std::cerr << "[fail] fixture setup: " << error.what() << '\n';
        ++failed;
    }

    std::cout << "core runtime: " << passed << " passed, " << failed << " failed\n";
    return failed == 0 ? 0 : 1;
}
