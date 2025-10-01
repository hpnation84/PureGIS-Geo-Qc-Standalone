using PureGIS_Geo_QC.Models;
using PureGIS_Geo_QC.WPF;
using System.Linq;
using System.Windows;

namespace PureGIS_Geo_QC_Standalone
{
    public partial class MainWindow
    {
        /// <summary>
        /// 새 컬럼(행)을 추가합니다.
        /// </summary>
        private void AddColumnButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedTable == null)
            {
                CustomMessageBox.Show(this, "알림", "컬럼을 추가할 테이블을 먼저 선택하세요.");
                return;
            }

            currentSelectedTable.Columns.Add(new ColumnDefinition
            {
                ColumnId = "NEW_COLUMN",
                ColumnName = "새 컬럼",
                Type = "VARCHAR2",
                Length = "100"
            });

            // 마지막 행으로 스크롤
            if (StandardGrid.Items.Count > 0)
            {
                StandardGrid.ScrollIntoView(StandardGrid.Items[StandardGrid.Items.Count - 1]);
            }
        }

        /// <summary>
        /// 선택한 컬럼(행)을 삭제합니다.
        /// </summary>
        private void DeleteColumnButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedTable == null || StandardGrid.SelectedCells.Count == 0)
            {
                CustomMessageBox.Show(this, "알림", "삭제할 컬럼(셀 또는 행)을 먼저 선택하세요.");
                return;
            }

            // 1. 선택된 셀들로부터 중복되지 않는 행(ColumnDefinition) 목록을 가져옵니다.
            var rowsToRemove = StandardGrid.SelectedCells
                .Select(cellInfo => cellInfo.Item as ColumnDefinition)
                .Where(item => item != null)
                .Distinct()
                .ToList();

            if (rowsToRemove.Count == 0)
            {
                CustomMessageBox.Show(this, "알림", "삭제할 컬럼을 선택하세요.");
                return;
            }

            if (CustomMessageBox.Show(this, "삭제 확인", $"{rowsToRemove.Count}개의 컬럼을 삭제하시겠습니까?", true) == true)
            {
                // 2. 추출된 목록을 기반으로 컬렉션에서 항목을 삭제합니다.
                foreach (var item in rowsToRemove)
                {
                    currentSelectedTable.Columns.Remove(item);
                }
                // 테이블 정보(컬럼 개수)를 새로고침합니다.
                ShowTableInfo(currentSelectedTable);
            }
        }

        /// <summary>
        /// 선택한 컬럼(행)을 위로 이동합니다.
        /// </summary>
        private void MoveColumnUp_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedTable == null || StandardGrid.SelectedItem == null) return;

            var selectedColumn = StandardGrid.SelectedItem as ColumnDefinition;
            int currentIndex = currentSelectedTable.Columns.IndexOf(selectedColumn);

            if (currentIndex > 0)
            {
                currentSelectedTable.Columns.RemoveAt(currentIndex);
                currentSelectedTable.Columns.Insert(currentIndex - 1, selectedColumn);
                StandardGrid.SelectedItem = selectedColumn; // 이동 후에도 선택 유지
            }
        }

        /// <summary>
        /// 선택한 컬럼(행)을 아래로 이동합니다.
        /// </summary>
        private void MoveColumnDown_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedTable == null || StandardGrid.SelectedItem == null) return;

            var selectedColumn = StandardGrid.SelectedItem as ColumnDefinition;
            int currentIndex = currentSelectedTable.Columns.IndexOf(selectedColumn);

            if (currentIndex < currentSelectedTable.Columns.Count - 1)
            {
                currentSelectedTable.Columns.RemoveAt(currentIndex);
                currentSelectedTable.Columns.Insert(currentIndex + 1, selectedColumn);
                StandardGrid.SelectedItem = selectedColumn; // 이동 후에도 선택 유지
            }
        }
    }
}