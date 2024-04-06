#!/bin/sh

set -e

rm -Rf dist

mkdir -p dist/StapleEngine
mkdir -p dist/StapleEngine/DefaultResources
mkdir -p dist/StapleEngine/Editor
mkdir -p dist/StapleEngine/Tools/bin
mkdir -p dist/StapleEngine/Tools/ShaderIncludes

cp -Rf DefaultResources dist/StapleEngine/
cp -Rf Staging/* dist/StapleEngine/Editor/
cp -Rf Tools/bin/* dist/StapleEngine/Tools/bin/
cp -Rf Tools/ShaderIncludes/* dist/StapleEngine/Tools/ShaderIncludes/
cp -Rf Engine/EditorApp/bin/Release/net8.0/runtimes dist/StapleEngine/Editor/runtimes
cp -Rf Staging/Player\ Backends/Linux/Redist/Debug/* dist/StapleEngine/Editor/

rm -Rf dist/StapleEngine/DefaultResources/Android
rm -Rf dist/StapleEngine/DefaultResources/iOS
rm -Rf dist/StapleEngine/DefaultResources/Linux
rm -Rf dist/StapleEngine/DefaultResources/MacOSX
rm -Rf dist/StapleEngine/DefaultResources/Windows
