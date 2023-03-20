local BUILD_DIR = path.join("build", _ACTION)
local cc = _ACTION

if _OPTIONS["cc"] ~= nil then
	BUILD_DIR = BUILD_DIR .. "_" .. _OPTIONS["cc"]
	cc = _OPTIONS["cc"]
end

solution "Tools"
	configurations { "Debug", "Release" }

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
		dotnetframework "net7.0"
		
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
			"../Dependencies/JsonNet/Newtonsoft.Json.dll",
			"../Dependencies/build/" .. cc .. "/bin/Release/net7.0/MessagePack.dll"
		}
		
		postbuildcommands {
			"{MKDIR} %{wks.location}/bin",
			"{COPYFILE} %{wks.location}../bin/Baker/%{cfg.buildcfg}/net7.0/*.exe %{wks.location}/bin/",
			"{COPYFILE} %{wks.location}../Dependencies/JsonNet/*.dll %{wks.location}/bin/"
		}

		filter { "system:windows", "system:macos" }
			links { "../Dependencies/build/" .. cc .. "/bin/Release/net7.0/MessagePack.dll" }

			postbuildcommands {
				"{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/Release/*.dll %{wks.location}/bin/"
			}

		filter "system:linux"
			links { "../Dependencies/build/vs2019/bin/Release/net7.0/MessagePack.dll" }

			postbuildcommands {
				"{COPYFILE} %{wks.location}/../Dependencies/build/vs2019/bin/x86_64/Release/*.dll %{wks.location}/bin/"
			}
