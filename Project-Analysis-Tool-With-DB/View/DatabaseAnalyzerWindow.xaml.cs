using Microsoft.Data.Sql;
using Microsoft.Data.SqlClient;
using SpAnalyzerTool.Helper;
using SpAnalyzerTool.Models;
using SpAnalyzerTool.ViewModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using userControl = System.Windows.Controls.UserControl;



namespace SpAnalyzerTool.View
{


    //public partial class DatabaseAnalyzerWindow : Window 
    //{
    //    private List<string> _extractedProcedures;
    //    private List<string> _UnusedProcedures;
    //    private AppSettings settings;


    //    private DatabaseAnalyzerViewModel vm;

    //    public DatabaseAnalyzerWindow()
    //    {
    //        InitializeComponent();

    //        settings = SettingsHelper.Load<AppSettings>("SettingesFiles\\appsettings.json");
    //       // obProgect = new();

    //        vm = new DatabaseAnalyzerViewModel(settings);
    //        DataContext = vm;

    //        // تمرير عرض الأوفرلاي
    //        vm.ShowOverlayAction = ShowOverlayControl;

    //        // تمرير إجراء التحديث بعد الإضافة
    //        vm.AnalyzeProjectAction = () =>
    //        {
    //            Analyze_Click(this, new RoutedEventArgs());
    //        };



    //        _extractedProcedures = new();
    //        _UnusedProcedures = new();

    //    }

    //    private async void Window_Loaded(object sender, RoutedEventArgs e)
    //    {
    //        // إذا كان الملف يحتوي على سيرفر محفوظ، نضعه مباشرة في الكومبو
    //        if (!string.IsNullOrWhiteSpace(settings.DefaultConnectionString))
    //        {
    //            try
    //            {
    //                var builder = new SqlConnectionStringBuilder(settings.DefaultConnectionString);
    //                string savedServer = builder.DataSource;
    //                settings.DefaultConnectionString = builder.ConnectionString;
    //                cmbServers.ItemsSource = null;
    //                cmbServers.Items.Clear();
    //                cmbServers.IsEditable = true;
    //                cmbServers.Items.Add(savedServer);
    //                cmbServers.SelectedIndex = 0;
    //                txtBackupSize.Text = settings.FileSize;
    //                LoadDatabasesForSelectedServer(null, null);
    //            }
    //            catch
    //            {
    //                // سلسلة الاتصال غير صالحة → لا تفعل شيء
    //            }
    //        }
    //        else
    //        {
    //            // إذا لا يوجد سيرفر محفوظ → نبدأ بالبحث تلقائيًا
    //            await LoadAvailableSqlServersAsync();
    //        }
    //    }

    //    private void ShowOverlayControl(userControl? control)
    //    {
    //        OverlayContainer.Children.Clear();

    //        if (control == null)
    //        {
    //            OverlayContainer.Visibility = Visibility.Collapsed;
    //            return;
    //        }

    //        var container = new Border
    //        {
    //            Style = (Style)FindResource("OverlayEditorContainerStyle"),
    //            Child = control
    //        };

    //        OverlayContainer.Children.Add(container);
    //        OverlayContainer.Visibility = Visibility.Visible;
    //    }

    //    // تحميل السيرفرات المتاحة في الخلفية
    //    private async Task LoadAvailableSqlServersAsync()
    //    {
    //        try
    //        {
    //            cmbServers.IsEnabled = false;
    //            cmbServers.ItemsSource = null;
    //            cmbServers.Items.Clear();
    //            cmbServers.Text = string.Empty;
    //            cmbServers.IsEditable = true;

    //            // رسالة انتظار مؤقتة
    //            cmbServers.Items.Add("🔄 جارٍ البحث عن السيرفرات...");
    //            cmbServers.SelectedIndex = 0;

    //            // استخراج السيرفرات في الخلفية
    //            var serverNames = await Task.Run(() =>
    //            {
    //                var result = new List<string>();
    //                try
    //                {
    //                    var table = SqlDataSourceEnumerator.Instance.GetDataSources();
    //                    foreach (DataRow row in table.Rows)
    //                    {
    //                        string server = row["ServerName"].ToString()!;
    //                        string? instance = row["InstanceName"]?.ToString();

    //                        result.Add(string.IsNullOrEmpty(instance) ? server : $"{server}\\{instance}");
    //                    }
    //                }
    //                catch
    //                {
    //                    // تجاهل أي خطأ داخلي
    //                }
    //                return result;
    //            });

    //            cmbServers.Items.Clear(); // ✅ مهم لتجنب الخطأ
    //            cmbServers.ItemsSource = serverNames;

