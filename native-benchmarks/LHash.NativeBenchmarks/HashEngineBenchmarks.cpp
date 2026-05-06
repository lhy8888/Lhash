#include "..\..\trunk\source\stdafx.h"

#include <algorithm>
#include <chrono>
#include <fstream>
#include <iomanip>
#include <sstream>
#include <vector>

#include "NativeBenchmarkHarness.h"

#include "Common/HashEngine.h"
#include "Common/Global.h"
#include "Domain/HashAlgorithmRegistryCore.h"
#include "Domain/HashRequest.h"
#include "Runtime/HashExecutionContext.h"
#include "WinCommon/WinHandleGuard.h"

namespace
{
	static const uint32_t kWriteBufferBytes = 1u << 20;
	static const uint64_t kSmallFileBytes = 64ull * 1024ull;
	static const uint64_t kLargeFileBytes = 128ull * 1024ull * 1024ull;
	static const uint32_t kManySmallFileCount = 256;

	struct HashAlgorithmSetDefinition
	{
		const char *id;
		const char *label;
		const char *algorithmIds[4];
		uint32_t algorithmCount;
	};

	struct BenchmarkScenarioDefinition
	{
		const char *id;
		const char *label;
		uint32_t fileCount;
		uint64_t bytesPerFile;
		uint32_t warmupIterations;
		uint32_t measuredIterations;
	};

	struct PreparedBenchmarkScenario
	{
		BenchmarkScenarioDefinition definition;
		std::vector<sunjwbase::tstring> filePaths;
	};

	static const HashAlgorithmSetDefinition kHashAlgorithmSets[] =
	{
		{ "openssl-sha-256", "SHA-256", { "openssl-sha-256", NULL, NULL, NULL }, 1 },
		{ "blake3-256", "BLAKE3-256", { "blake3-256", NULL, NULL, NULL }, 1 },
		{ "classic-4", "MD5+SHA1+SHA-256+SHA-512", { "md5", "sha1", "openssl-sha-256", "openssl-sha-512" }, 4 },
		{ "hybrid-4", "SHA-256+BLAKE3-256+BLAKE3-512+BLAKE3-XOF", { "openssl-sha-256", "blake3-256", "blake3-512", "blake3-xof" }, 4 }
	};

	static const BenchmarkScenarioDefinition kBenchmarkScenarios[] =
	{
		{ "small-single-64k", "1 file x 64 KiB", 1, kSmallFileBytes, 1, 3 },
		{ "many-small-256x64k", "256 files x 64 KiB", kManySmallFileCount, kSmallFileBytes, 1, 3 },
		{ "large-single-128m", "1 file x 128 MiB", 1, kLargeFileBytes, 1, 2 }
	};

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

	class ScopedBenchmarkTempDirectory
	{
	public:
		ScopedBenchmarkTempDirectory()
		{
			TCHAR tempPath[MAX_PATH] = { 0 };
			DWORD copied = ::GetTempPath(MAX_PATH, tempPath);
			if (copied == 0 || copied >= MAX_PATH)
			{
				throw NativeBenchmarkFailure("Unable to locate the Windows temp directory for native benchmarks.");
			}

			sunjwbase::tstring rootPath(tempPath);
			if (!rootPath.empty() && rootPath[rootPath.length() - 1] == _T('\\'))
			{
				rootPath.erase(rootPath.length() - 1);
			}

			rootPath += _T("\\lhash-native-benchmarks");
			::CreateDirectory(rootPath.c_str(), NULL);

			std::basic_ostringstream<TCHAR> pathBuilder;
			pathBuilder << rootPath << _T("\\run-") << ::GetCurrentProcessId() << _T('-') << ::GetTickCount64();
			rootPath_ = pathBuilder.str();
			if (!::CreateDirectory(rootPath_.c_str(), NULL))
			{
				DWORD error = ::GetLastError();
				if (error != ERROR_ALREADY_EXISTS)
				{
					throw NativeBenchmarkFailure("Unable to create the benchmark working directory.");
				}
			}
		}

		~ScopedBenchmarkTempDirectory()
		{
			for (size_t fileIndex = 0; fileIndex < files_.size(); ++fileIndex)
			{
				::DeleteFile(files_[fileIndex].c_str());
			}

			for (size_t directoryIndex = 0; directoryIndex < directories_.size(); ++directoryIndex)
			{
				::RemoveDirectory(directories_[directoryIndex].c_str());
			}

			if (!rootPath_.empty())
			{
				::RemoveDirectory(rootPath_.c_str());
			}
		}

