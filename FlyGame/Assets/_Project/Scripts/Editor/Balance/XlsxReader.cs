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
