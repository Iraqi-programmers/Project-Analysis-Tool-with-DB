using Newtonsoft.Json;
using SpAnalyzerTool.Models;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Brushes = System.Windows.Media.Brushes;
using Orientation = System.Windows.Controls.Orientation;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace SpAnalyzerTool.View
{
   
    public partial class HelpViewer : Window
    {
        public HelpViewer()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string jsonPath = "SettingesFiles\\HelpContent.json";
            if (!File.Exists(jsonPath))
                return;

            var json = File.ReadAllText(jsonPath);
            var helpContent = JsonConvert.DeserializeObject<HelpContentModel>(json);
            if (helpContent == null) return;

            // 1. عنوان شرح البرنامج
            txtTitle.Text = helpContent.Title;

            // 2. عناصر شرح الوظائف
            foreach (var item in helpContent.Items!)
            {
                var title = new TextBlock
                {
                    Text = item.Title,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DarkBlue,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var description = new TextBlock
                {
                    Text = item.Description,
                    TextWrapping = TextWrapping.Wrap
                };

                stackPanelContent.Children.Add(title);
                stackPanelContent.Children.Add(description);
            }

            // 3. عنوان قسم التواصل
            var contactHeader = new TextBlock
            {
                Text = "📞 تواصل معنا:",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkBlue,
                Margin = new Thickness(0, 20, 0, 10)
            };
            stackPanelContent.Children.Add(contactHeader);

            // 4. تفاصيل الاتصال
            foreach (var detail in helpContent?.Contact?.Details!)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };

                var icon = new TextBlock
                {
                    Text = detail.Icon,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                panel.Children.Add(icon);

                if (!string.IsNullOrWhiteSpace(detail.Link))
                {
                    var hyperlinkText = new TextBlock { VerticalAlignment = VerticalAlignment.Center };

                    var hyperlink = new Hyperlink
                    {
                        NavigateUri = new Uri(detail.Link)
                    };
                    hyperlink.Inlines.Add(detail.Text);
                    hyperlink.RequestNavigate += (s, e) =>
                    {
                        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                    };

                    hyperlinkText.Inlines.Add(hyperlink);
                    panel.Children.Add(hyperlinkText);
                }
                else
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = detail.Text,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                stackPanelContent.Children.Add(panel);
            }

            var versionText = new TextBlock
            {
                Text = helpContent.Version,
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 3, 0)
            };

            stackPanelContent.Children.Add(versionText);

            
        }
    }
}
