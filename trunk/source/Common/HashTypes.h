#ifndef _HASH_TYPES_H_
#define _HASH_TYPES_H_

#include <atomic>
#include <cstddef>
#include <cstdint>
#include <list>
#include <vector>

#include "Common/strhelper.h"

class HashProgressSink;

typedef std::vector<sunjwbase::tstring> TStrVector;
typedef std::vector<uint64_t> ULLongVector;

static constexpr size_t kMaxHashFilesPerSession = 8192;

enum ResultState
{
	RESULT_NONE = 0,
	RESULT_PATH,
	RESULT_META,
	RESULT_ALL,
	RESULT_ERROR
};

struct ResultDigestStorage
{
	std::vector<sunjwbase::tstring> values;
};

struct HashAlgorithmSelectionState
{
	std::vector<bool> enabled;
};

struct HashExecutionPreferenceState
{
	HashExecutionPreferenceState()
		: uppercaseDigest(false)
	{
	}

	bool uppercaseDigest;
	HashAlgorithmSelectionState hashAlgorithms;
};

struct HashCancellationState
{
	HashCancellationState()
		: stopRequested(false)
	{
	}

	HashCancellationState(const HashCancellationState& other)
		: stopRequested(other.stopRequested.load())
	{
	}

	HashCancellationState& operator=(const HashCancellationState& other)
	{
		stopRequested.store(other.stopRequested.load());
		return *this;
	}

	std::atomic<bool> stopRequested;
};

struct ResultDigestState
{
	ResultDigestStorage storage;
};

struct ResultCoreState
{
	ResultState state;
	sunjwbase::tstring path;
	uint64_t size;
	sunjwbase::tstring modifiedDate;
	sunjwbase::tstring version;
	sunjwbase::tstring error;
};

struct ResultData
{
	ResultCoreState coreState;
	ResultDigestState digestState;
};

struct HashDigestResult
{
	HashDigestResult() {}
	sunjwbase::tstring algorithmId;
	sunjwbase::tstring stableName;
	sunjwbase::tstring displayLabel;
	sunjwbase::tstring value;
};

struct HashFileMeta
{
	HashFileMeta()
		: size(0)
	{
	}

	uint64_t size;
	sunjwbase::tstring modifiedDate;
	sunjwbase::tstring version;
};

struct HashResult
{
	HashResult()
		: state(RESULT_NONE)
	{
	}

	ResultState state;
	sunjwbase::tstring path;
	HashFileMeta meta;
	sunjwbase::tstring error;
	std::vector<HashDigestResult> digests;
};

typedef std::list<HashResult> HashResultList;
typedef HashResultList ResultList;

struct HashJobState
{
	HashJobState()
		: working(false),
		countedSize(0)
	{
	}

	HashJobState(const HashJobState& other)
		: working(other.working.load()),
		countedSize(other.countedSize.load(std::memory_order_relaxed)),
		results(other.results)
	{
	}

	HashJobState& operator=(const HashJobState& other)
	{
		working.store(other.working.load());
		countedSize.store(other.countedSize.load(std::memory_order_relaxed), std::memory_order_relaxed);
		results = other.results;
		return *this;
	}

	std::atomic<bool> working;
	std::atomic<uint64_t> countedSize;
	// HashJobState.results is owned by the synchronous HashEngine execution thread.
	// UI code must consume published snapshots instead of mutating or traversing
	// this list concurrently.
	HashResultList results;
};

#endif
