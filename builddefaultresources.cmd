@echo off

call Tools\bin\Baker -i "Builtin Resources" -o "DefaultResources/Windows" -platform Windows -r d3d11 -r d3d12 -r opengl -r spirv
call Tools\bin\Baker -i "Builtin Resources" -o "DefaultResources/Linux" -platform Linux -r opengl -r spirv
call Tools\bin\Baker -i "Builtin Resources" -o "DefaultResources/MacOSX" -platform MacOSX -r metal
call Tools\bin\Baker -i "Builtin Resources" -o "DefaultResources/Android" -platform Android -r opengles -r spirv
call Tools\bin\Baker -i "Builtin Resources" -o "DefaultResources/iOS" -platform iOS -r metal

pause