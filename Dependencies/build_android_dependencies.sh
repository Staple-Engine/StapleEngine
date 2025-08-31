#!/bin/sh

export PATH=$ANDROID_NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64/bin:$PATH

premake5 --os=android --file=premake5_android.lua cmake
premake5 --os=android --file=premake5_dotnet.lua vs2022

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_PLATFORM=26 -DANDROID_ABI=arm64-v8a -DCMAKE_INSTALL_PREFIX:String="Sdk" -B build/native/joltc -DCMAKE_BUILD_TYPE=Distribution -DBUILD_SHARED_LIBS=true -DCMAKE_SHARED_LINKER_FLAGS="-Wl,-z,max-page-size=16384" -S joltc -G "Unix Makefiles"

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_PLATFORM=26 -DANDROID_ABI=arm64-v8a -DCMAKE_INSTALL_PREFIX:String="Sdk" -B build/native/openal-soft/Debug -DCMAKE_BUILD_TYPE=Debug -DBUILD_SHARED_LIBS=true -DCMAKE_SHARED_LINKER_FLAGS="-Wl,-z,max-page-size=16384" -S openal-soft -G "Unix Makefiles"

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_PLATFORM=26 -DANDROID_ABI=arm64-v8a -DCMAKE_INSTALL_PREFIX:String="Sdk" -B build/native/openal-soft/Release -DCMAKE_BUILD_TYPE=Distribution -DBUILD_SHARED_LIBS=true -DCMAKE_SHARED_LINKER_FLAGS="-Wl,-z,max-page-size=16384" -S openal-soft -G "Unix Makefiles"

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_PLATFORM=26 -DANDROID_ABI=arm64-v8a -DCMAKE_INSTALL_PREFIX:String="Sdk" -B build/native/freetype/Debug -DCMAKE_BUILD_TYPE=Debug -DBUILD_SHARED_LIBS=true -DCMAKE_SHARED_LINKER_FLAGS="-Wl,-z,max-page-size=16384" -S freetype -G "Unix Makefiles"

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_PLATFORM=26 -DANDROID_ABI=arm64-v8a -DCMAKE_INSTALL_PREFIX:String="Sdk" -B build/native/freetype/Release -DCMAKE_BUILD_TYPE=Distribution -DBUILD_SHARED_LIBS=true -DCMAKE_SHARED_LINKER_FLAGS="-Wl,-z,max-page-size=16384" -S freetype -G "Unix Makefiles"

cd build/native/joltc

make -j $(nproc)

llvm-strip --strip-unneeded lib/libjoltc.so

cd ../freetype/Debug

make -j $(nproc)

cd ../Release

make -j $(nproc)

llvm-strip --strip-unneeded libfreetype.so

cd ../../openal-soft/Debug

make -j $(nproc)

cd ../Release

make -j $(nproc)

llvm-strip --strip-unneeded libopenal.so

cd ../../../../

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_PLATFORM=26 -DANDROID_ABI=arm64-v8a -DCMAKE_BUILD_TYPE=Debug -S build/native -DCMAKE_INSTALL_PREFIX:String="Sdk" -DCMAKE_SHARED_LINKER_FLAGS="-Wl,-z,max-page-size=16384" -B build/native -G "Unix Makefiles"

cd build/native

make -j $(nproc)

cd ../../

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_PLATFORM=26 -DANDROID_ABI=arm64-v8a -DCMAKE_BUILD_TYPE=Release -S build/native -DCMAKE_INSTALL_PREFIX:String="Sdk" -DCMAKE_SHARED_LINKER_FLAGS="-Wl,-z,max-page-size=16384" -B build/native -G "Unix Makefiles"

cd build/native

make -j $(nproc)

llvm-strip --strip-unneeded bin/Release/libbgfx.so
llvm-strip --strip-unneeded bin/Release/libStapleSupport.so
