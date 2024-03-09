@echo off

rmdir /S /Q dist

mkdir dist\StapleEngine

robocopy DefaultResources dist\StapleEngine\DefaultResources /E
robocopy Staging dist\StapleEngine\Editor /E
robocopy Tools\bin dist\StapleEngine\Tools\bin /E
robocopy Tools\ShaderIncludes dist\StapleEngine\Tools\ShaderIncludes /E

rmdir /S /Q dist\StapleEngine\DefaultResources\Android
rmdir /S /Q dist\StapleEngine\DefaultResources\iOS
rmdir /S /Q dist\StapleEngine\DefaultResources\Linux
rmdir /S /Q dist\StapleEngine\DefaultResources\MacOSX
rmdir /S /Q dist\StapleEngine\DefaultResources\Windows
