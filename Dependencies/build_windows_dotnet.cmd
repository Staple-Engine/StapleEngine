@echo off

dotnet publish build\vs2022\Dependencies_Dotnet.sln -c Debug -o build\vs2022\bin\Debug\net8.0
dotnet publish build\vs2022\Dependencies_Dotnet.sln -c Release -o build\vs2022\bin\Release\net8.0

pause