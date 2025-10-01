// managers/ExcelManager.cs

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelDataReader;
using PureGIS_Geo_QC.Models;
using Vml = DocumentFormat.OpenXml.Vml;

namespace PureGIS_Geo_QC.Managers
{
    public static class ExcelManager
    {
        // ... 다른 메서드들은 변경 없습니다 ...
        public static ProjectDefinition ImportProjectFromExcel(string filePath)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                    });
                    var project = new ProjectDefinition
                    {
                        ProjectName = System.IO.Path.GetFileNameWithoutExtension(filePath),
                        CreatedDate = DateTime.Now,
                        LastModifiedDate = DateTime.Now
                    };
                    if (dataSet.Tables.Contains("테이블 정의"))
                    {
                        ParseTableDefinitions(dataSet.Tables["테이블 정의"], project);
                    }
                    if (dataSet.Tables.Contains("코드 정의"))
                    {
                        ParseCodeDefinitions(dataSet.Tables["코드 정의"], project);
                    }
                    return project;
                }
            }
        }
        private static void ParseTableDefinitions(DataTable tableSheet, ProjectDefinition project)
        {
            foreach (DataRow row in tableSheet.Rows)
            {
                try
                {
                    var categoryId = row["분류ID"]?.ToString();
                    var categoryName = row["분류명"]?.ToString();
                    var tableId = row["테이블ID"]?.ToString();
                    var tableName = row["테이블명"]?.ToString();
                    if (string.IsNullOrWhiteSpace(tableId) || string.IsNullOrWhiteSpace(categoryId)) continue;
                    var category = project.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
                    if (category == null)
                    {
                        category = new InfrastructureCategory { CategoryId = categoryId, CategoryName = categoryName };
                        project.Categories.Add(category);
                    }
                    var table = category.Tables.FirstOrDefault(t => t.TableId == tableId);
                    if (table == null)
                    {
                        table = new TableDefinition { TableId = tableId, TableName = tableName };
                        category.Tables.Add(table);
                    }
                    var column = new ColumnDefinition
                    {
                        ColumnId = row["컬럼ID"]?.ToString(),
                        ColumnName = row["컬럼명"]?.ToString(),
                        Type = row["타입"]?.ToString(),
                        Length = row["길이"]?.ToString(),
                        IsNotNull = (row["NOT NULL"]?.ToString() ?? "N").Equals("Y", StringComparison.OrdinalIgnoreCase),
                        CodeName = row["코드명"]?.ToString()
                    };
                    if (!string.IsNullOrWhiteSpace(column.ColumnId))
                    {
                        table.Columns.Add(column);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"테이블 정의 파싱 오류: {ex.Message}");
                    continue;
                }
            }
        }
        private static void ParseCodeDefinitions(DataTable codeSheet, ProjectDefinition project)
        {
            foreach (DataRow row in codeSheet.Rows)
            {
                try
                {
                    var codeGroupName = row["코드 그룹명"]?.ToString();
                    var codeValue = row["코드"]?.ToString();
                    var codeDesc = row["코드 한글명"]?.ToString();
                    if (string.IsNullOrWhiteSpace(codeGroupName) || string.IsNullOrWhiteSpace(codeValue)) continue;
                    var codeSet = project.CodeSets.FirstOrDefault(cs => cs.CodeName == codeGroupName);
                    if (codeSet == null)
                    {
                        codeSet = new CodeSet { CodeName = codeGroupName };
                        project.CodeSets.Add(codeSet);
                    }
                    if (!codeSet.Codes.Any(c => c.Code == codeValue))
                    {
                        codeSet.Codes.Add(new CodeValue { Code = codeValue, Description = codeDesc });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"코드 정의 파싱 오류: {ex.Message}");
                    continue;
                }
            }
        }
        public static void CreateSampleExcelFile(string filePath)
        {
            using (var spreadsheetDocument = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
            {
                var workbookpart = spreadsheetDocument.AddWorkbookPart();
                workbookpart.Workbook = new Workbook();
                var sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
                var sheetPart1 = workbookpart.AddNewPart<WorksheetPart>();
                sheetPart1.Worksheet = new Worksheet(new SheetData());
                var sheet1 = new Sheet() { Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(sheetPart1), SheetId = 1, Name = "테이블 정의" };
                sheets.Append(sheet1);
                var sheetData1 = sheetPart1.Worksheet.GetFirstChild<SheetData>();
                var headerRow1 = new Row();
                var headers1 = new List<string> { "분류ID", "분류명", "테이블ID", "테이블명", "컬럼ID", "컬럼명", "타입", "길이", "NOT NULL", "코드명" };
                foreach (var header in headers1) { headerRow1.Append(new Cell() { CellValue = new CellValue(header), DataType = CellValues.String }); }
                sheetData1.Append(headerRow1);
                var sheetPart2 = workbookpart.AddNewPart<WorksheetPart>();
                sheetPart2.Worksheet = new Worksheet(new SheetData());
                var sheet2 = new Sheet() { Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(sheetPart2), SheetId = 2, Name = "코드 정의" };
                sheets.Append(sheet2);
                var sheetData2 = sheetPart2.Worksheet.GetFirstChild<SheetData>();
                var headerRow2 = new Row();
                var headers2 = new List<string> { "코드 그룹명", "코드", "코드 한글명" };
                foreach (var header in headers2) { headerRow2.Append(new Cell() { CellValue = new CellValue(header), DataType = CellValues.String }); }
                sheetData2.Append(headerRow2);
                AddComment(sheetPart2, "A1", "PureGIS Geo-QC", "'테이블 정의' 시트의 '코드명' 컬럼에 기입하는 이름과 동일해야 합니다.\n이 이름을 기준으로 두 데이터가 연결됩니다.");
                workbookpart.Workbook.Save();
            }
        }

        // ✨ ----- 이 메서드의 내용이 수정되었습니다 ----- ✨
        private static void AddComment(WorksheetPart worksheetPart, string cellReference, string author, string commentText)
        {
            WorksheetCommentsPart commentsPart = worksheetPart.AddNewPart<WorksheetCommentsPart>();
            commentsPart.Comments = new Comments(
                new Authors(new Author(author)),
                new CommentList(
                    new Comment(
                        new CommentText(
                            new Run(
                                new Text(commentText))))
                    { Reference = cellReference, AuthorId = 0 }
                )
            );

            VmlDrawingPart vmlPart = worksheetPart.AddNewPart<VmlDrawingPart>();
            using (XmlTextWriter writer = new XmlTextWriter(vmlPart.GetStream(), Encoding.UTF8))
            {
                // ✨ ----- style 속성의 width와 height, Anchor 태그의 값을 수정했습니다 ----- ✨
                writer.WriteRaw(
                    "<xml xmlns:v=\"urn:schemas-microsoft-com:vml\"\r\n" +
                    " xmlns:o=\"urn:schemas-microsoft-com:office:office\"\r\n" +
                    " xmlns:x=\"urn:schemas-microsoft-com:office:excel\">\r\n" +
                    " <o:shapelayout v:ext=\"edit\">\r\n" +
                    "  <o:idmap v:ext=\"edit\" data=\"1\"/>\r\n" +
                    " </o:shapelayout><v:shapetype id=\"_x0000_t202\" coordsize=\"21600,21600\" o:spt=\"202\"\r\n" +
                    "  path=\"m,l,21600r21600,l21600,xe\">\r\n" +
                    "  <v:stroke joinstyle=\"miter\"/>\r\n" +
                    "  <v:path gradientshapeok=\"t\" o:connecttype=\"rect\"/>\r\n" +
                    " </v:shapetype><v:shape id=\"_x0000_s1025\" type=\"#_x0000_t202\" style='position:absolute;\r\n" +
                    "  margin-left:59.25pt;margin-top:1.5pt;width:368.55pt;height:56.7pt;z-index:1;\r\n" + // ... // 너비(width)와 높이(height)를 늘렸습니다.
                    "  visibility:hidden' fillcolor=\"#ffffe1\" o:insetmode=\"auto\">\r\n" +
                    "  <v:fill color2=\"#ffffe1\"/>\r\n" +
                    "  <v:shadow on=\"t\" color=\"black\" obscured=\"t\"/>\r\n" +
                    "  <v:path o:connecttype=\"none\"/>\r\n" +
                    "  <v:textbox style='mso-direction-alt:auto'/>\r\n" +
                    "  <x:ClientData ObjectType=\"Note\">\r\n" +
                    "   <x:MoveWithCells/>\r\n" +
                    "   <x:ResizeWithCells/>\r\n" +
                    "   <x:Anchor>1, 15, 0, 10, 4, 1, 4, 1</x:Anchor>\r\n" + // 박스가 차지하는 셀 범위를 늘렸습니다.
                    "   <x:AutoFill>False</x:AutoFill>\r\n" +
                    "   <x:Row>0</x:Row>\r\n" +
                    "   <x:Column>0</x:Column>\r\n" +
                    "  </x:ClientData>\r\n" +
                    " </v:shape></xml>"
                );
            }

            worksheetPart.Worksheet.Append(new LegacyDrawing() { Id = worksheetPart.GetIdOfPart(vmlPart) });
        }
    }
}