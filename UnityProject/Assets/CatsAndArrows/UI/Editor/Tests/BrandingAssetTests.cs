using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace CatsAndArrows.Tests
{
    // #80: pin owner references and check the actual platform icon assignments/import settings.
    public sealed class BrandingAssetTests
    {
        private const string IconPath = "Assets/CatsAndArrows/UI/PawsAppIcon.png";
        private const string PawPath = "Assets/Resources/MenuPaw.png";

        [TestCase(IconPath, "b891542a9a84aafcf079f222b3002037ee07f80f9dbd457059f8262fe475a788")]
        [TestCase(PawPath, "bf7e5ee38e7b5ab28e6d4d15d8d42d61f77bf30f443a44b71e765ceb34cd2afe")]
        public void OwnerReferenceFilesAreUnmodified(string path, string expected)
        {
            using (var sha = SHA256.Create())
                Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(), Is.EqualTo(expected));
        }

        [Test]
        public void EveryIOSIconSlotAndDefaultUseTheApprovedIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.width, Is.EqualTo(1024), "Native iOS icon source is imported at 1024 square");
            Assert.That(icon.height, Is.EqualTo(1024));
            Assert.That(PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any), Is.EqualTo(new[] { icon }));
            var slots = 0;
            var store = false;
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS))
            foreach (var slot in PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind))
            {
                Assert.That(slot.GetTexture(), Is.SameAs(icon), kind + " " + slot.width + "x" + slot.height);
                store |= slot.width == 1024 && slot.height == 1024;
                slots++;
            }
            Assert.That(slots, Is.GreaterThanOrEqualTo(19));
            Assert.That(store, Is.True, "App Store icon is assigned");
        }

        [Test]
        public void MenuImportsOnlyReferenceAlphaWithFlatWhiteRGBForExistingInkTint()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(PawPath);
            Assert.That(importer.swizzleR, Is.EqualTo(TextureImporterSwizzle.One));
            Assert.That(importer.swizzleG, Is.EqualTo(TextureImporterSwizzle.One));
            Assert.That(importer.swizzleB, Is.EqualTo(TextureImporterSwizzle.One));
            Assert.That(importer.swizzleA, Is.EqualTo(TextureImporterSwizzle.A));
            Assert.That(importer.alphaSource, Is.EqualTo(TextureImporterAlphaSource.FromInput));
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            foreach (var platform in new[] { "iPhone", "Standalone", "Android" })
                Assert.That(importer.GetPlatformTextureSettings(platform).overridden, Is.False, platform);
            var texture = Resources.Load<Texture2D>("MenuPaw");
            Assert.That(texture, Is.SameAs(AssetDatabase.LoadAssetAtPath<Texture2D>(PawPath)));
            Assert.That(texture.width, Is.EqualTo(1254)); Assert.That(texture.height, Is.EqualTo(1254));
        }
    }
}
