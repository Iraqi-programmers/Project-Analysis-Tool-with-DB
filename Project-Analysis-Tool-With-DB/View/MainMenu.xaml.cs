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
            dbWindow.ShowDialog();
        }

        private void BackupMode_Click(object sender, RoutedEventArgs e)
        {
            var backupWindow = new BackupAnalyzerWindow();
            backupWindow.ShowDialog();
            
        }

        private void OpenMergePage_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("This feature is under development. Please check back later.", "Under Development", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
            var mergeWindow = new MergeProcedures();
            mergeWindow.ShowDialog();
        }

        private void ShowHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpViewer = new HelpViewer();
            helpViewer.ShowDialog();
        }
    }
}
