local BUILD_DIR = path.join("build", "dotnet")
local BGFX_DIR = "bgfx"
local BIMG_DIR = "bimg"
local BX_DIR = "bx"

solution "Dependencies_Dotnet"
	location(BUILD_DIR)
	configurations { "Release", "Debug" }
	dotnetframework "net9.0"
	
	filter "configurations:Release"
		defines "NDEBUG"
		optimize "Full"

	filter "configurations:Debug*"
		defines "_DEBUG"
		optimize "Debug"
		symbols "On"

	filter "system:macosx"
		xcodebuildsettings {
			["MACOSX_DEPLOYMENT_TARGET"] = "10.9",
			["ALWAYS_SEARCH_USER_PATHS"] = "YES", -- This is the minimum version of macos we'll be able to run on
		};

project "CrossCopy"
	kind "ConsoleApp"
	language "C#"
	
	files {
		"CrossCopy/*.cs"
	}

project "SDL2-CS"
	kind "SharedLib"
	language "C#"
	clr "Unsafe"
	
	files {
		"SDL2-CS/**.cs"
	}

project "NAudio"
	kind "SharedLib"
	language "C#"
	clr "Unsafe"
	
	files {
		"NAudio/**.cs"
	}

project "NVorbis"
	kind "SharedLib"
	language "C#"
	clr "Unsafe"
	
	files {
		"NVorbis/**.cs"
	}

project "NfdSharp"
	kind "SharedLib"
	language "C#"
	
	files {
		"NfdSharp/**.cs"
	}

project "OggVorbisEncoder"
	kind "SharedLib"
	language "C#"
	clr "Unsafe"

	files {
		".NET-Ogg-Vorbis-Encoder/**.cs"
	}
