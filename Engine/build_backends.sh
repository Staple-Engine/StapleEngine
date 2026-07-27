#!/bin/sh

dotnet build "Staple.Player.Windows/Staple.Player.Windows.csproj" -c Debug -o "../Staging/PlayerBackends/Windows/Runtime/Debug" /p:STAPLE_WINDOWS=true /p:TargetFramework=net10.0
dotnet build "Staple.Player.Linux/Staple.Player.Linux.csproj" -c Debug -o "../Staging/PlayerBackends/Linux/Runtime/Debug" /p:STAPLE_LINUX=true /p:TargetFramework=net10.0
dotnet build "Staple.Player.MacOSX/Staple.Player.MacOSX.csproj" -c Debug -o "../Staging/PlayerBackends/MacOSX/Runtime/Debug" /p:STAPLE_OSX=true /p:TargetFramework=net10.0
#dotnet build "Staple.Core/Staple.Core.csproj" -c Debug -o "../Staging/PlayerBackends/iOS/Runtime/Debug" /p:STAPLE_IOS=true /p:TargetFramework=net10.0-ios15.0
dotnet build "Staple.Player.Android/Staple.Player.Android.csproj" -c Debug -o "../Staging/PlayerBackends/Android/Runtime/Debug" /p:STAPLE_ANDROID=true /p:TargetFramework=net10.0-android

dotnet build "Staple.Player.Windows/Staple.Player.Windows.csproj" -c Release -o "../Staging/PlayerBackends/Windows/Runtime/Release" /p:STAPLE_WINDOWS=true /p:TargetFramework=net10.0
dotnet build "Staple.Player.Linux/Staple.Player.Linux.csproj" -c Release -o "../Staging/PlayerBackends/Linux/Runtime/Release" /p:STAPLE_LINUX=true /p:TargetFramework=net10.0
dotnet build "Staple.Player.MacOSX/Staple.Player.MacOSX.csproj" -c Release -o "../Staging/PlayerBackends/MacOSX/Runtime/Release" /p:STAPLE_OSX=true /p:TargetFramework=net10.0
#dotnet build "Staple.Core/Staple.Core.csproj" -c Release -o "../Staging/PlayerBackends/iOS/Runtime/Release" /p:STAPLE_IOS=true /p:TargetFramework=net10.0-ios15.0
dotnet build "Staple.Player.Android/Staple.Player.Android.csproj" -c Release -o "../Staging/PlayerBackends/Android/Runtime/Release" /p:STAPLE_ANDROID=true /p:TargetFramework=net10.0-android

cp -Rf TypeRegistration "../Staging/PlayerBackends/Windows/Runtime/"
cp -Rf TypeRegistration "../Staging/PlayerBackends/Linux/Runtime/"
cp -Rf TypeRegistration "../Staging/PlayerBackends/MacOSX/Runtime/"
cp -Rf TypeRegistration "../Staging/PlayerBackends/iOS/Runtime/"
cp -Rf TypeRegistration "../Staging/PlayerBackends/Android/Runtime/"
