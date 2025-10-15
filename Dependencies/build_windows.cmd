@echo off

call premake5 vs2022
call premake5 --file=NativeFileDialog/build/premake5.lua vs2022
call premake5 --file=premake5_dotnet.lua vs2022

cmake -B build\native\freetype\Debug -DCMAKE_BUILD_TYPE=Debug -DBUILD_SHARED_LIBS=true -S freetype -G "Visual Studio 17 2022"

cmake -B build\native\freetype\Release -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=true -S freetype -G "Visual Studio 17 2022"

rem call build_bgfx.cmd

devenv build\native\freetype\Debug\freetype.sln  /Build "Debug|x64"

devenv build\native\freetype\Release\freetype.sln  /Build "Release|x64"

copy /Y build\native\freetype\Debug\Debug\*.dll build\native\bin\Debug\

copy /Y build\native\freetype\Release\Release\*.dll build\native\bin\Release\

devenv build\native\Dependencies.sln  /Build "Debug|x64"

devenv build\native\Dependencies.sln  /Build "Release|x64"

dotnet build build\dotnet\Dependencies_Dotnet.sln -c Debug -p:Platform="Any CPU" -o build\dotnet\bin\Debug\net10.0
dotnet build build\dotnet\Dependencies_Dotnet.sln -c Release -p:Platform="Any CPU" -o build\dotnet\bin\Release\net10.0
