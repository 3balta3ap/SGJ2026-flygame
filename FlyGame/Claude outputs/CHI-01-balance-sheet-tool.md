# CHI/01-balance-sheet-tool: 구글 시트 수치 동기화 툴

| 항목 | 값 |
|---|---|
| 브랜치 | `CHI/01-balance-sheet-tool` (`dev`에서 생성) |
| 선행 브랜치 | 없음 |
| 권장 커밋 메시지 | `Add Google Sheets balance sync tool` |
| 건드리지 않는 것 | 기존 게임 스크립트 전부, 모든 씬, 모든 `SYS_*` 프리팹 |

> **에이전트 작업 규칙** (AGENTS.md 4장과 같음): 스펙 코드를 그대로 쓰고, 고쳤다면 무엇을 왜 고쳤는지 보고한다.
> Console 에러 0개 확인. **git 명령(branch, add, commit, push)은 실행하지 않는다.** 커밋은 사람이 한다.

---

## 0. 목표와 원칙

**목표**: 기획자가 구글 시트에서 숫자를 고친 뒤 Unity에서 버튼 하나(**Pull**)를 누르면, 그 값이 게임의 수치 에셋(ScriptableObject)에 들어간다.

**이 브랜치에서 만드는 것**: 툴과 데이터 틀만. 실제 게임 수치 테이블(HP, 적 등)은 03번 이후 브랜치가 각자 하나씩 추가한다.

**원칙**
1. **링크만 있으면 된다.** 시트를 "링크가 있는 모든 사용자: 뷰어"로 공유하고 링크를 붙이면 끝. 로그인·API 키 없음.
2. **외부 패키지·DLL 없음.** 시트 전체를 `.xlsx`로 한 번에 내려받고(`/export?format=xlsx`), .NET 기본 `ZipArchive`와 `XmlDocument`로 직접 읽는다.
   - 탭 이름으로 정확히 찾고, 없는 탭은 "없음"으로 알려 준다.
   - 숫자·글자가 한 열에 섞여도 값이 깨지지 않는다.
3. **이름으로 맞춘다.** 시트의 `key` = 코드의 필드 이름. 새 수치를 추가할 때 툴 코드는 고치지 않는다(필드 하나 추가 + 시트에 한 줄 추가).
4. **수치는 코드가 기본값을 갖는다.** 시트에 없는 키는 지금 값을 그대로 둔다. 시트가 망가져도 게임이 멈추지 않는다.
5. **되돌릴 수 있다.** Pull은 Undo에 기록된다(Ctrl+Z). 어떤 값이 바뀌었는지 로그에 `old -> new`로 남는다.

**구조**

```
Google Sheets (탭: Example, ExampleRows, 이후 Health, Enemy …)
      │  export?format=xlsx (링크 공유만으로 다운로드)
      ▼
BalanceSyncWindow (FlyJam → Balance Sync)   ← Editor 전용
      │  XlsxReader: 탭 이름 → 행 목록
      │  BalanceApplier: 필드 이름으로 값 대입 (리플렉션)
      ▼
BalanceTable 에셋들 (Assets/_Project 아래 어디든)  ← 런타임(Core). 각 기능이 인스펙터로 참조
```

---

## 1. 파일 변경 요약

| 파일 | 구분 | 어셈블리 |
|---|---|---|
| `Scripts/Core/Balance/BalanceTable.cs` | 신규 | FlyJam.Core |
| `Scripts/Core/Balance/SheetRowsAttribute.cs` | 신규 | FlyJam.Core |
| `Scripts/Editor/FlyJam.Editor.asmdef` | 신규 | FlyJam.Editor (에디터 전용) |
| `Scripts/Editor/Balance/XlsxReader.cs` | 신규 | FlyJam.Editor |
| `Scripts/Editor/Balance/BalanceApplier.cs` | 신규 | FlyJam.Editor |
| `Scripts/Editor/Balance/BalanceSyncSettings.cs` | 신규 | FlyJam.Editor |
| `Scripts/Editor/Balance/BalanceSyncWindow.cs` | 신규 | FlyJam.Editor |
| `Scripts/Editor/Balance/ExampleBalance.cs` | 신규 (툴 자체 테스트용) | FlyJam.Editor |
| `Assets/_Project/Data/Balance/BalanceSyncSettings.asset` | 신규 (창을 처음 열면 자동 생성) | |
| `Assets/_Project/Data/Balance/_Example/ExampleBalance.asset` | 신규 | |
| `AGENTS.md` | 2.2절 한 줄 수정 + 8장 추가 | |

`Scripts/` = `Assets/_Project/Scripts/`. 폴더 `Core/Balance`, `Editor/Balance`, `Data/Balance/_Example`는 새로 만든다.

---

## 2. Core (런타임)

### 2-1. Scripts/Core/Balance/BalanceTable.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Base class for planner-editable numbers, filled from Google Sheets by FlyJam > Balance Sync.
    ///
    /// Key/value tab (SheetName): row 1 = "key | value | note", then one row per field.
    ///   key = field name of the subclass (public, or private with [SerializeField]).
    /// Row table tab: mark an array/List field with [SheetRows("TabName")].
    ///   row 1 = column names (= fields of the element type), then one row per element.
    /// Rows whose first cell starts with '#' are comments. Keys missing in the sheet keep their current value.
    ///
    /// Features reference their own BalanceTable asset through a [SerializeField] and read it at runtime.
    public abstract class BalanceTable : ScriptableObject
    {
        /// Google Sheets tab for key/value rows. Return null if this table only uses [SheetRows] tabs.
        public abstract string SheetName { get; }

        /// Optional: read the SheetName tab yourself (e.g., localization with "key | en | ja").
        /// Return true when handled; the default key/value import is then skipped.
        public virtual bool ImportCustom(List<string[]> rows, List<string> log) => false;

        /// Optional: write a custom tab back as TSV for the "Copy (TSV)" button. Return null to use the default.
        public virtual string ExportCustomTsv() => null;

        /// Called after a Pull. Override to clamp values or rebuild caches.
        public virtual void OnPulled() { }
    }
}
```

### 2-2. Scripts/Core/Balance/SheetRowsAttribute.cs

```csharp
using System;

namespace FlyJam
{
    /// Marks an array or List<T> field of a BalanceTable as a row table filled from the given sheet tab.
    /// T must be [Serializable] with a parameterless constructor (a struct is easiest).
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SheetRowsAttribute : Attribute
    {
        public string Tab { get; }
        public SheetRowsAttribute(string tab) { Tab = tab; }
    }
}
```

---

## 3. Editor (에디터 전용)

### 3-1. Scripts/Editor/FlyJam.Editor.asmdef

```json
{
    "name": "FlyJam.Editor",
    "rootNamespace": "FlyJam.EditorTools",
    "references": [
        "FlyJam.Core"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "autoReferenced": false
}
```

### 3-2. Scripts/Editor/Balance/XlsxReader.cs

```csharp
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace FlyJam.EditorTools
{
    /// Minimal .xlsx reader for Google Sheets exports. No external DLL.
    /// Returns tab name -> rows; each row is the cell text by column (A = index 0).
    /// Formulas are read as their last calculated value.
    public static class XlsxReader
    {
        const string MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        const string DocRelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        const string PkgRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        /// An .xlsx file is a zip and starts with "PK". A Google sign-in HTML page does not.
        public static bool LooksLikeXlsx(byte[] data) =>
            data != null && data.Length > 4 && data[0] == (byte)'P' && data[1] == (byte)'K';

        public static Dictionary<string, List<string[]>> Read(byte[] data)
        {
            var tabs = new Dictionary<string, List<string[]>>();
            using (var zip = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read))
            {
                var shared = ReadSharedStrings(zip);
                var targets = ReadWorkbookRels(zip);
                var wb = Load(zip, "xl/workbook.xml");
                if (wb == null) return tabs;

                foreach (XmlNode n in wb.GetElementsByTagName("sheet", MainNs))
                {
                    var sheet = (XmlElement)n;
                    string name = sheet.GetAttribute("name");
                    string rid = sheet.GetAttribute("id", DocRelNs);
                    if (!targets.TryGetValue(rid, out string target)) continue;

                    string path = target.StartsWith("/") ? target.Substring(1) : "xl/" + target;
                    var doc = Load(zip, path);
                    if (doc != null) tabs[name] = ReadSheet(doc, shared);
                }
            }
            return tabs;
        }

        static XmlDocument Load(ZipArchive zip, string path)
        {
            var entry = zip.GetEntry(path);
            if (entry == null) return null;
            var doc = new XmlDocument();
            using (var s = entry.Open()) doc.Load(s);
            return doc;
        }

        static Dictionary<string, string> ReadWorkbookRels(ZipArchive zip)
        {
            var map = new Dictionary<string, string>();
            var doc = Load(zip, "xl/_rels/workbook.xml.rels");
            if (doc == null) return map;
            foreach (XmlNode n in doc.GetElementsByTagName("Relationship", PkgRelNs))
            {
                var e = (XmlElement)n;
                map[e.GetAttribute("Id")] = e.GetAttribute("Target");
            }
            return map;
        }

        static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var list = new List<string>();
            var doc = Load(zip, "xl/sharedStrings.xml");
            if (doc == null) return list;
            foreach (XmlNode si in doc.GetElementsByTagName("si", MainNs)) list.Add(TextOf(si));
            return list;
        }

        /// Joins the <t> text of a string item. Skips phonetic runs (<rPh>, Japanese furigana).
        static string TextOf(XmlNode node)
        {
            var sb = new StringBuilder();
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.LocalName == "t") sb.Append(child.InnerText);
                else if (child.LocalName == "r")
                    foreach (XmlNode t in child.ChildNodes)
                        if (t.LocalName == "t") sb.Append(t.InnerText);
            }
            return sb.ToString();
        }

        static List<string[]> ReadSheet(XmlDocument doc, List<string> shared)
        {
            var rows = new List<string[]>();
            foreach (XmlNode rowNode in doc.GetElementsByTagName("row", MainNs))
            {
                var cells = new Dictionary<int, string>();
                int next = 0, max = -1;
                foreach (XmlNode cn in rowNode.ChildNodes)
                {
                    if (cn.LocalName != "c") continue;
                    var c = (XmlElement)cn;
                    int col = ColumnIndex(c.GetAttribute("r"));
                    if (col < 0) col = next;
                    cells[col] = CellValue(c, shared);
                    next = col + 1;
                    if (col > max) max = col;
                }
                var arr = new string[max + 1];
                for (int i = 0; i < arr.Length; i++) arr[i] = cells.TryGetValue(i, out var s) ? s : "";
                rows.Add(arr);
            }
            return rows;
        }

        static string CellValue(XmlElement c, List<string> shared)
        {
            string type = c.GetAttribute("t");
            if (type == "inlineStr")
            {
                foreach (XmlNode child in c.ChildNodes)
                    if (child.LocalName == "is") return TextOf(child);
                return "";
            }

            string v = null;
            foreach (XmlNode child in c.ChildNodes)
                if (child.LocalName == "v") { v = child.InnerText; break; }
            if (v == null) return "";

            if (type == "s")
                return int.TryParse(v, out int i) && i >= 0 && i < shared.Count ? shared[i] : "";
            return v;   // number (no type / "n"), boolean ("b": 1/0), formula text ("str"), error ("e")
        }

        /// "B12" -> 1. Returns -1 if there is no column letter.
        static int ColumnIndex(string cellRef)
        {
            if (string.IsNullOrEmpty(cellRef)) return -1;
            int col = 0, i = 0;
            while (i < cellRef.Length && char.IsLetter(cellRef[i]))
            {
                col = col * 26 + (char.ToUpperInvariant(cellRef[i]) - 'A' + 1);
                i++;
            }
            return i == 0 ? -1 : col - 1;
        }
    }
}
```

### 3-3. Scripts/Editor/Balance/BalanceApplier.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace FlyJam.EditorTools
{
    /// Writes sheet rows into BalanceTable fields by name, and writes current values back as TSV.
    /// Supported field types: float, int, bool (1/0, TRUE/FALSE), string, Vector2 ("x, y"), enum (by name).
    public static class BalanceApplier
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ---------- Pull ----------

        /// Applies every tab this table uses. Returns the number of changed values.
        public static int Apply(BalanceTable table, Dictionary<string, List<string[]>> tabs, List<string> log)
        {
            int changed = 0;
            string tag = $"[{table.name}]";

            string main = table.SheetName;
            if (!string.IsNullOrEmpty(main))
            {
                if (!tabs.TryGetValue(main, out var rows))
                    log.Add($"{tag} WARNING tab '{main}' not found - kept current values");
                else if (table.ImportCustom(rows, log))
                    changed++;
                else
                    changed += ApplyKeyValues(table, rows, tag, log);
            }

            foreach (var (tab, field) in RowTabs(table))
            {
                if (!tabs.TryGetValue(tab, out var rows))
                {
                    log.Add($"{tag} WARNING tab '{tab}' not found - kept current rows");
                    continue;
                }
                changed += ApplyRows(table, field, rows, tag, tab, log);
            }
            return changed;
        }

        static int ApplyKeyValues(BalanceTable table, List<string[]> rows, string tag, List<string> log)
        {
            var fields = ValueFields(table.GetType()).ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int changed = 0;

            for (int r = 0; r < rows.Count; r++)
            {
                string key = Cell(rows[r], 0).Trim();
                if (key.Length == 0 || key.StartsWith("#")) continue;
                if (r == 0 && key.Equals("key", StringComparison.OrdinalIgnoreCase)) continue;   // header

                if (!fields.TryGetValue(key, out var f))
                {
                    log.Add($"{tag} WARNING unknown key '{key}' (typo, or not in code yet)");
                    continue;
                }
                seen.Add(f.Name);

                string raw = Cell(rows[r], 1);
                if (!TryParse(raw, f.FieldType, out object value))
                {
                    log.Add($"{tag} ERROR '{key}': cannot read '{raw}' as {f.FieldType.Name} - kept {Format(f.GetValue(table))}");
                    continue;
                }

                object old = f.GetValue(table);
                if (Equals(old, value)) continue;
                f.SetValue(table, value);
                log.Add($"{tag} {f.Name}: {Format(old)} -> {Format(value)}");
                changed++;
            }

            foreach (var f in fields.Values)
                if (!seen.Contains(f.Name))
                    log.Add($"{tag} note: '{f.Name}' is not in the sheet - kept {Format(f.GetValue(table))}");
            return changed;
        }

        static int ApplyRows(BalanceTable table, FieldInfo field, List<string[]> rows, string tag, string tab, List<string> log)
        {
            Type elem = ElementType(field.FieldType);
            if (elem == null)
            {
                log.Add($"{tag} ERROR [SheetRows] field '{field.Name}' must be an array or List<T>");
                return 0;
            }
            if (rows.Count == 0)
            {
                log.Add($"{tag} WARNING tab '{tab}' is empty - kept current rows");
                return 0;
            }

            // Row 1 = column names
            var elemFields = ElementFields(elem).ToList();
            var header = rows[0];
            var cols = new FieldInfo[header.Length];
            for (int c = 0; c < header.Length; c++)
            {
                string h = header[c].Trim();
                if (h.Length == 0 || h.StartsWith("#")) continue;
                cols[c] = elemFields.FirstOrDefault(x => x.Name.Equals(h, StringComparison.OrdinalIgnoreCase));
                if (cols[c] == null) log.Add($"{tag} WARNING {tab}: unknown column '{h}'");
            }

            var items = new List<object>();
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.All(string.IsNullOrWhiteSpace) || Cell(row, 0).TrimStart().StartsWith("#")) continue;

                object item = Activator.CreateInstance(elem);
                for (int c = 0; c < cols.Length; c++)
                {
                    if (cols[c] == null) continue;
                    string raw = Cell(row, c);
                    if (string.IsNullOrWhiteSpace(raw)) continue;   // empty cell = type default
                    if (TryParse(raw, cols[c].FieldType, out object v)) cols[c].SetValue(item, v);
                    else log.Add($"{tag} ERROR {tab} row {r + 1}: cannot read '{raw}' as {cols[c].FieldType.Name} ({cols[c].Name})");
                }
                items.Add(item);
            }

            object newValue;
            if (field.FieldType.IsArray)
            {
                var arr = Array.CreateInstance(elem, items.Count);
                for (int i = 0; i < items.Count; i++) arr.SetValue(items[i], i);
                newValue = arr;
            }
            else
            {
                var list = (IList)Activator.CreateInstance(field.FieldType);
                foreach (var it in items) list.Add(it);
                newValue = list;
            }

            object oldValue = field.GetValue(table);
            if (RowsText(oldValue, elemFields) == RowsText(newValue, elemFields)) return 0;
            field.SetValue(table, newValue);
            log.Add($"{tag} {tab}: {Count(oldValue)} rows -> {items.Count} rows (updated)");
            return 1;
        }

        // ---------- Copy (TSV) ----------

        /// Current key/value fields as TSV ("key, value, note"). Note = the field's [Tooltip].
        public static string KeyValueTsv(BalanceTable table)
        {
            string custom = table.ExportCustomTsv();
            if (custom != null) return custom;

            var sb = new StringBuilder("key\tvalue\tnote\n");
            foreach (var f in ValueFields(table.GetType()))
            {
                var tip = f.GetCustomAttribute<TooltipAttribute>();
                sb.Append(f.Name).Append('\t')
                  .Append(Format(f.GetValue(table))).Append('\t')
                  .Append(tip != null ? OneLine(tip.tooltip) : "").Append('\n');
            }
            return sb.ToString();
        }

        /// Current rows of a [SheetRows] field as TSV (row 1 = column names).
        public static string RowsTsv(BalanceTable table, FieldInfo field)
        {
            var elemFields = ElementFields(ElementType(field.FieldType)).ToList();
            var sb = new StringBuilder(string.Join("\t", elemFields.Select(f => f.Name))).Append('\n');
            if (field.GetValue(table) is IEnumerable items)
                foreach (var it in items)
                    sb.Append(string.Join("\t", elemFields.Select(f => Format(f.GetValue(it))))).Append('\n');
            return sb.ToString();
        }

        // ---------- Reflection helpers ----------

        public static IEnumerable<(string tab, FieldInfo field)> RowTabs(BalanceTable table)
        {
            foreach (var f in SerializedFields(table.GetType()))
            {
                var attr = f.GetCustomAttribute<SheetRowsAttribute>();
                if (attr != null) yield return (attr.Tab, f);
            }
        }

        static IEnumerable<FieldInfo> ValueFields(Type t) =>
            SerializedFields(t).Where(f => f.GetCustomAttribute<SheetRowsAttribute>() == null);

        /// Serialized instance fields of the subclass chain, stopping at BalanceTable.
        static IEnumerable<FieldInfo> SerializedFields(Type t)
        {
            var chain = new List<Type>();
            for (var x = t; x != null && x != typeof(BalanceTable); x = x.BaseType) chain.Insert(0, x);
            foreach (var x in chain)
                foreach (var f in x.GetFields(Flags))
                    if (IsSerialized(f)) yield return f;
        }

        static IEnumerable<FieldInfo> ElementFields(Type elem) =>
            elem.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(IsSerialized);

        static bool IsSerialized(FieldInfo f) =>
            !f.IsNotSerialized && !f.IsInitOnly &&
            (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null);

        static Type ElementType(Type t)
        {
            if (t.IsArray) return t.GetElementType();
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)) return t.GetGenericArguments()[0];
            return null;
        }

        // ---------- Text <-> value ----------

        public static bool TryParse(string s, Type t, out object value)
        {
            s = (s ?? "").Trim();
            value = null;
            if (t == typeof(string)) { value = s; return true; }
            if (s.Length == 0) return false;

            if (t == typeof(float))
            {
                if (float.TryParse(s, NumberStyles.Float, Inv, out var f)) { value = f; return true; }
                return false;
            }
            if (t == typeof(int))
            {
                if (int.TryParse(s, NumberStyles.Integer, Inv, out var i)) { value = i; return true; }
                if (double.TryParse(s, NumberStyles.Float, Inv, out var d) && Math.Abs(d - Math.Round(d)) < 1e-6)
                { value = (int)Math.Round(d); return true; }
                return false;
            }
            if (t == typeof(bool))
            {
                switch (s.ToLowerInvariant())
                {
                    case "1": case "true": case "yes": value = true; return true;
                    case "0": case "false": case "no": value = false; return true;
                }
                return false;
            }
            if (t == typeof(Vector2))
            {
                var p = s.Split(',', ';');
                if (p.Length == 2 &&
                    float.TryParse(p[0].Trim(), NumberStyles.Float, Inv, out var x) &&
                    float.TryParse(p[1].Trim(), NumberStyles.Float, Inv, out var y))
                { value = new Vector2(x, y); return true; }
                return false;
            }
            if (t.IsEnum)
            {
                try { value = Enum.Parse(t, s, true); return true; }
                catch { return false; }
            }
            return false;
        }

        public static string Format(object v)
        {
            switch (v)
            {
                case null: return "";
                case float f: return f.ToString(Inv);
                case bool b: return b ? "1" : "0";
                case Vector2 p: return p.x.ToString(Inv) + ", " + p.y.ToString(Inv);
                case IFormattable fm: return fm.ToString(null, Inv);
                default: return v.ToString();
            }
        }

        static string Cell(string[] row, int i) => i < row.Length ? row[i] ?? "" : "";
        static string OneLine(string s) => (s ?? "").Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
        static int Count(object list) => list is ICollection c ? c.Count : 0;

        static string RowsText(object list, List<FieldInfo> fields)
        {
            if (!(list is IEnumerable items)) return "";
            var sb = new StringBuilder();
            foreach (var it in items)
                sb.Append(string.Join("\t", fields.Select(f => Format(f.GetValue(it))))).Append('\n');
            return sb.ToString();
        }
    }
}
```

### 3-4. Scripts/Editor/Balance/BalanceSyncSettings.cs

```csharp
using UnityEngine;

namespace FlyJam.EditorTools
{
    /// Stores the shared Google Sheets link. Committed to git so everyone pulls from the same sheet.
    public class BalanceSyncSettings : ScriptableObject
    {
        public string sheetUrl = "";
    }
}
```

### 3-5. Scripts/Editor/Balance/BalanceSyncWindow.cs

```csharp
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
```

### 3-6. Scripts/Editor/Balance/ExampleBalance.cs (툴 자체 테스트용)

```csharp
using UnityEngine;

namespace FlyJam.EditorTools
{
    /// Self-test table for the Balance Sync tool (tabs "Example" and "ExampleRows").
    /// Editor-only and not used by the game. Safe to delete once real tables exist.
    public class ExampleBalance : BalanceTable
    {
        public override string SheetName => "Example";

        [Tooltip("float test")] [SerializeField] float speed = 1.5f;
        [Tooltip("int test")] [SerializeField] int count = 3;
        [Tooltip("bool test: 1 or 0")] [SerializeField] bool flag = true;
        [Tooltip("Vector2 test: x, y")] [SerializeField] Vector2 range = new Vector2(1f, 2f);
        [Tooltip("text test")] [SerializeField] string label = "hello";

        [System.Serializable]
        public struct Row
        {
            public int level;
            public float value;
        }

        [SheetRows("ExampleRows")]
        [SerializeField] Row[] rows =
        {
            new Row { level = 1, value = 10f },
            new Row { level = 2, value = 25f },
        };
    }
}
```

> `speed`, `count` 등이 "할당만 하고 안 씀" 경고(CS0414)를 내면, 클래스 맨 위에 `#pragma warning disable 0414`를 한 줄 추가하고 보고한다.

---

## 4. 에디터 작업

1. 컴파일 후 Console 에러 0개 확인.
   - `System.IO.Compression`을 못 찾는다는 에러가 나면 `Scripts/Editor/csc.rsp` 파일을 만들어 `-r:System.IO.Compression.dll` 한 줄을 넣고 보고한다.
2. 메뉴 **FlyJam → Balance Sync** 실행 → 창이 열리고 `Assets/_Project/Data/Balance/BalanceSyncSettings.asset`이 생긴다.
3. `Assets/_Project/Data/Balance/_Example/` 폴더를 만들고, 그 안에 `ExampleBalance` 에셋을 만든다.
   - 메뉴가 없으므로 `ScriptableObject.CreateInstance<FlyJam.EditorTools.ExampleBalance>()` → `AssetDatabase.CreateAsset(..., "Assets/_Project/Data/Balance/_Example/ExampleBalance.asset")`로 만든다.
   - `Unity_RunCommand`로 만들 때 `System.Reflection`을 쓰지 않는다(AGENTS.md 6장). 툴 내부의 리플렉션은 프로젝트 코드라 상관없다.
4. 창의 테이블 목록에 `ExampleBalance` 한 줄과 버튼 두 개(`Copy 'Example' (TSV)`, `Copy 'ExampleRows' (TSV)`)가 보이면 된다.
5. **시트 연결과 Pull은 사람이 한다** (6장 체크포인트). 에이전트는 여기까지 하고 보고한다.

---

## 5. AGENTS.md 수정

### 5-1. 2.2절 첫 줄 수정
`Assets/_Project/Scripts/` 아래 **10개** 어셈블리 → **11개** 어셈블리 (런타임 10 + 에디터 전용 `Editor/FlyJam.Editor.asmdef`: 참조 `FlyJam.Core`)

### 5-2. 파일 끝에 8장 추가

```markdown
---

## 8. 본 개발 협업 규칙 (dev 이후)

- **브랜치**: `dev`에서 따고 `dev`로 머지한다. 이름은 `CHI/NN-기능-이름` (예: `CHI/03-player-health`). 계획은 `specs/CHI-00-plan.md`.
- **스펙**: 브랜치 하나 = 스펙 하나(`specs/CHI-NN-*.md`). 스펙 범위 밖의 파일은 건드리지 않는다.
- **git**: 에이전트는 branch/add/commit/push를 실행하지 않는다. 사람이 스펙의 영어 커밋 메시지로 커밋한다.
- **수치(밸런스)**: 기획자가 바꿀 값은 코드에 박지 않는다.
  1. 기능 폴더에 `BalanceTable`을 상속한 클래스를 만든다 (`SheetName` = 시트 탭 이름, 필드 이름 = 시트 key, `[Tooltip]` = 시트 note).
  2. 표 형태 값은 배열/List 필드에 `[SheetRows("탭이름")]`.
  3. 에셋은 `Assets/_Project/Data/Balance/<Feature>Balance.asset`. 기능 컴포넌트가 `[SerializeField]`로 참조한다(`SYS_*` 프리팹 안에서).
  4. 사람이 `FlyJam → Balance Sync`의 `Copy (TSV)`로 시트에 탭을 만들고 `Pull`로 받아 온다.
- **Balance Sync 툴**: `Scripts/Editor/Balance/`. 시트 전체를 xlsx로 받아(`/export?format=xlsx`) 탭 이름과 필드 이름으로 맞춘다. 시트에 없는 키는 현재 값을 유지한다.
```

---

## 6. 체크포인트 (사람이 확인)

**준비 (한 번만)**
1. 구글 시트를 새로 만든다(예: `FlyGame Balance`). 공유 → **링크가 있는 모든 사용자 / 뷰어**.
2. Unity: **FlyJam → Balance Sync**. 시트 주소창의 링크를 그대로 붙여 넣는다. 아래에 경고가 안 뜨면 OK.
3. `Copy 'Example' (TSV)` → 시트에 탭을 추가해 이름을 **`Example`**로 바꾸고 A1에 붙여 넣는다.
4. `Copy 'ExampleRows' (TSV)` → 탭 **`ExampleRows`**를 만들고 A1에 붙여 넣는다.
   - 첫 탭(`시트1`)은 지워도 되고 그대로 둬도 된다(툴이 무시함).

**확인**

| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 아무것도 안 바꾸고 **Pull** | 로그에 `Tabs in sheet: …, Example, ExampleRows`, 마지막 줄 `Done: 0 change(s).` |
| 2 | `Example` 탭에서 B2(`speed`)를 `9.5`, B4(`flag`)를 `0`, B6(`label`)을 `こんにちは`로 바꾸고 Pull | 로그에 `speed: 1.5 -> 9.5`, `flag: 1 -> 0`, `label: hello -> こんにちは`. `ExampleBalance` 인스펙터 값도 바뀜 |
| 3 | `ExampleRows` 탭의 4행에 A4 `3`, B4 `60`을 넣고 Pull | `ExampleRows: 2 rows -> 3 rows (updated)`. 인스펙터 `Rows` 크기 3 |
| 4 | Unity 창을 클릭한 뒤 **Cmd+Z**(Win: Ctrl+Z) 한 번, 한 번 더 | 첫 번째: `Rows`가 2개로. 두 번째: `speed` 1.5, `flag` 체크, `label` hello로. (Undo는 Pull 한 번씩 되돌림. 시트 값은 그대로라 다음 Pull에서 다시 적용됨) |
| 5 | `Example` 탭 7행에 A7 `typo_key`, B7 `1`을 넣고 Pull (확인 후 7행 삭제) | `WARNING unknown key 'typo_key'`. 다른 값은 정상 적용 |
| 6 | B3(`count`)에 `abc`를 넣고 Pull (확인 후 `3`으로 되돌리기) | `ERROR 'count': cannot read 'abc' as Int32 - kept 3` |
| 7 | 탭 이름을 `Example2`로 바꾸고 Pull (확인 후 되돌리기) | `WARNING tab 'Example' not found - kept current values` |
| 8 | 시트 공유를 "제한됨"으로 바꾸고 Pull (확인 후 되돌리기) | `ERROR Got a sign-in page …` 또는 `ERROR Download failed …`. Console에는 노란 경고만, 빨간 에러 없음 |
| 9 | Play로 Title→Game→Result 한 바퀴 | 이전과 똑같이 동작. Console 에러 0 |

**커밋 전 확인**: `git status`에 아래만 있어야 한다.
- `Assets/_Project/Scripts/Core/Balance/*` (+ `.meta`)
- `Assets/_Project/Scripts/Editor/**` (+ `.meta`)
- `Assets/_Project/Data/**` (`BalanceSyncSettings.asset`에 시트 링크가 들어 있음)
- `AGENTS.md`

**커밋**
```bash
git add -A FlyGame
git commit -m "Add Google Sheets balance sync tool"
git push
```
PR: `CHI/01-balance-sheet-tool` → `dev`.
