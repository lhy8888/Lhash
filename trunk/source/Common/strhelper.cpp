/*
 * strhelper implementation file
 * Author: Sun Junwen
 * Version: 2.2.4
 * Provides converting from tstring, string and wstring to each other
 * And provides string's utf8 converting.
 * Provides triming function to string and wstring.
 * Provides replacing function to string and wstring.
 * Provides newline fixing to string.
 */
#include "stdafx.h"

#include <climits>
#include <cctype>
#include <string>

#if defined (_WIN32)
#include "Windows.h"
#endif

#if defined (__APPLE__) || defined (__unix)
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <iconv.h>
#include <errno.h>
#include <iostream>
#include <locale>
#include <codecvt>
#include <vector>
#endif

#include "strhelper.h"

namespace sunjwbase
{
#if defined (_WIN32)
	// Windows convert
	static std::string _wstrtostr(const std::wstring& wstr, UINT codePage)
	{
		if (wstr.empty() || wstr.size() > static_cast<size_t>(INT_MAX))
			return std::string();

		const int inputLength = static_cast<int>(wstr.size());
		const DWORD flags = codePage == CP_UTF8 ? WC_ERR_INVALID_CHARS : 0;
		const int outputLength = WideCharToMultiByte(codePage, flags, wstr.data(), inputLength, NULL, 0, NULL, NULL);
		if (outputLength <= 0)
			return std::string();

		std::string result(static_cast<size_t>(outputLength), '\0');
		if (WideCharToMultiByte(codePage, flags, wstr.data(), inputLength,
			&result[0], outputLength, NULL, NULL) != outputLength)
			return std::string();
		return result;
	}

	static std::wstring _strtowstr(const std::string& str, UINT codePage)
	{
		if (str.empty() || str.size() > static_cast<size_t>(INT_MAX))
			return std::wstring();

		const int inputLength = static_cast<int>(str.size());
		const DWORD flags = codePage == CP_UTF8 ? MB_ERR_INVALID_CHARS : 0;
		const int outputLength = MultiByteToWideChar(codePage, flags, str.data(), inputLength, NULL, 0);
		if (outputLength <= 0)
			return std::wstring();

		std::wstring result(static_cast<size_t>(outputLength), L'\0');
		if (MultiByteToWideChar(codePage, flags, str.data(), inputLength,
			&result[0], outputLength) != outputLength)
			return std::wstring();
		return result;
	}
#endif
}

#if defined (__APPLE__) || defined (__unix)
namespace
{
	static bool ContainsOnlyUnicodeScalars(const std::wstring& input)
	{
		for (wchar_t value : input)
		{
			if (value < 0 || value > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF))
				return false;
		}
		return true;
	}

	static std::string ConvertWideToUtf8(const std::wstring& input)
	{
		if (!ContainsOnlyUnicodeScalars(input))
			return std::string();
		try
		{
			std::wstring_convert<std::codecvt_utf8<wchar_t>> converter;
			std::string result = converter.to_bytes(input);
			return converter.converted() == input.size() ? result : std::string();
		}
		catch (const std::range_error&)
		{
			return std::string();
		}
	}

	static std::wstring ConvertUtf8ToWide(const std::string& input)
	{
		try
		{
			std::wstring_convert<std::codecvt_utf8<wchar_t>> converter;
			std::wstring result = converter.from_bytes(input);
			return converter.converted() == input.size() && ContainsOnlyUnicodeScalars(result) ? result : std::wstring();
		}
		catch (const std::range_error&)
		{
			return std::wstring();
		}
	}
}

