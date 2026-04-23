#include "Common/Global.h"
#include "Algorithms/MD5.h"
#include "Algorithms/SHA1.h"
#include "Runtime/Hash/BLAKE3HashProvider.h"
#include "Runtime/Hash/CRC32CHashProvider.h"
#include "Runtime/Hash/XXHash3HashProvider.h"

#include <cstdint>
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

    std::string ToUpperHex(const sunjwbase::tstring& text)
    {
        return sunjwbase::tstrtostr(text);
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

    std::string ComputeBlake3Hex(const std::string& input, size_t outputBytes)
    {
        blake3_hasher hasher;
        HashRuntime::InitializeBlake3Hasher(&hasher);
        if (!input.empty())
        {
            HashRuntime::UpdateBlake3Hasher(hasher, reinterpret_cast<const unsigned char*>(input.data()), input.size());
        }
        return ToUpperHex(HashRuntime::FinalizeBlake3HasherHex(hasher, outputBytes));
    }

    std::string ComputeXXH3_64Hex(const std::string& input)
    {
        XXH3_state_t hasher;
        HashRuntime::InitializeXXH3_64Hasher(&hasher);
        if (!input.empty())
        {
            HashRuntime::UpdateXXH3_64Hasher(hasher, reinterpret_cast<const unsigned char*>(input.data()), input.size());
        }
        return ToUpperHex(HashRuntime::FinalizeXXH3_64HasherHex(hasher));
    }

    std::string ComputeXXH3_128Hex(const std::string& input)
    {
        XXH3_state_t hasher;
        HashRuntime::InitializeXXH3_128Hasher(&hasher);
        if (!input.empty())
        {
            HashRuntime::UpdateXXH3_128Hasher(hasher, reinterpret_cast<const unsigned char*>(input.data()), input.size());
        }
        return ToUpperHex(HashRuntime::FinalizeXXH3_128HasherHex(hasher));
    }

    std::string ComputeCRC32CHex(const std::string& input)
    {
        uint32_t hasher = 0;
        HashRuntime::InitializeCRC32CHasher(&hasher);
        if (!input.empty())
        {
            HashRuntime::UpdateCRC32CHasher(hasher, reinterpret_cast<const unsigned char*>(input.data()), input.size());
        }
        return ToUpperHex(HashRuntime::FinalizeCRC32CHasherHex(hasher));
    }

    std::string CreateOfficialXXH3SanityInput(size_t inputLength)
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

    std::string CreateOfficialCRC32CIscsiInput()
    {
        static const unsigned char kBytes[] = {
            0x01, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00,
            0x00, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00, 0x18,
            0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        return std::string(reinterpret_cast<const char*>(kBytes), sizeof(kBytes));
    }
}

int main()
{
    bool passed = true;

    passed &= ExpectEqual("MD5 empty", "D41D8CD98F00B204E9800998ECF8427E", ComputeMd5Hex(""));
    passed &= ExpectEqual("MD5 abc", "900150983CD24FB0D6963F7D28E17F72", ComputeMd5Hex("abc"));

    passed &= ExpectEqual("SHA1 empty", "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709", ComputeSha1Hex(""));
    passed &= ExpectEqual("SHA1 abc", "A9993E364706816ABA3E25717850C26C9CD0D89D", ComputeSha1Hex("abc"));

    const std::string blake3Input("\x00\x01\x02", 3);
    passed &= ExpectEqual("BLAKE3-256", "E1BE4D7A8AB5560AA4199EEA339849BA8E293D55CA0A81006726D184519E647F", ComputeBlake3Hex(blake3Input, HashRuntime::BLAKE3_256_OUTPUT_BYTES));
    passed &= ExpectEqual("BLAKE3-512", "E1BE4D7A8AB5560AA4199EEA339849BA8E293D55CA0A81006726D184519E647F5B49B82F805A538C68915C1AE8035C900FD1D4B13902920FD05E1450822F36DE", ComputeBlake3Hex(blake3Input, HashRuntime::BLAKE3_512_OUTPUT_BYTES));
    passed &= ExpectEqual("BLAKE3 XOF 128", "E1BE4D7A8AB5560AA4199EEA339849BA8E293D55CA0A81006726D184519E647F5B49B82F805A538C68915C1AE8035C900FD1D4B13902920FD05E1450822F36DE9454B7E9996DE4900C8E723512883F93F4345F8A58BFE64EE38D3AD71AB027765D25CDD0E448328A8E7A683B9A6AF8B0AF94FA09010D9186890B096A08471E42", ComputeBlake3Hex(blake3Input, HashRuntime::BLAKE3_XOF_OUTPUT_BYTES));

    const std::string xxh3Input = CreateOfficialXXH3SanityInput(3);
    passed &= ExpectEqual("XXH3-64", "54247382A8D6B94D", ComputeXXH3_64Hex(xxh3Input));
    passed &= ExpectEqual("XXH3-128", "20EFC49FF02422EA54247382A8D6B94D", ComputeXXH3_128Hex(xxh3Input));

    passed &= ExpectEqual("CRC32C ascending", "46DD794E", ComputeCRC32CHex(std::string("\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09\x0A\x0B\x0C\x0D\x0E\x0F\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1A\x1B\x1C\x1D\x1E\x1F", 32)));
    passed &= ExpectEqual("CRC32C zeros", "8A9136AA", ComputeCRC32CHex(std::string(32, '\0')));
    passed &= ExpectEqual("CRC32C ff", "62A8AB43", ComputeCRC32CHex(std::string(32, static_cast<char>(0xFF))));
    passed &= ExpectEqual("CRC32C descending", "113FDB5C", ComputeCRC32CHex(std::string("\x1F\x1E\x1D\x1C\x1B\x1A\x19\x18\x17\x16\x15\x14\x13\x12\x11\x10\x0F\x0E\x0D\x0C\x0B\x0A\x09\x08\x07\x06\x05\x04\x03\x02\x01\x00", 32)));
    passed &= ExpectEqual("CRC32C iSCSI", "D9963A56", ComputeCRC32CHex(CreateOfficialCRC32CIscsiInput()));

    if (!passed)
    {
        std::cerr << "lhash_core_smoke failed." << std::endl;
        return 1;
    }

    std::cout << "lhash_core_smoke passed." << std::endl;
    return 0;
}
