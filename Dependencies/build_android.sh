#!/bin/sh

export PATH=$ANDROID_NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64/bin:$PATH

premake5 --os=android --file=premake5_android.lua cmake
premake5 --os=android --file=premake5_dotnet.lua vs2022

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_PLATFORM=26 -DANDROID_ABI=arm64-v8a -DCMAKE_BUILD_TYPE=Debug -S build/native -DCMAKE_INSTALL_PREFIX:String="Sdk" -B build/native -G "Unix Makefiles"

cd build/native

make -j $(nproc)

cd ../../

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_PLATFORM=26 -DANDROID_ABI=arm64-v8a -DCMAKE_BUILD_TYPE=Release -S build/native -DCMAKE_INSTALL_PREFIX:String="Sdk" -B build/native -G "Unix Makefiles"

cd build/native

make -j $(nproc)

cd ../../build/dotnet

dotnet publish Dependencies_Dotnet.sln -c Debug -o bin/Debug/net7.0
dotnet publish Dependencies_Dotnet.sln -c Release -o bin/Release/net7.0

cd ../../GENie

make

cd ../bgfx

make GENIE=../GENie/bin/linux/genie tools -j $(nproc)

mkdir -p ../../Tools/bin

cp .build/linux64_gcc/bin/*cRelease ../../Tools/bin

cd ../../