    //            // ✅ إذا لا توجد نتائج، المستخدم سيكتب الاسم يدوياً
    //            if (serverNames.Count == 0)
    //            {
    //                cmbServers.IsEditable = true;
    //                cmbServers.ItemsSource = null;
    //                cmbServers.Items.Add("❗ لم يتم العثور على سيرفرات، اكتب الاسم يدويًا");
    //                cmbServers.SelectedIndex = 0;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            MessageBox.Show($"❌ فشل أثناء تحميل السيرفرات:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
    //        }
    //        finally
    //        {
    //            cmbServers.IsEnabled = true;
    //            btnLoadDataBases.IsEnabled = true;
    //            btnRefreshServers.IsEnabled = true;
    //        }
    //    }

    //    // إعادة تحميل السيرفرات 
    //    private async void ReloadServers_Click(object sender, RoutedEventArgs e)
    //    {
    //        // تعطيل الأزرار
    //        btnLoadDataBases.IsEnabled = false;
    //        btnRefreshServers.IsEnabled = false;

    //        //تفريغ النصوص
    //        if (!string.IsNullOrWhiteSpace(vm.ProjectPath)||!string.IsNullOrEmpty(cmbDatabases.Text))
    //        {
    //            txtProjectSize.Text = "";
    //            txtBackupSize.Text = "";
    //            txtProjectSize.Text = "";

    //        }


    //        // تفريغ قواعد البيانات
    //        cmbDatabases.ItemsSource = null;
    //        cmbDatabases.Items.Clear();
    //        cmbDatabases.Text = string.Empty;
    //        cmbDatabases.IsEnabled = false;

    //        settings.DefaultConnectionString = string.Empty;
    //        SettingsHelper.Save("SettingesFiles\\appsettings.json", settings);


    //        // إخفاء النتائج إن وُجدت
    //        //lstResults.ItemsSource = null;
    //        //lstResults.Visibility = Visibility.Collapsed;
    //        //spSummary.Visibility = Visibility.Collapsed;
    //        //btnExport.Visibility = Visibility.Collapsed;

    //        await LoadAvailableSqlServersAsync();
    //    }

    //    // تحميل قواعد البيانات عند اختيار سيرفر
    //    private async void LoadDatabasesForSelectedServer(object sender, RoutedEventArgs e)
    //    {
    //        if (cmbDatabases.ItemsSource is List<string> existingList && existingList.Count > 0)
    //        {
    //             MessageBox.Show("✅ قواعد البيانات محمّلة بالفعل.", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
    //            return;
    //        }

    //        txtBackupSize.Text = "";
    //        prgLoading.Visibility = Visibility.Visible;
    //        btnLoadDataBases.IsEnabled = false;

    //        if (string.IsNullOrWhiteSpace(cmbServers.Text))
    //        {
    //            MessageBox.Show("يرجى اختيار أو كتابة اسم السيرفر أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
    //            return;
    //        }

    //        string serverName = cmbServers.Text.Trim();
    //        string connectionString = $"Server={serverName};Database=master;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;";

    //        try
    //        {
    //            cmbDatabases.ItemsSource = null;
    //            var dbList = new List<string>();

    //            using var conn = new SqlConnection(connectionString);
    //            await conn.OpenAsync(); // ✅ إذا فشل الاتصال سيقف هنا

    //            using var cmd = new SqlCommand("SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name", conn);
    //            using var reader = await cmd.ExecuteReaderAsync();

    //            while (await reader.ReadAsync())
    //                dbList.Add(reader.GetString(0));

    //            cmbDatabases.ItemsSource = dbList;
    //            cmbDatabases.IsEnabled = true;

    //            if (dbList.Count == 0)
    //            {
    //                MessageBox.Show("لا توجد قواعد بيانات مستخدم.", "معلومات", MessageBoxButton.OK, MessageBoxImage.Information);
    //                return;
    //            }
    //            else
    //            {

    //                cmbDatabases.SelectedIndex = 0;

    //                //  تحديث الاتصال لقاعدة البيانات المختارة دائماً
    //                if (cmbDatabases.SelectedItem is string selectedDb)
    //                {
    //                    var builder = new SqlConnectionStringBuilder(connectionString)
    //                    {
    //                        InitialCatalog = selectedDb
    //                    };
    //                    connectionString = builder.ConnectionString;
    //                }

    //                settings.DefaultConnectionString = connectionString;
    //                settings.FileSize = await clsDatabaseHelper.GetDatabaseSizeAsync(connectionString);
    //                txtBackupSize.Text = settings.FileSize;
    //                SettingsHelper.Save("SettingesFiles\\appsettings.json", settings);

