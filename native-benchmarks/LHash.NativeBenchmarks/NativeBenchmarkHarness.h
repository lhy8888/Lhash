#ifndef _NATIVE_BENCHMARK_HARNESS_H_
#define _NATIVE_BENCHMARK_HARNESS_H_

#include <stdint.h>

#include <stdexcept>
#include <string>
#include <vector>

struct NativeBenchmarkOptions
{
	std::string profileName;
	std::string csvOutputPath;
};

struct NativeBenchmarkCaseResult
{
	std::string profileName;
	std::string scenarioId;
	std::string scenarioLabel;
	uint32_t fileCount;
	uint64_t bytesPerFile;
	uint64_t totalInputBytes;
	std::string algorithmSetId;
	std::string algorithmSetLabel;
	uint32_t algorithmCount;
	uint32_t warmupIterations;
	uint32_t measuredIterations;
	double medianMilliseconds;
	double fastestMilliseconds;
	double inputMiBPerSecond;
	double effectiveMiBPerSecond;
};

class NativeBenchmarkFailure : public std::runtime_error
{
public:
	explicit NativeBenchmarkFailure(const std::string& message)
		: std::runtime_error(message)
	{
	}
};

int RunHashEngineBenchmarks(const NativeBenchmarkOptions& options, std::vector<NativeBenchmarkCaseResult> *results);
bool WriteNativeBenchmarkCsv(const std::string& csvOutputPath, const std::vector<NativeBenchmarkCaseResult>& results);

#endif
