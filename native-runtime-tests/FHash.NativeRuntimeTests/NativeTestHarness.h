#pragma once

#include <functional>
#include <iostream>
#include <sstream>
#include <stdexcept>
#include <string>
#include <vector>

#include "Common/strhelper.h"

class NativeTestFailure : public std::runtime_error
{
public:
	explicit NativeTestFailure(const std::string& message)
		: std::runtime_error(message)
	{
	}
};

struct NativeTestCase
{
	const char *name;
	std::function<void()> body;
};

static inline std::string NativeTestToString(const sunjwbase::tstring& value)
{
	return sunjwbase::tstrtostr(value);
}

template<typename TValue>
static inline std::string NativeTestToString(const TValue& value)
{
	std::ostringstream stream;
	stream << value;
	return stream.str();
}

static inline void NativeAssertTrue(bool condition, const std::string& message)
{
	if (!condition)
	{
		throw NativeTestFailure(message);
	}
}

template<typename TValue>
static inline void NativeAssertEqual(const TValue& expected, const TValue& actual, const std::string& message)
{
	if (!(expected == actual))
	{
		throw NativeTestFailure(message + "\nExpected: " + NativeTestToString(expected) + "\nActual: " + NativeTestToString(actual));
	}
}

static inline void NativeAssertNotEmpty(const sunjwbase::tstring& value, const std::string& message)
{
	if (value.empty())
	{
		throw NativeTestFailure(message);
	}
}

static inline bool RunNativeTestCase(const NativeTestCase& testCase)
{
	try
	{
		testCase.body();
		std::cout << "PASS: " << testCase.name << std::endl;
		return true;
	}
	catch (const std::exception& ex)
	{
		std::cerr << "FAIL: " << testCase.name << std::endl;
		std::cerr << ex.what() << std::endl;
		return false;
	}
}
