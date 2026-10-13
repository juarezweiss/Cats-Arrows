using UnityEngine;

namespace CatsAndArrows.UI
{
    // Entry history only: lesson steps and board marks deliberately remain in memory.
    public static class TutorialPreference
    {
        public const string Key = "cats-arrows.tutorial-seen.v1";
        public static bool HasSeen() => PlayerPrefs.GetInt(Key, 0) == 1;
        public static void MarkSeen()
        {
            PlayerPrefs.SetInt(Key, 1);
            PlayerPrefs.Save();
        }
    }
}
