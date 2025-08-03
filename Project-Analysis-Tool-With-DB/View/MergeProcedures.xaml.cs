using SpAnalyzerTool.Helper;
using SpAnalyzerTool.ProcedureMergeEngine;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;


namespace SpAnalyzerTool.View
{
   
    public partial class MergeProcedures : Window
    {

        private List<StoredProcedureInfo> mergedList;
        private StoredProcedureInfo spInfo;

        public MergeProcedures()
        {
            InitializeComponent();
        }


        private string? ShowBakDialog()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "SQL Backup Files (*.bak)|*.bak",
                Title = "اختر ملف bak"
            };

            return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.FileName : null;
        }

        private void BrowseBak1_Click(object sender, RoutedEventArgs e)
        {
            string? path = ShowBakDialog();
            if (path != null)
                txtBak1.Text = path;
        }
        private void BrowseBak2_Click(object sender, RoutedEventArgs e)
        {
            string? path = ShowBakDialog();
            if (path != null)
                txtBak2.Text = path;
        }


        private async void AnalyzeAndMerge_Click(object sender, RoutedEventArgs e)
        {
            string bakPath1 = txtBak1.Text.Trim();
            string bakPath2 = txtBak2.Text.Trim();

            if (!File.Exists(bakPath1) || !File.Exists(bakPath2))
            {
                MessageBox.Show("يرجى تحديد مساري الباك أب بشكل صحيح.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                txtSummary.Text = "⏳ جاري تحليل ودمج الإجراءات...";

                // توليد أسماء قواعد مؤقتة عشوائية
                string db1 = "TempMergeDb1_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                string db2 = "TempMergeDb2_" + Guid.NewGuid().ToString("N").Substring(0, 8);

                string masterConn = "Server=.;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

                // 1. استعادة قواعد البيانات
                await clsBakFileRestorer.RestoreBackupAsync(bakPath1, db1, masterConn);
                await clsBakFileRestorer.RestoreBackupAsync(bakPath2, db2, masterConn);

                string connStr1 = $"Server=.;Database={db1};Trusted_Connection=True;TrustServerCertificate=True;";
                string connStr2 = $"Server=.;Database={db2};Trusted_Connection=True;TrustServerCertificate=True;";

                // 2. تحميل الإجراءات من كل قاعدة
                var procs1 = await clsDatabaseHelper.LoadAllStoredProceduresAsync(connStr1, db1);
                var procs2 = await clsDatabaseHelper.LoadAllStoredProceduresAsync(connStr2, db2);





                //3. التحقق من ان ملفي الباك اب من نفس البيئة
                List<string> tableDiffs;
                if (!ProcedureComparer.AreTableSetsCompatible(procs1, procs2, out tableDiffs))
                {
                    MessageBox.Show("لا يمكن دمج ملفات الباك اب لأن الإجراءات تستخدم جداول مختلفة:\n" + string.Join("\n", tableDiffs),
                                    "خطأ في الدمج", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                grdBakResult.Visibility = Visibility.Visible;
                grbResult.Visibility= Visibility.Visible;
                btnSave.Visibility = Visibility.Visible;
                stNewBakName.Visibility = Visibility.Visible;


                // 4. دمج الإجراءات
                mergedList = StoredProcedureMerger.MergeProcedures(procs1, procs2);

                // 5. عرض النتائج في القوائم الثلاثة
                ListBoxLeft.ItemsSource = procs1;
                ListBoxRight.ItemsSource = procs2;
                MergedList.ItemsSource = mergedList;

                txtSummary.Text = $"✅ تم الدمج بنجاح.\n📂 من: {db1} و {db2}\n🔢 عدد الإجراءات المدمجة: {mergedList.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ حدث خطأ أثناء الدمج:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveMergedProcedures_Click(object sender, RoutedEventArgs e)
        {
            if (mergedList == null || mergedList.Count == 0)
            {
                MessageBox.Show("لا توجد إجراءات مدمجة لحفظها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string BakName = txtOutputBakName.Text.Trim();

            if (string.IsNullOrEmpty(BakName))
            {
                MessageBox.Show("يجب كتابة اسم ملف الباك اب المدمج", "!تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }



            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "احفظ نسخة احتياطية من الإجراءات",
                Filter = "Backup Files (*.bak)|*.bak|SQL Files (*.sql)|*.sql|All Files (*.*)|*.*",
                FileName = $"{BakName}_{DateTime.Now:yyyyMMdd_HHmmss}.bak"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();

                    foreach (var proc in mergedList)
                    {
                        sb.AppendLine("-- ================================================");
                        sb.AppendLine($"-- Procedure: {proc.Name}");
                        sb.AppendLine("-- ================================================");
                        sb.AppendLine(proc.Definition);
                        sb.AppendLine(); // فاصل
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("تم حفظ النسخة الاحتياطية بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"حدث خطأ أثناء الحفظ:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }
}
