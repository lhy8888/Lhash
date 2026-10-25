cmake_minimum_required(VERSION 3.20)
project(LHashEngineFailureTests LANGUAGES CXX)
include("${CMAKE_CURRENT_SOURCE_DIR}/../../cmake/LHashAppleSilicon.cmake")
find_package(Threads REQUIRED)
enable_testing()

add_executable(lhash_engine_failure_tests
    engine_failure_tests.cpp
    ../../trunk/source/Common/HashEngine.cpp
    ../../trunk/source/Common/HashJobLifecycleWorkflow.cpp
)
target_compile_features(lhash_engine_failure_tests PRIVATE cxx_std_17)
target_compile_definitions(lhash_engine_failure_tests PRIVATE LHASH_CORE_BUILD=1)
target_include_directories(lhash_engine_failure_tests PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/include"
    "${CMAKE_CURRENT_SOURCE_DIR}/../../trunk/source"
)
if(NOT WIN32)
    target_include_directories(lhash_engine_failure_tests BEFORE PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}/../../cmake/posix-stubs")
endif()
if(WIN32)
    target_compile_definitions(lhash_engine_failure_tests PRIVATE WIN32 UNICODE _UNICODE)
endif()
target_link_libraries(lhash_engine_failure_tests PRIVATE Threads::Threads)

add_executable(lhash_threadpool_failure_tests threadpool_failure_tests.cpp)
target_compile_features(lhash_threadpool_failure_tests PRIVATE cxx_std_17)
target_include_directories(lhash_threadpool_failure_tests PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}/../../trunk/source")
target_link_libraries(lhash_threadpool_failure_tests PRIVATE Threads::Threads)

add_executable(lhash_digest_dispatch_failure_tests
    digest_dispatch_failure_tests.cpp
    ../../trunk/source/Common/HashDigestUpdater.cpp
    ../../trunk/source/Common/strhelper.cpp
)
target_compile_features(lhash_digest_dispatch_failure_tests PRIVATE cxx_std_17)
target_compile_definitions(lhash_digest_dispatch_failure_tests PRIVATE LHASH_CORE_BUILD=1)
target_include_directories(lhash_digest_dispatch_failure_tests PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/include" "${CMAKE_CURRENT_SOURCE_DIR}/../../trunk/source")
if(WIN32)
    target_compile_definitions(lhash_digest_dispatch_failure_tests PRIVATE WIN32 UNICODE _UNICODE)
else()
    target_include_directories(lhash_digest_dispatch_failure_tests BEFORE PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}/../../cmake/posix-stubs")
endif()
target_link_libraries(lhash_digest_dispatch_failure_tests PRIVATE Threads::Threads)
if(APPLE)
    target_link_libraries(lhash_digest_dispatch_failure_tests PRIVATE iconv)
endif()

if(MSVC)
    target_compile_options(lhash_engine_failure_tests PRIVATE /utf-8)
    target_compile_options(lhash_threadpool_failure_tests PRIVATE /utf-8)
    target_compile_options(lhash_digest_dispatch_failure_tests PRIVATE /utf-8)
endif()
add_test(NAME lhash_engine_failure_tests COMMAND lhash_engine_failure_tests)
add_test(NAME lhash_threadpool_failure_tests COMMAND lhash_threadpool_failure_tests)
add_test(NAME lhash_digest_dispatch_failure_tests COMMAND lhash_digest_dispatch_failure_tests)
set_tests_properties(lhash_engine_failure_tests lhash_threadpool_failure_tests lhash_digest_dispatch_failure_tests PROPERTIES TIMEOUT 30)
