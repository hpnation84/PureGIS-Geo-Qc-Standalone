using System;
using System.Collections.Generic;
using System.Linq; // .ToList()를 사용하기 위해 추가
using PureGIS_Geo_QC.Models;

namespace PureGIS_Geo_QC.Exports.Models
{
    /// <summary>
    /// 보고서 생성에 필요한 데이터를 담는 클래스
    /// </summary>
    public class ReportData
    {
        public DateTime ReportDate { get; set; } = DateTime.Now;
        public string FileName { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public List<ColumnValidationResult> ValidationResults { get; set; } = new List<ColumnValidationResult>();
        public int TotalCount => ValidationResults?.Count ?? 0;
        public int NormalCount => ValidationResults?.Count(r => r.Status == "정상") ?? 0;
        public int ErrorCount => ValidationResults?.Count(r => r.Status == "오류") ?? 0;

        public string SuccessRate
        {
            get
            {
                if (TotalCount == 0) return "0%";
                return ((double)NormalCount / TotalCount * 100).ToString("F1") + "%";
            }
        }

        /// <summary>
        /// 비고 정보 생성 (오류 개수 포함 버전)
        /// </summary>
        public static string GetRemarks(ColumnValidationResult result)
        {
            if (result == null) return "";

            var remarks = new List<string>();

            if (result.IsFieldFound == false) remarks.Add("필드없음");
            if (result.IsTypeCorrect == false) remarks.Add("타입불일치");
            if (result.IsLengthCorrect == false) remarks.Add("길이불일치");
            if (result.IsNotNullCorrect == false) remarks.Add($"NULL({result.NotNullErrorCount}개)");
            if (result.IsCodeCorrect == false) remarks.Add($"코드({result.CodeErrorCount}개)");

            return string.Join(", ", remarks);
        }
    }
}