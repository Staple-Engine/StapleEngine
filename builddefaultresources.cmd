@echo off

call Tools\bin\Baker -i "BuiltinResources" -o "DefaultResources/Windows" -platform Windows -r d3d11 -r d3d12 -r opengl -r spirv
call Tools\bin\Baker -i "BuiltinResources" -o "DefaultResources/Linux" -platform Linux -r opengl -r spirv
call Tools\bin\Baker -i "BuiltinResources" -o "DefaultResources/MacOSX" -platform MacOSX -r metal
call Tools\bin\Baker -i "BuiltinResources" -o "DefaultResources/Android" -platform Android -r opengles -r spirv
call Tools\bin\Baker -i "BuiltinResources" -o "DefaultResources/iOS" -platform iOS -r metal

call Tools\bin\Packer -p -r -i "DefaultResources/Windows" -o DefaultResources/DefaultResources-Windows.pak
call Tools\bin\Packer -p -r -i "DefaultResources/Linux" -o DefaultResources/DefaultResources-Linux.pak
call Tools\bin\Packer -p -r -i "DefaultResources/MacOSX" -o DefaultResources/DefaultResources-MacOSX.pak
call Tools\bin\Packer -p -r -i "DefaultResources/Android" -o DefaultResources/DefaultResources-Android.pak
call Tools\bin\Packer -p -r -i "DefaultResources/iOS" -o DefaultResources/DefaultResources-iOS.pak

pause