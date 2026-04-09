#include "..\..\trunk\source\stdafx.h"

#include <iostream>
#include <vector>

#include "NativeTestHarness.h"

void RegisterHashEngineRuntimeTests(std::vector<NativeTestCase>& tests);
void RegisterHashEngineSecurityRuntimeTests(std::vector<NativeTestCase>& tests);

int main()
{
	::SetConsoleOutputCP(CP_UTF8);

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
