using System.Globalization;
using CatsAndArrows.Gameplay;
using UnityEngine;

namespace CatsAndArrows.UI
{
    public sealed class DifficultyHistoryPreference : IDifficultyHistoryStore
    {
        public const string Key = "cats-arrows.difficulty-history.v1";
        public DifficultyHistory Load()
        {
            var fields = PlayerPrefs.GetString(Key, "").Split('|');
            if (fields.Length != 3 || !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var version) ||
                !int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)) return null;
            return new DifficultyHistory(fields[1], count, version);
        }
        public bool TrySave(DifficultyHistory history)
        {
            var existed = PlayerPrefs.HasKey(Key);
            var previous = PlayerPrefs.GetString(Key);
            try
            {
                PlayerPrefs.SetString(Key, history.Version.ToString(CultureInfo.InvariantCulture) + "|" + history.LastDifficulty + "|" +
                    history.ConsecutiveStarts.ToString(CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
                return true;
            }
            catch (PlayerPrefsException)
            {
                // Keep the in-memory preference at its previous committed value
                // too, so reconstructing the shell cannot consume a failed start.
                try
                {
                    if (existed) PlayerPrefs.SetString(Key, previous); else PlayerPrefs.DeleteKey(Key);
                    PlayerPrefs.Save();
                }
                catch (PlayerPrefsException) { }
                return false;
            }
        }
    }
}
