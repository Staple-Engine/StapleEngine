@echo off

dotnet build Tools.sln -c Release -p:Platform="Any CPU" -o bin/

pause
