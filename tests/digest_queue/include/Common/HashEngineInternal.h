#pragma once

#include <atomic>
#include <cstddef>
#include <cstdint>
#include <limits>
#include <thread>

class ThreadPool;

struct HashExecutionContext
{
    std::atomic<bool> stop{false};
};

inline bool ShouldStopHashExecution(const HashExecutionContext& context)
{
    return context.stop.load();
}

namespace HashEngineInternal
{
    inline unsigned char ExpectedByte(size_t offset)
    {
        return static_cast<unsigned char>((offset * 37 + offset / 251) % 256);
    }

    struct MockFile
    {
        size_t length = 0;
        size_t offset = 0;
        size_t failAfter = std::numeric_limits<size_t>::max();
        bool yieldAfterRead = false;

        int64_t read(void* output, unsigned int capacity)
        {
            if (offset >= failAfter)
                return -1;
            const size_t count = length - offset < capacity ? length - offset : capacity;
            auto* bytes = static_cast<unsigned char*>(output);
            for (size_t i = 0; i < count; ++i)
                bytes[i] = ExpectedByte(offset + i);
            offset += count;
            if (yieldAfterRead)
                std::this_thread::yield();
            return static_cast<int64_t>(count);
        }
    };

    struct FileHashContexts
    {
        size_t bytes = 0;
        bool contentMatches = true;
        bool yieldAfterUpdate = false;
        HashExecutionContext* cancelContext = nullptr;
        size_t cancelAfter = std::numeric_limits<size_t>::max();
    };

    struct FileProgressState { size_t bytes = 0; };
    struct FileAttemptState { MockFile* osFile = nullptr; bool readFailed = false; };
    struct FileExecutionState
    {
        FileAttemptState fileAttemptState;
        FileHashContexts hashContexts;
        FileProgressState progressState;
    };

    struct DigestUpdateRequest {};
    struct HashDigestQueuePlan { size_t maxBufferedChunkCount = 4; };
    inline size_t GetHashDigestQueueMaxBufferedChunkCount(const HashDigestQueuePlan& plan)
    {
        return plan.maxBufferedChunkCount;
    }

    class DigestDataBuffer
    {
    public:
        explicit DigestDataBuffer(unsigned int preferredLength);
        ~DigestDataBuffer();
        unsigned int datalen;
        unsigned int capacity;
        unsigned char* data;
    };

    unsigned int NormalizeDigestDataBufferPreferredLength(unsigned int preferredLength);
    uint64_t CalculateFileChunkIterations(uint64_t fileSize, unsigned int preferredLength);

    inline void UpdateDigestContextsParallel(const DigestUpdateRequest&, FileHashContexts& hash,
        unsigned char* data, unsigned int count, ThreadPool*)
    {
        for (unsigned int i = 0; i < count; ++i)
            hash.contentMatches = hash.contentMatches && data[i] == ExpectedByte(hash.bytes + i);
        hash.bytes += count;
        if (hash.cancelContext && hash.bytes >= hash.cancelAfter)
            hash.cancelContext->stop.store(true);
        if (hash.yieldAfterUpdate)
            std::this_thread::yield();
    }

    inline void UpdateHashExecutionProgress(HashExecutionContext*, uint64_t, bool,
        unsigned int count, FileProgressState* progress)
    {
        progress->bytes += count;
    }

    bool ProcessOpenedFileHashingParallel(HashExecutionContext*, const DigestUpdateRequest&,
        uint64_t, bool, unsigned int, const HashDigestQueuePlan&, FileExecutionState*, ThreadPool*);
}
