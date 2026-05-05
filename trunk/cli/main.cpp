#include "stdafx.h"

#include "Common/HashEngine.h"
#include "Domain/HashRequest.h"
#include "Domain/ProgressEvent.h"
#include "Runtime/HashExecutionContext.h"
#include "Runtime/HashProgressSink.h"
#include "Common/strhelper.h"

#include <iostream>

class CliProgressSink : public HashProgressSink
{
public:
    bool anyFailed = false;

    int progressMax() override
    {
        return 100;
    }

    void onProgressEvent(const ProgressEvent& ev) override
    {
        if (ev.type == PROGRESS_EVENT_FILE_HASH_READY)
        {
            std::cout << sunjwbase::tstrtostr(ev.result.path) << "\n";
            for (const auto& d : ev.result.digests)
            {
                std::cout << "  " << sunjwbase::tstrtostr(d.displayLabel)
                    << "  " << sunjwbase::tstrtostr(d.value) << "\n";
            }
        }
        else if (ev.type == PROGRESS_EVENT_FILE_FAILED)
        {
            anyFailed = true;
            std::cerr << "ERROR: " << sunjwbase::tstrtostr(ev.result.path)
                << ": " << sunjwbase::tstrtostr(ev.result.error) << "\n";
        }
    }
};

int main(int argc, char **argv)
{
    if (argc < 2)
    {
        std::cerr << "Usage: lhash <file> [file...]\n";
        return 2;
    }

    HashRequest request;
    for (int i = 1; i < argc; ++i)
    {
        // macOS CLI MVP: expects UTF-8 command-line path arguments.
        // Linux/non-UTF-8 locale support requires std::filesystem::path migration.
        request.files.push_back(sunjwbase::strtotstr(argv[i]));
    }

    AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr("md5"));
    AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr("sha1"));

    CliProgressSink sink;
    HashJobState jobState;
    HashCancellationState cancellationState;
    HashExecutionContext ctx(&sink, jobState, cancellationState);

    int result = RunHashRequest(&ctx, request);
    if (result != 0)
    {
        return result;
    }
    return sink.anyFailed ? 1 : 0;
}
