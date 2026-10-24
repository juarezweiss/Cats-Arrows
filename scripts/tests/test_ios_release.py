"""Issue #63 release evidence checks against synthetic exports, without Unity/Xcode."""

import copy
import importlib.util
import json
import os
import plistlib
import struct
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

SCRIPT = Path(__file__).resolve().parents[1] / "ios-release.py"
SPEC = importlib.util.spec_from_file_location("ios_release", SCRIPT)
ios = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(ios)
SHA = "a" * 40


class IOSReleaseTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="paws release ")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        settings = self.root / "UnityProject/ProjectSettings"
        settings.mkdir(parents=True)
        (settings / "ProjectSettings.asset").write_text("""PlayerSettings:
  targetDevice: 0
  productName: Paws
  bundleVersion: 0.1.0
  applicationIdentifier:
    iPhone: com.jweiss.paws
  buildNumber:
    iPhone: 1
  iOSTargetOSVersionString: 16.3
""")
        (settings / "ProjectVersion.txt").write_text("m_EditorVersion: 6000.3.25f1\n")
        packages = self.root / "UnityProject/Packages"
        packages.mkdir()
        (packages / "packages-lock.json").write_text('{"dependencies":{"com.unity.test-framework":{"version":"1.6.0"}}}')
        self.export = self.root / "export"
        self.export.mkdir()
        self.metadata = {"sourceSha": SHA, "unityVersion": "6000.3.25f1", "bundleIdentifier": ios.BUNDLE_ID,
                         "version": "0.1.0", "buildNumber": "1", "minimumIOS": "16.3", "deviceFamily": [1],
                         "monetizationEnabled": False, "productionDefine": "PAWS_PRODUCTION"}
        self.write_metadata()
        self.info = {"CFBundleIdentifier": "${PRODUCT_BUNDLE_IDENTIFIER}", "CFBundleDisplayName": "Paws",
                     "CFBundleShortVersionString": "0.1.0", "CFBundleVersion": "1",
                     "UISupportedInterfaceOrientations": ["UIInterfaceOrientationPortrait"],
                     "UIRequiredDeviceCapabilities": ["arm64", "metal"]}
        self.write_plist(self.export / "Info.plist", self.info)
        self.project = {"objects": {
            "target": {"isa": "PBXNativeTarget", "name": "Unity-iPhone", "buildConfigurationList": "list"},
            "list": {"buildConfigurations": ["release"]},
            "release": {"isa": "XCBuildConfiguration", "name": "Release", "buildSettings": {
                "PRODUCT_BUNDLE_IDENTIFIER": ios.BUNDLE_ID, "TARGETED_DEVICE_FAMILY": "1",
                "IPHONEOS_DEPLOYMENT_TARGET": "16.3",
                "PROVISIONING_PROFILE_SPECIFIER": "$(PROVISIONING_PROFILE_SPECIFIER_APP)"}},
            "framework": {"isa": "PBXNativeTarget", "name": "UnityFramework", "buildConfigurationList": "framework-list"},
            "framework-list": {"buildConfigurations": ["framework-release"]},
            "framework-release": {"isa": "XCBuildConfiguration", "name": "Release", "buildSettings": {
                "PROVISIONING_PROFILE_SPECIFIER": ""}}}}
        self.write_project()
        self.generated = self.export / "Il2CppOutputProject/Source/il2cppOutput/CatsAndArrows.UI.cpp"
        self.generated.parent.mkdir(parents=True)
        self.generated.write_text("void PawsBootstrap_Start() {}")
        self.archive = self.root / "Paws.xcarchive"
        self.app = self.archive / "Products/Applications/Paws.app"
        self.app.mkdir(parents=True)
        app_info = dict(self.info, CFBundleIdentifier=ios.BUNDLE_ID, UIDeviceFamily=[1], MinimumOSVersion="16.3", CFBundleExecutable="Paws")
        self.write_plist(self.app / "Info.plist", app_info)
        self.write_plist(self.archive / "Info.plist", {"ApplicationProperties": {
            "CFBundleIdentifier": ios.BUNDLE_ID, "CFBundleShortVersionString": "0.1.0", "CFBundleVersion": "1"}})
        (self.app / "Paws").write_bytes(b"fixture binary")
        self.entitlements = {}
        self.commands = []
        self.dirty_status = ""
        sdk = self.root / "UnityProject/ThirdParty~/FixtureSDK.bin"
        sdk.parent.mkdir()
        sdk.write_bytes(b"pinned-sdk")
        sdk_inventory = self.root / "docs/evidence/issue66/sdk-inventory.json"
        sdk_inventory.parent.mkdir(parents=True)
        sdk_inventory.write_text(json.dumps({"files": [{"path": sdk.relative_to(self.root).as_posix(),
                                                      "sha256": ios.file_sha256(sdk)}]}))
        self.mock = patch.object(ios, "command", side_effect=self.run_command)
        self.mock.start()
        self.addCleanup(self.mock.stop)

    @staticmethod
    def write_plist(path, data):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(plistlib.dumps(data))

    def write_metadata(self):
        (self.export / "paws-export.json").write_text(json.dumps(self.metadata))

    def write_project(self):
        path = self.export / "Unity-iPhone.xcodeproj/project.pbxproj"
        path.parent.mkdir(exist_ok=True)
        path.write_text(json.dumps(self.project))

    def run_command(self, args):
        self.commands.append(args)
        if args[0] == "git":
            if "show" in args:
                return (self.root / "docs/evidence/issue66/sdk-inventory.json").read_text()
            return self.dirty_status if "status" in args else SHA
        if args[0] == "xcodebuild":
            return "Xcode 27.0\nBuild version fixture\n"
        if args[0] == "otool":
            return "Paws:\n\t/System/Library/Frameworks/UIKit.framework/UIKit (compatibility version 1.0.0)\n"
        if args[0] == "codesign" and "-d" in args:
            return plistlib.dumps(self.entitlements).decode()
        if args[0] == "codesign":
            return ""
        if args[0] == "lipo":
            return "arm64\n"
        if args[0] == "dwarfdump":
            return "UUID: AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE (arm64) fixture\n"
        if args[0] == "nm":
            return "\n".join(("_PawsIapRead", "_PawsIapVerifyCache", "_PawsIapPresentOfferCodeSheet",
                              "_GADUInitializeWithCallback", "_GADURequestConsentInfoUpdate", "_GADUUMPCanRequestAds"))
        self.fail(f"Unexpected command: {args}")

    def receipt(self, archive=False):
        return ios.build_receipt(self.root, self.export, self.archive if archive else None)

    def write_runtime_asset(self, config, path=None):
        payload = json.dumps(config).encode()
        name = b"PawsMonetization.generated"
        record = struct.pack("<I", len(name)) + name
        record += b"\0" * (-len(record) % 4)
        record += struct.pack("<I", len(payload)) + payload
        target = (path or self.export) / "Data/resources.assets"
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(record)
        self.metadata["runtimeConfigurationSha256"] = ios.hashlib.sha256(payload).hexdigest()
        self.write_metadata()

    def enable(self, profile="production", archive=False):
        ids = dict(ios.TEST_IDS) if profile == "validation" else {
            "adMobAppId": "ca-app-pub-1111111111111111~1111111111",
            "interstitialAdUnitId": "ca-app-pub-1111111111111111/2222222222",
            "rewardedAdUnitId": "ca-app-pub-1111111111111111/3333333333"}
        self.metadata.update(ids, profile=profile, monetizationEnabled=True, appleProductId=ios.PRODUCT_ID,
                             simulatedPurchases=False, testAds=profile == "validation",
                             testDeviceCount=0, consentDebugGeography="Disabled",
                             privacyPolicyUrl="https://example.org/privacy" if profile == "production" else "",
                             supportUrl="https://example.org/support" if profile == "production" else "",
                             audiencePolicy="local-adult-only-v1",
                             **dict.fromkeys(ios.PROVIDERS, True))
        self.write_metadata()
        runtime = dict(ids, profile=profile, consentDebugGeography="Disabled", umpTestDeviceIds=[],
                       privacyPolicyUrl=self.metadata["privacyPolicyUrl"], supportUrl=self.metadata["supportUrl"],
                       audiencePolicy="local-adult-only-v1")
        (self.export / "paws-monetization.json").write_text(json.dumps(runtime))
        self.write_runtime_asset(runtime)
        self.info.update(GADApplicationIdentifier=ids["adMobAppId"], GADDisableAdInspector=True)
        self.write_plist(self.export / "Info.plist", self.info)
        self.generated.write_text("void ApplePurchaseService(); void GoogleAdsBridge(); void AdMobAdService();")
        for name in ("GoogleMobileAds", "UserMessagingPlatform"):
            self.write_plist(self.export / "Frameworks/PawsAdMob" / (name + "Resources.bundle/PrivacyInfo.xcprivacy"),
                             {"NSPrivacyTracking": False})
            if archive:
                self.write_plist(self.app / "Frameworks/UnityFramework.framework" / (name + "Resources.bundle/PrivacyInfo.xcprivacy"),
                                 {"NSPrivacyTracking": False})
        if archive:
            self.write_runtime_asset(runtime, self.app)
            info = ios.read_plist(self.app / "Info.plist")
            info.update(GADApplicationIdentifier=ids["adMobAppId"], GADDisableAdInspector=True)
            self.write_plist(self.app / "Info.plist", info)
            (self.app / "Frameworks/UnityFramework.framework/UnityFramework").write_bytes(b"framework")
            for name in ("Paws.app.dSYM", "UnityFramework.framework.dSYM"):
                (self.archive / "dSYMs" / name).mkdir(parents=True)

    def test_enabled_production_and_validation_are_distinct_with_no_fake_ownership(self):
        for profile in ("production", "validation"):
            with self.subTest(profile=profile):
                self.enable(profile)
                receipt = self.receipt()
                self.assertEqual(receipt["monetization"]["profile"], profile)
                self.assertEqual(receipt["monetization"]["distributionEligible"], profile == "production")
                self.assertEqual(len(receipt["monetization"]["runtimeConfigurationSha256"]), 64)
                self.assertEqual(receipt["physicalIPhoneReview"], "pending-human")

    def test_enabled_configuration_rejects_unsafe_or_unproven_flags(self):
        self.enable()
        for key, value in {"simulatedPurchases": True, "appleProductId": "other.product", "testAds": True,
                           "testDeviceCount": 1, "consentDebugGeography": "EEA",
                           **dict.fromkeys(ios.PROVIDERS, False)}.items():
            with self.subTest(key=key):
                original = copy.deepcopy(self.metadata)
                self.metadata[key] = value
                self.write_metadata()
                with self.assertRaises(ios.InspectionError):
                    self.receipt()
                self.metadata = original
        self.write_metadata()

    def test_runtime_configuration_must_match_receipt_identifiers_and_profile(self):
        self.enable()
        runtime = self.export / "paws-monetization.json"
        original = json.loads(runtime.read_text())
        for key in original:
            with self.subTest(key=key):
                runtime.write_text(json.dumps(dict(original, **{key: "mismatched"})))
                with self.assertRaises(ios.InspectionError):
                    self.receipt()
        runtime.unlink()
        with self.assertRaises(ios.InspectionError):
            self.receipt()

    def test_production_never_accepts_test_publisher_identifiers_or_generated_literals(self):
        self.enable()
        for key, value in ios.TEST_IDS.items():
            with self.subTest(key=key):
                original = dict(self.metadata)
                self.metadata[key] = value
                self.write_metadata()
                with self.assertRaisesRegex(ios.InspectionError, "test identifiers"):
                    self.receipt()
                self.metadata = original
        self.write_metadata()
        self.generated.write_text(self.generated.read_text() + ios.TEST_PUBLISHER)
        with self.assertRaisesRegex(ios.InspectionError, "test identifier.*IL2CPP"):
            self.receipt()

    def test_validation_cannot_be_used_with_live_placement_identifiers(self):
        self.enable()
        self.metadata.update(profile="validation", testAds=True)
        self.write_metadata()
        runtime = self.export / "paws-monetization.json"
        data = json.loads(runtime.read_text())
        runtime.write_text(json.dumps(dict(data, profile="validation")))
        with self.assertRaisesRegex(ios.InspectionError, "official Google validation identifier"):
            self.receipt()

    def test_validation_supports_real_app_for_ump_with_test_placements_and_private_devices(self):
        self.enable("validation")
        config = json.loads((self.export / "paws-monetization.json").read_text())
        config.update(adMobAppId="ca-app-pub-1111111111111111~1111111111", consentDebugGeography="EEA")
        self.metadata.update(adMobAppId=config["adMobAppId"], consentDebugGeography="EEA", testDeviceCount=1)
        self.info["GADApplicationIdentifier"] = config["adMobAppId"]
        self.write_plist(self.export / "Info.plist", self.info)
        (self.export / "paws-monetization.json").write_text(json.dumps(config))
        device_id = "ABCDEF0123456789ABCDEF0123456789"
        self.write_runtime_asset(dict(config, umpTestDeviceIds=[device_id]))
        receipt = self.receipt()
        self.assertEqual(receipt["monetization"]["exportRuntimeResource"]["testDeviceCount"], 1)
        self.assertNotIn(device_id, json.dumps(receipt))
        self.assertFalse(receipt["monetization"]["distributionEligible"])

    def test_32_to_64_character_private_ump_registration_hashes_are_supported(self):
        for length in (32, 48, 64):
            with self.subTest(length=length):
                self.enable("validation")
                self.metadata["testDeviceCount"] = 1
                config = json.loads((self.export / "paws-monetization.json").read_text())
                self.write_runtime_asset(dict(config, umpTestDeviceIds=["A" * length]))
                self.assertEqual(self.receipt()["monetization"]["exportRuntimeResource"]["testDeviceCount"], 1)

    def test_production_requires_owner_https_support_privacy_and_approved_audience_policy(self):
        self.enable()
        for key, value in (("privacyPolicyUrl", ""), ("supportUrl", "http://example.org"),
                           ("supportUrl", "https://user:password@example.org"),
                           ("audiencePolicy", "unapproved")):
            with self.subTest(key=key, value=value):
                original = dict(self.metadata)
                self.metadata[key] = value
                self.write_metadata()
                with self.assertRaises(ios.InspectionError):
                    self.receipt()
                self.metadata = original
        self.write_metadata()

    def test_bundled_support_privacy_urls_and_audience_policy_match_approved_configuration(self):
        self.enable()
        config = json.loads((self.export / "paws-monetization.json").read_text())
        for key in ("privacyPolicyUrl", "supportUrl", "audiencePolicy"):
            with self.subTest(key=key):
                self.write_runtime_asset(dict(config, **{key: "different"}))
                with self.assertRaisesRegex(ios.InspectionError, "bundled runtime configuration"):
                    self.receipt()

    def test_runtime_asset_cannot_be_replaced_by_an_external_json_receipt_copy(self):
        self.enable()
        asset = self.export / "Data/resources.assets"
        original = asset.read_bytes()
        asset.unlink()
        with self.assertRaisesRegex(ios.InspectionError, "TextAsset"):
            self.receipt()
        asset.write_bytes(original.replace(b'"production"', b'"validation"'))
        with self.assertRaisesRegex(ios.InspectionError, "runtime configuration hash"):
            self.receipt()
        asset.write_bytes(original + b"\0" * (-len(original) % 4) + original)
        with self.assertRaisesRegex(ios.InspectionError, "ambiguous"):
            self.receipt()

    def test_archive_runtime_payload_must_match_the_exported_source_configuration(self):
        self.enable(archive=True)
        asset = self.app / "Data/resources.assets"
        asset.write_bytes(asset.read_bytes().replace(b'"production"', b'"validation"'))
        with self.assertRaisesRegex(ios.InspectionError, "runtime configuration hash"):
            self.receipt(True)

    def test_test_device_hashes_are_prohibited_in_sanitized_export_copy(self):
        self.enable("validation")
        runtime = self.export / "paws-monetization.json"
        config = json.loads(runtime.read_text())
        config["umpTestDeviceIds"] = ["A" * 32]
        runtime.write_text(json.dumps(config))
        with self.assertRaisesRegex(ios.InspectionError, "redact"):
            self.receipt()

    def test_sdk_bytes_and_inventory_must_match_the_candidate_commit(self):
        self.enable()
        self.assertEqual(self.receipt()["sdkContentPins"]["filesVerified"], 1)
        sdk = self.root / "UnityProject/ThirdParty~/FixtureSDK.bin"
        sdk.write_bytes(b"tampered")
        with self.assertRaisesRegex(ios.InspectionError, "content pin"):
            self.receipt()
        with patch.object(ios, "command", side_effect=lambda args: "uncommitted inventory" if "show" in args else self.run_command(args)):
            with self.assertRaisesRegex(ios.InspectionError, "inventory versus source commit"):
                self.receipt()

    def test_enabled_export_requires_real_provider_code_privacy_resources_and_no_att_prompt(self):
        self.enable()
        self.generated.write_text("void PawsBootstrap();")
        with self.assertRaisesRegex(ios.InspectionError, "missing a real"):
            self.receipt()
        self.enable()
        self.write_plist(self.export / "Info.plist", dict(self.info, NSUserTrackingUsageDescription="unexpected"))
        with self.assertRaisesRegex(ios.InspectionError, "ATT"):
            self.receipt()
        self.enable()
        for manifest in list(self.export.rglob("*Resources.bundle/PrivacyInfo.xcprivacy")):
            saved = manifest.read_bytes()
            manifest.unlink()
            with self.assertRaisesRegex(ios.InspectionError, "privacy resource"):
                self.receipt()
            manifest.write_bytes(saved)

    def test_disabled_rollback_rejects_google_configuration_and_sdk_resources(self):
        self.write_plist(self.export / "Info.plist", dict(self.info, GADApplicationIdentifier=ios.TEST_IDS["adMobAppId"]))
        with self.assertRaisesRegex(ios.InspectionError, "advertising configuration"):
            self.receipt()
        self.write_plist(self.export / "Info.plist", self.info)
        (self.export / "GoogleMobileAds.framework").mkdir()
        with self.assertRaisesRegex(ios.InspectionError, "Google native SDK"):
            self.receipt()

    def test_enabled_archive_has_real_native_symbols_arm64_matching_symbols_and_privacy(self):
        self.enable(archive=True)
        receipt = self.receipt(True)
        self.assertTrue(receipt["archive"]["nativeIntegrity"]["requiredProviderSymbolsPresent"])
        self.assertEqual(len(receipt["archive"]["nativeIntegrity"]["binaries"]), 2)
        self.assertTrue(any(args[0] == "nm" for args in self.commands))

    def test_enabled_archive_rejects_missing_native_symbol_wrong_architecture_and_uuid(self):
        self.enable(archive=True)
        for tool, invalid in (("nm", "_GADUInitializeWithCallback"), ("lipo", "arm64 x86_64"),
                              ("dwarfdump", "missing UUID")):
            with self.subTest(tool=tool):
                with patch.object(ios, "command", side_effect=lambda args: invalid if args[0] == tool else self.run_command(args)):
                    with self.assertRaises(ios.InspectionError):
                        self.receipt(True)

    def test_owner_approved_1_0_0_is_supported_without_allocating_build_number(self):
        settings = self.root / "UnityProject/ProjectSettings/ProjectSettings.asset"
        settings.write_text(settings.read_text().replace("bundleVersion: 0.1.0", "bundleVersion: 1.0.0"))
        self.metadata["version"] = "1.0.0"
        self.write_metadata()
        self.info["CFBundleShortVersionString"] = "1.0.0"
        self.write_plist(self.export / "Info.plist", self.info)
        self.assertEqual(self.receipt()["configuration"]["buildNumber"], "1")

    def test_export_receipt_contains_real_config_packages_and_pending_human_gates(self):
        receipt = self.receipt()
        self.assertEqual(receipt["sourceSha"], SHA)
        self.assertEqual(receipt["stage"], "export-validated")
        self.assertEqual(receipt["configuration"]["deviceFamily"], [1])
        self.assertEqual(receipt["unityPackages"]["com.unity.test-framework"]["version"], "1.6.0")
        self.assertEqual(receipt["diagnosticExclusion"]["sourceFilesInspected"], 1)
        self.assertEqual(receipt["physicalIPhoneReview"], "pending-human")
        self.assertFalse(any(args[0] == "codesign" for args in self.commands))

    def test_metadata_mismatches_fail(self):
        for key, value in {"sourceSha": "b" * 40, "unityVersion": "6000.3.1f1", "bundleIdentifier": "old.bundle",
                           "version": "0.2.0", "buildNumber": "2", "minimumIOS": "15.0", "deviceFamily": [1, 2],
                           "monetizationEnabled": True, "productionDefine": "REVIEW", "developmentBuild": True,
                           "scriptDebugging": True, "profilerEnabled": True}.items():
            with self.subTest(key=key):
                original = copy.deepcopy(self.metadata)
                self.metadata[key] = value
                self.write_metadata()
                with self.assertRaises(ios.InspectionError):
                    self.receipt()
                self.metadata = original
        self.write_metadata()

    def test_modified_xcode_identifier_is_rejected(self):
        self.project["objects"]["release"]["buildSettings"]["PRODUCT_BUNDLE_IDENTIFIER"] = "com.review"
        self.write_project()
        with self.assertRaisesRegex(ios.InspectionError, "Xcode bundle identifier"):
            self.receipt()

    def test_dirty_build_inputs_reject_export_provenance(self):
        for status in (" M UnityProject/Assets/Game.cs", "?? scripts/new-build.py"):
            with self.subTest(status=status):
                self.dirty_status = status
                with self.assertRaisesRegex(ios.InspectionError, "build inputs differ from HEAD"):
                    self.receipt()
        self.assertIn(["git", "-C", str(self.root), "status", "--porcelain", "--untracked-files=all", "--",
                       "UnityProject", "scripts"], self.commands)

    def test_app_requires_unmodified_unity_provisioning_binding(self):
        settings = self.project["objects"]["release"]["buildSettings"]
        for profile in (None, "", "global-profile", "$(inherited)"):
            with self.subTest(profile=profile):
                settings["PROVISIONING_PROFILE_SPECIFIER"] = profile
                self.write_project()
                with self.assertRaisesRegex(ios.InspectionError, "app-specific provisioning binding"):
                    self.receipt()

    def test_framework_and_project_cannot_receive_app_provisioning_profile(self):
        for config_name in ("framework-release", "project-release"):
            with self.subTest(config=config_name):
                original = copy.deepcopy(self.project)
                self.project["objects"][config_name] = {"isa": "XCBuildConfiguration", "name": "Release",
                    "buildSettings": {"PROVISIONING_PROFILE_SPECIFIER": "$(PROVISIONING_PROFILE_SPECIFIER_APP)"}}
                self.write_project()
                with self.assertRaisesRegex(ios.InspectionError, "provisioning profile"):
                    self.receipt()
                self.project = original

    def test_framework_can_inherit_an_empty_project_profile(self):
        self.project["objects"]["framework-release"]["buildSettings"]["PROVISIONING_PROFILE_SPECIFIER"] = "$(inherited)"
        self.project["objects"]["project-release"] = {"isa": "XCBuildConfiguration", "name": "Release", "buildSettings": {}}
        self.write_project()
        self.assertEqual(self.receipt()["stage"], "export-validated")

    def test_release_development_flags_are_rejected(self):
        self.project["objects"]["release"]["buildSettings"]["GCC_PREPROCESSOR_DEFINITIONS"] = ["DEBUG=1"]
        self.write_project()
        with self.assertRaisesRegex(ios.InspectionError, "Development"):
            self.receipt()

    def test_missing_plist_project_metadata_or_il2cpp_fails(self):
        for path in (self.export / "Info.plist", self.export / "paws-export.json", self.generated,
                     self.export / "Unity-iPhone.xcodeproj/project.pbxproj"):
            with self.subTest(path=path.name):
                data = path.read_bytes()
                path.unlink()
                with self.assertRaises(ios.InspectionError):
                    self.receipt()
                path.write_bytes(data)

    def test_review_benchmark_and_mock_code_is_rejected(self):
        for name in ("BoardReviewBootstrap", "BoardInputReviewChecks", "GameLoopReviewChecks", "ReviewAdService",
                     "ReviewPurchaseService", "GenerationBenchmark"):
            with self.subTest(name=name):
                self.generated.write_text(f"void {name}_Start();")
                with self.assertRaisesRegex(ios.InspectionError, "production IL2CPP"):
                    self.receipt()

    def test_type_crossing_read_block_is_rejected(self):
        self.generated.write_bytes(b"x" * (1024 * 1024 - 5) + b"BoardReviewBootstrap")
        with self.assertRaises(ios.InspectionError):
            self.receipt()

    def test_export_plist_identity_version_family_and_os_are_verified(self):
        for key, value in {"CFBundleIdentifier": "old.bundle", "CFBundleDisplayName": "Other",
                           "CFBundleShortVersionString": "0.2.0", "CFBundleVersion": "2",
                           "UIDeviceFamily": [1, 2], "MinimumOSVersion": "15.0",
                           "UISupportedInterfaceOrientations": ["UIInterfaceOrientationLandscapeLeft"]}.items():
            with self.subTest(key=key):
                self.write_plist(self.export / "Info.plist", dict(self.info, **{key: value}))
                with self.assertRaises(ios.InspectionError):
                    self.receipt()
        self.write_plist(self.export / "Info.plist", self.info)

    def test_inspection_does_not_mutate_export_or_archive(self):
        before = (ios.tree_sha256(self.export), ios.tree_sha256(self.archive))
        self.receipt(True)
        self.assertEqual(before, (ios.tree_sha256(self.export), ios.tree_sha256(self.archive)))

    def test_export_records_system_framework_references(self):
        self.project["objects"]["UIKit"] = {"isa": "PBXFileReference", "path": "System/Library/Frameworks/UIKit.framework"}
        self.write_project()
        self.assertEqual(self.receipt()["exportLinkedSDKReferences"], ["System/Library/Frameworks/UIKit.framework"])

    def test_unsigned_archive_needs_no_signing_credentials(self):
        result = self.receipt(True)
        self.assertEqual(result["stage"], "unsigned-archive-validated")
        archive = result["archive"]
        self.assertFalse(archive["signatureVerified"])
        self.assertEqual(len(archive["archiveTreeSha256"]), 64)
        self.assertEqual(archive["capabilities"], ["arm64", "metal"])
        self.assertIn("UIKit.framework", archive["linkedLibraries"][0]["linkedLibraries"][0])
        self.assertFalse(any(args[0] == "codesign" for args in self.commands))

    def test_signed_archive_verifies_signature_and_reads_entitlements(self):
        signature = self.app / "_CodeSignature/CodeResources"
        signature.parent.mkdir()
        signature.write_bytes(b"fixture")
        self.entitlements = {"get-task-allow": False, "aps-environment": "production"}
        result = self.receipt(True)
        self.assertEqual(result["stage"], "signed-archive-validated")
        self.assertTrue(result["archive"]["signatureVerified"])
        self.assertEqual(result["archive"]["entitlements"]["aps-environment"], "production")
        self.assertTrue(any(args[:4] == ["codesign", "--verify", "--deep", "--strict"] for args in self.commands))
        self.entitlements["get-task-allow"] = True
        with self.assertRaisesRegex(ios.InspectionError, "development debugging"):
            self.receipt(True)

    def test_archive_requires_app_archive_metadata_and_executable(self):
        for path in (self.app / "Info.plist", self.archive / "Info.plist", self.app / "Paws"):
            with self.subTest(path=path):
                data = path.read_bytes()
                path.unlink()
                with self.assertRaises(ios.InspectionError):
                    self.receipt(True)
                path.write_bytes(data)

    def test_archive_mismatch_rejected(self):
        info = ios.read_plist(self.app / "Info.plist")
        info["UIDeviceFamily"] = [1, 2]
        self.write_plist(self.app / "Info.plist", info)
        with self.assertRaisesRegex(ios.InspectionError, "device family"):
            self.receipt(True)

    def test_inventory_records_framework_library_privacy_and_entitlements(self):
        framework = self.app / "Frameworks/UnityFramework.framework"
        framework.mkdir(parents=True)
        (framework / "UnityFramework").write_bytes(b"framework")
        (self.app / "libExample.dylib").write_bytes(b"library")
        self.write_plist(self.app / "PrivacyInfo.xcprivacy", {"NSPrivacyTracking": False})
        self.write_plist(self.export / "Paws.entitlements", {"get-task-allow": False})
        receipt = self.receipt(True)
        inventory = receipt["archive"]["inventory"]
        self.assertEqual(len(inventory["frameworks"]), 1)
        self.assertEqual(len(inventory["nativeLibraries"]), 1)
        self.assertFalse(inventory["privacyManifests"][0]["declarations"]["NSPrivacyTracking"])
        self.assertFalse(receipt["exportInventory"]["entitlementFiles"][0]["values"]["get-task-allow"])

    def test_checksum_ignores_times_but_tracks_paths_content_permissions_and_links(self):
        original = ios.tree_sha256(self.archive)
        os.utime(self.app / "Paws", (1, 1))
        self.assertEqual(original, ios.tree_sha256(self.archive))
        (self.app / "Paws").write_bytes(b"changed")
        changed = ios.tree_sha256(self.archive)
        self.assertNotEqual(original, changed)
        (self.app / "Paws").chmod(0o700)
        self.assertNotEqual(changed, ios.tree_sha256(self.archive))
        (self.app / "safe-link").symlink_to("Paws")
        self.assertEqual(len(ios.tree_sha256(self.archive)), 64)
        (self.app / "escape").symlink_to(self.root / "export")
        with self.assertRaisesRegex(ios.InspectionError, "escapes"):
            ios.tree_sha256(self.archive)

    def test_cli_preserves_existing_output_and_rejects_receipt_inside_export(self):
        output = self.root / "receipt.json"
        output.write_text("keep")
        for path in (output, self.export / "receipt.json"):
            with self.subTest(path=path), patch.object(ios.sys, "argv", [str(SCRIPT), "--export", str(self.export), "--output", str(path)]):
                self.assertEqual(ios.main(), 1)
        self.assertEqual(output.read_text(), "keep")
        self.assertFalse((self.export / "receipt.json").exists())


if __name__ == "__main__":
    unittest.main()
