#include "Runtime/Hash/OpenSslEvpHashProvider.h"
#include <openssl/crypto.h>
#include <openssl/opensslv.h>
#include <algorithm>
#include <atomic>
#include <iostream>
#include <fstream>
#include <filesystem>
#include <random>
#include <stdexcept>
#include <string>
#include <thread>
#include <limits>
#include <type_traits>
#include <vector>
static_assert(OPENSSL_VERSION_MAJOR == 3 && OPENSSL_VERSION_MINOR == 5 && OPENSSL_VERSION_PATCH == 9, "Wrong OpenSSL headers");
static_assert(!std::is_copy_constructible<HashRuntime::OpenSslEvpHashContext>::value, "EVP ownership cannot be copied");
static_assert(!std::is_copy_assignable<HashRuntime::OpenSslEvpHashContext>::value, "EVP ownership cannot be assigned");
// Fixed expected values were cross-checked with Python hashlib. Inputs are empty,
// "abc", and one million 'a' bytes; SHAKE uses the explicitly stated output size.
struct Spec {const char* name; size_t bytes; bool xof; const char* expected[3];};
static const Spec specs[] = {
{"SHA256",32,false,{"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855","BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD","CDC76E5C9914FB9281A1C7E284D73E67F1809A48A497200E046D39CCC7112CD0"}},
{"SHA384",48,false,{"38B060A751AC96384CD9327EB1B1E36A21FDB71114BE07434C0CC7BF63F6E1DA274EDEBFE76F65FBD51AD2F14898B95B","CB00753F45A35E8BB5A03D699AC65007272C32AB0EDED1631A8B605A43FF5BED8086072BA1E7CC2358BAECA134C825A7","9D0E1809716474CB086E834E310A4A1CED149E9C00F248527972CEC5704C2A5B07B8B3DC38ECC4EBAE97DDD87F3D8985"}},
{"SHA512",64,false,{"CF83E1357EEFB8BDF1542850D66D8007D620E4050B5715DC83F4A921D36CE9CE47D0D13C5D85F2B0FF8318D2877EEC2F63B931BD47417A81A538327AF927DA3E","DDAF35A193617ABACC417349AE20413112E6FA4E89A97EA20A9EEEE64B55D39A2192992A274FC1A836BA3C23A3FEEBBD454D4423643CE80E2A9AC94FA54CA49F","E718483D0CE769644E2E42C7BC15B4638E1F98B13B2044285632A803AFA973EBDE0FF244877EA60A4CB0432CE577C31BEB009C5C2C49AA2E4EADB217AD8CC09B"}},
{"SHA3-256",32,false,{"A7FFC6F8BF1ED76651C14756A061D662F580FF4DE43B49FA82D80A4B80F8434A","3A985DA74FE225B2045C172D6BD390BD855F086E3E9D525B46BFE24511431532","5C8875AE474A3634BA4FD55EC85BFFD661F32ACA75C6D699D0CDCB6C115891C1"}},
{"SHA3-384",48,false,{"0C63A75B845E4F7D01107D852E4C2485C51A50AAAA94FC61995E71BBEE983A2AC3713831264ADB47FB6BD1E058D5F004","EC01498288516FC926459F58E2C6AD8DF9B473CB0FC08C2596DA7CF0E49BE4B298D88CEA927AC7F539F1EDF228376D25","EEE9E24D78C1855337983451DF97C8AD9EEDF256C6334F8E948D252D5E0E76847AA0774DDB90A842190D2C558B4B8340"}},
{"SHA3-512",64,false,{"A69F73CCA23A9AC5C8B567DC185A756E97C982164FE25859E0D1DCC1475C80A615B2123AF1F5F94C11E3E9402C3AC558F500199D95B6D3E301758586281DCD26","B751850B1A57168A5693CD924B6B096E08F621827444F70D884F5D0240D2712E10E116E9192AF3C91A7EC57647E3934057340B4CF408D5A56592F8274EEC53F0","3C3A876DA14034AB60627C077BB98F7E120A2A5370212DFFB3385A18D4F38859ED311D0A9D5141CE9CC5C66EE689B266A8AA18ACE8282A0E0DB596C90B0A7B87"}},
{"BLAKE2B-512",64,false,{"786A02F742015903C6C6FD852552D272912F4740E15847618A86E217F71F5419D25E1031AFEE585313896444934EB04B903A685B1448B755D56F701AFE9BE2CE","BA80A53F981C4D0D6A2797B69F12F6E94C212F14685AC4B74B12BB6FDBFFA2D17D87C5392AAB792DC252D5DE4533CC9518D38AA8DBF1925AB92386EDD4009923","98FB3EFB7206FD19EBF69B6F312CF7B64E3B94DBE1A17107913975A793F177E1D077609D7FBA363CBBA00D05F7AA4E4FA8715D6428104C0A75643B0FF3FD3EAF"}},
{"BLAKE2S-256",32,false,{"69217A3079908094E11121D042354A7C1F55B6482CA1A51E1B250DFD1ED0EEF9","508C5E8C327C14E2E1A72BA34EEB452F37458B209ED63A294D999B4C86675982","BEC0C0E6CDE5B67ACB73B81F79A67A4079AE1C60DAC9D2661AF18E9F8B50DFA5"}},
{"SHAKE128",32,true,{"7F9C2BA4E88F827D616045507605853ED73B8093F6EFBC88EB1A6EACFA66EF26","5881092DD818BF5CF8A3DDB793FBCBA74097D5C526A6D35F97B83351940F2CC8","9D222C79C4FF9D092CF6CA86143AA411E369973808EF97093255826C5572EF58"}},
{"SHAKE256",64,true,{"46B9DD2B0BA88D13233B3FEB743EEB243FCD52EA62B81B82B50C27646ED5762FD75DC4DDD8C0F200CB05019D67B592F6FC821C49479AB48640292EACB3B7C4BE","483366601360A8771C6863080CC4114D8DB44530F8F1E1EE4F94EA37E78B5739D5A15BEF186A5386C75744C0527E1FAA9F8726E462A12A4FEB06BD8801E751E4","3578A7A4CA9137569CDF76ED617D31BB994FCA9C1BBF8B184013DE8234DFD13A3FD124D4DF76C0A539EE7DD2F6E1EC346124C815D9410E145EB561BCD97B18AB"}}
};
namespace fs = std::filesystem;

