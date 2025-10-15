local BUILD_DIR = path.join("build", "ios")
local SUPPORT_DIR = "StapleSupport"
local TOOLING_SUPPORT_DIR = "StapleToolingSupport"
local UFBX_DIR = "ufbx"

solution "Dependencies"
	location(BUILD_DIR)
	configurations { "Release", "Debug" }
	
	filter "configurations:Release"
		defines "NDEBUG"
		optimize "Full"

	filter "configurations:Debug*"
		defines "_DEBUG"
		optimize "Debug"
		symbols "On"

	filter "system:macosx"
		platforms "arm64"
		architecture "arm64"
		
		xcodebuildsettings {
			["MACOSX_DEPLOYMENT_TARGET"] = "10.13",
			["ALWAYS_SEARCH_USER_PATHS"] = "YES", -- This is the minimum version of macos we'll be able to run on
		};

	filter "system:ios"
		platforms "arm64"
		architecture "arm64"
		
		xcodebuildsettings {
			["ALWAYS_SEARCH_USER_PATHS"] = "YES",
			["CODE_SIGN_IDENTITY"] = "",
			["CODE_SIGNING_ALLOWED"] = "NO",
		};

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
