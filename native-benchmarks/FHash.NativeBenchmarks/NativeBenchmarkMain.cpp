#include "..\..\trunk\source\stdafx.h"

#include <cwchar>
#include <iostream>
#include <vector>

#include "NativeBenchmarkHarness.h"
#include "Common/strhelper.h"

namespace
{
	static bool TryConsumeOption(int argc, wchar_t *argv[], int *argumentIndex, const wchar_t *optionName, std::string *value)
	{
		if (*argumentIndex >= argc)
		{
			return false;
		}

		if (wcscmp(argv[*argumentIndex], optionName) != 0)
		{
			return false;
		}

		if (*argumentIndex + 1 >= argc)
		{
			throw NativeBenchmarkFailure("Missing benchmark option value.");
		}

		if (value != NULL)
		{
			*value = sunjwbase::tstrtostr(argv[*argumentIndex + 1]);
		}

		*argumentIndex += 2;
		return true;
	}
}

int wmain(int argc, wchar_t *argv[])
{
	::SetConsoleOutputCP(CP_UTF8);

	try
	{
		NativeBenchmarkOptions options;
		options.profileName = "current";

		for (int argumentIndex = 1; argumentIndex < argc;)
		{
			if (TryConsumeOption(argc, argv, &argumentIndex, L"--profile", &options.profileName))
			{
				continue;
			}

			if (TryConsumeOption(argc, argv, &argumentIndex, L"--csv", &options.csvOutputPath))
			{
				continue;
			}

			throw NativeBenchmarkFailure("Unknown benchmark command-line option.");
		}

		std::vector<NativeBenchmarkCaseResult> results;
		int runExitCode = RunHashEngineBenchmarks(options, &results);
		if (runExitCode != 0)
		{
			return runExitCode;
		}

		if (!options.csvOutputPath.empty() && !WriteNativeBenchmarkCsv(options.csvOutputPath, results))
		{
			throw NativeBenchmarkFailure("Unable to write the benchmark CSV output.");
		}

		std::cout << "Native benchmark cases completed: " << results.size() << std::endl;
		for (size_t resultIndex = 0; resultIndex < results.size(); ++resultIndex)
		{
			const NativeBenchmarkCaseResult& result = results[resultIndex];
			std::cout
				<< result.profileName
				<< " | " << result.scenarioId
				<< " | " << result.algorithmSetId
				<< " | median_ms=" << result.medianMilliseconds
				<< " | input_mib_s=" << result.inputMiBPerSecond
				<< " | effective_mib_s=" << result.effectiveMiBPerSecond
				<< std::endl;
		}

		return 0;
	}
	catch (const std::exception& ex)
	{
		std::cerr << "Native benchmark failed: " << ex.what() << std::endl;
		return 1;
	}
}
