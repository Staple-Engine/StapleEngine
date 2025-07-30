#!/bin/sh

dotnet tool restore

dotnet tool run mpc -i ".." -c USE_MESSAGEPACK -o MessagePackGenerated.cs
