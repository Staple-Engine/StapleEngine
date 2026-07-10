@echo off

set CUTTLEFISH_FILENAME=cuttlefish-win64-tool.zip
set CUTTLEFISH_RELEASE=v2.10.2
set CUTTLEFISH_URL=https://github.com/akb825/Cuttlefish/releases/download/%CUTTLEFISH_RELEASE%/%CUTTLEFISH_FILENAME%

curl -L -O "%CUTTLEFISH_URL%"

tar -xf %CUTTLEFISH_FILENAME%

del /S /Q %CUTTLEFISH_FILENAME%

set SLANG_FILENAME=slang-2026.10.2-windows-x86_64.tar.gz
set SLANG_RELEASE=v2026.10.2
set SLANG_URL=https://github.com/shader-slang/slang/releases/download/%SLANG_RELEASE%/%SLANG_FILENAME%

mkdir slang

cd slang

curl -L -O "%SLANG_URL%"

tar -zxf %SLANG_FILENAME%

del /S /Q %SLANG_FILENAME%

cd ..

call premake5 vs2026
call premake5 --file=NativeFileDialog/build/premake5.lua vs2026

cmake -B build\native\freetype\Debug -DCMAKE_BUILD_TYPE=Debug -DBUILD_SHARED_LIBS=true -S freetype -G "Visual Studio 18 2026"

cmake -B build\native\freetype\Release -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=true -S freetype -G "Visual Studio 18 2026"

devenv build\native\freetype\Debug\freetype.slnx  /Build "Debug|x64"

devenv build\native\freetype\Release\freetype.slnx  /Build "Release|x64"

copy /Y build\native\freetype\Debug\Debug\*.dll build\native\bin\Debug\

copy /Y build\native\freetype\Release\Release\*.dll build\native\bin\Release\

devenv build\native\Dependencies.slnx  /Build "Debug|x64"

devenv build\native\Dependencies.slnx  /Build "Release|x64"

dotnet build build\dotnet\Dependencies_Dotnet.sln -c Debug -p:Platform="Any CPU" -o build\dotnet\bin\Debug\net10.0
dotnet build build\dotnet\Dependencies_Dotnet.sln -c Release -p:Platform="Any CPU" -o build\dotnet\bin\Release\net10.0
