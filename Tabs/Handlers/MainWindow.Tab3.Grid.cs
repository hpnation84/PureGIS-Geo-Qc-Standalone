// 파일 경로: Tabs/Handlers/MainWindow.Tab3.Grid.cs

using PureGIS_Geo_QC.Models;
using PureGIS_Geo_QC.WPF;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PureGIS_Geo_QC_Standalone
{
    // MainWindow 클래스를 나누어 관리하기 위한 partial 클래스입니다.
    public partial class MainWindow
    {
        /// <summary>
        /// ResultGrid에서 셀을 더블클릭했을 때의 이벤트 핸들러입니다.
        /// </summary>
        private void ResultGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 선택된 셀이 없으면 아무것도 하지 않습니다.
            if (!(sender is DataGrid grid) || grid.SelectedItem == null) return;

            // 선택된 행(ColumnValidationResult)과 셀 정보를 가져옵니다.
            var selectedResult = grid.SelectedItem as ColumnValidationResult;
            var selectedCell = grid.CurrentCell;

            if (selectedResult == null || !selectedCell.IsValid) return;

            string header = selectedCell.Column.Header.ToString();
            string windowTitle = "";
            System.Collections.Generic.List<ErrorDetail> errorList = null;

            // 더블클릭한 컬럼이 'NULL오류' 또는 '코드오류'인지 확인
            if (header == "NULL오류" && selectedResult.NullErrorValues.Any())
            {
                windowTitle = $"'{selectedResult.Std_ColumnId}' - NULL 오류 목록";
                errorList = selectedResult.NullErrorValues; // ✨ 이미 List<ErrorDetail> 타입
            }
            else if (header == "코드오류" && selectedResult.CodeErrorValues.Any())
            {
                windowTitle = $"'{selectedResult.Std_ColumnId}' - 코드 불일치 오류 목록";
                errorList = selectedResult.CodeErrorValues; // ✨ 이미 List<ErrorDetail> 타입
            }

            // 보여줄 오류 목록이 있으면 새 창을 띄웁니다.
            if (errorList != null)
            {
                var errorWindow = new ErrorListWindow(windowTitle, errorList)
                {
                    Owner = this
                };
                errorWindow.ShowDialog();
            }
        }
    }
}