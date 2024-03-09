@echo off

if exist dist\ rmdir /S /Q dist

echo %ERRORLEVEL%

mkdir dist\StapleEngine

echo %ERRORLEVEL%

echo copy DefaultResources
robocopy DefaultResources dist\StapleEngine\DefaultResources /E /NFL /NDL /NJH /NJS /NP /NS /NC

echo %ERRORLEVEL%

echo copy Editor
robocopy Staging dist\StapleEngine\Editor /E /NFL /NDL /NJH /NJS /NP /NS /NC

echo %ERRORLEVEL%

echo copy Tools
robocopy Tools\bin dist\StapleEngine\Tools\bin /E /NFL /NDL /NJH /NJS /NP /NS /NC

echo %ERRORLEVEL%

echo copy Shader Includes
robocopy Tools\ShaderIncludes dist\StapleEngine\Tools\ShaderIncludes /E /NFL /NDL /NJH /NJS /NP /NS /NC

echo %ERRORLEVEL%

echo delete android compiled files
rmdir /S /Q dist\StapleEngine\DefaultResources\Android

echo %ERRORLEVEL%

echo delete iOS compiled files
rmdir /S /Q dist\StapleEngine\DefaultResources\iOS

echo %ERRORLEVEL%

echo delete linux compiled files
rmdir /S /Q dist\StapleEngine\DefaultResources\Linux

echo %ERRORLEVEL%

echo delete macOSX compiled files
rmdir /S /Q dist\StapleEngine\DefaultResources\MacOSX

echo %ERRORLEVEL%

echo delete windows compiled files
rmdir /S /Q dist\StapleEngine\DefaultResources\Windows

echo %ERRORLEVEL%
