require "cmake"

local BUILD_DIR = path.join("build", "native")
local SUPPORT_DIR = "StapleSupport"

solution "Dependencies"
	location(BUILD_DIR)
	configurations { "Release", "Debug" }
	architecture "x64"
	
	linkoptions { "-Wl,-z,max-page-size=16384" }
	
	filter "configurations:Release"
		defines "NDEBUG"
		optimize "Full"

	filter "configurations:Debug*"
		defines "_DEBUG"
		optimize "Debug"
		symbols "On"

	filter { "system:android or system:macosx" }
		architecture "arm64"

	filter "system:macosx"
		xcodebuildsettings {
			["MACOSX_DEPLOYMENT_TARGET"] = "13.0",
			["ALWAYS_SEARCH_USER_PATHS"] = "YES", -- This is the minimum version of macos we'll be able to run on
		};

project "StapleSupport"
	kind "SharedLib"
	language "C++"
	
	includedirs {
		"freetype/include"
	}
	
	libdirs { "build/native/freetype/Release" }
	
	links { "freetype", "log" }

	files {

		path.join(SUPPORT_DIR, "*.c");
		path.join(SUPPORT_DIR, "*.cpp");
		path.join(SUPPORT_DIR, "*.h");
		path.join(SUPPORT_DIR, "*.hpp");
	}
