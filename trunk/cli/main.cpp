#include "stdafx.h"

#include "Common/HashEngine.h"
#include "Domain/HashRequest.h"
#include "Domain/ProgressEvent.h"
#include "Runtime/HashExecutionContext.h"
#include "Runtime/HashProgressSink.h"
#include "Common/strhelper.h"

#include <algorithm>
#include <iostream>
#include <stdexcept>
#include <string>

namespace
{
    bool IsUtf8(const std::string& text)
    {
        for (size_t i = 0; i < text.size();)
        {
            const auto first = static_cast<unsigned char>(text[i++]);
            if (first < 0x80) continue;
            unsigned int code = 0, minimum = 0;
            size_t remaining = 0;
            if (first >= 0xc2 && first <= 0xdf) { code = first & 0x1f; remaining = 1; minimum = 0x80; }
            else if (first >= 0xe0 && first <= 0xef) { code = first & 0x0f; remaining = 2; minimum = 0x800; }
            else if (first >= 0xf0 && first <= 0xf4) { code = first & 7; remaining = 3; minimum = 0x10000; }
            else return false;
            if (text.size() - i < remaining) return false;
            while (remaining--)
            {
                const auto next = static_cast<unsigned char>(text[i++]);
                if ((next & 0xc0) != 0x80) return false;
                code = (code << 6) | (next & 0x3f);
            }
            if (code < minimum || code > 0x10ffff || (code >= 0xd800 && code <= 0xdfff)) return false;
        }
        return true;
    }

    std::string Escape(const std::string& text, bool json)
    {
        static const char hex[] = "0123456789abcdef";
        std::string output;
        for (unsigned char c : text)
        {
            switch (c)
            {
            case '\\': output += "\\\\"; break;
            case '\n': output += "\\n"; break;
            case '\r': output += "\\r"; break;
            case '\t': output += "\\t"; break;
            case '"': output += json ? "\\\"" : "\""; break;
            default:
                if (c < 0x20 || c == 0x7f)
                {
                    output += json ? "\\u00" : "\\x";
                    output += hex[c >> 4]; output += hex[c & 15];
                }
                else output += static_cast<char>(c);
            }
        }
        return output;
    }

    std::string JsonString(const sunjwbase::tstring& value)
    {
        return "\"" + Escape(sunjwbase::tstrtostrutf8(value), true) + "\"";
    }

    std::string AlgorithmKey(std::string value)
    {
        value = sunjwbase::str_lower(value);
        if (value.compare(0, 8, "openssl-") == 0) value.erase(0, 8);
        value.erase(std::remove_if(value.begin(), value.end(), [](char c) { return c == '-' || c == '_'; }), value.end());
        return value;
    }

    void AddAlgorithm(HashRequest& request, const std::string& requested)
    {
        const auto key = AlgorithmKey(requested);
        for (const auto& descriptor : GetRegisteredHashAlgorithmDescriptors())
        {
            if (AlgorithmKey(descriptor.stableName) == key)
            {
                const auto id = GetHashAlgorithmDescriptorId(descriptor);
                if (std::find(request.algorithmIds.begin(), request.algorithmIds.end(), id) == request.algorithmIds.end())
                    AppendHashRequestAlgorithmId(request, id);
                return;
            }
        }
        throw std::invalid_argument("Unknown algorithm: " + requested + ". Use --list-algorithms.");
    }

    void Usage(std::ostream& output)
    {
        output << "Usage: lhash [options] [--] <file> [file...]\n"
               << "  --list-algorithms        List the shared Windows/macOS algorithm set\n"
               << "  -a, --algorithm ID       Select an algorithm (repeat for multiple algorithms)\n"
               << "  --all                   Calculate every available algorithm\n"
               << "  --json                  Emit JSON records (one per file)\n"
               << "  --parallel              Use the parallel digest pipeline\n"
               << "  --single-threaded       Use the sequential digest pipeline\n"
               << "  -h, --help              Show this help\n"
               << "Defaults match Windows: SHA-256, SHA-512 and SHA3-256.\n"
               << "Algorithm aliases such as sha256 are accepted. Paths must be UTF-8.\n";
    }

    void ListAlgorithms(bool json)
    {
        if (json) std::cout << '[';
        bool first = true;
        for (const auto& descriptor : GetRegisteredHashAlgorithmDescriptors())
        {
            if (json)
            {
                if (!first) std::cout << ',';
                std::cout << "{\"id\":" << JsonString(GetHashAlgorithmDescriptorId(descriptor))
                          << ",\"label\":" << JsonString(GetHashAlgorithmDescriptorDisplayLabel(descriptor))
                          << ",\"default\":" << (descriptor.enabledByDefault ? "true" : "false") << '}';
            }
            else std::cout << descriptor.stableName << "\t" << descriptor.displayLabel
                           << (descriptor.enabledByDefault ? "\t(default)" : "") << '\n';
            first = false;
        }
        if (json) std::cout << "]\n";
    }

    int FinishOutput(int status = 0)
    {
        // Buffered output can fail only at flush time. Stream destruction after
        // main returns cannot change the exit status chosen by the application.
        std::cout.flush();
        if (!std::cout)
        {
            std::cerr << "ERROR: Could not write standard output.\n";
            return 1;
        }
        return status;
    }

