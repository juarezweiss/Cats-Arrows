#!/usr/bin/env python3
"""#63/#67: inspect immutable iOS rollback, validation and monetized releases."""

import argparse
import hashlib
import json
import os
import plistlib
import re
import struct
import subprocess
import sys
from pathlib import Path
from urllib.parse import urlsplit

BUNDLE_ID = "com.jweiss.paws"
EXCLUDED_TYPES = re.compile(rb"BoardReviewBootstrap|(?:BoardInput|GameLoop|Completion|HintIdle|ArrowHold|Tutorial|Monetization)ReviewChecks|ReviewAdService|ReviewPurchaseService|AppleIapReviewBootstrap|AdMobReviewBootstrap|GenerationBenchmark")
PRODUCT_ID = "com.jweiss.paws.removeads"
TEST_PUBLISHER = "ca-app-pub-3940256099942544"
TEST_IDS = {"adMobAppId": TEST_PUBLISHER + "~1458002511",
            "interstitialAdUnitId": TEST_PUBLISHER + "/4411468910",
            "rewardedAdUnitId": TEST_PUBLISHER + "/1712485313"}
PROVIDERS = ("appleIapEnabled", "offerCodesEnabled", "admobEnabled", "umpEnabled")


class InspectionError(ValueError):
    pass


def command(args):
    result = subprocess.run(args, capture_output=True, text=True, check=False)
    if result.returncode:
        # Tool stderr may contain signing identities or local account details.
        raise InspectionError(f"{Path(args[0]).name} inspection failed (exit {result.returncode}).")
    return result.stdout


def read_json(path):
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, ValueError) as error:
        raise InspectionError(f"Missing or invalid JSON: {path.name}") from error


def read_plist(path):
    try:
        with path.open("rb") as stream:
            result = plistlib.load(stream)
        if not isinstance(result, dict):
            raise ValueError("dictionary required")
        return result
    except (OSError, ValueError, plistlib.InvalidFileException) as error:
        raise InspectionError(f"Missing or invalid plist: {path.name}") from error


def file_sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def tree_sha256(path):
    """Hash sorted path/type/mode/content records; ignore modification times.

    Symlinks are hashed as links and must resolve inside the inspected tree.
    No tarball or other disposable archive is created.
    """
    root = path.resolve()
    digest = hashlib.sha256()
    for item in sorted(root.rglob("*"), key=lambda item: item.relative_to(root).as_posix()):
        relative = item.relative_to(root).as_posix()
        if item.is_symlink():
            if not item.resolve().is_relative_to(root):
                raise InspectionError(f"Archive symlink escapes its root: {relative}")
            kind, value = "symlink", os.readlink(item)
        elif item.is_file():
            kind, value = "file", file_sha256(item)
        elif item.is_dir():
            kind, value = "directory", ""
        else:
            raise InspectionError(f"Unsupported archive entry: {relative}")
        mode = item.lstat().st_mode & 0o777
        digest.update(json.dumps([relative, kind, mode, value], separators=(",", ":")).encode() + b"\n")
    return digest.hexdigest()


def unity_config(root):
    source = (root / "UnityProject/ProjectSettings/ProjectSettings.asset").read_text(encoding="utf-8-sig")

    def field(name, section=None):
        text = source
        if section:
            match = re.search(rf"^  {section}:\s*\n((?:    .*\n)+)", source, re.M)
            if not match:
                raise InspectionError(f"Missing Unity section: {section}")
            text = match.group(1)
        match = re.search(rf"^\s+{name}:\s*([^\n]+)", text, re.M)
        if not match:
            raise InspectionError(f"Missing Unity setting: {name}")
        return match.group(1).strip().strip('\"\'')

    family = {"0": [1], "1": [2], "2": [1, 2]}.get(field("targetDevice"))
    if family is None:
        raise InspectionError("Unsupported Unity iOS targetDevice setting.")
    unity_version = re.search(r"^m_EditorVersion:\s*(\S+)",
                              (root / "UnityProject/ProjectSettings/ProjectVersion.txt").read_text(), re.M)
    if not unity_version:
        raise InspectionError("Missing pinned Unity version.")
    return {"bundleIdentifier": field("iPhone", "applicationIdentifier"),
            "version": field("bundleVersion"), "buildNumber": field("iPhone", "buildNumber"),
            "minimumIOS": field("iOSTargetOSVersionString"), "deviceFamily": family,
            "displayName": field("productName"), "unityVersion": unity_version.group(1)}


