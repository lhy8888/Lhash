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

#define WM_THREAD_INFO		WM_USER + 1 // 线程发出消息
#define WP_WORKING			WM_USER + 2 // 开始工作
#define WP_FINISHED			WM_USER + 3 // 线程完成
#define WP_STOPPED			WM_USER + 4 // 线程停止(未完成)
#define WP_REFRESH_TEXT		WM_USER + 5 // 刷新文本框
#define WP_PROG				WM_USER + 6 // 文件进度条
#define WP_PROG_WHOLE		WM_USER + 7 // 全局进度条

#define WM_CUSTOM_MSG		WM_USER + 16 // 自定义消息
#define WM_HYPEREDIT_MENU	WM_USER + 17 // HyperEdit 弹出菜单消息

#else

#define WINAPI

#endif

#include "Common/strhelper.h"

class HashEngineObserver;

struct ResultData;

typedef std::vector<sunjwbase::tstring> TStrVector;
typedef std::vector<uint64_t> ULLongVector;
typedef std::list<ResultData> ResultList;

enum { HASH_ALGORITHM_REGISTRY_COUNT = 4, RESULT_DIGEST_STORAGE_COUNT = HASH_ALGORITHM_REGISTRY_COUNT };

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
	sunjwbase::tstring values[RESULT_DIGEST_STORAGE_COUNT]; // Internal digest storage
};

struct HashAlgorithmSelectionState
{
	bool enabled[HASH_ALGORITHM_REGISTRY_COUNT]; // Enabled hash algorithms for the current session
};

struct ResultDigestCompatibilityFields
{
	sunjwbase::tstring md5; // MD5
	sunjwbase::tstring sha1; // SHA1
	sunjwbase::tstring sha256; // SHA256
	sunjwbase::tstring sha512; // SHA512
};

struct ResultDigestState
{
	ResultDigestStorage storage;
	ResultDigestCompatibilityFields compatibilityFields;
};
struct ResultCoreState
{
	ResultState state; // State
	sunjwbase::tstring path; // 路径
	uint64_t size; // 大小
	sunjwbase::tstring modifiedDate; // 修改日期
	sunjwbase::tstring version; // 版本
	sunjwbase::tstring error; // Error string
};

struct ResultData // 计算结果
{
	ResultCoreState coreState;
	ResultDigestState digestState;
};
struct ThreadDataInputState
{
	uint32_t fileCount; // File count
	TStrVector inputFiles; // Input file paths
};

struct ThreadDataExecutionState
{
	bool working; // Working flag
	bool stopRequested; // Stop request flag
	bool uppercaseDigest; // Uppercase digest output
	HashAlgorithmSelectionState hashAlgorithms; // Enabled hash algorithms
	uint64_t countedSize; // Counted total size
	ResultList results;
};

struct ThreadData // Thread execution context
{
	HashEngineObserver *observer;
	ThreadDataInputState inputState;
	ThreadDataExecutionState executionState;
};

#endif