static void require(bool condition, const std::string& message)
{
    if (!condition)
        throw std::runtime_error(message);
}

struct ScopedContext
{
    HashRuntime::OpenSslEvpHashContext value;
    ~ScopedContext() { HashRuntime::CleanupOpenSslEvpHashContext(&value); }
};

static bool matches(const HashRuntime::OpenSslEvpHashFinalizeResult& result, const char* expected)
{
    return result.success && result.digest == sunjwbase::strtotstr(expected);
}

static HashRuntime::OpenSslEvpHashFinalizeResult digest(const Spec& spec, const std::string& input,
    size_t blockSize = 137)
{
    ScopedContext context;
    const char* names[] = { spec.name };
    require(HashRuntime::InitializeOpenSslEvpHashContext(&context.value, names, 1, spec.xof, spec.bytes),
        std::string("init ") + spec.name);
    for (size_t offset = 0; offset < input.size(); offset += blockSize)
    {
        const size_t size = std::min(blockSize, input.size() - offset);
        HashRuntime::UpdateOpenSslEvpHashContext(context.value,
            reinterpret_cast<const unsigned char*>(input.data() + offset), size);
    }
    return HashRuntime::FinalizeOpenSslEvpHashContextHex(context.value, spec.bytes);
}

struct FileCase {size_t size; const char* expected;};
static const FileCase fileCases[]={
{0,"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855"},
{1,"6E340B9CFFB37A989CA544E6BB780A2C78901D3FB33738768511A30617AFA01D"},
{63,"29AF2686FD53374A36B0846694CC342177E428D1647515F078784D69CDB9E488"},
{64,"FDEAB9ACF3710362BD2658CDC9A29E8F9C757FCF9811603A8C447CD1D9151108"},
{65,"4BFD2C8B6F1EEC7A2AFEB48B934EE4B2694182027E6D0FC075074F2FABB31781"},
{4095,"45DE2924756389E3CCAB98BDAACBEF8A81CDEB651B59F916A6D6385B4F7B999D"},
{4096,"D67C656E01756650D77717B0839985A056EC28FFE174601D690FC407A2CEFFCA"},
{4097,"A16560D668B843FB3BE99ACE41DBD18471F342BD3255A1D21204B35E43F74436"},
{4194303,"8BC25F24C0F447466930CDA6B0AC7405ADC7F6348C50204CAA9F9A5B6217ED42"},
{4194304,"A117210941A0B00DCB2D8577E680D84B6FA0EAF760D2AFC654C953B9859D54FA"},
{4194321,"7FA1366968EDF9A8490FFFAC3EAB7E0B1619480A152D8E75C7471F753A8C1231"}
};
struct TemporaryDirectory
{
    fs::path path;
    TemporaryDirectory()
    {
        std::random_device random;
        for (int attempt = 0; attempt < 100; ++attempt)
        {
            path = fs::temp_directory_path() /
                ("lhash-provider-" + std::to_string(random()) + "-" + std::to_string(attempt));
            if (fs::create_directory(path))
                return;
        }
        throw std::runtime_error("cannot create unique fixture directory");
    }
    ~TemporaryDirectory()
    {
        std::error_code ignored;
        fs::remove_all(path, ignored);
    }
};

