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

		libdirs {
			"../Dependencies/build/" .. cc .. "/bin/x86_64/Release/"
		}

        links {
			"glfwnet",
            "System.Drawing",
            "System.Numerics",
			"System.Core"
        }
		
		targetdir "../bin/Core/%{cfg.buildcfg}"
		objdir "../obj/Core/%{cfg.buildcfg}"
		
		files {
			"Core/**.cs"
		}

        filter "system:windows"
    		postbuildcommands {
			    "{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/Release/*.dll %{cfg.targetdir}"
    		}

        filter "system:linux"
    		postbuildcommands {
			    "{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/Release/*.so %{cfg.targetdir}",
			    "{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/Release/*.dll %{cfg.targetdir}"
    		}

		filter "system:macos"
    		postbuildcommands {
			    "{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/Release/*.so %{cfg.targetdir}",
			    "{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/Release/*.dll %{cfg.targetdir}"
    		}
