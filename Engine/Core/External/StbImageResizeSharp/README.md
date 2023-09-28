# StbImageResizeSharp
[![NuGet](https://img.shields.io/nuget/v/StbImageResizeSharp.svg)](https://www.nuget.org/packages/StbImageResizeSharp/) 
![Build & Publish](https://github.com/StbSharp/StbImageResizeSharp/workflows/Build%20&%20Publish/badge.svg)
[![Chat](https://img.shields.io/discord/628186029488340992.svg)](https://discord.gg/ZeHxhCY)

StbImageResizeSharp is C# port of the [stb_image_resize.h](https://github.com/nothings/stb/blob/master/stb_image_resize.h), which is C library to resize images.

# Adding Reference
There are two ways of referencing StbImageResizeSharp in the project:
1. Through nuget: https://www.nuget.org/packages/StbImageResizeSharp/
2. As submodule:
    
    a. `git submodule add https://github.com/rds1983/StbImageResizeSharp.git`
    
    b. Now there are two options:
       
      * Add src/StbImageResizeSharp.csproj to the solution
       
      * Include *.cs from folder "src" directly in the project. In this case, it might make sense to add STBSHARP_INTERNAL build compilation symbol to the project, so StbImageResizeSharp classes would become internal.
     
# Usage
StbImageResizeSharp exposes API similar to [stb_image_resize.h](https://github.com/nothings/stb/blob/master/stb_image_resize.h). 
Also it adds some wrapper methods.

Sample code to load and resize an image
```c# 
    // Load an image using StbImageSharp(https://github.com/StbSharp/StbImageSharp)
    ImageResult image;
    using(var stream = File.OpenRead("image.png"))
    {
      image = ImageResult.FromStream(stream);
    }
    
    // Retrieve amount of channels in the image(from 1 to 4)
    int channels = (int)image.Comp;

    // Resize to 400x400
    int newWidth = 400;
    int newHeight = 400;
    byte[] newImageData = new byte[newWidth * newHeight * channels];
    StbImageResize.stbir_resize_uint8(imageData, width, height, image.Width * channels,
        newImageData, newWidth, newHeight, newWidth * channels, channels);
```

# License
Public Domain

# Credits
https://github.com/nothings/stb
