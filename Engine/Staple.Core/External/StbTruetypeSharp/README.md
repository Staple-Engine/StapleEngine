# StbTrueTypeSharp
[![NuGet](https://img.shields.io/nuget/v/StbTrueTypeSharp.svg)](https://www.nuget.org/packages/StbTrueTypeSharp/) ![Build & Publish](https://github.com/StbSharp/StbTrueTypeSharp/workflows/Build%20&%20Publish/badge.svg) [![Chat](https://img.shields.io/discord/628186029488340992.svg)](https://discord.gg/ZeHxhCY)

C# port of stb_truetype.h

# Adding Reference
There are two ways of referencing StbTrueTypeSharp in the project:
1. Through nuget: `install-package StbTrueTypeSharp`
2. As submodule:
    
    a. `git submodule add https://github.com/StbSharp/StbTrueTypeSharp.git`
    
    b. Now there are two options:
       
      * Add StbTrueTypeSharp/src/StbTrueTypeSharp/StbTrueTypeSharp.csproj to the solution
       
      * Include *.cs from StbTrueTypeSharp/src/StbTrueTypeSharp directly in the project. In this case, it might make sense to add STBSHARP_INTERNAL build compilation symbol to the project, so StbTrueTypeSharp classes would become internal.

# License
Public Domain

# Who uses it?
https://github.com/rds1983/SpriteFontPlus

https://github.com/rds1983/FontStashSharp

## Credits
* [stb](https://github.com/nothings/stb)