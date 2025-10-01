using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using PureGIS_Geo_QC.Exports.Models;
using PureGIS_Geo_QC.Models;

namespace PureGIS_Geo_QC.WPF
{
    /// <summary>
    /// Boolean 값을 체크마크 문자로 변환하는 컨버터
    /// </summary>
    public class BoolToCheckMarkConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. 먼저 값이 null인지 확인합니다. (검사 안 함 상태)
            if (value == null)
            {
                return "-";
            }

            // 2. 값이 bool 타입인지 확인합니다.
            if (value is bool booleanValue)
            {
                // true이면 ✓, false이면 ✗
                return booleanValue ? "✓" : "✗";
            }

            // 예외적인 경우 기본값
            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ColumnValidationResult를 비고 메시지로 변환하는 컨버터
    /// </summary>
    public class RemarksConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ColumnValidationResult result)
            {
                // Remarks 생성 로직을 ReportData 클래스에서 가져와 일관성을 유지합니다.
                var remarks = new List<string>();

                if (result.IsFieldFound == false) remarks.Add("필드없음");
                if (result.IsTypeCorrect == false) remarks.Add("타입불일치");
                if (result.IsLengthCorrect == false) remarks.Add("길이불일치");
                if (result.IsNotNullCorrect == false) remarks.Add($"NULL({result.NotNullErrorCount}개)");
                if (result.IsCodeCorrect == false) remarks.Add($"코드({result.CodeErrorCount}개)");


                return string.Join(", ", remarks);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}