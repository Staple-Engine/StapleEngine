#!/bin/sh
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/iOS/Runtime/Debug" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios

dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/iOS/Runtime/Release" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios

dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Debug -o "../Staging/Player Backends/iOS/Modules/StapleJoltPhysics/Assembly/Debug" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios

dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Release -o "../Staging/Player Backends/iOS/Modules/StapleJoltPhysics/Assembly/Release" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios

dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Debug -o "../Staging/Player Backends/iOS/Modules/StapleOpenALAudio/Assembly/Debug" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios

dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Release -o "../Staging/Player Backends/iOS/Modules/StapleOpenALAudio/Assembly/Release" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios

dotnet build "StapleNetworking/StapleNetworking.csproj" -c Debug -o "../Staging/Player Backends/iOS/Modules/StapleNetworking/Assembly/Debug" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios

dotnet build "StapleNetworking/StapleNetworking.csproj" -c Release -o "../Staging/Player Backends/iOS/Modules/StapleNetworking/Assembly/Release" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios
