#!/bin/sh

dotnet workload restore

dotnet build Tools.sln -c Release -o bin
