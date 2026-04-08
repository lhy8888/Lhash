#include "..\..\trunk\source\stdafx.h"

#include <iostream>
#include <mutex>
#include <vector>

#include "NativeTestHarness.h"

#include "Domain/HashAlgorithmRegistryCore.h"
#include "Common/HashDigestOperationRegistry.h"
#include "Common/HashDigestUpdater.h"
#include "Common/HashEngine.h"
#include "LegacyCompat/HashThreadEntry.h"
#include "Runtime/HashExecutionContext.h"
#include "Runtime/HashProgressSink.h"
#include "Domain/HashRequest.h"
#include "Common/HashResultSearch.h"
#include "Common/ResultDataAccess.h"
#include "Common/ResultDigestValueAccess.h"
#include "LegacyCompat/HashAlgorithmTypeCompat.h"
#include "LegacyCompat/HashDigestOperationTypeCompat.h"
#include "LegacyCompat/HashThreadEntryProjection.h"
#include "LegacyCompat/HashRequestTypeCompat.h"
#include "LegacyCompat/ResultDigestTypeValueCompat.h"
#include "LegacyCompat/ThreadDataAccess.h"

namespace
{
	static const size_t kHashEngineBufferSize = 1048576;

	class ScopedHashAlgorithmRegistryReset
	{
	public:
		ScopedHashAlgorithmRegistryReset()
		{
			ResetHashAlgorithmDescriptorsToDefaultsForTesting();
		}

		~ScopedHashAlgorithmRegistryReset()
		{
			ResetHashAlgorithmDescriptorsToDefaultsForTesting();
		}
	};

	class CapturingProgressSink : public HashProgressSink
	{
	public:
		explicit CapturingProgressSink(int progressMaximum = 100)
			: progressMaximum_(progressMaximum),
			stopRequestedFlag_(NULL),
			stopEventType_(PROGRESS_EVENT_NONE),
			stopEventMinimumValue_(0),
			traceToStdout_(false),
			traceLabel_()
		{
		}

		virtual int progressMax()
		{
			return progressMaximum_;
		}

		virtual void onProgressEvent(const ProgressEvent& progressEvent)
		{
			if (traceToStdout_)
			{
				std::cout
					<< "TRACE_EVENT[" << traceLabel_ << "]: type=" << static_cast<int>(progressEvent.type)
					<< ", value=" << progressEvent.value
					<< ", uppercase=" << (progressEvent.uppercaseDigest ? 1 : 0)
					<< std::endl;
			}

			{
				std::lock_guard<std::mutex> lock(eventsMutex_);
				events_.push_back(progressEvent);
			}

			if (stopRequestedFlag_ != NULL &&
				progressEvent.type == stopEventType_ &&
				progressEvent.value >= stopEventMinimumValue_)
			{
				stopRequestedFlag_->store(true);
			}
		}

		void ConfigureStopOnEvent(std::atomic<bool> *stopRequestedFlag, ProgressEventType eventType, int minimumValue)
		{
			stopRequestedFlag_ = stopRequestedFlag;
			stopEventType_ = eventType;
			stopEventMinimumValue_ = minimumValue;
		}

		void EnableConsoleTrace(const std::string& traceLabel)
		{
			traceToStdout_ = true;
			traceLabel_ = traceLabel;
		}

		bool HasEvent(ProgressEventType eventType) const
		{
			return CountEvents(eventType) > 0;
		}

		size_t CountEvents(ProgressEventType eventType) const
		{
			size_t eventCount = 0;
			std::lock_guard<std::mutex> lock(eventsMutex_);
			for (size_t eventIndex = 0; eventIndex < events_.size(); ++eventIndex)
			{
				if (events_[eventIndex].type == eventType)
				{
					++eventCount;
				}
			}

			return eventCount;
		}

		bool TryGetFirstEvent(ProgressEventType eventType, ProgressEvent *progressEvent) const
		{
			std::lock_guard<std::mutex> lock(eventsMutex_);
			for (size_t eventIndex = 0; eventIndex < events_.size(); ++eventIndex)
			{
				if (events_[eventIndex].type == eventType)
				{
					*progressEvent = events_[eventIndex];
					return true;
				}
			}

			return false;
		}

		int GetFirstEventIndex(ProgressEventType eventType) const
		{
			std::lock_guard<std::mutex> lock(eventsMutex_);
			for (size_t eventIndex = 0; eventIndex < events_.size(); ++eventIndex)
			{
				if (events_[eventIndex].type == eventType)
				{
					return static_cast<int>(eventIndex);
				}
			}

			return -1;
		}

		int GetLastValue(ProgressEventType eventType, int defaultValue = -1) const
		{
			int lastValue = defaultValue;
			std::lock_guard<std::mutex> lock(eventsMutex_);
			for (size_t eventIndex = 0; eventIndex < events_.size(); ++eventIndex)
			{
				if (events_[eventIndex].type == eventType)
				{
					lastValue = events_[eventIndex].value;
				}
			}

			return lastValue;
		}

