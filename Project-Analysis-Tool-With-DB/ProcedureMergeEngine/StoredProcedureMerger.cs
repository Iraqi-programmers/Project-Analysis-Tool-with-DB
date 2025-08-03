using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpAnalyzerTool.ProcedureMergeEngine
{
    /// <summary>
    /// يمثل وحدة الدمج المركزية التي تقوم بجمع الإجراءات المخزنة
    /// من قاعدتي بيانات مؤقتتين، وإزالة التكرارات بناءً على
    /// أسماء الأعمدة وعددها فقط.
    /// </summary>
    public static class StoredProcedureMerger
    {
        /// <summary>
        /// يقوم بجلب البروسيجرات من قاعدتين، ثم يدمجها في قائمة واحدة
        /// بعد إزالة التكرارات بناءً على أسماء الأعمدة.
        /// </summary>
        /// <param name="connectionString1">سلسلة الاتصال بقاعدة الباك أب الأولى</param>
        /// <param name="connectionString2">سلسلة الاتصال بقاعدة الباك أب الثانية</param>
        /// <returns>قائمة موحدة من البروسيجرات بدون تكرار</returns>
        public static List<StoredProcedureInfo> MergeProcedures(string connectionString1, string connectionString2)
        {
            // استخراج البروسيجرات من كل قاعدة
            var procedures1 = ProcedureExtractor.ExtractProcedures(connectionString1);
            var procedures2 = ProcedureExtractor.ExtractProcedures(connectionString2);

            // دمج مع إزالة التكرار
            var merged = ProcedureComparer.MergeWithoutDuplicates(procedures1, procedures2);

            return merged;
        }

        public static List<StoredProcedureInfo> MergeProcedures(List<StoredProcedureInfo> list1, List<StoredProcedureInfo> list2)
        {
            if (list1 == null) list1 = new List<StoredProcedureInfo>();
            if (list2 == null) list2 = new List<StoredProcedureInfo>();

            // دمج القائمتين وإزالة التكرار حسب الاسم (أو المعرف الفريد)
            var merged = list1
                .Concat(list2)
                .GroupBy(sp => sp.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            return merged;
        }
    }
}
