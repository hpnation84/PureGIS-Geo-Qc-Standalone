// 파일 경로: MainWindow/Events/MainWindow.Events.cs

using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PureGIS_Geo_QC.Licensing;
using PureGIS_Geo_QC.Managers;
using PureGIS_Geo_QC.WPF;

namespace PureGIS_Geo_QC_Standalone
{
    public partial class MainWindow
    {
        #region 타이틀바 및 메뉴 이벤트 핸들러
        /// <summary>
        /// 타이틀바 드래그로 창 이동
        /// </summary>
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        /// <summary>
        /// 최소화 버튼 클릭
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 최대화/복원 버튼 클릭
        /// </summary>
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }
        /// <summary>
        /// 닫기 버튼 클릭
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        // 메뉴 이벤트 핸들러 구현
        private void NewProjectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var result = CustomMessageBox.Show(this, "새 프로젝트", "새 프로젝트를 생성하시겠습니까?", true);
            if (result == true)
            {
                // TODO: 프로젝트명 입력 다이얼로그 표시
                var projectName = "이름 없는 프로젝트";
                CurrentProject = ProjectManager.CreateNewProject(projectName);
                CustomMessageBox.Show(this, "새 프로젝트", "새 프로젝트가 생성되었습니다. 상단의 입력란에 프로젝트명을 입력하고 저장하세요.");
            }
        }
        private void SaveProjectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentProject == null)
            {
                CustomMessageBox.Show(this, "오류", "저장할 프로젝트가 없습니다.");
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PureGIS 프로젝트 파일 (*.pgs)|*.pgs",
                DefaultExt = ".pgs"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ProjectManager.SaveProject(CurrentProject, saveFileDialog.FileName);
                    CustomMessageBox.Show(this, "완료", "프로젝트가 저장되었습니다.");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, "오류", ex.Message);
                }
            }
        }
        private void OpenProjectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "PureGIS 프로젝트 파일 (*.pgs)|*.pgs",
                DefaultExt = ".pgs"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    CurrentProject = ProjectManager.LoadProject(openFileDialog.FileName);
                    CustomMessageBox.Show(this, "완료", "프로젝트를 불러왔습니다.");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, "오류", ex.Message);
                }
            }
        }
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 종료 확인 메시지
            var result = CustomMessageBox.Show(this, "종료 확인", "프로그램을 종료하시겠습니까?", true);
            if (result == true)
            {
                this.Close();
            }
        }
        /// <summary>
        /// ✨ ----- 새로 추가하는 이벤트 핸들러 ----- ✨
        /// 엑셀에서 기준 가져오기 메뉴 클릭
        /// </summary>
        private void ImportFromExcelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel 파일 (*.xlsx;*.xls)|*.xlsx;*.xls",
                DefaultExt = ".xlsx"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // ExcelManager를 통해 프로젝트 데이터 불러오기
                    var importedProject = ExcelManager.ImportProjectFromExcel(openFileDialog.FileName);

                    // 현재 프로젝트에 병합 (기존 프로젝트가 있으면 덮어쓰기 확인)
                    if (CurrentProject != null && (CurrentProject.Categories.Any(c => c.Tables.Any()) || CurrentProject.CodeSets.Any()))
                    {
                        var result = CustomMessageBox.Show(this, "가져오기 확인", "현재 프로젝트에 엑셀 데이터를 덮어쓰시겠습니까?", true);
                        if (result != true) return; // '아니오'를 누르면 중단
                    }

                    // 현재 프로젝트를 불러온 데이터로 교체
                    CurrentProject = importedProject;

                    CustomMessageBox.Show(this, "완료", "엑셀에서 기준 정보를 성공적으로 가져왔습니다.");
                }
                catch (FileNotFoundException fnfEx)
                {
                    CustomMessageBox.Show(this, "파일 오류", $"파일을 찾을 수 없습니다: {fnfEx.Message}");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, "가져오기 오류", $"엑셀 파일을 읽는 중 오류가 발생했습니다: {ex.Message}");
                }
            }
        }


        /// <summary>
        /// 프로그램 정보 메뉴 클릭
        /// </summary>
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var aboutWindow = new AboutWindow
                {
                    Owner = this // 부모 창 설정
                };

                // LicenseManager 인스턴스에서 현재 라이선스 정보 가져오기
                var licenseManager = LicenseManager.Instance;

                // AboutWindow의 상태 업데이트 메서드 호출
                aboutWindow.UpdateLicenseStatus(
                    this.IsTrialMode,
                    licenseManager.IsLicenseValid,
                    licenseManager.CompanyName, // LicenseManager에 CompanyName, ExpiryDate 속성 추가 필요
                    licenseManager.ExpiryDate
                );

                aboutWindow.ShowDialog(); // 모달 창으로 표시
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(this, "오류", $"정보 창을 여는 중 오류가 발생했습니다: {ex.Message}");
            }
        }
        /// <summary>
        /// 엑셀 양식 다운로드 메뉴 클릭
        /// </summary>
        private void DownloadExcelTemplateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel 파일 (*.xlsx)|*.xlsx",
                FileName = "PureGIS_입력양식.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ExcelManager.CreateSampleExcelFile(saveFileDialog.FileName);
                    CustomMessageBox.Show(this, "완료", $"엑셀 양식 파일이 저장되었습니다.\n\n경로: {saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, "오류", $"엑셀 양식 파일을 생성하는 중 오류가 발생했습니다: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Ctrl+V로 데이터 붙여넣기
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            // {
            //     // 현재 활성화된 탭에 따라 대상 그리드를 선택하고 새 붙여넣기 함수 호출
            //     switch (MainTabControl.SelectedIndex)
            //     {
            //         case 0: // 기준 정의 탭
            //             HandleExcelPaste(StandardGrid);
            //             break;
            //         case 1: // 코드 관리 탭
            //             HandleExcelPaste(CodeDataGrid);
            //             break;
            //     }
            //     e.Handled = true; // 이벤트 처리 완료
            // }
        }
        #endregion
    }
}