    class CliProgressSink : public HashProgressSink
    {
    public:
        explicit CliProgressSink(bool json) : json_(json) {}
        bool anyFailed = false;
        int progressMax() override { return 100; }
        void onProgressEvent(const ProgressEvent& ev) override
        {
            const bool failed = ev.type == PROGRESS_EVENT_FILE_FAILED;
            if (!failed && ev.type != PROGRESS_EVENT_FILE_HASH_READY) return;
            anyFailed = anyFailed || failed;
            if (json_)
            {
                std::cout << "{\"path\":" << JsonString(ev.result.path)
                          << ",\"status\":\"" << (failed ? "error" : "ok") << '\"';
                if (failed) std::cout << ",\"error\":" << JsonString(ev.result.error);
                else
                {
                    std::cout << ",\"digests\":{";
                    bool first = true;
                    for (const auto& digest : ev.result.digests)
                    {
                        if (!first) std::cout << ',';
                        std::cout << JsonString(digest.algorithmId) << ':' << JsonString(digest.value);
                        first = false;
                    }
                    std::cout << '}';
                }
                std::cout << "}\n";
            }
            else if (failed)
            {
                std::cerr << "ERROR: " << Escape(sunjwbase::tstrtostrutf8(ev.result.path), false)
                          << ": " << Escape(sunjwbase::tstrtostrutf8(ev.result.error), false) << '\n';
            }
            else
            {
                std::cout << Escape(sunjwbase::tstrtostrutf8(ev.result.path), false) << '\n';
                for (const auto& digest : ev.result.digests)
                    std::cout << "  " << sunjwbase::tstrtostrutf8(digest.displayLabel)
                              << "  " << sunjwbase::tstrtostrutf8(digest.value) << '\n';
            }
        }
    private:
        bool json_;
    };
}

int main(int argc, char **argv)
{
    HashRequest request;
    bool json = false, all = false, list = false, options = true;
    try
    {
        for (int i = 1; i < argc; ++i)
        {
            const std::string arg(argv[i]);
            if (!IsUtf8(arg)) throw std::invalid_argument("Arguments must use valid UTF-8.");
            if (options && arg == "--") { options = false; continue; }
            if (options && (arg == "--help" || arg == "-h")) { Usage(std::cout); return FinishOutput(); }
            if (options && arg == "--json") { json = true; continue; }
            if (options && arg == "--all") { all = true; continue; }
            if (options && arg == "--list-algorithms") { list = true; continue; }
            if (options && arg == "--parallel") { request.digestExecutionPolicy = HASH_REQUEST_DIGEST_EXECUTION_POLICY_PARALLEL; continue; }
            if (options && arg == "--single-threaded") { request.digestExecutionPolicy = HASH_REQUEST_DIGEST_EXECUTION_POLICY_SINGLE_THREADED; continue; }
            if (options && (arg == "--algorithm" || arg == "-a"))
            {
                if (++i >= argc) throw std::invalid_argument("--algorithm requires an algorithm ID.");
                const std::string value(argv[i]);
                if (!IsUtf8(value)) throw std::invalid_argument("Arguments must use valid UTF-8.");
                AddAlgorithm(request, value);
                continue;
            }
            if (options && arg.compare(0, 12, "--algorithm=") == 0) { AddAlgorithm(request, arg.substr(12)); continue; }
            if (options && !arg.empty() && arg[0] == '-') throw std::invalid_argument("Unknown option: " + arg + ". Use -- before filenames beginning with '-'.");
            if (request.files.size() >= kMaxHashFilesPerSession) throw std::invalid_argument("Too many files (maximum 8192).");
            request.files.push_back(sunjwbase::strtotstrutf8(arg));
        }
        if (list)
        {
            if (!request.files.empty() || all || !request.algorithmIds.empty()) throw std::invalid_argument("--list-algorithms cannot be combined with file selection.");
            ListAlgorithms(json);
            return FinishOutput();
        }
        if (request.files.empty()) { Usage(std::cerr); return 2; }
        if (all && !request.algorithmIds.empty()) throw std::invalid_argument("Use either --all or --algorithm.");
        if (all || request.algorithmIds.empty())
            for (const auto& descriptor : GetRegisteredHashAlgorithmDescriptors())
                if (all || descriptor.enabledByDefault) AppendHashRequestAlgorithmId(request, GetHashAlgorithmDescriptorId(descriptor));
        if (request.algorithmIds.empty()) throw std::runtime_error("No digest algorithms are available in this build.");
    }
    catch (const std::exception& error)
    {
        std::cerr << "ERROR: " << Escape(error.what(), false) << '\n';
        return 2;
    }

    CliProgressSink sink(json);
    HashJobState jobState;
    HashCancellationState cancellationState;
    HashExecutionContext context(&sink, jobState, cancellationState);
    try
    {
        const int result = RunHashRequest(&context, request);
        return FinishOutput(result != 0 || sink.anyFailed ? 1 : 0);
    }
    catch (const std::exception& error)
    {
        std::cerr << "ERROR: " << Escape(error.what(), false) << '\n';
        return 1;
    }
}
