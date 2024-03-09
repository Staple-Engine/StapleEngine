@echo off

rem due to a strange bug with dotnet, it won't restore properly the first time.
rem so we restore twice...
dotnet workload restore
dotnet workload restore

dotnet build Engine.sln -c Debug
dotnet build Engine.sln -c Release
