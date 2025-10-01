using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PureGIS_Geo_QC.Models;

namespace PureGIS_Geo_QC.WPF
{
    /// <summary>
    /// ErrorListWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ErrorListWindow : Window
    {
        public ErrorListWindow(string title, List<ErrorDetail> errors)
        {
            InitializeComponent();
            TitleText.Text = title;
            ErrorListView.ItemsSource = errors;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
