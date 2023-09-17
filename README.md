# StapleEngine

.NET Game Engine "stapled" together

Status: Extremely early state, unusable for games at this point

# Features

* [GLFW](https://www.glfw.org/) windowing with windowed, fullscreen, and borderless modes
* Basic renderer using [bgfx](https://github.com/bkaradzic/bgfx)
* Entities inspired by both modern ECS and Unity-style
* Scenes
* Input (Keyboard and Mouse for now)
* Math classes (with some of them based on `System.Numerics`(.NET))
* Materials
* Sprites
* Textures
* Shader format based on BGFX's, in a single file
* Baking pipeline (Baker) that processes game resources to fast usage in engine

# Building

You require [premake 5](https://premake.github.io/) to generate some files, as well as [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0).

For Windows, you also need visual studio 2022.

## Windows

To compile dependencies, go to `Dependencies` and run `premake5 vs2022` as well as `build_windows_dotnet.cmd`, then go to `Dependencies/build/vs2022/` and build the `Dependencies.sln` solution for both debug and release configurations. (Keep in mind you don't need to build `Dependencies_Dotnet.sln` here since you already did by running the cmd file)

After that, you will need to compile the tools, so go to `Tools` and run `build_windows.cmd`.

After building the tools, go to the main folder of the repo and run `builddefaultresources.cmd` to prepare the default assets, and `buildtestresources.cmd` if you'd like to run the test project.

Finally, go to `Engine` and build the `Engine` solution and run the `Player` project in the repo main folder. It should also be usable inside the `Staging` folder, as `Player.exe`.

## Linux

### Required Packages

#### Ubuntu

`sudo apt install git build-essential libxi-dev libxinerama-dev libxrandr-dev libxcursor-dev libgl1-mesa-dev libx11-dev libgtk-3-dev`

### Instructions (After required packages)

To compile dependencies, go to `Dependencies` and run `build_linux.sh`.

After that, you will need to compile the tools, so go to `Tools` and run `build_linux.sh`.

After building the tools, go to the main folder of the repo and run `builddefaultresources.sh` to prepare the default assts, and `buildtestresources.sh` if you'd like to run the test game.

Finally, go to `Engine` and run `build_linux.sh` and go to `Staging` and run `./Player`.
