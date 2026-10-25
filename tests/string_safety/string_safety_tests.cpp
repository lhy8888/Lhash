#include "stdafx.h"
#include "Algorithms/SHA1.h"
#include "Common/strhelper.h"

#include <climits>
#include <clocale>
#include <iostream>
#include <random>
#include <stdexcept>
#include <string>
#include <vector>

static void Require(bool condition, const char* message)
{
    if (!condition)
        throw std::runtime_error(message);
}

static void TestSha1Formatting()
{
    CSHA1 sha1;
    unsigned char input[] = { 'a', 'b', 'c' };
    sha1.Update(input, 3);
    sha1.Final();
    char guardedHex[43] = {};
    guardedHex[0] = guardedHex[42] = '!';
    sha1.ReportHash(guardedHex + 1);
    Require(std::string(guardedHex + 1) == "A9993E364706816ABA3E25717850C26C9CD0D89D", "SHA1 report abc");
    Require(guardedHex[0] == '!' && guardedHex[42] == '!', "SHA1 hex bounds");

    unsigned char raw[20];
    sha1.GetHash(raw);
    std::string expectedDecimal;
    for (unsigned char value : raw)
        expectedDecimal += std::to_string(value);
    char guardedDecimal[63] = {};
    guardedDecimal[0] = guardedDecimal[62] = '!';
    sha1.ReportHash(guardedDecimal + 1, CSHA1::REPORT_DIGIT);
    Require(guardedDecimal + 1 == expectedDecimal, "SHA1 decimal report");
    Require(guardedDecimal[0] == '!' && guardedDecimal[62] == '!', "SHA1 decimal bounds");
    expectedDecimal.clear();
    for (unsigned char& value : sha1.m_digest)
    {
        value = 255;
        expectedDecimal += "255";
    }
    sha1.ReportHash(guardedDecimal + 1, CSHA1::REPORT_DIGIT);
    Require(guardedDecimal + 1 == expectedDecimal && guardedDecimal[61] == '\0', "SHA1 maximum decimal length");
    Require(guardedDecimal[0] == '!' && guardedDecimal[62] == '!', "SHA1 maximum decimal bounds");
    sha1.ReportHash(nullptr);
}

static void AppendScalar(unsigned int scalar, std::string& utf8, std::wstring& wide)
{
    if (scalar < 0x80)
        utf8.push_back(static_cast<char>(scalar));
    else if (scalar < 0x800)
    {
        utf8.push_back(static_cast<char>(0xC0 | (scalar >> 6)));
        utf8.push_back(static_cast<char>(0x80 | (scalar & 63)));
    }
    else if (scalar < 0x10000)
    {
        utf8.push_back(static_cast<char>(0xE0 | (scalar >> 12)));
        utf8.push_back(static_cast<char>(0x80 | ((scalar >> 6) & 63)));
        utf8.push_back(static_cast<char>(0x80 | (scalar & 63)));
    }
    else
    {
        utf8.push_back(static_cast<char>(0xF0 | (scalar >> 18)));
        utf8.push_back(static_cast<char>(0x80 | ((scalar >> 12) & 63)));
        utf8.push_back(static_cast<char>(0x80 | ((scalar >> 6) & 63)));
        utf8.push_back(static_cast<char>(0x80 | (scalar & 63)));
    }
    if (sizeof(wchar_t) == 2 && scalar >= 0x10000)
    {
        scalar -= 0x10000;
        wide.push_back(static_cast<wchar_t>(0xD800 | (scalar >> 10)));
        wide.push_back(static_cast<wchar_t>(0xDC00 | (scalar & 1023)));
    }
    else
        wide.push_back(static_cast<wchar_t>(scalar));
}

