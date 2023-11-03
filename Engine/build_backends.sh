#!/bin/sh
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/Windows/Runtime/Debug" /p:STAPLE_WINDOWS=true
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/Linux/Runtime/Debug" /p:STAPLE_LINUX=true
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/MacOSX/Runtime/Debug" /p:STAPLE_OSX=true
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/Android/Runtime/Debug" /p:TargetFramework=net7.0-android

dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/Windows/Runtime/Release" /p:STAPLE_WINDOWS=true
dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/Linux/Runtime/Release" /p:STAPLE_LINUX=true
dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/MacOSX/Runtime/Release" /p:STAPLE_OSX=true
dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/Android/Runtime/Release" /p:TargetFramework=net7.0-android
