cmake_minimum_required(VERSION 3.22.1)
project(clashofdrayven LANGUAGES CXX)

add_library(clashofdrayven SHARED
    native-lib.cpp
    DrayvenEngine.cpp
)

target_compile_features(clashofdrayven PRIVATE cxx_std_17)
target_compile_options(clashofdrayven PRIVATE
    -O3
    -fvisibility=hidden
    -fvisibility-inlines-hidden
    -ffunction-sections
    -fdata-sections
    -fno-exceptions
    -fno-rtti
)
target_link_options(clashofdrayven PRIVATE
    -Wl,--gc-sections
    -Wl,--strip-all
    -Wl,--exclude-libs,ALL
)
