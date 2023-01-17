#!/bin/sh

premake5 --os=linux gmake
premake5 --os=linux --file=premake5_dotnet.lua vs2019

cd build/gmake

make -j -l $(nproc)

make config=debug_x86_64 -j -l $(nproc)

cd ../../build/vs2019

msbuild Dependencies_Dotnet.sln /p:Configuration=Debug
msbuild Dependencies_Dotnet.sln /p:Configuration=Release

cd ../../bgfx

make tools -j -l $(nproc)

cp .build/linux64_gcc/bin/*cRelease ../../Tools/bin

cd ../../
