#!/bin/sh

premake5 --os=macosx gmake
premake5 --os=macosx --file=NativeFileDialog/build/premake5.lua gmake
premake5 --os=macosx --file=premake5_dotnet.lua vs2022

cd build/vs2022

make -j $(sysctl -n hw.logicalcpu)

make config=debug_x86_64 -j $(sysctl -n hw.logicalcpu)

cd ../../build/vs2022

dotnet publish Dependencies_Dotnet.sln -c Debug -o bin/Debug/net7.0
dotnet publish Dependencies_Dotnet.sln -c Release -o bin/Release/net7.0

cd ../../GENie

make

cd ../bgfx

make GENIE=../GENie/bin/darwin/genie tools -j $(sysctl -n hw.logicalcpu)

mkdir -p ../../Tools/bin

cp .build/osx-x64/bin/*cRelease ../../Tools/bin

cd ../../
