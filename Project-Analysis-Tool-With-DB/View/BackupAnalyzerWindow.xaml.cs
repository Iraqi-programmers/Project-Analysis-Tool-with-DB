using Microsoft.Data.SqlClient;
using SpAnalyzerTool.Helper;
using SpAnalyzerTool.Models;
using SpAnalyzerTool.ViewModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SpAnalyzerTool.View
{
    public partial class BackupAnalyzerWindow : Window
    {
        private List<string> _extractedProcedures = new();
        private List<string> _UnusedProcedures = new();
        private DatabaseAnalyzerViewModel vm;

        public BackupAnalyzerWindow()
        {
            InitializeComponent();
            vm = new DatabaseAnalyzerViewModel();
            DataContext = vm;
        }

        private void BrowseBackup_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Backup Files (*.bak;*.file)|*.bak;*.file|All Files (*.*)|*.*";
            var result = dlg.ShowDialog();
            if (result == true)
            {
                txtBackupPath.Text = dlg.FileName;
                txtBackupSize.Text =  clsProjectAnalyzer.FormatBytes(clsProjectAnalyzer.GetFileSize(txtBackupPath.Text));

            }



        }

        private void BrowseProject_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                vm.ProjectPath = dlg.SelectedPath;
                txtProjectSize.Text =  clsProjectAnalyzer.FormatBytes(clsProjectAnalyzer.GetDirectorySize(vm.ProjectPath));

            }
        }

        private async void AnalyzeBackup_Click(object sender, RoutedEventArgs e)
        {
            if (vm.ObProject!.Count > 0)
                return;


            try
            {

                string sqlPath = txtBackupPath.Text;

                if (!File.Exists(sqlPath) || !Directory.Exists(vm.ProjectPath))
                {
                    MessageBox.Show("يرجى اختيار ملف باك أب ومسار مشروع صحيح.");
                    return;
                }

                txtStatus.Visibility = Visibility.Visible;

                string sqlInstance = "."; // اسم السيرفر المحلي
                string tempDbName = "TempRestoreDb_" + Guid.NewGuid().ToString("N").Substring(0, 8);

                var Procedures = await clsDatabaseHelper.LoadProceduresFromBakAsync(sqlInstance, sqlPath,tempDbName);
                if (Procedures == null)
                    return;

                var usageInfoList = clsProjectAnalyzer.MatchDatabaseProceduresInProject(vm.ProjectPath, Procedures.Select(s => s.Procedure).ToList()!);


                // بعد اكتمال التحليل
                grdUC.Visibility = Visibility.Visible;

                vm.ObProject = new(usageInfoList);
                vm.TotalCount = usageInfoList.Count;
                vm.UsedCount = usageInfoList.Count(x => x.Count > 0);
                vm.UnusedCount = usageInfoList.Count(x => x.Count == 0);


                // تخزين للعمليات الأخرى مثل التصدير أو الحذف
                _extractedProcedures = usageInfoList.Select(p => p.Procedure).ToList()!;
                _UnusedProcedures = usageInfoList.Where(p => p.Count == 0).Select(p => p.Procedure).ToList()!;

                // تحديث واجهة المستخدم
                foreach (var item in vm.ObProject)
                {
                    if (item.Count == 0)
                        item.IsSelectedForDelete = true;
                }


            }
            catch(Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء التحليل: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);

            }
            finally
            {
                txtStatus.Visibility = Visibility.Collapsed;

            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            vm.ToggleSelectAll();
        }
    }
}
