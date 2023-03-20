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

	filter "configurations:Release"
		defines "NDEBUG"
		optimize "Full"
	filter "configurations:Debug*"
		defines "_DEBUG"
		optimize "Debug"
		symbols "On"

	project "StapleCore"
		kind "SharedLib"
		language "C#"
		clr "Unsafe"
		dotnetframework "net7.0"
		
		targetdir "../bin/StapleCore/%{cfg.buildcfg}"
		objdir "../obj/StapleCore/%{cfg.buildcfg}"

		libdirs {
			"../Dependencies/build/" .. cc .. "/bin/Release/net7.0/"
		}

        links {
			"../Dependencies/build/" .. cc .. "/bin/Release/net7.0/glfwnet.dll",
			"../Dependencies/build/" .. cc .. "/bin/Release/net7.0/MessagePack.dll",
			"../Dependencies/JsonNet/Newtonsoft.Json.dll"
        }
		
		files {
			"Core/**.cs"
		}

	project "Player"
		kind "ConsoleApp"
		language "C#"
		clr "Unsafe"
		dotnetframework "net7.0"
		
		targetdir "../bin/Player/%{cfg.buildcfg}"
		objdir "../obj/Player/%{cfg.buildcfg}"
		
		links {
			"StapleCore"
		}
		
		files {
			"Player/**.cs"
		}
		
		postbuildcommands {
			"{MKDIR} %{wks.location}../Staging/Data",
			"{COPYFILE} %{wks.location}%{cfg.targetdir}/net7.0/*.exe %{wks.location}../Staging",
			"{COPYFILE} %{wks.location}%{cfg.targetdir}/../../StapleCore/Release/net7.0/StapleCore.dll %{wks.location}../Staging",
		}

        filter "system:windows"
    		postbuildcommands {
			    "{COPYFILE} %{wks.location}../Dependencies/build/" .. cc .. "/bin/Release/net7.0/*.dll %{wks.location}../Staging",
    		}

        filter "system:linux"
    		postbuildcommands {
				-- Linux is messy to build .NET projects with premake makefiles so we need to copy from two places
			    "{COPYFILE} %{wks.location}../Dependencies/build/gmake/bin/x86_64/Release/*.so %{wks.location}../Staging",
			    "{COPYFILE} %{wks.location}../Dependencies/build/vs2022/bin/Release/net7.0/*.dll %{wks.location}../Staging"
    		}

		filter "system:macos"
    		postbuildcommands {
			    "{COPYFILE} %{wks.location}../Dependencies/build/" .. cc .. "/bin/x86_64/Release/*.so %{wks.location}../Staging",
			    "{COPYFILE} %{wks.location}../Dependencies/build/" .. cc .. "/bin/Release/net7.0/*.dll %{wks.location}../Staging"
    		}

	project "StapleEditor"
		kind "SharedLib"
		language "C#"
		clr "Unsafe"
		dotnetframework "net7.0"
		
		targetdir "../bin/StapleEditor/%{cfg.buildcfg}"
		objdir "../obj/StapleEditor/%{cfg.buildcfg}"
		
		links {
			"StapleCore",
			"../Dependencies/JsonNet/Newtonsoft.Json.dll",
			"../Dependencies/build/" .. cc .. "/bin/Release/net7.0/ImGui.NET.dll"
		}
		
		files {
			"Editor/**.cs"
		}
		
		postbuildcommands {
			"{COPYFILE} %{wks.location}%{cfg.targetdir}/net7.0/*.dll %{wks.location}../Staging",
		}

        filter "system:windows"
    		postbuildcommands {
			    "{COPYFILE} %{wks.location}../Dependencies/build/" .. cc .. "/bin/Release/net7.0/*.dll %{wks.location}../Staging",
    		}

        filter "system:linux"
    		postbuildcommands {
				-- Linux is messy to build .NET projects with premake makefiles so we need to copy from two places
			    "{COPYFILE} %{wks.location}../Dependencies/build/gmake/bin/x86_64/Release/*.so %{wks.location}../Staging",
			    "{COPYFILE} %{wks.location}../Dependencies/build/vs2022/bin/Release/net7.0/*.dll %{wks.location}../Staging"
    		}

		filter "system:macos"
    		postbuildcommands {
			    "{COPYFILE} %{wks.location}../Dependencies/build/" .. cc .. "/bin/x86_64/Release/*.so %{wks.location}../Staging",
			    "{COPYFILE} %{wks.location}../Dependencies/build/" .. cc .. "/bin/Release/net7.0/*.dll %{wks.location}../Staging"
    		}

	project "StapleEditorApp"
		kind "ConsoleApp"
		language "C#"
		clr "Unsafe"
		dotnetframework "net7.0"
		
		targetdir "../bin/StapleEditorApp/%{cfg.buildcfg}"
		objdir "../obj/StapleEditorApp/%{cfg.buildcfg}"
		
		links {
			"StapleEditor"
		}
		
		files {
			"EditorApp/**.cs"
		}
		
		postbuildcommands {
			"{MKDIR} %{wks.location}../Staging/Data",
			"{COPYFILE} %{wks.location}%{cfg.targetdir}/net7.0/*.exe %{wks.location}../Staging",
			"{COPYFILE} %{wks.location}%{cfg.targetdir}/../../StapleCore/Release/net7.0/StapleCore.dll %{wks.location}../Staging",
		}

        filter "system:windows"
    		postbuildcommands {
			    "{COPYFILE} %{wks.location}../Dependencies/build/" .. cc .. "/bin/x86_64/Release/*.dll %{wks.location}../Staging",
    		}

        filter "system:linux"
    		postbuildcommands {
				-- Linux is messy to build .NET projects with premake makefiles so we need to copy from two places
			    "{COPYFILE} %{wks.location}../Dependencies/build/gmake/bin/x86_64/Release/*.so %{wks.location}../Staging",
			    "{COPYFILE} %{wks.location}../Dependencies/build/vs2022/bin/Release/net7.0/*.dll %{wks.location}../Staging"
    		}

		filter "system:macos"
    		postbuildcommands {
			    "{COPYFILE} %{wks.location}../Dependencies/build/" .. cc .. "/bin/x86_64/Release/*.so %{wks.location}../Staging",
			    "{COPYFILE} %{wks.location}../Dependencies/build/" .. cc .. "/bin/Release/net7.0/*.dll %{wks.location}../Staging"
    		}
	
	project "TestGame"
		kind "SharedLib"
		language "C#"
		clr "Unsafe"
		dotnetframework "net7.0"
		
		targetdir "../bin/TestGame/%{cfg.buildcfg}"
		objdir "../obj/TestGame/%{cfg.buildcfg}"
		
		links {
			"StapleCore",
		}
		
		files {
			"TestGame/**.cs"
		}

		postbuildcommands {
			"{MKDIR} %{wks.location}../Staging/Data",
			"{COPYFILE} %{wks.location}%{cfg.targetdir}/net7.0/TestGame.dll %{wks.location}../Staging/Data/Game.dll",
		}
