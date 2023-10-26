# StapleEngine

.NET Game Engine "stapled" together

Status: Extremely early state, unusable for games at this point

# Features

* GLFW windowing with windowed, fullscreen, and borderless modes
* Basic renderer using bgfx
* Entities inspired by both modern ECS and Unity
* Scenes
* Input (Keyboard and Mouse for now)
* Math classes
* Materials
* Sprites
* Textures
* Shader format based on BGFX's, in a single file
* Baking pipeline (Baker) that processes game resources to fast usage in engine
* Resource Packer that packs multiple files in its own format
* Editor app that can edit game data and make builds for windows, linux, and android
* Custom asset system
* Unity-Style custom editor and editor window support
* Text rendering using FreeType
* Windows, Linux, and Android support
* Physics support (3D: Jolt Physics)
* Sound support through OpenAL

# Building

You need [premake 5](https://premake.github.io/) to generate some files, as well as [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0).

For Windows, you also need visual studio 2022.

## Windows

To compile dependencies, go to `Dependencies` and run `premake5 vs2022` as well as `build_windows_dotnet.cmd`, then go to `Dependencies/build/vs2022/` and build the `Dependencies.sln` solution for both debug and release configurations. (Keep in mind you don't need to build `Dependencies_Dotnet.sln` here since you already did by running the cmd file)

After that, you will need to compile the engine, so go to `Engine` and run `build_windows.cmd`.

After building the engine, you must build the tools, so go to `Tools` and run `build_windows.cmd`.

After building the tools, go to the main folder of the repo and run `builddefaultresources.cmd` to prepare the default assets.

Finally, go to the `Redist` folder in `Dependencies` and copy the windows DLLs to the `Staging` folder. You can now run `StapleEditorApp`.

## Linux

### Required Packages

#### Ubuntu

`sudo apt install git build-essential libxi-dev libxinerama-dev libxrandr-dev libxcursor-dev libgl1-mesa-dev libx11-dev libgtk-3-dev`

### Instructions (After required packages)

To compile dependencies, go to `Dependencies` and run `build_linux.sh`.

After that, you will need to compile the engine, so go to `Engine` and run `build_linux.sh`.

After that, you will need to compile the tools, so go to `Tools` and run `build_linux.sh`.

After building the tools, go to the main folder of the repo and run `builddefaultresources.sh` to prepare the default assts. Do notice that we can't build windows direct3D shaders in linux, so you'll be limited to OpenGL and Vulkan there.

Finally, go to the `Redist` folder in `Dependencies` and copy the linux DLLs to the `Staging` folder. You can now run `StapleEditorApp`.
