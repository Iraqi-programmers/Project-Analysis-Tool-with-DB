using Microsoft.Data.SqlClient;
using SpAnalyzerTool.Helper;
using SpAnalyzerTool.Models;
using SpAnalyzerTool.ProcedureMergeEngine;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace SpAnalyzerTool
{
    public static class clsDatabaseHelper
    {
        /// <summary>
        /// تعبئة كل الستورد بروسيجر التابعة للداتا بيز المحددة في الكونكشن سترنك
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<List<string>> GetAllStoredProceduresAsync(string connectionString)
        {

            try
            {


                var procedures = new List<string>();

                using (var conn = new SqlConnection(connectionString))
                {

                    await conn.OpenAsync();

                    using (var cmd = new SqlCommand(@"
                         SELECT p.name
                         FROM sys.procedures p
                         LEFT JOIN sys.extended_properties ep 
                             ON p.object_id = ep.major_id 
                             AND ep.name = 'microsoft_database_tools_support'
                         WHERE is_ms_shipped = 0
                           AND ep.name IS NULL
        ", conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            procedures.Add(reader.GetString(0));
                        }
                    }
                }

                return procedures;
            }
            catch (SqlException sqlEx)
            {
                // معالجة أخطاء SQL بشكل específic
                Console.WriteLine($"SQL Error: {sqlEx.Message}");

                // تحقق من رقم الخطأ لمزيد من التفاصيل
                if (sqlEx.Number == 18456) // خطأ في login
                {
                    Console.WriteLine("خطأ في اسم المستخدم أو كلمة المرور");
                }
                else if (sqlEx.Number == 4060) // قاعدة البيانات غير موجودة
                {
                    Console.WriteLine("قاعدة البيانات غير موجودة");
                }

                return new List<string>(); // إرجاع قائمة فارغة بدلاً من throw
            }
            catch (Exception ex)
            {
                // معالجة الأخطاء العامة
                Console.WriteLine($"General Error: {ex.Message}");
                return new List<string>();
            }

        }

        /// <summary>
        /// تعبة كل الستور بروسيجر من ملف الداتا بك اب 
        /// </summary>
        /// <param name="sqlContent"></param>
        /// <returns></returns>
        public static List<string> ExtractStoredProcedureNames(string sqlText)
        {
            var procedureNames = new List<string>();

            // نقسم الملف إلى كتل بناءً على GO (لفصل كل إجراء)
            var blocks = Regex.Split(sqlText, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            foreach (var block in blocks)
            {
                var match = Regex.Match(block, @"\bCREATE\s+PROCEDURE\s+(?:\[dbo\]\.)?\[?(\w+)\]?", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var name = match.Groups[1].Value;
                    if (!string.IsNullOrWhiteSpace(name))
                        procedureNames.Add(name);
                }
            }

            return procedureNames;
        }

        /// <summary>
        /// دالة استخراج اسم البروسيجر
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static string? ExtractProcedureName(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return null;

            // أزل التعليقات لجعل regex أنظف
            sql = RemoveSqlComments(sql);

            // Regex يدعم:
            // - CREATE OR ALTER PROCEDURE
            // - CREATE PROCEDURE
            // - ALTER PROCEDURE
            // - مع أو بدون schema
            var pattern = @"(?i)(CREATE\s+(OR\s+ALTER\s+)?|ALTER\s+)PROCEDURE\s+(?:\[(?<schema>\w+)\]|\b(?<schema>\w+)\b)?\.?\[?(?<name>\w+)\]?";

            var match = System.Text.RegularExpressions.Regex.Match(sql, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                string procName = match.Groups["name"].Value;
                string? schema = match.Groups["schema"].Success ? match.Groups["schema"].Value : null;

                return schema != null ? $"{schema}.{procName}" : procName;
            }

            return null;
        }

        /// <summary>
        ///<para> يستعيد قاعدة بيانات SQL Server المؤقتة من ملف .bak (يدعم الأجزاء المتعددة)،</para>
        /// يستخرج جميع الإجراءات المخزنة، ثم يحذف قاعدة البيانات المؤقتة والملفات بعد ذلك.
        /// </summary>
        /// 
        private static async Task<List<string>> RestoreAndExtractProceduresAsync(string sqlInstanceName, string bakFilePath, string restoreDbName)
        {
            if (!File.Exists(bakFilePath))
                throw new FileNotFoundException("Backup file not found", bakFilePath);

            string masterConnStr = $"Server={sqlInstanceName};Database=master;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;";
            string dbName = "TempRestoreDb_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string restoreDbConnStr = $"Server={sqlInstanceName};Database={dbName};Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;";

            string dataFileLogicalName, logFileLogicalName;
            string dataFilePath, logFilePath;

            #region 🧱 STEP 1: استخراج أسماء الملفات المنطقية

            using var masterConn = new SqlConnection(masterConnStr);
            await masterConn.OpenAsync();

            using (var cmd = new SqlCommand("RESTORE FILELISTONLY FROM DISK = @bak", masterConn))
            {
                cmd.Parameters.AddWithValue("@bak", bakFilePath);
                using var reader = await cmd.ExecuteReaderAsync();
                var table = new DataTable();
                table.Load(reader);

                if (table.Rows.Count < 2)
                    throw new Exception("❌ فشل في استخراج أسماء الملفات المنطقية من الباك أب");

                dataFileLogicalName = table.Rows[0]["LogicalName"].ToString()!;
                logFileLogicalName = table.Rows[1]["LogicalName"].ToString()!;
            }

            #endregion

            #region 📦 STEP 2: اكتشاف عدد ملفات الـ Media Set (ديناميكي)

            int mediaFamilyCount = 1;
            using (var cmd = new SqlCommand("RESTORE HEADERONLY FROM DISK = @bak", masterConn))
            {
                cmd.Parameters.AddWithValue("@bak", bakFilePath);
                using var reader = await cmd.ExecuteReaderAsync();
                var headerTable = new DataTable();
                headerTable.Load(reader);

                if (headerTable.Rows.Count > 0 && headerTable.Columns.Contains("FamilySequenceNumber"))
                {
                    mediaFamilyCount = headerTable.AsEnumerable()
                        .Select(r => r.Field<int>("FamilySequenceNumber"))
                        .Max();
                }
            }

            #endregion

            #region 📁 STEP 3: إعداد مسارات الملفات

            string defaultDataPath;
            using (var cmd = new SqlCommand("SELECT TOP 1 physical_name FROM sys.master_files WHERE database_id = 1", masterConn))
            {
                string? fullPath = (string?)await cmd.ExecuteScalarAsync();
                defaultDataPath = Path.GetDirectoryName(fullPath!)!;
            }

            dataFilePath = Path.Combine(defaultDataPath, $"{dbName}.mdf");
            logFilePath = Path.Combine(defaultDataPath, $"{dbName}_log.ldf");

            #endregion

            #region 🧹 STEP 4: حذف قاعدة بيانات سابقة بنفس الاسم (إن وجدت)

            string killSql = $@"
        IF EXISTS (SELECT name FROM sys.databases WHERE name = '{dbName}')
        BEGIN
            ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [{dbName}];
        END";
            await new SqlCommand(killSql, masterConn).ExecuteNonQueryAsync();

            #endregion

            #region 📂 STEP 5: تحميل جميع أجزاء الباك أب

            var bakParts = new List<string>();

            for (int i = 1; i <= mediaFamilyCount; i++)
            {
                string partPath = i == 1
                    ? bakFilePath
                    : Path.Combine(
                        Path.GetDirectoryName(bakFilePath)!,
                        Path.GetFileNameWithoutExtension(bakFilePath) + $".part{i}.bak");

                if (!File.Exists(partPath))
                {
                    MessageBox.Show(
                        $"⚠️ الملف {Path.GetFileName(partPath)} مفقود. يجب توفير {mediaFamilyCount} جزء من ملف الباك أب.",
                        "ملف ناقص",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return null!;
                }

                bakParts.Add(partPath);
            }

            #endregion

            #region 🔄 STEP 6: تنفيذ عملية الاستعادة

            string restoreSql = $@"
        RESTORE DATABASE [{dbName}]
        FROM {string.Join(", ", bakParts.Select((_, i) => $"DISK = @bak{i}"))}
        WITH 
            MOVE @dataLogicalName TO @dataPhysicalPath,
            MOVE @logLogicalName TO @logPhysicalPath,
            REPLACE;";

            try
            {
                using (var restoreCmd = new SqlCommand(restoreSql, masterConn))
                {
                    for (int i = 0; i < bakParts.Count; i++)
                        restoreCmd.Parameters.AddWithValue($"@bak{i}", bakParts[i]);

                    restoreCmd.Parameters.AddWithValue("@dataLogicalName", dataFileLogicalName);
                    restoreCmd.Parameters.AddWithValue("@logLogicalName", logFileLogicalName);
                    restoreCmd.Parameters.AddWithValue("@dataPhysicalPath", dataFilePath);
                    restoreCmd.Parameters.AddWithValue("@logPhysicalPath", logFilePath);

                    await restoreCmd.ExecuteNonQueryAsync();
                }
            }
            catch (SqlException ex) when (ex.Message.Contains("The media set has") && ex.Message.Contains("media families"))
            {
                MessageBox.Show("❌ لا يمكن استعادة ملف الباك أب لأن بعض أجزاءه مفقودة.\nيرجى التأكد من وجود جميع الملفات قبل المتابعة.", "ملفات ناقصة", MessageBoxButton.OK, MessageBoxImage.Error);
                return null!;
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ غير متوقع أثناء استعادة ملف الباك أب:\n" + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return null!;
            }


            #endregion

            #region 📋 STEP 7: استخراج أسماء الإجراءات المخزنة

            var procedures = new List<string>();
            using (var restoredConn = new SqlConnection(restoreDbConnStr))
            {
                await restoredConn.OpenAsync();

                string readProcs = @"
            SELECT p.name
            FROM sys.procedures p
            LEFT JOIN sys.extended_properties ep 
                ON p.object_id = ep.major_id 
                AND ep.name = 'microsoft_database_tools_support'
            WHERE is_ms_shipped = 0
              AND ep.name IS NULL";

                using var cmd = new SqlCommand(readProcs, restoredConn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    procedures.Add(reader.GetString(0));
            }

            #endregion

            #region 🧹 STEP 8: حذف القاعدة المؤقتة والملفات الفيزيائية

            try
            {
                using var cleanupConn = new SqlConnection(masterConnStr);
                await cleanupConn.OpenAsync();

                string dropDbSql = $@"
            IF EXISTS (SELECT name FROM sys.databases WHERE name = '{dbName}')
            BEGIN
                ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{dbName}];
            END";
                await new SqlCommand(dropDbSql, cleanupConn).ExecuteNonQueryAsync();

                if (File.Exists(dataFilePath)) File.Delete(dataFilePath);
                if (File.Exists(logFilePath)) File.Delete(logFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ فشل في حذف الملفات المؤقتة: " + ex.Message);
            }

            #endregion

            return procedures;
        }


        /// <summary>
        /// يرجع كولكشن من اوبجكتات لي  اسماء البروسيجر عن طريق ملف الباك اب
        /// </summary>
        /// <param name="sqlInstanceName"></param>
        /// <param name="bakFilePath"></param>
        /// <param name="tempDbName"></param>
        /// <returns></returns>
        public static async Task<ObservableCollection<ProcedureUsageInfo>> LoadProceduresFromBakAsync(string sqlInstanceName, string bakFilePath,  string tempDbName)
        {
            var procedures = await RestoreAndExtractProceduresAsync(sqlInstanceName, bakFilePath, tempDbName);

            if (procedures == null || procedures.Count == 0)
                return new ObservableCollection<ProcedureUsageInfo>();

            var result = new ObservableCollection<ProcedureUsageInfo>();

  
            foreach (var proc in procedures)
            {
                result.Add(new ProcedureUsageInfo
                {
                    Procedure = proc,
                    Count = 0 // مبدئياً، ويمكن حسابه لاحقاً
                });
            }

            return result;
        }


        /// <summary>
        /// يحسب إجمالي حجم ملفات قاعدة البيانات (Data + Log).
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<string> GetDatabaseSizeAsync(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return "❌ جملة الاتصال فارغة أو غير صحيحة.";

            try
            {
                using var conn = new SqlConnection(connectionString);

                try
                {
                    await conn.OpenAsync();
                }
                catch (SqlException ex)
                {
                    return $"❌ فشل الاتصال بقاعدة البيانات.\nالرسالة: {ex.Message}";
                }

                string dbName = conn.Database;

                string query = @"
            SELECT SUM(size) * 8 * 1024  -- الحجم بالبايت
            FROM sys.master_files 
            WHERE database_id = DB_ID(@dbName);";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@dbName", dbName);

                var result = await cmd.ExecuteScalarAsync();

                if (result != null && long.TryParse(result.ToString(), out long sizeInBytes))
                {
                    return clsProjectAnalyzer.FormatBytes(sizeInBytes);
                }

                return "❓ لم يتمكن من تحديد حجم قاعدة البيانات.";
            }
            catch (Exception ex)
            {
                return $"⚠️ حدث خطأ أثناء المعالجة:\n{ex.Message}";
            }
        }


        private static string RemoveSqlComments(string sql)
        {
            // إزالة التعليقات من نوع -- و /* */
            string noLineComments = Regex.Replace(sql, @"--.*?$", "", RegexOptions.Multiline);
            string noBlockComments = Regex.Replace(noLineComments, @"/\*.*?\*/", "", RegexOptions.Singleline);
            return noBlockComments.Trim();
        }

        public static async Task<string> LoadProcedureDefinitionAsync(string procedureName, string connectionString)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(
                @"SELECT definition FROM sys.sql_modules 
          WHERE object_id = OBJECT_ID(@name)", conn);

            cmd.Parameters.AddWithValue("@name", procedureName);

            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString() ?? string.Empty;
        }


        /// <summary>
        /// يتحقق مما إذا كان الإجراء المخزن موجودًا في قاعدة البيانات المحددة.
        /// </summary>
        /// <param name="procedureName">اسم الإجراء (بدون schema اختياريًا)</param>
        /// <param name="connectionString">سلسلة الاتصال</param>
        /// <returns>true إذا كان موجودًا، false إذا لم يوجد</returns>
        public static bool ProcedureExists(string procedureName, string connectionString)
        {
            string safeProcName = procedureName.Contains(".") ? procedureName : $"dbo.{procedureName}";

            const string sql = @"
                SELECT COUNT(*) 
                FROM sys.objects 
                WHERE object_id = OBJECT_ID(@name) AND type = 'P'";

            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@name", safeProcName);

                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// يجلب جميع أسماء الجداول في قاعدة البيانات المحددة.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<List<string>> GetAllTableNamesAsync(string connectionString)
        {
            try
            {

                var list = new List<string>();
                string query = "SELECT name FROM sys.tables ORDER BY name";

                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    list.Add(reader.GetString(0));

                return list;
            }catch(Exception ex)
            {
                Debug.WriteLine("حدث خطأ أثناء جلب أسماء الجداول:\n" + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<string>();
            }
        }

        /// <summary>
        /// يجلب أسماء الأعمدة لجدول معين في قاعدة البيانات المحددة.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="ConnectionString"></param>
        /// <returns></returns>
        public static async Task<List<string>> GetTableColumnsAsync(string tableName,string ConnectionString)
        {
            try
            {


                var columns = new List<string>();
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                string sql = $@"
        SELECT COLUMN_NAME 
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = @TableName";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TableName", tableName);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString(0));
                }

                return columns;
            }catch(Exception ex)
            {
                Debug.WriteLine("حدث خطأ أثناء جلب أسماء الأعمدة:\n" + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<string>();
            }
        }

        /// <summary>
        /// التحقق من صحة سلسلة الاتصال بقاعدة البيانات.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public static bool TryValidateConnection(string connectionString)
        {

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("فشل الاتصال بقاعدة البيانات:\n" + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// يعدل على اول سطر في البروسيجر Create Or Alter
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="procedureName"></param>
        /// <returns></returns>
        public static string FixProcedureHeaderToCreateOrAlter(string sql, string procedureName)
        {
            var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            var pattern = @"^\s*(CREATE|ALTER)\s+PROCEDURE\s+.*?\b" + Regex.Escape(procedureName) + @"\b";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            for (int i = 0; i < lines.Length; i++)
            {
                if (regex.IsMatch(lines[i]))
                {
                    lines[i] = $"CREATE OR ALTER PROCEDURE [dbo].[{procedureName}]";
                    break;
                }
            }

            return string.Join(Environment.NewLine, lines);
        }



        public static async Task<List<StoredProcedureInfo>> LoadAllStoredProceduresAsync(string connectionString, string sourceDatabase)
        {
            var procedures = new List<StoredProcedureInfo>();

            using SqlConnection conn = new(connectionString);
            await conn.OpenAsync();

            // الخطوة 1: جلب أسماء جميع الإجراءات في قاعدة البيانات
            var cmdText = @"
        SELECT SPECIFIC_NAME
        FROM INFORMATION_SCHEMA.ROUTINES
        WHERE ROUTINE_TYPE = 'PROCEDURE'";

            using SqlCommand cmd = new(cmdText, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var procedureNames = new List<string>();

            while (await reader.ReadAsync())
            {
                procedureNames.Add(reader.GetString(0));
            }

            reader.Close();

            // الخطوة 2: جلب التعريف الكامل لكل إجراء
            foreach (var procName in procedureNames)
            {
                string definition = await LoadProcedureDefinitionAsync(procName, connectionString);

                // يمكنك لاحقًا استخراج الأعمدة المرجعة لو أردت
                var outputColumns = new HashSet<string>(); // فارغة مؤقتًا

                procedures.Add(new StoredProcedureInfo(procName, definition, outputColumns, sourceDatabase));
            }

            return procedures;
        }





    }
}
