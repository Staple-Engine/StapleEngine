local BUILD_DIR = path.join("build", _ACTION)
local cc = _ACTION

if _OPTIONS["cc"] ~= nil then
	BUILD_DIR = BUILD_DIR .. "_" .. _OPTIONS["cc"]
	cc = _OPTIONS["cc"]
end

solution "Tools"
	configurations { "Debug", "Release" }
	platforms { "x64" }

	filter "configurations:Release"
		defines "NDEBUG"
		optimize "Full"
	filter "configurations:Debug*"
		defines "_DEBUG"
		optimize "Debug"
		symbols "On"

	project "Baker"
		kind "ConsoleApp"
		language "C#"
		
		targetdir "../bin/Baker/%{cfg.buildcfg}"
		objdir "../obj/Baker/%{cfg.buildcfg}"
		
		files {
			"Baker/**.cs"
		}
		
		links {
			"System"
		}
		
		postbuildcommands {
			"{MKDIR} %{wks.location}/bin",
			"{COPYFILE} %{wks.location}../bin/Baker/%{cfg.buildcfg}/*.exe %{wks.location}/bin/"
		}
