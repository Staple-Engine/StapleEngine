local BUILD_DIR = path.join("build", _ACTION)
if _OPTIONS["cc"] ~= nil then
	BUILD_DIR = BUILD_DIR .. "_" .. _OPTIONS["cc"]
end
local BGFX_DIR = "bgfx"
local BIMG_DIR = "bimg"
local BX_DIR = "bx"
local GLFW_DIR = "glfw"
local GLFWNET_DIR = "glfw-net"

solution "Dependencies_Dotnet"
	location(BUILD_DIR)
	configurations { "Release", "Debug" }
	platforms "x86_64"
	
	filter "configurations:Release"
		defines "NDEBUG"
		optimize "Full"

	filter "configurations:Debug*"
		defines "_DEBUG"
		optimize "Debug"
		symbols "On"

	filter "platforms:x86_64"
		architecture "x86_64"

	filter "system:macosx"
		xcodebuildsettings {
			["MACOSX_DEPLOYMENT_TARGET"] = "10.9",
			["ALWAYS_SEARCH_USER_PATHS"] = "YES", -- This is the minimum version of macos we'll be able to run on
		};

project "glfwnet"
	kind "SharedLib"
	language "C#"

	links {
		"System",
		"System.Core",
		"System.Drawing"
	}

	files {
		"glfw-net/GLFW.NET/**.cs"
	}
	
	postbuildcommands {
		"{COPYFILE} %{wks.location}../../ImGui.NET/*.dll %{wks.location}/bin/x86_64/%{cfg.buildcfg}",
		"{COPYFILE} %{wks.location}../../Serilog/*.dll %{wks.location}/bin/x86_64/%{cfg.buildcfg}"
	}
	
	filter "system:windows"
		postbuildcommands {
			"{COPYFILE} %{wks.location}../../ImGui.NET/win-x64/native/*.dll %{wks.location}/bin/x86_64/%{cfg.buildcfg}"
		}
	
	filter "system:linux"
		postbuildcommands {
			"{COPYFILE} %{wks.location}../../ImGui.NET/linux-x64/native/*.so %{wks.location}/bin/x86_64/%{cfg.buildcfg}"
		}

	filter "system:macos"
		postbuildcommands {
			"{COPYFILE} %{wks.location}../../ImGui.NET/osx-universal/native/*.dylib %{wks.location}/bin/x86_64/%{cfg.buildcfg}"
		}

project "MessagePack"
	kind "SharedLib"
	language "C#"
	clr "Unsafe"

	links {
		"System",
		"System.Numerics",
		"System.Runtime.Serialization",
		"MessagePack/Plugins/System.Buffers.dll",
		"MessagePack/Plugins/System.Memory.dll",
		"MessagePack/Plugins/System.Runtime.CompilerServices.Unsafe.dll",
		"MessagePack/Plugins/System.Threading.Tasks.Extensions.dll",
	}

	files {
		"MessagePack/**.cs"
	}

project "EnTTSharp"
	kind "SharedLib"
	language "C#"

	links {
		"System",
		"System.Numerics",
		"System.Runtime.Serialization",
		"Serilog/Serilog.dll"
	}

	files {
		"EnTTSHarp/EnTTSharp/**.cs"
	}

project "EnTTSharp.Annotations"
	kind "SharedLib"
	language "C#"

	links {
		"System",
		"System.Numerics",
		"System.Runtime.Serialization",
		"EnTTSharp",
		"Serilog/Serilog.dll"
	}

	files {
		"EnTTSHarp/EnTTSharp.Annotations/**.cs"
	}