static void TestConversions()
{
    Require(sunjwbase::wstrtostrutf8(L"").empty(), "empty wide input");
    Require(sunjwbase::strtowstrutf8("").empty(), "empty UTF8 input");
    Require(sunjwbase::wstrtostr(L"SHA256") == "SHA256", "native ASCII output");
    Require(sunjwbase::strtowstr("SHA256") == L"SHA256", "native ASCII input");
    const std::string nulBytes("a\0b", 3);
    const std::wstring nulWide(L"a\0b", 3);
    Require(sunjwbase::wstrtostr(nulWide) == nulBytes, "native embedded NUL output");
    Require(sunjwbase::strtowstr(nulBytes) == nulWide, "native embedded NUL input");

    std::string utf8;
    std::wstring wide;
    for (unsigned int scalar : { 0u, 0x7Fu, 0x80u, 0x7FFu, 0x800u, 0xD7FFu, 0xE000u,
        0xFFFFu, 0x10000u, 0x6821u, 0x9A8Cu, 0x1F600u, 0x10FFFFu })
        AppendScalar(scalar, utf8, wide);
    Require(sunjwbase::wstrtostrutf8(wide) == utf8, "UTF8 boundary output");
    Require(sunjwbase::strtowstrutf8(utf8) == wide, "UTF8 boundary input");

    std::mt19937 random(0x359u);
    for (int round = 0; round < 64; ++round)
    {
        utf8.clear();
        wide.clear();
        for (int i = 0; i < 257; ++i)
        {
            unsigned int scalar;
            do { scalar = random() % 0x110000; } while (scalar >= 0xD800 && scalar <= 0xDFFF);
            AppendScalar(scalar, utf8, wide);
        }
        Require(sunjwbase::wstrtostrutf8(wide) == utf8, "random Unicode output");
        Require(sunjwbase::strtowstrutf8(utf8) == wide, "random Unicode input");
    }

    for (const std::string& invalid : { std::string("\x80"), std::string("\xC0\xAF"),
        std::string("\xED\xA0\x80"), std::string("\xF4\x90\x80\x80"),
        std::string("ok\xE4\xB8"), std::string("ok\xFF") })
        Require(sunjwbase::strtowstrutf8(invalid).empty(), "invalid UTF8 accepted or partially decoded");
    Require(sunjwbase::wstrtostrutf8(std::wstring(1, static_cast<wchar_t>(0xD800))).empty(), "unpaired high surrogate");
    Require(sunjwbase::wstrtostrutf8(std::wstring(1, static_cast<wchar_t>(0xDFFF))).empty(), "unpaired low surrogate");
    if (sizeof(wchar_t) > 2)
        Require(sunjwbase::wstrtostrutf8(std::wstring(1, static_cast<wchar_t>(0x110000))).empty(), "out-of-range Unicode");
}

static void TestStringEdges()
{
    std::setlocale(LC_CTYPE, "C");
    Require(sunjwbase::str_upper("a1Z") == "A1Z", "uppercase ASCII");
    Require(sunjwbase::str_lower("a1Z") == "a1z", "lowercase ASCII");
    std::string highBytes;
    for (unsigned int value = 128; value <= 255; ++value)
        highBytes.push_back(static_cast<char>(value));
    Require(sunjwbase::str_lower(highBytes) == highBytes, "high byte lowercase");
    Require(sunjwbase::str_upper(highBytes) == highBytes, "high byte uppercase");
    Require(sunjwbase::strreplace("abc", "", "x") == "abc", "empty replacement pattern");
    Require(sunjwbase::strreplace(L"abc", L"", L"x") == L"abc", "empty wide replacement pattern");
    Require(sunjwbase::strreplace("aa-aa", "aa", "aaa") == "aaa-aaa", "replacement makes progress");
    Require(sunjwbase::itostr(INT_MIN) == std::to_string(INT_MIN), "INT_MIN formatting");
    Require(sunjwbase::itostr(INT_MAX) == std::to_string(INT_MAX), "INT_MAX formatting");
    Require(sunjwbase::itostr(-255, 16) == "-FF", "negative hex formatting");
    Require(sunjwbase::itostr(0, 2) == "0", "zero formatting");
    for (int radix : { -1, 0, 1, 17 })
        Require(sunjwbase::itostr(9, radix).empty(), "invalid radix");
}

int main()
{
    try
    {
        TestSha1Formatting();
        TestConversions();
        TestStringEdges();
        std::cout << "PASS SHA1 formatting, Unicode boundaries and 64 random roundtrips, malformed inputs, string edges\n";
        return 0;
    }
    catch (const std::exception& error)
    {
        std::cerr << "FAIL " << error.what() << '\n';
        return 1;
    }
}