static void verifyFiles()
{
    TemporaryDirectory temporary;
    const fs::path path = temporary.path / fs::u8path(u8"文件 checksum.bin");
    size_t count = 0;
    for (const auto& spec : fileCases)
    {
        // Fixed binary fixture, including NUL and bytes above 0x7f.
        std::vector<unsigned char> content(spec.size);
        for (size_t i = 0; i < content.size(); ++i)
            content[i] = static_cast<unsigned char>(i % 251);
        {
            std::ofstream out(path, std::ios::binary | std::ios::trunc);
            require(static_cast<bool>(out), "create fixture failed");
            if (!content.empty())
                out.write(reinterpret_cast<const char*>(content.data()),
                    static_cast<std::streamsize>(content.size()));
            out.close();
            require(static_cast<bool>(out), "write fixture failed");
        }
        ScopedContext context;
        const char* names[] = { "SHA256" };
        require(HashRuntime::InitializeOpenSslEvpHashContext(&context.value, names, 1, false, 32),
            "file context init");
        std::ifstream in(path, std::ios::binary);
        std::vector<unsigned char> block(4 * 1024 * 1024);
        require(static_cast<bool>(in), "open fixture failed");
        while (in)
        {
            in.read(reinterpret_cast<char*>(block.data()), static_cast<std::streamsize>(block.size()));
            const auto size = in.gcount();
            if (size > 0)
                HashRuntime::UpdateOpenSslEvpHashContext(context.value, block.data(), static_cast<size_t>(size));
        }
        require(in.eof() && !in.bad(), "fixture read error");
        require(matches(HashRuntime::FinalizeOpenSslEvpHashContextHex(context.value, 32), spec.expected),
            "file vector mismatch size=" + std::to_string(spec.size));
        ++count;
    }
    std::cout << "PASS file_boundary_cases=" << count << '\n';
}

static void verifyVectors()
{
    const std::string inputs[] = { "", "abc", std::string(1000000, 'a') };
    size_t count = 0;
    for (const auto& spec : specs)
    {
        for (size_t i = 0; i < 3; ++i)
        {
            require(matches(digest(spec, inputs[i]), spec.expected[i]),
                std::string("vector mismatch ") + spec.name + " case=" + std::to_string(i));
            ++count;
        }
    }
    // Exercise single-byte, compression-block, and page-size chunking using the
    // same independent million-'a' SHA256 vector.
    const size_t chunks[] = { 1, 63, 64, 65, 4095, 4096, 4097, 1000000 };
    for (const size_t size : chunks)
        require(matches(digest(specs[0], inputs[2], size), specs[0].expected[2]),
            "SHA256 chunk mismatch size=" + std::to_string(size));
    std::cout << "PASS vectors=" << count << " sha256_chunk_sizes=8\n";
}

struct FailureInjectionGuard
{
    ~FailureInjectionGuard() { HashRuntime::ResetOpenSslEvpFailureInjection(); }
};

static void verifyFailures()
{
    FailureInjectionGuard guard;
    HashRuntime::ConfigureOpenSslEvpFailureInjection(1, false);
    const auto updateFailure = digest(specs[0], "abc");
    require(!updateFailure.success && updateFailure.digest.empty(), "update failure propagated as success");
    HashRuntime::ConfigureOpenSslEvpFailureInjection(0, true);
    const auto finalFailure = digest(specs[0], "abc");
    require(!finalFailure.success && finalFailure.digest.empty(), "final failure propagated as success");
    HashRuntime::ResetOpenSslEvpFailureInjection();
    require(matches(digest(specs[0], "abc"), specs[0].expected[1]), "digest did not recover after failure reset");
    std::cout << "PASS failure_propagation=2 recovery=1\n";
}

