#!/bin/sh

APPS_DIR=$HOME/.local/share/applications

mkdir -p $APPS_DIR

DESKTOP_FILE=$APPS_DIR/StapleEditor.desktop

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