		sunjwbase::tstring CreateSubDirectory(const sunjwbase::tstring& directoryName)
		{
			sunjwbase::tstring directoryPath = rootPath_ + _T("\\") + directoryName;
			if (!::CreateDirectory(directoryPath.c_str(), NULL))
			{
				DWORD error = ::GetLastError();
				if (error != ERROR_ALREADY_EXISTS)
				{
					throw NativeBenchmarkFailure("Unable to create a benchmark scenario directory.");
				}
			}

			directories_.push_back(directoryPath);
			return directoryPath;
		}

		sunjwbase::tstring WritePatternFile(const sunjwbase::tstring& directoryPath, const sunjwbase::tstring& fileName, uint64_t totalBytes, uint32_t seed)
		{
			sunjwbase::tstring filePath = directoryPath + _T("\\") + fileName;
			WinHandleGuard::UniqueWinHandle fileHandle(::CreateFile(filePath.c_str(), GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL));
			if (!fileHandle.isValid())
			{
				throw NativeBenchmarkFailure("Unable to create a benchmark input file.");
			}

			std::vector<unsigned char> patternBuffer(kWriteBufferBytes);
			for (size_t bufferIndex = 0; bufferIndex < patternBuffer.size(); ++bufferIndex)
			{
				patternBuffer[bufferIndex] = static_cast<unsigned char>((seed + static_cast<uint32_t>(bufferIndex * 17)) & 0xFFu);
			}

			uint64_t remainingBytes = totalBytes;
			while (remainingBytes > 0)
			{
				DWORD bytesThisWrite = static_cast<DWORD>(std::min<uint64_t>(remainingBytes, static_cast<uint64_t>(patternBuffer.size())));
				DWORD bytesWritten = 0;
				if (!::WriteFile(fileHandle.get(), &patternBuffer[0], bytesThisWrite, &bytesWritten, NULL) ||
					bytesWritten != bytesThisWrite)
				{
					throw NativeBenchmarkFailure("Unable to write a benchmark input file.");
				}

				remainingBytes -= bytesThisWrite;
			}

			files_.push_back(filePath);
			return filePath;
		}

	private:
		sunjwbase::tstring rootPath_;
		std::vector<sunjwbase::tstring> files_;
		std::vector<sunjwbase::tstring> directories_;
	};

	static std::string EscapeCsv(const std::string& value)
	{
		if (value.find_first_of("\",\r\n") == std::string::npos)
		{
			return value;
		}

		std::string escapedValue = "\"";
		for (size_t index = 0; index < value.length(); ++index)
		{
			if (value[index] == '"')
			{
				escapedValue += "\"\"";
			}
			else
			{
				escapedValue += value[index];
			}
		}

		escapedValue += "\"";
		return escapedValue;
	}

	static double CalculateMedianMilliseconds(std::vector<double> iterationMilliseconds)
	{
		if (iterationMilliseconds.empty())
		{
			return 0.0;
		}

		std::sort(iterationMilliseconds.begin(), iterationMilliseconds.end());
		size_t middleIndex = iterationMilliseconds.size() / 2;
		if ((iterationMilliseconds.size() % 2) == 0)
		{
			return (iterationMilliseconds[middleIndex - 1] + iterationMilliseconds[middleIndex]) / 2.0;
		}

		return iterationMilliseconds[middleIndex];
	}

	static double CalculateFastestMilliseconds(const std::vector<double>& iterationMilliseconds)
	{
		if (iterationMilliseconds.empty())
		{
			return 0.0;
		}

		return *std::min_element(iterationMilliseconds.begin(), iterationMilliseconds.end());
	}

	static std::string BuildFileName(uint32_t fileIndex)
	{
		std::ostringstream builder;
		builder << "file-";
		builder << std::setw(4) << std::setfill('0') << fileIndex;
		builder << ".bin";
		return builder.str();
	}

	static HashRequest BuildBenchmarkRequest(const PreparedBenchmarkScenario& scenario, const HashAlgorithmSetDefinition& algorithmSet)
	{
		HashRequest request;
		request.uppercaseDigest = false;
		request.digestExecutionPolicy = HASH_REQUEST_DIGEST_EXECUTION_POLICY_AUTO;
		request.files = scenario.filePaths;
		for (uint32_t algorithmIndex = 0; algorithmIndex < algorithmSet.algorithmCount; ++algorithmIndex)
		{
			AppendHashRequestAlgorithmId(request, NormalizeHashAlgorithmId(sunjwbase::strtotstr(std::string(algorithmSet.algorithmIds[algorithmIndex]))));
		}

		return request;
	}

