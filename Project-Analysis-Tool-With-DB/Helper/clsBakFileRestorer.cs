using Microsoft.Data.SqlClient;
using System.IO;
using System.Security.AccessControl;

namespace SpAnalyzerTool.Helper
{
    /// <summary>
    /// يسترجع ملف bak إلى قاعدة بيانات مؤقتة على نفس الخادم.
    /// ملاحظة: هذه النسخة مبسَّطة وتفترض أن المستخدم يملك صلاحيات RESTORE
    /// وأن المسارات الافتراضية لملفات البيانات سليمة.
    /// </summary>
    public static class clsBakFileRestorer
    {
        /// <summary>
        /// يسترجع ملف bak إلى قاعدة باسم targetDbName.
        /// إذا كانت القاعدة موجودة تُحذف أولاً (WITH REPLACE).
        /// </summary>
        public static void RestoreBackup(string bakFullPath, string targetDbName, string masterConnection)
        {
            using var conn = new SqlConnection(masterConnection);
            conn.Open();

            // إفلات القاعدة إن كانت موجودة
            using (var drop = new SqlCommand(
                $"IF DB_ID('{targetDbName}') IS NOT NULL ALTER DATABASE [{targetDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"IF DB_ID('{targetDbName}') IS NOT NULL DROP DATABASE [{targetDbName}];", conn))
            {
                drop.ExecuteNonQuery();
            }

            // أمر RESTORE بسيط (قد يحتاج MOVE حسب بنية السيرفر)
            using var restore = new SqlCommand(
                $"RESTORE DATABASE [{targetDbName}] FROM DISK = N'{bakFullPath}' WITH REPLACE, RECOVERY;", conn);
            restore.CommandTimeout = 0; // بلا حد لتفادي الوقت الطويل
            restore.ExecuteNonQuery();
        }

        public static async Task RestoreBackupAsync(string bakFullPath, string targetDbName, string masterConnection)
        {
            string? copiedBackupPath = null;
            SqlConnection? conn = null;

            try
            {
                // 1. نسخ ملف الباك أب إلى موقع يمكن الوصول إليه
                copiedBackupPath = CopyBackupToSqlAccessibleLocation(bakFullPath);

                // 2. إنشاء مجلد مؤقت لقواعد البيانات
                string targetFolder = @"C:\TempDbs\";
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                    GrantAccessToFolder(targetFolder);
                }

                conn = new SqlConnection(masterConnection);
                await conn.OpenAsync();

                // 3. استخراج أسماء الملفات من الباك أب
                string? logicalMdfName = null;
                string? logicalLdfName = null;

                using (var fileListCmd = new SqlCommand($"RESTORE FILELISTONLY FROM DISK = N'{copiedBackupPath.Replace("'", "''")}'", conn))
                using (var reader = await fileListCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string? type = reader["Type"].ToString();
                        if (type == "D" && logicalMdfName == null)
                        {
                            logicalMdfName = reader["LogicalName"].ToString();
                        }
                        else if (type == "L" && logicalLdfName == null)
                        {
                            logicalLdfName = reader["LogicalName"].ToString();
                        }
                    }
                }

                if (logicalMdfName == null || logicalLdfName == null)
                    throw new InvalidOperationException("لم يتم العثور على أسماء الملفات في الباك أب.");

                // 4. تحديد المسارات الجديدة للملفات
                var mdfPath = Path.Combine(targetFolder, $"{targetDbName}.mdf");
                var ldfPath = Path.Combine(targetFolder, $"{targetDbName}_log.ldf");

                // 5. حذف الملفات القديمة إذا كانت موجودة
                SafeDeleteFile(mdfPath);
                SafeDeleteFile(ldfPath);

                // 6. حذف قاعدة البيانات إذا كانت موجودة
                await DropDatabaseIfExists(conn, targetDbName);

                // 7. أمر الاسترجاع مع MOVE
                string restoreSql = $@"
RESTORE DATABASE [{targetDbName.Replace("'", "''")}]
FROM DISK = N'{copiedBackupPath.Replace("'", "''")}'
WITH MOVE N'{logicalMdfName.Replace("'", "''")}' TO N'{mdfPath.Replace("'", "''")}',
     MOVE N'{logicalLdfName.Replace("'", "''")}' TO N'{ldfPath.Replace("'", "''")}',
     REPLACE, RECOVERY;";

                using var restore = new SqlCommand(restoreSql, conn);
                restore.CommandTimeout = 0;
                await restore.ExecuteNonQueryAsync();

                // 8. الانتظار قليلاً لضمان اكتمال الاستعادة
                await Task.Delay(2000);

                // 9. تغيير وضع قاعدة البيانات لتكون متاحة للجميع
                string multiUserSql = $"ALTER DATABASE [{targetDbName.Replace("'", "''")}] SET MULTI_USER WITH ROLLBACK IMMEDIATE;";
                using var multiUserCmd = new SqlCommand(multiUserSql, conn);
                await multiUserCmd.ExecuteNonQueryAsync();

                // 10. منح الصلاحيات للمستخدم الحالي
                string grantAccessSql = $@"
USE [{targetDbName.Replace("'", "''")}];
EXEC sp_addrolemember 'db_owner', '{Environment.UserName}';";

                try
                {
                    using var grantCmd = new SqlCommand(grantAccessSql, conn);
                    await grantCmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // تجاهل الخطأ إذا لم نتمكن من منح الصلاحيات
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"فشل استرجاع الباك أب: {ex.Message}", ex);
            }
            finally
            {
                // إغلاق الاتصال
                conn?.Close();

                // تنظيف ملف الباك أب المؤقت
                if (copiedBackupPath != null && File.Exists(copiedBackupPath))
                {
                    try
                    {
                        File.Delete(copiedBackupPath);
                    }
                    catch
                    {
                        // تجاهل الأخطاء في الحذف النهائي
                    }
                }
            }
        }
        // الدالة المساعدة لنسخ ملف الباك أب إلى موقع يمكن الوصول إليه
        private static string CopyBackupToSqlAccessibleLocation(string originalBakPath)
        {
            // المسارات التي يمكن لـ SQL Server الوصول إليها عادةً
            string[] possiblePaths = {
        @"C:\SQLBackups",
        @"C:\TempSQL",
        @"C:\Temp",
        Path.Combine(Path.GetTempPath(), "SQLBackups"),
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\SQLBackups"
    };

            string fileName = Path.GetFileName(originalBakPath);

            foreach (var path in possiblePaths)
            {
                try
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);

                        // منح صلاحيات القراءة للجميع على المجلد
                        GrantAccessToFolder(path);
                    }

                    string destinationPath = Path.Combine(path, fileName);

                    // نسخ الملف مع overwrite إذا كان موجوداً
                    File.Copy(originalBakPath, destinationPath, true);

                    // منح صلاحية القراءة للجميع على الملف
                    File.SetAttributes(destinationPath, FileAttributes.Normal);
                    GrantAccessToFile(destinationPath);

                    return destinationPath;
                }
                catch (Exception ex)
                {
                    // تسجيل الخطأ والمحاولة بالمسار التالي
                    Console.WriteLine($"فشل النسخ إلى {path}: {ex.Message}");
                    continue;
                }
            }

