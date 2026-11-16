#!/bin/sh

STAPLE_JDK_CHECK_PASSED=""

check_jvm() {
	if [ -d "$1" ] && [ -f "$1/bin/javac" ] && ("$1/bin/javac" --version 2>&1 &> /dev/null); then
		STAPLE_JDK_CHECK_PASSED="yes"
		export JAVA_HOME="$1"
	fi
}

if [ -z "$JAVA_HOME" ]; then
	check_jvm "$JAVA_HOME"
fi

for candidate in /usr/lib/jvm/*; do
	if [[ "$STAPLE_JDK_CHECK_PASSED" == "yes" ]]; then
		break
	fi
	check_jvm "$candidate"
done

if [ -z "$JAVA_HOME" ]; then
	echo "Missing Java JDK. Please ensure you have a compatible JDK installed!"
	exit 1
fi

if [ -z "$ANDROID_HOME" ]; then
	echo "Missing Android SDK. Please ensure you have a compatible Android SDK installed!"
	exit 1
fi

dotnet workload restore

dotnet build Engine.sln -c Debug
dotnet build Engine.sln -c Release

dotnet publish Staple.Editor.App/Staple.Editor.App.csproj -r linux-x64 -c Release --self-contained

../Dependencies/build/dotnet/bin/Release/net11.0/CrossCopy "Staple.Editor.App/bin/Release/net11.0/linux-x64/Staple.Editor.App*" "../Staging"
../Dependencies/build/dotnet/bin/Release/net11.0/CrossCopy "Staple.Editor.App/bin/Release/net11.0/linux-x64/*.dll" "../Staging"
../Dependencies/build/dotnet/bin/Release/net11.0/CrossCopy "Staple.Editor.App/bin/Release/net11.0/linux-x64/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net11.0/CrossCopy "../Dependencies/build/native/bin/Release/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net11.0/CrossCopy "../Staging/PlayerBackends/Linux/Redist/Release/*.[DLL]" "../Staging"
../Dependencies/build/dotnet/bin/Release/net11.0/CrossCopy "../Staging/Packages/com.staple.joltphysics/Plugins/Linux/libjoltc.so" "../Staging"
../Dependencies/build/dotnet/bin/Release/net11.0/CrossCopy "../Staging/Packages/com.staple.openal/Plugins/Linux/libopenal.so" "../Staging"
