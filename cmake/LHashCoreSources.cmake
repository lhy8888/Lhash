set(LHASH_COMMON_SOURCES
    "${LHASH_SOURCE_ROOT}/Common/strhelper.cpp"
    "${LHASH_SOURCE_ROOT}/Common/Utils.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashEngine.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashEnginePreparation.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashEngineResult.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashScheduler.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashSchedulerPlan.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashSchedulerDispatch.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashJobExecutionPlan.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashJobLifecycleWorkflow.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashFileRunner.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashFileAttemptWorkflow.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashFileAttemptStateOps.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashFileAttemptCompletionWorkflow.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashSuccessfulFileCompletionWorkflow.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashFileResultWorkflow.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashFileSizeAccounting.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashFileVersionResolver.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashErrorResultWorkflow.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashPreparationPlan.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashPreparationWorkflow.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashPreScanWorkflow.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashPreScanSizeProbe.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashPreScanSizeAccounting.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashDigestBufferPlan.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashDigestCompletion.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashDigestContextOps.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashDigestExecution.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashDigestExecutionMode.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashDigestLifecycle.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashDigestOperationRegistry.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashDigestPipeline.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashDigestQueue.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashDigestQueuePlan.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashDigestRuntimePlan.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashDigestSinglePass.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashDigestUpdater.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashProgressTracker.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashResultEventWorkflow.cpp"
    "${LHASH_SOURCE_ROOT}/Common/HashResultPublisher.cpp"
)

set(LHASH_ALGORITHM_SOURCES
    "${LHASH_SOURCE_ROOT}/Algorithms/MD5.cpp"
    "${LHASH_SOURCE_ROOT}/Algorithms/SHA1.cpp"
)

set(LHASH_RUNTIME_HASH_SOURCES
    "${LHASH_SOURCE_ROOT}/Runtime/Hash/BLAKE3HashProvider.cpp"
    "${LHASH_SOURCE_ROOT}/Runtime/Hash/CRC32CHashProvider.cpp"
    "${LHASH_SOURCE_ROOT}/Runtime/Hash/OpenSslEvpHashProvider.cpp"
    "${LHASH_SOURCE_ROOT}/Runtime/Hash/XXHash3HashProvider.cpp"
)

set(LHASH_PLATFORM_SOURCES "")
if (WIN32)
    list(APPEND LHASH_PLATFORM_SOURCES
        "${LHASH_SOURCE_ROOT}/WinCommon/WindowsComm.cpp"
        "${LHASH_SOURCE_ROOT}/OsUtils/OsFileWinApi.cpp"
        "${LHASH_SOURCE_ROOT}/OsUtils/OsThreadWinApi.cpp"
    )
elseif (APPLE)
    list(APPEND LHASH_PLATFORM_SOURCES
        "${LHASH_SOURCE_ROOT}/OsUtils/OsFilePosixDarwin.cpp"
        "${LHASH_SOURCE_ROOT}/OsUtils/OsThreadPosixDarwin.cpp"
    )
endif()

set(LHASH_BLAKE3_SOURCES
    "${LHASH_THIRD_PARTY_ROOT}/blake3/1.8.4/c/blake3.c"
    "${LHASH_THIRD_PARTY_ROOT}/blake3/1.8.4/c/blake3_dispatch.c"
    "${LHASH_THIRD_PARTY_ROOT}/blake3/1.8.4/c/blake3_portable.c"
)
set(LHASH_BLAKE3_COMPILE_DEFINITIONS
    BLAKE3_NO_SSE2
    BLAKE3_NO_SSE41
    BLAKE3_NO_AVX2
    BLAKE3_NO_AVX512
)
set(LHASH_BLAKE3_USE_NEON_VALUE 0)

set(LHASH_XXHASH_SOURCES
    "${LHASH_THIRD_PARTY_ROOT}/xxhash/0.8.3/xxhash.c"
)

set(LHASH_CRC32C_SOURCES
    "${LHASH_THIRD_PARTY_ROOT}/crc32c/1.1.2/src/crc32c.cc"
    "${LHASH_THIRD_PARTY_ROOT}/crc32c/1.1.2/src/crc32c_portable.cc"
)

set(_LHASH_TARGET_ARCH "${CMAKE_SYSTEM_PROCESSOR}")
if (DEFINED CMAKE_GENERATOR_PLATFORM AND NOT CMAKE_GENERATOR_PLATFORM STREQUAL "")
    set(_LHASH_TARGET_ARCH "${CMAKE_GENERATOR_PLATFORM}")
endif()
string(TOLOWER "${_LHASH_TARGET_ARCH}" _LHASH_TARGET_ARCH_LOWER)

if (_LHASH_TARGET_ARCH_LOWER MATCHES "^(x86_64|amd64|x64)$")
    list(APPEND LHASH_CRC32C_SOURCES
        "${LHASH_THIRD_PARTY_ROOT}/crc32c/1.1.2/src/crc32c_sse42.cc"
    )
elseif (APPLE AND _LHASH_TARGET_ARCH_LOWER MATCHES "^(arm64|aarch64)$")
    list(APPEND LHASH_CRC32C_SOURCES
        "${LHASH_THIRD_PARTY_ROOT}/crc32c/1.1.2/src/crc32c_arm64.cc"
    )
endif()

if (APPLE AND _LHASH_TARGET_ARCH_LOWER MATCHES "^(arm64|aarch64)$")
    list(APPEND LHASH_BLAKE3_SOURCES
        "${LHASH_THIRD_PARTY_ROOT}/blake3/1.8.4/c/blake3_neon.c"
    )
    set(LHASH_BLAKE3_USE_NEON_VALUE 1)
endif()
