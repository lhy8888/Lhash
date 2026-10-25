cmake_minimum_required(VERSION 3.20)
project(LHashOpenSslProviderTests LANGUAGES CXX)

# This standalone target exercises the production EVP adapter on platforms where
# the full desktop application cannot be built (including a Linux cloud runner).
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(OPENSSL_USE_STATIC_LIBS TRUE)
set(LHASH_OPENSSL_INCLUDE_DIRS "" CACHE STRING "Explicit OpenSSL include directories, for an uninstalled build")
set(LHASH_OPENSSL_CRYPTO_LIBRARY "" CACHE FILEPATH "Explicit static libcrypto, for an uninstalled build")
option(LHASH_PROVIDER_SANITIZERS "Instrument the provider and tests with ASan/UBSan" OFF)

find_package(Threads REQUIRED)
if (LHASH_OPENSSL_INCLUDE_DIRS AND LHASH_OPENSSL_CRYPTO_LIBRARY)
    add_library(lhash_test_crypto UNKNOWN IMPORTED)
    set_target_properties(lhash_test_crypto PROPERTIES
        IMPORTED_LOCATION "${LHASH_OPENSSL_CRYPTO_LIBRARY}"
        INTERFACE_INCLUDE_DIRECTORIES "${LHASH_OPENSSL_INCLUDE_DIRS}"
    )
    set(LHASH_CRYPTO_TARGET lhash_test_crypto)
elseif (LHASH_OPENSSL_INCLUDE_DIRS OR LHASH_OPENSSL_CRYPTO_LIBRARY)
    message(FATAL_ERROR "Pass both LHASH_OPENSSL_INCLUDE_DIRS and LHASH_OPENSSL_CRYPTO_LIBRARY")
else()
    find_package(OpenSSL 3.5.9 EXACT REQUIRED COMPONENTS Crypto)
    set(LHASH_CRYPTO_TARGET OpenSSL::Crypto)
endif()

get_filename_component(LHASH_REPO_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/../.." ABSOLUTE)
set(LHASH_SOURCE_ROOT "${LHASH_REPO_ROOT}/trunk/source")
add_executable(lhash_openssl_provider_tests
    provider_vectors.cpp
    "${LHASH_SOURCE_ROOT}/Runtime/Hash/OpenSslEvpHashProvider.cpp"
    "${LHASH_SOURCE_ROOT}/Common/strhelper.cpp"
)
target_compile_definitions(lhash_openssl_provider_tests PRIVATE
    LHASH_CORE_BUILD=1 LHASH_WITH_OPENSSL3_VENDOR=1
)
target_include_directories(lhash_openssl_provider_tests PRIVATE "${LHASH_SOURCE_ROOT}")
target_link_libraries(lhash_openssl_provider_tests PRIVATE
    ${LHASH_CRYPTO_TARGET} Threads::Threads ${CMAKE_DL_LIBS}
)
if (WIN32)
    target_compile_definitions(lhash_openssl_provider_tests PRIVATE WIN32 UNICODE _UNICODE NOMINMAX)
    target_link_libraries(lhash_openssl_provider_tests PRIVATE ws2_32 gdi32 advapi32 crypt32 user32)
endif()
if (APPLE)
    target_link_libraries(lhash_openssl_provider_tests PRIVATE iconv)
endif()
if (MSVC)
    # Match build_openssl_vendor.ps1 and the native runtime suite's static CRT.
    set_property(TARGET lhash_openssl_provider_tests PROPERTY MSVC_RUNTIME_LIBRARY MultiThreaded)
    target_compile_options(lhash_openssl_provider_tests PRIVATE /utf-8 /W4)
else()
    target_include_directories(lhash_openssl_provider_tests BEFORE PRIVATE
        "${LHASH_REPO_ROOT}/cmake/posix-stubs"
    )
    target_compile_options(lhash_openssl_provider_tests PRIVATE -Wall -Wextra)
endif()
if (LHASH_PROVIDER_SANITIZERS)
    if (MSVC)
        message(FATAL_ERROR "LHASH_PROVIDER_SANITIZERS requires GCC or Clang")
    endif()
    target_compile_options(lhash_openssl_provider_tests PRIVATE -fsanitize=address,undefined -fno-omit-frame-pointer)
    target_link_options(lhash_openssl_provider_tests PRIVATE -fsanitize=address,undefined)
endif()

enable_testing()
foreach (test_group IN ITEMS version vectors files failures lifecycle concurrent)
    add_test(NAME "openssl_provider_${test_group}" COMMAND lhash_openssl_provider_tests "${test_group}")
    set_tests_properties("openssl_provider_${test_group}" PROPERTIES TIMEOUT 120)
endforeach()
