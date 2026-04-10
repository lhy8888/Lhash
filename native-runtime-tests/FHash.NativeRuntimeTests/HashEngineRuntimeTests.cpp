#include "..\..\trunk\source\stdafx.h"

#include <future>
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
#include "WinCommon/WinHandleGuard.h"

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
			WinHandleGuard::UniqueWinHandle fileHandle(::CreateFile(filePath.c_str(), GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL));
			NativeAssertTrue(fileHandle.isValid(), "Unable to create the native runtime test input file.");

			DWORD bytesWritten = 0;
			BOOL writeSucceeded = ::WriteFile(fileHandle.get(), contents.data(), static_cast<DWORD>(contents.size()), &bytesWritten, NULL);

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

	static HashAlgorithmId CreateAlgorithmId(const char *stableName)
	{
		return NormalizeHashAlgorithmId(sunjwbase::strtotstr(std::string(stableName)));
	}

	static std::vector<HashAlgorithmId> CreateBlake3VariantAlgorithmIds()
	{
		std::vector<HashAlgorithmId> algorithmIds;
		algorithmIds.push_back(CreateAlgorithmId("blake3-256"));
		algorithmIds.push_back(CreateAlgorithmId("blake3-512"));
		algorithmIds.push_back(CreateAlgorithmId("blake3-xof"));
		return algorithmIds;
	}

	static std::vector<HashAlgorithmId> CreateXXH3VariantAlgorithmIds()
	{
		std::vector<HashAlgorithmId> algorithmIds;
		algorithmIds.push_back(CreateAlgorithmId("xxh3-64"));
		algorithmIds.push_back(CreateAlgorithmId("xxh3-128"));
		return algorithmIds;
	}

	static HashAlgorithmId GetCRC32CAlgorithmId()
	{
		return CreateAlgorithmId("crc32c");
	}

	static sunjwbase::tstring GetOfficialBlake3_256Vector()
	{
		return sunjwbase::strtotstr(std::string("E1BE4D7A8AB5560AA4199EEA339849BA8E293D55CA0A81006726D184519E647F"));
	}

	static sunjwbase::tstring GetOfficialBlake3_512Vector()
	{
		return sunjwbase::strtotstr(std::string("E1BE4D7A8AB5560AA4199EEA339849BA8E293D55CA0A81006726D184519E647F5B49B82F805A538C68915C1AE8035C900FD1D4B13902920FD05E1450822F36DE"));
	}

	static sunjwbase::tstring GetOfficialBlake3Xof128Vector()
	{
		return sunjwbase::strtotstr(std::string("E1BE4D7A8AB5560AA4199EEA339849BA8E293D55CA0A81006726D184519E647F5B49B82F805A538C68915C1AE8035C900FD1D4B13902920FD05E1450822F36DE9454B7E9996DE4900C8E723512883F93F4345F8A58BFE64EE38D3AD71AB027765D25CDD0E448328A8E7A683B9A6AF8B0AF94FA09010D9186890B096A08471E42"));
	}

	static std::string CreateOfficialXXH3SanityInput(size_t inputLength)
	{
		static const uint32_t kPrime32 = 2654435761U;
		static const uint64_t kPrime64 = 11400714785074694797ULL;

		std::string input(inputLength, '\0');
		uint64_t byteGenerator = static_cast<uint64_t>(kPrime32);
		for (size_t index = 0; index < input.size(); ++index)
		{
			input[index] = static_cast<char>(byteGenerator >> 56);
			byteGenerator *= kPrime64;
		}

		return input;
	}

	static std::string CreateAscendingByteInput(size_t inputLength)
	{
		std::string input(inputLength, '\0');
		for (size_t index = 0; index < input.size(); ++index)
		{
			input[index] = static_cast<char>(index & 0xFF);
		}

		return input;
	}

	static std::string CreateFilledByteInput(size_t inputLength, unsigned char fillByte)
	{
		return std::string(inputLength, static_cast<char>(fillByte));
	}

	static std::string CreateDescendingByteInput(size_t inputLength)
	{
		std::string input(inputLength, '\0');
		for (size_t index = 0; index < input.size(); ++index)
		{
			input[index] = static_cast<char>((input.size() - 1 - index) & 0xFF);
		}

		return input;
	}

	static std::string CreateOfficialCRC32CIscsiInput()
	{
		static const unsigned char kBytes[] = {
			0x01, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00,
			0x00, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00, 0x18,
			0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
		};

		return std::string(reinterpret_cast<const char *>(kBytes), sizeof(kBytes));
	}

	static sunjwbase::tstring GetOfficialXXH3_64Vector()
	{
		return sunjwbase::strtotstr(std::string("54247382A8D6B94D"));
	}

	static sunjwbase::tstring GetOfficialXXH3_128Vector()
	{
		return sunjwbase::strtotstr(std::string("20EFC49FF02422EA54247382A8D6B94D"));
	}

	static sunjwbase::tstring GetOfficialCRC32CVector()
	{
		return sunjwbase::strtotstr(std::string("46DD794E"));
	}

	static sunjwbase::tstring GetOfficialCRC32CZeroVector()
	{
		return sunjwbase::strtotstr(std::string("8A9136AA"));
	}

	static sunjwbase::tstring GetOfficialCRC32CAllOnesVector()
	{
		return sunjwbase::strtotstr(std::string("62A8AB43"));
	}

	static sunjwbase::tstring GetOfficialCRC32CDescendingVector()
	{
		return sunjwbase::strtotstr(std::string("113FDB5C"));
	}

	static sunjwbase::tstring GetOfficialCRC32CIscsiVector()
	{
		return sunjwbase::strtotstr(std::string("D9963A56"));
	}

