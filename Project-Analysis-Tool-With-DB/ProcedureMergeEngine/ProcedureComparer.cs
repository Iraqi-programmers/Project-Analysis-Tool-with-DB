using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        private static HashSet<string> ExtractTableNames(string procedureDefinition)
        {
            var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // هذه التعبيرات تبحث عن FROM و JOIN و INTO وغيرها
            var regex = new Regex(@"\b(?:FROM|JOIN|INTO|UPDATE|INSERT\s+INTO|DELETE\s+FROM)\s+(\[?\w+\]?(?:\.\[?\w+\]?)?)", RegexOptions.IgnoreCase);

            var matches = regex.Matches(procedureDefinition);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    tableNames.Add(match.Groups[1].Value.Trim());
                }
            }

            return tableNames;
        }

        public static bool AreTableSetsCompatible(List<StoredProcedureInfo> list1, List<StoredProcedureInfo> list2, out List<string> diff)
        {
            var tables1 = list1.SelectMany(p => ExtractTableNames(p.Definition)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var tables2 = list2.SelectMany(p => ExtractTableNames(p.Definition)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            diff = tables1
                .Union(tables2)
                .Where(t => !tables1.Contains(t) || !tables2.Contains(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return diff.Count == 0;
        }

    }
}
