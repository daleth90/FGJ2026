using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace GrassRun.EditorTools
{
    /// <summary>
    /// 從企劃表格（xlsx 的 title_list 分頁）匯入稱號文字到 TitleTable.asset。
    /// 依 title_id 對應，只更新名稱（title_name）、結局描述（title_desc）、達成條件（title_condition）；
    /// 圖片、requirement 和稱號順序都不動。表格儲存格空白就保留原本的內容；表格有、asset 沒有的編號只列出來，不會新增。
    /// </summary>
    public static class TitleTableImporter
    {
        const string BalancePath = "Assets/Game/Data/Balance.asset";
        const string SheetName = "title_list";
        const string LastPathKey = "GrassRun.TitleTableImporter.LastPath";
        const int KeyRow = 2;          // 第 2 列是欄位 key（title_id、title_name…）
        const int FirstDataRow = 4;    // 第 3 列是說明，資料從第 4 列開始

        public class Change
        {
            public TitleDefinition title;
            public string field;
            public string before;
            public string after;
        }

        public class Preview
        {
            public readonly List<Change> changes = new List<Change>();
            public readonly List<string> notes = new List<string>();
            public int rowCount;
        }

        [MenuItem("GrassRun/從表格匯入稱號")]
        public static void ImportFromMenu()
        {
            var table = LoadTitleTable();
            if (table == null)
            {
                EditorUtility.DisplayDialog("匯入稱號", $"找不到稱號表：{BalancePath} 的 Title Table 沒有指定。", "確定");
                return;
            }

            string lastPath = EditorPrefs.GetString(LastPathKey, Path.GetFullPath("Doc"));
            string directory = File.Exists(lastPath) ? Path.GetDirectoryName(lastPath) : lastPath;
            string path = EditorUtility.OpenFilePanel("選擇企劃表格", directory, "xlsx");
            if (string.IsNullOrEmpty(path)) return;
            EditorPrefs.SetString(LastPathKey, path);

            Preview preview;
            try
            {
                preview = BuildPreview(path, table);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("匯入稱號", "讀取表格失敗：\n" + e.Message, "確定");
                Debug.LogException(e);
                return;
            }

            var report = new StringBuilder();
            report.AppendLine($"表格：{Path.GetFileName(path)}（{SheetName}，{preview.rowCount} 列）");
            foreach (var change in preview.changes)
                report.AppendLine($"[{change.title.titleId} {change.title.titleName}] {change.field}\n  原本：{change.before}\n  表格：{change.after}");
            foreach (var note in preview.notes) report.AppendLine("注意：" + note);
            Debug.Log("[匯入稱號] 預覽\n" + report);

            if (preview.changes.Count == 0)
            {
                EditorUtility.DisplayDialog("匯入稱號", "稱號表已經和表格一致，沒有需要更新的內容。" + NotesSummary(preview), "確定");
                return;
            }

            var titles = new HashSet<int>();
            foreach (var change in preview.changes) titles.Add(change.title.titleId);
            var message = new StringBuilder();
            message.AppendLine($"將更新 {titles.Count} 個稱號、共 {preview.changes.Count} 個欄位（名稱／描述／條件）。圖片與需求不變。");
            message.AppendLine();
            for (int i = 0; i < preview.changes.Count && i < 3; i++)
            {
                var change = preview.changes[i];
                message.AppendLine($"[{change.title.titleId}] {change.field}：\n  {Shorten(change.before)}\n→ {Shorten(change.after)}");
            }
            if (preview.changes.Count > 3) message.AppendLine($"…其餘 {preview.changes.Count - 3} 筆見 Console。");
            message.Append(NotesSummary(preview));
            if (!EditorUtility.DisplayDialog("匯入稱號", message.ToString(), "套用", "取消")) return;

            Apply(preview, table);
            Debug.Log($"[匯入稱號] 已更新 {preview.changes.Count} 個欄位到 {AssetDatabase.GetAssetPath(table)}。");
        }

        static string Shorten(string text)
        {
            text = (text ?? string.Empty).Replace("\n", " ");
            return text.Length > 40 ? text.Substring(0, 40) + "…" : text;
        }

        static string NotesSummary(Preview preview) =>
            preview.notes.Count == 0 ? string.Empty : $"\n\n另有 {preview.notes.Count} 則注意事項（見 Console）。";

        public static TitleTable LoadTitleTable()
        {
            var balance = AssetDatabase.LoadAssetAtPath<GameBalance>(BalancePath);
            return balance != null ? balance.titleTable : null;
        }

        /// <summary>讀表格並比對，不修改任何資產。</summary>
        public static Preview BuildPreview(string xlsxPath, TitleTable table)
        {
            var preview = new Preview();
            var rows = ReadSheet(xlsxPath, SheetName);
            preview.rowCount = rows.Count;

            var byId = new Dictionary<int, TitleDefinition>();
            AddTitles(byId, table.titles);
            AddTitles(byId, table.specialEndingTitles);

            var seen = new HashSet<int>();
            foreach (var row in rows)
            {
                if (!row.TryGetValue("title_id", out string idText) || string.IsNullOrWhiteSpace(idText)) continue;
                if (!int.TryParse(idText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
                    && !(double.TryParse(idText.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
                         && (id = (int)number) == number))
                {
                    preview.notes.Add($"title_id「{idText}」不是整數，略過。");
                    continue;
                }
                if (!byId.TryGetValue(id, out var title))
                {
                    preview.notes.Add($"表格有稱號 {id}，但稱號表沒有，略過（不會自動新增）。");
                    continue;
                }
                seen.Add(id);

                Compare(preview, title, "名稱", title.titleName, Get(row, "title_name"));
                Compare(preview, title, "描述", title.description, Get(row, "title_desc"));
                Compare(preview, title, "條件", title.conditionDescription, Get(row, "title_condition"));

                // requirement 不由這個工具更新；只提醒表格的達標欄和稱號表不一致。
                string sheetRequirement = JoinRequirement(row);
                if (Normalize(sheetRequirement) != Normalize(title.requirement ?? string.Empty))
                    preview.notes.Add($"稱號 {id} 的需求不一致（不會更新）：表格「{sheetRequirement}」／稱號表「{title.requirement}」。");
            }

            foreach (var pair in byId)
                if (!seen.Contains(pair.Key)) preview.notes.Add($"稱號表有稱號 {pair.Key}，但表格沒有這一列。");
            return preview;
        }

        public static void Apply(Preview preview, TitleTable table)
        {
            Undo.RecordObject(table, "匯入稱號");
            foreach (var change in preview.changes)
            {
                switch (change.field)
                {
                    case "名稱": change.title.titleName = change.after; break;
                    case "描述": change.title.description = change.after; break;
                    case "條件": change.title.conditionDescription = change.after; break;
                }
            }
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssetIfDirty(table);
        }

        static void AddTitles(Dictionary<int, TitleDefinition> byId, TitleDefinition[] titles)
        {
            if (titles == null) return;
            foreach (var title in titles)
                if (title != null && !byId.ContainsKey(title.titleId)) byId.Add(title.titleId, title);
        }

        static string Get(Dictionary<string, string> row, string key) =>
            row.TryGetValue(key, out string value) ? value : null;

        /// <summary>把 reach_mor／reach_spd／reach_tgh 依 mor;spd;tgh 順序串成 requirement 字串。</summary>
        public static string JoinRequirement(Dictionary<string, string> row)
        {
            var parts = new List<string>();
            foreach (var key in new[] { "reach_mor", "reach_spd", "reach_tgh" })
            {
                string value = Get(row, key);
                if (!string.IsNullOrWhiteSpace(value)) parts.Add(value.Trim());
            }
            return string.Join(";", parts);
        }

        static void Compare(Preview preview, TitleDefinition title, string field, string current, string incoming)
        {
            if (string.IsNullOrWhiteSpace(incoming)) return;   // 表格空白：保留原本內容
            incoming = Normalize(incoming);
            if (incoming == Normalize(current ?? string.Empty)) return;
            preview.changes.Add(new Change { title = title, field = field, before = current, after = incoming });
        }

        static string Normalize(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();

        // ---- 最小的 xlsx 讀取（xlsx 是 zip 包著 XML）：只讀文字與數字，不處理公式運算結果以外的東西。

        const string MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        const string PackageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        /// <summary>讀指定分頁，以第 2 列為欄位 key，回傳第 4 列起每列的 key→值。</summary>
        public static List<Dictionary<string, string>> ReadSheet(string xlsxPath, string sheetName)
        {
            // Excel 開著檔案時仍可讀取。
            using var stream = new FileStream(xlsxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

            var shared = ReadSharedStrings(zip);
            string sheetPath = FindSheetPath(zip, sheetName);
            var sheet = LoadXml(zip, sheetPath);

            var ns = new XmlNamespaceManager(sheet.NameTable);
            ns.AddNamespace("m", MainNs);

            var cells = new Dictionary<int, Dictionary<int, string>>();
            foreach (XmlElement row in sheet.SelectNodes("//m:sheetData/m:row", ns))
            {
                foreach (XmlElement cell in row.SelectNodes("m:c", ns))
                {
                    ParseReference(cell.GetAttribute("r"), out int rowIndex, out int column);
                    string value = CellValue(cell, ns, shared);
                    if (value == null) continue;
                    if (!cells.TryGetValue(rowIndex, out var rowCells)) cells[rowIndex] = rowCells = new Dictionary<int, string>();
                    rowCells[column] = value;
                }
            }

            if (!cells.TryGetValue(KeyRow, out var keyCells))
                throw new InvalidDataException($"「{sheetName}」第 {KeyRow} 列沒有欄位 key。");

            var result = new List<Dictionary<string, string>>();
            foreach (var pair in cells)
            {
                if (pair.Key < FirstDataRow) continue;
                var record = new Dictionary<string, string>();
                foreach (var key in keyCells)
                {
                    if (string.IsNullOrWhiteSpace(key.Value)) continue;
                    if (pair.Value.TryGetValue(key.Key, out string value)) record[key.Value.Trim()] = value;
                }
                if (record.Count > 0) result.Add(record);
            }
            return result;
        }

        static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var list = new List<string>();
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return list;

            var doc = LoadXml(zip, entry.FullName);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("m", MainNs);
            foreach (XmlElement si in doc.SelectNodes("//m:sst/m:si", ns))
                list.Add(JoinText(si, ns));
            return list;
        }

        static string FindSheetPath(ZipArchive zip, string sheetName)
        {
            var workbook = LoadXml(zip, "xl/workbook.xml");
            var ns = new XmlNamespaceManager(workbook.NameTable);
            ns.AddNamespace("m", MainNs);

            string relId = null;
            foreach (XmlElement sheet in workbook.SelectNodes("//m:sheets/m:sheet", ns))
            {
                if (sheet.GetAttribute("name") != sheetName) continue;
                relId = sheet.GetAttribute("id", RelNs);
                break;
            }
            if (relId == null) throw new InvalidDataException($"表格裡沒有「{sheetName}」分頁。");

            var rels = LoadXml(zip, "xl/_rels/workbook.xml.rels");
            var rns = new XmlNamespaceManager(rels.NameTable);
            rns.AddNamespace("p", PackageRelNs);
            foreach (XmlElement rel in rels.SelectNodes("//p:Relationship", rns))
            {
                if (rel.GetAttribute("Id") != relId) continue;
                string target = rel.GetAttribute("Target");
                return target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target;
            }
            throw new InvalidDataException($"找不到「{sheetName}」分頁的內容。");
        }

        static XmlDocument LoadXml(ZipArchive zip, string path)
        {
            var entry = zip.GetEntry(path) ?? throw new InvalidDataException($"xlsx 缺少 {path}。");
            var doc = new XmlDocument();
            using (var s = entry.Open()) doc.Load(s);
            return doc;
        }

        static string CellValue(XmlElement cell, XmlNamespaceManager ns, List<string> shared)
        {
            string type = cell.GetAttribute("t");
            if (type == "inlineStr")
            {
                var inline = cell.SelectSingleNode("m:is", ns) as XmlElement;
                return inline != null ? JoinText(inline, ns) : null;
            }

            var v = cell.SelectSingleNode("m:v", ns);
            if (v == null) return null;
            if (type == "s")
            {
                int index = int.Parse(v.InnerText, CultureInfo.InvariantCulture);
                return index >= 0 && index < shared.Count ? shared[index] : null;
            }
            return v.InnerText;
        }

        static string JoinText(XmlElement element, XmlNamespaceManager ns)
        {
            var sb = new StringBuilder();
            foreach (XmlNode t in element.SelectNodes(".//m:t", ns)) sb.Append(t.InnerText);
            return sb.ToString();
        }

        static void ParseReference(string reference, out int row, out int column)
        {
            column = 0;
            int i = 0;
            while (i < reference.Length && char.IsLetter(reference[i]))
            {
                column = column * 26 + (char.ToUpperInvariant(reference[i]) - 'A' + 1);
                i++;
            }
            column -= 1;
            row = int.Parse(reference.Substring(i), CultureInfo.InvariantCulture);
        }
    }
}
