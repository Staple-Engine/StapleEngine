@echo off

dotnet publish build\vs2022\Dependencies_Dotnet.sln -c Debug -o build\vs2022\bin\Debug\net7.0
dotnet publish build\vs2022\Dependencies_Dotnet.sln -c Release -o build\vs2022\bin\Release\net7.0

pause