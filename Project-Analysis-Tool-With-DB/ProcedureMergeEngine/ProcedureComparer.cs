using System.Text.RegularExpressions;

namespace SpAnalyzerTool.ProcedureMergeEngine
{
    /// <summary>
    /// مسؤول عن مقارنة قائمتين من الإجراءات المخزنة
    /// لتحديد الإجراءات الفريدة والمكررة بناءً على
    /// عدد الأعمدة وأسمائها فقط (بغض النظر عن الترتيب).
    /// </summary>
    public static class ProcedureComparer
    {
        /// <summary>
        /// يقارن قائمتين من الإجراءات ويرجع قائمة موحدة بدون تكرار.
        /// يعتبر البروسيجرات متطابقة إذا كانت ترجع نفس أسماء الأعمدة ونفس العدد.
        /// </summary>
        /// <param name="procedures1">قائمة البروسيجرات من المصدر الأول</param>
        /// <param name="procedures2">قائمة البروسيجرات من المصدر الثاني</param>
        /// <returns>قائمة بالإجراءات الفريدة الموحدة</returns>
        public static List<StoredProcedureInfo> MergeWithoutDuplicates(List<StoredProcedureInfo> procedures1, List<StoredProcedureInfo> procedures2)
        {
            var merged = new List<StoredProcedureInfo>();
            var compared = new HashSet<string>(); // لمنع التكرار من procedures2

            // أضف من القائمة الأولى كما هي
            foreach (var proc1 in procedures1)
            {
                merged.Add(proc1);

                // احسب بصمة الأعمدة
                var signature1 = GetColumnSignature(proc1.OutputColumnNames);

                // ابحث في القائمة الثانية عن بروسيجر متطابق
                foreach (var proc2 in procedures2)
                {
                    var signature2 = GetColumnSignature(proc2.OutputColumnNames);

                    if (signature1.SetEquals(signature2))
                    {
                        compared.Add(proc2.Name); // سجل أنه مكرر
                    }
                }
            }

            // أضف البروسيجرات من القائمة الثانية التي لم تُعتبر مكررة
            foreach (var proc in procedures2)
            {
                if (!compared.Contains(proc.Name))
                    merged.Add(proc);
            }

            return merged;
        }

        /// <summary>
        /// يبني بصمة من أسماء الأعمدة لتستخدم في المقارنة.
        /// يتم تجاهل الترتيب واستخدام مقارنة غير حساسة لحالة الأحرف.
        /// </summary>
        private static HashSet<string> GetColumnSignature(HashSet<string> columnNames)
        {
            return new HashSet<string>(columnNames, StringComparer.OrdinalIgnoreCase);
        }
        public static bool AreTableSetsCompatible(List<StoredProcedureInfo> list1, List<StoredProcedureInfo> list2, out List<string> diff)
        {
            var tables1 = list1.SelectMany(p => ExtractTableNames(p.Definition))
                              .Where(t => !string.IsNullOrWhiteSpace(t))
                              .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var tables2 = list2.SelectMany(p => ExtractTableNames(p.Definition))
                              .Where(t => !string.IsNullOrWhiteSpace(t))
                              .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // العثور على الاختلافات في كلا الاتجاهين
            var diff1 = tables1.Except(tables2, StringComparer.OrdinalIgnoreCase).ToList();
            var diff2 = tables2.Except(tables1, StringComparer.OrdinalIgnoreCase).ToList();

            diff = diff1.Concat(diff2).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            return diff.Count == 0;
        }

        private static readonly HashSet<string> _systemTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // جداول النظام الشائعة
            "sys.tables", "sys.objects", "sys.columns", "sys.types", "sys.parameters",
            "sys.sql_modules", "sys.indexes", "sys.foreign_keys", "sys.key_constraints",
            "sys.views", "sys.procedures", "sys.schemas", "sys.databases", "sys.configurations",
            "sys.servers", "sys.linked_logins", "sys.credentials", "sys.filegroups", "sys.partitions",
            "sys.allocation_units", "sys.computed_columns", "sys.identity_columns", "sys.default_constraints",
            
            // معلومات schema
            "information_schema.tables", "information_schema.columns", "information_schema.views",
            "information_schema.routines", "information_schema.table_constraints",
            
            // Dynamic Management Views/Functions
            "sys.dm_", "sys.dm_db_", "sys.dm_exec_", "sys.dm_io_", "sys.dm_tran_",
            
            // جداول نظام أخرى
            "msdb.dbo.sysjobs", "msdb.dbo.sysjobhistory", "msdb.dbo.syscategories"
        };

        private static HashSet<string> ExtractTableNames(string procedureDefinition)
        {
            var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // هذه التعبيرات تبحث عن FROM و JOIN و INTO وغيرها
            var regex = new Regex(@"\b(?:FROM|JOIN|INTO|UPDATE|INSERT\s+INTO|DELETE\s+FROM)\s+(?:#?#?\[?[\w@$#]+]?\.)?    # Schema name (optional)
        \[?([\w@$#]+)]?             # Table name (captured)
        (?:\s+(?:AS\s+)?\[?[\w@$#]+]?)?  # Alias (ignored)
        ", RegexOptions.IgnoreCase | RegexOptions.Compiled );

            

                        var matches = regex.Matches(procedureDefinition);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1 && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    string tableName = match.Groups[1].Value.Trim();

                    // استبعاد الجداول المؤقتة وجداول النظام
                    if (!IsSystemOrTempTable(tableName))
                    {
                        tableNames.Add(tableName);
                    }
                }
            }

            return tableNames;
        }

        private static bool IsSystemOrTempTable(string tableName)
        {
            // استبعاد الجداول المؤقتة المحلية والعامة
            if (tableName.StartsWith("#") || tableName.StartsWith("##"))
                return true;

            // استبعاد جداول النظام المعروفة
            if (_systemTables.Contains(tableName))
                return true;

            // استبعاد الجداول التي تبدأ ببادئات النظام
            if (tableName.StartsWith("sys.", StringComparison.OrdinalIgnoreCase) ||
                tableName.StartsWith("information_schema.", StringComparison.OrdinalIgnoreCase) ||
                tableName.StartsWith("dm_", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }


    }
}
