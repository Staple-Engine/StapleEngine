@echo off

call Tools\bin\Baker -i "Builtin Resources" -o "Staging\Data" -r d3d11 -r d3d12 -r metal -r opengl -r opengles -r spirv

pause