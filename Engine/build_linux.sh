#!/bin/sh

dotnet build Engine.sln -c Debug
dotnet build Engine.sln -c Release

dotnet publish EditorApp/Staple.Editor.App.csproj -r linux-x64 -c Release --self-contained

../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "EditorApp/bin/Release/net9.0/linux-x64/Staple.Editor.App*" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "EditorApp/bin/Release/net9.0/linux-x64/*.dll" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "EditorApp/bin/Release/net9.0/linux-x64/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "../Dependencies/build/native/bin/Release/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "../Staging/PlayerBackends/Linux/Redist/Release/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "../Staging/Packages/com.staple.joltphysics/Plugins/Linux/libjoltc.so" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "../Staging/Packages/com.staple.openal/Plugins/Linux/libopenal.so" "../Staging"