std::string sunjwbase::striconv(const std::string& input,
								const std::string& to_code,
								const std::string& from_code)
{
	if (input.empty() || to_code == from_code)
	{
		return input;
	}

	iconv_t cd = iconv_open(to_code.c_str(), from_code.c_str());
	if (cd == (iconv_t)(-1))
	{
		return input;
	}

	std::vector<char> inputBuffer(input.begin(), input.end());
	size_t inleft = inputBuffer.size();
	char* in = inputBuffer.empty() ? NULL : inputBuffer.data();

	size_t outputCapacity = inputBuffer.empty() ? 1 : inputBuffer.size() * 4 + 1;
	std::vector<char> outputBuffer(outputCapacity, '\0');
	char* out = outputBuffer.data();
	size_t outleft = outputBuffer.size() - 1;

	for (;;)
	{
		size_t iconvResult = iconv(cd, inleft > 0 ? &in : NULL, &inleft, &out, &outleft);
		if (iconvResult != static_cast<size_t>(-1))
		{
			break;
		}

		if (errno == E2BIG)
		{
			size_t bytesWritten = static_cast<size_t>(out - outputBuffer.data());
			outputCapacity *= 2;
			outputBuffer.resize(outputCapacity, '\0');
			out = outputBuffer.data() + bytesWritten;
			outleft = outputBuffer.size() - bytesWritten - 1;
			continue;
		}

		iconv_close(cd);
		return input;
	}

	iconv_close(cd);
	return std::string(outputBuffer.data(), static_cast<size_t>(out - outputBuffer.data()));
}
#endif

/*
 * convert wstring to utf8 encoded string
 */
std::string sunjwbase::wstrtostrutf8(const std::wstring& wstr)
{
	// Convert a wstring to an utf8 encoded string
#if defined (_WIN32)
	return _wstrtostr(wstr, CP_UTF8);
#endif
#if defined (__APPLE__) || defined (__unix)
	return ConvertWideToUtf8(wstr);
#endif
}

/*
 * convert utf8 encoded string to wstring
 * 1st, convert utf8 encoded string to native string
 * 2nd, call strtowstr
 */
std::wstring sunjwbase::strtowstrutf8(const std::string& str)
{
	// Convert an utf8 encoded string to a wstring
#if defined (_WIN32)
	return _strtowstr(str, CP_UTF8);
#endif
#if defined (__APPLE__) || defined (__unix)
	return ConvertUtf8ToWide(str);
#endif
}

std::string sunjwbase::wstrtostr(const std::wstring& wstr)
{
	// Convert a wstring to an string
#if defined (_WIN32)
	return _wstrtostr(wstr, CP_ACP);
#endif
#if defined (__APPLE__) || defined (__unix)
	return ConvertWideToUtf8(wstr);
#endif
}

std::wstring sunjwbase::strtowstr(const std::string& str)
{
	// Convert an string to a wtring
#if defined (_WIN32)
	return _strtowstr(str, CP_ACP);
#endif
#if defined (__APPLE__) || defined (__unix)
	return ConvertUtf8ToWide(str);
#endif
}

std::string sunjwbase::strtrim_right(const std::string& s, const std::string& spaces)
{
	std::string d(s);
	std::string::size_type i(d.find_last_not_of(spaces));
	if (i == std::string::npos)
	{
		return "";
	}
	else
	{
		return d.erase(d.find_last_not_of(spaces) + 1);
	}
}  // end of trim_right

std::string sunjwbase::strtrim_left(const std::string& s, const std::string& spaces)
{
	std::string d(s);
	return d.erase(0, s.find_first_not_of(spaces));
}  // end of trim_left

std::wstring sunjwbase::strtrim_right(const std::wstring& s, const std::wstring& spaces)
{
	std::wstring d(s);
	std::wstring::size_type i(d.find_last_not_of(spaces));
	if (i == std::wstring::npos)
	{
		return L"";
	}
	else
	{
		return d.erase(d.find_last_not_of(spaces) + 1);
	}
}

std::wstring sunjwbase::strtrim_left(const std::wstring& s, const std::wstring& spaces)
{
	std::wstring d(s);
	return d.erase(0, s.find_first_not_of(spaces));
}

std::string sunjwbase::strreplace(const std::string& base, const std::string& src, const std::string& des)
{
	if (src.empty())
		return base;
	std::string ret(base);
	std::string::size_type pos = 0;
	std::string::size_type srcLen = src.size();
	std::string::size_type desLen = des.size();
	pos = ret.find(src, pos);
	while ((pos != std::string::npos))
	{
		ret.replace(pos, srcLen, des);
		pos = ret.find(src, pos + desLen);
	}

	return ret;
}

