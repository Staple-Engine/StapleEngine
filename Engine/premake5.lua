local BUILD_DIR = path.join("build", _ACTION)
local cc = _ACTION

if _OPTIONS["cc"] ~= nil then
	BUILD_DIR = BUILD_DIR .. "_" .. _OPTIONS["cc"]
	cc = _OPTIONS["cc"]
end
local BGFX_DIR = "bgfx"
local BIMG_DIR = "bimg"
local BX_DIR = "bx"
local ENTT_DIR = "entt"
local GLFW_DIR = "glfw"

solution "Engine"
	configurations { "Debug", "Release" }
	platforms { "x64" }

	filter "configurations:Release"
		defines "NDEBUG"
		optimize "Full"
	filter "configurations:Debug*"
		defines "_DEBUG"
		optimize "Debug"
		symbols "On"

	project "Core"
		kind "ConsoleApp"
		language "C#"
		clr "Unsafe"
		
		nuget {
			"glfw-net:3.3.1"
		}
		
		links {
			"System.Drawing",
			"System.Numerics"
		}
		
		targetdir "../bin/Core/%{cfg.buildcfg}"
		objdir "../obj/Core/%{cfg.buildcfg}"
		
		files {
			"Core/**.cs"
		}
		
		prebuildcommands {
			"{COPYFILE} %{wks.location}../Dependencies/build/" .. cc .. "/bin/x86_64/Release/*.dll %{wks.location}%{cfg.targetdir}"
		}
