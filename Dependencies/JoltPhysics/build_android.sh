#!/bin/sh

export PATH=$ANDROID_NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64/bin:$PATH

cmake -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_ABI=arm64-v8a -S ./../ -DCMAKE_INSTALL_PREFIX:String="Sdk" -B build -G "Unix Makefiles"
