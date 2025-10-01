using System;
using System.Collections.Generic;
using System.Linq;
using PureGIS_Geo_QC.Models;

namespace PureGIS_Geo_QC.Helpers
{
    /// <summary>
    /// 클립보드 데이터 파싱 관련 헬퍼 클래스
    /// </summary>
    public static class ClipboardHelper
    {
        /// <summary>
        /// 클립보드에서 복사한 그리드 형태의 텍스트를 파싱하여 2차원 문자열 리스트로 반환합니다.
        /// (예: 엑셀, 데이터 그리드에서 복사한 데이터)
        /// </summary>
        /// <param name="clipboardText">Clipboard.GetText()로 얻어온 원본 텍스트</param>
        /// <returns>각 행(row)을 string[]로 담고 있는 리스트</returns>
        public static List<string[]> ParseClipboardGridData(string clipboardText)
        {
            // 최종 결과를 담을 리스트를 생성합니다.
            var result = new List<string[]>();

            // 클립보드 데이터가 비어있으면 아무것도 하지 않고 빈 리스트를 반환합니다.
            if (string.IsNullOrEmpty(clipboardText))
            {
                return result;
            }

            // 1. 먼저 텍스트를 줄 바꿈 문자 기준으로 나누어 각 '행'을 분리합니다.
            //    \r\n (Windows)과 \n (Unix/Mac)을 모두 처리하기 위해 배열로 지정합니다.
            var rows = clipboardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // 2. 각 행을 순회하면서 처리합니다.
            foreach (var row in rows)
            {
                // 마지막 줄이 비어있는 경우가 많으므로, 빈 행은 건너뜁니다.
                if (string.IsNullOrEmpty(row))
                {
                    continue;
                }

                // 3. 한 개의 행(row)을 탭(\t) 문자를 기준으로 나누어 '열'들을 분리합니다.
                var columns = row.Split('\t');

                // 4. 분리된 열 데이터를 최종 결과 리스트에 추가합니다.
                result.Add(columns);
            }

            return result;
        }
        /// <summary>
        /// 클립보드의 텍스트를 파싱하여 ColumnDefinition 객체의 리스트로 변환합니다.
        /// 각 라인은 하나의 컬럼을 의미하며, 각 속성은 탭으로 구분됩니다.
        /// </summary>
        /// <param name="clipboardText">클립보드에서 가져온 텍스트</param>
        /// <returns>파싱된 ColumnDefinition 객체 리스트</returns>
        public static List<ColumnDefinition> ParseColumnsFromClipboard(string clipboardText)
        {
            var newColumns = new List<ColumnDefinition>();

            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                return newColumns;
            }

            // 1. 전체 텍스트를 줄바꿈 기준으로 나누어 한 줄씩 처리합니다.
            var lines = clipboardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // 2. 각 줄을 탭(\t) 기준으로 나누어 속성 값들을 분리합니다.
                var parts = line.Split('\t');

                // 3. 형식에 맞는지 최소한의 개수를 확인합니다. (최소 5개: ID, 이름, 타입, 길이, Null여부)
                if (parts.Length < 5)
                {
                    continue; // 형식이 맞지 않는 줄은 건너뜁니다.
                }

                try
                {
                    // 4. 분리된 데이터를 사용하여 ColumnDefinition 객체를 생성합니다.
                    var column = new ColumnDefinition
                    {
                        // 각 부분의 앞뒤 공백을 제거(Trim)하여 깔끔하게 저장합니다.
                        ColumnId = parts[0].Trim(),
                        ColumnName = parts[1].Trim(),
                        Type = parts[2].Trim(),
                        Length = parts[3].Trim(),

                        // "Y" 또는 "y"일 경우 true로 설정합니다.
                        IsNotNull = parts[4].Trim().Equals("Y", StringComparison.OrdinalIgnoreCase),

                        // 코드명은 선택적 값이므로, 6번째 값이 있을 경우에만 할당합니다.
                        CodeName = parts.Length > 5 ? parts[5].Trim() : string.Empty
                    };

                    newColumns.Add(column);
                }
                catch (Exception)
                {
                    // 특정 라인 파싱 중 오류가 발생하더라도 전체 프로세스가 중단되지 않도록
                    // 해당 라인만 건너뛰고 계속 진행합니다.
                    continue;
                }
            }