#if defined(FHASH_WITH_OPENSSL3_VENDOR)
	static std::vector<HashAlgorithmId> CreateOpenSslDigestAlgorithmIds()
	{
		std::vector<HashAlgorithmId> algorithmIds;
		algorithmIds.push_back(CreateAlgorithmId("openssl-sha-256"));
		algorithmIds.push_back(CreateAlgorithmId("openssl-sha-384"));
		algorithmIds.push_back(CreateAlgorithmId("openssl-sha-512"));
		algorithmIds.push_back(CreateAlgorithmId("openssl-sha3-256"));
		algorithmIds.push_back(CreateAlgorithmId("openssl-sha3-384"));
		algorithmIds.push_back(CreateAlgorithmId("openssl-sha3-512"));
		algorithmIds.push_back(CreateAlgorithmId("openssl-blake2b-160"));
		algorithmIds.push_back(CreateAlgorithmId("openssl-blake2b-256"));
		algorithmIds.push_back(CreateAlgorithmId("openssl-blake2b-512"));
		algorithmIds.push_back(CreateAlgorithmId("openssl-blake2s-128"));
		algorithmIds.push_back(CreateAlgorithmId("openssl-blake2s-256"));
		algorithmIds.push_back(CreateAlgorithmId("openssl-shake128-256"));
		algorithmIds.push_back(CreateAlgorithmId("openssl-shake256-512"));
		return algorithmIds;
	}

	static sunjwbase::tstring GetOfficialOpenSslSha256Vector()
	{
		return sunjwbase::strtotstr(std::string("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD"));
	}

	static sunjwbase::tstring GetOfficialOpenSslSha384Vector()
	{
		return sunjwbase::strtotstr(std::string("CB00753F45A35E8BB5A03D699AC65007272C32AB0EDED1631A8B605A43FF5BED8086072BA1E7CC2358BAECA134C825A7"));
	}

	static sunjwbase::tstring GetOfficialOpenSslSha512Vector()
	{
		return sunjwbase::strtotstr(std::string("DDAF35A193617ABACC417349AE20413112E6FA4E89A97EA20A9EEEE64B55D39A2192992A274FC1A836BA3C23A3FEEBBD454D4423643CE80E2A9AC94FA54CA49F"));
	}

	static sunjwbase::tstring GetOfficialOpenSslSha3_256Vector()
	{
		return sunjwbase::strtotstr(std::string("3A985DA74FE225B2045C172D6BD390BD855F086E3E9D525B46BFE24511431532"));
	}

	static sunjwbase::tstring GetOfficialOpenSslSha3_384Vector()
	{
		return sunjwbase::strtotstr(std::string("EC01498288516FC926459F58E2C6AD8DF9B473CB0FC08C2596DA7CF0E49BE4B298D88CEA927AC7F539F1EDF228376D25"));
	}

	static sunjwbase::tstring GetOfficialOpenSslSha3_512Vector()
	{
		return sunjwbase::strtotstr(std::string("B751850B1A57168A5693CD924B6B096E08F621827444F70D884F5D0240D2712E10E116E9192AF3C91A7EC57647E3934057340B4CF408D5A56592F8274EEC53F0"));
	}

	static sunjwbase::tstring GetOfficialOpenSslBlake2b_160Vector()
	{
		return sunjwbase::strtotstr(std::string("384264F676F39536840523F284921CDC68B6846B"));
	}

	static sunjwbase::tstring GetOfficialOpenSslBlake2b_256Vector()
	{
		return sunjwbase::strtotstr(std::string("BDDD813C634239723171EF3FEE98579B94964E3BB1CB3E427262C8C068D52319"));
	}

	static sunjwbase::tstring GetOfficialOpenSslBlake2b_512Vector()
	{
		return sunjwbase::strtotstr(std::string("BA80A53F981C4D0D6A2797B69F12F6E94C212F14685AC4B74B12BB6FDBFFA2D17D87C5392AAB792DC252D5DE4533CC9518D38AA8DBF1925AB92386EDD4009923"));
	}

	static sunjwbase::tstring GetOfficialOpenSslBlake2s_128Vector()
	{
		return sunjwbase::strtotstr(std::string("AA4938119B1DC7B87CBAD0FFD200D0AE"));
	}

	static sunjwbase::tstring GetOfficialOpenSslBlake2s_256Vector()
	{
		return sunjwbase::strtotstr(std::string("508C5E8C327C14E2E1A72BA34EEB452F37458B209ED63A294D999B4C86675982"));
	}

	static sunjwbase::tstring GetOfficialOpenSslShake128_256Vector()
	{
		return sunjwbase::strtotstr(std::string("5881092DD818BF5CF8A3DDB793FBCBA74097D5C526A6D35F97B83351940F2CC8"));
	}

	static sunjwbase::tstring GetOfficialOpenSslShake256_512Vector()
	{
		return sunjwbase::strtotstr(std::string("483366601360A8771C6863080CC4114D8DB44530F8F1E1EE4F94EA37E78B5739D5A15BEF186A5386C75744C0527E1FAA9F8726E462A12A4FEB06BD8801E751E4"));
	}
