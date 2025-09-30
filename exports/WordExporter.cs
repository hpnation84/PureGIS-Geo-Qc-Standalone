﻿using System;
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
            var titleParagraph = body.AppendChild(new Paragraph());
            var titleRun = titleParagraph.AppendChild(new Run());
            titleRun.AppendChild(new RunProperties(new Bold(), new FontSize() { Val = "28" }, new RunFonts() { Ascii = "맑은 고딕", EastAsia = "맑은 고딕" }, new Color() { Val = "1F497D" }));
            titleRun.AppendChild(new Text($"[{projectName}] SHP데이터 형식 결과 보고서"));
            titleParagraph.ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center }, new SpacingBetweenLines() { After = "240" });
            var separatorParagraph = body.AppendChild(new Paragraph());
            separatorParagraph.ParagraphProperties = new ParagraphProperties(new ParagraphBorders(new TopBorder() { Val = BorderValues.Single, Size = 6, Color = "1F497D" }));
            separatorParagraph.AppendChild(new Run(new Text("")));
        }

        private void CreateOverallSummarySection(Body body, MultiFileReport multiReport)
        {
            CreateSectionTitle(body, "전체 검사 요약");
            var infoTable = body.AppendChild(new Table());
            ConfigureTable(infoTable, new[] { "2000", "4000" });
            AddInfoRow(infoTable, "검사 실행일", multiReport.ReportDate.ToString("yyyy년 MM월 dd일 HH시 mm분"));
            AddInfoRow(infoTable, "총 파일 수", $"{multiReport.TotalFiles} 개");
            AddInfoRow(infoTable, "전체 컬럼 수", $"{multiReport.TotalColumns} 개");
            AddInfoRow(infoTable, "전체 성공률", multiReport.OverallSuccessRate);
            body.AppendChild(new Paragraph());
        }

        private void CreateFileDetailSection(Body body, ReportData reportData)
        {
            CreateSectionTitle(body, $"파일별 상세 결과: {reportData.FileName}");
            var summaryTable = body.AppendChild(new Table());
            ConfigureTable(summaryTable, new[] { "1500", "1500", "1500", "1500" });
            var headerRow = summaryTable.AppendChild(new TableRow());
            AddTableCell(headerRow, "전체 필드", true);
            AddTableCell(headerRow, "정상", true);
            AddTableCell(headerRow, "오류", true);
            AddTableCell(headerRow, "정상률", true);
            var dataRow = summaryTable.AppendChild(new TableRow());
            AddTableCell(dataRow, reportData.TotalCount.ToString());
            AddTableCell(dataRow, reportData.NormalCount.ToString(), false, "00B050");
            AddTableCell(dataRow, reportData.ErrorCount.ToString(), false, "C5504B");
            AddTableCell(dataRow, reportData.SuccessRate);
            body.AppendChild(new Paragraph());

            var detailTable = body.AppendChild(new Table());
            var colWidths = new[] { "800", "1000", "1200", "800", "700", "1000", "800", "700", "1000" };
            ConfigureTable(detailTable, colWidths);
            var detailHeaderRow = detailTable.AppendChild(new TableRow());
            var headers = new[] { "상태", "기준컬럼ID", "기준컬럼명", "기준타입", "기준길이", "찾은필드명", "파일타입", "파일길이", "비고" };
            foreach (var header in headers) { AddTableCell(detailHeaderRow, header, true); }

            foreach (var result in reportData.ValidationResults)
            {
                var detailDataRow = detailTable.AppendChild(new TableRow());
                var statusColor = result.Status == "정상" ? "00B050" : "C5504B";
                AddTableCell(detailDataRow, result.Status ?? "", false, statusColor);
                AddTableCell(detailDataRow, result.Std_ColumnId ?? "");
                AddTableCell(detailDataRow, result.Std_ColumnName ?? "");
                AddTableCell(detailDataRow, result.Std_Type ?? "");
                AddTableCell(detailDataRow, result.Std_Length ?? "");
                AddTableCell(detailDataRow, result.Found_FieldName ?? "");
                AddTableCell(detailDataRow, result.Cur_Type ?? "");
                AddTableCell(detailDataRow, result.Cur_Length ?? "");
                AddTableCell(detailDataRow, ReportData.GetRemarks(result));
            }
            body.AppendChild(new Paragraph());
        }

        private void CreateFooter(Body body)
        {
            var footerParagraph = body.AppendChild(new Paragraph());
            var footerRun = footerParagraph.AppendChild(new Run());
            footerRun.AppendChild(new RunProperties(new FontSize() { Val = "16" }, new RunFonts() { Ascii = "맑은 고딕", EastAsia = "맑은 고딕" }, new Color() { Val = "808080" }));
            footerRun.AppendChild(new Text($"보고서 생성: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | PureGIS GEO-QC v1.0 | {ExporterName}"));
            footerParagraph.ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Right }, new SpacingBetweenLines() { Before = "240" });
        }

        private void CreateSectionTitle(Body body, string title)
        {
            var titleParagraph = body.AppendChild(new Paragraph());
            var titleRun = titleParagraph.AppendChild(new Run());
            titleRun.AppendChild(new RunProperties(new Bold(), new FontSize() { Val = "20" }, new RunFonts() { Ascii = "맑은 고딕", EastAsia = "맑은 고딕" }, new Color() { Val = "1F497D" }));
            titleRun.AppendChild(new Text(title));
            titleParagraph.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { Before = "120", After = "120" });
        }

        private void ConfigureTable(Table table, string[] columnWidths)
        {
            var tableProps = table.AppendChild(new TableProperties());
            tableProps.AppendChild(new TableWidth() { Type = TableWidthUnitValues.Pct, Width = "5000" });
            tableProps.AppendChild(new TableBorders(new TopBorder() { Val = BorderValues.Single, Size = 6 }, new BottomBorder() { Val = BorderValues.Single, Size = 6 }, new LeftBorder() { Val = BorderValues.Single, Size = 6 }, new RightBorder() { Val = BorderValues.Single, Size = 6 }, new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 4 }, new InsideVerticalBorder() { Val = BorderValues.Single, Size = 4 }));
            var grid = table.AppendChild(new TableGrid());
            foreach (var width in columnWidths) { grid.AppendChild(new GridColumn() { Width = width }); }
        }

        private void AddInfoRow(Table table, string label, string value)
        {
            var row = table.AppendChild(new TableRow());
            AddTableCell(row, label, true, "F2F2F2");
            AddTableCell(row, value);
        }

        private void AddTableCell(TableRow row, string text, bool isHeader = false, string backgroundColor = null)
        {
            var cell = row.AppendChild(new TableCell());
            var paragraph = cell.AppendChild(new Paragraph());
            var run = paragraph.AppendChild(new Run());
            var runProps = new RunProperties();
            runProps.AppendChild(new RunFonts() { Ascii = "맑은 고딕", EastAsia = "맑은 고딕" });
            runProps.AppendChild(new FontSize() { Val = isHeader ? "20" : "18" });
            if (isHeader) { runProps.AppendChild(new Bold()); runProps.AppendChild(new Color() { Val = "FFFFFF" }); }
            else if (!string.IsNullOrEmpty(backgroundColor)) { runProps.AppendChild(new Color() { Val = "FFFFFF" }); }
            run.AppendChild(runProps);
            run.AppendChild(new Text(text));
            paragraph.ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center });
            var cellProps = cell.AppendChild(new TableCellProperties());
            if (isHeader) { cellProps.AppendChild(new Shading() { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "1F497D" }); }
            else if (!string.IsNullOrEmpty(backgroundColor)) { cellProps.AppendChild(new Shading() { Val = ShadingPatternValues.Clear, Color = "auto", Fill = backgroundColor }); }
            cellProps.AppendChild(new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center });
        }
    }
}