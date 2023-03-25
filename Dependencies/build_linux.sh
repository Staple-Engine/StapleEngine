#!/bin/sh

premake5 --os=linux gmake
premake5 --os=linux --file=premake5_dotnet.lua vs2022

cd build/vs2022

make -j $(nproc)

make config=debug_x86_64 -j $(nproc)

cd ../../build/vs2022

dotnet publish Dependencies_Dotnet.sln -c Debug -o bin/Debug/net7.0
dotnet publish Dependencies_Dotnet.sln -c Release -o bin/Release/net7.0

cd ../../GENie

make

cd ../bgfx

make GENIE=../GENie/bin/linux/genie tools -j $(nproc)

cp .build/linux64_gcc/bin/*cRelease ../../Tools/bin

cd ../../
