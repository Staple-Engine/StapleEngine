#!/bin/sh

export PATH=$ANDROID_NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64/bin:$PATH

premake5 --os=android --file=premake5_android.lua cmake
premake5 --os=android --file=premake5_dotnet.lua vs2022

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_PLATFORM=26 -DANDROID_ABI=arm64-v8a -DCMAKE_INSTALL_PREFIX:String="Sdk" -B build/native/freetype/Debug -DCMAKE_BUILD_TYPE=Debug -DBUILD_SHARED_LIBS=true -S freetype -G "Unix Makefiles"

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_PLATFORM=26 -DANDROID_ABI=arm64-v8a -DCMAKE_INSTALL_PREFIX:String="Sdk" -B build/native/freetype/Release -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=true -S freetype -G "Unix Makefiles"

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_PLATFORM=26 -DANDROID_ABI=arm64-v8a -DCMAKE_BUILD_TYPE=Debug -S build/native -DCMAKE_INSTALL_PREFIX:String="Sdk" -B build/native -G "Unix Makefiles"

cd build/native/freetype/Debug

make -j $(nproc)

cd ../Release

make -j $(nproc)

cd ../../../../

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_PLATFORM=26 -DANDROID_ABI=arm64-v8a -DCMAKE_BUILD_TYPE=Release -S build/native -DCMAKE_INSTALL_PREFIX:String="Sdk" -B build/native -G "Unix Makefiles"

cd build/native

make -j $(nproc)
