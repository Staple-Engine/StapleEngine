#!/bin/sh

premake5 --os=linux vs2019

msbuild Tools.sln