static void verifyLifecycle()
{
    ScopedContext context;
    const char* names[] = { "SHA256" };
    const char* unavailable[] = { "lhash-nonexistent-algorithm" };
    require(!HashRuntime::InitializeOpenSslEvpHashContext(nullptr, names, 1, false, 32), "null context accepted");
    require(!HashRuntime::InitializeOpenSslEvpHashContext(&context.value, nullptr, 1, false, 32), "null names accepted");
    require(!HashRuntime::InitializeOpenSslEvpHashContext(&context.value, names, 0, false, 32), "zero names accepted");
    require(!HashRuntime::InitializeOpenSslEvpHashContext(&context.value, unavailable, 1, false, 32),
        "unavailable algorithm accepted");
    const auto uninitialized = HashRuntime::FinalizeOpenSslEvpHashContextHex(context.value, 32);
    require(!uninitialized.success && uninitialized.digest.empty(), "uninitialized context finalized");

    const char* fallback[] = { nullptr, "", "lhash-nonexistent-algorithm", "SHA256" };
    require(HashRuntime::InitializeOpenSslEvpHashContext(&context.value, fallback, 4, false, 32),
        "algorithm alias fallback failed");
    HashRuntime::UpdateOpenSslEvpHashContext(context.value, reinterpret_cast<const unsigned char*>("discard"), 7);
    require(HashRuntime::InitializeOpenSslEvpHashContext(&context.value, names, 1, false, 32), "context reinit failed");
    HashRuntime::UpdateOpenSslEvpHashContext(context.value, reinterpret_cast<const unsigned char*>("abc"), 3);
    HashRuntime::UpdateOpenSslEvpHashContext(context.value, nullptr, 0);
    require(matches(HashRuntime::FinalizeOpenSslEvpHashContextHex(context.value, 32), specs[0].expected[1]),
        "reinitialized context retained earlier data");
    const auto repeated = HashRuntime::FinalizeOpenSslEvpHashContextHex(context.value, 32);
    require(!repeated.success && repeated.digest.empty(), "repeated finalization returned a digest");
    HashRuntime::CleanupOpenSslEvpHashContext(&context.value);
    HashRuntime::CleanupOpenSslEvpHashContext(&context.value);
    HashRuntime::CleanupOpenSslEvpHashContext(nullptr);
    std::cout << "PASS invalid_init=4 uninitialized_finalize=1 alias_fallback=1 reinit=1 repeated_finalize=1 cleanup=3\n";
}

static void verifyConcurrent()
{
    // No earlier hash in this CTest process: contend on the first provider cache
    // initialization as well as subsequent use of independent EVP contexts.
    std::atomic<size_t> ready{0};
    std::atomic<bool> start{false};
    std::atomic<size_t> errors{0};
    std::vector<std::thread> threads;
    try
    {
        for (size_t i = 0; i < 8; ++i)
        {
            threads.emplace_back([&] {
                ++ready;
                while (!start.load())
                    std::this_thread::yield();
                try
                {
                    for (size_t j = 0; j < 100; ++j)
                        if (!matches(digest(specs[0], "abc"), specs[0].expected[1]))
                            ++errors;
                }
                catch (...)
                {
                    ++errors;
                }
            });
        }
        while (ready.load() < 8)
            std::this_thread::yield();
        start = true;
    }
    catch (...)
    {
        start = true;
        for (auto& thread : threads)
            thread.join();
        throw;
    }
    for (auto& thread : threads)
        thread.join();
    require(errors.load() == 0, "concurrent contexts failed");
    std::cout << "PASS concurrent_sha256=800 threads=8\n";
}

