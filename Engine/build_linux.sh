#!/bin/sh

premake5 --os=linux vs2019

dotnet publish Engine.sln -c Debug
dotnet publish Engine.sln -c Release
