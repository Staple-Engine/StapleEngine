@echo off

call Tools\bin\Baker -i "Builtin Resources" -o "DefaultResources/Windows" -r d3d11 -r d3d12 -r opengl -r spirv
call Tools\bin\Baker -i "Builtin Resources" -o "DefaultResources/Linux" -r opengl -r spirv
call Tools\bin\Baker -i "Builtin Resources" -o "DefaultResources/MacOSX" -r metal
call Tools\bin\Baker -i "Builtin Resources" -o "DefaultResources/Android" -r opengles -r spirv
call Tools\bin\Baker -i "Builtin Resources" -o "DefaultResources/iOS" -r metal

pause