def expect(actual, expected, label):
    if actual != expected:
        raise InspectionError(f"Configuration mismatch: {label}.")


def ios_version(value):
    if not isinstance(value, str) or not re.fullmatch(r"\d+(?:\.\d+){0,2}", value):
        raise InspectionError("Invalid minimum iOS version.")
    return tuple((list(map(int, value.split("."))) + [0, 0])[:3])


def project_settings(export):
    project = export / "Unity-iPhone.xcodeproj/project.pbxproj"
    if not project.is_file():
        raise InspectionError("Missing Unity-iPhone.xcodeproj/project.pbxproj.")
    try:
        data = json.loads(project.read_text())
    except ValueError:
        data = json.loads(command(["plutil", "-convert", "json", "-o", "-", str(project)]))
    objects = data.get("objects", {})
    def target_release(name):
        targets = [item for item in objects.values()
                   if item.get("isa") == "PBXNativeTarget" and item.get("name") == name]
        if len(targets) != 1:
            raise InspectionError(f"Expected exactly one {name} target.")
        config_list = objects.get(targets[0].get("buildConfigurationList"), {})
        configs = [objects.get(key, {}) for key in config_list.get("buildConfigurations", [])]
        release = [item.get("buildSettings", {}) for item in configs if item.get("name") == "Release"]
        if len(release) != 1:
            raise InspectionError(f"Expected exactly one {name} Release configuration.")
        return release[0]

    app_release = target_release("Unity-iPhone")
    framework_release = target_release("UnityFramework")
    expect(app_release.get("PROVISIONING_PROFILE_SPECIFIER"), "$(PROVISIONING_PROFILE_SPECIFIER_APP)",
           "Unity app-specific provisioning binding")
    if framework_release.get("PROVISIONING_PROFILE_SPECIFIER") not in (None, "", "$(inherited)"):
        raise InspectionError("UnityFramework must not receive an app provisioning profile.")
    # Project and framework Release flags can enable development code too.
    for item in objects.values():
        if item.get("isa") == "XCBuildConfiguration" and item.get("name") == "Release":
            settings = item.get("buildSettings", {})
            if settings is not app_release and settings.get("PROVISIONING_PROFILE_SPECIFIER") not in (None, "", "$(inherited)"):
                raise InspectionError("App provisioning profile must be scoped to Unity-iPhone only.")
            flags = json.dumps(settings)
            if re.search(r"(?:DEBUG|DEVELOPMENT_BUILD|ENABLE_PROFILER|IL2CPP_MONO_DEBUGGER)\s*=\s*1", flags):
                raise InspectionError("Development/debug/profiler flag in Release configuration.")
            if settings.get("ENABLE_TESTABILITY") == "YES":
                raise InspectionError("Release configuration enables testability.")
    return app_release, data


def resolve_setting(value, settings):
    if isinstance(value, str):
        match = re.fullmatch(r"\$[({]([A-Z_]+)[)}]", value)
        if match:
            return settings.get(match.group(1), value)
    return value


def inspect_identity(info, config, settings=None, archived=False):
    settings = settings or {}
    value = lambda key: resolve_setting(info.get(key), settings)
    expect(value("CFBundleIdentifier"), config["bundleIdentifier"], "bundle identifier")
    expect(str(value("CFBundleShortVersionString")), config["version"], "app version")
    expect(str(value("CFBundleVersion")), config["buildNumber"], "build number")
    expect(value("CFBundleDisplayName"), config["displayName"], "public display name")
    minimum = info.get("MinimumOSVersion", settings.get("IPHONEOS_DEPLOYMENT_TARGET"))
    expect(ios_version(minimum), ios_version(config["minimumIOS"]), "minimum iOS")
    family = info.get("UIDeviceFamily")
    if family is None and not archived:
        family = [int(part) for part in str(settings.get("TARGETED_DEVICE_FAMILY", "")).split(",") if part]
    expect(family, config["deviceFamily"], "supported device family")
    expect(info.get("UISupportedInterfaceOrientations"), ["UIInterfaceOrientationPortrait"], "portrait orientation")
    if info.get("get-task-allow"):
        raise InspectionError("Development entitlement in app Info.plist.")


