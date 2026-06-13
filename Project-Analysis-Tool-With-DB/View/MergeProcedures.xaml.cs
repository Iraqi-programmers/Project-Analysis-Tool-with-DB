using SpAnalyzerTool.Helper;
using SpAnalyzerTool.ProcedureMergeEngine;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SpAnalyzerTool.View
{
    /// <summary>
    /// نافذة دمج الإجراءات المخزّنة من ملفّي نسخ احتياطي (.bak): تستعيد الملفين إلى
    /// قاعدتين مؤقتتين، تستخرج الإجراءات، تُصنّف الدمج (مفرد/متطابق/متعارض)، وتُولّد
    /// سكربت SQL موحّدًا. تُنظّف القواعد المؤقتة دائمًا بعد الانتهاء.
    /// </summary>
    public partial class MergeProcedures : Window
    {
        private List<MergedProcedure>? _merged;

        public MergeProcedures()
        {
            InitializeComponent();
        }

        /// <summary>يعرض مربع حوار اختيار ملف .bak ويعيد مساره أو null عند الإلغاء.</summary>
        private static string? ShowBakDialog()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "SQL Backup Files (*.bak)|*.bak|All Files (*.*)|*.*",
                Title = "اختر ملف النسخة الاحتياطية (.bak)"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        private void BrowseBak1_Click(object sender, RoutedEventArgs e)
        {
            string? path = ShowBakDialog();
            if (path != null) txtBak1.Text = path;
        }

        private void BrowseBak2_Click(object sender, RoutedEventArgs e)
        {
            string? path = ShowBakDialog();
            if (path != null) txtBak2.Text = path;
        }

        /// <summary>يستعيد الملفين، يستخرج الإجراءات، ويُنفّذ الدمج مع تنظيف مضمون للقواعد المؤقتة.</summary>
        private async void AnalyzeAndMerge_Click(object sender, RoutedEventArgs e)
        {
            string bakPath1 = txtBak1.Text.Trim();
            string bakPath2 = txtBak2.Text.Trim();

            // تحقّق من المدخلات قبل أي عمل.
            if (!File.Exists(bakPath1) || !File.Exists(bakPath2))
            {
                MessageBox.Show("يرجى تحديد مساري ملفّي الباك أب بشكل صحيح.", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.Equals(bakPath1, bakPath2, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("لا يمكن دمج ملف مع نفسه. اختر ملفين مختلفين.", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ResetResults();

            string masterConn = ConnectionStringFactory.Build(".", "master");
            string db1 = "TempMergeDb1_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string db2 = "TempMergeDb2_" + Guid.NewGuid().ToString("N").Substring(0, 8);

            btnAnalyze.IsEnabled = false;

            try
            {
                txtSummary.Text = "⏳ ... جاري استعادة الملفين وتحليل الإجراءات";

                // 1) استعادة كل ملف إلى قاعدة مؤقتة مستقلة.
                await clsBakFileRestorer.RestoreBackupAsync(bakPath1, db1, masterConn);
                await clsBakFileRestorer.RestoreBackupAsync(bakPath2, db2, masterConn);

                // 2) تحميل الإجراءات (الاسم + التعريف الكامل) من كل قاعدة.
                var procs1 = await clsDatabaseHelper.LoadAllStoredProceduresAsync(
                    ConnectionStringFactory.Build(".", db1), db1);
                var procs2 = await clsDatabaseHelper.LoadAllStoredProceduresAsync(
                    ConnectionStringFactory.Build(".", db2), db2);

                // 3) الدمج والتصنيف.
                _merged = ProcedureMergeService.Merge(procs1, procs2);

                // 4) عرض النتائج.
                ListBoxLeft.ItemsSource = procs1;
                ListBoxRight.ItemsSource = procs2;
                MergedList.ItemsSource = _merged;

                grdBakResult.Visibility = Visibility.Visible;
                grbResult.Visibility = Visibility.Visible;
                btnSave.Visibility = Visibility.Visible;
                stNewBakName.Visibility = Visibility.Visible;

                int identical = _merged.Count(m => m.Status == MergeStatus.Identical);
                int conflicts = _merged.Count(m => m.Status == MergeStatus.Conflict);

                txtSummary.Text =
                    $"✅ تم التحليل بنجاح | الإجمالي: {procs1.Count + procs2.Count} • الملف الأول: {procs1.Count} • " +
                    $"الملف الثاني: {procs2.Count} • متطابق: {identical} • تعارض: {conflicts}";
            }
            catch (Exception ex)
            {
                txtSummary.Text = "❌ فشل الدمج.";
                MessageBox.Show($"حدث خطأ أثناء الدمج:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // تنظيف مضمون للقواعد المؤقتة وملفاتها (التعريفات محمّلة في الذاكرة بالفعل).
                await clsBakFileRestorer.DropTempDatabaseAsync(masterConn, db1);
                await clsBakFileRestorer.DropTempDatabaseAsync(masterConn, db2);
                btnAnalyze.IsEnabled = true;
            }
        }

        /// <summary>يحفظ الإجراءات المُضمَّنة كسكربت SQL (CREATE OR ALTER) محترِمًا اختيارات المستخدم.</summary>
        private async void SaveMergedProcedures_Click(object sender, RoutedEventArgs e)
        {
            if (_merged == null || _merged.Count == 0)
            {
                MessageBox.Show("لا توجد نتائج دمج لحفظها.", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var included = _merged.Where(m => m.IsIncluded).ToList();
            if (included.Count == 0)
            {
                MessageBox.Show("لم يتم تضمين أي إجراء. فعّل خانة 'تضمين' لإجراء واحد على الأقل.", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string baseName = txtOutputBakName.Text.Trim();
            if (string.IsNullOrEmpty(baseName))
            {
                MessageBox.Show("يرجى كتابة اسم لملف الناتج.", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            baseName = Path.GetFileNameWithoutExtension(baseName);

            var dialog = new SaveFileDialog
            {
                Title = "احفظ سكربت الإجراءات المدموجة",
                Filter = "SQL Script (*.sql)|*.sql|All Files (*.*)|*.*",
                FileName = $"{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}.sql"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                string script = ProcedureMergeService.BuildMergeScript(included);
                await File.WriteAllTextAsync(dialog.FileName, script, new UTF8Encoding(true));

                MessageBox.Show($"✅ تم حفظ السكربت بنجاح ({included.Count} إجراء):\n{dialog.FileName}",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الحفظ:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>يُعيد الواجهة إلى حالتها الأولية قبل دمج جديد.</summary>
        private void ResetResults()
        {
            _merged = null;
            ListBoxLeft.ItemsSource = null;
            ListBoxRight.ItemsSource = null;
            MergedList.ItemsSource = null;
            grdBakResult.Visibility = Visibility.Collapsed;
            grbResult.Visibility = Visibility.Collapsed;
            btnSave.Visibility = Visibility.Collapsed;
            stNewBakName.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>يُلوّن عناصر القائمتين المصدريتين حسب الملف الذي أتت منه (لتمييز بصري).</summary>
    public class SourceToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StoredProcedureInfo info && info.SourceDatabase != null)
            {
                if (info.SourceDatabase.Contains("TempMergeDb1_"))
                    return new SolidColorBrush(Color.FromRgb(0x34, 0x40, 0x6B)); // أزرق داكن (الملف الأول)
                if (info.SourceDatabase.Contains("TempMergeDb2_"))
                    return new SolidColorBrush(Color.FromRgb(0x23, 0x4E, 0x42)); // أخضر داكن (الملف الثاني)
            }
            return new SolidColorBrush(Color.FromRgb(0x2F, 0x33, 0x46));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>يحوّل حالة الدمج إلى لون نص دلالي في قائمة النتائج.</summary>
    public class MergeStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MergeStatus status)
            {
                return status switch
                {
                    MergeStatus.OnlyInFirst => new SolidColorBrush(Color.FromRgb(0x6B, 0xB0, 0xFF)),  // أزرق
                    MergeStatus.OnlyInSecond => new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99)), // أخضر
                    MergeStatus.Identical => new SolidColorBrush(Color.FromRgb(0x9A, 0xA0, 0xB4)),    // رمادي
                    MergeStatus.Conflict => new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)),     // برتقالي
                    _ => new SolidColorBrush(Color.FromRgb(0xE6, 0xE8, 0xEF))
                };
            }
            return new SolidColorBrush(Color.FromRgb(0xE6, 0xE8, 0xEF));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
