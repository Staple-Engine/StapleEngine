#!/bin/sh

./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/Windows" -platform Windows -r opengl -r spirv
./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/Linux" -platform Linux -r opengl -r spirv
./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/MacOSX" -platform MacOSX -r metal
./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/Android" -platform Android -r opengles -r spirv
./Tools/bin/Baker -i "Builtin Resources" -o "DefaultResources/iOS" -platform iOS -r metal

./Tools/bin/Packer -p -i "DefaultResources/Windows" -o DefaultResources/DefaultResources-Windows.pak
./Tools/bin/Packer -p -i "DefaultResources/Linux" -o DefaultResources/DefaultResources-Linux.pak
./Tools/bin/Packer -p -i "DefaultResources/MacOSX" -o DefaultResources/DefaultResources-MacOSX.pak
./Tools/bin/Packer -p -i "DefaultResources/Android" -o DefaultResources/DefaultResources-Android.pak
./Tools/bin/Packer -p -i "DefaultResources/iOS" -o DefaultResources/DefaultResources-iOS.pak