	private:
		int progressMaximum_;
		std::atomic<bool> *stopRequestedFlag_;
		ProgressEventType stopEventType_;
		int stopEventMinimumValue_;
		bool traceToStdout_;
		std::string traceLabel_;
		mutable std::mutex eventsMutex_;
		std::vector<ProgressEvent> events_;
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
			SetThreadDataHashAlgorithmEnabledById(threadData, GetHashAlgorithmDescriptorId(descriptor), false);
			return true;
		});
	}

	static void ConfigureThreadDataFiles(ThreadData& threadData, CapturingProgressSink& progressSink, const std::vector<sunjwbase::tstring>& filePaths, const std::vector<ResultDigestType>& enabledAlgorithms, bool uppercaseDigest = false)
	{
		ResetThreadDataForNewSession(threadData);
		SetThreadDataObserver(threadData, &progressSink);
		SetThreadDataUppercase(threadData, uppercaseDigest);
		for (size_t fileIndex = 0; fileIndex < filePaths.size(); ++fileIndex)
		{
			AppendThreadDataInputFile(threadData, filePaths[fileIndex]);
		}

		DisableAllAlgorithms(threadData);
		for (size_t algorithmIndex = 0; algorithmIndex < enabledAlgorithms.size(); ++algorithmIndex)
		{
			SetThreadDataHashAlgorithmEnabled(threadData, enabledAlgorithms[algorithmIndex], true);
		}
	}

	static void ConfigureThreadData(ThreadData& threadData, CapturingProgressSink& progressSink, const sunjwbase::tstring& filePath, const std::vector<ResultDigestType>& enabledAlgorithms, bool uppercaseDigest = false)
	{
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(filePath);
		ConfigureThreadDataFiles(threadData, progressSink, filePaths, enabledAlgorithms, uppercaseDigest);
	}

		static HashExecutionContext CreateExecutionContext(CapturingProgressSink& progressSink, HashJobState& jobState, HashCancellationState& cancellationState)
		{
			return HashExecutionContext(&progressSink, jobState, cancellationState);
		}

	static HashRequest CreateRequest(const std::vector<sunjwbase::tstring>& filePaths, const std::vector<ResultDigestType>& enabledAlgorithms, bool uppercaseDigest = false)
	{
		HashRequest request;
		request.files = filePaths;
		for (size_t algorithmIndex = 0; algorithmIndex < enabledAlgorithms.size(); ++algorithmIndex)
		{
			AppendHashRequestAlgorithm(request, enabledAlgorithms[algorithmIndex]);
		}
		request.uppercaseDigest = uppercaseDigest;
		return request;
	}

	static int RunHashThreadData(ThreadData& threadData)
	{
		std::cout << "TRACE_PHASE: CreateThreadDataHashExecutionContext" << std::endl;
		HashExecutionContext executionContext = CreateThreadDataHashExecutionContext(threadData);
		std::cout << "TRACE_PHASE: CreateThreadDataHashRequest" << std::endl;
		HashRequest request = CreateThreadDataHashRequest(threadData);
		std::cout << "TRACE_PHASE: RunHashRequest begin" << std::endl;
		int exitCode = RunHashRequest(&executionContext, request);
		std::cout << "TRACE_PHASE: RunHashRequest end, exitCode=" << exitCode << std::endl;
		return exitCode;
	}

	static HashAlgorithmId ResolveDigestResultAlgorithmId(const HashDigestResult& digestResult)
	{
		HashAlgorithmId normalizedAlgorithmId = NormalizeHashAlgorithmId(digestResult.algorithmId);
		if (!normalizedAlgorithmId.empty())
		{
			return normalizedAlgorithmId;
		}

		return NormalizeHashAlgorithmId(digestResult.stableName);
	}

	static sunjwbase::tstring FindDigestValue(const HashResult& result, ResultDigestType digestType)
	{
		HashAlgorithmId targetAlgorithmId = NormalizeHashAlgorithmId(GetHashAlgorithmId(digestType));
		for (size_t digestIndex = 0; digestIndex < result.digests.size(); ++digestIndex)
		{
			if (ResolveDigestResultAlgorithmId(result.digests[digestIndex]) == targetAlgorithmId)
			{
				return result.digests[digestIndex].value;
			}
		}

		return sunjwbase::tstring();
	}

	static const HashResult *FindHashResultByPath(const HashResultList& results, const sunjwbase::tstring& path)
	{
		for (HashResultList::const_iterator itr = results.begin(); itr != results.end(); ++itr)
		{
			if (itr->path == path)
			{
				return &(*itr);
			}
		}

		return NULL;
	}

	static void HashThreadFunc_ComputesExpectedDigestsForSingleFile()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("abc.txt"), "abc");

		CapturingProgressSink progressSink;
		progressSink.EnableConsoleTrace("single-file");
		ThreadData threadData;
		std::vector<ResultDigestType> algorithms;
		algorithms.push_back(RESULT_DIGEST_MD5);
		algorithms.push_back(RESULT_DIGEST_SHA1);
		algorithms.push_back(RESULT_DIGEST_SHA256);
		algorithms.push_back(RESULT_DIGEST_SHA512);
		ConfigureThreadData(threadData, progressSink, filePath, algorithms);
		std::cout << "TRACE_PHASE: single-file configured thread data" << std::endl;

		int exitCode = RunHashThreadData(threadData);
		std::cout << "TRACE_PHASE: single-file after RunHashThreadData" << std::endl;
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

	static void HashThreadFunc_ProcessesMultipleFilesAndWholeProgress()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring abcPath = tempDirectory.WriteTextFile(_T("abc.txt"), "abc");
		sunjwbase::tstring helloPath = tempDirectory.WriteTextFile(_T("hello.txt"), "hello");

		CapturingProgressSink progressSink;
		ThreadData threadData;
		std::vector<ResultDigestType> algorithms;
		algorithms.push_back(RESULT_DIGEST_MD5);
		algorithms.push_back(RESULT_DIGEST_SHA256);
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(abcPath);
		filePaths.push_back(helloPath);
		ConfigureThreadDataFiles(threadData, progressSink, filePaths, algorithms);

		int exitCode = RunHashThreadData(threadData);
		NativeAssertEqual(0, exitCode, "HashThreadFunc should succeed for a multi-file request.");
		NativeAssertEqual(static_cast<uint64_t>(2), GetThreadDataResultCount(threadData), "A two-file request should append two results.");
		NativeAssertEqual(static_cast<uint64_t>(8), GetThreadDataTotalSize(threadData), "The counted runtime size should match the sum of the two input files.");

		const HashResultList& results = GetThreadDataResults(threadData);
		const HashResult *abcResult = FindHashResultByPath(results, abcPath);
		const HashResult *helloResult = FindHashResultByPath(results, helloPath);
		NativeAssertTrue(abcResult != NULL, "The multi-file runtime path should preserve the first file result.");
		NativeAssertTrue(helloResult != NULL, "The multi-file runtime path should preserve the second file result.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("900150983CD24FB0D6963F7D28E17F72")), FindDigestValue(*abcResult, RESULT_DIGEST_MD5), "The first file MD5 digest did not match the known vector.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("5D41402ABC4B2A76B9719D911017C592")), FindDigestValue(*helloResult, RESULT_DIGEST_MD5), "The second file MD5 digest did not match the known vector.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD")), FindDigestValue(*abcResult, RESULT_DIGEST_SHA256), "The first file SHA256 digest did not match the known vector.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824")), FindDigestValue(*helloResult, RESULT_DIGEST_SHA256), "The second file SHA256 digest did not match the known vector.");

		NativeAssertEqual(static_cast<size_t>(2), progressSink.CountEvents(PROGRESS_EVENT_FILE_STARTED), "The multi-file runtime path should emit one file-started event per input file.");
		NativeAssertEqual(static_cast<size_t>(2), progressSink.CountEvents(PROGRESS_EVENT_FILE_META_READY), "The multi-file runtime path should emit one file-meta event per input file.");
		NativeAssertEqual(static_cast<size_t>(2), progressSink.CountEvents(PROGRESS_EVENT_FILE_HASH_READY), "The multi-file runtime path should emit one file-hash event per input file.");
		NativeAssertEqual(static_cast<size_t>(2), progressSink.CountEvents(PROGRESS_EVENT_FILE_CALCULATED), "The multi-file runtime path should emit one file-calculated event per input file.");
		NativeAssertEqual(static_cast<size_t>(2), progressSink.CountEvents(PROGRESS_EVENT_FILE_FINISHED), "The multi-file runtime path should emit one file-finished event per input file.");
		NativeAssertEqual(progressSink.progressMax(), progressSink.GetLastValue(PROGRESS_EVENT_TOTAL_PROGRESS), "The total progress should finish at progressMax for a completed multi-file run.");
		NativeAssertTrue(progressSink.GetFirstEventIndex(PROGRESS_EVENT_JOB_PREPARING) < progressSink.GetFirstEventIndex(PROGRESS_EVENT_JOB_PREPARATION_FINISHED), "Preparing should happen before preparation-finished.");
		NativeAssertTrue(progressSink.GetFirstEventIndex(PROGRESS_EVENT_JOB_PREPARATION_FINISHED) < progressSink.GetFirstEventIndex(PROGRESS_EVENT_FILE_STARTED), "Preparation should finish before file-started events.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_JOB_COMPLETED), "The multi-file runtime path should emit a completed event.");
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

		int exitCode = RunHashThreadData(threadData);
		NativeAssertEqual(0, exitCode, "HashThreadFunc should succeed for a SHA256-only request.");
		NativeAssertEqual(static_cast<uint64_t>(1), GetThreadDataResultCount(threadData), "A SHA256-only request should still produce one file result.");

		const HashResult& result = GetThreadDataResults(threadData).front();
		NativeAssertEqual(static_cast<size_t>(1), result.digests.size(), "Only the explicitly enabled algorithm should be emitted.");
		NativeAssertEqual(
			NormalizeHashAlgorithmId(GetHashAlgorithmId(RESULT_DIGEST_SHA256)),
			ResolveDigestResultAlgorithmId(result.digests[0]),
			"The only emitted digest should be SHA256.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD")), result.digests[0].value, "The SHA256-only digest value did not match the known vector.");
	}

	static void RunHashRequest_IgnoresUnknownAndDuplicateAlgorithmsInRequest()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("request-filter.txt"), "abc");

		CapturingProgressSink progressSink;
		HashJobState jobState;
		HashCancellationState cancellationState;
		HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);

		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(filePath);
		std::vector<ResultDigestType> algorithms;
		algorithms.push_back(RESULT_DIGEST_UNKNOWN);
		algorithms.push_back(RESULT_DIGEST_SHA256);
		algorithms.push_back(RESULT_DIGEST_SHA256);
		algorithms.push_back(static_cast<ResultDigestType>(9999));
		HashRequest request = CreateRequest(filePaths, algorithms);

		int exitCode = RunHashRequest(&executionContext, request);
		NativeAssertEqual(0, exitCode, "RunHashRequest should ignore unknown/duplicate algorithms and still succeed.");
		NativeAssertEqual(static_cast<size_t>(1), jobState.results.size(), "Unknown/duplicate algorithms should not prevent result publication.");

		const HashResult& result = jobState.results.front();
		NativeAssertEqual(static_cast<size_t>(1), result.digests.size(), "Unknown/duplicate algorithms should collapse to one registered SHA256 digest.");
		NativeAssertEqual(
			NormalizeHashAlgorithmId(GetHashAlgorithmId(RESULT_DIGEST_SHA256)),
			ResolveDigestResultAlgorithmId(result.digests[0]),
			"The emitted digest type should be SHA256 after request sanitization.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD")), result.digests[0].value, "The sanitized SHA256 digest value should match the known vector.");
		NativeAssertEqual(static_cast<size_t>(1), progressSink.CountEvents(PROGRESS_EVENT_FILE_HASH_READY), "Sanitized requests should still emit exactly one hash-ready event.");
	}

	static void RunHashRequest_DescriptorOnlyAlgorithmDoesNotBreakSupportedDigests()
	{
		ScopedHashAlgorithmRegistryReset scopedRegistryReset;
		NativeAssertTrue(RegisterHashAlgorithmDescriptor({
			"sha3-256",
			"SHA3-256",
			false
		}), "Descriptor/id-only algorithm registration should be allowed.");

		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("descriptor-only-safe.txt"), "abc");

		CapturingProgressSink progressSink;
		HashJobState jobState;
		HashCancellationState cancellationState;
		HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);

		HashRequest request;
		request.files.push_back(filePath);
		AppendHashRequestAlgorithm(request, RESULT_DIGEST_MD5);
		AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr(std::string("sha3-256")));

		int exitCode = RunHashRequest(&executionContext, request);
		NativeAssertEqual(0, exitCode, "RunHashRequest should keep supported digests operational when descriptor-only algorithms are present.");
		NativeAssertEqual(static_cast<size_t>(1), jobState.results.size(), "Descriptor/id-only algorithms should not block result publication.");

		const HashResult& result = jobState.results.front();
		NativeAssertEqual(RESULT_ALL, result.state, "Supported digest execution should still complete the file result.");
		NativeAssertEqual(static_cast<size_t>(1), result.digests.size(), "Descriptor/id-only algorithms without digest operations should be skipped.");
		NativeAssertEqual(
			NormalizeHashAlgorithmId(GetHashAlgorithmId(RESULT_DIGEST_MD5)),
			ResolveDigestResultAlgorithmId(result.digests[0]),
			"Supported MD5 digest should remain available.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("900150983CD24FB0D6963F7D28E17F72")), result.digests[0].value, "Supported MD5 digest should keep the expected known vector.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_FILE_HASH_READY), "Supported digest execution should still emit file-hash-ready events.");
	}

	static void ThreadDataExecutionAccess_IgnoresUnknownAlgorithmSelection()
	{
		ThreadData threadData;
		ResetThreadDataForNewSession(threadData);

		size_t initialEnabledCount = GetEnabledThreadDataHashAlgorithmCount(threadData);
		NativeAssertTrue(initialEnabledCount >= 1, "ThreadData should start with at least one enabled algorithm.");

		SetThreadDataHashAlgorithmEnabled(threadData, RESULT_DIGEST_UNKNOWN, false);
		NativeAssertEqual(initialEnabledCount, GetEnabledThreadDataHashAlgorithmCount(threadData), "Unknown digest toggles should not mutate enabled algorithm count.");
		NativeAssertTrue(!IsThreadDataHashAlgorithmEnabled(threadData, RESULT_DIGEST_UNKNOWN), "Unknown digest types should always be reported as disabled.");
	}

	static void HashAlgorithmRegistry_SupportsDescriptorIdRegistrationAndReset()
	{
		ScopedHashAlgorithmRegistryReset scopedRegistryReset;
		int baselineCount = GetRegisteredHashAlgorithmCount();

		NativeAssertTrue(RegisterHashAlgorithmDescriptor({
			"blake3",
			"BLAKE3",
			false
		}), "RegisterHashAlgorithmDescriptor should accept descriptor/id-based custom algorithm registration.");

		NativeAssertEqual(baselineCount + 1, GetRegisteredHashAlgorithmCount(), "Descriptor/id-based custom algorithm registration should increase the registry count.");
		NativeAssertTrue(IsRegisteredHashAlgorithmId(sunjwbase::strtotstr(std::string("BLAKE3"))), "Algorithm id lookups should be case-insensitive.");

		const HashAlgorithmDescriptor *registeredDescriptor = NULL;
		NativeAssertTrue(TryGetHashAlgorithmDescriptorById(sunjwbase::strtotstr(std::string("blake3")), &registeredDescriptor), "Descriptor/id-based lookup should resolve custom algorithms.");
		NativeAssertTrue(registeredDescriptor != NULL, "Descriptor/id lookup should expose the registered descriptor.");
		NativeAssertEqual(
			NormalizeHashAlgorithmId(sunjwbase::strtotstr(std::string("blake3"))),
			GetHashAlgorithmDescriptorId(*registeredDescriptor),
			"Descriptor/id lookup should preserve custom algorithm identities.");
		ResultDigestType digestType = RESULT_DIGEST_UNKNOWN;
		NativeAssertTrue(!TryGetHashAlgorithmTypeById(sunjwbase::strtotstr(std::string("blake3")), &digestType), "Descriptor/id-only custom algorithms should not require legacy digest-type mappings.");
	}

	static void HashRequest_AlgorithmIdsDriveSelectionAndDeduplication()
	{
		HashRequest request;
		AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr(std::string("SHA512")));
		AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr(std::string("sha512")));
		AppendHashRequestAlgorithm(request, RESULT_DIGEST_MD5);
		AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr(std::string("unknown")));

		std::vector<HashAlgorithmId> algorithmIds;
		VisitHashRequestAlgorithmIds(request, [&](const HashAlgorithmId& algorithmId)
		{
			algorithmIds.push_back(algorithmId);
			return true;
		});

		std::vector<ResultDigestType> digestTypes;
		VisitHashRequestAlgorithms(request, [&](ResultDigestType digestType)
		{
			digestTypes.push_back(digestType);
			return true;
		});

		NativeAssertEqual(static_cast<size_t>(2), algorithmIds.size(), "HashRequest should deduplicate descriptor/id algorithms while keeping compatibility algorithms.");
		NativeAssertTrue(HasHashRequestAlgorithmId(request, sunjwbase::strtotstr(std::string("sha512"))), "HashRequest should report selected algorithms by descriptor/id.");
		NativeAssertTrue(HasHashRequestAlgorithm(request, RESULT_DIGEST_MD5), "HashRequest should preserve digest-type compatibility selection.");
		NativeAssertEqual(static_cast<size_t>(2), digestTypes.size(), "HashRequest digest iteration should resolve deduplicated descriptor/id selections.");
	}

	static void HashRequest_SelectionStateResolvesByAlgorithmIdForUnknownDigestTypes()
	{
		ScopedHashAlgorithmRegistryReset scopedRegistryReset;
		const int baselineCount = GetRegisteredHashAlgorithmCount();

		NativeAssertTrue(RegisterHashAlgorithmDescriptor({
			"sha3-256",
			"SHA3-256",
			false
		}), "Descriptor/id registration should allow extending unknown digest identities.");
		NativeAssertTrue(RegisterHashAlgorithmDescriptor({
			"blake3",
			"BLAKE3",
			false
		}), "Descriptor/id registration should allow multiple unknown digest identities.");
		NativeAssertEqual(baselineCount + 2, GetRegisteredHashAlgorithmCount(), "Unknown digest registrations with different ids should not collapse into one slot.");

		HashRequest request;
		AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr(std::string("sha3-256")));
		AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr(std::string("blake3")));

		HashAlgorithmSelectionState selectionState = CreateHashRequestAlgorithmSelectionState(request);

		int sha3Index = -1;
		int blake3Index = -1;
		NativeAssertTrue(TryGetHashAlgorithmIndexById(sunjwbase::strtotstr(std::string("sha3-256")), &sha3Index), "Descriptor/id index lookup should resolve SHA3-256.");
		NativeAssertTrue(TryGetHashAlgorithmIndexById(sunjwbase::strtotstr(std::string("blake3")), &blake3Index), "Descriptor/id index lookup should resolve BLAKE3.");
		NativeAssertTrue(sha3Index != blake3Index, "Descriptor/id index lookup should keep unknown digest algorithms distinct.");
		NativeAssertTrue(static_cast<size_t>(sha3Index) < selectionState.enabled.size(), "Selection state should include SHA3-256 index.");
		NativeAssertTrue(static_cast<size_t>(blake3Index) < selectionState.enabled.size(), "Selection state should include BLAKE3 index.");
		NativeAssertTrue(selectionState.enabled[static_cast<size_t>(sha3Index)], "Selection state should enable SHA3-256 by descriptor/id.");
		NativeAssertTrue(selectionState.enabled[static_cast<size_t>(blake3Index)], "Selection state should enable BLAKE3 by descriptor/id.");
	}

	static void HashResult_ProjectsRegistryExtendedDigestValuesWithoutFixedSlots()
	{
		ScopedHashAlgorithmRegistryReset scopedRegistryReset;
		NativeAssertTrue(RegisterHashAlgorithmDescriptor({
			"xxh3",
			"XXH3",
			false
		}), "Custom descriptor/id registration should allow result projection coverage for non-fixed digests.");

		ResultData resultData;
		ResetResultData(resultData);
		SetResultState(resultData, RESULT_ALL);
		SetResultPath(resultData, sunjwbase::strtotstr(std::string("custom-id.bin")));
		SetResultDigestById(resultData, NormalizeHashAlgorithmId(sunjwbase::strtotstr(std::string("xxh3"))), sunjwbase::strtotstr(std::string("CAFEBABE")));

		HashResult result = ProjectHashResult(resultData);
		NativeAssertEqual(static_cast<size_t>(1), result.digests.size(), "HashResult projection should emit descriptor/id-based digest results without fixed digest slots.");
		NativeAssertEqual(
			NormalizeHashAlgorithmId(sunjwbase::strtotstr(std::string("xxh3"))),
			ResolveDigestResultAlgorithmId(result.digests[0]),
			"Projected digest should preserve custom descriptor digest identity.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("xxh3")), result.digests[0].stableName, "Projected digest should preserve descriptor/id stable names.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("CAFEBABE")), result.digests[0].value, "Projected digest should preserve descriptor/id digest values.");
	}

	static void HashDigestOperationRegistry_StaysConsistentWithAlgorithmRegistry()
	{
		NativeAssertTrue(HashEngineInternal::IsHashDigestOperationRegistryConsistent(), "Digest operation registry should cover every registered algorithm with complete operations.");

		VisitRegisteredHashAlgorithms([&](int index, const HashAlgorithmDescriptor& descriptor)
		{
			(void)index;
			NativeAssertTrue(
				HashEngineInternal::IsHashDigestOperationDescriptorSupportedById(GetHashAlgorithmDescriptorId(descriptor)),
				"Registered algorithms should resolve to supported digest operation descriptors.");
			return true;
		});
	}

	static void HashDigestOperationRegistry_BuildsDescriptorSnapshotFromAlgorithmRegistry()
	{
		int descriptorCount = 0;
		const HashEngineInternal::HashDigestOperationDescriptor *operationDescriptors = HashEngineInternal::GetHashDigestOperationDescriptors(&descriptorCount);
		NativeAssertTrue(operationDescriptors != NULL, "Digest operation registry should publish descriptors for registered algorithms.");
		NativeAssertEqual(GetRegisteredHashAlgorithmCount(), descriptorCount, "Digest operation descriptor count should match registered algorithm count.");

		for (int descriptorIndex = 0; descriptorIndex < descriptorCount; ++descriptorIndex)
		{
			const HashAlgorithmDescriptor& algorithmDescriptor = GetHashAlgorithmDescriptorAt(descriptorIndex);
			NativeAssertEqual(
				GetHashAlgorithmDescriptorId(algorithmDescriptor),
				NormalizeHashAlgorithmId(operationDescriptors[descriptorIndex].algorithmId),
				"Digest operation descriptors should preserve registry ordering.");
			NativeAssertTrue(HashEngineInternal::IsHashDigestOperationDescriptorComplete(operationDescriptors[descriptorIndex]), "Digest operation descriptors published from the registry should always be complete.");
		}
	}

	static void HashDigestOperationRegistry_AllowsNullDescriptorProbeForKnownDigests()
	{
		NativeAssertTrue(HashEngineInternal::TryGetHashDigestOperationDescriptor(RESULT_DIGEST_MD5, NULL), "Known digests should support existence probes without an output descriptor.");
		NativeAssertTrue(HashEngineInternal::TryGetHashDigestOperationDescriptor(RESULT_DIGEST_SHA256, NULL), "Known digests should support existence probes without an output descriptor.");
		NativeAssertTrue(HashEngineInternal::TryGetHashDigestOperationDescriptorById(GetHashAlgorithmId(RESULT_DIGEST_MD5), NULL), "Known descriptor ids should support existence probes without an output descriptor.");
		NativeAssertTrue(HashEngineInternal::TryGetHashDigestOperationDescriptorById(GetHashAlgorithmId(RESULT_DIGEST_SHA256), NULL), "Known descriptor ids should support existence probes without an output descriptor.");
		NativeAssertTrue(!HashEngineInternal::TryGetHashDigestOperationDescriptor(RESULT_DIGEST_UNKNOWN, NULL), "Unknown digest probes should fail.");
		NativeAssertTrue(!HashEngineInternal::TryGetHashDigestOperationDescriptor(static_cast<ResultDigestType>(9999), NULL), "Out-of-range digest probes should fail.");
		NativeAssertTrue(!HashEngineInternal::TryGetHashDigestOperationDescriptorById(sunjwbase::strtotstr(std::string("unknown")), NULL), "Unknown descriptor-id probes should fail.");
	}

	static void HashDigestOperationRegistry_ValidatesDescriptorCompletenessAndUnknownSupport()
	{
		HashEngineInternal::HashDigestOperationDescriptor descriptor = {};
		NativeAssertTrue(HashEngineInternal::TryGetHashDigestOperationDescriptor(RESULT_DIGEST_SHA512, &descriptor), "Known SHA512 descriptors should resolve.");
		NativeAssertTrue(HashEngineInternal::IsHashDigestOperationDescriptorComplete(descriptor), "Resolved registry descriptors should be complete.");
		NativeAssertTrue(HashEngineInternal::IsHashDigestOperationDescriptorSupported(RESULT_DIGEST_SHA512), "Known SHA512 descriptors should be reported as supported.");

		HashEngineInternal::HashDigestOperationDescriptor incompleteDescriptor = descriptor;
		incompleteDescriptor.updateAction = NULL;
		NativeAssertTrue(!HashEngineInternal::IsHashDigestOperationDescriptorComplete(incompleteDescriptor), "Descriptors missing update actions should be rejected as incomplete.");
		NativeAssertTrue(!HashEngineInternal::IsHashDigestOperationDescriptorSupported(RESULT_DIGEST_UNKNOWN), "Unknown digest types should not be reported as supported.");
		NativeAssertTrue(HashEngineInternal::IsHashDigestOperationDescriptorSupportedById(GetHashAlgorithmId(RESULT_DIGEST_SHA512)), "Known descriptor ids should be reported as supported.");
		NativeAssertTrue(!HashEngineInternal::IsHashDigestOperationDescriptorSupportedById(sunjwbase::strtotstr(std::string("unknown"))), "Unknown descriptor ids should not be reported as supported.");
	}

	static void HashDigestUpdater_CreatesRegistryOrderedOperationsForSelectedAlgorithms()
	{
		HashRequest request;
		AppendHashRequestAlgorithm(request, RESULT_DIGEST_SHA512);
		AppendHashRequestAlgorithm(request, RESULT_DIGEST_MD5);
		AppendHashRequestAlgorithm(request, RESULT_DIGEST_SHA256);
		AppendHashRequestAlgorithm(request, RESULT_DIGEST_SHA256);
		AppendHashRequestAlgorithm(request, RESULT_DIGEST_UNKNOWN);

		HashEngineInternal::DigestUpdateRequest digestUpdateRequest = HashEngineInternal::CreateDigestUpdateRequest(request);
		NativeAssertEqual(static_cast<size_t>(3), digestUpdateRequest.operationDescriptors.size(), "Digest update planning should keep only requested registered algorithms without duplicates.");
		NativeAssertEqual(GetHashAlgorithmId(RESULT_DIGEST_MD5), NormalizeHashAlgorithmId(digestUpdateRequest.operationDescriptors[0].algorithmId), "Digest update planning should preserve registry order for MD5.");
		NativeAssertEqual(GetHashAlgorithmId(RESULT_DIGEST_SHA256), NormalizeHashAlgorithmId(digestUpdateRequest.operationDescriptors[1].algorithmId), "Digest update planning should preserve registry order for SHA256.");
		NativeAssertEqual(GetHashAlgorithmId(RESULT_DIGEST_SHA512), NormalizeHashAlgorithmId(digestUpdateRequest.operationDescriptors[2].algorithmId), "Digest update planning should preserve registry order for SHA512.");
	}

	static void HashDigestUpdater_IgnoresDescriptorOnlyAlgorithmsWithoutBreakingConsistency()
	{
		ScopedHashAlgorithmRegistryReset scopedRegistryReset;
		NativeAssertTrue(RegisterHashAlgorithmDescriptor({
			"sha3-256",
			"SHA3-256",
			false
		}), "Descriptor/id-only algorithms should be registerable before digest operations are implemented.");

		NativeAssertTrue(HashEngineInternal::IsHashDigestOperationRegistryConsistent(), "Descriptor/id-only algorithm registrations should not invalidate digest operation consistency.");

		HashRequest request;
		AppendHashRequestAlgorithm(request, RESULT_DIGEST_MD5);
		AppendHashRequestAlgorithmId(request, sunjwbase::strtotstr(std::string("sha3-256")));

		HashEngineInternal::DigestUpdateRequest digestUpdateRequest = HashEngineInternal::CreateDigestUpdateRequest(request);
		NativeAssertEqual(static_cast<size_t>(1), digestUpdateRequest.operationDescriptors.size(), "Descriptor/id-only algorithms without backend operations should be skipped instead of disabling digest planning.");
		NativeAssertEqual(GetHashAlgorithmId(RESULT_DIGEST_MD5), NormalizeHashAlgorithmId(digestUpdateRequest.operationDescriptors[0].algorithmId), "Supported digest planning should remain intact when descriptor-only algorithms are present.");
	}

	static void HashThreadFunc_AllowsMetadataOnlyRequestsWithoutEnabledAlgorithms()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("meta-only.txt"), "abc");

		CapturingProgressSink progressSink;
		progressSink.EnableConsoleTrace("multi-file");
		ThreadData threadData;
		std::vector<ResultDigestType> algorithms;
		ConfigureThreadData(threadData, progressSink, filePath, algorithms);

		int exitCode = RunHashThreadData(threadData);
		NativeAssertEqual(0, exitCode, "HashThreadFunc should succeed even when no digest algorithms are enabled.");
		NativeAssertEqual(static_cast<uint64_t>(1), GetThreadDataResultCount(threadData), "A metadata-only request should still produce one file result.");

		const HashResult& result = GetThreadDataResults(threadData).front();
		NativeAssertEqual(RESULT_META, result.state, "Without enabled algorithms the final result should remain metadata-only.");
		NativeAssertEqual(static_cast<size_t>(0), result.digests.size(), "Without enabled algorithms no digest values should be emitted.");
		NativeAssertEqual(static_cast<uint64_t>(3), result.meta.size, "Metadata-only hashing should still report the correct file size.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_FILE_META_READY), "Metadata-only hashing should emit file-metadata events.");
		NativeAssertEqual(static_cast<size_t>(0), progressSink.CountEvents(PROGRESS_EVENT_FILE_HASH_READY), "Metadata-only hashing should not emit file-hash events.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_JOB_COMPLETED), "Metadata-only hashing should still complete.");
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

		int exitCode = RunHashThreadData(threadData);
		NativeAssertEqual(0, exitCode, "HashThreadFunc should succeed for a searchable digest result.");

		const HashResultList& results = GetThreadDataResults(threadData);
		sunjwbase::tstring digestQuery = NormalizeHashResultDigestSearchText(sunjwbase::strtotstr(std::string(" 90015098 ")));
		sunjwbase::tstring missingQuery = NormalizeHashResultDigestSearchText(sunjwbase::strtotstr(std::string(" no-match ")));

		NativeAssertEqual(static_cast<size_t>(1), CountDigestMatchingHashResults(results, digestQuery), "The runtime digest search should find the emitted MD5 prefix.");
		NativeAssertEqual(static_cast<size_t>(0), CountDigestMatchingHashResults(results, missingQuery), "The runtime digest search should reject non-matching digests.");
	}

	static void HashResultSearch_MatchesPathAndDigestForRuntimeResults()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring alphaPath = tempDirectory.WriteTextFile(_T("alpha.txt"), "abc");
		sunjwbase::tstring betaPath = tempDirectory.WriteTextFile(_T("beta.txt"), "hello");

		CapturingProgressSink progressSink;
		ThreadData threadData;
		std::vector<ResultDigestType> algorithms;
		algorithms.push_back(RESULT_DIGEST_MD5);
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(alphaPath);
		filePaths.push_back(betaPath);
		ConfigureThreadDataFiles(threadData, progressSink, filePaths, algorithms);

		int exitCode = RunHashThreadData(threadData);
		NativeAssertEqual(0, exitCode, "HashThreadFunc should succeed for path+digest runtime search coverage.");

		std::vector<sunjwbase::tstring> matchedPaths;
		size_t matchCount = VisitPathAndDigestMatchingHashResults(GetThreadDataResults(threadData),
			NormalizeHashResultPathSearchText(_T("alpha")),
			NormalizeHashResultDigestSearchText(sunjwbase::strtotstr(std::string("90015098"))),
			[&](const HashResult& result)
		{
			matchedPaths.push_back(result.path);
		});

		NativeAssertEqual(static_cast<size_t>(1), matchCount, "The runtime path+digest search should match exactly one result.");
		NativeAssertEqual(static_cast<size_t>(1), matchedPaths.size(), "The runtime path+digest visitor should receive exactly one result.");
		NativeAssertEqual(alphaPath, matchedPaths[0], "The runtime path+digest match should select the alpha test file.");
		NativeAssertEqual(static_cast<size_t>(0), VisitPathAndDigestMatchingHashResults(GetThreadDataResults(threadData),
			NormalizeHashResultPathSearchText(_T("alpha")),
			NormalizeHashResultDigestSearchText(sunjwbase::strtotstr(std::string("5D4140"))),
			[&](const HashResult& result)
		{
			(void)result;
		}), "The runtime path+digest search should reject digest text that belongs to another file.");
	}

	static void HashThreadFunc_ComputesExpectedDigestsForEmptyFile()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("empty.txt"), "");

		CapturingProgressSink progressSink;
		ThreadData threadData;
		std::vector<ResultDigestType> algorithms;
		algorithms.push_back(RESULT_DIGEST_MD5);
		algorithms.push_back(RESULT_DIGEST_SHA1);
		algorithms.push_back(RESULT_DIGEST_SHA256);
		algorithms.push_back(RESULT_DIGEST_SHA512);
		ConfigureThreadData(threadData, progressSink, filePath, algorithms);

		int exitCode = RunHashThreadData(threadData);
		NativeAssertEqual(0, exitCode, "HashThreadFunc should succeed for an empty file.");

		const HashResult& result = GetThreadDataResults(threadData).front();
		NativeAssertEqual(static_cast<uint64_t>(0), result.meta.size, "The empty-file runtime path should report zero bytes.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("D41D8CD98F00B204E9800998ECF8427E")), FindDigestValue(result, RESULT_DIGEST_MD5), "The empty-file MD5 digest did not match the known vector.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("DA39A3EE5E6B4B0D3255BFEF95601890AFD80709")), FindDigestValue(result, RESULT_DIGEST_SHA1), "The empty-file SHA1 digest did not match the known vector.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855")), FindDigestValue(result, RESULT_DIGEST_SHA256), "The empty-file SHA256 digest did not match the known vector.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("CF83E1357EEFB8BDF1542850D66D8007D620E4050B5715DC83F4A921D36CE9CE47D0D13C5D85F2B0FF8318D2877EEC2F63B931BD47417A81A538327AF927DA3E")), FindDigestValue(result, RESULT_DIGEST_SHA512), "The empty-file SHA512 digest did not match the known vector.");
		NativeAssertEqual(progressSink.progressMax(), progressSink.GetLastValue(PROGRESS_EVENT_FILE_PROGRESS), "The empty-file runtime path should still complete file progress.");
	}

	static void RunHashRequest_ReportsMissingFileAsErrorResult()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring missingPath = tempDirectory.BuildPath(_T("missing.txt"));

		CapturingProgressSink progressSink;
		HashJobState jobState;
		HashCancellationState cancellationState;

			HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);

		HashRequest request;
		request.files.push_back(missingPath);
		AppendHashRequestAlgorithm(request, RESULT_DIGEST_MD5);

		int exitCode = RunHashRequest(&executionContext, request);
		NativeAssertEqual(0, exitCode, "RunHashRequest should complete even when a file cannot be opened.");
		NativeAssertTrue(!jobState.working.load(), "RunHashRequest should clear the working flag after a missing-file attempt.");
		NativeAssertEqual(static_cast<size_t>(1), jobState.results.size(), "A missing file should still produce one error result.");

		const HashResult& result = jobState.results.front();
		NativeAssertEqual(RESULT_ERROR, result.state, "A missing file should produce RESULT_ERROR.");
		NativeAssertEqual(missingPath, result.path, "The error result should preserve the requested file path.");
		NativeAssertNotEmpty(result.error, "The missing-file result should expose an error message.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_FILE_FAILED), "A missing file should emit a file-failed event.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_JOB_COMPLETED), "A missing file should still emit a completed event.");
	}

	static void RunHashRequest_ContinuesAfterOpenFileErrorInBatch()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring existingPath = tempDirectory.WriteTextFile(_T("existing.txt"), "abc");
		sunjwbase::tstring missingPath = tempDirectory.BuildPath(_T("missing.txt"));

		CapturingProgressSink progressSink;
		HashJobState jobState;
		HashCancellationState cancellationState;

		HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(missingPath);
		filePaths.push_back(existingPath);
		std::vector<ResultDigestType> algorithms;
		algorithms.push_back(RESULT_DIGEST_MD5);
		HashRequest request = CreateRequest(filePaths, algorithms);

		int exitCode = RunHashRequest(&executionContext, request);
		NativeAssertEqual(0, exitCode, "RunHashRequest should continue past an open-file error and complete the batch.");
		NativeAssertEqual(static_cast<size_t>(2), jobState.results.size(), "A mixed missing+existing batch should still append two file results.");
		const HashResult *missingResult = FindHashResultByPath(jobState.results, missingPath);
		const HashResult *existingResult = FindHashResultByPath(jobState.results, existingPath);
		NativeAssertTrue(missingResult != NULL, "The mixed batch should preserve the missing-file error result.");
		NativeAssertTrue(existingResult != NULL, "The mixed batch should preserve the later successful file result.");
		NativeAssertEqual(RESULT_ERROR, missingResult->state, "The missing file should remain RESULT_ERROR inside a mixed batch.");
		NativeAssertEqual(RESULT_ALL, existingResult->state, "The existing file should still complete successfully inside a mixed batch.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("900150983CD24FB0D6963F7D28E17F72")), FindDigestValue(*existingResult, RESULT_DIGEST_MD5), "The successful file in the mixed batch did not produce the expected MD5 digest.");
		NativeAssertEqual(static_cast<size_t>(1), progressSink.CountEvents(PROGRESS_EVENT_FILE_FAILED), "The mixed batch should emit exactly one file-failed event.");
		NativeAssertEqual(static_cast<size_t>(1), progressSink.CountEvents(PROGRESS_EVENT_FILE_HASH_READY), "The mixed batch should emit exactly one file-hash event.");
		NativeAssertEqual(static_cast<size_t>(2), progressSink.CountEvents(PROGRESS_EVENT_FILE_FINISHED), "The mixed batch should mark both file attempts as finished.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_JOB_COMPLETED), "The mixed batch should still emit a completed event.");
	}

	static void RunHashRequest_CancelsWhenStopRequestedBeforeStart()
	{
		CapturingProgressSink progressSink;
		HashJobState jobState;
		HashCancellationState cancellationState;
		cancellationState.stopRequested.store(true);

			HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);

		HashRequest request;
		request.files.push_back(sunjwbase::strtotstr(std::string("should-not-run.txt")));
		AppendHashRequestAlgorithm(request, RESULT_DIGEST_MD5);

		int exitCode = RunHashRequest(&executionContext, request);
		NativeAssertEqual(0, exitCode, "RunHashRequest should return 0 for a cooperative cancellation.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_JOB_CANCELLED), "A pre-stopped execution context should emit a cancelled event.");
		NativeAssertTrue(!jobState.working.load(), "A cancelled execution should not leave the working flag enabled.");
		NativeAssertEqual(static_cast<size_t>(0), jobState.results.size(), "A cancelled execution should not append any file results.");
	}

	static void RunHashRequest_PropagatesUppercasePreferenceInHashReadyEvent()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("uppercase.txt"), "abc");

		CapturingProgressSink progressSink;
		HashJobState jobState;
		HashCancellationState cancellationState;

		HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(filePath);
		std::vector<ResultDigestType> algorithms;
		algorithms.push_back(RESULT_DIGEST_MD5);
		HashRequest request = CreateRequest(filePaths, algorithms, true);

		int exitCode = RunHashRequest(&executionContext, request);
		NativeAssertEqual(0, exitCode, "RunHashRequest should succeed for an uppercase-digest request.");

		ProgressEvent hashReadyEvent;
		NativeAssertTrue(progressSink.TryGetFirstEvent(PROGRESS_EVENT_FILE_HASH_READY, &hashReadyEvent), "The uppercase-digest runtime path should emit a file-hash-ready event.");
		NativeAssertTrue(hashReadyEvent.uppercaseDigest, "The file-hash-ready event should preserve the uppercaseDigest request flag.");
		NativeAssertEqual(sunjwbase::strtotstr(std::string("900150983CD24FB0D6963F7D28E17F72")), FindDigestValue(hashReadyEvent.result, RESULT_DIGEST_MD5), "The uppercase-digest event result should carry the expected MD5 value.");
	}

	static void RunHashRequest_CancelsDuringFileProgressAndSkipsRemainingFiles()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring largeFilePath = tempDirectory.WriteTextFile(_T("large.bin"), std::string((kHashEngineBufferSize * 3) + 17, 'A'));
		sunjwbase::tstring secondFilePath = tempDirectory.WriteTextFile(_T("second.txt"), "second");

		CapturingProgressSink progressSink;
		HashJobState jobState;
		HashCancellationState cancellationState;

		progressSink.ConfigureStopOnEvent(&cancellationState.stopRequested, PROGRESS_EVENT_FILE_PROGRESS, 1);

		HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(largeFilePath);
		filePaths.push_back(secondFilePath);
		std::vector<ResultDigestType> algorithms;
		algorithms.push_back(RESULT_DIGEST_MD5);
		HashRequest request = CreateRequest(filePaths, algorithms);

		int exitCode = RunHashRequest(&executionContext, request);
		NativeAssertEqual(0, exitCode, "RunHashRequest should cooperatively cancel during file progress.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_JOB_CANCELLED), "A mid-run cancellation should emit a cancelled event.");
		NativeAssertTrue(!progressSink.HasEvent(PROGRESS_EVENT_JOB_COMPLETED), "A mid-run cancellation should not emit a completed event.");
		NativeAssertEqual(static_cast<size_t>(1), progressSink.CountEvents(PROGRESS_EVENT_FILE_STARTED), "Cancellation during the first file should prevent later files from starting.");
		NativeAssertEqual(static_cast<size_t>(0), progressSink.CountEvents(PROGRESS_EVENT_FILE_FINISHED), "Cancellation during file progress should stop before file-finished is emitted.");
		NativeAssertTrue(FindHashResultByPath(jobState.results, secondFilePath) == NULL, "Cancellation during the first file should prevent later file results from being appended.");
		NativeAssertTrue(!jobState.working.load(), "A mid-run cancellation should clear the working flag.");
	}
}

