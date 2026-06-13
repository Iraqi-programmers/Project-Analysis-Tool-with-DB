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
        /// يجلب أسماء أعمدة أول نتيجة يُرجعها الإجراء دون تنفيذه فعليًا،
        /// باستخدام <c>sys.dm_exec_describe_first_result_set_for_object</c> الذي يُحلّل
        /// البيانات الوصفية فقط (آمن وبلا آثار جانبية، بعكس التنفيذ الفعلي عبر FMTONLY).
        /// تُستخدم النتيجة للمقارنة فقط، لذا الترتيب والقيم غير مهمين.
        /// </summary>
        /// <param name="conn">اتصال SQL مفتوح بقاعدة البيانات التي تحتوي الإجراء.</param>
        /// <param name="procName">اسم الإجراء (يُمرَّر كمعامل لمنع حقن SQL).</param>
        /// <returns>أسماء الأعمدة، أو قائمة فارغة إذا تعذّر الاستنتاج.</returns>
        private static IEnumerable<string> TryGetProcedureOutputColumns(SqlConnection conn, string procName)
        {
            try
            {
                // الوسيط الثاني = 0 يكتم أخطاء الاستنتاج ويُعيد مجموعة فارغة بدل رمي استثناء.
                const string sql = @"
                    SELECT name
                    FROM sys.dm_exec_describe_first_result_set_for_object(OBJECT_ID(@proc), 0)
                    WHERE name IS NOT NULL
                    ORDER BY column_ordinal;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@proc", SqlDbType.NVarChar, 257).Value = procName;

                var columns = new List<string>();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                        columns.Add(reader.GetString(0));
                }

                return columns;
            }
            catch (SqlException ex)
            {
                // تعذّر استنتاج الأعمدة (مثلاً نتيجة ديناميكية) — نُسجّل ونُكمل بقائمة فارغة.
                System.Diagnostics.Debug.WriteLine($"تعذّر استخراج أعمدة الإجراء '{procName}': {ex.Message}");
                return Array.Empty<string>();
            }
        }
    }
}
