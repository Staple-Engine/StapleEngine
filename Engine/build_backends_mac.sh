#!/bin/sh
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/iOS/Runtime/Debug" /p:STAPLE_IOS=true /p:TargetFramework=net8.0-ios

dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/iOS/Runtime/Release" /p:STAPLE_IOS=true /p:TargetFramework=net8.0-ios

dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Debug -o "../Staging/Player Backends/iOS/Modules/Debug" /p:STAPLE_IOS=true /p:TargetFramework=net8.0-ios

dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Release -o "../Staging/Player Backends/iOS/Modules/Release" /p:STAPLE_IOS=true /p:TargetFramework=net8.0-ios

dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Debug -o "../Staging/Player Backends/iOS/Modules/Debug" /p:STAPLE_IOS=true /p:TargetFramework=net8.0-ios

dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Release -o "../Staging/Player Backends/iOS/Modules/Release" /p:STAPLE_IOS=true /p:TargetFramework=net8.0-ios
