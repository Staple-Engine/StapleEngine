@echo off

cd bgfx

call ..\bx\tools\bin\windows\genie.exe --with-tools --with-shared-lib vs2022

devenv .build\projects\vs2022\bgfx.sln  /Build "Debug|x64"

devenv .build\projects\vs2022\bgfx.sln  /Build "Release|x64"

pause