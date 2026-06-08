#!/bin/sh

dotnet build Tools.sln -c Release -o bin

cp ../Dependencies/build/native/bin/Release/libStapleToolingSupport.dylib bin/
cp -R ../Dependencies/SDL_shadercross/MacOSX/* bin/
cp -R ../Dependencies/slang/MacOSX/* bin/
cp -R ../Dependencies/cuttlefish/MacOSX/* bin/
