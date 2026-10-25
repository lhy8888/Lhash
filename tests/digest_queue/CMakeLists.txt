cmake_minimum_required(VERSION 3.20)
project(LHashDigestQueueTests LANGUAGES CXX)

include("${CMAKE_CURRENT_SOURCE_DIR}/../../cmake/LHashAppleSilicon.cmake")

find_package(Threads REQUIRED)
enable_testing()
add_executable(lhash_digest_queue_tests
    queue_tests.cpp
    ../../trunk/source/Common/HashDigestQueue.cpp
)
target_compile_features(lhash_digest_queue_tests PRIVATE cxx_std_17)
# The queue itself and ThreadPool are production code. These narrow test doubles
# isolate file reads and digest/progress callbacks from platform-specific APIs.
target_include_directories(lhash_digest_queue_tests PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/include"
    "${CMAKE_CURRENT_SOURCE_DIR}/../../trunk/source"
)
target_link_libraries(lhash_digest_queue_tests PRIVATE Threads::Threads)
if(MSVC)
    target_compile_options(lhash_digest_queue_tests PRIVATE /utf-8)
endif()
add_test(NAME lhash_digest_queue_tests COMMAND lhash_digest_queue_tests)
set_tests_properties(lhash_digest_queue_tests PROPERTIES TIMEOUT 120)
