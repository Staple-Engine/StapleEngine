#!/bin/sh
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/PlayerBackends/Windows/Runtime/Debug" /p:STAPLE_WINDOWS=true /p:TargetFramework=net9.0
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/PlayerBackends/Linux/Runtime/Debug" /p:STAPLE_LINUX=true /p:TargetFramework=net9.0
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/PlayerBackends/MacOSX/Runtime/Debug" /p:STAPLE_OSX=true /p:TargetFramework=net9.0
#dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/PlayerBackends/iOS/Runtime/Debug" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios15.0
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/PlayerBackends/Android/Runtime/Debug" /p:STAPLE_ANDROID=true /p:TargetFramework=net9.0-android

dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/PlayerBackends/Windows/Runtime/Release" /p:STAPLE_WINDOWS=true /p:TargetFramework=net9.0
dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/PlayerBackends/Linux/Runtime/Release" /p:STAPLE_LINUX=true /p:TargetFramework=net9.0
dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/PlayerBackends/MacOSX/Runtime/Release" /p:STAPLE_OSX=true /p:TargetFramework=net9.0
#dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/PlayerBackends/iOS/Runtime/Release" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios15.0
dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/PlayerBackends/Android/Runtime/Release" /p:STAPLE_ANDROID=true /p:TargetFramework=net9.0-android

cp -Rf TypeRegistration "../Staging/PlayerBackends/Windows/Runtime/"
cp -Rf TypeRegistration "../Staging/PlayerBackends/Linux/Runtime/"
cp -Rf TypeRegistration "../Staging/PlayerBackends/MacOSX/Runtime/"
cp -Rf TypeRegistration "../Staging/PlayerBackends/iOS/Runtime/"
cp -Rf TypeRegistration "../Staging/PlayerBackends/Android/Runtime/"
