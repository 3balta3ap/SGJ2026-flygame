using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace FlyJam.EditorTools
{
    /// Planner tool. Paste the Google Sheets link once, then press Pull to copy sheet values into balance assets.
    /// Menu: FlyJam/Balance Sync
    public class BalanceSyncWindow : EditorWindow
    {
        const string SettingsPath = "Assets/_Project/Data/Balance/BalanceSyncSettings.asset";
        const string SearchRoot = "Assets/_Project";

        static readonly Regex IdInUrl = new Regex(@"/spreadsheets/d/([A-Za-z0-9_-]{20,})");
        static readonly Regex BareId = new Regex(@"^[A-Za-z0-9_-]{20,}$");

        BalanceSyncSettings settings;
        List<BalanceTable> tables;
        UnityWebRequest request;
        readonly List<string> log = new List<string>();
        Vector2 tableScroll, logScroll;

        [MenuItem("FlyJam/Balance Sync")]
        static void Open() => GetWindow<BalanceSyncWindow>("Balance Sync");

        void OnFocus() => tables = null;
        void OnProjectChange() { tables = null; Repaint(); }

        void OnDisable()
        {
            EditorApplication.update -= Poll;
            request?.Dispose();
            request = null;
        }

        void OnGUI()
        {
            if (settings == null) settings = LoadOrCreateSettings();
            if (tables == null) tables = FindTables();

            // --- Link ---
            EditorGUILayout.LabelField("Google Sheets link", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Share the sheet as 'Anyone with the link: Viewer'. Use the normal link from the Share button. " +
                                    "The link is saved in the project, so commit it once.", MessageType.None);

            EditorGUI.BeginChangeCheck();
            string url = EditorGUILayout.TextField(settings.sheetUrl ?? "");
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Change Sheet Link");
                settings.sheetUrl = url.Trim();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }

            string id = SheetId(settings.sheetUrl);
            if (!string.IsNullOrEmpty(settings.sheetUrl) && id == null)
                EditorGUILayout.HelpBox("This does not look like a Google Sheets link (…/spreadsheets/d/<id>/edit).", MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(id == null || request != null))
                    if (GUILayout.Button(request != null ? "Pulling..." : "Pull", GUILayout.Height(28))) StartPull(id);
                using (new EditorGUI.DisabledScope(id == null))
                    if (GUILayout.Button("Open Sheet", GUILayout.Width(110), GUILayout.Height(28))) Application.OpenURL(settings.sheetUrl);
            }

            // --- Tables ---
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Balance tables in project ({tables.Count})", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Copy (TSV) puts the current values on the clipboard. " +
                                    "Make a tab with the same name in the sheet and paste at cell A1.", MessageType.None);

            tableScroll = EditorGUILayout.BeginScrollView(tableScroll, GUILayout.MaxHeight(160));
            foreach (var t in tables)
            {
                if (t == null) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(t.name, EditorStyles.linkLabel)) { Selection.activeObject = t; EditorGUIUtility.PingObject(t); }
                    GUILayout.FlexibleSpace();
                    if (!string.IsNullOrEmpty(t.SheetName) && GUILayout.Button($"Copy '{t.SheetName}' (TSV)", GUILayout.Width(190)))
                        Copy(BalanceApplier.KeyValueTsv(t), t.SheetName);
                    foreach (var (tab, field) in BalanceApplier.RowTabs(t))
                        if (GUILayout.Button($"Copy '{tab}' (TSV)", GUILayout.Width(190)))
                            Copy(BalanceApplier.RowsTsv(t, field), tab);
                }
            }
            EditorGUILayout.EndScrollView();

            // --- Log ---
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.ExpandHeight(true));
            foreach (var line in log)
                EditorGUILayout.SelectableLabel(line, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndScrollView();
        }

        // ---------- Pull ----------

        void StartPull(string id)
        {
            log.Clear();
            log.Add($"Downloading sheet {id} ...");
            request = UnityWebRequest.Get($"https://docs.google.com/spreadsheets/d/{id}/export?format=xlsx");
            request.SendWebRequest();
            EditorApplication.update += Poll;
        }

        void Poll()
        {
            if (request == null) { EditorApplication.update -= Poll; return; }
            if (!request.isDone) return;

            EditorApplication.update -= Poll;
            var req = request;
            request = null;
            try
            {
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Fail($"Download failed: {req.error} (HTTP {req.responseCode}). Check the link and your network.");
                    return;
                }
                byte[] data = req.downloadHandler.data;
                if (!XlsxReader.LooksLikeXlsx(data))
                {
                    Fail("Got a sign-in page instead of the sheet. Set sharing to 'Anyone with the link: Viewer'.");
                    return;
                }
                ApplyAll(XlsxReader.Read(data));
            }
            catch (Exception e)
            {
                Fail("Pull failed: " + e.Message);
            }
            finally
            {
                req.Dispose();
                Repaint();
            }
        }

        void ApplyAll(Dictionary<string, List<string[]>> tabs)
        {
            log.Add($"Tabs in sheet: {string.Join(", ", tabs.Keys)}");
            tables = FindTables();
            if (tables.Count == 0) { log.Add("No balance tables in the project yet."); return; }

            Undo.RecordObjects(tables.ToArray(), "Pull Balance Sheet");
            int total = 0;
            foreach (var t in tables)
            {
                total += BalanceApplier.Apply(t, tabs, log);
                t.OnPulled();
                EditorUtility.SetDirty(t);
            }
            AssetDatabase.SaveAssets();

            log.Add($"Done: {total} change(s). Ctrl+Z to undo.");
            Debug.Log($"[BalanceSync] Pull done: {total} change(s).");
        }

        void Fail(string message)
        {
            log.Add("ERROR " + message);
            Debug.LogWarning("[BalanceSync] " + message);   // warning, not error: a bad link must not break the build check
        }

        void Copy(string tsv, string tab)
        {
            EditorGUIUtility.systemCopyBuffer = tsv;
            ShowNotification(new GUIContent($"Copied '{tab}'"));
        }

        // ---------- Helpers ----------

        public static string SheetId(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var m = IdInUrl.Match(s);
            if (m.Success) return m.Groups[1].Value;
            s = s.Trim();
            return BareId.IsMatch(s) ? s : null;
        }

        static List<BalanceTable> FindTables()
        {
            var list = new List<BalanceTable>();
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { SearchRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) is BalanceTable t) list.Add(t);
            }
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list;
        }

        static BalanceSyncSettings LoadOrCreateSettings()
        {
            var s = AssetDatabase.LoadAssetAtPath<BalanceSyncSettings>(SettingsPath);
            if (s != null) return s;
            EnsureFolder(Path.GetDirectoryName(SettingsPath).Replace('\\', '/'));
            s = CreateInstance<BalanceSyncSettings>();
            AssetDatabase.CreateAsset(s, SettingsPath);
            AssetDatabase.SaveAssets();
            return s;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
