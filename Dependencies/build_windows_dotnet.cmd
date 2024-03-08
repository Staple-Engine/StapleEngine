@echo off

dotnet publish build\dotnet\Dependencies_Dotnet.sln -c Debug -o build\dotnet\bin\Debug\net8.0
dotnet publish build\dotnet\Dependencies_Dotnet.sln -c Release -o build\dotnet\bin\Release\net8.0

pause