void RegisterHashEngineRuntimeTests(std::vector<NativeTestCase>& tests)
{
	tests.push_back({ "HashThreadFunc_ComputesExpectedDigestsForSingleFile", &HashThreadFunc_ComputesExpectedDigestsForSingleFile });
	tests.push_back({ "HashThreadFunc_ProcessesMultipleFilesAndWholeProgress", &HashThreadFunc_ProcessesMultipleFilesAndWholeProgress });
	tests.push_back({ "HashThreadFunc_RespectsSelectedAlgorithms", &HashThreadFunc_RespectsSelectedAlgorithms });
	tests.push_back({ "RunHashRequest_IgnoresUnknownAndDuplicateAlgorithmsInRequest", &RunHashRequest_IgnoresUnknownAndDuplicateAlgorithmsInRequest });
	tests.push_back({ "RunHashRequest_DescriptorOnlyAlgorithmDoesNotBreakSupportedDigests", &RunHashRequest_DescriptorOnlyAlgorithmDoesNotBreakSupportedDigests });
	tests.push_back({ "ThreadDataExecutionAccess_IgnoresUnknownAlgorithmSelection", &ThreadDataExecutionAccess_IgnoresUnknownAlgorithmSelection });
	tests.push_back({ "HashAlgorithmRegistry_SupportsDescriptorIdRegistrationAndReset", &HashAlgorithmRegistry_SupportsDescriptorIdRegistrationAndReset });
	tests.push_back({ "HashRequest_AlgorithmIdsDriveSelectionAndDeduplication", &HashRequest_AlgorithmIdsDriveSelectionAndDeduplication });
	tests.push_back({ "HashRequest_SelectionStateResolvesByAlgorithmIdForUnknownDigestTypes", &HashRequest_SelectionStateResolvesByAlgorithmIdForUnknownDigestTypes });
	tests.push_back({ "HashResult_ProjectsRegistryExtendedDigestValuesWithoutFixedSlots", &HashResult_ProjectsRegistryExtendedDigestValuesWithoutFixedSlots });
	tests.push_back({ "HashDigestOperationRegistry_StaysConsistentWithAlgorithmRegistry", &HashDigestOperationRegistry_StaysConsistentWithAlgorithmRegistry });
	tests.push_back({ "HashDigestOperationRegistry_BuildsDescriptorSnapshotFromAlgorithmRegistry", &HashDigestOperationRegistry_BuildsDescriptorSnapshotFromAlgorithmRegistry });
	tests.push_back({ "HashDigestOperationRegistry_AllowsNullDescriptorProbeForKnownDigests", &HashDigestOperationRegistry_AllowsNullDescriptorProbeForKnownDigests });
	tests.push_back({ "HashDigestOperationRegistry_ValidatesDescriptorCompletenessAndUnknownSupport", &HashDigestOperationRegistry_ValidatesDescriptorCompletenessAndUnknownSupport });
	tests.push_back({ "HashDigestUpdater_CreatesRegistryOrderedOperationsForSelectedAlgorithms", &HashDigestUpdater_CreatesRegistryOrderedOperationsForSelectedAlgorithms });
	tests.push_back({ "HashDigestUpdater_IgnoresDescriptorOnlyAlgorithmsWithoutBreakingConsistency", &HashDigestUpdater_IgnoresDescriptorOnlyAlgorithmsWithoutBreakingConsistency });
	tests.push_back({ "HashThreadFunc_AllowsMetadataOnlyRequestsWithoutEnabledAlgorithms", &HashThreadFunc_AllowsMetadataOnlyRequestsWithoutEnabledAlgorithms });
	tests.push_back({ "HashResultSearch_FindsMatchingRuntimeDigests", &HashResultSearch_FindsMatchingRuntimeDigests });
	tests.push_back({ "HashResultSearch_MatchesPathAndDigestForRuntimeResults", &HashResultSearch_MatchesPathAndDigestForRuntimeResults });
	tests.push_back({ "HashThreadFunc_ComputesExpectedDigestsForEmptyFile", &HashThreadFunc_ComputesExpectedDigestsForEmptyFile });
	tests.push_back({ "RunHashRequest_ReportsMissingFileAsErrorResult", &RunHashRequest_ReportsMissingFileAsErrorResult });
	tests.push_back({ "RunHashRequest_ContinuesAfterOpenFileErrorInBatch", &RunHashRequest_ContinuesAfterOpenFileErrorInBatch });
	tests.push_back({ "RunHashRequest_CancelsWhenStopRequestedBeforeStart", &RunHashRequest_CancelsWhenStopRequestedBeforeStart });
	tests.push_back({ "RunHashRequest_PropagatesUppercasePreferenceInHashReadyEvent", &RunHashRequest_PropagatesUppercasePreferenceInHashReadyEvent });
	tests.push_back({ "RunHashRequest_CancelsDuringFileProgressAndSkipsRemainingFiles", &RunHashRequest_CancelsDuringFileProgressAndSkipsRemainingFiles });
}