def inventory(path):
    frameworks, libraries, manifests, entitlements = [], [], [], []
    for item in sorted(path.rglob("*")):
        relative = item.relative_to(path).as_posix()
        if item.suffix == ".framework" and item.is_dir():
            frameworks.append(relative)
        if item.suffix in (".a", ".dylib", ".tbd") and item.is_file():
            libraries.append({"path": relative, "sha256": file_sha256(item)})
        if item.name == "PrivacyInfo.xcprivacy" and item.is_file():
            manifests.append({"path": relative, "sha256": file_sha256(item), "declarations": read_plist(item)})
        if item.suffix == ".entitlements" and item.is_file():
            entitlements.append({"path": relative, "values": read_plist(item)})
    return {"frameworks": frameworks, "nativeLibraries": libraries,
            "privacyManifests": manifests, "entitlementFiles": entitlements}


def inspect_il2cpp(export, profile="disabled"):
    directory = export / "Il2CppOutputProject/Source/il2cppOutput"
    sources = sorted(item for item in directory.rglob("*") if item.suffix in (".cpp", ".h") and item.is_file())
    if not sources:
        raise InspectionError("Missing generated IL2CPP sources; cannot verify diagnostic exclusion.")
    found = set()
    required = (b"ApplePurchaseService", b"GoogleAdsBridge", b"AdMobAdService")
    for source in sources:
        if EXCLUDED_TYPES.search(source.name.encode()):
            raise InspectionError("Review, benchmark or mock type found in production IL2CPP output.")
        with source.open("rb") as stream:
            tail = b""
            for block in iter(lambda: stream.read(1024 * 1024), b""):
                data = tail + block
                if EXCLUDED_TYPES.search(data):
                    raise InspectionError("Review, benchmark or mock type found in production IL2CPP output.")
                if profile == "production" and TEST_PUBLISHER.encode() in data:
                    raise InspectionError("Google test identifier found in production IL2CPP output.")
                for provider in required:
                    if provider in data:
                        found.add(provider)
                tail = data[-256:]
    if profile != "disabled" and found != set(required):
        raise InspectionError("Enabled export is missing a real purchase/advertising provider.")
    if profile == "disabled" and b"GoogleAdsBridge" in found:
        raise InspectionError("Disabled rollback includes the Google provider binding.")
    return {"sourceFilesInspected": len(sources), "excludedTypesAbsent": True}


