cmake_minimum_required(VERSION 3.20)
project(LHashWindowsInputPolicyTests LANGUAGES CXX)
find_package(Python3 REQUIRED COMPONENTS Interpreter)
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(INPUT_PROBES copydata_probe dialog_limit_probe registry_command_probe folder_walk_probe windows_utility_probe version_api_probe input_token_probe)
set(INPUT_PROBE_SOURCES "")
foreach(probe IN LISTS INPUT_PROBES)
    list(APPEND INPUT_PROBE_SOURCES "${CMAKE_CURRENT_BINARY_DIR}/${probe}.cpp")
endforeach()
get_filename_component(LHASH_REPO_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/../.." ABSOLUTE)
add_custom_command(OUTPUT ${INPUT_PROBE_SOURCES}
    COMMAND "${Python3_EXECUTABLE}" "${CMAKE_CURRENT_SOURCE_DIR}/generate_probes.py" "${CMAKE_CURRENT_BINARY_DIR}"
    DEPENDS generate_probes.py
        "${LHASH_REPO_ROOT}/trunk/source/WinMFC/FilesHashInputController.cpp"
        "${LHASH_REPO_ROOT}/trunk/source/WinMFC/WindowsUtils.cpp"
        "${LHASH_REPO_ROOT}/trunk/source/WinCommon/WindowsComm.cpp"
        "${LHASH_REPO_ROOT}/trunk/source/Adapters/MfcBridge/ThreadDataInputAccess.h"
    VERBATIM)
add_custom_target(generate_windows_input_probes DEPENDS ${INPUT_PROBE_SOURCES})
option(LHASH_INPUT_SANITIZERS "Run portable input control-flow probes with ASan/UBSan" OFF)
enable_testing()
foreach(probe IN LISTS INPUT_PROBES)
    add_executable(${probe} "${CMAKE_CURRENT_BINARY_DIR}/${probe}.cpp")
    add_dependencies(${probe} generate_windows_input_probes)
    target_include_directories(${probe} PRIVATE "${LHASH_REPO_ROOT}/trunk/source")
    if(MSVC)
        target_compile_options(${probe} PRIVATE /utf-8 /W4)
    else()
        target_compile_options(${probe} PRIVATE -Wall -Wextra)
        if(LHASH_INPUT_SANITIZERS)
            target_compile_options(${probe} PRIVATE -fsanitize=address,undefined -fno-omit-frame-pointer)
            target_link_options(${probe} PRIVATE -fsanitize=address,undefined)
        endif()
    endif()
    add_test(NAME "windows_input_${probe}" COMMAND ${probe})
    set_tests_properties("windows_input_${probe}" PROPERTIES TIMEOUT 30)
endforeach()
