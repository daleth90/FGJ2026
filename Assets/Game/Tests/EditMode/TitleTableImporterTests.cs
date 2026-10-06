using System.IO;
using System.IO.Compression;
using System.Text;
using GrassRun.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace GrassRun.Tests
{
    public class TitleTableImporterTests
    {
        string path;

        [SetUp]
        public void CreateXlsx()
        {
            path = Path.Combine(Path.GetTempPath(), "GrassRunTitleImport_" + System.Guid.NewGuid().ToString("N") + ".xlsx");
            using var stream = new FileStream(path, FileMode.Create);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
            Write(zip, "xl/workbook.xml",
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"event_list\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"title_list\" sheetId=\"2\" r:id=\"rId2\"/></sheets></workbook>");
            Write(zip, "xl/_rels/workbook.xml.rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                "<Relationship Id=\"rId2\" Type=\"worksheet\" Target=\"worksheets/sheet2.xml\"/></Relationships>");
            // 共用字串：0 title_id、1 title_name、2 title_desc、3 title_condition、4 新名稱、5 新條件、6 reach_spd、7 spd>=15
            Write(zip, "xl/sharedStrings.xml",
                "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<si><t>title_id</t></si><si><t>title_name</t></si><si><t>title_desc</t></si><si><t>title_condition</t></si>" +
                "<si><r><t>新</t></r><r><t>名稱</t></r></si><si><t>新條件</t></si><si><t>reach_spd</t></si><si><t>spd&gt;=15</t></si></sst>");
            Write(zip, "xl/worksheets/sheet1.xml",
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData/></worksheet>");
            // 欄位順序故意和預設不同：A=title_condition、B=title_id、C=title_name、D=reach_spd、E=title_desc
            Write(zip, "xl/worksheets/sheet2.xml",
                "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
                "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>說明列</t></is></c></row>" +
                "<row r=\"2\"><c r=\"A2\" t=\"s\"><v>3</v></c><c r=\"B2\" t=\"s\"><v>0</v></c><c r=\"C2\" t=\"s\"><v>1</v></c><c r=\"D2\" t=\"s\"><v>6</v></c><c r=\"E2\" t=\"s\"><v>2</v></c></row>" +
                "<row r=\"3\"><c r=\"A3\" t=\"inlineStr\"><is><t>中文說明</t></is></c></row>" +
                "<row r=\"4\"><c r=\"A4\" t=\"s\"><v>5</v></c><c r=\"B4\"><v>1</v></c><c r=\"C4\" t=\"s\"><v>4</v></c><c r=\"D4\" t=\"s\"><v>7</v></c></row>" +
                "<row r=\"5\"><c r=\"B5\"><v>3021</v></c></row>" +
                "</sheetData></worksheet>");
        }

        [TearDown]
        public void DeleteXlsx()
        {
            if (File.Exists(path)) File.Delete(path);
        }

        static void Write(ZipArchive zip, string name, string xml)
        {
            using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
            writer.Write(xml);
        }

        static TitleTable Table()
        {
            var table = ScriptableObject.CreateInstance<TitleTable>();
            table.titles = new[]
            {
                new TitleDefinition { titleId = 1, titleName = "舊名稱", description = "舊描述", conditionDescription = "舊條件", requirement = "spd>=15" },
                new TitleDefinition { titleId = 2, titleName = "沒在表格", description = "", conditionDescription = "", requirement = "" },
            };
            table.specialEndingTitles = new TitleDefinition[0];
            return table;
        }

        [Test]
        public void ReadSheet_MapsColumnsByKeyRowAndSkipsHeaderRows()
        {
            var rows = TitleTableImporter.ReadSheet(path, "title_list");

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("1", rows[0]["title_id"]);
            Assert.AreEqual("新名稱", rows[0]["title_name"]);
            Assert.AreEqual("新條件", rows[0]["title_condition"]);
            Assert.IsFalse(rows[0].ContainsKey("title_desc"));
            Assert.AreEqual("3021", rows[1]["title_id"]);
        }

        [Test]
        public void BuildPreview_UpdatesTextOnlyAndKeepsEmptyCells()
        {
            var table = Table();
            var preview = TitleTableImporter.BuildPreview(path, table);

            Assert.AreEqual(2, preview.changes.Count);   // 名稱、條件；描述空白不覆蓋
            TitleTableImporter.Apply(preview, table);

            var title = table.titles[0];
            Assert.AreEqual("新名稱", title.titleName);
            Assert.AreEqual("舊描述", title.description);
            Assert.AreEqual("新條件", title.conditionDescription);
            Assert.AreEqual("spd>=15", title.requirement);
            Object.DestroyImmediate(table);
        }

        [Test]
        public void BuildPreview_ReportsUnknownAndMissingIdsWithoutAdding()
        {
            var table = Table();
            var preview = TitleTableImporter.BuildPreview(path, table);

            Assert.IsTrue(preview.notes.Exists(n => n.Contains("3021")));
            Assert.IsTrue(preview.notes.Exists(n => n.Contains("稱號 2")));
            Assert.IsFalse(preview.notes.Exists(n => n.Contains("需求不一致")));
            Assert.AreEqual(2, table.titles.Length);
            Object.DestroyImmediate(table);
        }
    }
}
