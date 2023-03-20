@echo off
call C:\premake\premake5 vs2022
dotnet publish Engine.sln -c Release -o ../Staging
pause