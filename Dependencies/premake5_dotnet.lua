local BUILD_DIR = path.join("build", _ACTION)
if _OPTIONS["cc"] ~= nil then
	BUILD_DIR = BUILD_DIR .. "_" .. _OPTIONS["cc"]
end
local BGFX_DIR = "bgfx"
local BIMG_DIR = "bimg"
local BX_DIR = "bx"
local GLFW_DIR = "glfw"
local GLFWNET_DIR = "glfw-net"

solution "Dependencies"
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