    //                await clsAutoCompleteProvider.UpdateAutoCompleteJsonAsync(connectionString);

    //            }
    //        }
    //        catch (SqlException ex)
    //        {
    //            // تحليل رمز الخطأ (اختياري إن أردت تمييز حالات محددة)
    //            if (ex.Number == 53 || ex.Message.Contains("A network-related") || ex.Message.Contains("server was not found"))
    //            {
    //                MessageBox.Show("❌ تعذر الاتصال بالسيرفر.\nيرجى التأكد من أن اسم السيرفر صحيح وأن الخدمة تعمل.", "خطأ في الاتصال", MessageBoxButton.OK, MessageBoxImage.Warning);
    //            }
    //            else
    //            {
    //                // لأخطاء SQL أخرى نعرض رسالة عامة
    //                MessageBox.Show("❌ حدث خطأ أثناء محاولة الاتصال بالخادم.\n" + ex.Message, "خطأ SQL", MessageBoxButton.OK, MessageBoxImage.Error);
    //            }
    //        }

    //        catch (Exception ex)
    //        {
    //            MessageBox.Show($"حدث خطأ غير متوقع:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
    //        }
    //        finally
    //        {
    //            prgLoading.Visibility = Visibility.Collapsed;
    //            btnLoadDataBases.IsEnabled = true;


    //        }

    //    }


    //    //تحميل ملف المشروع
    //    private void BrowseProject_Click(object sender, RoutedEventArgs e)
    //    {
    //        var dialog = new System.Windows.Forms.FolderBrowserDialog();
    //        var result = dialog.ShowDialog();

    //        if (result == System.Windows.Forms.DialogResult.OK)
    //        {
    //            vm.ProjectPath = dialog.SelectedPath;
    //            txtProjectSize.Text = clsProjectAnalyzer.FormatBytes(clsProjectAnalyzer.GetDirectorySize(vm.ProjectPath));

    //        }
    //    }

    //    // تحليل الإجراءات المخزنة في قاعدة البيانات

    //    private async void Analyze_Click(object sender, RoutedEventArgs e)
    //    {
    //        // ✅ التحقق من الاتصال
    //        if (!clsDatabaseHelper.TryValidateConnection(settings.DefaultConnectionString) || string.IsNullOrEmpty(settings.DefaultConnectionString))
    //        {
    //            MessageBox.Show("يرجى إدخال جملة الاتصال بقاعدة البيانات.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
    //            return;
    //        }

    //        // ✅ التحقق من المسار
    //        if (string.IsNullOrWhiteSpace(vm.ProjectPath))
    //        {
    //            MessageBox.Show("يرجى تحديد مجلد المشروع.", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
    //            return;
    //        }

    //        try
    //        {
    //            // 🔄 إخفاء النتائج السابقة وإظهار مؤشر التحميل
    //            txtStatus.Text = "⏳ جارٍ التحليل ...";
    //            txtStatus.Visibility = Visibility.Visible;

    //            // 🧠 تحليل الإجراءات
    //            var allProcedures = await clsDatabaseHelper.GetAllStoredProceduresAsync(settings.DefaultConnectionString);
    //            var usageInfoList = clsProjectAnalyzer.MatchDatabaseProceduresInProject(vm.ProjectPath.Trim(), allProcedures);

    //            // 📌 تحديث البيانات في ViewModel
    //            vm.ObProject = new ObservableCollection<ProcedureUsageInfo>(usageInfoList);
    //            vm.TotalCount = usageInfoList.Count;
    //            vm.UsedCount = usageInfoList.Count(p => p.Count > 0);
    //            vm.UnusedCount = usageInfoList.Count(p => p.Count == 0);

    //            // ✅ تحديد الإجراءات غير المستخدمة مسبقاً لحذفها
    //            foreach (var item in vm.ObProject)
    //            {
    //                if (item.Count == 0)
    //                    item.IsSelectedForDelete = true;
    //            }

    //            // ✅ عرض النتائج وإخفاء مؤشر التحميل
    //            txtStatus.Visibility = Visibility.Collapsed;
    //            ucProcedureResults.Visibility = Visibility.Visible;

    //            // 🧠 تخزين داخلي للاستخدام لاحقاً (اختياري)
    //            _extractedProcedures = usageInfoList.Select(p => p.Procedure).ToList()!;
    //            _UnusedProcedures = usageInfoList.Where(p => p.Count == 0).Select(p => p.Procedure).ToList()!;
    //        }
    //        catch (Exception ex)
    //        {
    //            txtStatus.Visibility = Visibility.Collapsed;
    //            MessageBox.Show("حدث خطأ أثناء التحليل: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
    //        }
    //    }






