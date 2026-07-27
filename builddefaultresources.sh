#!/bin/sh

./Tools/bin/Baker -i "BuiltinResources" -o "DefaultResources/Windows" -platform Windows -r spirv
./Tools/bin/Baker -i "BuiltinResources" -o "DefaultResources/Linux" -platform Linux -r spirv
./Tools/bin/Baker -i "BuiltinResources" -o "DefaultResources/MacOSX" -platform MacOSX -r metal
./Tools/bin/Baker -i "BuiltinResources" -o "DefaultResources/Android" -platform Android -r spirv
./Tools/bin/Baker -i "BuiltinResources" -o "DefaultResources/iOS" -platform iOS -r metal

./Tools/bin/Packer -p -r -i "DefaultResources/Windows" -o DefaultResources/DefaultResources-Windows.pak
./Tools/bin/Packer -p -r -i "DefaultResources/Linux" -o DefaultResources/DefaultResources-Linux.pak
./Tools/bin/Packer -p -r -i "DefaultResources/MacOSX" -o DefaultResources/DefaultResources-MacOSX.pak
./Tools/bin/Packer -p -r -i "DefaultResources/Android" -o DefaultResources/DefaultResources-Android.pak
./Tools/bin/Packer -p -r -i "DefaultResources/iOS" -o DefaultResources/DefaultResources-iOS.pak
