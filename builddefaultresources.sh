#!/bin/sh

./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/Windows" -platform Windows -r d3d11 -r d3d12 -r opengl -r spirv
./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/Linux" -platform Linux -r opengl -r spirv
./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/MacOSX" -platform MacOSX -r metal
./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/Android" -platform Android -r opengles -r spirv
./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/iOS" -platform iOS -r metal