static void verifyInputBounds()
{
    size_t rejected = 0;
    for (const auto& spec : specs)
    {
        const char* names[] = { spec.name };
        HashRuntime::OpenSslEvpHashContext context;
        const auto init = [&]() {
            return HashRuntime::InitializeOpenSslEvpHashContext(&context, names, 1, spec.xof, spec.bytes);
        };
        for (size_t wrongSize : { size_t(0), std::numeric_limits<size_t>::max() })
        {
            require(!HashRuntime::InitializeOpenSslEvpHashContext(&context, names, 1, spec.xof, wrongSize),
                "invalid initial output size accepted");
            ++rejected;
        }
        if (!spec.xof)
        {
            for (size_t wrongSize : { size_t(1), spec.bytes - 1, spec.bytes + 1 })
            {
                require(!HashRuntime::InitializeOpenSslEvpHashContext(&context, names, 1, false, wrongSize),
                    "fixed digest initialized with the wrong output size");
                ++rejected;
            }
        }
        require(!HashRuntime::InitializeOpenSslEvpHashContext(&context, names, 1, !spec.xof, spec.bytes),
            "incorrect XOF mode accepted");
        ++rejected;
        for (size_t wrongSize : { size_t(0), size_t(1), spec.bytes - 1, spec.bytes + 1,
            std::numeric_limits<size_t>::max() })
        {
            require(init(), "init before invalid finalization");
            HashRuntime::UpdateOpenSslEvpHashContext(context, reinterpret_cast<const unsigned char*>("abc"), 3);
            const auto result = HashRuntime::FinalizeOpenSslEvpHashContextHex(context, wrongSize);
            require(!result.success && result.digest.empty() && context.mdContext == nullptr,
                "mismatched final output size accepted or retained state");
            ++rejected;
        }
        require(init(), "init before null data");
        HashRuntime::UpdateOpenSslEvpHashContext(context, nullptr, 1);
        require(!HashRuntime::FinalizeOpenSslEvpHashContextHex(context, spec.bytes).success,
            "null nonempty input silently omitted");
        ++rejected;
        require(init(), "init before invalid reinitialization");
        require(!HashRuntime::InitializeOpenSslEvpHashContext(&context, nullptr, 1, spec.xof, spec.bytes),
            "invalid reinitialization succeeded");
        require(!HashRuntime::FinalizeOpenSslEvpHashContextHex(context, spec.bytes).success,
            "invalid reinitialization retained a live digest");
        ++rejected;
        require(matches(digest(spec, "abc"), spec.expected[1]), "did not recover after rejected input");
        // Destruction must release an unfinished context, including during an exception.
        try
        {
            HashRuntime::OpenSslEvpHashContext unfinished;
            require(HashRuntime::InitializeOpenSslEvpHashContext(&unfinished, names, 1, spec.xof, spec.bytes),
                "unfinished init");
            throw 1;
        }
        catch (int) {}
    }
    std::cout << "PASS rejected_inputs=" << rejected << " recovered_algorithms=10 scoped_cleanup=10\n";
}

static void verifyConfigurationIsolation()
{
    // CTest supplies a harmless openssl.cnf excluding all providers from the
    // process default context. Establish that the fixture actually took effect.
    EVP_MD* blocked = EVP_MD_fetch(nullptr, "SHA256", nullptr);
    const bool blockedByConfiguration = blocked == nullptr;
    EVP_MD_free(blocked);
    require(blockedByConfiguration, "restrictive default-context configuration was not active");
    for (const auto& spec : specs)
        require(matches(digest(spec, "abc"), spec.expected[1]),
            std::string("local digest affected by external config: ") + spec.name);
    std::cout << "PASS private_context_algorithms=10 external_config_and_module_path_ignored\n";
}

int main(int argc, char** argv)
{
    try
    {
        const std::string version = OpenSSL_version(OPENSSL_VERSION);
        std::cout << "runtime=" << version << '\n';
        require(version.find("OpenSSL 3.5.9 ") == 0, "wrong runtime version");
        require(argc == 2, "usage: lhash_openssl_provider_tests version|vectors|files|failures|lifecycle|concurrent|bounds|isolation");
        const std::string group = argv[1];
        if (group == "version") std::cout << "PASS headers_and_runtime=3.5.9\n";
        else if (group == "vectors") verifyVectors();
        else if (group == "files") verifyFiles();
        else if (group == "failures") verifyFailures();
        else if (group == "lifecycle") verifyLifecycle();
        else if (group == "concurrent") verifyConcurrent();
        else if (group == "bounds") verifyInputBounds();
        else if (group == "isolation") verifyConfigurationIsolation();
        else throw std::runtime_error("unknown test group: " + group);
    }
    catch (const std::exception& error)
    {
        std::cerr << "FAIL " << error.what() << '\n';
        return 1;
    }
    return 0;
}
