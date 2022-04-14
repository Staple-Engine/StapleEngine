local BUILD_DIR = path.join("build", _ACTION)
if _OPTIONS["cc"] ~= nil then
	BUILD_DIR = BUILD_DIR .. "_" .. _OPTIONS["cc"]
end
local BGFX_DIR = "bgfx"
local BIMG_DIR = "bimg"
local BX_DIR = "bx"
local ENTT_DIR = "entt"
local GLFW_DIR = "glfw"

function setBxCompat()
	filter "action:vs*"
		includedirs { path.join("../../Dependencies", BX_DIR, "include/compat/msvc") }
	filter { "system:windows", "action:gmake" }
		includedirs { path.join("../../Dependencies", BX_DIR, "include/compat/mingw") }
	filter { "system:macosx" }
		includedirs { path.join("../../Dependencies", BX_DIR, "include/compat/osx") }
		buildoptions { "-x objective-c++" }
end

solution "Engine"
	configurations { "Debug", "Release" }
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

	project "Core"
		kind "ConsoleApp"
		language "C++"
		cppdialect "C++14"
		exceptionhandling "Off"
		rtti "Off"
		
		targetdir "../../bin/Core/%{cfg.buildcfg}"
		objdir "../../obj/Core/%{cfg.buildcfg}"
		
		files {
			"main.cpp"
		}
		
		defines {
			"__STDC_LIMIT_MACROS",
			"__STDC_FORMAT_MACROS",
			"__STDC_CONSTANT_MACROS",
		}
		
		includedirs
		{
			path.join("../../Dependencies/", ENTT_DIR, "include"),
			path.join("../../Dependencies/", BX_DIR, "include"),
			path.join("../../Dependencies/", BIMG_DIR, "include"),
			path.join("../../Dependencies/", BGFX_DIR, "include"),
			path.join("../../Dependencies/", GLFW_DIR, "include")
		}
		
		libdirs
		{
			path.join("../../Dependencies/", BUILD_DIR, "bin", "%{cfg.platform}", "%{cfg.buildcfg}"),
		}
		
		links { "bgfx", "bimg", "bx", "glfw" }
		
		setBxCompat()
		
		filter "system:windows"
			links { "gdi32", "kernel32", "psapi" }
		
		filter "system:linux"
			links { "dl", "GL", "pthread", "X11" }
			
		filter "system:macosx"
			links { "QuartzCore.framework", "Metal.framework", "Cocoa.framework", "IOKit.framework", "CoreVideo.framework" }
		
		filter "configurations:Debug"

			defines {
				"BX_CONFIG_DEBUG=1"
			}
		
		filter "configurations:Release"

			defines {
				"BX_CONFIG_DEBUG=0"
			}
		
			optimize "Full"