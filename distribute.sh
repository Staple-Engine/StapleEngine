#!/bin/sh

set -e

rm -Rf dist

mkdir dist/StapleEngine

cp -Rf DefaultResources dist/StapleEngine/DefaultResources
cp -Rf Staging dist/StapleEngine/Editor
cp -Rf Tools/bin dist/StapleEngine/Tools/bin
cp -Rf Tools/ShaderIncludes dist/StapleEngine/Tools/ShaderIncludes

rm -Rf dist/StapleEngine/DefaultResources/Android
rm -Rf dist/StapleEngine/DefaultResources/iOS
rm -Rf dist/StapleEngine/DefaultResources/Linux
rm -Rf dist/StapleEngine/DefaultResources/MacOSX
rm -Rf dist/StapleEngine/DefaultResources/Windows
