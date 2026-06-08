# StbRectPackSharp
[![NuGet](https://img.shields.io/nuget/v/StbRectPackSharp.svg)](https://www.nuget.org/packages/StbRectPackSharp/) 
![Build & Publish](https://github.com/StbSharp/StbRectPackSharp/workflows/Build%20&%20Publish/badge.svg)
[![Chat](https://img.shields.io/discord/628186029488340992.svg)](https://discord.gg/ZeHxhCY)

StbRectPackSharp is C# port of the [stb_rect_pack.h](https://github.com/nothings/stb/blob/master/stb_rect_pack.h), which is C library to pack rectangles on atlas.

# Adding Reference
There are two ways of referencing StbRectPackSharp in the project:
1. Through nuget: https://www.nuget.org/packages/StbRectPackSharp/
2. As submodule:
    
    a. `git submodule add https://github.com/rds1983/StbRectPackSharp.git`
    
    b. Now there are two options:
       
      * Add src/StbRectPackSharp.csproj to the solution
       
      * Include *.cs from folder "src" directly in the project. In this case, it might make sense to add STBSHARP_INTERNAL build compilation symbol to the project, so StbRectPackSharp classes would become internal.
     
# Usage
StbRectPackSharp exposes API similar to [stb_rect_pack.h](https://github.com/nothings/stb/blob/master/stb_rect_pack.h). 

Also it has utility class Packer.

Sample usage code
```c# 
    // Create packer with size 256x256
    Packer packer = new Packer(256, 256);
    
    // Pack a few rectangles
    // data1, data2, data3 are arbitrary user objects that will be stored within instances of PackRectangle class
    // PackRect returns either object of PackerRectangle class(packing was succesful) or null(no more place)
    packer.PackRect(10, 10, data1);
    packer.PackRect(15, 10, data2);
    packer.PackRect(2, 2, data3);
    
    // Enumerate packed rectangles
    foreach(PackerRectangle packRect in packer.PackRectangles)
    {
      // ...
    }
```

If there's no more space to fit the new rectangle, then PackRect method will return null. 

It could be addressed by creating newer and bigger Packer.

I.e.
```c#
    PackerRectangle pr = packer.PackRect(800, 600, data4);
    
    // If pr is null, it means there's no place for the new rect
    // Double the size of the packer until the new rectangle will fit
    while(pr == null)
    {
      Packer newPacker = new Packer(packer.Width * 2, packer.Height * 2);
      
      // Place existing rectangles
      foreach(PackerRectangle existingRect in packer.PackRectangles)
      {
        newPacker.PackRect(existingRect.Width, existingRect.Height, existingRect.Data);
      }
      
      // Now dispose old packer and assign new one
      packer.Dispose();
      packer = newPacker;
      
      // Try to fit the rectangle again
      pr = packer.PackRect(800, 600, data4);
    }
```

See also sample [VisualizePacking](https://github.com/StbSharp/StbRectPackSharp/tree/master/tests/StbRectPackSharp.VisualizePacking) that uses above code.

# License
Public Domain

# Credits
https://github.com/nothings/stb