def inspect_monetization(metadata, export, info):
    # Missing profile is accepted only for immutable historical #63 disabled exports.
    profile = metadata.get("profile", "disabled")
    if profile not in ("disabled", "validation", "production"):
        raise InspectionError("Unknown monetization profile.")
    enabled = profile != "disabled"
    expect(metadata.get("monetizationEnabled"), enabled, "profile monetization state")
    if not enabled:
        if info.get("GADApplicationIdentifier") or (export / "paws-monetization.json").exists():
            raise InspectionError("Disabled rollback contains advertising configuration.")
        return {"profile": profile, "distributionEligible": False,
                "purpose": "disabled-rollback", "providersEnabled": False}
    for flag in PROVIDERS:
        expect(metadata.get(flag), True, flag)
    expect(metadata.get("appleProductId"), PRODUCT_ID, "non-consumable product identity")
    expect(metadata.get("simulatedPurchases"), False, "simulated ownership exclusion")
    expect(metadata.get("testAds"), profile == "validation", "Google test-ad isolation")
    count = metadata.get("testDeviceCount")
    geography = metadata.get("consentDebugGeography")
    if type(count) is not int or count < 0 or geography not in ("Disabled", "EEA", "RegulatedUSState", "Other"):
        raise InspectionError("Invalid UMP validation configuration.")
    if profile == "production":
        expect(count, 0, "production test-device configuration")
        expect(geography, "Disabled", "production UMP debug geography")
    runtime = read_json(export / "paws-monetization.json")
    expect(runtime.get("profile"), profile, "runtime profile")
    expect(metadata.get("audiencePolicy"), "local-adult-only-v1", "approved audience policy")
    expect(runtime.get("audiencePolicy"), metadata["audiencePolicy"], "runtime audience policy")
    for key in ("privacyPolicyUrl", "supportUrl"):
        value = metadata.get(key, "")
        if value or profile == "production":
            if not isinstance(value, str):
                raise InspectionError("Invalid public HTTPS support/privacy URL.")
            url = urlsplit(value)
            if url.scheme != "https" or not url.hostname or url.username is not None or url.password is not None:
                raise InspectionError("Production requires public HTTPS support/privacy URLs without credentials.")
        expect(runtime.get(key, ""), value, "runtime public support/privacy URL")
    for key in TEST_IDS:
        value = metadata.get(key, "")
        pattern = r"ca-app-pub-\d{16}" + (r"~\d{10}" if key == "adMobAppId" else r"/\d{10}")
        if not isinstance(value, str) or not re.fullmatch(pattern, value):
            raise InspectionError("Missing or invalid advertising identifier.")
        if profile == "production" and value.startswith(TEST_PUBLISHER):
            raise InspectionError("Google test identifiers are prohibited in production.")
        if profile == "validation" and key != "adMobAppId":
            expect(value, TEST_IDS[key], "official Google validation identifier")
        expect(runtime.get(key), value, "runtime advertising identifier")
    if metadata["interstitialAdUnitId"] == metadata["rewardedAdUnitId"]:
        raise InspectionError("Advertising placements must have distinct units.")
    if profile == "production":
        publisher = metadata["adMobAppId"].split("~")[0]
        if any(not metadata[key].startswith(publisher + "/") for key in ("interstitialAdUnitId", "rewardedAdUnitId")):
            raise InspectionError("Production ad units must match the configured app publisher.")
    # This readable copy is deliberately sanitized; only the serialized TextAsset
    # below can establish the actual device-debug configuration shipped in Data.
    if runtime.get("umpTestDeviceIds", []) != []:
        raise InspectionError("Export configuration copy must redact test-device identifiers.")
    expect(runtime.get("consentDebugGeography", "Disabled"), geography, "sanitized UMP geography")
    expect(info.get("GADApplicationIdentifier"), metadata["adMobAppId"], "native Google app identifier")
    expect(info.get("GADDisableAdInspector"), True, "disabled Ad Inspector")
    if "NSUserTrackingUsageDescription" in info:
        raise InspectionError("Unexpected ATT prompt configuration for approved nontracking profile.")
    resource = inspect_runtime_resource(export, metadata)
    return {"profile": profile, "providersEnabled": True,
            "runtimeConfigurationSha256": resource["sha256"], "exportRuntimeResource": resource,
            "distributionEligible": profile == "production",
            "purchaseEnvironmentEvidence": "pending-human-apple-sandbox",
            "storeKitTestEvidence": "separate-simulation-only",
            "liveTrafficAuthorization": "not-granted"}


