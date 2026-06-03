#!/bin/sh

STAPLE_REQUIRED_JDK_VERSION="21"
STAPLE_JDK_CHECK_PASSED=""

check_jvm() {
	if [ -d "$1" ] && [ -f "$1/bin/java" ] && ("$1/bin/java" --version |& grep "build $STAPLE_REQUIRED_JDK_VERSION." &> /dev/null); then
		STAPLE_JDK_CHECK_PASSED="yes"
		export JAVA_HOME="$1"
	fi
}

if [ -n "$JAVA_HOME" ]; then
	check_jvm "$JAVA_HOME"
fi
for candidate in /usr/lib/jvm/*; do
	if [ -n "$STAPLE_JDK_CHECK_PASSED" ]; then
		break
	fi
	check_jvm "$candidate"
done

dotnet build Engine.sln -c Debug
dotnet build Engine.sln -c Release

dotnet publish Staple.Editor.App/Staple.Editor.App.csproj -r linux-x64 -c Release --self-contained

../Dependencies/build/dotnet/bin/Release/net10.0/CrossCopy "Staple.Editor.App/bin/Release/net10.0/linux-x64/Staple.Editor.App*" "../Staging"
../Dependencies/build/dotnet/bin/Release/net10.0/CrossCopy "Staple.Editor.App/bin/Release/net10.0/linux-x64/*.dll" "../Staging"
../Dependencies/build/dotnet/bin/Release/net10.0/CrossCopy "Staple.Editor.App/bin/Release/net10.0/linux-x64/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net10.0/CrossCopy "../Dependencies/build/native/bin/Release/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net10.0/CrossCopy "../Staging/PlayerBackends/Linux/Redist/Release/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net10.0/CrossCopy "../Staging/Packages/com.staple.joltphysics/Plugins/Linux/libjoltc.so" "../Staging"
../Dependencies/build/dotnet/bin/Release/net10.0/CrossCopy "../Staging/Packages/com.staple.openal/Plugins/Linux/libopenal.so" "../Staging"
