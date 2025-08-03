using SpAnalyzerTool.Helper;
using SpAnalyzerTool.Models;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace SpAnalyzerTool.View
{
   
    public partial class DeleteUnusedPrompt : Window
    {
        public string EnteredConnectionString { get; private set; } = string.Empty;

        public DeleteUnusedPrompt()
        {
            InitializeComponent();
            var settings = SettingsHelper.Load<AppSettings>("appsettings.json");
            txtConnectionString.Text = settings.DefaultConnectionString;

        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtConnectionString.Text))
            {
                MessageBox.Show("يرجى إدخال سلسلة اتصال صالحة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EnteredConnectionString = txtConnectionString.Text.Trim();

            var settings = new AppSettings
            {
                DefaultConnectionString = EnteredConnectionString
            };
            SettingsHelper.Save("appsettings.json", settings);

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
