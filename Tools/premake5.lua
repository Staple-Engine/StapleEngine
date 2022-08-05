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
			"Baker/**.cs",
			"../Engine/Core/Resources/MaterialMetadata.cs",
			"../Engine/Core/Resources/ShaderMetadata.cs",
			"../Engine/Core/Resources/TextureMetadata.cs",
			"../Engine/Core/Resources/SerializableMaterial.cs",
			"../Engine/Core/Resources/SerializableScene.cs",
			"../Engine/Core/Resources/SerializableShader.cs",
			"../Engine/Core/Resources/SerializableTexture.cs",
			"../Engine/Core/Scene/SceneList.cs",
			"../Engine/Core/Math/Color32.cs",
			"../Engine/Core/Math/Color.cs",
			"../Engine/Core/Math/Math.cs"
		}
		
		links {
			"System",
			"System.Numerics",
			"../Dependencies/JsonNet/Newtonsoft.Json.dll",
			"../Dependencies/build/" .. cc .. "/bin/x86_64/Release/MessagePack.dll"
		}
		
		postbuildcommands {
			"{MKDIR} %{wks.location}/bin",
			"{COPYFILE} %{wks.location}../bin/Baker/%{cfg.buildcfg}/*.exe %{wks.location}/bin/",
			"{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/Release/*.dll %{wks.location}/bin/",
			"{COPYFILE} %{wks.location}../Dependencies/JsonNet/*.dll %{wks.location}/bin/"
		}
