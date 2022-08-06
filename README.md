# StapleEngine

Game Engine "stapled" together

Status: Extremely early state, unusable for games at this point

# Features

* [GLFW](https://www.glfw.org/) windowing with windowed, fullscreen, and borderless modes
* Basic renderer using [bgfx](https://github.com/bkaradzic/bgfx)
* Entities inspired by both modern ECS and Unity-style
* Scenes
* Input (Keyboard and Mouse for now)
* Math classes (with some of them based on `System.Numerics`(.NET)
* Materials
* Sprites
* Textures
* Shader format based on BGFX's, in a single file
* Baking pipeline (Baker) that processes game resources to fast usage in engine

# Building

You require [premake 5](https://premake.github.io/) to generate project files.

## Windows

To compile dependencies, go to `Dependencies` and run `premake5 vs2019` (doesn't need to use vs, but you might want to), then go to `Dependencies/build/<your premake action>/` and build the dependencies for both debug and release configurations.

After that, you will need to compile the tools, so go to `Tools` and do `premake5 vs2019` then build the `Tools` solution.

After building the tools, go to the main folder of the repo and run `builddefaultresources.cmd` to prepare the default assets, and `buildtestresources.cmd` if you'd like to run the test game.

Finally, go to `Engine` and do `premake5 vs2019` then build the `Engine` solution and run the `Player` project in the repo main folder. It should also be usable inside the `Staging` folder, as `Player.exe`.

## Linux

First, you need to have [mono](https://www.mono-project.com/) in your system.

To compile dependencies, go to `Dependencies` and run `build_linux.sh`.

After that, you will need to compile the tools, so go to `Tools` and run `build_linux.sh`.

After building the tools, go to the main folder of the repo and run `builddefaultresources.sh` to prepare the default assts, and `buildtestresources.sh` if you'd like to run the test game.

Finally, go to `Engine` and run `build_linux.sh` and go to `Staging` and run `mono Player.exe`.
