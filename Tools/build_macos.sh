#!/bin/sh

dotnet build Tools.sln -c Release -o bin

cp ../Dependencies/build/native/bin/Release/libStapleToolingSupport.dylib bin/
cp -R ../Dependencies/SDL_shadercross/* bin/
cp -R ../Dependencies/slang/* bin/
cp -R ../Dependencies/cuttlefish/* bin/
