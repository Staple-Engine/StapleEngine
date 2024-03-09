@echo off
dotnet workload restore
dotnet build Engine.sln -c Release -o ../Staging
