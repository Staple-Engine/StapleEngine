@echo off

call premake5 vs2022
call premake5 --file=NativeFileDialog/build/premake5.lua vs2022
call premake5 --file=premake5_dotnet.lua vs2022

call build_bgfx.cmd

devenv build\native\Dependencies.sln  /Build "Debug|x64"

devenv build\native\Dependencies.sln  /Build "Release|x64"

dotnet publish build\dotnet\Dependencies_Dotnet.sln -c Debug -o build\dotnet\bin\Debug\net8.0
dotnet publish build\dotnet\Dependencies_Dotnet.sln -c Release -o build\dotnet\bin\Release\net8.0
