using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CatsAndArrows.Core.Localization
{
    /// <summary>Issue #3: immutable text resources, independent of puzzle truth and Unity.</summary>
    public sealed class LocaleResource
    {
        public string Locale { get; }
        public CultureInfo Culture { get; }
        public IReadOnlyDictionary<string, string> Messages { get; }

        public LocaleResource(string locale, IDictionary<string, string> messages)
        {
            if (string.IsNullOrWhiteSpace(locale)) throw new ArgumentException("A locale is required.", nameof(locale));
            if (messages == null) throw new ArgumentNullException(nameof(messages));
            Culture = CultureInfo.GetCultureInfo(locale);
            Locale = Culture.Name;
            var copy = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var message in messages)
            {
                if (string.IsNullOrWhiteSpace(message.Key) || message.Value == null)
                    throw new ArgumentException("Message keys and text must be defined.", nameof(messages));
                copy.Add(message.Key, message.Value);
            }
            Messages = new ReadOnlyDictionary<string, string>(copy);
        }
    }

    public sealed class LocalizationCatalog
    {
        private readonly Dictionary<string, LocaleResource> resources;
        public IReadOnlyList<string> AvailableLocales { get; }

        public LocalizationCatalog(IEnumerable<LocaleResource> locales)
        {
            if (locales == null) throw new ArgumentNullException(nameof(locales));
            resources = new Dictionary<string, LocaleResource>(StringComparer.OrdinalIgnoreCase);
            foreach (var resource in locales)
            {
                if (resource == null) throw new ArgumentException("A locale resource cannot be null.", nameof(locales));
                resources.Add(resource.Locale, resource);
            }
            if (!resources.ContainsKey("en")) throw new ArgumentException("English fallback resources are required.", nameof(locales));
            var names = new List<string>(resources.Keys);
            names.Sort(StringComparer.Ordinal);
            AvailableLocales = names.AsReadOnly();
        }

        public string ResolveLocale(string requestedLocale)
        {
            if (string.IsNullOrWhiteSpace(requestedLocale)) return "en";
            CultureInfo requested;
            try { requested = CultureInfo.GetCultureInfo(requestedLocale); }
            catch (CultureNotFoundException) { return "en"; }
            // Only a registered neutral language can absorb its regional variants.
            // pt-PT therefore never silently selects Brazilian Portuguese.
            for (var culture = requested; !string.IsNullOrEmpty(culture.Name); culture = culture.Parent)
                if (resources.ContainsKey(culture.Name)) return resources[culture.Name].Locale;
            return "en";
        }

        internal LocaleResource Resource(string locale) => resources[locale];
        public LocaleResource GetResource(string requestedLocale) => Resource(ResolveLocale(requestedLocale));
    }

    /// <summary>One instance per presentation context. Never modifies process-wide culture.</summary>
    public sealed class LocalizationService
    {
        private static readonly Regex Placeholder = new Regex(@"\{(?<name>[A-Za-z][A-Za-z0-9_]*)(:(?<format>[^{}]+))?\}");
        private readonly LocalizationCatalog catalog;
        public string SelectedLocale { get; private set; }
        public CultureInfo Culture => catalog.Resource(SelectedLocale).Culture;

        public LocalizationService(LocalizationCatalog catalog, CultureInfo deviceCulture = null)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            UseDeviceLanguage(deviceCulture);
        }

        public void UseDeviceLanguage(CultureInfo deviceCulture = null)
        {
            SelectLanguage((deviceCulture ?? CultureInfo.CurrentUICulture).Name);
        }

        public void SelectLanguage(string locale) { SelectedLocale = catalog.ResolveLocale(locale); }

        public string GetText(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            string message;
            if (catalog.Resource(SelectedLocale).Messages.TryGetValue(key, out message)) return message;
            if (catalog.Resource("en").Messages.TryGetValue(key, out message)) return message;
            throw new KeyNotFoundException("Unknown localization key: " + key);
        }

        /// <summary>Named values allow translators to reorder phrases. Optional formats use the selected locale.</summary>
        public string Format(string key, IReadOnlyDictionary<string, object> values)
        {
            return Placeholder.Replace(GetText(key), match =>
            {
                var name = match.Groups["name"].Value;
                object value;
                if (values == null || !values.TryGetValue(name, out value))
                    throw new ArgumentException("Missing localization value: " + name, nameof(values));
                var formattable = value as IFormattable;
                return formattable != null
                    ? formattable.ToString(match.Groups["format"].Success ? match.Groups["format"].Value : null, Culture)
                    : Convert.ToString(value, Culture);
            });
        }

        public string FormatNumber(decimal value, string format = "N0") => value.ToString(format, Culture);
    }
}
