cmake_minimum_required(VERSION 3.22.1)
project(IrAutoX LANGUAGES CXX)

set(LUAJIT_ROOT ${CMAKE_CURRENT_SOURCE_DIR}/vendor/luajit)
set(LUAJIT_LIB ${LUAJIT_ROOT}/${ANDROID_ABI}/libluajit.a)
if(NOT EXISTS ${LUAJIT_LIB})
    message(FATAL_ERROR "LuaJIT static library missing for ${ANDROID_ABI}: ${LUAJIT_LIB}")
endif()

add_library(luajit STATIC IMPORTED)
set_target_properties(luajit PROPERTIES IMPORTED_LOCATION ${LUAJIT_LIB})

add_library(IrAutoX SHARED
    native-lib.cpp
    DrayvenEngine.cpp
    LuaRuntime.cpp
)

target_compile_features(IrAutoX PRIVATE cxx_std_17)
target_include_directories(IrAutoX PRIVATE
    ${LUAJIT_ROOT}/include
    ${CMAKE_CURRENT_SOURCE_DIR}/generated
)
target_compile_options(IrAutoX PRIVATE
    -O3
    -fvisibility=hidden
    -fvisibility-inlines-hidden
    -ffunction-sections
    -fdata-sections
    -fno-exceptions
    -fno-rtti
    -fomit-frame-pointer
)
target_link_options(IrAutoX PRIVATE
    -Wl,--gc-sections
    -Wl,--strip-all
    -Wl,--exclude-libs,ALL
    -Wl,-z,max-page-size=16384
    -Wl,-z,common-page-size=16384
)
target_link_libraries(IrAutoX PRIVATE luajit log android dl m)
