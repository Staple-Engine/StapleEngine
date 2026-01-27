using Staple.Internal;
using System;
using System.IO;

namespace Staple.ProjectManagement;

public class iOSBuildProcessor : IBuildPreprocessor
{
    public BuildProcessorResult OnPreprocessBuild(BuildInfo buildInfo)
    {
        if (buildInfo.platform != AppPlatform.iOS)
        {
            return BuildProcessorResult.Continue;
        }

        var projectDirectory = buildInfo.assemblyProjectPath;
        var projectAppSettings = buildInfo.projectAppSettings;

        var appDelegate = $$"""
using Foundation;
using UIKit;

[Register ("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
	public override UIWindow? Window
    {
		get;
		set;
	}

	public override bool FinishedLaunching (UIApplication application, NSDictionary launchOptions)
	{
		return true;
	}
}
""";

        if (!StorageUtils.WriteFile(Path.Combine(projectDirectory, "AppDelegate.cs"), appDelegate))
        {
            return BuildProcessorResult.Failed;
        }

        var main = $$"""
using UIKit;

UIApplication.Main (args, null, typeof (AppDelegate));
""";

        if (!StorageUtils.WriteFile(Path.Combine(projectDirectory, "Main.cs"), main))
        {
            return BuildProcessorResult.Failed;
        }

        var orientationTypeiPhone = $$"""
        <string>UIInterfaceOrientationPortrait</string>
        <string>UIInterfaceOrientationLandscapeLeft</string>
        <string>UIInterfaceOrientationLandscapeRight</string>
""";

        var orientationTypeiPad = $$"""
        <string>UIInterfaceOrientationPortrait</string>
        <string>UIInterfaceOrientationPortraitUpsideDown</string>
        <string>UIInterfaceOrientationLandscapeLeft</string>
        <string>UIInterfaceOrientationLandscapeRight</string>
""";

        if (!projectAppSettings.portraitOrientation || !projectAppSettings.landscapeOrientation)
        {
            if (projectAppSettings.portraitOrientation)
            {
                orientationTypeiPhone = $$"""
        <string>UIInterfaceOrientationPortrait</string>
""";

                orientationTypeiPad = $$"""
        <string>UIInterfaceOrientationPortrait</string>
        <string>UIInterfaceOrientationPortraitUpsideDown</string>
""";
            }
            else if (projectAppSettings.landscapeOrientation)
            {
                orientationTypeiPhone = $$"""
        <string>UIInterfaceOrientationLandscapeLeft</string>
        <string>UIInterfaceOrientationLandscapeRight</string>
""";

                orientationTypeiPad = $$"""
        <string>UIInterfaceOrientationLandscapeLeft</string>
        <string>UIInterfaceOrientationLandscapeRight</string>
""";
            }
        }

        var infoPlist = $$"""
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDisplayName</key>
    <string>{{projectAppSettings.appName}}</string>
    <key>CFBundleIdentifier</key>
    <string>{{projectAppSettings.appBundleID}}</string>
    <key>CFBundleShortVersionString</key>
    <string>{{projectAppSettings.appDisplayVersion}}</string>
    <key>CFBundleVersion</key>
    <string>{{projectAppSettings.appVersion}}</string>
    <key>LSRequiresIPhoneOS</key>
    <true/>
    <key>UIDeviceFamily</key>
    <array>
        <integer>1</integer>
        <integer>2</integer>
    </array>
    <key>UILaunchStoryboardName</key>
    <string>LaunchScreen</string>
    <key>UIMainStoryboardFile</key>
	<string>MainStoryboard_iPhone</string>
	<key>UIMainStoryboardFile~ipad</key>
	<string>MainStoryboard_iPad</string>
    <key>UIRequiredDeviceCapabilities</key>
    <array>
        <string>armv7</string>
    </array>
    <key>UISupportedInterfaceOrientations</key>
    <array>
        {{orientationTypeiPhone}}
    </array>
    <key>UISupportedInterfaceOrientations~ipad</key>
    <array>
        {{orientationTypeiPad}}
    </array>
    <key>XSAppIconAssets</key>
    <string>Assets.xcassets/AppIcon.appiconset</string>
</dict>
</plist>
""";

        if (!StorageUtils.WriteFile(Path.Combine(projectDirectory, "Info.plist"), infoPlist))
        {
            return BuildProcessorResult.Failed;
        }

        var launchScreen = $$"""
<?xml version="1.0" encoding="UTF-8" standalone="no"?>
<document type="com.apple.InterfaceBuilder3.CocoaTouch.Storyboard.XIB" version="3.0" toolsVersion="13771" targetRuntime="iOS.CocoaTouch" propertyAccessControl="none" useAutolayout="YES" useTraitCollections="YES" colorMatched="YES" initialViewController="222" launchScreen="YES">
    <dependencies>
        <plugIn identifier="com.apple.InterfaceBuilder.IBCocoaTouchPlugin" version="13772"/>
        <capability name="documents saved in the Xcode 8 format" minToolsVersion="8.0"/>
    </dependencies>
    <scenes>
        <scene sceneID="221">
            <objects>
                <viewController id="222" sceneMemberID="viewController">
                    <layoutGuides>
                        <viewControllerLayoutGuide type="top" id="219"/>
                        <viewControllerLayoutGuide type="bottom" id="220"/>
                    </layoutGuides>
                    <view key="view" contentMode="scaleToFill" id="223">
                        <rect key="frame" x="0.0" y="0.0" width="414" height="736"/>
                        <autoresizingMask key="autoresizingMask" widthSizable="YES" heightSizable="YES"/>
                        <color key="backgroundColor" white="1" alpha="1" colorSpace="calibratedWhite"/>
                    </view>
                </viewController>
                <placeholder placeholderIdentifier="IBFirstResponder" id="224" userLabel="First Responder" sceneMemberID="firstResponder"/>
            </objects>
            <point key="canvasLocation" x="-200" y="-388"/>
        </scene>
    </scenes>
</document>
""";

        if (!StorageUtils.WriteFile(Path.Combine(projectDirectory, "LaunchScreen.storyboard"), launchScreen))
        {
            return BuildProcessorResult.Failed;
        }

        var storyboardiPad = $$"""
<?xml version="1.0" encoding="UTF-8" standalone="no"?>
<document type="com.apple.InterfaceBuilder3.CocoaTouch.Storyboard.XIB" version="3.0" toolsVersion="6245" systemVersion="14A343f" targetRuntime="iOS.CocoaTouch" propertyAccessControl="none" useAutolayout="YES" useTraitCollections="YES" initialViewController="BYZ-38-t0r">
    <dependencies>
        <plugIn identifier="com.apple.InterfaceBuilder.IBCocoaTouchPlugin" version="6238"/>
    </dependencies>
    <scenes>
        <!--Staple View Controller-->
        <scene sceneID="tne-QT-ifu">
            <objects>
                <viewController id="BYZ-38-t0r" customClass="StapleViewController" sceneMemberID="viewController">
                    <layoutGuides>
                        <viewControllerLayoutGuide type="top" id="y3c-jy-aDJ"/>
                        <viewControllerLayoutGuide type="bottom" id="wfy-db-euE"/>
                    </layoutGuides>
                    <view key="view" contentMode="scaleToFill" id="8bC-Xf-vdC" customClass="MetalView">
                        <rect key="frame" x="0.0" y="0.0" width="600" height="600"/>
                        <autoresizingMask key="autoresizingMask" widthSizable="YES" heightSizable="YES"/>
                        <color key="backgroundColor" white="1" alpha="1" colorSpace="custom" customColorSpace="calibratedWhite"/>
                    </view>
                </viewController>
                <placeholder placeholderIdentifier="IBFirstResponder" id="dkx-z0-nzr" sceneMemberID="firstResponder"/>
            </objects>
        </scene>
    </scenes>
</document>
""";

        if (!StorageUtils.WriteFile(Path.Combine(projectDirectory, "MainStoryboard_iPad.storyboard"), storyboardiPad))
        {
            return BuildProcessorResult.Failed;
        }

        var storyboardiPhone = $$"""
<?xml version="1.0" encoding="UTF-8" standalone="no"?>
<document type="com.apple.InterfaceBuilder3.CocoaTouch.Storyboard.XIB" version="3.0" toolsVersion="6245" systemVersion="14A343f" targetRuntime="iOS.CocoaTouch" propertyAccessControl="none" useAutolayout="YES" useTraitCollections="YES" initialViewController="vXZ-lx-hvc">
    <dependencies>
        <plugIn identifier="com.apple.InterfaceBuilder.IBCocoaTouchPlugin" version="6238"/>
    </dependencies>
    <scenes>
        <!--Staple View Controller-->
        <scene sceneID="ufC-wZ-h7g">
            <objects>
                <viewController id="vXZ-lx-hvc" customClass="StapleViewController" sceneMemberID="viewController">
                    <layoutGuides>
                        <viewControllerLayoutGuide type="top" id="jyV-Pf-zRb"/>
                        <viewControllerLayoutGuide type="bottom" id="2fi-mo-0CV"/>
                    </layoutGuides>
                    <view key="view" contentMode="scaleToFill" id="kh9-bI-dsS" customClass="MetalView">
                        <rect key="frame" x="0.0" y="0.0" width="600" height="600"/>
                        <autoresizingMask key="autoresizingMask" flexibleMaxX="YES" flexibleMaxY="YES"/>
                        <color key="backgroundColor" white="1" alpha="1" colorSpace="custom" customColorSpace="calibratedWhite"/>
                    </view>
                </viewController>
                <placeholder placeholderIdentifier="IBFirstResponder" id="x5A-6p-PRh" sceneMemberID="firstResponder"/>
            </objects>
        </scene>
    </scenes>
</document>
""";

        if (!StorageUtils.WriteFile(Path.Combine(projectDirectory, "MainStoryboard_iPhone.storyboard"), storyboardiPhone))
        {
            return BuildProcessorResult.Failed;
        }

        return BuildProcessorResult.Success;
    }
}
