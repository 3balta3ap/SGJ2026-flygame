using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FlyJam
{
    public enum GameLanguage { English = 0, Japanese = 1 }

    /// Localized UI text. Strings live in the TextTable asset (Resources/TextTable), filled from the "Text" sheet tab.
    /// Usage: Loc.Get("result.hit"), Loc.Format("court.ready", 2). A missing key shows as [key].
    /// The language is saved in PlayerPrefs. First launch follows the OS language (Japanese -> ja, otherwise en).
    public static class Loc
    {
        const string PrefsKey = "FlyJam.Language";
        const string TableResource = "TextTable";

        static TextTable table;
        static Dictionary<string, TextTable.Entry> map;
        static bool hasLanguage;
        static GameLanguage language;
        static readonly HashSet<string> warned = new HashSet<string>();

        /// Raised when the language changes or the text table is pulled again. Unsubscribe in OnDisable.
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            table = null;
            map = null;
            hasLanguage = false;
            warned.Clear();
            Changed = null;
        }

        public static GameLanguage Language
        {
            get
            {
                if (!hasLanguage) { language = LoadSaved(); hasLanguage = true; }
                return language;
            }
            set
            {
                if (hasLanguage && language == value) return;
                language = value;
                hasLanguage = true;
                PlayerPrefs.SetInt(PrefsKey, (int)value);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static void Toggle() =>
            Language = Language == GameLanguage.English ? GameLanguage.Japanese : GameLanguage.English;

        public static bool Has(string key) => !string.IsNullOrEmpty(key) && Map().ContainsKey(key);

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (!Map().TryGetValue(key, out var e))
            {
                Warn(key, "missing key");
                return "[" + key + "]";
            }
            string s = Language == GameLanguage.Japanese ? e.ja : e.en;
            return string.IsNullOrEmpty(s) ? e.en : s;   // empty Japanese cell falls back to English
        }

        /// Get + string.Format. Placeholders like {0} or {0:0.00} come from the sheet.
        public static string Format(string key, params object[] args)
        {
            string f = Get(key);
            try { return string.Format(CultureInfo.InvariantCulture, f, args); }
            catch (FormatException)
            {
                Warn(key, "bad {0} placeholder in");
                return f;
            }
        }

        /// Called by TextTable after a sheet Pull so open screens refresh.
        public static void Reload()
        {
            map = null;
            warned.Clear();
            Changed?.Invoke();
        }

        static Dictionary<string, TextTable.Entry> Map()
        {
            if (map != null) return map;
            map = new Dictionary<string, TextTable.Entry>();
            if (table == null) table = Resources.Load<TextTable>(TableResource);
            if (table == null)
            {
                Debug.LogWarning("[Loc] Resources/TextTable not found - all texts show as [key]");
                return map;
            }
            foreach (var e in table.Entries)
                if (!string.IsNullOrEmpty(e.key)) map[e.key.Trim()] = e;
            return map;
        }

        static GameLanguage LoadSaved()
        {
            if (PlayerPrefs.HasKey(PrefsKey))
                return PlayerPrefs.GetInt(PrefsKey) == (int)GameLanguage.Japanese ? GameLanguage.Japanese : GameLanguage.English;
            return Application.systemLanguage == SystemLanguage.Japanese ? GameLanguage.Japanese : GameLanguage.English;
        }

        static void Warn(string key, string what)
        {
            if (warned.Add(key)) Debug.LogWarning($"[Loc] {what} '{key}'");
        }
    }
}
