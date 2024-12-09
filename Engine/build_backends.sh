#!/bin/sh
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/Windows/Runtime/Debug" /p:STAPLE_WINDOWS=true /p:TargetFramework=net9.0
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/Linux/Runtime/Debug" /p:STAPLE_LINUX=true /p:TargetFramework=net9.0
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/MacOSX/Runtime/Debug" /p:STAPLE_OSX=true /p:TargetFramework=net9.0
#dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/iOS/Runtime/Debug" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios15.0
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/Android/Runtime/Debug" /p:STAPLE_ANDROID=true /p:TargetFramework=net9.0-android

dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/Windows/Runtime/Release" /p:STAPLE_WINDOWS=true /p:TargetFramework=net9.0
dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/Linux/Runtime/Release" /p:STAPLE_LINUX=true /p:TargetFramework=net9.0
dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/MacOSX/Runtime/Release" /p:STAPLE_OSX=true /p:TargetFramework=net9.0
#dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/iOS/Runtime/Release" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios15.0
dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/Android/Runtime/Release" /p:STAPLE_ANDROID=true /p:TargetFramework=net9.0-android

dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Debug -o "../Staging/Player Backends/Windows/Modules/StapleJoltPhysics/Assembly/Debug" /p:STAPLE_WINDOWS=true /p:TargetFramework=net9.0
dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Debug -o "../Staging/Player Backends/Linux/Modules/StapleJoltPhysics/Assembly/Debug" /p:STAPLE_LINUX=true /p:TargetFramework=net9.0
dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Debug -o "../Staging/Player Backends/MacOSX/Modules/StapleJoltPhysics/Assembly/Debug" /p:STAPLE_OSX=true /p:TargetFramework=net9.0
#dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Debug -o "../Staging/Player Backends/iOS/Modules/StapleJoltPhysics/Assembly/Debug" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios15.0
dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Debug -o "../Staging/Player Backends/Android/Modules/StapleJoltPhysics/Assembly/Debug" /p:STAPLE_ANDROID=true /p:TargetFramework=net9.0-android

dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Release -o "../Staging/Player Backends/Windows/Modules/StapleJoltPhysics/Assembly/Release" /p:STAPLE_WINDOWS=true /p:TargetFramework=net9.0
dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Release -o "../Staging/Player Backends/Linux/Modules/StapleJoltPhysics/Assembly/Release" /p:STAPLE_LINUX=true /p:TargetFramework=net9.0
dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Release -o "../Staging/Player Backends/MacOSX/Modules/StapleJoltPhysics/Assembly/Release" /p:STAPLE_OSX=true /p:TargetFramework=net9.0
#dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Release -o "../Staging/Player Backends/iOS/Modules/StapleJoltPhysics/Assembly/Release" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios15.0
dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Release -o "../Staging/Player Backends/Android/Modules/StapleJoltPhysics/Assembly/Release" /p:STAPLE_ANDROID=true /p:TargetFramework=net9.0-android

dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Debug -o "../Staging/Player Backends/Windows/Modules/StapleOpenALAudio/Assembly/Debug" /p:STAPLE_WINDOWS=true /p:TargetFramework=net9.0
dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Debug -o "../Staging/Player Backends/Linux/Modules/StapleOpenALAudio/Assembly/Debug" /p:STAPLE_LINUX=true /p:TargetFramework=net9.0
dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Debug -o "../Staging/Player Backends/MacOSX/Modules/StapleOpenALAudio/Assembly/Debug" /p:STAPLE_OSX=true /p:TargetFramework=net9.0
#dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Debug -o "../Staging/Player Backends/iOS/Modules/StapleOpenALAudio/Assembly/Debug" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios15.0
dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Debug -o "../Staging/Player Backends/Android/Modules/StapleOpenALAudio/Assembly/Debug" /p:STAPLE_ANDROID=true /p:TargetFramework=net9.0-android

dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Release -o "../Staging/Player Backends/Windows/Modules/StapleOpenALAudio/Assembly/Release" /p:STAPLE_WINDOWS=true /p:TargetFramework=net9.0
dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Release -o "../Staging/Player Backends/Linux/Modules/StapleOpenALAudio/Assembly/Release" /p:STAPLE_LINUX=true /p:TargetFramework=net9.0
dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Release -o "../Staging/Player Backends/MacOSX/Modules/StapleOpenALAudio/Assembly/Release" /p:STAPLE_OSX=true /p:TargetFramework=net9.0
#dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Release -o "../Staging/Player Backends/iOS/Modules/StapleOpenALAudio/Assembly/Release" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios15.0
dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Release -o "../Staging/Player Backends/Android/Modules/StapleOpenALAudio/Assembly/Release" /p:STAPLE_ANDROID=true /p:TargetFramework=net9.0-android

dotnet build "StapleNetworking/StapleNetworking.csproj" -c Debug -o "../Staging/Player Backends/Windows/Modules/StapleNetworking/Assembly/Debug" /p:STAPLE_WINDOWS=true /p:TargetFramework=net9.0
dotnet build "StapleNetworking/StapleNetworking.csproj" -c Debug -o "../Staging/Player Backends/Linux/Modules/StapleNetworking/Assembly/Debug" /p:STAPLE_LINUX=true /p:TargetFramework=net9.0
dotnet build "StapleNetworking/StapleNetworking.csproj" -c Debug -o "../Staging/Player Backends/MacOSX/Modules/StapleNetworking/Assembly/Debug" /p:STAPLE_OSX=true /p:TargetFramework=net9.0
#dotnet build "StapleNetworking/StapleNetworking.csproj" -c Debug -o "../Staging/Player Backends/iOS/Modules/StapleNetworking/Assembly/Debug" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios15.0
dotnet build "StapleNetworking/StapleNetworking.csproj" -c Debug -o "../Staging/Player Backends/Android/Modules/StapleNetworking/Assembly/Debug" /p:STAPLE_ANDROID=true /p:TargetFramework=net9.0-android

dotnet build "StapleNetworking/StapleNetworking.csproj" -c Release -o "../Staging/Player Backends/Windows/Modules/StapleNetworking/Assembly/Release" /p:STAPLE_WINDOWS=true /p:TargetFramework=net9.0
dotnet build "StapleNetworking/StapleNetworking.csproj" -c Release -o "../Staging/Player Backends/Linux/Modules/StapleNetworking/Assembly/Release" /p:STAPLE_LINUX=true /p:TargetFramework=net9.0
dotnet build "StapleNetworking/StapleNetworking.csproj" -c Release -o "../Staging/Player Backends/MacOSX/Modules/StapleNetworking/Assembly/Release" /p:STAPLE_OSX=true /p:TargetFramework=net9.0
#dotnet build "StapleNetworking/StapleNetworking.csproj" -c Release -o "../Staging/Player Backends/iOS/Modules/StapleNetworking/Assembly/Release" /p:STAPLE_IOS=true /p:TargetFramework=net9.0-ios15.0
dotnet build "StapleNetworking/StapleNetworking.csproj" -c Release -o "../Staging/Player Backends/Android/Modules/StapleNetworking/Assembly/Release" /p:STAPLE_ANDROID=true /p:TargetFramework=net9.0-android

cp -Rf TypeRegistration "../Staging/Player Backends/Windows/Runtime/"
cp -Rf TypeRegistration "../Staging/Player Backends/Linux/Runtime/"
cp -Rf TypeRegistration "../Staging/Player Backends/MacOSX/Runtime/"
cp -Rf TypeRegistration "../Staging/Player Backends/iOS/Runtime/"
cp -Rf TypeRegistration "../Staging/Player Backends/Android/Runtime/"
