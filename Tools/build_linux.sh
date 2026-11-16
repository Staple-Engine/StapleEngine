#!/bin/sh

dotnet build Tools.sln -c Release -o bin

rm -Rf bin/bin
rm -Rf bin/shadercross
rm -Rf bin/slang
rm -Rf bin/cuttlefish
rm -Rf staging

mkdir bin/bin
mkdir bin/lib
mkdir bin/slang
mkdir -p staging/shadercross
mkdir -p staging/cuttlefish

cp ../Dependencies/build/native/bin/Release/libStapleToolingSupport.so bin/
cp -R ../Dependencies/SDL_shadercross/* staging/shadercross
cp -R ../Dependencies/slang/* bin/slang
cp -R ../Dependencies/cuttlefish/* staging/cuttlefish

cp -R staging/shadercross/bin/* bin/bin/
cp -R staging/shadercross/lib/* bin/lib/
cp -R staging/cuttlefish/bin/* bin/
cp -R staging/cuttlefish/lib/* bin/

rm -Rf staging/shadercross
rm -Rf staging/cuttlefish