	static void ValidateBenchmarkRun(const HashJobState& jobState, uint32_t expectedFileCount, uint32_t expectedAlgorithmCount)
	{
		if (jobState.results.size() != expectedFileCount)
		{
			throw NativeBenchmarkFailure("A benchmark run produced an unexpected number of file results.");
		}

		for (HashResultList::const_iterator resultIterator = jobState.results.begin(); resultIterator != jobState.results.end(); ++resultIterator)
		{
			if (resultIterator->state == RESULT_ERROR)
			{
				throw NativeBenchmarkFailure("A benchmark run produced an error result.");
			}

			if (resultIterator->digests.size() != expectedAlgorithmCount)
			{
				throw NativeBenchmarkFailure("A benchmark run produced an unexpected digest count.");
			}
		}
	}

	static PreparedBenchmarkScenario PrepareScenario(const BenchmarkScenarioDefinition& definition, ScopedBenchmarkTempDirectory& tempDirectory)
	{
		PreparedBenchmarkScenario preparedScenario;
		preparedScenario.definition = definition;
		preparedScenario.filePaths.reserve(definition.fileCount);

		sunjwbase::tstring scenarioDirectory = tempDirectory.CreateSubDirectory(sunjwbase::strtotstr(std::string(definition.id)));
		for (uint32_t fileIndex = 0; fileIndex < definition.fileCount; ++fileIndex)
		{
			std::string fileName = BuildFileName(fileIndex);
			preparedScenario.filePaths.push_back(
				tempDirectory.WritePatternFile(
					scenarioDirectory,
					sunjwbase::strtotstr(fileName),
					definition.bytesPerFile,
					static_cast<uint32_t>(fileIndex + 17)));
		}

		return preparedScenario;
	}

	static NativeBenchmarkCaseResult ExecuteBenchmarkCase(
		const NativeBenchmarkOptions& options,
		const PreparedBenchmarkScenario& scenario,
		const HashAlgorithmSetDefinition& algorithmSet)
	{
		HashRequest request = BuildBenchmarkRequest(scenario, algorithmSet);
		const uint64_t totalInputBytes = static_cast<uint64_t>(scenario.definition.fileCount) * scenario.definition.bytesPerFile;
		std::vector<double> measuredMilliseconds;
		measuredMilliseconds.reserve(scenario.definition.measuredIterations);

		for (uint32_t warmupIndex = 0; warmupIndex < scenario.definition.warmupIterations; ++warmupIndex)
		{
			HashJobState jobState;
			HashCancellationState cancellationState;
			HashExecutionContext executionContext = CreateHashExecutionContext(NULL, jobState, cancellationState);
			int exitCode = RunHashRequest(&executionContext, request);
			if (exitCode != 0)
			{
				throw NativeBenchmarkFailure("A benchmark warmup run failed.");
			}

			ValidateBenchmarkRun(jobState, scenario.definition.fileCount, algorithmSet.algorithmCount);
		}

		for (uint32_t measureIndex = 0; measureIndex < scenario.definition.measuredIterations; ++measureIndex)
		{
			HashJobState jobState;
			HashCancellationState cancellationState;
			HashExecutionContext executionContext = CreateHashExecutionContext(NULL, jobState, cancellationState);

			const std::chrono::steady_clock::time_point start = std::chrono::steady_clock::now();
			int exitCode = RunHashRequest(&executionContext, request);
			const std::chrono::steady_clock::time_point end = std::chrono::steady_clock::now();

			if (exitCode != 0)
			{
				throw NativeBenchmarkFailure("A measured benchmark run failed.");
			}

			ValidateBenchmarkRun(jobState, scenario.definition.fileCount, algorithmSet.algorithmCount);
			double elapsedMilliseconds = std::chrono::duration<double, std::milli>(end - start).count();
			measuredMilliseconds.push_back(elapsedMilliseconds);
		}

		const double totalInputMiB = static_cast<double>(totalInputBytes) / (1024.0 * 1024.0);
		const double medianMilliseconds = CalculateMedianMilliseconds(measuredMilliseconds);
		const double medianSeconds = medianMilliseconds / 1000.0;
		const double fastestMilliseconds = CalculateFastestMilliseconds(measuredMilliseconds);

		NativeBenchmarkCaseResult result;
		result.profileName = options.profileName;
		result.scenarioId = scenario.definition.id;
		result.scenarioLabel = scenario.definition.label;
		result.fileCount = scenario.definition.fileCount;
		result.bytesPerFile = scenario.definition.bytesPerFile;
		result.totalInputBytes = totalInputBytes;
		result.algorithmSetId = algorithmSet.id;
		result.algorithmSetLabel = algorithmSet.label;
		result.algorithmCount = algorithmSet.algorithmCount;
		result.warmupIterations = scenario.definition.warmupIterations;
		result.measuredIterations = scenario.definition.measuredIterations;
		result.medianMilliseconds = medianMilliseconds;
		result.fastestMilliseconds = fastestMilliseconds;
		result.inputMiBPerSecond = medianSeconds > 0.0 ? totalInputMiB / medianSeconds : 0.0;
		result.effectiveMiBPerSecond = medianSeconds > 0.0 ? (totalInputMiB * static_cast<double>(algorithmSet.algorithmCount)) / medianSeconds : 0.0;
		return result;
	}
}

