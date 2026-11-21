using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// All UI strings as "key | en | ja" rows. Lives in a Resources folder so Loc can load it from any scene.
    /// Filled from the "Text" sheet tab by FlyJam > Balance Sync (row table, no key/value tab).
    public class TextTable : BalanceTable
    {
        [Serializable]
        public struct Entry
        {
            public string key;
            public string en;
            public string ja;
        }

        public override string SheetName => null;

        [SheetRows("Text")]
        [SerializeField] Entry[] entries = Array.Empty<Entry>();

        public IReadOnlyList<Entry> Entries => entries;

        public override void OnPulled() => Loc.Reload();

#if UNITY_EDITOR
        /// Replaces all entries from TSV text (row 1 must contain "key", "en", "ja"). Used to seed the asset.
        public int EditorImportTsv(string tsv)
        {
            var list = new List<Entry>();
            int ki = -1, ei = -1, ji = -1;
            foreach (var raw in (tsv ?? "").Replace("\r", "").Split('\n'))
            {
                if (raw.Trim().Length == 0) continue;
                var c = raw.Split('\t');
                if (ki < 0)
                {
                    for (int i = 0; i < c.Length; i++)
                    {
                        string h = c[i].Trim().ToLowerInvariant();
                        if (h == "key") ki = i;
                        else if (h == "en") ei = i;
                        else if (h == "ja") ji = i;
                    }
                    if (ki < 0) { Debug.LogError("[TextTable] TSV row 1 must have a 'key' column"); return 0; }
                    continue;
                }
                string key = Cell(c, ki).Trim();
                if (key.Length == 0 || key.StartsWith("#")) continue;
                list.Add(new Entry { key = key, en = Cell(c, ei), ja = Cell(c, ji) });
            }
            UnityEditor.Undo.RecordObject(this, "Import Text TSV");
            entries = list.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
            return entries.Length;
        }

        [ContextMenu("Import TSV from Clipboard")]
        void ImportFromClipboard() =>
            Debug.Log($"[TextTable] imported {EditorImportTsv(GUIUtility.systemCopyBuffer)} entries");

        static string Cell(string[] c, int i) => i >= 0 && i < c.Length ? c[i] : "";
#endif
    }
}