#endif

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

	static void ConfigureThreadDataFilesByAlgorithmIds(ThreadData& threadData, CapturingProgressSink& progressSink, const std::vector<sunjwbase::tstring>& filePaths, const std::vector<HashAlgorithmId>& enabledAlgorithmIds, bool uppercaseDigest = false)
	{
		ResetThreadDataForNewSession(threadData);
		SetThreadDataObserver(threadData, &progressSink);
		SetThreadDataUppercase(threadData, uppercaseDigest);
		for (size_t fileIndex = 0; fileIndex < filePaths.size(); ++fileIndex)
		{
			AppendThreadDataInputFile(threadData, filePaths[fileIndex]);
		}

		DisableAllAlgorithms(threadData);
		for (size_t algorithmIndex = 0; algorithmIndex < enabledAlgorithmIds.size(); ++algorithmIndex)
		{
			SetThreadDataHashAlgorithmEnabledById(threadData, enabledAlgorithmIds[algorithmIndex], true);
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

	static HashRequest CreateRequestByAlgorithmIds(const std::vector<sunjwbase::tstring>& filePaths, const std::vector<HashAlgorithmId>& enabledAlgorithmIds, bool uppercaseDigest = false)
	{
		HashRequest request;
		request.files = filePaths;
		for (size_t algorithmIndex = 0; algorithmIndex < enabledAlgorithmIds.size(); ++algorithmIndex)
		{
			AppendHashRequestAlgorithmId(request, enabledAlgorithmIds[algorithmIndex]);
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

	static sunjwbase::tstring FindDigestValueByAlgorithmId(const HashResult& result, const HashAlgorithmId& algorithmId)
	{
		HashAlgorithmId targetAlgorithmId = NormalizeHashAlgorithmId(algorithmId);
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

	static void HashThreadFunc_ComputesOfficialBlake3DigestsForKnownVector()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("blake3-vector.bin"), std::string("\x00\x01\x02", 3));

		CapturingProgressSink progressSink;
		ThreadData threadData;
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(filePath);
		std::vector<HashAlgorithmId> algorithmIds = CreateBlake3VariantAlgorithmIds();
		ConfigureThreadDataFilesByAlgorithmIds(threadData, progressSink, filePaths, algorithmIds);

		int exitCode = RunHashThreadData(threadData);
		NativeAssertEqual(0, exitCode, "BLAKE3 hashing should succeed for the official vector input.");
		NativeAssertEqual(static_cast<uint64_t>(1), GetThreadDataResultCount(threadData), "BLAKE3 hashing should still emit one result for one input file.");

		const HashResult& result = GetThreadDataResults(threadData).front();
		NativeAssertEqual(RESULT_ALL, result.state, "Successful BLAKE3 hashing should end in RESULT_ALL.");
		NativeAssertEqual(static_cast<size_t>(3), result.digests.size(), "Only the requested BLAKE3 variants should be emitted.");
		NativeAssertEqual(
			GetOfficialBlake3_256Vector(),
			FindDigestValueByAlgorithmId(result, algorithmIds[0]),
			"BLAKE3-256 did not match the official BLAKE3 vector for input length 3.");
		NativeAssertEqual(
			GetOfficialBlake3_512Vector(),
			FindDigestValueByAlgorithmId(result, algorithmIds[1]),
			"BLAKE3-512 did not match the official 64-byte extended BLAKE3 vector for input length 3.");
		NativeAssertEqual(
			GetOfficialBlake3Xof128Vector(),
			FindDigestValueByAlgorithmId(result, algorithmIds[2]),
			"BLAKE3 XOF did not match the official 128-byte extended BLAKE3 vector for input length 3.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_FILE_HASH_READY), "BLAKE3 hashing should emit a hash-ready event.");
	}

	static void HashThreadFunc_ComputesOfficialXXH3DigestsForKnownVector()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("xxh3-vector.bin"), CreateOfficialXXH3SanityInput(3));

		CapturingProgressSink progressSink;
		ThreadData threadData;
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(filePath);
		std::vector<HashAlgorithmId> algorithmIds = CreateXXH3VariantAlgorithmIds();
		ConfigureThreadDataFilesByAlgorithmIds(threadData, progressSink, filePaths, algorithmIds);

		int exitCode = RunHashThreadData(threadData);
		NativeAssertEqual(0, exitCode, "XXH3 hashing should succeed for the official sanity-vector input.");
		NativeAssertEqual(static_cast<uint64_t>(1), GetThreadDataResultCount(threadData), "XXH3 hashing should still emit one result for one input file.");

		const HashResult& result = GetThreadDataResults(threadData).front();
		NativeAssertEqual(RESULT_ALL, result.state, "Successful XXH3 hashing should end in RESULT_ALL.");
		NativeAssertEqual(static_cast<size_t>(2), result.digests.size(), "Only the requested XXH3 variants should be emitted.");
		NativeAssertEqual(
			GetOfficialXXH3_64Vector(),
			FindDigestValueByAlgorithmId(result, algorithmIds[0]),
			"XXH3-64 did not match the official xxHash vector for input length 3.");
		NativeAssertEqual(
			GetOfficialXXH3_128Vector(),
			FindDigestValueByAlgorithmId(result, algorithmIds[1]),
			"XXH3-128 did not match the official xxHash vector for input length 3.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_FILE_HASH_READY), "XXH3 hashing should emit a hash-ready event.");
	}

	static void HashThreadFunc_ComputesOfficialCRC32CDigestForKnownVector()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("crc32c-vector.bin"), CreateAscendingByteInput(32));

		CapturingProgressSink progressSink;
		ThreadData threadData;
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(filePath);
		std::vector<HashAlgorithmId> algorithmIds;
		algorithmIds.push_back(GetCRC32CAlgorithmId());
		ConfigureThreadDataFilesByAlgorithmIds(threadData, progressSink, filePaths, algorithmIds);

		int exitCode = RunHashThreadData(threadData);
		NativeAssertEqual(0, exitCode, "CRC32C hashing should succeed for the official capi vector input.");
		NativeAssertEqual(static_cast<uint64_t>(1), GetThreadDataResultCount(threadData), "CRC32C hashing should still emit one result for one input file.");

		const HashResult& result = GetThreadDataResults(threadData).front();
		NativeAssertEqual(RESULT_ALL, result.state, "Successful CRC32C hashing should end in RESULT_ALL.");
		NativeAssertEqual(static_cast<size_t>(1), result.digests.size(), "Only the requested CRC32C digest should be emitted.");
		NativeAssertEqual(
			GetOfficialCRC32CVector(),
			FindDigestValueByAlgorithmId(result, algorithmIds[0]),
			"CRC32C did not match the official google/crc32c vector for the ascending 32-byte input.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_FILE_HASH_READY), "CRC32C hashing should emit a hash-ready event.");
	}

	static void HashThreadFunc_ComputesOfficialCRC32CBoundaryDigestsForKnownVectors()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring zeroPath = tempDirectory.WriteTextFile(_T("crc32c-zeros.bin"), CreateFilledByteInput(32, 0x00));
		sunjwbase::tstring onesPath = tempDirectory.WriteTextFile(_T("crc32c-ff.bin"), CreateFilledByteInput(32, 0xFF));
		sunjwbase::tstring ascendingPath = tempDirectory.WriteTextFile(_T("crc32c-ascending.bin"), CreateAscendingByteInput(32));
		sunjwbase::tstring descendingPath = tempDirectory.WriteTextFile(_T("crc32c-descending.bin"), CreateDescendingByteInput(32));
		sunjwbase::tstring iscsiPath = tempDirectory.WriteTextFile(_T("crc32c-iscsi.bin"), CreateOfficialCRC32CIscsiInput());

		CapturingProgressSink progressSink;
		ThreadData threadData;
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(zeroPath);
		filePaths.push_back(onesPath);
		filePaths.push_back(ascendingPath);
		filePaths.push_back(descendingPath);
		filePaths.push_back(iscsiPath);
		std::vector<HashAlgorithmId> algorithmIds;
		algorithmIds.push_back(GetCRC32CAlgorithmId());
		ConfigureThreadDataFilesByAlgorithmIds(threadData, progressSink, filePaths, algorithmIds);

		int exitCode = RunHashThreadData(threadData);
		NativeAssertEqual(0, exitCode, "CRC32C boundary hashing should succeed for the official capi vectors.");
		NativeAssertEqual(static_cast<uint64_t>(5), GetThreadDataResultCount(threadData), "CRC32C boundary hashing should emit one result per official vector input.");

		const HashResultList& results = GetThreadDataResults(threadData);
		const HashResult *zeroResult = FindHashResultByPath(results, zeroPath);
		const HashResult *onesResult = FindHashResultByPath(results, onesPath);
		const HashResult *ascendingResult = FindHashResultByPath(results, ascendingPath);
		const HashResult *descendingResult = FindHashResultByPath(results, descendingPath);
		const HashResult *iscsiResult = FindHashResultByPath(results, iscsiPath);
		NativeAssertTrue(zeroResult != NULL, "CRC32C boundary hashing should preserve the zero-input file result.");
		NativeAssertTrue(onesResult != NULL, "CRC32C boundary hashing should preserve the all-0xFF file result.");
		NativeAssertTrue(ascendingResult != NULL, "CRC32C boundary hashing should preserve the ascending-input file result.");
		NativeAssertTrue(descendingResult != NULL, "CRC32C boundary hashing should preserve the descending-input file result.");
		NativeAssertTrue(iscsiResult != NULL, "CRC32C boundary hashing should preserve the iSCSI-input file result.");
		NativeAssertEqual(GetOfficialCRC32CZeroVector(), FindDigestValueByAlgorithmId(*zeroResult, algorithmIds[0]), "CRC32C zero-input vector did not match the official google/crc32c value.");
		NativeAssertEqual(GetOfficialCRC32CAllOnesVector(), FindDigestValueByAlgorithmId(*onesResult, algorithmIds[0]), "CRC32C all-0xFF input vector did not match the official google/crc32c value.");
		NativeAssertEqual(GetOfficialCRC32CVector(), FindDigestValueByAlgorithmId(*ascendingResult, algorithmIds[0]), "CRC32C ascending input vector did not match the official google/crc32c value.");
		NativeAssertEqual(GetOfficialCRC32CDescendingVector(), FindDigestValueByAlgorithmId(*descendingResult, algorithmIds[0]), "CRC32C descending input vector did not match the official google/crc32c value.");
		NativeAssertEqual(GetOfficialCRC32CIscsiVector(), FindDigestValueByAlgorithmId(*iscsiResult, algorithmIds[0]), "CRC32C iSCSI input vector did not match the official google/crc32c value.");
	}

#if defined(FHASH_WITH_OPENSSL3_VENDOR)
	static void HashThreadFunc_ComputesOfficialOpenSslDigestsForKnownVector()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("openssl-vector.txt"), "abc");

		CapturingProgressSink progressSink;
		ThreadData threadData;
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(filePath);
		std::vector<HashAlgorithmId> algorithmIds = CreateOpenSslDigestAlgorithmIds();
		ConfigureThreadDataFilesByAlgorithmIds(threadData, progressSink, filePaths, algorithmIds);

		int exitCode = RunHashThreadData(threadData);
		NativeAssertEqual(0, exitCode, "OpenSSL EVP hashing should succeed for the known 'abc' vector input.");
		NativeAssertEqual(static_cast<uint64_t>(1), GetThreadDataResultCount(threadData), "OpenSSL EVP hashing should emit one result for one input file.");

		const HashResult& result = GetThreadDataResults(threadData).front();
		NativeAssertEqual(RESULT_ALL, result.state, "Successful OpenSSL EVP hashing should end in RESULT_ALL.");
		NativeAssertEqual(static_cast<size_t>(13), result.digests.size(), "All requested OpenSSL digest variants should be emitted.");
		NativeAssertEqual(GetOfficialOpenSslSha256Vector(), FindDigestValueByAlgorithmId(result, algorithmIds[0]), "OpenSSL SHA-256 did not match the known vector for 'abc'.");
		NativeAssertEqual(GetOfficialOpenSslSha384Vector(), FindDigestValueByAlgorithmId(result, algorithmIds[1]), "OpenSSL SHA-384 did not match the known vector for 'abc'.");
		NativeAssertEqual(GetOfficialOpenSslSha512Vector(), FindDigestValueByAlgorithmId(result, algorithmIds[2]), "OpenSSL SHA-512 did not match the known vector for 'abc'.");
		NativeAssertEqual(GetOfficialOpenSslSha3_256Vector(), FindDigestValueByAlgorithmId(result, algorithmIds[3]), "OpenSSL SHA3-256 did not match the known vector for 'abc'.");
		NativeAssertEqual(GetOfficialOpenSslSha3_384Vector(), FindDigestValueByAlgorithmId(result, algorithmIds[4]), "OpenSSL SHA3-384 did not match the known vector for 'abc'.");
		NativeAssertEqual(GetOfficialOpenSslSha3_512Vector(), FindDigestValueByAlgorithmId(result, algorithmIds[5]), "OpenSSL SHA3-512 did not match the known vector for 'abc'.");
		NativeAssertEqual(GetOfficialOpenSslBlake2b_160Vector(), FindDigestValueByAlgorithmId(result, algorithmIds[6]), "OpenSSL BLAKE2b-160 did not match the known vector for 'abc'.");
		NativeAssertEqual(GetOfficialOpenSslBlake2b_256Vector(), FindDigestValueByAlgorithmId(result, algorithmIds[7]), "OpenSSL BLAKE2b-256 did not match the known vector for 'abc'.");
		NativeAssertEqual(GetOfficialOpenSslBlake2b_512Vector(), FindDigestValueByAlgorithmId(result, algorithmIds[8]), "OpenSSL BLAKE2b-512 did not match the known vector for 'abc'.");
		NativeAssertEqual(GetOfficialOpenSslBlake2s_128Vector(), FindDigestValueByAlgorithmId(result, algorithmIds[9]), "OpenSSL BLAKE2s-128 did not match the known vector for 'abc'.");
		NativeAssertEqual(GetOfficialOpenSslBlake2s_256Vector(), FindDigestValueByAlgorithmId(result, algorithmIds[10]), "OpenSSL BLAKE2s-256 did not match the known vector for 'abc'.");
		NativeAssertEqual(GetOfficialOpenSslShake128_256Vector(), FindDigestValueByAlgorithmId(result, algorithmIds[11]), "OpenSSL SHAKE128-256 did not match the known vector for 'abc'.");
		NativeAssertEqual(GetOfficialOpenSslShake256_512Vector(), FindDigestValueByAlgorithmId(result, algorithmIds[12]), "OpenSSL SHAKE256-512 did not match the known vector for 'abc'.");
		NativeAssertTrue(progressSink.HasEvent(PROGRESS_EVENT_FILE_HASH_READY), "OpenSSL EVP hashing should emit a hash-ready event.");
	}

	static void RunHashRequest_OpenSslSha2VariantsCanCoexistWithLegacySha2()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("openssl-coexist.txt"), "abc");
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(filePath);

		std::vector<HashAlgorithmId> algorithmIds;
		algorithmIds.push_back(CreateAlgorithmId("sha256"));
		algorithmIds.push_back(CreateAlgorithmId("sha512"));
		algorithmIds.push_back(CreateAlgorithmId("openssl-sha-256"));
		algorithmIds.push_back(CreateAlgorithmId("openssl-sha-512"));

		CapturingProgressSink progressSink;
		HashJobState jobState;
		HashCancellationState cancellationState;
		HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);
		HashRequest request = CreateRequestByAlgorithmIds(filePaths, algorithmIds);

		int exitCode = RunHashRequest(&executionContext, request);
		NativeAssertEqual(0, exitCode, "Legacy SHA2 and OpenSSL SHA-2 variants should coexist in the same request.");
		NativeAssertEqual(static_cast<size_t>(1), jobState.results.size(), "Coexisting legacy/OpenSSL SHA2 variants should still emit one file result.");

		const HashResult& result = jobState.results.front();
		NativeAssertEqual(static_cast<size_t>(4), result.digests.size(), "Legacy SHA2 and OpenSSL SHA-2 variants should all stay visible as separate digests.");
		NativeAssertEqual(CreateAlgorithmId("sha256"), ResolveDigestResultAlgorithmId(result.digests[0]), "Legacy SHA256 should remain addressable through its original id.");
		NativeAssertEqual(CreateAlgorithmId("sha512"), ResolveDigestResultAlgorithmId(result.digests[1]), "Legacy SHA512 should remain addressable through its original id.");
		NativeAssertEqual(CreateAlgorithmId("openssl-sha-256"), ResolveDigestResultAlgorithmId(result.digests[2]), "OpenSSL SHA-256 should stay distinct from legacy SHA256.");
		NativeAssertEqual(CreateAlgorithmId("openssl-sha-512"), ResolveDigestResultAlgorithmId(result.digests[3]), "OpenSSL SHA-512 should stay distinct from legacy SHA512.");
		NativeAssertEqual(GetOfficialOpenSslSha256Vector(), result.digests[0].value, "Legacy SHA256 should keep the expected vector when OpenSSL SHA-256 is also selected.");
		NativeAssertEqual(GetOfficialOpenSslSha512Vector(), result.digests[1].value, "Legacy SHA512 should keep the expected vector when OpenSSL SHA-512 is also selected.");
		NativeAssertEqual(GetOfficialOpenSslSha256Vector(), result.digests[2].value, "OpenSSL SHA-256 should match the expected vector.");
		NativeAssertEqual(GetOfficialOpenSslSha512Vector(), result.digests[3].value, "OpenSSL SHA-512 should match the expected vector.");
	}

	static void RunHashRequest_OpenSslUnknownIdsAreIgnoredAndKnownVariantsStayOrdered()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("openssl-order.txt"), "abc");

		CapturingProgressSink progressSink;
		HashJobState jobState;
		HashCancellationState cancellationState;
		HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);

		HashRequest request;
		request.files.push_back(filePath);
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("openssl-sha3"));
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("openssl-sha3-512"));
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("openssl-blake2b-256"));
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("openssl-shake256-512"));
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("openssl-sha-256"));
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("openssl-sha3-512"));
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("openssl-blake2"));
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("openssl-blake2s-128"));

		std::vector<HashAlgorithmId> normalizedAlgorithmIds = GetHashRequestNormalizedAlgorithmIds(request);
		NativeAssertEqual(static_cast<size_t>(5), normalizedAlgorithmIds.size(), "Unknown or duplicate OpenSSL EVP ids should be removed during request normalization.");
		NativeAssertEqual(CreateAlgorithmId("openssl-sha3-512"), normalizedAlgorithmIds[0], "OpenSSL EVP ids should preserve explicit request order after unknown ids are removed.");
		NativeAssertEqual(CreateAlgorithmId("openssl-blake2b-256"), normalizedAlgorithmIds[1], "OpenSSL EVP ids should preserve explicit request order after unknown ids are removed.");
		NativeAssertEqual(CreateAlgorithmId("openssl-shake256-512"), normalizedAlgorithmIds[2], "OpenSSL EVP ids should preserve explicit request order after unknown ids are removed.");
		NativeAssertEqual(CreateAlgorithmId("openssl-sha-256"), normalizedAlgorithmIds[3], "OpenSSL EVP ids should preserve explicit request order after unknown ids are removed.");
		NativeAssertEqual(CreateAlgorithmId("openssl-blake2s-128"), normalizedAlgorithmIds[4], "OpenSSL EVP ids should preserve explicit request order after unknown ids are removed.");

		int exitCode = RunHashRequest(&executionContext, request);
		NativeAssertEqual(0, exitCode, "OpenSSL EVP hashing should ignore unknown ids and still succeed.");
		NativeAssertEqual(static_cast<size_t>(1), jobState.results.size(), "Ignoring unknown OpenSSL EVP ids should still produce one file result.");

		const HashResult& result = jobState.results.front();
		NativeAssertEqual(static_cast<size_t>(5), result.digests.size(), "Only known OpenSSL EVP variants should be emitted.");
		NativeAssertEqual(CreateAlgorithmId("openssl-sha3-512"), ResolveDigestResultAlgorithmId(result.digests[0]), "OpenSSL EVP result order should follow the normalized request order.");
		NativeAssertEqual(CreateAlgorithmId("openssl-blake2b-256"), ResolveDigestResultAlgorithmId(result.digests[1]), "OpenSSL EVP result order should follow the normalized request order.");
		NativeAssertEqual(CreateAlgorithmId("openssl-shake256-512"), ResolveDigestResultAlgorithmId(result.digests[2]), "OpenSSL EVP result order should follow the normalized request order.");
		NativeAssertEqual(CreateAlgorithmId("openssl-sha-256"), ResolveDigestResultAlgorithmId(result.digests[3]), "OpenSSL EVP result order should follow the normalized request order.");
		NativeAssertEqual(CreateAlgorithmId("openssl-blake2s-128"), ResolveDigestResultAlgorithmId(result.digests[4]), "OpenSSL EVP result order should follow the normalized request order.");
		NativeAssertEqual(GetOfficialOpenSslSha3_512Vector(), result.digests[0].value, "OpenSSL SHA3-512 should stay deterministic after unknown id filtering.");
		NativeAssertEqual(GetOfficialOpenSslBlake2b_256Vector(), result.digests[1].value, "OpenSSL BLAKE2b-256 should stay deterministic after unknown id filtering.");
		NativeAssertEqual(GetOfficialOpenSslShake256_512Vector(), result.digests[2].value, "OpenSSL SHAKE256-512 should stay deterministic after unknown id filtering.");
		NativeAssertEqual(GetOfficialOpenSslSha256Vector(), result.digests[3].value, "OpenSSL SHA-256 should stay deterministic after unknown id filtering.");
		NativeAssertEqual(GetOfficialOpenSslBlake2s_128Vector(), result.digests[4].value, "OpenSSL BLAKE2s-128 should stay deterministic after unknown id filtering.");
	}

	static void HashThreadFunc_OpenSslVariantsRemainStableAcrossConcurrentRuns()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("openssl-concurrency.bin"), std::string((kHashEngineBufferSize * 2) + 511, 'O'));
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(filePath);
		std::vector<HashAlgorithmId> algorithmIds = CreateOpenSslDigestAlgorithmIds();

		auto runSingleRequest = [&]() -> HashResult
		{
			CapturingProgressSink progressSink;
			HashJobState jobState;
			HashCancellationState cancellationState;
			HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);
			HashRequest request = CreateRequestByAlgorithmIds(filePaths, algorithmIds);

			int exitCode = RunHashRequest(&executionContext, request);
			NativeAssertEqual(0, exitCode, "Concurrent OpenSSL EVP hashing should succeed.");
			NativeAssertEqual(static_cast<size_t>(1), jobState.results.size(), "Concurrent OpenSSL EVP hashing should still produce exactly one file result per request.");
			return jobState.results.front();
		};

		HashResult baselineResult = runSingleRequest();
		const size_t concurrentRunCount = 4;
		std::vector<std::future<HashResult>> tasks;
		tasks.reserve(concurrentRunCount);
		for (size_t runIndex = 0; runIndex < concurrentRunCount; ++runIndex)
		{
			tasks.push_back(std::async(std::launch::async, runSingleRequest));
		}

		for (size_t taskIndex = 0; taskIndex < tasks.size(); ++taskIndex)
		{
			HashResult concurrentResult = tasks[taskIndex].get();
			for (size_t algorithmIndex = 0; algorithmIndex < algorithmIds.size(); ++algorithmIndex)
			{
				NativeAssertEqual(
					FindDigestValueByAlgorithmId(baselineResult, algorithmIds[algorithmIndex]),
					FindDigestValueByAlgorithmId(concurrentResult, algorithmIds[algorithmIndex]),
					"Concurrent OpenSSL EVP hashing produced an inconsistent digest.");
			}
		}
	}
