# macOS targets use the common Apple Silicon ABI, without a chip-generation
# whitelist or an Intel/universal build. Include this after project().
if (APPLE)
    if (CMAKE_OSX_ARCHITECTURES)
        set(_LHASH_MACOS_ARCH "${CMAKE_OSX_ARCHITECTURES}")
    else()
        set(_LHASH_MACOS_ARCH "${CMAKE_SYSTEM_PROCESSOR}")
    endif()
    if (NOT _LHASH_MACOS_ARCH MATCHES "^(arm64|aarch64)$")
        message(FATAL_ERROR "LHash macOS builds support Apple Silicon (arm64) only. Set CMAKE_OSX_ARCHITECTURES=arm64.")
    endif()
    unset(_LHASH_MACOS_ARCH)
endif()