std::wstring sunjwbase::strreplace(const std::wstring& base, const std::wstring& src, const std::wstring& des)
{
	if (src.empty())
		return base;
	std::wstring ret(base);
	std::wstring::size_type pos = 0;
	std::wstring::size_type srcLen = src.size();
	std::wstring::size_type desLen = des.size();
	pos = ret.find(src, pos);
	while ((pos != std::wstring::npos))
	{
		ret.replace(pos, srcLen, des);
		pos = ret.find(src, pos + desLen);
	}

	return ret;
}

std::string sunjwbase::fixnewline(const std::string& str)
{
	std::string ret;
	std::string strRepairSrc(""), strRepairDst("");
	// convert new line, /10->/13/10, /13/13/10->/13/10
	strRepairSrc += (char)10;
	strRepairDst += (char)13;
	strRepairDst += (char)10;
	ret = strreplace(str, strRepairSrc, strRepairDst);
	strRepairSrc = (char)13;
	strRepairSrc += (char)13;
	strRepairSrc += (char)10;
	ret = strreplace(ret, strRepairSrc, strRepairDst);
	// convert new line, /13->/13/10, /13/10/10->/13/10
	strRepairSrc = "";
	strRepairDst = "";
	strRepairSrc += (char)13;
	strRepairDst += (char)13;
	strRepairDst += (char)10;
	ret = strreplace(ret, strRepairSrc, strRepairDst);
	strRepairSrc = (char)13;
	strRepairSrc += (char)10;
	strRepairSrc += (char)10;
	ret = strreplace(ret, strRepairSrc, strRepairDst);
	return ret;
}

std::string sunjwbase::str_upper(const std::string& str)
{
	std::string ret(str);
	std::transform(ret.begin(), ret.end(), ret.begin(),
		[](unsigned char value) { return static_cast<char>(std::toupper(value)); });
	return ret;
}

std::string sunjwbase::str_lower(const std::string& str)
{
	std::string ret(str);
	std::transform(ret.begin(), ret.end(), ret.begin(),
		[](unsigned char value) { return static_cast<char>(std::tolower(value)); });
	return ret;
}

std::string sunjwbase::itostr(int num, int idx /* = 10 */)
{
	if (idx < 2 || idx > 16)
		return std::string();

	static const char digits[] = "0123456789ABCDEF";
	const bool negative = num < 0;
	unsigned int magnitude = negative ? 0u - static_cast<unsigned int>(num) : static_cast<unsigned int>(num);
	std::string result;
	do
	{
		result.push_back(digits[magnitude % static_cast<unsigned int>(idx)]);
		magnitude /= static_cast<unsigned int>(idx);
	} while (magnitude != 0);
	if (negative)
		result.push_back('-');
	std::reverse(result.begin(), result.end());
	return result;
}

std::string sunjwbase::strappendformat(std::string& str, const char *format, ...)
{
	int size = 100;
	std::string temp;
	va_list vl;
	while (1)
	{
		temp.resize(size + 1); // for null terminator
		va_start(vl, format);
#if defined (_WIN32)
		int n = vsnprintf_s((char *)temp.data(), size, _TRUNCATE, format, vl);
#else
		int n = vsnprintf((char *)temp.data(), size, format, vl);
#endif
		va_end(vl);
		if (n > -1 && n < size) {
			// temp.resize(n);
			break;
		}
		if (n > -1)
			size = n + 1; // not large enough
		else
			size *= 2;
	}
	str.append(temp.c_str());

	return str;
}

bool sunjwbase::str_startwith(const std::string& str, const std::string& target)
{
	if (str.length() < target.length())
		return false;

	// Length is enough, let's check content.
	size_t i = 0;
	for (; i < target.length(); ++i)
	{
		if (str[i] != target[i])
			return false;
	}

	return (i == target.length());
}

bool sunjwbase::str_endwith(const std::string& str, const std::string& target)
{
	if (str.length() < target.length())
		return false;

	// Length is enough, let's check content.
	size_t str_len = str.length();
	size_t target_len = target.length();
	size_t i = 0;
	for (; i < target.length(); ++i)
	{
		if (str[str_len - target_len + i] != target[i])
			return false;
	}

	return (i == target.length());
}
