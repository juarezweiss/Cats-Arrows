using System;
using System.Text.RegularExpressions;

namespace CatsAndArrows.Production
{
    [Serializable]
    public sealed class ProductionMonetizationConfig
    {
        public const string AudiencePolicy = "local-adult-only-v1";
        public const string ProductId = "com.jweiss.paws.removeads";
        public string audiencePolicy;
        public string privacyPolicyUrl = "", supportUrl = "";
        public string profile, adMobAppId, interstitialAdUnitId, rewardedAdUnitId;
        public string consentDebugGeography = "Disabled";
        public string[] umpTestDeviceIds = Array.Empty<string>();
        public bool Enabled => profile != "disabled";
        public bool TestAds => profile == "validation";
        public void Validate()
        {
            if (profile != "disabled" && profile != "validation" && profile != "production")
                throw new ArgumentException("Monetization profile must be production, validation or disabled.");
            if (umpTestDeviceIds == null) umpTestDeviceIds = Array.Empty<string>();
            if (consentDebugGeography != "Disabled" && consentDebugGeography != "EEA" &&
                consentDebugGeography != "RegulatedUSState" && consentDebugGeography != "Other")
                throw new ArgumentException("Invalid UMP debug geography.");
            if (!TestAds && (consentDebugGeography != "Disabled" || umpTestDeviceIds.Length > 0))
                throw new ArgumentException("UMP debug configuration is validation-only.");
            if (consentDebugGeography != "Disabled" && umpTestDeviceIds.Length == 0)
                throw new ArgumentException("Debug geography requires registered UMP test devices.");
            foreach (var device in umpTestDeviceIds)
                if (!Regex.IsMatch(device ?? "", @"^[A-Fa-f0-9]{32,64}$"))
                    throw new ArgumentException("Invalid UMP test-device hash.");
            if (!Enabled)
            {
                if (!string.IsNullOrEmpty(adMobAppId) || !string.IsNullOrEmpty(interstitialAdUnitId) || !string.IsNullOrEmpty(rewardedAdUnitId))
                    throw new ArgumentException("Disabled profile must contain no ad identifiers.");
                return;
            }
            if (audiencePolicy != AudiencePolicy)
                throw new ArgumentException("Explicit local-adult-only-v1 audience policy is required.");
            ValidateLink(privacyPolicyUrl, !TestAds);
            ValidateLink(supportUrl, !TestAds);
            if (!Regex.IsMatch(adMobAppId ?? "", @"^ca-app-pub-[0-9]{16}~[0-9]{10}$") ||
                !Regex.IsMatch(interstitialAdUnitId ?? "", @"^ca-app-pub-[0-9]{16}/[0-9]{10}$") ||
                !Regex.IsMatch(rewardedAdUnitId ?? "", @"^ca-app-pub-[0-9]{16}/[0-9]{10}$"))
                throw new ArgumentException("Valid owner-provided iOS AdMob app and placement identifiers are required.");
            var publisher = adMobAppId.Substring(0, 27);
            if ((!TestAds && (!interstitialAdUnitId.StartsWith(publisher + "/", StringComparison.Ordinal) ||
                !rewardedAdUnitId.StartsWith(publisher + "/", StringComparison.Ordinal))) || interstitialAdUnitId == rewardedAdUnitId)
                throw new ArgumentException("AdMob placements must be distinct and belong to the configured publisher.");
            // Compare the numeric publisher; production carries no sample ad-unit strings.
            var testPublisher = long.Parse(publisher.Substring(11), System.Globalization.CultureInfo.InvariantCulture) == 3940256099942544L;
            if (!TestAds && testPublisher)
                throw new ArgumentException("Production rejects Google test identifiers.");
#if UNITY_EDITOR || PAWS_MONETIZATION_VALIDATION
            if (TestAds && (interstitialAdUnitId != "ca-app-pub-3940256099942544/4411468910" ||
                rewardedAdUnitId != "ca-app-pub-3940256099942544/1712485313"))
                throw new ArgumentException("Validation requires official Google test placements.");
#endif
        }
        private static void ValidateLink(string url, bool required)
        {
            if (!required && string.IsNullOrEmpty(url)) return;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps ||
                string.IsNullOrEmpty(parsed.Host) || !string.IsNullOrEmpty(parsed.UserInfo))
                throw new ArgumentException("Owner-provided HTTPS privacy policy and support URLs are required.");
        }
    }
}
