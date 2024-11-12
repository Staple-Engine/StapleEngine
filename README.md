![Github top languages](https://img.shields.io/github/languages/top/littlecodingfox/stapleengine)
[![GitHub version](https://img.shields.io/github/v/release/littlecodingfox/stapleengine?include_prereleases&style=flat-square)](https://github.com/littlecodingfox/stapleengine/releases) 
[![GitHub license](https://img.shields.io/github/license/littlecodingfox/stapleengine?style=flat-square)](https://github.com/littlecodingfox/stapleengine/blob/main/LICENSE) 
[![GitHub issues](https://img.shields.io/github/issues/littlecodingfox/stapleengine?style=flat-square)](https://github.com/littlecodingfox/stapleengine/issues) 
[![GitHub stars](https://img.shields.io/github/stars/littlecodingfox/stapleengine?style=flat-square)](https://github.com/littlecodingfox/stapleengine/stargazers) 

.NET Game Engine "stapled" together

Status: Early state, usable for small demos

# Features

* Unity-style Editor app that runs on windows, linux, and mac, that can edit game data and make builds for windows, linux, mac, and android
* Unity-style custom editor scripting and editor window scripting
* C# game scripting
* Custom asset system
* Graphics API using BGFX as the base
* Entities inspired by both modern ECS and Unity
* Input (Keyboard, Mouse, Touch, Gamepad) and Input Actions
* Meshes, including some imported formats and skeletal animation
* Physics (3D: Jolt Physics)
* Audio (MP3, WAV, OGG) through OpenAL
* Baking pipeline (Baker) that processes game assets into engine-ready formats
* Resource Packer that packs multiple files into a single file
* Text Rendering using FreeType supporting optional gradients and outlines
* Module system to enable/disable specific engine features such as physics and audio

# Installation

You can grab binaries from the releases page. You also need the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

# Building

You need [premake](https://premake.github.io/) to generate some project files, as well as the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

## Windows

You need visual studio 2022.

To compile dependencies, open the visual studio dev terminal, go to the `Dependencies` directory, and run `build_windows`.

After that, you will need to compile the engine, so go to `Engine` and run `build_windows.cmd`.

After building the engine, you must build the tools, so go to `Tools` and run `build_windows.cmd`.

After building the tools, go to the main folder of the repo and run `builddefaultresources.cmd` to prepare the default assets.

Finally, go to the `Redist` folder in `Dependencies` and copy the windows DLLs to the `Staging` folder. You can now run `StapleEditorApp`.

## MacOS

You need xcode.

To compile dependencies, go to `Dependencies` and run `build_macos.sh`.

After that, you will need to compile the engine, so go to `Engine` and run `build_linux.sh` and then run `build_backends.sh`.

After that, you will need to compile the tools, so go to `Tools` and run `build_linux.sh`.

After building the tools, go to the main folder of the repo and run `builddefaultresources.sh` to prepare the default assets. Do notice that we can't build windows direct3D shaders in linux, so you'll be limited to OpenGL, Metal, and Vulkan there.

Finally, go to the `Redist` folder in `Dependencies` and copy the linux DLLs to the `Staging` folder. You can now run `StapleEditorApp`.

## Linux

### Required Packages

#### Ubuntu

```bash
sudo apt install premake git build-essential libxi-dev libxinerama-dev libxrandr-dev libxcursor-dev libgl1-mesa-dev libx11-dev libgtk-3-dev cmake clang
```

##### To install .NET

```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --version latest
```

##### Don't forget to add to your shell

```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
```

#### Arch

```bash
sudo pacman -Syu premake git base-devel libxi libxinerama librandr libxcursor libx11 gtk3 cmake clang
```

##### To install .NET

```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --version latest
```

##### Don't forget to add to your shell

```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
```

### Instructions (After required packages)

To compile dependencies, go to `Dependencies` and run `build_linux.sh`.

After that, you will need to compile the engine, so go to `Engine` and run `build_linux.sh` and then run `build_backends.sh`.

After that, you will need to compile the tools, so go to `Tools` and run `build_linux.sh`.

After building the tools, go to the main folder of the repo and run `builddefaultresources.sh` to prepare the default assts. Do notice that we can't build windows direct3D shaders in linux, so you'll be limited to OpenGL, Metal, and Vulkan there.

Finally, go to the `Redist` folder in `Dependencies` and copy the linux DLLs to the `Staging` folder. You can now run `StapleEditorApp`.
