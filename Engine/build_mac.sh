#!/bin/sh

dotnet build Engine.sln -c Debug
dotnet build Engine.sln -c Release

dotnet build EditorApp/Staple.Editor.App.csproj -r osx-arm64 -c Release --self-contained

../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "EditorApp/bin/Release/net9.0/osx-arm64/Staple.Editor.App*" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "EditorApp/bin/Release/net9.0/osx-arm64/*.dll" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "EditorApp/bin/Release/net9.0/osx-arm64/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "../Dependencies/build/native/bin/Release/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "../Staging/PlayerBackends/MacOSX/Redist/Release/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "../Staging/Packages/com.staple.joltphysics/Plugins/MacOS/libjoltc.dylib" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "../Staging/Packages/com.staple.openal/Plugins/MacOS/libopenal.1.dylib" "../Staging"
