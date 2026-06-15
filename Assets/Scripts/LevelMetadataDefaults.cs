using UnityEngine;

namespace LevelMaker
{
    /// <summary>
    /// Central place to read/write user-supplied defaults for level metadata
    /// (default author name, common tags, etc.) so designers don't have to
    /// retype the same things for every save.
    /// </summary>
    public static class LevelMetadataDefaults
    {
        private const string PrefKeyAuthor = "LevelMaker.DefaultAuthor";

        public static string DefaultAuthor
        {
            get => PlayerPrefs.GetString(PrefKeyAuthor, "");
            set => PlayerPrefs.SetString(PrefKeyAuthor, value ?? "");
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(PrefKeyAuthor);
        }
    }
}