    //    //private async void Analyze_Click(object sender, RoutedEventArgs e)
    //    //{

    //    //    if (!clsDatabaseHelper.TryValidateConnection(settings.DefaultConnectionString) || string.IsNullOrEmpty(settings.DefaultConnectionString))
    //    //    {
    //    //        MessageBox.Show("يرجى إدخال جملة الاتصال بقاعدة البيانات.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
    //    //        return;
    //    //    }

    //    //    if (string.IsNullOrWhiteSpace(vm.ProjectPath))
    //    //    {
    //    //        MessageBox.Show("يرجى تحديد مجلد المشروع.", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
    //    //        return;
    //    //    }



    //    //    try
    //    //    {
    //    //        //lstResults.Visibility = Visibility.Visible;
    //    //        //spSummary.Visibility = Visibility.Visible;
    //    //        //btnExport.Visibility = Visibility.Visible;
    //    //        // إشعار البدء
    //    //        txtStatus.Visibility = Visibility.Visible;

    //    //        var allProcedures = await clsDatabaseHelper.GetAllStoredProceduresAsync(settings.DefaultConnectionString);
    //    //        var usageInfoList = clsProjectAnalyzer.MatchDatabaseProceduresInProject(vm.ProjectPath.Trim(), allProcedures);

    //    //        // تحديث القائمة
    //    //        vm.ObProject = new(usageInfoList);
    //    //        vm.TotalCount= usageInfoList.Count;
    //    //        vm.UsedCount = usageInfoList.Count(p => p.Count > 0);
    //    //        vm.UnusedCount = usageInfoList.Count(p => p.Count == 0);

    //    //        txtStatus.Visibility = Visibility.Collapsed;

    //    //        // تخزين للعمليات الأخرى مثل التصدير أو الحذف
    //    //        _extractedProcedures = usageInfoList.Select(p => p.Procedure).ToList()!;
    //    //        _UnusedProcedures = usageInfoList.Where(p => p.Count == 0).Select(p => p.Procedure).ToList()!;

    //    //        // تحديث واجهة المستخدم
    //    //        foreach (var item in vm.ObProject)
    //    //        {
    //    //            if (item.Count == 0)
    //    //                item.IsSelectedForDelete = true;
    //    //        }


    //    //    }
    //    //    catch (Exception ex)
    //    //    {
    //    //        txtStatus.Visibility = Visibility.Collapsed;

    //    //        MessageBox.Show("حدث خطأ أثناء التحليل: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
    //    //    }
    //    //}





    //    // حذف الإجراءات غير المستخدمة
    //    private async void DeleteUnusedProcedures_Click(object sender, RoutedEventArgs e)
    //    {
    //        if (_extractedProcedures == null || _UnusedProcedures == null /*|| lstResults.ItemsSource == null*/)
    //        {
    //            MessageBox.Show("يرجى أولاً تنفيذ التحليل من خلال زر 'تحليل الملف'.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
    //            return;
    //        }

    //        // ✅ الإجراءات التي تم تحديدها يدوياً للحذف
    //        var selectedToDelete = vm.ObProject
    //            .Where(p => p.IsSelectedForDelete)
    //            .Select(p => p.Procedure)
    //            .ToList();

    //        if (selectedToDelete.Count == 0)
    //        {
    //            MessageBox.Show("لم يتم تحديد أي إجراء للحذف.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
    //            return;
    //        }


    //        // ✅ رسالة تأكيد مخصصة حسب وجود غير مستخدمة أو لا
    //        string warningMessage = _UnusedProcedures.Count == 0
    //            ? $"⚠️ لا توجد إجراءات غير مستخدمة.\nهل ترغب بحذف الإجراءات المحددة ({selectedToDelete.Count})؟"
    //            : $"⚠️ سيتم حذف {selectedToDelete.Count} إجراء تم تحديده.\nهل ترغب بتنفيذ الحذف على قاعدة البيانات الأصلية؟";

    //        var confirmDelete = MessageBox.Show(
    //              warningMessage,
    //              "تأكيد الحذف",
    //              MessageBoxButton.YesNo,
    //              MessageBoxImage.Warning
    //                      );




    //        if (confirmDelete != MessageBoxResult.Yes)
    //            return;

    //        string connectionString = settings.DefaultConnectionString;

    //        // نسخ احتياطي؟
    //        var backupConfirm = MessageBox.Show(
    //            "📦 هل ترغب بإنشاء نسخة احتياطية قبل الحذف؟",
    //            "نسخة احتياطية",
    //            MessageBoxButton.YesNo,
    //            MessageBoxImage.Question
    //        );

