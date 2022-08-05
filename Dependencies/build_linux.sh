#!/bin/sh

premake5 --os=linux gmake
premake5 --os=linux --file=premake5_dotnet.lua vs2019

cd build/gmake

make -j -l $(grep -c ^processor /proc/cpuinfo)

make config=debug_x86_64 -j -l $(shell grep -c ^processor /proc/cpuinfo)

cd ../../build/vs2019

msbuild Dependencies.sln /p:Configuration=Debug
msbuild Dependencies.sln /p:Configuration=Release

cd ../../bgfx

make tools -j -l $(grep -c ^processor /proc/cpuinfo)

cp .build/linux64_gcc/bin/*cRelease ../../Tools/bin

cd ../../


