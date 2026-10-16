using UnityEngine;

namespace CatsAndArrows.UI
{
    public static class FirstPlayPreference
    {
        public const string Key = "cats-arrows.first-play.v1";

        // Freeze legacy migration before manual Tutorial can change its old seen
        // record. A fresh install records false; legacy returning players keep true.
        public static void Initialize()
        {
            if (PlayerPrefs.HasKey(Key)) return;
            PlayerPrefs.SetInt(Key, TutorialPreference.HasSeen() ? 1 : 0);
            PlayerPrefs.Save();
        }
        public static bool HasPlayed() { Initialize(); return PlayerPrefs.GetInt(Key, 0) == 1; }
        public static void MarkPlayed()
        {
            PlayerPrefs.SetInt(Key, 1);
            // Retain onboarding history when rolling back to the legacy shell.
            PlayerPrefs.SetInt(TutorialPreference.Key, 1);
            PlayerPrefs.Save();
        }
    }
}