    //        if (backupConfirm == MessageBoxResult.Yes)
    //        {
    //            try
    //            {
    //                // تحديد مسار النسخة الاحتياطية

    //                var dialog = new System.Windows.Forms.FolderBrowserDialog();
    //                var result = dialog.ShowDialog();

    //                if (result == System.Windows.Forms.DialogResult.OK)
    //                {
    //                    string backupDirectory = dialog.SelectedPath;

    //                    if (!Directory.Exists(backupDirectory))
    //                        Directory.CreateDirectory(backupDirectory);

    //                    string backupPath = Path.Combine(backupDirectory, $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak");

    //                    using var conn = new SqlConnection(connectionString);
    //                    await conn.OpenAsync();

    //                    string dbName = conn.Database;
    //                    var cmdText = $"BACKUP DATABASE [{dbName}] TO DISK = N'{backupPath}' WITH FORMAT;";
    //                    var backupCmd = new SqlCommand(cmdText, conn);
    //                    await backupCmd.ExecuteNonQueryAsync();

    //                    MessageBox.Show($"✅ تم حفظ النسخة الاحتياطية في: \n{backupPath}", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                MessageBox.Show("❌ فشل في حفظ النسخة الاحتياطية:\n" + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
    //                return;
    //            }
    //        }

    //        // بدء الحذف

    //        using var mainConn = new SqlConnection(connectionString);
    //        await mainConn.OpenAsync();


    //        foreach (var proc in selectedToDelete)
    //        {
    //            try
    //            {
    //                var dropCmd = new SqlCommand($"IF OBJECT_ID('{proc}', 'P') IS NOT NULL DROP PROCEDURE [{proc}];", mainConn);
    //                await dropCmd.ExecuteNonQueryAsync();
    //            }
    //            catch (Exception ex)
    //            {
    //                MessageBox.Show($"❌ خطأ أثناء حذف '{proc}':\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
    //            }
    //        }

    //        MessageBox.Show("✅ تم حذف جميع الإجراءات غير المستخدمة من قاعدة البيانات الأصلية.");
    //        // تحديث القائمة بعد الحذف
    //        Analyze_Click(sender, e);
    //    }
    //    //فتح الملف عند النقر على الموقع
    //    private void Location_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    //    {
    //        if (sender is TextBlock tb && tb.DataContext is LocationInfo location)
    //        {
    //            try
    //            {
    //                if (!File.Exists(location.FullPath))
    //                {
    //                    MessageBox.Show("⚠️ الملف غير موجود: " + location.FullPath);
    //                    return;
    //                }

    //                string filePath = location.FullPath;
    //                string? visualStudioPath = clsVisualStudioPath.GetFirstAvailableEditor();

    //                if (!string.IsNullOrEmpty(visualStudioPath) || visualStudioPath != null)
    //                {
    //                    // فتح الملف باستخدام Visual Studio أو VS Code
    //                    Process.Start(new ProcessStartInfo
    //                    {
    //                        FileName = visualStudioPath,
    //                        Arguments = $"\"{filePath}\"",
    //                        UseShellExecute = true
    //                    });
    //                }
    //                else
    //                {
    //                    // fallback إلى Notepad
    //                    Process.Start(new ProcessStartInfo
    //                    {
    //                        FileName = "notepad.exe",
    //                        Arguments = $"\"{filePath}\"",
    //                        UseShellExecute = true
    //                    });
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                MessageBox.Show("خطأ في فتح الملف: " + ex.Message);
    //            }
    //        }
    //    }
    //    private void CheckAll_Click(object sender, RoutedEventArgs e)
    //    {

    //        bool allChecked = vm.ObProject!.All(p => p.IsSelectedForDelete);

    //        foreach (var proc in vm.ObProject!)
    //            proc.IsSelectedForDelete = !allChecked;


    //    }
    //    private void cmbServers_Loaded(object sender, RoutedEventArgs e)
    //    {
    //        if (cmbServers.Template.FindName("PART_EditableTextBox", cmbServers) is TextBox textBox)
    //        {
    //            textBox.TextChanged += ServerComboBoxTextBox_TextChanged;
    //        }
    //    }
    //    private void ServerComboBoxTextBox_TextChanged(object sender, TextChangedEventArgs e)
    //    {
    //        if (cmbServers.Items.Count == 1 &&
    //            cmbServers.Items[0]?.ToString()?.StartsWith("❗") == true)
    //        {
    //            cmbServers.Items.Clear(); // ✅ إزالة الرسالة المؤقتة فور بدء الكتابة
    //        }
    //    }



    //    private void MenuItem_Click(object sender, RoutedEventArgs e)
    //    {
    //        //vm.ExecuteEditProcedure();
    //    }
    //}



