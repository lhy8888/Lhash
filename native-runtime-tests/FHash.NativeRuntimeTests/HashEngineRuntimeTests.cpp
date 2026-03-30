#include "..\..\trunk\source\stdafx.h"

#include <iostream>
#include <vector>

#include "NativeTestHarness.h"

#include "Common/HashAlgorithmRegistry.h"
#include "Common/HashEngine.h"
#include "Common/HashExecutionContext.h"
#include "Common/HashProgressSink.h"
#include "Common/HashRequest.h"
#include "Common/HashResultSearch.h"
#include "Common/ThreadDataAccess.h"

namespace
{
	class CapturingProgressSink : public HashProgressSink
	{
	public:
		explicit CapturingProgressSink(int progressMaximum = 100)
			: progressMaximum_(progressMaximum)
		{
		}

		virtual int progressMax()
		{
			return progressMaximum_;
		}

		virtual void onProgressEvent(const ProgressEvent& progressEvent)
		{
			events.push_back(progressEvent);
		}

		bool HasEvent(ProgressEventType eventType) const
		{
			for (size_t eventIndex = 0; eventIndex < events.size(); ++eventIndex)
			{
				if (events[eventIndex].type == eventType)
				{
					return true;
				}
			}

			return false;
		}

		std::vector<ProgressEvent> events;

	private:
		int progressMaximum_;
	};

	class ScopedTempDirectory
	{
	public:
		ScopedTempDirectory()
		{
			TCHAR tempPath[MAX_PATH] = { 0 };
			DWORD copied = ::GetTempPath(MAX_PATH, tempPath);
			NativeAssertTrue(copied > 0 && copied < MAX_PATH, "Unable to locate the Windows temp directory.");

			sunjwbase::tstring rootPath(tempPath);
			if (!rootPath.empty() &&
				rootPath[rootPath.length() - 1] == _T('\\'))
			{
				rootPath.erase(rootPath.length() - 1);
			}

			rootPath += _T("\\fhash-native-runtime-tests");
			::CreateDirectory(rootPath.c_str(), NULL);

			std::basic_ostringstream<TCHAR> pathBuilder;
			pathBuilder << rootPath << _T("\\run-") << ::GetCurrentProcessId() << _T('-') << ::GetTickCount64();
			path_ = pathBuilder.str();

			if (!::CreateDirectory(path_.c_str(), NULL))
			{
				DWORD error = ::GetLastError();
				if (error != ERROR_ALREADY_EXISTS)
				{
					throw NativeTestFailure("Unable to create the native runtime test directory.");
				}
			}
		}

		~ScopedTempDirectory()
		{
			for (size_t fileIndex = 0; fileIndex < files_.size(); ++fileIndex)
			{
				::DeleteFile(files_[fileIndex].c_str());
			}

			if (!path_.empty())
			{
				::RemoveDirectory(path_.c_str());
			}
		}

		sunjwbase::tstring WriteTextFile(const sunjwbase::tstring& fileName, const std::string& contents)
		{
			sunjwbase::tstring filePath = path_ + _T("\\") + fileName;
			HANDLE fileHandle = ::CreateFile(filePath.c_str(), GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
			NativeAssertTrue(fileHandle != INVALID_HANDLE_VALUE, "Unable to create the native runtime test input file.");

			DWORD bytesWritten = 0;
			BOOL writeSucceeded = ::WriteFile(fileHandle, contents.data(), static_cast<DWORD>(contents.size()), &bytesWritten, NULL);
			::CloseHandle(fileHandle);

			NativeAssertTrue(writeSucceeded == TRUE, "Unable to write the native runtime test input file.");
			NativeAssertEqual(static_cast<DWORD>(contents.size()), bytesWritten, "The native runtime test input file was only partially written.");

			files_.push_back(filePath);
			return filePath;
		}

		sunjwbase::tstring BuildPath(const sunjwbase::tstring& fileName) const
		{
			return path_ + _T("\\") + fileName;
		}

	private:
		sunjwbase::tstring path_;
		std::vector<sunjwbase::tstring> files_;
	};