int RunHashEngineBenchmarks(const NativeBenchmarkOptions& options, std::vector<NativeBenchmarkCaseResult> *results)
{
	if (results == NULL)
	{
		throw NativeBenchmarkFailure("The benchmark result sink must not be null.");
	}

	results->clear();
	ScopedHashAlgorithmRegistryReset registryReset;
	ScopedBenchmarkTempDirectory tempDirectory;

	std::vector<PreparedBenchmarkScenario> preparedScenarios;
	preparedScenarios.reserve(sizeof(kBenchmarkScenarios) / sizeof(kBenchmarkScenarios[0]));
	for (size_t scenarioIndex = 0; scenarioIndex < (sizeof(kBenchmarkScenarios) / sizeof(kBenchmarkScenarios[0])); ++scenarioIndex)
	{
		preparedScenarios.push_back(PrepareScenario(kBenchmarkScenarios[scenarioIndex], tempDirectory));
	}

	for (size_t scenarioIndex = 0; scenarioIndex < preparedScenarios.size(); ++scenarioIndex)
	{
		for (size_t algorithmSetIndex = 0; algorithmSetIndex < (sizeof(kHashAlgorithmSets) / sizeof(kHashAlgorithmSets[0])); ++algorithmSetIndex)
		{
			results->push_back(ExecuteBenchmarkCase(options, preparedScenarios[scenarioIndex], kHashAlgorithmSets[algorithmSetIndex]));
		}
	}

	return 0;
}

bool WriteNativeBenchmarkCsv(const std::string& csvOutputPath, const std::vector<NativeBenchmarkCaseResult>& results)
{
	std::ofstream output(csvOutputPath.c_str(), std::ios::out | std::ios::trunc);
	if (!output.good())
	{
		return false;
	}

	output << "Profile,ScenarioId,ScenarioLabel,FileCount,BytesPerFile,TotalInputBytes,AlgorithmSetId,AlgorithmSetLabel,AlgorithmCount,WarmupIterations,MeasuredIterations,MedianMilliseconds,FastestMilliseconds,InputMiBPerSecond,EffectiveMiBPerSecond\r\n";
	for (size_t resultIndex = 0; resultIndex < results.size(); ++resultIndex)
	{
		const NativeBenchmarkCaseResult& result = results[resultIndex];
		output
			<< EscapeCsv(result.profileName) << ','
			<< EscapeCsv(result.scenarioId) << ','
			<< EscapeCsv(result.scenarioLabel) << ','
			<< result.fileCount << ','
			<< result.bytesPerFile << ','
			<< result.totalInputBytes << ','
			<< EscapeCsv(result.algorithmSetId) << ','
			<< EscapeCsv(result.algorithmSetLabel) << ','
			<< result.algorithmCount << ','
			<< result.warmupIterations << ','
			<< result.measuredIterations << ','
			<< std::fixed << std::setprecision(3) << result.medianMilliseconds << ','
			<< std::fixed << std::setprecision(3) << result.fastestMilliseconds << ','
			<< std::fixed << std::setprecision(3) << result.inputMiBPerSecond << ','
			<< std::fixed << std::setprecision(3) << result.effectiveMiBPerSecond
			<< "\r\n";
	}

	return output.good();
}