            throw new Exception("لم يتمكن من نسخ ملف الباك أب إلى أي موقع يمكن لـ SQL Server الوصول إليه. يرجى التحقق من الصلاحيات.");
        }

        // منح صلاحيات الوصول إلى المجلد
        private static void GrantAccessToFolder(string folderPath)
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
                DirectorySecurity dirSecurity = dirInfo.GetAccessControl();

                // منح صلاحية القراءة للجميع
                dirSecurity.AddAccessRule(new FileSystemAccessRule(
                    "Everyone",
                    FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Modify,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));

                dirInfo.SetAccessControl(dirSecurity);
            }
            catch
            {
                // تجاهل الأخطاء في منح الصلاحيات
            }
        }

        // منح صلاحيات الوصول إلى الملف
        private static void GrantAccessToFile(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                FileSecurity fileSecurity = fileInfo.GetAccessControl();

                // منح صلاحية القراءة للجميع
                fileSecurity.AddAccessRule(new FileSystemAccessRule(
                    "Everyone",
                    FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Modify,
                    AccessControlType.Allow));

                fileInfo.SetAccessControl(fileSecurity);
            }
            catch
            {
                // تجاهل الأخطاء في منح الصلاحيات
            }
        }

        // حذف الملف بأمان مع إعادة المحاولة
        private static void SafeDeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                    File.Delete(filePath);
                    break;
                }
                catch
                {
                    if (i == maxRetries - 1) throw;
                    System.Threading.Thread.Sleep(100);
                }
            }
        }

        // حذف قاعدة البيانات إذا كانت موجودة
        private static async Task DropDatabaseIfExists(SqlConnection conn, string dbName)
        {
            try
            {
                string dropSql = $@"
IF DB_ID('{dbName.Replace("'", "''")}') IS NOT NULL 
BEGIN
    ALTER DATABASE [{dbName.Replace("'", "''")}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{dbName.Replace("'", "''")}];
END";

                using (var cmd = new SqlCommand(dropSql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"تحذير: لم يتم حذف قاعدة البيانات الموجودة. {ex.Message}");
            }
        }


        //public static async Task RestoreBackupAsync(string bakFullPath, string targetDbName, string masterConnection)
        //{
        //    try
        //    {


        //        // 1. إنشاء مجلد C:\TempDbs\ إذا لم يكن موجودًا
        //        string targetFolder = @"C:\TempDbs\";
        //        if (!Directory.Exists(targetFolder))
        //        {
        //            Directory.CreateDirectory(targetFolder);
        //        }

        //        using var conn = new SqlConnection(masterConnection);
        //        await conn.OpenAsync();

        //        // 2. استخراج أسماء الملفات داخل الباك أب (logical names)
        //        string? logicalMdfName = null;
        //        string? logicalLdfName = null;

        //        using (var fileListCmd = new SqlCommand($"RESTORE FILELISTONLY FROM DISK = N'{bakFullPath}'", conn))
        //        using (var reader = await fileListCmd.ExecuteReaderAsync())
        //        {
        //            if (await reader.ReadAsync())
        //            {
        //                logicalMdfName = reader["LogicalName"].ToString();
        //            }

        //            if (await reader.ReadAsync())
        //            {
        //                logicalLdfName = reader["LogicalName"].ToString();
        //            }
        //        }

        //        if (logicalMdfName == null || logicalLdfName == null)
        //            throw new InvalidOperationException("لم يتم العثور على أسماء الملفات في الباك أب.");

        //        // 3. تحديد المسارات الجديدة
        //        var mdfPath = Path.Combine(targetFolder, $"{targetDbName}.mdf");
        //        var ldfPath = Path.Combine(targetFolder, $"{targetDbName}_log.ldf");

        //        // 4. حذف القاعدة إن كانت موجودة
        //        using (var drop = new SqlCommand(
        //            $"IF DB_ID('{targetDbName}') IS NOT NULL ALTER DATABASE [{targetDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
        //            $"IF DB_ID('{targetDbName}') IS NOT NULL DROP DATABASE [{targetDbName}];", conn))
        //        {
        //            await drop.ExecuteNonQueryAsync();
        //        }

        //        // 5. أمر الاسترجاع مع MOVE
        //        string restoreSql = $@"
        //RESTORE DATABASE [{targetDbName}]
        //FROM DISK = N'{bakFullPath}'
        //WITH MOVE N'{logicalMdfName}' TO N'{mdfPath}',
        //     MOVE N'{logicalLdfName}' TO N'{ldfPath}',
        //     REPLACE, RECOVERY;";

        //        using var restore = new SqlCommand(restoreSql, conn);
        //        restore.CommandTimeout = 0;
        //        await restore.ExecuteNonQueryAsync();

        //        // 6. حذف الملفات المؤقتة بعد نجاح الاسترجاع
        //        try
        //        {
        //            if (File.Exists(mdfPath)) File.Delete(mdfPath);
        //            if (File.Exists(ldfPath)) File.Delete(ldfPath);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"تحذير: لم يتم حذف ملفات الاسترجاع المؤقتة. {ex.Message}");
        //            // أو يمكن تسجيلها في لوق خاص
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"فشل استرجاع الباك أب: {ex.Message}", ex);
        //    }




        //}
    }
}