def inspect_runtime_resource(path, metadata):
    """Inspect Unity's serialized TextAsset payload, not an unbundled JSON copy.

    TextAsset serializes length-prefixed m_Name then aligned length-prefixed
    m_Script. Require a unique, valid named payload and its build-recorded hash;
    do not claim a complete Unity serialized-file/dependency graph validation.
    Never include private UMP device identifiers in the receipt or diagnostics.
    """
    files = list(path.rglob("Data/resources.assets"))
    name = b"PawsMonetization.generated"
    candidates = []
    for file in files:
        data = file.read_bytes()
        offset = data.find(name)
        while offset != -1:
            if offset >= 4 and data[offset - 4:offset] == struct.pack("<I", len(name)):
                cursor = (offset + len(name) + 3) & ~3
                if cursor + 4 <= len(data):
                    length = struct.unpack_from("<I", data, cursor)[0]
                    if 0 < length <= 1024 * 1024 and cursor + 4 + length <= len(data):
                        payload = data[cursor + 4:cursor + 4 + length]
                        try:
                            config = json.loads(payload.decode("utf-8"))
                            if isinstance(config, dict):
                                candidates.append((file, payload, config))
                        except (UnicodeDecodeError, ValueError):
                            pass
            offset = data.find(name, offset + len(name))
    if len(candidates) != 1:
        raise InspectionError("Bundled monetization TextAsset is missing, malformed or ambiguous.")
    file, payload, config = candidates[0]
    digest = hashlib.sha256(payload).hexdigest()
    expect(digest, metadata.get("runtimeConfigurationSha256"), "bundled runtime configuration hash")
    for key in ("profile", *TEST_IDS, "privacyPolicyUrl", "supportUrl", "audiencePolicy"):
        expect(config.get(key), metadata.get(key), "bundled runtime configuration")
    geography = config.get("consentDebugGeography", "Disabled")
    devices = config.get("umpTestDeviceIds", [])
    if not isinstance(devices, list) or any(not isinstance(item, str) or not re.fullmatch(r"[0-9A-Fa-f]{32,64}", item) for item in devices):
        raise InspectionError("Invalid bundled UMP test-device configuration.")
    expect(len(devices), metadata.get("testDeviceCount"), "bundled UMP test-device count")
    expect(geography, metadata.get("consentDebugGeography"), "bundled UMP geography")
    if metadata.get("profile") == "production" and (devices or geography != "Disabled"):
        raise InspectionError("Production runtime contains UMP debug configuration.")
    return {"path": file.relative_to(path).as_posix(), "sha256": digest,
            "payloadBytes": len(payload), "namedTextAssetPayloadVerified": True,
            "testDeviceCount": len(devices), "consentDebugGeography": geography}


def inspect_sdk_pins(root, source_sha):
    relative = "docs/evidence/issue66/sdk-inventory.json"
    path = root / relative
    pinned = command(["git", "-C", str(root), "show", source_sha + ":" + relative])
    current = path.read_text(encoding="utf-8")
    expect(current.rstrip("\n"), pinned.rstrip("\n"), "SDK inventory versus source commit")
    files = read_json(path).get("files", [])
    if not files:
        raise InspectionError("SDK content pin inventory is empty.")
    seen = set()
    for item in files:
        source = root / item["path"]
        if not source.resolve().is_relative_to(root.resolve()) or item["path"] in seen:
            raise InspectionError("SDK inventory contains an unsafe or duplicate path.")
        seen.add(item["path"])
        expect(file_sha256(source), item["sha256"], "inspected SDK content pin")
    return {"sourceSha": source_sha, "inventoryPath": relative,
            "inventorySha256": file_sha256(path), "filesVerified": len(files)}


def inspect_provider_resources(path, config):
    if not config["providersEnabled"]:
        if any("PawsAdMob" in str(item) or "GoogleMobileAds" in item.name or
               "UserMessagingPlatform" in item.name for item in path.rglob("*")):
            raise InspectionError("Disabled rollback contains Google native SDK resources.")
        return
    for name in ("GoogleMobileAds", "UserMessagingPlatform"):
        manifests = list(path.rglob(name + "Resources.bundle/PrivacyInfo.xcprivacy"))
        if len(manifests) != 1:
            raise InspectionError("Required Google SDK privacy resource is missing or ambiguous.")
        read_plist(manifests[0])


def inspect_native_integrity(archive):
    apps = list((archive / "Products/Applications").glob("*.app"))
    app = apps[0]
    executable = app / read_plist(app / "Info.plist")["CFBundleExecutable"]
    framework = app / "Frameworks/UnityFramework.framework/UnityFramework"
    records = []
    for binary, symbols in ((executable, archive / "dSYMs" / (app.name + ".dSYM")),
                            (framework, archive / "dSYMs/UnityFramework.framework.dSYM")):
        if not binary.is_file() or not symbols.is_dir():
            raise InspectionError("Release binary or matching dSYM is missing.")
        expect(command(["lipo", "-archs", str(binary)]).strip(), "arm64", "release binary architecture")
        def uuids(target):
            output = command(["dwarfdump", "--uuid", str(target)])
            values = re.findall(r"UUID: ([0-9A-Fa-f-]{36}) \(([^)]+)\)", output)
            if not values:
                raise InspectionError("Missing Mach-O UUID evidence.")
            return sorted((uuid.upper(), arch) for uuid, arch in values)
        binary_ids = uuids(binary)
        expect(uuids(symbols), binary_ids, "binary/dSYM UUID match")
        records.append({"binary": binary.relative_to(archive).as_posix(),
                        "dSYM": symbols.relative_to(archive).as_posix(), "uuids": binary_ids})
    native_symbols = command(["nm", "-gjU", str(framework)])
    required = ("_PawsIapRead", "_PawsIapVerifyCache", "_PawsIapPresentOfferCodeSheet",
                "_GADUInitializeWithCallback", "_GADURequestConsentInfoUpdate", "_GADUUMPCanRequestAds")
    lines = set(native_symbols.splitlines())
    if not all(symbol in lines for symbol in required):
        raise InspectionError("Real Apple IAP/Offer Code/AdMob/UMP native entry point is missing.")
    return {"binaries": records, "requiredProviderSymbolsPresent": True}


