#!/bin/sh

./premake.sh --os=ios --file=premake5-ios.lua xcode4
./premake.sh --os=ios --file=NativeFileDialog/build/premake5.lua xcode4

cd build/ios

xcodebuild -scheme bx -configuration Debug build -workspace Dependencies.xcworkspace
xcodebuild -scheme bx -configuration Release build -workspace Dependencies.xcworkspace

xcodebuild -scheme bimg -configuration Debug build -workspace Dependencies.xcworkspace
xcodebuild -scheme bimg -configuration Release build -workspace Dependencies.xcworkspace

xcodebuild -scheme bgfx -configuration Debug build -workspace Dependencies.xcworkspace
xcodebuild -scheme bgfx -configuration Release build -workspace Dependencies.xcworkspace

xcodebuild -scheme StapleSupport -configuration Debug build -workspace Dependencies.xcworkspace
xcodebuild -scheme StapleSupport -configuration Release build -workspace Dependencies.xcworkspace

cd ../../
