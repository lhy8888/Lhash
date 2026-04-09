// Fixed configuration for the vendored google/crc32c snapshot inside LHash.

#ifndef CRC32C_CRC32C_CONFIG_H_
#define CRC32C_CRC32C_CONFIG_H_

#define BYTE_ORDER_BIG_ENDIAN 0

#if defined(__clang__) || defined(__GNUC__)
#define HAVE_BUILTIN_PREFETCH 1
#else
#define HAVE_BUILTIN_PREFETCH 0
#endif

#if defined(_M_IX86) || defined(_M_X64) || defined(__i386__) || defined(__x86_64__)
#define HAVE_MM_PREFETCH 1
#else
#define HAVE_MM_PREFETCH 0
#endif

#if defined(_M_X64) || defined(__x86_64__)
#define HAVE_SSE42 1
#else
#define HAVE_SSE42 0
#endif

#if defined(_M_ARM64) || defined(__aarch64__)
#define HAVE_ARM64_CRC32C 1
#else
#define HAVE_ARM64_CRC32C 0
#endif

#define HAVE_STRONG_GETAUXVAL 0
#define HAVE_WEAK_GETAUXVAL 0
#define CRC32C_TESTS_BUILT_WITH_GLOG 0

#endif  // CRC32C_CRC32C_CONFIG_H_
