@echo off

if exist dist\ rmdir /S /Q dist

mkdir dist\StapleEngine

echo copy DefaultResources
robocopy DefaultResources dist\StapleEngine\DefaultResources /E /NFL /NDL /NJH /NJS /NP /NS /NC

echo copy Editor
robocopy Staging dist\StapleEngine\Editor /E /NFL /NDL /NJH /NJS /NP /NS /NC

echo copy Tools
robocopy Tools\bin dist\StapleEngine\Tools\bin /E /NFL /NDL /NJH /NJS /NP /NS /NC

echo copy Shader Includes
robocopy Tools\ShaderIncludes dist\StapleEngine\Tools\ShaderIncludes /E /NFL /NDL /NJH /NJS /NP /NS /NC

echo delete android compiled files
rmdir /S /Q dist\StapleEngine\DefaultResources\Android

echo delete iOS compiled files
rmdir /S /Q dist\StapleEngine\DefaultResources\iOS

echo delete linux compiled files
rmdir /S /Q dist\StapleEngine\DefaultResources\Linux

echo delete macOSX compiled files
rmdir /S /Q dist\StapleEngine\DefaultResources\MacOSX

echo delete windows compiled files
rmdir /S /Q dist\StapleEngine\DefaultResources\Windows
