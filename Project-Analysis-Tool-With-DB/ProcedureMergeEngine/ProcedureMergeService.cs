using System.Text;
using System.Text.RegularExpressions;

namespace SpAnalyzerTool.ProcedureMergeEngine
{
    /// <summary>
    /// خدمة دمج الإجراءات المخزّنة من مصدرين: تُصنّف كل إجراء (مفرد/متطابق/متعارض)
    /// وتُنشئ سكربت SQL موحّدًا قابلًا لإعادة التنفيذ بأمان (CREATE OR ALTER + GO).
    /// كل الدوال نقية (بلا حالة) وقابلة للاختبار.
    /// </summary>
    public static class ProcedureMergeService
    {
        private static readonly Regex LineComments =
            new(@"--.*?$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex BlockComments =
            new(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex WhitespaceRuns =
            new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex CreateAlterHeader =
            new(@"\b(CREATE\s+OR\s+ALTER|CREATE|ALTER)\s+PROC(EDURE)?\b",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// يدمج قائمتي إجراءات حسب الاسم (غير حسّاس لحالة الأحرف) ويُصنّف كل إجراء.
        /// التعقيد O(n+m) باستخدام قواميس بحث.
        /// </summary>
        /// <param name="first">إجراءات الملف الأول.</param>
        /// <param name="second">إجراءات الملف الثاني.</param>
        /// <returns>قائمة نتائج الدمج مرتّبة أبجديًا حسب الاسم.</returns>
        public static List<MergedProcedure> Merge(
            IEnumerable<StoredProcedureInfo> first,
            IEnumerable<StoredProcedureInfo> second)
        {
            var firstMap = ToMap(first);
            var secondMap = ToMap(second);

            var allNames = new HashSet<string>(firstMap.Keys, StringComparer.OrdinalIgnoreCase);
            allNames.UnionWith(secondMap.Keys);

            var result = new List<MergedProcedure>(allNames.Count);

            foreach (var name in allNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                bool inFirst = firstMap.TryGetValue(name, out var p1);
                bool inSecond = secondMap.TryGetValue(name, out var p2);

                MergeStatus status;
                if (inFirst && inSecond)
                    status = AreDefinitionsEqual(p1!.Definition, p2!.Definition)
                        ? MergeStatus.Identical
                        : MergeStatus.Conflict;
                else if (inFirst)
                    status = MergeStatus.OnlyInFirst;
                else
                    status = MergeStatus.OnlyInSecond;

                result.Add(new MergedProcedure(
                    name,
                    inFirst ? p1!.Definition : null,
                    inSecond ? p2!.Definition : null,
                    status));
            }

            return result;
        }

        /// <summary>
        /// يُنشئ سكربت SQL من الإجراءات المُضمَّنة فقط؛ كل إجراء كـ CREATE OR ALTER يفصله GO.
        /// </summary>
        /// <param name="procedures">عناصر نتيجة الدمج.</param>
        /// <returns>نص السكربت الجاهز للحفظ/التنفيذ.</returns>
        public static string BuildMergeScript(IEnumerable<MergedProcedure> procedures)
        {
            ArgumentNullException.ThrowIfNull(procedures);

            var sb = new StringBuilder();
            sb.AppendLine("/* ===========================================================");
            sb.AppendLine("   سكربت دمج الإجراءات المخزّنة (تم توليده تلقائيًا)");
            sb.AppendLine($"   تاريخ الإنشاء: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("   =========================================================== */");
            sb.AppendLine();
            sb.AppendLine("SET ANSI_NULLS ON;");
            sb.AppendLine("GO");
            sb.AppendLine("SET QUOTED_IDENTIFIER ON;");
            sb.AppendLine("GO");
            sb.AppendLine();

            foreach (var proc in procedures.Where(p => p.IsIncluded))
            {
                string body = EnsureCreateOrAlter(proc.EffectiveDefinition);
                if (string.IsNullOrWhiteSpace(body))
                    continue;

                sb.AppendLine("-- ------------------------------------------------------------");
                sb.AppendLine($"-- Procedure: {proc.Name}   [{proc.StatusText}]");
                sb.AppendLine("-- ------------------------------------------------------------");
                sb.AppendLine(body.Trim());
                sb.AppendLine("GO");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>يقارن تعريفين بعد إزالة التعليقات وتوحيد المسافات (غير حسّاس لحالة الأحرف).</summary>
        public static bool AreDefinitionsEqual(string? a, string? b)
            => string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);

        /// <summary>يحوّل ترويسة الإجراء إلى CREATE OR ALTER PROCEDURE لإعادة التنفيذ بأمان.</summary>
        /// <param name="definition">نص تعريف الإجراء.</param>
        /// <returns>التعريف بعد توحيد الترويسة.</returns>
        public static string EnsureCreateOrAlter(string? definition)
        {
            if (string.IsNullOrWhiteSpace(definition))
                return string.Empty;

            var match = CreateAlterHeader.Match(definition);
            if (!match.Success)
                return definition;

            return definition.Substring(0, match.Index)
                 + "CREATE OR ALTER PROCEDURE"
                 + definition.Substring(match.Index + match.Length);
        }

        private static Dictionary<string, StoredProcedureInfo> ToMap(IEnumerable<StoredProcedureInfo> procs)
        {
            var map = new Dictionary<string, StoredProcedureInfo>(StringComparer.OrdinalIgnoreCase);
            if (procs == null) return map;

            foreach (var p in procs)
            {
                if (p?.Name is { Length: > 0 } && !map.ContainsKey(p.Name))
                    map[p.Name] = p;
            }
            return map;
        }

        private static string Normalize(string? sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return string.Empty;

            string s = LineComments.Replace(sql, " ");
            s = BlockComments.Replace(s, " ");
            s = WhitespaceRuns.Replace(s, " ");
            return s.Trim();
        }
    }
}
