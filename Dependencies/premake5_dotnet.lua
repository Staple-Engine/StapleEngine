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
	dotnetframework "net7.0"
	
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
			"{COPYFILE} %{wks.location}../../ImGui.NET/win-x64/*.dll %{wks.location}/bin/x86_64/Debug",
			"{COPYFILE} %{wks.location}../../ImGui.NET/win-x64/*.dll %{wks.location}/bin/x86_64/Release"
		}
	
	filter "system:linux"
		postbuildcommands {
			"{COPYFILE} %{wks.location}../../ImGui.NET/linux-x64/*.so %{wks.location}/bin/x86_64/$(Configuration)"
		}

	filter "system:macos"
		postbuildcommands {
			"{COPYFILE} %{wks.location}../../ImGui.NET/osx/*.dylib %{wks.location}/bin/x86_64/$(Configuration)"
		}
