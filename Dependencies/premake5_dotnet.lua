local BUILD_DIR = path.join("build", "dotnet")
local BGFX_DIR = "bgfx"
local BIMG_DIR = "bimg"
local BX_DIR = "bx"
local GLFW_DIR = "glfw"
local GLFWNET_DIR = "glfw-net"

solution "Dependencies_Dotnet"
	location(BUILD_DIR)
	configurations { "Release", "Debug" }
	dotnetframework "net8.0"
	
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

project "TiledCS"
	kind "SharedLib"
	language "C#"
	clr "Unsafe"
	
	files {
		"TiledCS/**.cs"
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

project "glfwnet"
	kind "SharedLib"
	language "C#"

	files {
		"glfw-net/GLFW.NET/**.cs"
	}

project "MessagePack"
	kind "SharedLib"
	language "C#"
	clr "Unsafe"

	files {
		"MessagePack/**.cs"
	}

project "ImGui.NET"
	kind "SharedLib"
	language "C#"
	clr "Unsafe"

	files {
		"ImGui.NET/src/**.cs"
	}
	
	filter "system:windows"
		postbuildcommands {
			"{COPYFILE} %{wks.location}../../ImGui.NET/win-x64/*.dll %{wks.location}../native/bin/Debug",
			"{COPYFILE} %{wks.location}../../ImGui.NET/win-x64/*.dll %{wks.location}../native/bin/Release"
		}
	
	filter "system:linux"
		postbuildcommands {
			"{COPYFILE} %{wks.location}../../ImGui.NET/linux-x64/*.so %{wks.location}../native/bin/$(Configuration)/"
		}

	filter "system:macos"
		postbuildcommands {
			"{COPYFILE} %{wks.location}../../ImGui.NET/osx/*.dylib %{wks.location}../native/bin/$(Configuration)/"
		}
