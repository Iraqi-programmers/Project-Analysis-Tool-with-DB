using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpAnalyzerTool.ProcedureMergeEngine
{
    /// <summary>
    /// مسؤول عن استخراج الإجراءات المخزنة (Stored Procedures) من قاعدة بيانات معينة.
    /// يتضمن استخراج أسماء الإجراءات، تعريفها الكامل (CREATE PROCEDURE)،
    /// وتحليل الأعمدة الناتجة منها.
    /// </summary>
    public static class ProcedureExtractor
    {
        /// <summary>
        /// يستخرج جميع الإجراءات المخزنة من قاعدة بيانات ويعيد قائمة بها.
        /// لكل إجراء يتم محاولة تنفيذ واستخراج أسماء الأعمدة الناتجة.
        /// </summary>
        /// <param name="connectionString">سلسلة الاتصال بقاعدة البيانات</param>
        /// <returns>قائمة بالكائنات StoredProcedureInfo</returns>
        public static List<StoredProcedureInfo> ExtractProcedures(string connectionString)
        {
            var procedures = new List<StoredProcedureInfo>();

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            // جلب أسماء البروسيجرات
            var getProcCmd = new SqlCommand(@"
                SELECT name 
                FROM sys.objects 
                WHERE type = 'P' AND is_ms_shipped = 0", conn);

            using var reader = getProcCmd.ExecuteReader();
            var procNames = new List<string>();
            while (reader.Read())
                procNames.Add(reader.GetString(0));

            reader.Close();

            foreach (var procName in procNames)
            {
                string definition = GetProcedureDefinition(conn, procName);
                var columns = TryGetProcedureOutputColumns(conn, procName);

                procedures.Add(new StoredProcedureInfo(
                    name: procName,
                    definition: definition,
                    outputColumns: columns,
                    sourceDatabase: conn.Database
                ));
            }

            return procedures;
        }

        /// <summary>
        /// يجلب نص تعريف البروسيجر (CREATE PROCEDURE ...)
        /// </summary>
        private static string GetProcedureDefinition(SqlConnection conn, string procName)
        {
            var cmd = new SqlCommand(@"
                SELECT sm.definition 
                FROM sys.sql_modules sm
                JOIN sys.objects o ON sm.object_id = o.object_id
                WHERE o.name = @procName", conn);

            cmd.Parameters.AddWithValue("@procName", procName);

            return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// يحاول تنفيذ البروسيجر وجلب أسماء الأعمدة الناتجة منه،
        /// بغض النظر عن القيم أو الترتيب، وتُستخدم للمقارنة فقط.
        /// </summary>
        private static IEnumerable<string> TryGetProcedureOutputColumns(SqlConnection conn, string procName)
        {
            try
            {
                using var cmd = new SqlCommand(procName, conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                using var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
                var schema = reader.GetSchemaTable();

                var columns = new List<string>();
                foreach (DataRow row in schema.Rows)
                {
                    if (row["ColumnName"] is string columnName)
                        columns.Add(columnName);
                }

                return columns;
            }
            catch
            {
                // فشل التنفيذ (مثلاً يحتاج معلمات)، نعيد قائمة فارغة
                return Array.Empty<string>();
            }
        }
    }
}
