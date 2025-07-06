@echo off

dotnet workload restore

dotnet build Tools.sln -c Release -o bin/

pause
