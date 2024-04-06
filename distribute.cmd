@echo off

if exist dist\ rmdir /S /Q dist

mkdir dist\StapleEngine

robocopy DefaultResources dist\StapleEngine\DefaultResources /E /NFL /NDL /NJH /NJS /NP /NS /NC
robocopy Staging dist\StapleEngine\Editor /E /NFL /NDL /NJH /NJS /NP /NS /NC
robocopy Tools\bin dist\StapleEngine\Tools\bin /E /NFL /NDL /NJH /NJS /NP /NS /NC
robocopy Tools\ShaderIncludes dist\StapleEngine\Tools\ShaderIncludes /E /NFL /NDL /NJH /NJS /NP /NS /NC
robocopy Engine\EditorApp\bin\Release\net8.0\runtimes dist\StapleEngine\Editor\runtimes /E /NFL /NDL /NJH /NJS /NP /NS /NC
robocopy "Staging\Player Backends\Windows\Redist\Release" dist\StapleEngine\Editor /E /NFL /NDL /NJH /NJS /NP /NS /NC

rmdir /S /Q dist\StapleEngine\DefaultResources\Android
rmdir /S /Q dist\StapleEngine\DefaultResources\iOS
rmdir /S /Q dist\StapleEngine\DefaultResources\Linux
rmdir /S /Q dist\StapleEngine\DefaultResources\MacOSX
rmdir /S /Q dist\StapleEngine\DefaultResources\Windows

if ErrorLevel 8 exit /B 1

exit /B 0
