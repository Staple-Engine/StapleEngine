#!/bin/sh

set -e

./premake.sh --os=linux gmake
./premake.sh --os=linux --file=NativeFileDialog/build/premake5.lua gmake
./premake.sh --os=linux --file=premake5_dotnet.lua vs2022

cmake -B build/native/freetype/Debug -DCMAKE_BUILD_TYPE=Debug -DBUILD_SHARED_LIBS=true -S freetype -G "Unix Makefiles"

cmake -B build/native/freetype/Release -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=true -S freetype -G "Unix Makefiles"

cd build/native/freetype/Debug

make -j $(nproc)

cd ../Release

make -j $(nproc)

cd ../../

make -j $(nproc)

make config=debug -j $(nproc)

cd ../dotnet

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
