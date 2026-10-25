#include "stdafx.h"
#include "Common/HashEngine.h"
#include "Common/HashEngineInternal.h"
#include <iostream>

using namespace HashEngineInternal;

class ThrowingSink : public HashProgressSink
{
public:
    int progressMax() override { return 100; }
    void onProgressEvent(const ProgressEvent& event) override
    {
        if (event.type == PROGRESS_EVENT_JOB_COMPLETED)
            InjectFailure(FailureStage::Completion);
        if (event.type == PROGRESS_EVENT_JOB_CANCELLED)
            InjectFailure(FailureStage::Cancellation);
    }
};

int main()
{
    try
    {
        for (FailureStage stage : {FailureStage::Initialization, FailureStage::Preparation,
            FailureStage::Scheduler, FailureStage::Completion, FailureStage::Cancellation, FailureStage::None})
        {
            failureStage = stage;
            cancelDuringPreparation = stage == FailureStage::Cancellation;
            ThrowingSink sink;
            HashJobState state;
            HashCancellationState cancellation;
            HashExecutionContext context(&sink, state, cancellation);
            HashRequest request;
            bool caught = false;
            try
            {
                RunHashRequest(&context, request);
            }
            catch (const std::runtime_error& error)
            {
                caught = std::string(error.what()) == "injected engine failure";
            }
            if (caught != (stage != FailureStage::None) || state.working.load())
                throw std::runtime_error("failure propagation or working-state cleanup mismatch");
        }
        std::cout << "PASS engine_failure_stages=5 normal_completion=1\n";
        return 0;
    }
    catch (const std::exception& error)
    {
        std::cerr << "FAIL: " << error.what() << '\n';
        return 1;
    }
}
