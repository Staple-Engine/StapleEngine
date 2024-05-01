@echo off
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/Windows/Runtime/Debug" /p:STAPLE_WINDOWS=true /p:TargetFramework=net8.0
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/Linux/Runtime/Debug" /p:STAPLE_LINUX=true /p:TargetFramework=net8.0
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/MacOSX/Runtime/Debug" /p:STAPLE_OSX=true /p:TargetFramework=net8.0
rem dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/iOS/Runtime/Debug" /p:TargetFramework=net8.0-ios15.0
dotnet build "Core/StapleCore.csproj" -c Debug -o "../Staging/Player Backends/Android/Runtime/Debug" /p:TargetFramework=net8.0-android

dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/Windows/Runtime/Release" /p:STAPLE_WINDOWS=true /p:TargetFramework=net8.0
dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/Linux/Runtime/Release" /p:STAPLE_LINUX=true /p:TargetFramework=net8.0
dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/MacOSX/Runtime/Release" /p:STAPLE_OSX=true /p:TargetFramework=net8.0
rem dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/iOS/Runtime/Release" /p:TargetFramework=net8.0-ios15.0
dotnet build "Core/StapleCore.csproj" -c Release -o "../Staging/Player Backends/Android/Runtime/Release" /p:TargetFramework=net8.0-android

dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Debug -o "../Staging/Player Backends/Windows/Modules/Debug" /p:STAPLE_WINDOWS=true /p:TargetFramework=net8.0
dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Debug -o "../Staging/Player Backends/Linux/Modules/Debug" /p:STAPLE_LINUX=true /p:TargetFramework=net8.0
dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Debug -o "../Staging/Player Backends/MacOSX/Modules/Debug" /p:STAPLE_OSX=true /p:TargetFramework=net8.0
rem dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Debug -o "../Staging/Player Backends/iOS/Modules/Debug" /p:TargetFramework=net8.0-ios15.0
dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Debug -o "../Staging/Player Backends/Android/Modules/Debug" /p:TargetFramework=net8.0-android

dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Release -o "../Staging/Player Backends/Windows/Modules/Release" /p:STAPLE_WINDOWS=true /p:TargetFramework=net8.0
dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Release -o "../Staging/Player Backends/Linux/Modules/Release" /p:STAPLE_LINUX=true /p:TargetFramework=net8.0
dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Release -o "../Staging/Player Backends/MacOSX/Modules/Release" /p:STAPLE_OSX=true /p:TargetFramework=net8.0
rem dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Debug -o "../Staging/Player Backends/iOS/Modules/Release" /p:TargetFramework=net8.0-ios15.0
dotnet build "StapleJoltPhysics/StapleJoltPhysics.csproj" -c Release -o "../Staging/Player Backends/Android/Modules/Release" /p:TargetFramework=net8.0-android

dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Debug -o "../Staging/Player Backends/Windows/Modules/Debug" /p:STAPLE_WINDOWS=true /p:TargetFramework=net8.0
dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Debug -o "../Staging/Player Backends/Linux/Modules/Debug" /p:STAPLE_LINUX=true /p:TargetFramework=net8.0
dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Debug -o "../Staging/Player Backends/MacOSX/Modules/Debug" /p:STAPLE_OSX=true /p:TargetFramework=net8.0
rem dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Debug -o "../Staging/Player Backends/iOS/Modules/Debug" /p:TargetFramework=net8.0-ios15.0
dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Debug -o "../Staging/Player Backends/Android/Modules/Debug" /p:TargetFramework=net8.0-android

dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Release -o "../Staging/Player Backends/Windows/Modules/Release" /p:STAPLE_WINDOWS=true /p:TargetFramework=net8.0
dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Release -o "../Staging/Player Backends/Linux/Modules/Release" /p:STAPLE_LINUX=true /p:TargetFramework=net8.0
dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Release -o "../Staging/Player Backends/MacOSX/Modules/Release" /p:STAPLE_OSX=true /p:TargetFramework=net8.0
rem dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Release -o "../Staging/Player Backends/iOS/Modules/Release" /p:TargetFramework=net8.0-ios15.0
dotnet build "StapleOpenALAudio/StapleOpenALAudio.csproj" -c Release -o "../Staging/Player Backends/Android/Modules/Release" /p:TargetFramework=net8.0-android

robocopy TypeRegistration\ "..\Staging\Player Backends\Windows\Runtime\TypeRegistration" /E /NFL /NDL /NJH /NJS /NP /NS /NC
robocopy TypeRegistration\ "..\Staging\Player Backends\Linux\Runtime\TypeRegistration" /E /NFL /NDL /NJH /NJS /NP /NS /NC
robocopy TypeRegistration\ "..\Staging\Player Backends\MacOSX\Runtime\TypeRegistration" /E /NFL /NDL /NJH /NJS /NP /NS /NC
robocopy TypeRegistration\ "..\Staging\Player Backends\Android\Runtime\TypeRegistration" /E /NFL /NDL /NJH /NJS /NP /NS /NC

if ErrorLevel 8 exit /B 1

exit /B 0
