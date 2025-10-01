using System;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PureGIS_Geo_QC.Exports.Models;
using PureGIS_Geo_QC.Models;

namespace PureGIS_Geo_QC.Exports
{
    public class WordExporter : IReportExporter
    {
        public string FileExtension => ".docx";
        public string FileFilter => "Word 문서 (*.docx)|*.docx";
        public string ExporterName => "Microsoft Word";

        public async Task<bool> ExportAsync(MultiFileReport multiReport, string filePath)
        {
            return await Task.Run(() => Export(multiReport, filePath));
        }

        private string ConvertBoolToSymbol(bool? value)
        {
            if (!value.HasValue) return "-";
            return value.Value ? "✓" : "✗";
        }

        public bool Export(MultiFileReport multiReport, string filePath)
        {
            try
            {
                using (var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
                {
                    var mainPart = document.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    CreateTitle(body, multiReport.ProjectName);
                    CreateOverallSummarySection(body, multiReport);

                    foreach (var reportData in multiReport.FileResults)
                    {
                        CreateFileDetailSection(body, reportData);
                    }

                    CreateFooter(body);
                    mainPart.Document.Save();
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Word Export Error: {ex.Message}");
                return false;
            }
        }

        private void CreateTitle(Body body, string projectName)
        {
            var p = body.AppendChild(new Paragraph());
            var r = p.AppendChild(new Run());
            r.AppendChild(new RunProperties(new Bold(), new FontSize() { Val = "32" }, new RunFonts() { Ascii = "맑은 고딕", EastAsia = "맑은 고딕" }, new Color() { Val = "1F497D" }));
            r.AppendChild(new Text($"[{projectName}] SHP데이터 형식 결과 보고서"));
            p.ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center }, new SpacingBetweenLines() { After = "240" });
        }

        private void CreateOverallSummarySection(Body body, MultiFileReport multiReport)
        {
            CreateSectionTitle(body, "전체 검사 요약");
            var table = body.AppendChild(new Table());
            ConfigureTable(table);
            AddInfoRow(table, "검사 실행일", multiReport.ReportDate.ToString("yyyy년 MM월 dd일 HH시 mm분"));
            AddInfoRow(table, "총 파일 수", $"{multiReport.TotalFiles} 개");
            AddInfoRow(table, "전체 성공률", multiReport.OverallSuccessRate);
            body.AppendChild(new Paragraph());
        }

        private void CreateFileDetailSection(Body body, ReportData reportData)
        {
            CreateSectionTitle(body, $"파일별 상세 결과: {reportData.FileName}");

            var summaryTable = body.AppendChild(new Table());
            ConfigureTable(summaryTable);
            var headerRow = summaryTable.AppendChild(new TableRow());
            AddTableCell(headerRow, "전체 필드", true);
            AddTableCell(headerRow, "정상", true);
            AddTableCell(headerRow, "오류", true);
            AddTableCell(headerRow, "정상률", true);
            var dataRow = summaryTable.AppendChild(new TableRow());
            AddTableCell(dataRow, reportData.TotalCount.ToString(), JustificationValues.Center);
            AddTableCell(dataRow, reportData.NormalCount.ToString(), JustificationValues.Center, "92D050");
            AddTableCell(dataRow, reportData.ErrorCount.ToString(), JustificationValues.Center, "C5504B");
            AddTableCell(dataRow, reportData.SuccessRate, JustificationValues.Center);
            body.AppendChild(new Paragraph());

            var detailTable = body.AppendChild(new Table());
            ConfigureTable(detailTable);
            var detailHeaderRow = detailTable.AppendChild(new TableRow());
            var headers = new[] { "상태", "기준컬럼", "기준타입/길이", "파일타입/길이", "NULL허용", "코드일치", "오류수(NULL/코드)", "비고" };
            foreach (var header in headers) { AddTableCell(detailHeaderRow, header, true); }

            foreach (var result in reportData.ValidationResults)
            {
                var detailDataRow = detailTable.AppendChild(new TableRow());
                var statusColor = result.Status == "정상" ? "92D050" : "C5504B";
                AddTableCell(detailDataRow, result.Status ?? "", JustificationValues.Center, statusColor);
                AddTableCell(detailDataRow, result.Std_ColumnId ?? "");
                AddTableCell(detailDataRow, $"{result.Std_Type}({result.Std_Length})");
                AddTableCell(detailDataRow, $"{result.Cur_Type}({result.Cur_Length})");
                AddTableCell(detailDataRow, ConvertBoolToSymbol(result.IsNotNullCorrect), JustificationValues.Center);
                AddTableCell(detailDataRow, ConvertBoolToSymbol(result.IsCodeCorrect), JustificationValues.Center);
                AddTableCell(detailDataRow, $"{result.NotNullErrorCount} / {result.CodeErrorCount}", JustificationValues.Center);
                AddTableCell(detailDataRow, ReportData.GetRemarks(result));
            }
            body.AppendChild(new Paragraph());
        }

