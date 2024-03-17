#!/bin/sh

set -e

premake5 --os=linux gmake
premake5 --os=linux --file=NativeFileDialog/build/premake5.lua gmake
premake5 --os=linux --file=premake5_dotnet.lua vs2022

cd build/native

make -j $(nproc)

make config=debug -j $(nproc)

cd ../../build/dotnet

dotnet publish Dependencies_Dotnet.sln -c Debug -o bin/Debug/net7.0
dotnet publish Dependencies_Dotnet.sln -c Release -o bin/Release/net7.0

cd ../../GENie

make

cd ../bgfx

make GENIE=../GENie/bin/linux/genie projgen

make GENIE=../GENie/bin/linux/genie tools -j $(nproc)

mkdir -p ../../Tools/bin

cp .build/linux64_gcc/bin/*cRelease ../../Tools/bin

cd ../../
