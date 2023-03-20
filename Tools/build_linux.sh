#!/bin/sh

premake5 --os=linux vs2022

msbuild Tools.sln
