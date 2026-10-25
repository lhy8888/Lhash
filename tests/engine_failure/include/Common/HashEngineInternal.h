#pragma once

// Narrow fault-injection boundary: the entry point and lifecycle are production
// sources; preparation and scheduling throw without requiring a platform backend.
#include "Domain/HashRequest.h"
#include "Runtime/HashExecutionContext.h"
#include "Common/HashDigestUpdater.h"
#include <future>
#include <stdexcept>

namespace HashEngineInternal
{
    struct FileHashContexts
    {
        std::promise<void>* started = nullptr;
        std::shared_future<void>* release = nullptr;
        unsigned int completed = 0;
    };
    enum class FailureStage { None, Initialization, Preparation, Scheduler, Completion, Cancellation };
    inline FailureStage failureStage = FailureStage::None;
    inline bool cancelDuringPreparation = false;
    struct HashJobExecutionPlan {};
    struct HashPreparationPlan {};
    inline void InjectFailure(FailureStage stage)
    {
        if (failureStage == stage)
            throw std::runtime_error("injected engine failure");
    }
    inline void InitializeHashJobExecutionPlan(const HashRequest&, HashJobExecutionPlan*)
    {
        InjectFailure(FailureStage::Initialization);
    }
    inline HashPreparationPlan GetHashJobPreparationPlan(const HashJobExecutionPlan&) { return {}; }
    inline bool PrepareHashingWork(HashExecutionContext*, const HashRequest&, const HashPreparationPlan&,
        ULLongVector&, bool* wasCancelled)
    {
        InjectFailure(FailureStage::Preparation);
        *wasCancelled = cancelDuringPreparation;
        return true;
    }
    inline bool RunHashScheduler(HashExecutionContext*, const HashRequest&, const HashJobExecutionPlan&, bool, ULLongVector&)
    {
        InjectFailure(FailureStage::Scheduler);
        return true;
    }
    void ExecuteCancelledHashingWorkflow(HashExecutionContext*, HashProgressSink*);
    void ExecuteCompletedHashingWorkflow(HashExecutionContext*, HashProgressSink*);
}
