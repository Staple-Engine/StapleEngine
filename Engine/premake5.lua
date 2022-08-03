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
			"System.Memory",
            "System.Numerics",
			"System.Core",
			"../Dependencies/build/" .. cc .. "/bin/x86_64/Release/MessagePack.dll"
        }
		
		targetdir "../bin/Core/%{cfg.buildcfg}"
		objdir "../obj/Core/%{cfg.buildcfg}"
		
		files {
			"Core/**.cs"
		}

        filter "system:windows"
    		prebuildcommands {
			    "{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/%{cfg.buildcfg}/*.dll %{wks.location}%{cfg.targetdir}",
    		}

        filter "system:linux"
    		prebuildcommands {
			    "{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/%{cfg.buildcfg}/*.so %{cfg.targetdir}",
			    "{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/%{cfg.buildcfg}/*.dll %{cfg.targetdir}"
    		}

		filter "system:macos"
    		prebuildcommands {
			    "{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/%{cfg.buildcfg}/*.so %{cfg.targetdir}",
			    "{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/%{cfg.buildcfg}/*.dll %{cfg.targetdir}"
    		}
