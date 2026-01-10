#!/bin/sh

dotnet build Tools.sln -c Release -o bin

cp ../Dependencies/build/native/bin/Release/libStapleToolingSupport.so bin/
cp -R ../Dependencies/SDL_shadercross/Linux/* bin/
cp -R ../Dependencies/slang/Linux/* bin/
cp -R ../Dependencies/cuttlefish/Linux/* bin/