def inspect_archive(archive, config):
    if not archive.is_dir() or archive.suffix != ".xcarchive":
        raise InspectionError("Archive must be an existing .xcarchive directory.")
    archive_info = read_plist(archive / "Info.plist")
    applications = list((archive / "Products/Applications").glob("*.app"))
    if len(applications) != 1:
        raise InspectionError("Archive must contain exactly one application.")
    app = applications[0]
    app_info = read_plist(app / "Info.plist")
    inspect_identity(app_info, config, archived=True)
    properties = archive_info.get("ApplicationProperties", {})
    expect(properties.get("CFBundleIdentifier"), config["bundleIdentifier"], "archive bundle identifier")
    expect(str(properties.get("CFBundleShortVersionString")), config["version"], "archive app version")
    expect(str(properties.get("CFBundleVersion")), config["buildNumber"], "archive build number")
    executable = app / str(app_info.get("CFBundleExecutable", ""))
    if not executable.is_file():
        raise InspectionError("Archive application executable is missing.")
    signed = (app / "_CodeSignature/CodeResources").is_file()
    entitlements = {}
    if signed:
        command(["codesign", "--verify", "--deep", "--strict", str(app)])
        raw = command(["codesign", "-d", "--entitlements", ":-", str(app)])
        try:
            entitlements = plistlib.loads(raw.encode())
        except (ValueError, plistlib.InvalidFileException) as error:
            raise InspectionError("Cannot inspect signed application entitlements.") from error
        if entitlements.get("get-task-allow"):
            raise InspectionError("Archive permits development debugging (get-task-allow).")
    binaries = [executable]
    for framework in app.rglob("*.framework"):
        binary = framework / framework.stem
        if binary.is_file():
            binaries.append(binary)
    binaries.extend(item for item in app.rglob("*.dylib") if item.is_file())
    dependencies = []
    for binary in sorted(set(binaries)):
        lines = command(["otool", "-L", str(binary)]).splitlines()
        dependencies.append({"binary": binary.relative_to(archive).as_posix(),
                             "linkedLibraries": [line.strip() for line in lines[1:] if line.strip()]})
    return {"stage": "signed-archive-validated" if signed else "unsigned-archive-validated",
            "archiveTreeSha256": tree_sha256(archive), "checksumAlgorithm": "paws-tree-sha256-v1",
            "signatureVerified": signed, "entitlements": entitlements,
            "capabilities": app_info.get("UIRequiredDeviceCapabilities", []),
            "backgroundModes": app_info.get("UIBackgroundModes", []),
            "infoPlist": app_info, "inventory": inventory(archive), "linkedLibraries": dependencies,
            "distributionApproval": "pending-owner", "physicalIPhoneReview": "pending-human"}