	static void DisableAllAlgorithms(ThreadData& threadData)
	{
		VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& descriptor)
		{
			(void)index;
			SetThreadDataHashAlgorithmEnabled(threadData, GetHashAlgorithmDescriptorType(descriptor), false);
			return true;
		});
	}

	static void ConfigureThreadData(ThreadData& threadData, CapturingProgressSink& progressSink, const sunjwbase::tstring& filePath, const std::vector<ResultDigestType>& enabledAlgorithms)
	{
		ResetThreadDataForNewSession(threadData);
		SetThreadDataObserver(threadData, &progressSink);
		AppendThreadDataInputFile(threadData, filePath);

		DisableAllAlgorithms(threadData);
		for (size_t algorithmIndex = 0; algorithmIndex < enabledAlgorithms.size(); ++algorithmIndex)
		{
			SetThreadDataHashAlgorithmEnabled(threadData, enabledAlgorithms[algorithmIndex], true);
		}
	}

	static sunjwbase::tstring FindDigestValue(const HashResult& result, ResultDigestType digestType)
	{
		for (size_t digestIndex = 0; digestIndex < result.digests.size(); ++digestIndex)
		{
			if (result.digests[digestIndex].type == digestType)
			{
				return result.digests[digestIndex].value;
			}
		}

		return sunjwbase::tstring();
	}

	static void HashThreadFunc_ComputesExpectedDigestsForSingleFile()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("abc.txt"), "abc");

		CapturingProgressSink progressSink;
		ThreadData threadData;
		std::vector<ResultDigestType> algorithms;
		algorithms.push_back(RESULT_DIGEST_MD5);
		algorithms.push_back(RESULT_DIGEST_SHA1);
		algorithms.push_back(RESULT_DIGEST_SHA256);
		algorithms.push_back(RESULT_DIGEST_SHA512);
		ConfigureThreadData(threadData, progressSink, filePath, algorithms);

		int exitCode = HashThreadFunc(&threadData);
		NativeAssertEqual(0, exitCode, "HashThreadFunc should succeed for an existing file.");
		NativeAssertTrue(!IsThreadDataWorking(threadData), "ThreadData should not remain in the working state after hashing completes.");
		NativeAssertEqual(static_cast<uint64_t>(1), GetThreadDataResultCount(threadData), "HashThreadFunc should append exactly one result for a single file.");

		const HashResult& result = GetThreadDataResults(threadData).front();
		NativeAssertEqual(RESULT_ALL, result.state, "Successful hashing should end in RESULT_ALL.");
		NativeAssertEqual(filePath, result.path, "The resulting path should match the requested file.");
		NativeAssertEqual(static_cast<uint64_t>(3), result.meta.size, "The runtime result should report the correct file size.");
		NativeAssertEqual(static_cast<size_t>(4), result.digests.size(), "The runtime result should expose all requested digest values.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("900150983CD24FB0D6963F7D28E17F72")), FindDigestValue(result, RESULT_DIGEST_MD5), "The MD5 digest did not match the known vector for 'abc'.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("A9993E364706816ABA3E25717850C26C9CD0D89D")), FindDigestValue(result, RESULT_DIGEST_SHA1), "The SHA1 digest did not match the known vector for 'abc'.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD")), FindDigestValue(result, RESULT_DIGEST_SHA256), "The SHA256 digest did not match the known vector for 'abc'.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("DDAF35A193617ABACC417349AE20413112E6FA4E89A97EA20A9EEEE64B55D39A2192992A274FC1A836BA3C23A3FEEBBD454D4423643CE80E2A9AC94FA54CA49F")), FindDigestValue(result, RESULT_DIGEST_SHA512), "The SHA512 digest did not match the known vector for 'abc'.");

		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_JOB_PREPARING), "The runtime path should emit a preparing event.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_JOB_PREPARATION_FINISHED), "The runtime path should emit a preparation-finished event.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_FILE_STARTED), "The runtime path should emit a file-started event.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_FILE_META_READY), "The runtime path should emit a file-meta event.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_FILE_HASH_READY), "The runtime path should emit a file-hash event.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_JOB_COMPLETED), "The runtime path should emit a completed event.");
	}

	static void HashThreadFunc_RespectsSelectedAlgorithms()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("sha256-only.txt"), "abc");

		CapturingProgressSink progressSink;
		ThreadData threadData;
		std::vector<ResultDigestType> algorithms;
		algorithms.push_back(RESULT_DIGEST_SHA256);
		ConfigureThreadData(threadData, progressSink, filePath, algorithms);

		int exitCode = HashThreadFunc(&threadData);
		NativeAssertEqual(0, exitCode, "HashThreadFunc should succeed for a SHA256-only request.");
		NativeAssertEqual(static_cast<uint64_t>(1), GetThreadDataResultCount(threadData), "A SHA256-only request should still produce one file result.");

		const HashResult& result = GetThreadDataResults(threadData).front();
		NativeAssertEqual(static_cast<size_t>(1), result.digests.size(), "Only the explicitly enabled algorithm should be emitted.");
		NativeAssertEqual(RESULT_DIGEST_SHA256, result.digests[0].type, "The only emitted digest should be SHA256.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD")), result.digests[0].value, "The SHA256-only digest value did not match the known vector.");
	}

	static void HashResultSearch_FindsMatchingRuntimeDigests()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("searchable.txt"), "abc");

		CapturingProgressSink progressSink;
		ThreadData threadData;
		std::vector<ResultDigestType> algorithms;
		algorithms.push_back(RESULT_DIGEST_MD5);
		ConfigureThreadData(threadData, progressSink, filePath, algorithms);

		int exitCode = HashThreadFunc(&threadData);
		NativeAssertEqual(0, exitCode, "HashThreadFunc should succeed for a searchable digest result.");

		const HashResultList& results = GetThreadDataResults(threadData);
		sunjwbase::tstring digestQuery = NormalizeHashResultDigestSearchText(sunjwbase::strtotstr(std::string(" 90015098 ")));
		sunjwbase::tstring missingQuery = NormalizeHashResultDigestSearchText(sunjwbase::strtotstr(std::string(" no-match ")));

		NativeAssertEqual(static_cast<size_t>(1), CountDigestMatchingHashResults(results, digestQuery), "The runtime digest search should find the emitted MD5 prefix.");
		NativeAssertEqual(static_cast<size_t>(0), CountDigestMatchingHashResults(results, missingQuery), "The runtime digest search should reject non-matching digests.");
	}

	static void RunHashRequest_ReportsMissingFileAsErrorResult()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring missingPath = tempDirectory.BuildPath(_T("missing.txt"));

		CapturingProgressSink progressSink;
		bool workingFlag = false;
		bool stopRequestedFlag = false;
		uint64_t countedSize = 0;
		HashResultList results;

		HashExecutionContext executionContext;
		executionContext.progressSink = &progressSink;
		executionContext.workingFlag = &workingFlag;
		executionContext.stopRequestedFlag = &stopRequestedFlag;
		executionContext.countedSize = &countedSize;
		executionContext.results = &results;

		HashRequest request;
		request.files.push_back(missingPath);
		request.algorithms.push_back(RESULT_DIGEST_MD5);

		int exitCode = RunHashRequest(&executionContext, request);
		NativeAssertEqual(0, exitCode, "RunHashRequest should complete even when a file cannot be opened.");
		NativeAssertTrue(!workingFlag, "RunHashRequest should clear the working flag after a missing-file attempt.");
		NativeAssertEqual(static_cast<size_t>(1), results.size(), "A missing file should still produce one error result.");

		const HashResult& result = results.front();
		NativeAssertEqual(RESULT_ERROR, result.state, "A missing file should produce RESULT_ERROR.");
		NativeAssertEqual(missingPath, result.path, "The error result should preserve the requested file path.");
		NativeAssertNotEmpty(result.error, "The missing-file result should expose an error message.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_FILE_FAILED), "A missing file should emit a file-failed event.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_JOB_COMPLETED), "A missing file should still emit a completed event.");
	}

	static void RunHashRequest_CancelsWhenStopRequestedBeforeStart()
	{
		CapturingProgressSink progressSink;
		bool workingFlag = false;
		bool stopRequestedFlag = true;
		uint64_t countedSize = 0;
		HashResultList results;

		HashExecutionContext executionContext;
		executionContext.progressSink = &progressSink;
		executionContext.workingFlag = &workingFlag;
		executionContext.stopRequestedFlag = &stopRequestedFlag;
		executionContext.countedSize = &countedSize;
		executionContext.results = &results;

		HashRequest request;
		request.files.push_back(sunjwbase::strtotstr(std::string("should-not-run.txt")));
		request.algorithms.push_back(RESULT_DIGEST_MD5);

		int exitCode = RunHashRequest(&executionContext, request);
		NativeAssertEqual(0, exitCode, "RunHashRequest should return 0 for a cooperative cancellation.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_JOB_CANCELLED), "A pre-stopped execution context should emit a cancelled event.");
		NativeAssertTrue(!workingFlag, "A cancelled execution should not leave the working flag enabled.");
		NativeAssertEqual(static_cast<size_t>(0), results.size(), "A cancelled execution should not append any file results.");
	}
}

void RegisterHashEngineRuntimeTests(std::vector<NativeTestCase>& tests)
{
	tests.push_back({ "HashThreadFunc_ComputesExpectedDigestsForSingleFile", &HashThreadFunc_ComputesExpectedDigestsForSingleFile });
	tests.push_back({ "HashThreadFunc_RespectsSelectedAlgorithms", &HashThreadFunc_RespectsSelectedAlgorithms });
	tests.push_back({ "HashResultSearch_FindsMatchingRuntimeDigests", &HashResultSearch_FindsMatchingRuntimeDigests });
	tests.push_back({ "RunHashRequest_ReportsMissingFileAsErrorResult", &RunHashRequest_ReportsMissingFileAsErrorResult });
	tests.push_back({ "RunHashRequest_CancelsWhenStopRequestedBeforeStart", &RunHashRequest_CancelsWhenStopRequestedBeforeStart });
}
