using SpAnalyzerTool.View;
using SpAnalyzerTool.View.UserControl;
using System.Windows;

namespace SpAnalyzerTool
{
    /// <summary>
    /// Interaction logic for MainMenu.xaml
    /// </summary>
    public partial class MainMenu : Window
    {
        public MainMenu()
        {
            InitializeComponent();
        }

        private void DatabaseMode_Click(object sender, RoutedEventArgs e)
        {
            var dbWindow = new DatabaseAnalyzerWindow();
            dbWindow.Show();
            this.Close();
        }

        private void BackupMode_Click(object sender, RoutedEventArgs e)
        {
            var backupWindow = new BackupAnalyzerWindow();
            backupWindow.Show();
            this.Close();
        }

        private void OpenMergePage_Click(object sender, RoutedEventArgs e)
        {
            var mergeWindow = new MergeProcedures();
            mergeWindow.ShowDialog();
         //  this.Content=  mergeWindow.NavigationService;
        }

        private void ShowHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpViewer = new HelpViewer();
            helpViewer.ShowDialog();
        }
    }
}
