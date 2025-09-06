using Microsoft.Data.SqlClient;
using SpAnalyzerTool.Helper;
using SpAnalyzerTool.Models;
using System.Text.RegularExpressions;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using userControl = System.Windows.Controls.UserControl;

namespace SpAnalyzerTool.View.UserControl
{
    
    public partial class UC_ProcedureEditor : userControl
    {
        public event EventHandler? ExitRequested;
        public event EventHandler? RefreshData;

        private clsAutoCompleteProvider? _autoComplete;
        private  string? ConnectionString;

        public UC_ProcedureEditor(string? connectionString,string? procedureName=null)
        {
            InitializeComponent();
            ConnectionString = connectionString;
            if(procedureName != null )
            {
                LoadProcedureCode(procedureName);
            }
            _autoComplete = new clsAutoCompleteProvider(SqlEditor, SettingsHelper.GetSettingsPath("SettingesFiles\\autocomplete.json"));
            SqlEditor.Focus();
            
        }
        
        private async void LoadProcedureCode(string procedureName)
        {
            try
            {
                string sql = await clsDatabaseHelper.LoadProcedureDefinitionAsync(procedureName, ConnectionString!);

                // استبدال السطر الأول بـ CREATE OR ALTER
                sql = clsDatabaseHelper.FixProcedureHeaderToCreateOrAlter(sql, procedureName);


                SqlEditor.Text = sql; 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشل في تحميل الإجراء:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool CheckSyntaxError()
        {
            var allErrors = new List<SqlParseErrorModle>();

            //  تحليل بناء الجملة
            var syntaxErrors = clsSqlSyntaxValidator.GetErrors(SqlEditor.Text);

            //تحميل الاقتراحات من ملف JSON
            clsSqlErrorFixSuggester.LoadSuggestionsFromJson("SettingesFiles\\autocomplete.json");
          
            //  اقتراح تصحيحات لأخطاء بناء الجملة
            foreach (var error in syntaxErrors)
            {
                error.SuggestedText = clsSqlErrorFixSuggester.SuggestFix(error.Message!);
                allErrors.Add(error);
            }

            //التحقق من اسماء الجداول غير المعروفة
            var unknownTables = clsSqlErrorFixSuggester.GetUnknownTables(SqlEditor.Text);
            foreach (var table in unknownTables)
            {
                int lineNumber = clsSqlErrorFixSuggester.FindLineNumber(SqlEditor.Text, table);

                allErrors.Add(new SqlParseErrorModle
                {
                    Line = lineNumber,
                    Level = "تحذير",
                    Message = $"❌ اسم الجدول غير معروف: {table}",
                    SuggestedText = clsSqlErrorFixSuggester.FindClosestKeywordOfType(table, "Table"),
                    IsTableError = true
                });
            }


            //  عرض النتائج
            if (allErrors.Count > 0)
            {
                dgErrors.ItemsSource = allErrors;
                dgErrors.Visibility = Visibility.Visible;
                spError.Visibility = Visibility.Visible;
                txtErrorSummary.Text = $"❌ تم العثور على {allErrors.Count} خطأ أو تحذير.";
                return false;
            }

            dgErrors.Visibility = Visibility.Collapsed;
            spError.Visibility = Visibility.Collapsed;
            return true;
        }


        private async void SaveProcedure_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(SqlEditor.Text))
                return;
            
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                MessageBox.Show("لم يتم تعيين سلسلة الاتصال بقاعدة البيانات.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if(!CheckSyntaxError())
                return; // إذا كان هناك أخطاء في الصياغة، لا نستمر في الحفظ


       

            string sql = SqlEditor.Text;

            // استخراج اسم الإجراء من النص
            string? procName = clsDatabaseHelper.ExtractProcedureName(sql);
            if (string.IsNullOrWhiteSpace(procName))
            {
                MessageBox.Show("❌ لم يتمكن النظام من تحديد اسم البروسيجر من النص. تأكد من صحة CREATE PROCEDURE.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // تحقق من وجود البروسيجر
            if (clsDatabaseHelper.ProcedureExists(procName, ConnectionString))
            {
                var confirm = MessageBox.Show(
                    $"⚠️ الإجراء '{procName}' موجود بالفعل.\nهل ترغب في استبداله؟",
                    "تأكيد الاستبدال",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;
            }

            try
            {

                using var conn = new SqlConnection(ConnectionString);
                conn.Open();

                using var cmd = new SqlCommand(sql, conn);
                cmd.ExecuteNonQuery();

                MessageBox.Show("✅ تم حفظ الإجراء بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                SqlEditor.Text = string.Empty; // مسح المحرر بعد الحفظ
                ExitRequested?.Invoke(this, EventArgs.Empty); // إغلاق المحرر بعد الحفظ
                RefreshData?.Invoke(this, EventArgs.Empty); // تحديث البيانات في الواجهة الرئيسية
            }
            catch (Exception ex)
            {
                string message = $"❌ خطأ أثناء تنفيذ SQL:\n{ex.Message}";

                // تحقق إن كان الخطأ بسبب عمود غير موجود
                if (ex.Message.Contains("Invalid column name"))
                {
                    // محاولة استخراج اسم الجدول من النص (بشكل بسيط)
                    var tableMatch = Regex.Match(sql, @"FROM\s+(\w+)", RegexOptions.IgnoreCase);
                    if (tableMatch.Success)
                    {
                        string tableName = tableMatch.Groups[1].Value;

                        try
                        {
                            // استخراج الأعمدة من قاعدة البيانات
                            var availableColumns = await clsDatabaseHelper.GetTableColumnsAsync(tableName,ConnectionString);
                            if (availableColumns.Any())
                            {
                                message += $"\n\n📌 الأعمدة المتوفرة في الجدول [{tableName}]:\n- {string.Join("\n- ", availableColumns)}";
                            }
                        }
                        catch (Exception innerEx)
                        {
                            message += $"\n(تعذر استخراج أسماء الأعمدة: {innerEx.Message})";
                        }
                    }
                }

                MessageBox.Show(message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }
    }
}