#endif

	static void RunHashRequest_Blake3UppercaseFlagRemainsDeterministicAcrossVariants()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("blake3-uppercase.bin"), std::string("\x00\x01\x02", 3));
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(filePath);
		std::vector<HashAlgorithmId> algorithmIds = CreateBlake3VariantAlgorithmIds();

		CapturingProgressSink lowercaseProgressSink;
		HashJobState lowercaseJobState;
		HashCancellationState lowercaseCancellationState;
		HashExecutionContext lowercaseExecutionContext = CreateExecutionContext(lowercaseProgressSink, lowercaseJobState, lowercaseCancellationState);
		HashRequest lowercaseRequest = CreateRequestByAlgorithmIds(filePaths, algorithmIds, false);

		int lowercaseExitCode = RunHashRequest(&lowercaseExecutionContext, lowercaseRequest);
		NativeAssertEqual(0, lowercaseExitCode, "BLAKE3 hashing should succeed for lowercase output preference.");
		ProgressEvent lowercaseHashReadyEvent;
		NativeAssertTrue(lowercaseProgressSink.TryGetFirstEvent(PROGRESS_EVENT_FILE_HASH_READY, &lowercaseHashReadyEvent), "BLAKE3 hashing should emit a hash-ready event for lowercase preference.");
		NativeAssertTrue(!lowercaseHashReadyEvent.uppercaseDigest, "The lowercase BLAKE3 request should preserve uppercaseDigest=false in the event.");
		NativeAssertEqual(GetOfficialBlake3_256Vector(), FindDigestValueByAlgorithmId(lowercaseHashReadyEvent.result, algorithmIds[0]), "The lowercase BLAKE3-256 digest should stay deterministic.");
		NativeAssertEqual(GetOfficialBlake3_512Vector(), FindDigestValueByAlgorithmId(lowercaseHashReadyEvent.result, algorithmIds[1]), "The lowercase BLAKE3-512 digest should stay deterministic.");
		NativeAssertEqual(GetOfficialBlake3Xof128Vector(), FindDigestValueByAlgorithmId(lowercaseHashReadyEvent.result, algorithmIds[2]), "The lowercase BLAKE3 XOF digest should stay deterministic.");

		CapturingProgressSink uppercaseProgressSink;
		HashJobState uppercaseJobState;
		HashCancellationState uppercaseCancellationState;
		HashExecutionContext uppercaseExecutionContext = CreateExecutionContext(uppercaseProgressSink, uppercaseJobState, uppercaseCancellationState);
		HashRequest uppercaseRequest = CreateRequestByAlgorithmIds(filePaths, algorithmIds, true);

		int uppercaseExitCode = RunHashRequest(&uppercaseExecutionContext, uppercaseRequest);
		NativeAssertEqual(0, uppercaseExitCode, "BLAKE3 hashing should succeed for uppercase output preference.");
		ProgressEvent uppercaseHashReadyEvent;
		NativeAssertTrue(uppercaseProgressSink.TryGetFirstEvent(PROGRESS_EVENT_FILE_HASH_READY, &uppercaseHashReadyEvent), "BLAKE3 hashing should emit a hash-ready event for uppercase preference.");
		NativeAssertTrue(uppercaseHashReadyEvent.uppercaseDigest, "The uppercase BLAKE3 request should preserve uppercaseDigest=true in the event.");
		NativeAssertEqual(GetOfficialBlake3_256Vector(), FindDigestValueByAlgorithmId(uppercaseHashReadyEvent.result, algorithmIds[0]), "The uppercase BLAKE3-256 digest should stay deterministic.");
		NativeAssertEqual(GetOfficialBlake3_512Vector(), FindDigestValueByAlgorithmId(uppercaseHashReadyEvent.result, algorithmIds[1]), "The uppercase BLAKE3-512 digest should stay deterministic.");
		NativeAssertEqual(GetOfficialBlake3Xof128Vector(), FindDigestValueByAlgorithmId(uppercaseHashReadyEvent.result, algorithmIds[2]), "The uppercase BLAKE3 XOF digest should stay deterministic.");
	}

	static void RunHashRequest_Blake3UnknownIdsAreIgnoredAndKnownVariantsStayOrdered()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("blake3-order.bin"), std::string("\x00\x01\x02", 3));

		CapturingProgressSink progressSink;
		HashJobState jobState;
		HashCancellationState cancellationState;
		HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);

		HashRequest request;
		request.files.push_back(filePath);
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("blake3-1024"));
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("blake3-512"));
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("blake3"));
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("blake3-xof"));
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("blake3-256"));
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("blake3-512"));

		std::vector<HashAlgorithmId> normalizedAlgorithmIds = GetHashRequestNormalizedAlgorithmIds(request);
		NativeAssertEqual(static_cast<size_t>(3), normalizedAlgorithmIds.size(), "Unknown or duplicate BLAKE3 ids should be removed during request normalization.");
		NativeAssertEqual(CreateAlgorithmId("blake3-512"), normalizedAlgorithmIds[0], "Known BLAKE3 variants should preserve explicit request order after unknown ids are removed.");
		NativeAssertEqual(CreateAlgorithmId("blake3-xof"), normalizedAlgorithmIds[1], "Known BLAKE3 variants should preserve explicit request order after unknown ids are removed.");
		NativeAssertEqual(CreateAlgorithmId("blake3-256"), normalizedAlgorithmIds[2], "Known BLAKE3 variants should preserve explicit request order after unknown ids are removed.");

		int exitCode = RunHashRequest(&executionContext, request);
		NativeAssertEqual(0, exitCode, "BLAKE3 hashing should ignore unknown ids and still succeed.");
		NativeAssertEqual(static_cast<size_t>(1), jobState.results.size(), "Ignoring unknown BLAKE3 ids should still produce one file result.");

		const HashResult& result = jobState.results.front();
		NativeAssertEqual(static_cast<size_t>(3), result.digests.size(), "Only known BLAKE3 variants should be emitted.");
		NativeAssertEqual(CreateAlgorithmId("blake3-512"), ResolveDigestResultAlgorithmId(result.digests[0]), "BLAKE3 result order should follow the normalized request order.");
		NativeAssertEqual(CreateAlgorithmId("blake3-xof"), ResolveDigestResultAlgorithmId(result.digests[1]), "BLAKE3 result order should follow the normalized request order.");
		NativeAssertEqual(CreateAlgorithmId("blake3-256"), ResolveDigestResultAlgorithmId(result.digests[2]), "BLAKE3 result order should follow the normalized request order.");
		NativeAssertEqual(GetOfficialBlake3_512Vector(), result.digests[0].value, "BLAKE3-512 digest value should stay deterministic after unknown id filtering.");
		NativeAssertEqual(GetOfficialBlake3Xof128Vector(), result.digests[1].value, "BLAKE3 XOF digest value should stay deterministic after unknown id filtering.");
		NativeAssertEqual(GetOfficialBlake3_256Vector(), result.digests[2].value, "BLAKE3-256 digest value should stay deterministic after unknown id filtering.");
	}

	static void HashThreadFunc_Blake3VariantsRemainStableAcrossConcurrentRuns()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("blake3-concurrency.bin"), std::string((kHashEngineBufferSize * 2) + 257, 'B'));
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(filePath);
		std::vector<HashAlgorithmId> algorithmIds = CreateBlake3VariantAlgorithmIds();

		auto runSingleRequest = [&]() -> HashResult
		{
			CapturingProgressSink progressSink;
			HashJobState jobState;
			HashCancellationState cancellationState;
			HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);
			HashRequest request = CreateRequestByAlgorithmIds(filePaths, algorithmIds);

			int exitCode = RunHashRequest(&executionContext, request);
			NativeAssertEqual(0, exitCode, "Concurrent BLAKE3 hashing should succeed.");
			NativeAssertEqual(static_cast<size_t>(1), jobState.results.size(), "Concurrent BLAKE3 hashing should still produce exactly one file result per request.");
			return jobState.results.front();
		};

		HashResult baselineResult = runSingleRequest();
		sunjwbase::tstring expectedBlake3_256 = FindDigestValueByAlgorithmId(baselineResult, algorithmIds[0]);
		sunjwbase::tstring expectedBlake3_512 = FindDigestValueByAlgorithmId(baselineResult, algorithmIds[1]);
		sunjwbase::tstring expectedBlake3Xof = FindDigestValueByAlgorithmId(baselineResult, algorithmIds[2]);

		const size_t concurrentRunCount = 6;
		std::vector<std::future<HashResult>> tasks;
		tasks.reserve(concurrentRunCount);
		for (size_t runIndex = 0; runIndex < concurrentRunCount; ++runIndex)
		{
			tasks.push_back(std::async(std::launch::async, runSingleRequest));
		}

		for (size_t taskIndex = 0; taskIndex < tasks.size(); ++taskIndex)
		{
			HashResult concurrentResult = tasks[taskIndex].get();
			NativeAssertEqual(expectedBlake3_256, FindDigestValueByAlgorithmId(concurrentResult, algorithmIds[0]), "Concurrent runtime hashing produced an inconsistent BLAKE3-256 digest.");
			NativeAssertEqual(expectedBlake3_512, FindDigestValueByAlgorithmId(concurrentResult, algorithmIds[1]), "Concurrent runtime hashing produced an inconsistent BLAKE3-512 digest.");
			NativeAssertEqual(expectedBlake3Xof, FindDigestValueByAlgorithmId(concurrentResult, algorithmIds[2]), "Concurrent runtime hashing produced an inconsistent BLAKE3 XOF digest.");
		}
	}

	static void RunHashRequest_XXH3AndCRC32CUnknownIdsAreIgnoredAndKnownVariantsStayOrdered()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("xxh3-crc32c-order.bin"), CreateAscendingByteInput(32));
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(filePath);

		std::vector<HashAlgorithmId> expectedAlgorithmIds;
		expectedAlgorithmIds.push_back(CreateAlgorithmId("xxh3-128"));
		expectedAlgorithmIds.push_back(GetCRC32CAlgorithmId());
		expectedAlgorithmIds.push_back(CreateAlgorithmId("xxh3-64"));

		CapturingProgressSink baselineProgressSink;
		HashJobState baselineJobState;
		HashCancellationState baselineCancellationState;
		HashExecutionContext baselineExecutionContext = CreateExecutionContext(baselineProgressSink, baselineJobState, baselineCancellationState);
		HashRequest baselineRequest = CreateRequestByAlgorithmIds(filePaths, expectedAlgorithmIds);

		int baselineExitCode = RunHashRequest(&baselineExecutionContext, baselineRequest);
		NativeAssertEqual(0, baselineExitCode, "Baseline XXH3/CRC32C hashing should succeed.");
		NativeAssertEqual(static_cast<size_t>(1), baselineJobState.results.size(), "Baseline XXH3/CRC32C hashing should emit exactly one file result.");
		const HashResult& baselineResult = baselineJobState.results.front();

		CapturingProgressSink progressSink;
		HashJobState jobState;
		HashCancellationState cancellationState;
		HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);

		HashRequest request;
		request.files.push_back(filePath);
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("xxh3"));
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("xxh3-128"));
		AppendHashRequestAlgorithmId(request, GetCRC32CAlgorithmId());
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("xxh3-64"));
		AppendHashRequestAlgorithmId(request, GetCRC32CAlgorithmId());
		AppendHashRequestAlgorithmId(request, CreateAlgorithmId("crc32c-64"));

		std::vector<HashAlgorithmId> normalizedAlgorithmIds = GetHashRequestNormalizedAlgorithmIds(request);
		NativeAssertEqual(static_cast<size_t>(3), normalizedAlgorithmIds.size(), "Unknown or duplicate XXH3/CRC32C ids should be removed during request normalization.");
		NativeAssertEqual(expectedAlgorithmIds[0], normalizedAlgorithmIds[0], "Known XXH3/CRC32C variants should preserve explicit request order after unknown ids are removed.");
		NativeAssertEqual(expectedAlgorithmIds[1], normalizedAlgorithmIds[1], "Known XXH3/CRC32C variants should preserve explicit request order after unknown ids are removed.");
		NativeAssertEqual(expectedAlgorithmIds[2], normalizedAlgorithmIds[2], "Known XXH3/CRC32C variants should preserve explicit request order after unknown ids are removed.");

		int exitCode = RunHashRequest(&executionContext, request);
		NativeAssertEqual(0, exitCode, "XXH3/CRC32C hashing should ignore unknown ids and still succeed.");
		NativeAssertEqual(static_cast<size_t>(1), jobState.results.size(), "Ignoring unknown XXH3/CRC32C ids should still produce one file result.");

		const HashResult& result = jobState.results.front();
		NativeAssertEqual(static_cast<size_t>(3), result.digests.size(), "Only known XXH3/CRC32C variants should be emitted.");
		NativeAssertEqual(expectedAlgorithmIds[0], ResolveDigestResultAlgorithmId(result.digests[0]), "XXH3/CRC32C result order should follow the normalized request order.");
		NativeAssertEqual(expectedAlgorithmIds[1], ResolveDigestResultAlgorithmId(result.digests[1]), "XXH3/CRC32C result order should follow the normalized request order.");
		NativeAssertEqual(expectedAlgorithmIds[2], ResolveDigestResultAlgorithmId(result.digests[2]), "XXH3/CRC32C result order should follow the normalized request order.");
		NativeAssertEqual(baselineResult.digests[0].value, result.digests[0].value, "XXH3-128 digest value should stay deterministic after unknown id filtering.");
		NativeAssertEqual(GetOfficialCRC32CVector(), result.digests[1].value, "CRC32C digest value should stay deterministic after unknown id filtering.");
		NativeAssertEqual(baselineResult.digests[2].value, result.digests[2].value, "XXH3-64 digest value should stay deterministic after unknown id filtering.");
	}

	static void HashThreadFunc_XXH3AndCRC32CRemainStableAcrossConcurrentRuns()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("xxh3-crc32c-concurrency.bin"), std::string((kHashEngineBufferSize * 2) + 193, 'X'));
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(filePath);
		std::vector<HashAlgorithmId> algorithmIds;
		algorithmIds.push_back(CreateAlgorithmId("xxh3-64"));
		algorithmIds.push_back(CreateAlgorithmId("xxh3-128"));
		algorithmIds.push_back(GetCRC32CAlgorithmId());

		auto runSingleRequest = [&]() -> HashResult
		{
			CapturingProgressSink progressSink;
			HashJobState jobState;
			HashCancellationState cancellationState;
			HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);
			HashRequest request = CreateRequestByAlgorithmIds(filePaths, algorithmIds);

			int exitCode = RunHashRequest(&executionContext, request);
			NativeAssertEqual(0, exitCode, "Concurrent XXH3/CRC32C hashing should succeed.");
			NativeAssertEqual(static_cast<size_t>(1), jobState.results.size(), "Concurrent XXH3/CRC32C hashing should still produce exactly one file result per request.");
			return jobState.results.front();
		};

		HashResult baselineResult = runSingleRequest();
		sunjwbase::tstring expectedXXH3_64 = FindDigestValueByAlgorithmId(baselineResult, algorithmIds[0]);
		sunjwbase::tstring expectedXXH3_128 = FindDigestValueByAlgorithmId(baselineResult, algorithmIds[1]);
		sunjwbase::tstring expectedCRC32C = FindDigestValueByAlgorithmId(baselineResult, algorithmIds[2]);

		const size_t concurrentRunCount = 6;
		std::vector<std::future<HashResult>> tasks;
		tasks.reserve(concurrentRunCount);
		for (size_t runIndex = 0; runIndex < concurrentRunCount; ++runIndex)
		{
			tasks.push_back(std::async(std::launch::async, runSingleRequest));
		}

		for (size_t taskIndex = 0; taskIndex < tasks.size(); ++taskIndex)
		{
			HashResult concurrentResult = tasks[taskIndex].get();
			NativeAssertEqual(expectedXXH3_64, FindDigestValueByAlgorithmId(concurrentResult, algorithmIds[0]), "Concurrent runtime hashing produced an inconsistent XXH3-64 digest.");
			NativeAssertEqual(expectedXXH3_128, FindDigestValueByAlgorithmId(concurrentResult, algorithmIds[1]), "Concurrent runtime hashing produced an inconsistent XXH3-128 digest.");
			NativeAssertEqual(expectedCRC32C, FindDigestValueByAlgorithmId(concurrentResult, algorithmIds[2]), "Concurrent runtime hashing produced an inconsistent CRC32C digest.");
		}
	}

	static void RunHashRequest_XXH3AndCRC32CMultiFileConcurrentMatchesSingleRun()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring firstPath = tempDirectory.WriteTextFile(_T("xxh3-crc32c-a.bin"), CreateAscendingByteInput(32));
		sunjwbase::tstring secondPath = tempDirectory.WriteTextFile(_T("xxh3-crc32c-b.bin"), std::string((kHashEngineBufferSize * 2) + 41, 'Q'));
		sunjwbase::tstring thirdPath = tempDirectory.WriteTextFile(_T("xxh3-crc32c-c.bin"), CreateOfficialXXH3SanityInput(257));
		std::vector<sunjwbase::tstring> filePaths;
		filePaths.push_back(firstPath);
		filePaths.push_back(secondPath);
		filePaths.push_back(thirdPath);
		std::vector<HashAlgorithmId> algorithmIds;
		algorithmIds.push_back(CreateAlgorithmId("xxh3-64"));
		algorithmIds.push_back(CreateAlgorithmId("xxh3-128"));
		algorithmIds.push_back(GetCRC32CAlgorithmId());

		auto runSingleRequest = [&]() -> HashResultList
		{
			CapturingProgressSink progressSink;
			HashJobState jobState;
			HashCancellationState cancellationState;
			HashExecutionContext executionContext = CreateExecutionContext(progressSink, jobState, cancellationState);
			HashRequest request = CreateRequestByAlgorithmIds(filePaths, algorithmIds);

			int exitCode = RunHashRequest(&executionContext, request);
			NativeAssertEqual(0, exitCode, "Concurrent multi-file XXH3/CRC32C hashing should succeed.");
			NativeAssertEqual(filePaths.size(), jobState.results.size(), "Concurrent multi-file XXH3/CRC32C hashing should emit one result per requested file.");
			return jobState.results;
		};

		HashResultList baselineResults = runSingleRequest();
		const size_t concurrentRunCount = 4;
		std::vector<std::future<HashResultList>> tasks;
		tasks.reserve(concurrentRunCount);
		for (size_t runIndex = 0; runIndex < concurrentRunCount; ++runIndex)
		{
			tasks.push_back(std::async(std::launch::async, runSingleRequest));
		}

		for (size_t taskIndex = 0; taskIndex < tasks.size(); ++taskIndex)
		{
			HashResultList concurrentResults = tasks[taskIndex].get();
			for (size_t fileIndex = 0; fileIndex < filePaths.size(); ++fileIndex)
			{
				const HashResult *baselineResult = FindHashResultByPath(baselineResults, filePaths[fileIndex]);
				const HashResult *concurrentResult = FindHashResultByPath(concurrentResults, filePaths[fileIndex]);
				NativeAssertTrue(baselineResult != NULL, "The baseline XXH3/CRC32C multi-file run should preserve every file result.");
				NativeAssertTrue(concurrentResult != NULL, "Concurrent XXH3/CRC32C multi-file runs should preserve every file result.");
				NativeAssertEqual(FindDigestValueByAlgorithmId(*baselineResult, algorithmIds[0]), FindDigestValueByAlgorithmId(*concurrentResult, algorithmIds[0]), "Concurrent multi-file hashing produced an inconsistent XXH3-64 digest.");
				NativeAssertEqual(FindDigestValueByAlgorithmId(*baselineResult, algorithmIds[1]), FindDigestValueByAlgorithmId(*concurrentResult, algorithmIds[1]), "Concurrent multi-file hashing produced an inconsistent XXH3-128 digest.");
				NativeAssertEqual(FindDigestValueByAlgorithmId(*baselineResult, algorithmIds[2]), FindDigestValueByAlgorithmId(*concurrentResult, algorithmIds[2]), "Concurrent multi-file hashing produced an inconsistent CRC32C digest.");
			}
		}
	}

	static void HashThreadFunc_ProducesConsistentDigestsAcrossConcurrentRuns()
	{
		ScopedTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("concurrency.txt"), std::string((kHashEngineBufferSize * 2) + 321, 'A'));

		std::vector<ResultDigestType> algorithms;
		algorithms.push_back(RESULT_DIGEST_MD5);
		algorithms.push_back(RESULT_DIGEST_SHA1);
		algorithms.push_back(RESULT_DIGEST_SHA256);

		auto runSingleRequest = [&]() -> HashResult
		{
			CapturingProgressSink progressSink;
			ThreadData threadData;
			ConfigureThreadData(threadData, progressSink, filePath, algorithms);

			int exitCode = RunHashThreadData(threadData);
			NativeAssertEqual(0, exitCode, "Concurrent runtime hashing should succeed for each isolated task.");
			NativeAssertEqual(static_cast<uint64_t>(1), GetThreadDataResultCount(threadData), "Each concurrent runtime hashing task should still produce one result.");
			return GetThreadDataResults(threadData).front();
		};

		HashResult baselineResult = runSingleRequest();
		sunjwbase::tstring expectedMd5 = FindDigestValue(baselineResult, RESULT_DIGEST_MD5);
		sunjwbase::tstring expectedSha1 = FindDigestValue(baselineResult, RESULT_DIGEST_SHA1);
		sunjwbase::tstring expectedSha256 = FindDigestValue(baselineResult, RESULT_DIGEST_SHA256);

		const size_t concurrentRunCount = 8;
		std::vector<std::future<HashResult>> pendingRuns;
		pendingRuns.reserve(concurrentRunCount);
		for (size_t runIndex = 0; runIndex < concurrentRunCount; ++runIndex)
		{
			pendingRuns.push_back(std::async(std::launch::async, runSingleRequest));
		}

		for (size_t runIndex = 0; runIndex < pendingRuns.size(); ++runIndex)
		{
			HashResult concurrentResult = pendingRuns[runIndex].get();
			NativeAssertEqual(expectedMd5, FindDigestValue(concurrentResult, RESULT_DIGEST_MD5), "Concurrent runtime hashing produced an inconsistent MD5 digest.");
			NativeAssertEqual(expectedSha1, FindDigestValue(concurrentResult, RESULT_DIGEST_SHA1), "Concurrent runtime hashing produced an inconsistent SHA1 digest.");
			NativeAssertEqual(expectedSha256, FindDigestValue(concurrentResult, RESULT_DIGEST_SHA256), "Concurrent runtime hashing produced an inconsistent SHA256 digest.");
		}
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
#if defined(FHASH_WITH_OPENSSL3_VENDOR)
	tests.push_back({ "HashThreadFunc_ComputesOfficialOpenSslDigestsForKnownVector", &HashThreadFunc_ComputesOfficialOpenSslDigestsForKnownVector });
	tests.push_back({ "RunHashRequest_OpenSslSha2VariantsCanCoexistWithLegacySha2", &RunHashRequest_OpenSslSha2VariantsCanCoexistWithLegacySha2 });
	tests.push_back({ "RunHashRequest_OpenSslUnknownIdsAreIgnoredAndKnownVariantsStayOrdered", &RunHashRequest_OpenSslUnknownIdsAreIgnoredAndKnownVariantsStayOrdered });
	tests.push_back({ "HashThreadFunc_OpenSslVariantsRemainStableAcrossConcurrentRuns", &HashThreadFunc_OpenSslVariantsRemainStableAcrossConcurrentRuns });