def build_receipt(root, export, archive=None):
    dirty = command(["git", "-C", str(root), "status", "--porcelain", "--untracked-files=all", "--",
                     "UnityProject", "scripts"]).strip()
    if dirty:
        raise InspectionError("UnityProject/scripts build inputs differ from HEAD; commit or restore them before receipt inspection.")
    config = unity_config(root)
    expect(config["bundleIdentifier"], BUNDLE_ID, "approved production identity")
    if not re.fullmatch(r"(?:0\.\d+\.\d+|1\.0\.0)", config["version"]):
        raise InspectionError("Production preparation requires an approved 0.x.y or 1.0.0 app version.")
    if not re.fullmatch(r"[1-9]\d*", config["buildNumber"]):
        raise InspectionError("iOS build number must be a positive integer.")
    metadata = read_json(export / "paws-export.json")
    sha = command(["git", "-C", str(root), "rev-parse", "HEAD"]).strip()
    if not re.fullmatch(r"[0-9a-f]{40}", sha):
        raise InspectionError("Cannot establish source commit SHA.")
    expect(metadata.get("sourceSha"), sha, "export source SHA versus current checkout")
    for key in ("bundleIdentifier", "version", "buildNumber", "minimumIOS", "deviceFamily", "unityVersion"):
        expect(metadata.get(key), config[key], f"export {key}")
    expect(metadata.get("productionDefine"), "PAWS_PRODUCTION", "production compilation define")
    for flag in ("developmentBuild", "scriptDebugging", "profilerEnabled"):
        expect(metadata.get(flag, False), False, flag)
    settings, project = project_settings(export)
    expect(settings.get("PRODUCT_BUNDLE_IDENTIFIER"), BUNDLE_ID, "Unity-generated Xcode bundle identifier")
    info = read_plist(export / "Info.plist")
    inspect_identity(info, config, settings)
    monetization = inspect_monetization(metadata, export, info)
    inspect_provider_resources(export, monetization)
    exclusion = inspect_il2cpp(export, monetization["profile"])
    packages = read_json(root / "UnityProject/Packages/packages-lock.json").get("dependencies", {})
    receipt = {"schema": "paws-ios-release-receipt-v1", "sourceSha": sha, "configuration": config,
               "enabledConfiguration": metadata, "monetization": monetization, "stage": "export-validated",
               "unityVersion": config["unityVersion"], "xcodeVersion": command(["xcodebuild", "-version"]).strip(),
               "exportInfoPlist": info, "exportInventory": inventory(export),
               "exportLinkedSDKReferences": sorted({str(item.get("path", ""))
                    for item in project.get("objects", {}).values()
                    if item.get("isa") == "PBXFileReference" and
                    str(item.get("path", "")).endswith((".framework", ".tbd", ".dylib", ".a"))}),
               "exportProjectCapabilities": [item.get("attributes", {}).get("TargetAttributes", {})
                                             for item in project.get("objects", {}).values() if item.get("isa") == "PBXProject"],
               "unityPackages": packages, "diagnosticExclusion": exclusion,
               "physicalIPhoneReview": "pending-human", "distributionApproval": "pending-owner"}
    if monetization["providersEnabled"]:
        receipt["sdkContentPins"] = inspect_sdk_pins(root, sha)
    if archive:
        receipt["archive"] = inspect_archive(archive, config)
        inspect_provider_resources(archive, monetization)
        if monetization["providersEnabled"]:
            archived_info = receipt["archive"]["infoPlist"]
            for key in ("GADApplicationIdentifier", "GADDisableAdInspector", "NSUserTrackingUsageDescription"):
                expect(archived_info.get(key), info.get(key), "archived native advertising/privacy configuration")
            receipt["archive"]["nativeIntegrity"] = inspect_native_integrity(archive)
            receipt["archive"]["runtimeResource"] = inspect_runtime_resource(archive, metadata)
        receipt["stage"] = receipt["archive"]["stage"]
    return receipt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--export", required=True, type=Path)
    parser.add_argument("--archive", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    try:
        export = args.export.resolve(strict=True)
        archive = args.archive.resolve(strict=True) if args.archive else None
        output = args.output.resolve()
        if output.exists():
            raise InspectionError("Receipt output already exists; choose a fresh path.")
        if output.is_relative_to(export) or (archive and output.is_relative_to(archive)):
            raise InspectionError("Write the receipt outside the inspected export/archive.")
        receipt = build_receipt(Path(__file__).resolve().parents[1], export, archive)
        with output.open("x", encoding="utf-8") as stream:
            json.dump(receipt, stream, indent=2, sort_keys=True)
            stream.write("\n")
    except (InspectionError, OSError, ValueError, KeyError, TypeError) as error:
        print(f"iOS release inspection failed: {error}", file=sys.stderr)
        return 1
    print(f"iOS release receipt written ({receipt['stage']}): {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
