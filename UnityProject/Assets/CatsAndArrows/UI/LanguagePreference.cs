using System.Globalization;
using UnityEngine;

namespace CatsAndArrows.UI
{
    // MVP preferences only. The current puzzle is intentionally not persisted.
    public static class LanguagePreference
    {
        public const string Key = "cats-arrows.language";
        public static string Load(string deviceLocale = null) => GameLocalization.CreateCatalog().ResolveLocale(
            PlayerPrefs.GetString(Key, deviceLocale ?? CultureInfo.CurrentUICulture.Name));
        public static void Save(string locale)
        {
            PlayerPrefs.SetString(Key, GameLocalization.CreateCatalog().ResolveLocale(locale));
            PlayerPrefs.Save();
        }
    }
}
