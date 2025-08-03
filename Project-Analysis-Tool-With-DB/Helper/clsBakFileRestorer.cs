using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        //public static async Task RestoreBackupAsync(string bakFullPath, string targetDbName, string masterConnection)
        //{
        //    using var conn = new SqlConnection(masterConnection);
        //    await conn.OpenAsync();

        //    // حذف القاعدة إن كانت موجودة
        //    using (var drop = new SqlCommand(
        //        $"IF DB_ID('{targetDbName}') IS NOT NULL ALTER DATABASE [{targetDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
        //        $"IF DB_ID('{targetDbName}') IS NOT NULL DROP DATABASE [{targetDbName}];", conn))
        //    {
        //        await drop.ExecuteNonQueryAsync();
        //    }

        //    // أمر الاسترجاع
        //    using var restore = new SqlCommand(
        //        $"RESTORE DATABASE [{targetDbName}] FROM DISK = N'{bakFullPath}' WITH REPLACE, RECOVERY;", conn);

        //    restore.CommandTimeout = 0;
        //    await restore.ExecuteNonQueryAsync();
        //}

        public static async Task RestoreBackupAsync(string bakFullPath, string targetDbName, string masterConnection)
        {
            // 1. إنشاء مجلد C:\TempDbs\ إذا لم يكن موجودًا
            string targetFolder = @"C:\TempDbs\";
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            using var conn = new SqlConnection(masterConnection);
            await conn.OpenAsync();

            // 2. استخراج أسماء الملفات داخل الباك أب (logical names)
            string logicalMdfName = null;
            string logicalLdfName = null;

            using (var fileListCmd = new SqlCommand($"RESTORE FILELISTONLY FROM DISK = N'{bakFullPath}'", conn))
            using (var reader = await fileListCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    logicalMdfName = reader["LogicalName"].ToString();
                }

                if (await reader.ReadAsync())
                {
                    logicalLdfName = reader["LogicalName"].ToString();
                }
            }

            if (logicalMdfName == null || logicalLdfName == null)
                throw new InvalidOperationException("لم يتم العثور على أسماء الملفات في الباك أب.");

            // 3. تحديد المسارات الجديدة
            var mdfPath = Path.Combine(targetFolder, $"{targetDbName}.mdf");
            var ldfPath = Path.Combine(targetFolder, $"{targetDbName}_log.ldf");

            // 4. حذف القاعدة إن كانت موجودة
            using (var drop = new SqlCommand(
                $"IF DB_ID('{targetDbName}') IS NOT NULL ALTER DATABASE [{targetDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"IF DB_ID('{targetDbName}') IS NOT NULL DROP DATABASE [{targetDbName}];", conn))
            {
                await drop.ExecuteNonQueryAsync();
            }

            // 5. أمر الاسترجاع مع MOVE
            string restoreSql = $@"
        RESTORE DATABASE [{targetDbName}]
        FROM DISK = N'{bakFullPath}'
        WITH MOVE N'{logicalMdfName}' TO N'{mdfPath}',
             MOVE N'{logicalLdfName}' TO N'{ldfPath}',
             REPLACE, RECOVERY;";

            using var restore = new SqlCommand(restoreSql, conn);
            restore.CommandTimeout = 0;
            await restore.ExecuteNonQueryAsync();

            // 6. حذف الملفات المؤقتة بعد نجاح الاسترجاع
            try
            {
                if (File.Exists(mdfPath)) File.Delete(mdfPath);
                if (File.Exists(ldfPath)) File.Delete(ldfPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"تحذير: لم يتم حذف ملفات الاسترجاع المؤقتة. {ex.Message}");
                // أو يمكن تسجيلها في لوق خاص
            }
        }


    }


}