    public partial class DatabaseAnalyzerWindow : Window
    {
        private readonly AppSettings settings;
        private  List<string> _extractedProcedures = new();
        private  List<string> _UnusedProcedures = new();
        private  DatabaseAnalyzerViewModel vm;

        public DatabaseAnalyzerWindow()
        {
            InitializeComponent();

            settings = SettingsHelper.Load<AppSettings>("SettingesFiles\\appsettings.json");
            
            vm = new DatabaseAnalyzerViewModel(settings);
            DataContext = vm;

            vm.ShowOverlayAction = ShowOverlayControl;
            vm.AnalyzeProjectAction = () => Analyze_Click(this, new RoutedEventArgs());
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {

            if (!string.IsNullOrWhiteSpace(settings.DefaultConnectionString))
            {
                try
                {
                    var builder = new SqlConnectionStringBuilder(settings.DefaultConnectionString);
                    settings.DefaultConnectionString = builder.ConnectionString;

                    cmbServers.ItemsSource = null;
                    cmbServers.Items.Clear();
                    cmbServers.Items.Add(builder.DataSource);
                    txtBackupSize.Text = settings.FileSize;

                    LoadDatabasesForSelectedServer(null, null);
                }
                catch { }
            }
            else
            {
                await LoadAvailableSqlServersAsync();
            }
        }


        //تحديث السيرفرات
        private async void ReloadServers_Click(object sender, RoutedEventArgs e)
        {
            // تعطيل الأزرار
            btnLoadDataBases.IsEnabled = false;
            btnRefreshServers.IsEnabled = false;
            grdUC.Visibility = Visibility.Collapsed;


            //تفريغ النصوص
            if (!string.IsNullOrWhiteSpace(vm.ProjectPath) || !string.IsNullOrEmpty(cmbDatabases.Text))
            {
                txtProjectSize.Text = "";
                txtBackupSize.Text = "";
                txtProjectSize.Text = "";

            }


            // تفريغ قواعد البيانات
            cmbDatabases.ItemsSource = null;
            cmbDatabases.Items.Clear();
            cmbDatabases.Text = string.Empty;
            cmbDatabases.IsEnabled = false;

            settings.DefaultConnectionString = string.Empty;
            SettingsHelper.Save("SettingesFiles\\appsettings.json", settings);


            await LoadAvailableSqlServersAsync();
        }

        // تحميل قواعد البيانات عند اختيار سيرفر
        private async void LoadDatabasesForSelectedServer(object sender, RoutedEventArgs e)
        {
            if (cmbDatabases.ItemsSource is List<string> existingList && existingList.Count > 0)
            {
                MessageBox.Show("✅ قواعد البيانات محمّلة بالفعل.", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            grdUC.Visibility = Visibility.Collapsed;

            txtBackupSize.Text = "";
            prgLoading.Visibility = Visibility.Visible;
            btnLoadDataBases.IsEnabled = false;

            if (string.IsNullOrWhiteSpace(cmbServers.Text))
            {
                MessageBox.Show("يرجى اختيار أو كتابة اسم السيرفر أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                btnLoadDataBases.IsEnabled = true;

                return;
            }

            string serverName = cmbServers.Text.Trim();
            string connectionString = $"Server={serverName};Database=master;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;";

            try
            {
                cmbDatabases.ItemsSource = null;
                var dbList = new List<string>();

                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync(); 


                using var cmd = new SqlCommand("SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name", conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    dbList.Add(reader.GetString(0));

                cmbDatabases.ItemsSource = dbList;
                cmbDatabases.IsEnabled = true;

                if (dbList.Count == 0)
                {
                    MessageBox.Show("لا توجد قواعد بيانات مستخدم.", "معلومات", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                else
                {

                    cmbDatabases.SelectedIndex = 0;

                    //  تحديث الاتصال لقاعدة البيانات المختارة دائماً
                    if (cmbDatabases.SelectedItem is string selectedDb)
                    {
                        var builder = new SqlConnectionStringBuilder(connectionString)
                        {
                            InitialCatalog = selectedDb
                        };
                        connectionString = builder.ConnectionString;
                    }

                    settings.DefaultConnectionString = connectionString;
                    settings.FileSize = await clsDatabaseHelper.GetDatabaseSizeAsync(connectionString);
                    txtBackupSize.Text = settings.FileSize;
                    SettingsHelper.Save("SettingesFiles\\appsettings.json", settings);

                    await clsAutoCompleteProvider.UpdateAutoCompleteJsonAsync(connectionString);

                }
            }
            catch (SqlException ex)
            {
                // تحليل رمز الخطأ (اختياري إن أردت تمييز حالات محددة)
                if (ex.Number == 53 || ex.Message.Contains("A network-related") || ex.Message.Contains("server was not found"))
                {
                    MessageBox.Show("❌ تعذر الاتصال بالسيرفر.\nيرجى التأكد من أن اسم السيرفر صحيح وأن الخدمة تعمل.", "خطأ في الاتصال", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    // لأخطاء SQL أخرى نعرض رسالة عامة
                    MessageBox.Show("❌ حدث خطأ أثناء محاولة الاتصال بالخادم.\n" + ex.Message, "خطأ SQL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ غير متوقع:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                prgLoading.Visibility = Visibility.Collapsed;
                btnLoadDataBases.IsEnabled = true;


            }

        }

        // تحميل السيرفرات المتاحة في الخلفية
        private async Task LoadAvailableSqlServersAsync()
        {
            try
            {
                cmbServers.IsEnabled = false;
                cmbServers.ItemsSource = null;
                cmbServers.Items.Clear();
                cmbServers.Text = string.Empty;
                cmbServers.IsEditable = true;
                cmbServers.Items.Add("🔄 جارٍ البحث عن السيرفرات...");
                cmbServers.SelectedIndex = 0;

                var serverNames = await Task.Run(() =>
                {
                    var result = new List<string>();
                    try
                    {
                        var table = SqlDataSourceEnumerator.Instance.GetDataSources();
                        foreach (DataRow row in table.Rows)
                        {
                            string server = row["ServerName"].ToString()!;
                            string? instance = row["InstanceName"]?.ToString();
                            result.Add(string.IsNullOrEmpty(instance) ? server : $"{server}\\{instance}");
                        }
                    }
                    catch { }
                    return result;
                });

                cmbServers.Items.Clear();
                cmbServers.ItemsSource = serverNames;

                if (serverNames.Count == 0)
                {
                    cmbServers.IsEditable = true;
                    cmbServers.ItemsSource = null;
                    cmbServers.Items.Add("❗ لم يتم العثور على سيرفرات، اكتب الاسم يدويًا");
                    cmbServers.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشل أثناء تحميل السيرفرات:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                cmbServers.IsEnabled = true;
                btnLoadDataBases.IsEnabled = true;
                btnRefreshServers.IsEnabled = true;
            }
        }

        //تحليل المشروع
        private async void Analyze_Click(object sender, RoutedEventArgs e)
        {

            if (!clsDatabaseHelper.TryValidateConnection(settings.DefaultConnectionString) || string.IsNullOrEmpty(settings.DefaultConnectionString))
            {
                MessageBox.Show("يرجى إدخال جملة الاتصال بقاعدة البيانات.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(vm.ProjectPath))
            {
                MessageBox.Show("يرجى تحديد مجلد المشروع.", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }



            try
            {

                grdUC.Visibility = Visibility.Visible;
                // إشعار البدء
                txtStatus.Visibility = Visibility.Visible;

                var allProcedures = await clsDatabaseHelper.GetAllStoredProceduresAsync(settings.DefaultConnectionString);
                var usageInfoList = clsProjectAnalyzer.MatchDatabaseProceduresInProject(vm.ProjectPath.Trim(), allProcedures);

                // تحديث القائمة
                 vm.ObProject = new(usageInfoList);
                vm.TotalCount = usageInfoList.Count;
                vm.UsedCount = usageInfoList.Count(p => p.Count > 0);
                vm.UnusedCount = usageInfoList.Count(p => p.Count == 0);

               

                txtStatus.Visibility = Visibility.Collapsed;

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
            catch (Exception ex)
            {
                txtStatus.Visibility = Visibility.Collapsed;

                MessageBox.Show("حدث خطأ أثناء التحليل: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowOverlayControl(userControl? control)
        {
            OverlayContainer.Children.Clear();
            OverlayContainer.Visibility = control == null ? Visibility.Collapsed : Visibility.Visible;

            if (control != null)
            {
                var container = new Border
                {
                    Style = (Style)FindResource("OverlayEditorContainerStyle"),
                    Child = control
                };
                OverlayContainer.Children.Add(container);
            }
        }

        private void cmbServers_Loaded(object sender, RoutedEventArgs e)
        {
            if (cmbServers.Template.FindName("PART_EditableTextBox", cmbServers) is TextBox textBox)
            {
                textBox.TextChanged += ServerComboBoxTextBox_TextChanged;
            }
        }

        private void ServerComboBoxTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (cmbServers.Items.Count == 1 &&
                cmbServers.Items[0]?.ToString()?.StartsWith("❗") == true)
            {
                cmbServers.Items.Clear();
            }
        }

        private void BrowseProject_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                vm.ProjectPath = dialog.SelectedPath;
                txtProjectSize.Text = clsProjectAnalyzer.FormatBytes(clsProjectAnalyzer.GetDirectorySize(vm.ProjectPath));
            }
        }

        // حذف الإجراءات غير المستخدمة
        private async void DeleteUnusedProcedures_Click(object sender, RoutedEventArgs e)
        {
            if (_extractedProcedures == null || _UnusedProcedures == null )
            {
                MessageBox.Show("يرجى أولاً تنفيذ التحليل من خلال زر 'تحليل الملف'.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ✅ الإجراءات التي تم تحديدها يدوياً للحذف
            var selectedToDelete = vm.ObProject
                .Where(p => p.IsSelectedForDelete)
                .Select(p => p.Procedure)
                .ToList();

            if (selectedToDelete.Count == 0)
            {
                MessageBox.Show("لم يتم تحديد أي إجراء للحذف.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }


            // ✅ رسالة تأكيد مخصصة حسب وجود غير مستخدمة أو لا
            string warningMessage = _UnusedProcedures.Count == 0
                ? $"⚠️ لا توجد إجراءات غير مستخدمة.\nهل ترغب بحذف الإجراءات المحددة ({selectedToDelete.Count})؟"
                : $"⚠️ سيتم حذف {selectedToDelete.Count} إجراء تم تحديده.\nهل ترغب بتنفيذ الحذف على قاعدة البيانات الأصلية؟";

            var confirmDelete = MessageBox.Show(
                  warningMessage,
                  "تأكيد الحذف",
                  MessageBoxButton.YesNo,
                  MessageBoxImage.Warning
                          );




            if (confirmDelete != MessageBoxResult.Yes)
                return;

            string? connectionString = settings.DefaultConnectionString;

            // نسخ احتياطي؟
            var backupConfirm = MessageBox.Show(
                "📦 هل ترغب بإنشاء نسخة احتياطية قبل الحذف؟",
                "نسخة احتياطية",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (backupConfirm == MessageBoxResult.Yes)
            {
                try
                {
                    // تحديد مسار النسخة الاحتياطية

                    var dialog = new System.Windows.Forms.FolderBrowserDialog();
                    var result = dialog.ShowDialog();

                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        string backupDirectory = dialog.SelectedPath;

                        if (!Directory.Exists(backupDirectory))
                            Directory.CreateDirectory(backupDirectory);

                        string backupPath = Path.Combine(backupDirectory, $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak");

                        using var conn = new SqlConnection(connectionString);
                        await conn.OpenAsync();

                        string dbName = conn.Database;
                        var cmdText = $"BACKUP DATABASE [{dbName}] TO DISK = N'{backupPath}' WITH FORMAT;";
                        var backupCmd = new SqlCommand(cmdText, conn);
                        await backupCmd.ExecuteNonQueryAsync();

                        MessageBox.Show($"✅ تم حفظ النسخة الاحتياطية في: \n{backupPath}", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ فشل في حفظ النسخة الاحتياطية:\n" + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // بدء الحذف

            using var mainConn = new SqlConnection(connectionString);
            await mainConn.OpenAsync();


            foreach (var proc in selectedToDelete)
            {
                try
                {
                    var dropCmd = new SqlCommand($"IF OBJECT_ID('{proc}', 'P') IS NOT NULL DROP PROCEDURE [{proc}];", mainConn);
                    await dropCmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ خطأ أثناء حذف '{proc}':\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            MessageBox.Show("✅ تم حذف جميع الإجراءات غير المستخدمة من قاعدة البيانات الأصلية.");
            // تحديث القائمة بعد الحذف
            Analyze_Click(sender, e);
        }

        //تحديد الكل للحذف
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            vm.ToggleSelectAll();
        }

        private async void cmbDatabases_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbDatabases.SelectedItem is string selectedDb)
            {

                // تحديث الاتصال
                string oldConnStr = settings.defaultConnectionString;
                var builder = new SqlConnectionStringBuilder(oldConnStr)
                {
                    InitialCatalog = selectedDb
                };
                
                string newConnStr = builder.ToString();

                settings.defaultConnectionString = newConnStr;

                //حفظ الاتصال الجديد
                SettingsHelper.Save("SettingesFiles\\appsettings.json", settings);


                if (!string.IsNullOrEmpty(vm.ProjectPath))
                    Analyze_Click(null, null);

                await clsAutoCompleteProvider.UpdateAutoCompleteJsonAsync(newConnStr);

               
            }
        }



    }

}
