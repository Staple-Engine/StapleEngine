# StapleEngine

Game Engine "stapled" together

Status: Extremely early state, unusable for games at this point

# Building

You require [premake](https://premake.github.io/) to generate project files. It should work out of the box for the main PC OSs, but that's untested at this point.

First, you must build the dependencies. Go to the `Dependencies` folder and run premake there, then go inside the `build/<your premake action>/` to find the project files to build the dependencies.

Afterwards, go to the `Engine/Core` and run premake there, then build the Engine solution. You should have a little demo with some basic functionality working, just showing some debug text for now.

