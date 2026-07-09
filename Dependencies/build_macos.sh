#!/bin/sh

set -e

set CUTTLEFISH_FILENAME=cuttlefish-mac.tar.gz 
set CUTTLEFISH_RELEASE=v2.10.2
set CUTTLEFISH_URL=https://github.com/akb825/Cuttlefish/releases/download/$CUTTLEFISH_RELEASE/$CUTTLEFISH_FILENAME

curl -L -O $CUTTLEFISH_URL

tar -zxf $CUTTLEFISH_FILENAME

rm -f $CUTTLEFISH_FILENAME

set SLANG_RELEASE=2026.10.2
set SLANG_FILENAME=slang-$SLANG_RELEASE-macos-aarch64.tar.gz
set SLANG_URL=https://github.com/shader-slang/slang/releases/download/v$SLANG_RELEASE/$SLANG_FILENAME

mkdir slang

cd slang

curl -L -O $SLANG_URL

tar -zxf $SLANG_FILENAME

rm -f $SLANG_FILENAME

cd ..

premake5 --os=macosx xcode4

cmake -B build/native/freetype/Debug -DCMAKE_BUILD_TYPE=Debug -DBUILD_SHARED_LIBS=true -S freetype -G "Xcode"

cmake -B build/native/freetype/Release -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=true -S freetype -G "Xcode"

mkdir -p build/native/bin/Debug

mkdir -p build/native/bin/Release

cd build/native/freetype/Debug

xcodebuild -scheme freetype -configuration Release build -project freetype.xcodeproj

cp Release/*.dylib ../../bin/Debug

cd ../Release

xcodebuild -scheme freetype -configuration Release build -project freetype.xcodeproj

cp Release/*.dylib ../../bin/Release

cd ../../

xcodebuild -scheme StapleSupport -configuration Debug build -workspace Dependencies.xcworkspace
xcodebuild -scheme StapleSupport -configuration Release build -workspace Dependencies.xcworkspace

xcodebuild -scheme StapleToolingSupport -configuration Debug build -workspace Dependencies.xcworkspace
xcodebuild -scheme StapleToolingSupport -configuration Release build -workspace Dependencies.xcworkspace

cd ../dotnet

dotnet publish Dependencies_Dotnet.sln -c Debug -o bin/Debug/net10.0
dotnet publish Dependencies_Dotnet.sln -c Release -o bin/Release/net10.0
