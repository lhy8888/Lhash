#include "..\..\trunk\source\stdafx.h"

#include <iostream>
#include <cstring>
#include <vector>

#include "NativeTestHarness.h"

#if defined(LHASH_WITH_OPENSSL3_VENDOR)
#include <openssl/crypto.h>
#include <openssl/opensslv.h>
#endif

void RegisterHashEngineRuntimeTests(std::vector<NativeTestCase>& tests);
void RegisterHashEngineSecurityRuntimeTests(std::vector<NativeTestCase>& tests);

int main(int argc, char **argv)
{
	::SetConsoleOutputCP(CP_UTF8);

#if defined(_M_ARM64)
	const char *architecture = "ARM64";
#elif defined(_M_X64)
	const char *architecture = "x64";
#else
	const char *architecture = "unknown";
#endif
	std::cout << "Native runtime test target: " << architecture << std::endl;
#if defined(LHASH_WITH_OPENSSL3_VENDOR)
	std::cout << "Native runtime OpenSSL: " << OpenSSL_version(OPENSSL_VERSION) << std::endl;
#else
	std::cout << "Native runtime OpenSSL: disabled" << std::endl;
#endif
	for (int index = 1; index < argc; ++index)
	{
		if (std::strcmp(argv[index], "--expected-architecture") == 0 && index + 1 < argc)
		{
			if (std::strcmp(argv[++index], architecture) != 0)
			{
				std::cerr << "The executable architecture does not match the requested test target." << std::endl;
				return 2;
			}
		}
		else if (std::strcmp(argv[index], "--require-openssl") == 0)
		{
#if !defined(LHASH_WITH_OPENSSL3_VENDOR)
			std::cerr << "The test matrix requires the OpenSSL vendor provider to be enabled." << std::endl;
			return 2;
#endif
		}
		else
		{
			std::cerr << "Usage: LHash.NativeRuntimeTests [--expected-architecture x64|ARM64] [--require-openssl]" << std::endl;
			return 2;
		}
	}

	std::vector<NativeTestCase> tests;
	RegisterHashEngineRuntimeTests(tests);
	RegisterHashEngineSecurityRuntimeTests(tests);
	std::cout << "Native runtime tests discovered: " << tests.size() << std::endl;

	size_t passedCount = 0;
	for (size_t testIndex = 0; testIndex < tests.size(); ++testIndex)
	{
		std::cout << "RUN: " << tests[testIndex].name << std::endl;
		if (RunNativeTestCase(tests[testIndex]))
		{
			++passedCount;
		}
	}

	if (passedCount != tests.size())
	{
		std::cerr << "Native runtime tests failed: " << passedCount << "/" << tests.size() << " passed." << std::endl;
		return 1;
	}

	std::cout << "All native runtime tests passed: " << passedCount << "/" << tests.size() << std::endl;
	return 0;
}
