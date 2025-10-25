cmake_minimum_required(VERSION 3.21)
project(miniaudionet_native C)

include(GNUInstallDirs)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

set(MANET_MINIAUDIO_DIR
    "${CMAKE_CURRENT_SOURCE_DIR}/../third_party/miniaudio"
    CACHE PATH "Directory that contains miniaudio.h")

add_library(miniaudionet SHARED manet_bridge.c)

target_include_directories(miniaudionet
    PRIVATE
        "${MANET_MINIAUDIO_DIR}")

target_compile_definitions(miniaudionet
    PRIVATE
        MINIAUDIO_IMPLEMENTATION
        MINIAUDIO_STATIC)

if(MSVC)
    target_compile_options(miniaudionet PRIVATE /W4 /permissive-)
    target_compile_definitions(miniaudionet PRIVATE _CRT_SECURE_NO_WARNINGS)
else()
    target_compile_options(miniaudionet PRIVATE -Wall -Wextra -Wpedantic)
endif()

install(
    TARGETS miniaudionet
    RUNTIME DESTINATION native
    LIBRARY DESTINATION native)
