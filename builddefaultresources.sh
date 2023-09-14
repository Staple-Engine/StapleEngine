#!/bin/sh

./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/Android" -r opengles -r spirv
./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/Windows" -r d3d11 -r d3d12 -r opengl -r spirv
./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/Linux" -r opengl -r spirv
./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/MacOSX" -r metal
./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/iOS" -r metal
