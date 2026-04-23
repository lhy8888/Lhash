#include "Common/Global.h"
#include "Algorithms/MD5.h"
#include "Algorithms/SHA1.h"

#include <iostream>
#include <string>

namespace
{
    std::string ToUpperHex(const unsigned char* bytes, size_t byteCount)
    {
        static const char kHexDigits[] = "0123456789ABCDEF";
        std::string hex;
        hex.reserve(byteCount * 2);
        for (size_t index = 0; index < byteCount; ++index)
        {
            unsigned char byteValue = bytes[index];
            hex.push_back(kHexDigits[(byteValue >> 4) & 0x0F]);
            hex.push_back(kHexDigits[byteValue & 0x0F]);
        }
        return hex;
    }

    bool ExpectEqual(const char* label, const std::string& expected, const std::string& actual)
    {
        if (expected == actual)
        {
            std::cout << "[ok] " << label << '\n';
            return true;
        }

        std::cerr << "[fail] " << label << "\n  expected: " << expected << "\n  actual:   " << actual << '\n';
        return false;
    }

    std::string ComputeMd5Hex(const std::string& input)
    {
        MD5_CTX context;
        MD5Init(&context);
        if (!input.empty())
        {
            MD5Update(&context, reinterpret_cast<const unsigned char*>(input.data()), static_cast<unsigned int>(input.size()));
        }
        MD5Final(&context);
        return ToUpperHex(context.digest, sizeof(context.digest));
    }

    std::string ComputeSha1Hex(const std::string& input)
    {
        CSHA1 sha1;
        sha1.Reset();
        if (!input.empty())
        {
            sha1.Update(reinterpret_cast<unsigned char*>(const_cast<char*>(input.data())),
                static_cast<unsigned int>(input.size()));
        }
        sha1.Final();
        unsigned char digest[20] = { 0 };
        sha1.GetHash(digest);
        return ToUpperHex(digest, sizeof(digest));
    }

}

int main()
{
    bool passed = true;

    passed &= ExpectEqual("MD5 empty", "D41D8CD98F00B204E9800998ECF8427E", ComputeMd5Hex(""));
    passed &= ExpectEqual("MD5 abc", "900150983CD24FB0D6963F7D28E17F72", ComputeMd5Hex("abc"));

    passed &= ExpectEqual("SHA1 empty", "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709", ComputeSha1Hex(""));
    passed &= ExpectEqual("SHA1 abc", "A9993E364706816ABA3E25717850C26C9CD0D89D", ComputeSha1Hex("abc"));

    if (!passed)
    {
        std::cerr << "lhash_core_smoke failed." << std::endl;
        return 1;
    }

    std::cout << "lhash_core_smoke passed." << std::endl;
    return 0;
}
