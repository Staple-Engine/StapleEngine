![Github top languages](https://img.shields.io/github/languages/top/staple-engine/stapleengine)
[![GitHub version](https://img.shields.io/github/v/release/staple-engine/stapleengine?include_prereleases&style=flat-square)](https://github.com/staple-engine/stapleengine/releases) 
[![GitHub license](https://img.shields.io/github/license/staple-engine/stapleengine?style=flat-square)](https://github.com/staple-engine/stapleengine/blob/main/LICENSE) 
[![GitHub issues](https://img.shields.io/github/issues/staple-engine/stapleengine?style=flat-square)](https://github.com/staple-engine/stapleengine/issues) 
[![GitHub stars](https://img.shields.io/github/stars/staple-engine/stapleengine?style=flat-square)](https://github.com/staple-engine/stapleengine/stargazers) 

![Screenshot of Staple Editor in use](Screenshots/MainSample.png)

.NET Game Engine "stapled" together

# Status

Early state, usable for small demos

# Features

* Unity-style Editor app that runs on windows, linux, and mac, that can edit game data and make builds for windows, linux, mac, and android
* Unity-style custom editor scripting and editor window scripting
* C# game scripting
* Custom asset system
* Graphics API using BGFX as the base
* Entities inspired by both modern ECS and Unity
* Input (Keyboard, Mouse, Touch, Gamepad) and Input Actions
* Meshes, including some imported formats like FBX, GLTF/GLB, OBJ, skeletal animation/skinning, instancing, and simplification
* Physics (3D: Jolt Physics)
* Audio (MP3, WAV, OGG) through OpenAL
* Baking pipeline (Baker) that processes game assets into engine-ready formats
* Resource Packer that packs multiple files into a single file
* Text Rendering using FreeType supporting optional gradients and outlines
* Package manager to manage builtin features, local and git packages
* Unity-style Assembly Definition system for defining specific projects per parts of the project
* Unity-style Plugin system for defining native dependencies for specific platforms
* Shaders and Compute Shaders with variants and toggleable variants based on set properties
* Culling Volumes for more performant culling of parts of the world or characters

# Installation

You can grab binaries from the releases page. You also need the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0).

Please note that binaries aren't as updated as the main branch, so you'll likely want to build either way.

# Building

You need [premake](https://premake.github.io/) to generate some project files, as well as the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0).

<details>

<summary>Windows</summary>

You need visual studio 2022.

To compile dependencies, open the visual studio dev terminal, go to the `Dependencies` directory, and run `build_windows`.

After that, you will need to compile the engine, so go to `Engine` and run `build_windows.cmd`.

After building the engine, you must build the tools, so go to `Tools` and run `build_windows.cmd`.

After building the tools, go to the main folder of the repo and run `builddefaultresources.cmd` to prepare the default assets.

</details>

<details>

<summary>MacOS</summary>

You need xcode.

To compile dependencies, go to `Dependencies` and run `build_macos.sh`.

After that, you will need to compile the engine, so go to `Engine` and run `build_macos.sh` and then run `build_backends.sh`.

After that, you will need to compile the tools, so go to `Tools` and run `build_linux.sh` (yes, that's the right file).

After building the tools, go to the main folder of the repo and run `builddefaultresources.sh` to prepare the default assets. Do notice that we can't build windows direct3D shaders in macOS, so you'll be limited to OpenGL, Metal, and Vulkan there.

</details>

<details>

<summary>Linux</summary>

### Required Packages

#### Ubuntu

```bash
sudo apt install premake git build-essential libxi-dev libxinerama-dev libxrandr-dev libxcursor-dev libgl1-mesa-dev libx11-dev libgtk-3-dev cmake clang
```

##### To install .NET

```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --version 9.0.301
```

##### Don't forget to add to your shell

```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
```

#### Arch

```bash
sudo pacman -S premake git base-devel libxi libxinerama libxcursor libx11 gtk3 cmake clang
```

##### To install .NET

```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --version 9.0.301
```

##### Don't forget to add to your shell

```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
```

### Instructions (After required packages)

To compile dependencies, go to `Dependencies` and run `build_linux.sh`.

After that, you will need to compile the engine, so go to `Engine` and run `build_linux.sh` and then run `build_backends.sh`.

Optionally, run `make_linux_menu_entry.sh` which should add an entry on your DE's start menu in the Development category

After that, you will need to compile the tools, so go to `Tools` and run `build_linux.sh`.

After building the tools, go to the main folder of the repo and run `builddefaultresources.sh` to prepare the default assts. Do notice that we can't build windows direct3D shaders in linux, so you'll be limited to OpenGL, Metal, and Vulkan there.
</details>
