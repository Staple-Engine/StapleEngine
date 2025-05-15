# Dependencies

## .NET-Ogg-Vorbis-Encoder

- Upstream: https://github.com/SteveLillis/.NET-Ogg-Vorbis-Encoder/
- Version: 1.2.2 (7d84d7c55bc8ff335cf6fd1e6571555fff47a386, 2023)
- License: MIT

## BGFX

- Upstream: https://github.com/bkaradzic/bgfx
- Version: 1.129 (0e734522cd8fafa29c8035cbde671ecec62668a3, 2025)
- License: BSD-2 Clause
  - Uses the following third party dependencies:
    - cgltf (MIT)
	- Dear ImGui (MIT)
	- Blendish (MIT)
	- fcpp (BSD)
	- glsl-optimizer (MIT)
	- glslang (Apache 2.0)
	- IconFontCppHeaders (MIT)
	- meshoptimizer (MIT)
	- NanoVG (ZLIB)
	- SDF (MIT)
	- spirv-cross (Apache 2.0)
	- spirv-headers (Khronos/MIT)
	- spirv-tools (Apache 2.0)
	- stb (Public Domain)

## Dr. Libs

- Upstream: https://github.com/mackron/dr_libs
- Version: (Various) (0906935b7e35538397ec9f930991706b8edad05f, 2024)
- License: Public Domain

## FreeType

- Upstream: https://github.com/freetype/freetype
- Version: 2.13.3 (42608f77f20749dd6ddc9e0536788eaad70ea4b5, 2024)
- License: FreeType License (FTL)

## Jolt Physics

- Upstream: https://github.com/jrouwe/JoltPhysics
- Version: 5.3.0 (unknown hash, distributed by Jolt Physics Sharp, 2025)
- License: MIT

## Jolt Physics Sharp

- Upstream: https://github.com/amerkoleci/JoltPhysicsSharp
- Version: 2.16.1 (c5ca685a18976793d71452432e7b336832d27eb6, 2025)
- License: MIT

## MessagePack

- Upstream: https://github.com/MessagePack-CSharp/MessagePack-CSharp
- Version: Unknown (to be upgraded eventually and this info will be updated as well)
- License: MIT

## Native File Dialog

- Upstream: https://github.com/mlabbe/nativefiledialog
- Version: 1.1.6 (67345b80ebb429ecc2aeda94c478b3bcc5f7888e, 2019)
- License: Zlib

## NAudio.Core

- Upstream: https://github.com/naudio/NAudio
- Version: 2.2 (94f4ea64765c041d3df2feef774e856aefff5b39, 2023)
- License: MIT

## Newtonsoft Json

- Upstream: https://github.com/JamesNK/Newtonsoft.Json
- Version: 11.0.1-beta2 (unknown hash, 2024)
- License: MIT

## NfdSharp

- Upstream: https://github.com/benklett/nfd-sharp
- Version: 1.0.1 (2529c4164590744c7ea791efe1d0cbec0133bcd3, 2018)
- License: MIT

## NVorbis

- Upstream: https://github.com/NVorbis/NVorbis
- Version: 0.10.5.0 (519d4e2aae7d6a4d5bab552ec5c1e517e9c78855, 2022)
- License: MIT

## OpenAL-Soft

- Upstream: https://github.com/kcat/openal-soft
- Version: Unknown (to be upgraded in the future and this information will be updated)
- License: LGPL

## SDL2

- Upstream: https://github.com/libsdl-org/SDL
- Version: 2 (unknown hash and year)
- License: zlib

## SDL2-CS

- Upstream: https://github.com/flibitijibibo/SDL2-CS
- Version: 2000.1.1 (1ef072adb1653d3e79fa99586ebcb1797a54caca, 2024)
- License: zlib

## SharpGLTF

- Upstream: https://github.com/vpenades/SharpGLTF
- Version: 1.0.4 (5c4bd9643aef7c600304eac85b1325bb249feb70, 2025)
- License: MIT

# Dependencies in Engine folder

## BGFX (CSharp bindings)

- Please check BGFX at the top of this document for license and other information

### Modifications

- Adjusted imports to more modern C#
- Added support for Staple-specific DLL names

## FastNoiseLite

- Upstream: https://github.com/Auburn/FastNoiseLite
- Version: 1.1.1 (722ddecd94b25eb27e852086257a32d388680dbc, 2024)
- License: MIT

### Modifications

- Minor modification to FastNoiseLite.cs to make class internal and in its own namespace

## ImGui

- Upstream: https://github.com/ocornut/imgui
- Version: Provided by Nuget
- License: MIT

## cimgui

- Upstream: https://github.com/cimgui/cimgui
- Version: Provided by Nuget
- License: MIT

## ImGuizmo

- Upstream: https://github.com/CedricGuillemet/ImGuizmo
- Version: Provided by Nuget
- License: MIT

## ImNodes

- Upstream: https://github.com/Nelarius/imnodes
- Version: Provided by Nuget
- License: MIT

## ImPlot

- Upstream: https://github.com/epezent/implot
- Version: Provided by Nuget
- License: MIT

## Hexa.ImGui/ImGuizmo/ImNodes/ImPlot

- Upstream: https://github.com/HexaEngine/Hexa.NET.ImGui
- Version: 1.0.3 (Nuget)
- License: MIT

## OpenAL-CS

- Upstream: https://github.com/flibitijibibo/OpenAL-CS
- Version: 9.0.21022 (28994d73cf1e57c383e21da73824d730bd421a57, 2021)
- License: zlib

### Modifications

- Added support for Staple-specific DLL names

## StbImageResizeSharp

- Upstream: https://github.com/StbSharp/StbImageResizeSharp
- Version: 0.97.1 (4732507e1fd6f96a9945b5210deb21d81dfc4d0e, 2021)
- License: Public Domain

## StbImageSharp

- Upstream: https://github.com/StbSharp/StbImageSharp
- Version: 2.27.13 (9feb07693cde152ebd1cceff7e995db4d7fe6d8d, 2023)
- License: Public Domain

## StbImageWriteSharp

- Upstream: https://github.com/StbSharp/StbImageWriteSharp
- Version: 1.16.7 (51cb5859a7266e7115885c079757dd44ba8c1265, 2022)
- License: Public Domain

## StbRectPackSharp

- Upstream: https://github.com/StbSharp/StbRectPackSharp
- Version: 1.0.4 (aeab866195d872fd311a1bf6d291fc7d2c79f5df, 2021)
- License: Public Domain

## StbTrueTypeSharp

- Upstream: https://github.com/StbSharp/StbTrueTypeSharp
- Version: 1.26.12 (91b2755cb4e531808cd3008be2cdc5658b77b5a8, 2023)
- License: Public Domain

# Dependencies in Tools folder

## Silk.NET.Assimp

- Version: 2.22.0 (Nuget)
