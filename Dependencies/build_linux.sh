#!/bin/sh

set -e

./premake.sh --os=linux gmake
./premake.sh --os=linux --file=NativeFileDialog/build/premake5.lua gmake

cmake -B build/native/freetype/Debug -DCMAKE_BUILD_TYPE=Debug -DBUILD_SHARED_LIBS=true -S freetype -G "Unix Makefiles"

cmake -B build/native/freetype/Release -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=true -S freetype -G "Unix Makefiles"

mkdir -p build/native/bin/Debug

mkdir -p build/native/bin/Release

cd build/native/freetype/Debug

make -j $(nproc)

cp *.so* ../../bin/Debug

cd ../Release

make -j $(nproc)

cp *.so* ../../bin/Release

cd ../../

make -j $(nproc)

make config=debug -j $(nproc)

cd ../dotnet

dotnet publish Dependencies_Dotnet.sln -c Debug -o bin/Debug/net10.0
dotnet publish Dependencies_Dotnet.sln -c Release -o bin/Release/net10.0
