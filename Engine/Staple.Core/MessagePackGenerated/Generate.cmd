@echo off

call dotnet tool restore

call dotnet tool run mpc -i ".." -c USE_MESSAGEPACK -o MessagePackGenerated.cs

pause