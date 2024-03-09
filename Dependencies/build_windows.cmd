@echo off

call C:\premake\premake5 vs2022
call C:\premake\premake5 --file=NativeFileDialog/build/premake5.lua vs2022
call C:\premake\premake5 --file=premake5_dotnet.lua vs2022

call build_bgfx.cmd

devenv build\native\Dependencies.sln  /Build "Debug|x64"

devenv build\native\Dependencies.sln  /Build "Release|x64"