#endif
	tests.push_back({ "HashThreadFunc_ComputesOfficialBlake3DigestsForKnownVector", &HashThreadFunc_ComputesOfficialBlake3DigestsForKnownVector });
	tests.push_back({ "HashThreadFunc_ComputesOfficialXXH3DigestsForKnownVector", &HashThreadFunc_ComputesOfficialXXH3DigestsForKnownVector });
	tests.push_back({ "HashThreadFunc_ComputesOfficialCRC32CDigestForKnownVector", &HashThreadFunc_ComputesOfficialCRC32CDigestForKnownVector });
	tests.push_back({ "HashThreadFunc_ComputesOfficialCRC32CBoundaryDigestsForKnownVectors", &HashThreadFunc_ComputesOfficialCRC32CBoundaryDigestsForKnownVectors });
	tests.push_back({ "RunHashRequest_Blake3UppercaseFlagRemainsDeterministicAcrossVariants", &RunHashRequest_Blake3UppercaseFlagRemainsDeterministicAcrossVariants });
	tests.push_back({ "RunHashRequest_Blake3UnknownIdsAreIgnoredAndKnownVariantsStayOrdered", &RunHashRequest_Blake3UnknownIdsAreIgnoredAndKnownVariantsStayOrdered });
	tests.push_back({ "HashThreadFunc_Blake3VariantsRemainStableAcrossConcurrentRuns", &HashThreadFunc_Blake3VariantsRemainStableAcrossConcurrentRuns });
	tests.push_back({ "RunHashRequest_XXH3AndCRC32CUnknownIdsAreIgnoredAndKnownVariantsStayOrdered", &RunHashRequest_XXH3AndCRC32CUnknownIdsAreIgnoredAndKnownVariantsStayOrdered });
	tests.push_back({ "HashThreadFunc_XXH3AndCRC32CRemainStableAcrossConcurrentRuns", &HashThreadFunc_XXH3AndCRC32CRemainStableAcrossConcurrentRuns });
	tests.push_back({ "RunHashRequest_XXH3AndCRC32CMultiFileConcurrentMatchesSingleRun", &RunHashRequest_XXH3AndCRC32CMultiFileConcurrentMatchesSingleRun });
	tests.push_back({ "HashThreadFunc_ProducesConsistentDigestsAcrossConcurrentRuns", &HashThreadFunc_ProducesConsistentDigestsAcrossConcurrentRuns });
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
