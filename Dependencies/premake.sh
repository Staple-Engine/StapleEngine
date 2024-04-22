#!/bin/sh

DIR=$( cd "$( dirname "$0" )" && pwd )

oshost=""
detect_os()
{
  case "$(uname -s)" in
    Darwin)
      oshost=macosx
      ;;
    Linux)
      oshost=linux
      ;;
    CYGWIN*|MINGW32*|MSYS*|MINGW*)
      oshost=windows
      ;;
    *)
      echo "Unsupported platform"
      exit 1
      ;;
  esac

  os=$oshost
}

download_premake()
{
  detect_os

  premake_dir="$DIR/premake"
  premake_filename=premake5
  premake_archive_ext=tar.gz
  if [ $oshost = "windows" ]; then
    premake_filename=$premake_filename.exe
    premake_archive_ext=zip
  fi
  premake_path=$premake_dir/$premake_filename

  if ! [ -f "$premake_path" ]; then
    echo "Downloading and unpacking Premake..."
    premake_version=5.0.0-beta2
    premake_archive=premake-$premake_version-$oshost.$premake_archive_ext
    premake_url=https://github.com/premake/premake-core/releases/download/v$premake_version/$premake_archive
    curl -L -O $premake_url
    mkdir $premake_dir
    if [ $oshost = "windows" ]; then
      unzip $premake_archive $premake_filename -d "$premake_dir"
    else
      tar -xf $premake_archive -C "$premake_dir" ./$premake_filename
    fi
    chmod +x "$premake_path"
    rm $premake_archive
  fi
}

download_premake

case "$(uname -s)" in

   Darwin|Linux)
     "$DIR/premake/premake5" "$@"
     ;;

   CYGWIN*|MINGW32*|MSYS*|MINGW*)
     "$DIR/premake/premake5.exe" "$@"
     ;;

   *)
    echo "Unsupported platform"
    exit 1
     ;;
esac



