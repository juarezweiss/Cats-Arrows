using System;
using System.IO;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace CatsAndArrows.UI.Editor
{
    // #67: identical pinned native SDK packaging for validation and production.
    public static class NativeAdMobBuild
    {
        public static void Add(string output, string appId)
        {
            var source = Path.GetFullPath(Path.Combine(Application.dataPath, "../ThirdParty~/GoogleMobileAds"));
            var folder = "Frameworks/PawsAdMob";
            var projectPath = PBXProject.GetPBXProjectPath(output);
            var project = new PBXProject(); project.ReadFromFile(projectPath);
            var framework = project.GetUnityFrameworkTargetGuid();
            foreach (var name in new[] { "unity-plugin-library", "GoogleMobileAds", "UserMessagingPlatform" })
            {
                var relative = folder + "/" + name + ".xcframework";
                CopyDirectory(Path.Combine(source, name + ".xcframework"), Path.Combine(output, relative));
                var guid = project.AddFile(relative, relative, PBXSourceTree.Source);
                project.AddFileToBuild(framework, guid);
            }
            foreach (var name in new[] { "AudioToolbox", "AVFoundation", "CFNetwork", "CoreGraphics", "CoreMedia", "CoreTelephony", "CoreVideo", "JavaScriptCore", "MediaPlayer", "MessageUI", "MobileCoreServices", "Network", "QuartzCore", "Security", "StoreKit", "SystemConfiguration", "AdSupport", "SafariServices", "WebKit" })
                project.AddFrameworkToProject(framework, name + ".framework", false);
            foreach (var library in new[] { "libz.tbd", "libsqlite3.tbd", "libc++.tbd" })
            { var guid = project.AddFile("usr/lib/" + library, "Frameworks/" + library, PBXSourceTree.Sdk); project.AddFileToBuild(framework, guid); }
            project.AddBuildProperty(framework, "OTHER_LDFLAGS", "-ObjC");
            foreach (var name in new[] { "GoogleMobileAds", "UserMessagingPlatform" })
            {
                var relative = folder + "/" + name + "Resources.bundle";
                Directory.CreateDirectory(Path.Combine(output, relative));
                File.Copy(Path.Combine(source, name + ".xcframework/ios-arm64/" + name + ".framework/PrivacyInfo.xcprivacy"), Path.Combine(output, relative, "PrivacyInfo.xcprivacy"), true);
                if (name == "GoogleMobileAds") CopyDirectory(Path.Combine(source, "Licenses"), Path.Combine(output, relative, "Licenses"));
                var info = new PlistDocument(); info.root.SetString("CFBundleIdentifier", "com.google." + name + ".resources");
                info.root.SetString("CFBundlePackageType", "BNDL"); info.root.SetString("CFBundleVersion", "1");
                info.WriteToFile(Path.Combine(output, relative, "Info.plist"));
                project.AddFileToBuild(framework, project.AddFile(relative, relative, PBXSourceTree.Source));
            }
            project.WriteToFile(projectPath);
            var plistPath = Path.Combine(output, "Info.plist"); var plist = new PlistDocument(); plist.ReadFromFile(plistPath);
            plist.root.SetString("GADApplicationIdentifier", appId);
            plist.root.SetString("GADUUnityVersion", Application.unityVersion);
            plist.root.SetBoolean("GADDisableAdInspector", true);
            plist.root.values.Remove("NSUserTrackingUsageDescription");
            var networks = plist.root.CreateArray("SKAdNetworkItems");
            networks.AddDict().SetString("SKAdNetworkIdentifier", "cstr6suwn9.skadnetwork");
            plist.WriteToFile(plistPath);
        }
        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var path in Directory.GetFiles(source)) File.Copy(path, Path.Combine(destination, Path.GetFileName(path)), true);
            foreach (var path in Directory.GetDirectories(source)) CopyDirectory(path, Path.Combine(destination, Path.GetFileName(path)));
        }
    }
}
