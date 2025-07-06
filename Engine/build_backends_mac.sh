#!/bin/sh
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/PlayerBackends/iOS/Runtime/Debug" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios

dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/PlayerBackends/iOS/Runtime/Release" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios
