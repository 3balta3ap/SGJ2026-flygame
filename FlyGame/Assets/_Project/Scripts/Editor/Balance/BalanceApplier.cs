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
    /// Supported field types: float, int, bool (1/0, TRUE/FALSE), string, enum (by name), Vector2.
    /// A Vector2 field uses two keys/columns, "name.x" and "name.y":
    /// Google Sheets turns text like "1, 2" into a date, so one cell per number is the only safe format.
    public static class BalanceApplier
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// One sheet cell <-> one value. A Vector2 field is split into two float slots.
        sealed class Slot
        {
            public string Key;
            public Type Type;
            public FieldInfo Field;
            public int Axis = -1;   // -1 = whole field, 0 = Vector2.x, 1 = Vector2.y

            public object Get(object owner)
            {
                object v = Field.GetValue(owner);
                if (Axis < 0) return v;
                var p = (Vector2)v;
                return Axis == 0 ? p.x : p.y;
            }

            /// 'owner' may be a boxed struct (row element); SetValue writes into the box.
            public void Set(object owner, object value)
            {
                if (Axis < 0) { Field.SetValue(owner, value); return; }
                var p = (Vector2)Field.GetValue(owner);
                if (Axis == 0) p.x = (float)value; else p.y = (float)value;
                Field.SetValue(owner, p);
            }
        }

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
            var slots = Slots(ValueFields(table.GetType())).ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int changed = 0;

            for (int r = 0; r < rows.Count; r++)
            {
                string key = Cell(rows[r], 0).Trim();
                if (key.Length == 0 || key.StartsWith("#")) continue;
                if (r == 0 && key.Equals("key", StringComparison.OrdinalIgnoreCase)) continue;   // header

                if (!slots.TryGetValue(key, out var slot))
                {
                    log.Add($"{tag} WARNING unknown key '{key}' (typo, or not in code yet)");
                    continue;
                }
                seen.Add(slot.Key);

                string raw = Cell(rows[r], 1);
                if (!TryParse(raw, slot.Type, out object value))
                {
                    log.Add($"{tag} ERROR '{key}': cannot read '{raw}' as {slot.Type.Name} - kept {Format(slot.Get(table))}");
                    continue;
                }

                object old = slot.Get(table);
                if (Equals(old, value)) continue;
                slot.Set(table, value);
                log.Add($"{tag} {slot.Key}: {Format(old)} -> {Format(value)}");
                changed++;
            }

            foreach (var s in slots.Values)
                if (!seen.Contains(s.Key))
                    log.Add($"{tag} note: '{s.Key}' is not in the sheet - kept {Format(s.Get(table))}");
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
            var elemSlots = Slots(ElementFields(elem)).ToList();
            var header = rows[0];
            var cols = new Slot[header.Length];
            for (int c = 0; c < header.Length; c++)
            {
                string h = header[c].Trim();
                if (h.Length == 0 || h.StartsWith("#")) continue;
                cols[c] = elemSlots.FirstOrDefault(s => s.Key.Equals(h, StringComparison.OrdinalIgnoreCase));
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
                    if (TryParse(raw, cols[c].Type, out object v)) cols[c].Set(item, v);
                    else log.Add($"{tag} ERROR {tab} row {r + 1}: cannot read '{raw}' as {cols[c].Type.Name} ({cols[c].Key})");
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
            if (RowsText(oldValue, elemSlots) == RowsText(newValue, elemSlots)) return 0;
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
            foreach (var s in Slots(ValueFields(table.GetType())))
            {
                var tip = s.Field.GetCustomAttribute<TooltipAttribute>();
                sb.Append(s.Key).Append('\t')
                  .Append(Format(s.Get(table))).Append('\t')
                  .Append(tip != null ? OneLine(tip.tooltip) : "").Append('\n');
            }
            return sb.ToString();
        }

        /// Current rows of a [SheetRows] field as TSV (row 1 = column names).
        public static string RowsTsv(BalanceTable table, FieldInfo field)
        {
            var elemSlots = Slots(ElementFields(ElementType(field.FieldType))).ToList();
            var sb = new StringBuilder(string.Join("\t", elemSlots.Select(s => s.Key))).Append('\n');
            sb.Append(RowsText(field.GetValue(table), elemSlots));
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

        static IEnumerable<Slot> Slots(IEnumerable<FieldInfo> fields)
        {
            foreach (var f in fields)
            {
                if (f.FieldType == typeof(Vector2))
                {
                    yield return new Slot { Key = f.Name + ".x", Type = typeof(float), Field = f, Axis = 0 };
                    yield return new Slot { Key = f.Name + ".y", Type = typeof(float), Field = f, Axis = 1 };
                }
                else yield return new Slot { Key = f.Name, Type = f.FieldType, Field = f };
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
                    case "true": case "yes": value = true; return true;
                    case "false": case "no": value = false; return true;
                }
                // Google Sheets exports numbers as "1.0" / "0.0"
                if (double.TryParse(s, NumberStyles.Float, Inv, out var b)) { value = Math.Abs(b) > 1e-6; return true; }
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
                case IFormattable fm: return fm.ToString(null, Inv);
                default: return v.ToString();
            }
        }

        static string Cell(string[] row, int i) => i < row.Length ? row[i] ?? "" : "";
        static string OneLine(string s) => (s ?? "").Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
        static int Count(object list) => list is ICollection c ? c.Count : 0;

        static string RowsText(object list, List<Slot> slots)
        {
            if (!(list is IEnumerable items)) return "";
            var sb = new StringBuilder();
            foreach (var it in items)
                sb.Append(string.Join("\t", slots.Select(s => Format(s.Get(it))))).Append('\n');
            return sb.ToString();
        }
    }
}
