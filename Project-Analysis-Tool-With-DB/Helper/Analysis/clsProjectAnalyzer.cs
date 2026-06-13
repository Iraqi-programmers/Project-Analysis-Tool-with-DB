using SpAnalyzerTool.Models;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace SpAnalyzerTool.Helper
{
    public static class clsProjectAnalyzer
    {
        /// <summary>
        /// ( تبحث بحرية في كل الملفات وتعطيك قائمة بالبروسيجرات المستخدمه (اسماء فقط
        /// </summary>
        /// <param name="projectDirectory"></param>
        /// <param name="saveToFile"></param>
        /// <returns></returns>
        public static List<string> ExtractUsedStoredProcedures(string projectDirectory, bool saveToFile = true)
        {
            var procedures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allowedExtensions = new[] { ".cs", ".sql", ".txt", ".config" };

            var files = Directory.GetFiles(projectDirectory, "*.*", SearchOption.AllDirectories)
                                 .Where(f => allowedExtensions.Contains(Path.GetExtension(f)));

            var regexPatterns = new[]
            {
            // EXEC SP_Name or EXECUTE SP_Name
            @"\bEXEC(?:UTE)?\s+(?:\[dbo\]\.)?\[?(?<sp>[a-zA-Z0-9_]+)\]?",
            // SqlCommand("SP_Name")
            @"SqlCommand\s*\(\s*@?""(?<sp>[a-zA-Z0-9_]+)""",
            // CommandText = "SP_Name"
            @"CommandText\s*=\s*@?""(?<sp>[a-zA-Z0-9_]+)""",
            // ExecuteSqlRaw("EXEC SP_Name")
            @"Execute(Sql|SqlRaw|SqlInterpolated)?\s*\(\s*@?""(?:EXEC(?:UTE)?\s+)?(?<sp>[a-zA-Z0-9_]+)""",
        };

            foreach (var file in files)
            {
                string content = File.ReadAllText(file);
                foreach (var pattern in regexPatterns)
                {
                    var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        if (match.Groups["sp"].Success)
                            procedures.Add(match.Groups["sp"].Value);
                    }
                }
            }

            // حفظ النتائج في ملف
            if (saveToFile)
            {
                try
                {
                    string logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
                    Directory.CreateDirectory(logDir);

                    string logPath = Path.Combine(logDir, $"UsedProcedures_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    File.WriteAllLines(logPath, procedures.OrderBy(p => p), Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("فشل في حفظ سجل الـ Stored Procedures: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return procedures.ToList();
        }

       
        /// <summary>
        /// يطابق إجراءات قاعدة البيانات مع ملفات المشروع لحساب عدد مرات الاستخدام ومواقعه.
        /// محسّن: يُقرأ كل ملف مرّة واحدة فقط ويُفحص بتعبير منتظم موحّد لكل الإجراءات،
        /// بدل إعادة قراءة كل ملف لكل إجراء — مما يخفض تعقيد الإدخال/الإخراج
        /// من O(عدد الإجراءات × عدد الملفات) إلى O(عدد الملفات).
        /// </summary>
        /// <param name="projectDirectory">مسار مجلد المشروع المراد فحصه.</param>
        /// <param name="dbProcedures">أسماء الإجراءات القادمة من قاعدة البيانات.</param>
        /// <returns>معلومات استخدام كل إجراء بنفس ترتيب الإدخال (إجراء واحد لكل سطر كحدٍّ أقصى).</returns>
        public static List<ProcedureUsageInfo> MatchDatabaseProceduresInProject(
                                    string projectDirectory, List<string> dbProcedures)
        {
            var usageByName = BuildUsageMap(dbProcedures);
            if (usageByName.Count == 0)
                return new List<ProcedureUsageInfo>();

            var regex = BuildCombinedRegex(usageByName.Keys);
            var allowedExtensions = new[] { ".cs", ".sql", ".txt", ".config", ".cshtml", ".vb" };

            foreach (var file in EnumerateSourceFiles(projectDirectory, allowedExtensions))
            {
                string[] lines;
                try { lines = File.ReadAllLines(file); }
                catch { continue; } // تجاهل الملفات غير القابلة للقراءة

                string fullPath = Path.GetFullPath(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    // كل إجراء يُحتسب مرّة واحدة كحدٍّ أقصى لكل سطر (مطابق للسلوك الأصلي).
                    var countedOnLine = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (Match m in regex.Matches(lines[i]))
                    {
                        string name = m.Groups[1].Value;
                        if (countedOnLine.Add(name) && usageByName.TryGetValue(name, out var info))
                        {
                            info.Count++;
                            info.LocationPaths.Add(new LocationInfo { FullPath = fullPath, LineNumber = i + 1 });
                        }
                    }
                }
            }

            return dbProcedures
                .Where(sp => !string.IsNullOrWhiteSpace(sp))
                .Select(sp => usageByName[sp])
                .ToList();
        }


        /// <summary>
        /// تحلّل استخدام قائمة إجراءات داخل ملفات ‎.cs‎ فقط، وتحسب كل ظهور (لا كل سطر).
        /// محسّنة بنفس مبدأ القراءة الواحدة لكل ملف.
        /// </summary>
        /// <param name="projectPath">مسار مجلد المشروع.</param>
        /// <param name="procedureNames">أسماء الإجراءات المراد تحليلها.</param>
        /// <returns>معلومات الاستخدام لكل إجراء بنفس ترتيب الإدخال.</returns>
        public static List<ProcedureUsageInfo> AnalyzeProjectUsage(string projectPath, List<string> procedureNames)
        {
            var usageByName = BuildUsageMap(procedureNames);
            if (usageByName.Count == 0)
                return new List<ProcedureUsageInfo>();

            var regex = BuildCombinedRegex(usageByName.Keys);

            foreach (var file in EnumerateSourceFiles(projectPath, new[] { ".cs" }))
            {
                string[] lines;
                try { lines = File.ReadAllLines(file); }
                catch { continue; }

                for (int i = 0; i < lines.Length; i++)
                {
                    var locationAddedFor = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (Match m in regex.Matches(lines[i]))
                    {
                        string name = m.Groups[1].Value;
                        if (!usageByName.TryGetValue(name, out var info))
                            continue;

                        info.Count++; // كل ظهور يُحتسب
                        if (locationAddedFor.Add(name)) // موقع واحد لكل سطر لكل إجراء
                            info.LocationPaths.Add(new LocationInfo { FullPath = file, LineNumber = i + 1 });
                    }
                }
            }

            return procedureNames
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => usageByName[p])
                .ToList();
        }

        /// <summary>يبني قاموسًا غير حسّاس لحالة الأحرف من أسماء الإجراءات إلى كائنات الاستخدام (تجاهل المكرر/الفارغ).</summary>
        /// <param name="procedureNames">أسماء الإجراءات.</param>
        /// <returns>قاموس الأسماء → معلومات الاستخدام.</returns>
        private static Dictionary<string, ProcedureUsageInfo> BuildUsageMap(IEnumerable<string> procedureNames)
        {
            var map = new Dictionary<string, ProcedureUsageInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var sp in procedureNames)
            {
                if (string.IsNullOrWhiteSpace(sp) || map.ContainsKey(sp))
                    continue;

                map[sp] = new ProcedureUsageInfo
                {
                    Procedure = sp,
                    Count = 0,
                    LocationPaths = new List<LocationInfo>()
                };
            }
            return map;
        }

        /// <summary>يبني تعبيرًا منتظمًا موحّدًا (مُجمّعًا ومُترجمًا) يطابق أي اسم إجراء ضمن حدود كلمات.</summary>
        /// <param name="names">أسماء الإجراءات المراد دمجها.</param>
        /// <returns>تعبير منتظم واحد يطابق كل الأسماء.</returns>
        private static Regex BuildCombinedRegex(IEnumerable<string> names)
        {
            string pattern = @"\b(" + string.Join("|", names.Select(Regex.Escape)) + @")\b";
            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        /// <summary>يُعدّد ملفات المصدر ذات الامتدادات المسموح بها بشكل كسول (lazy enumeration).</summary>
        /// <param name="root">المجلد الجذر.</param>
        /// <param name="allowedExtensions">الامتدادات المسموح بها.</param>
        /// <returns>مسارات الملفات المطابقة.</returns>
        private static IEnumerable<string> EnumerateSourceFiles(string root, string[] allowedExtensions)
        {
            return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                            .Where(f => allowedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
        }

        #region Meatadata
        public static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        public static long GetDirectorySize(string path)
        {
            return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                            .Sum(f => new FileInfo(f).Length);
        }
        public static long GetFileSize(string filePath)
        {
            return new FileInfo(filePath).Length;
        }
        #endregion

    }
}