        private void CreateFooter(Body body)
        {
            var p = body.AppendChild(new Paragraph());
            var r = p.AppendChild(new Run());
            r.AppendChild(new RunProperties(new FontSize() { Val = "16" }, new RunFonts() { Ascii = "맑은 고딕", EastAsia = "맑은 고딕" }, new Color() { Val = "808080" }));
            r.AppendChild(new Text($"보고서 생성: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | PureGIS GEO-QC"));
            p.ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Right }, new SpacingBetweenLines() { Before = "240" });
        }

        private void CreateSectionTitle(Body body, string title)
        {
            var p = body.AppendChild(new Paragraph());
            var r = p.AppendChild(new Run());
            r.AppendChild(new RunProperties(new Bold(), new FontSize() { Val = "24" }, new RunFonts() { Ascii = "맑은 고딕", EastAsia = "맑은 고딕" }, new Color() { Val = "1F497D" }));
            r.AppendChild(new Text(title));
            p.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { Before = "120", After = "120" });
        }

        private void ConfigureTable(Table table)
        {
            var props = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder() { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder() { Val = BorderValues.Single, Size = 4 },
                    new RightBorder() { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 2 },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 2 }
                ),
                new TableWidth() { Type = TableWidthUnitValues.Pct, Width = "5000" }
            );
            table.AppendChild(props);
        }

        private void AddInfoRow(Table table, string label, string value)
        {
            var row = table.AppendChild(new TableRow());
            AddTableCell(row, label, JustificationValues.Left, "F2F2F2", true);
            AddTableCell(row, value);
        }
        // 가장 상세한 기능을 가진 기본 메서드
        private void AddTableCell(TableRow row, string text, JustificationValues justification, string backgroundColor, bool isBold)
        {
            var cell = row.AppendChild(new TableCell());
            var p = cell.AppendChild(new Paragraph());
            var r = p.AppendChild(new Run());
            var rp = new RunProperties();
            rp.AppendChild(new RunFonts() { Ascii = "맑은 고딕", EastAsia = "맑은 고딕" });
            rp.AppendChild(new FontSize() { Val = "18" }); // 9pt
            if (isBold)
            {
                rp.AppendChild(new Bold());
            }

            // 배경색이 있으면 글자색을 흰색으로
            if (!string.IsNullOrEmpty(backgroundColor))
            {
                rp.AppendChild(new Color() { Val = "FFFFFF" });
            }

            r.AppendChild(rp);
            r.AppendChild(new Text(text));
            p.ParagraphProperties = new ParagraphProperties(new Justification() { Val = justification });

            var cp = new TableCellProperties(new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center });
            if (!string.IsNullOrEmpty(backgroundColor))
            {
                cp.AppendChild(new Shading() { Val = ShadingPatternValues.Clear, Color = "auto", Fill = backgroundColor });
            }
            cell.AppendChild(cp);
        }

        // 위 기본 메서드를 호출하는 다양한 오버로딩(Overloading) 메서드들
        private void AddTableCell(TableRow row, string text)
        {
            AddTableCell(row, text, JustificationValues.Left, null, false);
        }
        private void AddTableCell(TableRow row, string text, JustificationValues justification)
        {
            AddTableCell(row, text, justification, null, false);
        }

        private void AddTableCell(TableRow row, string text, JustificationValues justification, string backgroundColor)
        {
            AddTableCell(row, text, justification, backgroundColor, false);
        }

        private void AddTableCell(TableRow row, string text, bool isHeader)
        {
            AddTableCell(row, text, JustificationValues.Center, isHeader ? "1F497D" : null, isHeader);
        }
    }
}