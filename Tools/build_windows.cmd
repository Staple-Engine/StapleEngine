@echo off
call C:\premake\premake5 vs2022
dotnet publish Tools.sln -c Release -o bin/
pause