#!/bin/sh

premake5 --os=linux gmake
premake5 --os=linux --file=premake5_dotnet.lua vs2022

cd build/gmake

make -j -l $(nproc)

make config=debug_x86_64 -j -l $(nproc)

cd ../../build/vs2022

dotnet publish build/vs2022/Dependencies_Dotnet.sln -c Debug -o build/vs2022/bin/Debug/net7.0
dotnet publish build/vs2022/Dependencies_Dotnet.sln -c Release -o build/vs2022/bin/Release/net7.0

cd ../../bgfx

make tools -j -l $(nproc)

cp .build/linux64_gcc/bin/*cRelease ../../Tools/bin

cd ../../
