cmake_minimum_required(VERSION 3.20)

project(LHash LANGUAGES C CXX)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

option(LHASH_BUILD_M2_SMOKE "Build the M2 core smoke executable" ON)
option(LHASH_ENABLE_OPENSSL_VENDOR "Enable the OpenSSL vendor backend in the CMake core build" OFF)

set(LHASH_REPO_ROOT "${CMAKE_CURRENT_SOURCE_DIR}")
set(LHASH_SOURCE_ROOT "${LHASH_REPO_ROOT}/trunk/source")
set(LHASH_THIRD_PARTY_ROOT "${LHASH_REPO_ROOT}/third_party")

include("${CMAKE_CURRENT_SOURCE_DIR}/cmake/LHashCoreSources.cmake")

add_library(lhash_core STATIC)
target_sources(lhash_core PRIVATE
    ${LHASH_COMMON_SOURCES}
    ${LHASH_ALGORITHM_SOURCES}
    ${LHASH_RUNTIME_HASH_SOURCES}
    ${LHASH_PLATFORM_SOURCES}
    ${LHASH_BLAKE3_SOURCES}
    ${LHASH_XXHASH_SOURCES}
    ${LHASH_CRC32C_SOURCES}
)

target_include_directories(lhash_core PUBLIC
    "${LHASH_SOURCE_ROOT}"
    "${LHASH_THIRD_PARTY_ROOT}/blake3/1.8.4/c"
    "${LHASH_THIRD_PARTY_ROOT}/xxhash/0.8.3"
    "${LHASH_THIRD_PARTY_ROOT}/crc32c/1.1.2/include"
    "${LHASH_THIRD_PARTY_ROOT}/crc32c/1.1.2/src"
)

if (WIN32)
    target_compile_definitions(lhash_core PRIVATE UNICODE _UNICODE)
endif()

target_compile_definitions(lhash_core PRIVATE FHASH_CORE_BUILD=1)

target_compile_definitions(lhash_core PRIVATE
    BLAKE3_NO_SSE2
    BLAKE3_NO_SSE41
    BLAKE3_NO_AVX2
    BLAKE3_NO_AVX512
    BLAKE3_USE_NEON=0
)

if (LHASH_ENABLE_OPENSSL_VENDOR)
    target_compile_definitions(lhash_core PRIVATE FHASH_WITH_OPENSSL3_VENDOR=1)
endif()

if (WIN32)
    target_compile_definitions(lhash_core PRIVATE WIN32 _WINDOWS _LIB)
    target_link_libraries(lhash_core PUBLIC Version)
endif()

if (MSVC)
    target_compile_options(lhash_core PRIVATE
        $<$<COMPILE_LANGUAGE:C>:/utf-8>
        $<$<COMPILE_LANGUAGE:CXX>:/utf-8>
    )
endif()

if (WIN32 AND _LHASH_TARGET_ARCH_LOWER MATCHES "^(x86_64|amd64|x64)$")
    set_source_files_properties(
        "${LHASH_THIRD_PARTY_ROOT}/crc32c/1.1.2/src/crc32c_sse42.cc"
        PROPERTIES COMPILE_OPTIONS "/arch:AVX"
    )
endif()

source_group(TREE "${LHASH_SOURCE_ROOT}" PREFIX "Source" FILES
    ${LHASH_COMMON_SOURCES}
    ${LHASH_ALGORITHM_SOURCES}
    ${LHASH_RUNTIME_HASH_SOURCES}
    ${LHASH_PLATFORM_SOURCES}
)

source_group(TREE "${LHASH_THIRD_PARTY_ROOT}" PREFIX "ThirdParty" FILES
    ${LHASH_BLAKE3_SOURCES}
    ${LHASH_XXHASH_SOURCES}
    ${LHASH_CRC32C_SOURCES}
)

if (LHASH_BUILD_M2_SMOKE)
    enable_testing()

    add_executable(lhash_core_smoke
        "${CMAKE_CURRENT_SOURCE_DIR}/tests/core_smoke/lhash_core_smoke.cpp"
    )
    target_link_libraries(lhash_core_smoke PRIVATE lhash_core)
    target_include_directories(lhash_core_smoke PRIVATE "${LHASH_SOURCE_ROOT}")
    if (MSVC)
        target_compile_options(lhash_core_smoke PRIVATE /utf-8)
    endif()

    add_test(NAME lhash_core_smoke COMMAND lhash_core_smoke)
endif()
