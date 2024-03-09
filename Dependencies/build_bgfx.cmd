@echo off

cd bgfx

call ..\bx\tools\bin\windows\genie.exe --with-tools --with-shared-lib vs2022

devenv .build\projects\vs2022\bgfx.sln  /Build "Debug|x64"

devenv .build\projects\vs2022\bgfx.sln  /Build "Release|x64"

mkdir ..\..\Tools\bin

copy /B /Y .build/win64_vs2022/bin/*cRelease ../../Tools/bin

cd ..
