@echo off

call Tools\bin\Baker -i "Test Project\Assets" -o "Staging\Data" -r d3d11 -editor

pause