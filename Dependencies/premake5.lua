local BUILD_DIR = path.join("build", "vs2022")
local BGFX_DIR = "bgfx"
local BIMG_DIR = "bimg"
local BX_DIR = "bx"
local GLFW_DIR = "glfw"
local GLFWNET_DIR = "glfw-net"
local SUPPORT_DIR = "StapleSupport"

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

	filter "platforms:x86_64"
		platforms "x86_64"
		architecture "x86_64"

	filter "system:macosx"
		platforms "arm64"
		architecture "arm64"
		
		xcodebuildsettings {
			["MACOSX_DEPLOYMENT_TARGET"] = "10.13",
			["ALWAYS_SEARCH_USER_PATHS"] = "YES", -- This is the minimum version of macos we'll be able to run on
		};

function setBxCompat()
	filter "action:vs*"
		includedirs { path.join(BX_DIR, "include/compat/msvc") }

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
	language "C"

	files {

		path.join(SUPPORT_DIR, "*.c");
		path.join(SUPPORT_DIR, "*.cpp");
		path.join(SUPPORT_DIR, "*.h");
		path.join(SUPPORT_DIR, "*.hpp");
	}

	filter "system:macosx"
		files { path.join(SUPPORT_DIR, "*.m") }

		links { "QuartzCore.framework" }

project "glfw"
	kind "SharedLib"
	language "C"

	files {
		path.join(GLFW_DIR, "include/GLFW/*.h"),
		path.join(GLFW_DIR, "src/context.c"),
		path.join(GLFW_DIR, "src/egl_context.*"),
		path.join(GLFW_DIR, "src/init.c"),
		path.join(GLFW_DIR, "src/input.c"),
		path.join(GLFW_DIR, "src/internal.h"),
		path.join(GLFW_DIR, "src/monitor.c"),
		path.join(GLFW_DIR, "src/null_init.c"),
		path.join(GLFW_DIR, "src/null_joystick.c"),
		path.join(GLFW_DIR, "src/null_monitor.c"),
		path.join(GLFW_DIR, "src/null_window.c"),
		path.join(GLFW_DIR, "src/osmesa_context.*"),
		path.join(GLFW_DIR, "src/platform.c"),
		path.join(GLFW_DIR, "src/vulkan.c"),
		path.join(GLFW_DIR, "src/window.c"),
	}

	includedirs { path.join(GLFW_DIR, "include") }

	filter "system:windows"
		defines "_GLFW_WIN32"
		defines "_GLFW_BUILD_DLL"

		files {
			path.join(GLFW_DIR, "src/win32_*.*"),
			path.join(GLFW_DIR, "src/wgl_context.*")
		}

	filter "system:linux"
		defines "_GLFW_X11"

		files {
			path.join(GLFW_DIR, "src/glx_context.*"),
			path.join(GLFW_DIR, "src/linux*.*"),
			path.join(GLFW_DIR, "src/posix*.*"),
			path.join(GLFW_DIR, "src/x11*.*"),
			path.join(GLFW_DIR, "src/xkb*.*")
		}

	filter "system:macosx"
		defines "_GLFW_COCOA"

		files {
			path.join(GLFW_DIR, "src/cocoa_*.*"),
			path.join(GLFW_DIR, "src/posix_thread.h"),
			path.join(GLFW_DIR, "src/nsgl_context.h"),
			path.join(GLFW_DIR, "src/egl_context.h"),
			path.join(GLFW_DIR, "src/osmesa_context.h"),

			path.join(GLFW_DIR, "src/posix_thread.c"),
			path.join(GLFW_DIR, "src/posix_module.c"),
			path.join(GLFW_DIR, "src/nsgl_context.m"),
			path.join(GLFW_DIR, "src/egl_context.c"),
			path.join(GLFW_DIR, "src/nsgl_context.m"),
			path.join(GLFW_DIR, "src/osmesa_context.c"),
		}

		links {
			"Cocoa.framework",
			"OpenGL.framework",
			"IOKit.framework"
		}

	filter "action:vs*"
		defines "_CRT_SECURE_NO_WARNINGS"


project "dr_libs"
	kind "SharedLib"
	language "C"
	
	files {
		"dr_libs/*.c"
	}
