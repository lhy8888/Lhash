#ifndef _GLOBAL_H_
#define _GLOBAL_H_
#include <stdint.h>
#include <atomic>
#include <vector>
#include <list>
#if defined (_WIN32)
#include <atlbase.h>
#include <WinUser.h>
#include <WinDef.h>
#include <WinNT.h>
#define WM_THREAD_INFO		WM_USER + 1 // ??????
#define WP_WORKING			WM_USER + 2 // ????
#define WP_FINISHED			WM_USER + 3 // ????
#define WP_STOPPED			WM_USER + 4 // ????(???)
#define WP_REFRESH_TEXT		WM_USER + 5 // ?????
#define WP_PROG			WM_USER + 6 // ?????
#define WP_PROG_WHOLE		WM_USER + 7 // ?????
#define WM_CUSTOM_MSG		WM_USER + 16 // ?????
#define WM_HYPEREDIT_MENU	WM_USER + 17 // HyperEdit ??????
#else
#define WINAPI
#endif
#include "Common/strhelper.h"
class HashProgressSink;
typedef std::vector<sunjwbase::tstring> TStrVector;
typedef std::vector<uint64_t> ULLongVector;

#define MAX_FILES_NUM 8192

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
		countedSize(other.countedSize),
		results(other.results)
	{
	}

	HashJobState& operator=(const HashJobState& other)
	{
		working.store(other.working.load());
		countedSize = other.countedSize;
		results = other.results;
		return *this;
	}

	std::atomic<bool> working;
	uint64_t countedSize;
	HashResultList results;
};

#endif
