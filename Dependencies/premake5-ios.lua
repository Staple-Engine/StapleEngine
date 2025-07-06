local BUILD_DIR = path.join("build", "ios")
local BGFX_DIR = "bgfx"
local BIMG_DIR = "bimg"
local BX_DIR = "bx"

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

function setBxCompat()
	filter "action:vs*"
		includedirs { path.join(BX_DIR, "include/compat/msvc") }

	filter { "system:ios" }
		includedirs {
			path.join(BGFX_DIR, "3rdparty/directx-headers/include/wsl"),
			path.join(BGFX_DIR, "3rdparty/directx-headers/include/wsl/stubs"),
			path.join(BX_DIR, "include/compat/ios")
		}
		buildoptions { "-x objective-c++" }

	filter { "system:macosx" }
		includedirs {
			path.join(BGFX_DIR, "3rdparty/directx-headers/include/wsl"),
			path.join(BGFX_DIR, "3rdparty/directx-headers/include/wsl/stubs"),
			path.join(BX_DIR, "include/compat/osx")
		}
		buildoptions { "-x objective-c++" }
	
	filter { "system:linux" }
		includedirs {
			path.join(BGFX_DIR, "3rdparty/directx-headers/include/wsl"),
			path.join(BGFX_DIR, "3rdparty/directx-headers/include/wsl/stubs"),
			path.join(BX_DIR, "include/compat/linux")
		}

	filter { "system:windows", "action:gmake" }
		includedirs { path.join(BX_DIR, "include/compat/mingw") }

    filter "action:gmake"
        buildoptions { "-fPIC" }
end

project "bgfx"
	kind "SharedLib"
	language "C++"
	cppdialect "C++20"
	exceptionhandling "Off"
	rtti "Off"

	defines {
		"__STDC_FORMAT_MACROS",
		"BGFX_SHARED_LIB_BUILD"
	}

	files {
		path.join(BGFX_DIR, "include/bgfx/**.h"),
		path.join(BGFX_DIR, "src/*.cpp"),
		path.join(BGFX_DIR, "src/*.h"),
	}

	excludes {
		path.join(BGFX_DIR, "src/amalgamated.cpp"),
	}

	includedirs {
		path.join(BX_DIR, "include"),
		path.join(BIMG_DIR, "include"),
		path.join(BGFX_DIR, "include"),
		path.join(BGFX_DIR, "3rdparty"),
		path.join(BGFX_DIR, "3rdparty/directx-headers/include"),
		path.join(BGFX_DIR, "3rdparty/directx-headers/include/directx"),
		path.join(BGFX_DIR, "3rdparty/khronos")
	}
	
	links { "bx", "bimg" }
	
	filter "configurations:Debug"
		defines { "BX_CONFIG_DEBUG=1" }

	filter "configurations:Release"
		defines { "BX_CONFIG_DEBUG=0" }

	filter "action:vs*"
		defines "_CRT_SECURE_NO_WARNINGS"
		buildoptions { "/Zc:__cplusplus" }

		excludes {
			path.join(BGFX_DIR, "src/glcontext_glx.cpp"),
			path.join(BGFX_DIR, "src/glcontext_egl.cpp")
		}
		
	filter "system:macosx"
		files {
			path.join(BGFX_DIR, "src/*.mm"),
		}

		excludes {
			path.join(BGFX_DIR, "src/amalgamated.mm"),
		}

		links { "Cocoa.framework", "IOKit.framework", "CoreGraphics.framework", "Metal.framework", "QuartzCore.framework" }
		
	filter "system:ios"
		files {
			path.join(BGFX_DIR, "src/*.mm"),
		}

		excludes {
			path.join(BGFX_DIR, "src/amalgamated.mm"),
		}

		links { "UIKit.framework", "Metal.framework", "MetalKit.framework", "QuartzCore.framework" }
	
	filter "system:linux"
		links {
			"m", "pthread", "X11", "GL"
		}

	setBxCompat()

project "bimg"
	kind "StaticLib"
	language "C++"
	cppdialect "C++20"
	exceptionhandling "Off"
	rtti "Off"

	files {
		path.join(BIMG_DIR, "include/bimg/*.h"),
		path.join(BIMG_DIR, "src/image*.cpp"),
		path.join(BIMG_DIR, "src/*.h"),
		path.join(BIMG_DIR, "3rdparty/**.cpp"),
		path.join(BIMG_DIR, "3rdparty/**.c"),
	}

	includedirs {
		path.join(BX_DIR, "include"),
		path.join(BIMG_DIR, "include"),
		path.join(BIMG_DIR, "3rdparty/"),
		path.join(BIMG_DIR, "3rdparty/astc-encoder/include"),
		path.join(BIMG_DIR, "3rdparty/edtaa3/"),
		path.join(BIMG_DIR, "3rdparty/etc1"),
		path.join(BIMG_DIR, "3rdparty/etc2"),
		path.join(BIMG_DIR, "3rdparty/iqa/include/"),
		path.join(BIMG_DIR, "3rdparty/libsquish/include"),
		path.join(BIMG_DIR, "3rdparty/lodepng"),
		path.join(BIMG_DIR, "3rdparty/nvtt"),
		path.join(BIMG_DIR, "3rdparty/nvtt/**"),
		path.join(BIMG_DIR, "3rdparty/pvrtc"),
		path.join(BIMG_DIR, "3rdparty/stb"),
		path.join(BIMG_DIR, "3rdparty/tinyexr"),
		path.join(BIMG_DIR, "3rdparty/tinyexr/deps/miniz/"),
	}
	
	excludes { path.join(BIMG_DIR, "3rdparty/lodepng/lodepng.cpp") }
	
	filter "configurations:Debug"
		defines { "BX_CONFIG_DEBUG=1" }

	filter "configurations:Release"
		defines { "BX_CONFIG_DEBUG=0" }

	setBxCompat()
	
	filter "action:vs*"
		defines "_CRT_SECURE_NO_WARNINGS"
		buildoptions { "/Zc:__cplusplus" }
		
project "bx"
	kind "StaticLib"
	language "C++"
	cppdialect "C++20"
	exceptionhandling "Off"
	rtti "Off"
	defines "__STDC_FORMAT_MACROS"

	files {
		path.join(BX_DIR, "include/bx/*.h"),
		path.join(BX_DIR, "include/bx/inline/*.inl"),
		path.join(BX_DIR, "src/*.cpp")
	}

	excludes {
		path.join(BX_DIR, "src/amalgamated.cpp"),
		path.join(BX_DIR, "src/crtnone.cpp")
	}

	includedirs {
		path.join(BX_DIR, "3rdparty"),
		path.join(BX_DIR, "include")
	}

	filter "action:vs*"
		defines "_CRT_SECURE_NO_WARNINGS"
		buildoptions { "/Zc:__cplusplus" }

	filter "configurations:Debug"
		defines { "BX_CONFIG_DEBUG=1" }

	filter "configurations:Release"
		defines { "BX_CONFIG_DEBUG=0" }

	setBxCompat()

project "StapleSupport"
	kind "SharedLib"
	language "C++"
	
	includedirs {
		"freetype/include"
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
