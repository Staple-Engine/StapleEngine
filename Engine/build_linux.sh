#!/bin/sh

dotnet build Engine.sln -c Debug
dotnet build Engine.sln -c Release

dotnet publish EditorApp/StapleEditorApp.csproj -r linux-x64 -c Release --self-contained

../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "EditorApp/bin/Release/net9.0/linux-x64/StapleEditorApp*" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "EditorApp/bin/Release/net9.0/linux-x64/*.dll" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "EditorApp/bin/Release/net9.0/linux-x64/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "../Dependencies/build/native/bin/Release/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "../Staging/PlayerBackends/Linux/Redist/Release/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "../Staging/Packages/com.staple.joltphysics/Plugins/Linux/libjoltc.so" "../Staging"
../Dependencies/build/dotnet/bin/Release/net9.0/CrossCopy "../Staging/Packages/com.staple.openal/Plugins/Linux/libopenal.so" "../Staging"

DESKTOP_FILE=$HOME/.local/share/applications/StapleEditor.desktop

cat >$DESKTOP_FILE <<EOL
[Desktop Entry]
Name=Staple Editor
Comment=Staple Editor App
Exec=$(pwd)/../Staging/StapleEditorApp
Icon=$(pwd)/../icon_gradient.png
Terminal=false
Type=Application
Categories=Development;
EOL

chmod +x $DESKTOP_FILE
