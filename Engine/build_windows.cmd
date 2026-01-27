@echo off

rem due to a strange bug with dotnet, it won't restore properly the first time.
rem so we restore twice...
dotnet workload restore
dotnet workload restore

dotnet build Engine.sln -c Debug -p:Platform="Any CPU"
dotnet build Engine.sln -c Release -p:Platform="Any CPU"
