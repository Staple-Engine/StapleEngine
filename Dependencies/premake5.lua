local BUILD_DIR = path.join("build", "native")
local SUPPORT_DIR = "StapleSupport"
local TOOLING_SUPPORT_DIR = "StapleToolingSupport"
local UFBX_DIR = "ufbx"

solution "Dependencies"
	location(BUILD_DIR)
	configurations { "Release", "Debug" }
	architecture "x64"
	
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
		
	filter "action:vs*"
		buildoptions { "/Zc:preprocessor" }

project "StapleSupport"
	kind "SharedLib"
	language "C++"
	
	includedirs {
		"freetype/include",
	}
	
	libdirs { "build/native/freetype/Release/Release" }
	
	links { "freetype" }

	files {

		path.join(SUPPORT_DIR, "*.c");
		path.join(SUPPORT_DIR, "*.cpp");
		path.join(SUPPORT_DIR, "*.h");
		path.join(SUPPORT_DIR, "*.hpp");
	}

	filter "system:macosx"
		files { path.join(SUPPORT_DIR, "*.m") }

		links { "QuartzCore.framework" }

project "StapleToolingSupport"
	kind "SharedLib"
	language "C"
	
	includedirs {
		SUPPORT_DIR,
		"ufbx",
	}
	
	defines { "UFBX_REAL_IS_FLOAT" }

	files {

		path.join(SUPPORT_DIR, "*.h");
		path.join(SUPPORT_DIR, "*.hpp");
		path.join(TOOLING_SUPPORT_DIR, "**.c");
		path.join(TOOLING_SUPPORT_DIR, "**.cpp");
		path.join(TOOLING_SUPPORT_DIR, "**.h");
		path.join(TOOLING_SUPPORT_DIR, "**.hpp");
		path.join(UFBX_DIR, "*.h");
		path.join(UFBX_DIR, "*.c");
	}

	filter "system:macosx"
		files { path.join(SUPPORT_DIR, "*.m") }

		links { "QuartzCore.framework" }