            return newColumns;
        }
        /// <summary>
        /// 단일 테이블 텍스트를 TableDefinition으로 파싱
        /// </summary>
        /// <param name="tableText">테이블 텍스트</param>
        /// <returns>파싱된 TableDefinition</returns>
        private static TableDefinition ParseSingleTable(string tableText)
        {
            var lines = tableText.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 3) return null;

            var newTable = new TableDefinition();

            // 첫 번째 줄에서 TableId 안전하게 추출
            if (lines.Length > 0 && lines[0].Contains(':'))
            {
                var tableParts = lines[0].Split(':');
                newTable.TableId = tableParts.Length > 1 ? tableParts[1].Trim() : "Unknown";
            }
            else
            {
                newTable.TableId = "Table_" + DateTime.Now.Ticks.ToString().Substring(0, 6);
            }

            // 두 번째 줄에서 TableName 안전하게 추출
            if (lines.Length > 1 && lines[1].Contains(':'))
            {
                var nameParts = lines[1].Split(':');
                newTable.TableName = nameParts.Length > 1 ? nameParts[1].Trim() : "Unknown Table";
            }
            else
            {
                newTable.TableName = "테이블_" + newTable.TableId;
            }

            // 나머지 줄들에서 컬럼 정보 추출
            for (int i = 2; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var cols = lines[i].Split('\t');
                if (cols.Length < 2) continue; // 최소 2개 컬럼은 있어야 함

                newTable.Columns.Add(new ColumnDefinition
                {
                    ColumnId = GetSafeArrayValue(cols, 0, "COL_" + i),
                    ColumnName = GetSafeArrayValue(cols, 1, "컬럼_" + i),
                    Type = GetSafeArrayValue(cols, 2, "VARCHAR2"),
                    Length = GetSafeArrayValue(cols, 3, "50"),
                    IsNotNull = GetSafeArrayValue(cols, 4, "N").ToUpper() == "Y",
                    KeyType = GetSafeArrayValue(cols, 5, "")
                });
            }

            return newTable;
        }

        /// <summary>
        /// 단순한 엑셀 데이터 (헤더 + 데이터 행들) 파싱
        /// </summary>
        /// <param name="clipboardText">클립보드 텍스트</param>
        /// <returns>파싱된 TableDefinition</returns>
        private static TableDefinition ParseSimpleExcelData(string clipboardText)
        {
            var lines = clipboardText.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return null; // 최소 헤더 + 1개 행은 있어야 함

            // 첫 번째 줄이 헤더인지 확인 (컬럼ID, 컬럼명, 타입, 길이 등의 헤더가 있는지)
            var headerLine = lines[0];
            var isHeaderRow = headerLine.ToLower().Contains("컬럼") ||
                              headerLine.ToLower().Contains("column") ||
                              headerLine.ToLower().Contains("타입") ||
                              headerLine.ToLower().Contains("type");

            var newTable = new TableDefinition
            {
                TableId = "EXCEL_TABLE_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                TableName = "엑셀에서 가져온 테이블"
            };

            int startRow = isHeaderRow ? 1 : 0; // 헤더가 있으면 1번째 줄부터, 없으면 0번째 줄부터

            for (int i = startRow; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var cols = lines[i].Split('\t');
                if (cols.Length < 2) continue;

                newTable.Columns.Add(new ColumnDefinition
                {
                    ColumnId = GetSafeArrayValue(cols, 0, "COL_" + (i - startRow + 1)),
                    ColumnName = GetSafeArrayValue(cols, 1, "컬럼_" + (i - startRow + 1)),
                    Type = GetSafeArrayValue(cols, 2, "VARCHAR2"),
                    Length = GetSafeArrayValue(cols, 3, "50"),
                    IsNotNull = GetSafeArrayValue(cols, 4, "N").ToUpper() == "Y",
                    KeyType = GetSafeArrayValue(cols, 5, "")
                });
            }

            return newTable;
        }

        /// <summary>
        /// 배열에서 안전하게 값을 가져오는 헬퍼 함수
        /// </summary>
        /// <param name="array">대상 배열</param>
        /// <param name="index">인덱스</param>
        /// <param name="defaultValue">기본값</param>
        /// <returns>안전하게 추출된 값</returns>
        public static string GetSafeArrayValue(string[] array, int index, string defaultValue)
        {
            if (array != null && index >= 0 && index < array.Length)
            {
                return string.IsNullOrWhiteSpace(array[index]) ? defaultValue : array[index].Trim();
            }
            return defaultValue;
        }
    }
}