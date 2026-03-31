#ifndef _GLOBAL_H_
#define _GLOBAL_H_
#include <stdint.h>
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

enum ResultDigestType
{
	RESULT_DIGEST_MD5 = 0,
	RESULT_DIGEST_SHA1,
	RESULT_DIGEST_SHA256,
	RESULT_DIGEST_SHA512
};

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
	HashDigestResult()
		: type(RESULT_DIGEST_MD5)
	{
	}
	ResultDigestType type;
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

struct ThreadDataInputState
{
	uint32_t fileCount;
	TStrVector inputFiles;
};

struct ThreadDataExecutionState
{
	bool working;
	bool stopRequested;
	bool uppercaseDigest;
	HashAlgorithmSelectionState hashAlgorithms;
	uint64_t countedSize;
	HashResultList results;
};

struct ThreadData
{
	HashProgressSink *observer;
	ThreadDataInputState inputState;
	ThreadDataExecutionState executionState;
};
#endif
