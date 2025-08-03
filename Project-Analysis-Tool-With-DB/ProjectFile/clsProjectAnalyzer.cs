using SpAnalyzerTool.Models;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace SpAnalyzerTool
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

       
        public static List<ProcedureUsageInfo> MatchDatabaseProceduresInProject(
                                    string projectDirectory, List<string> dbProcedures)
        {
            var allowedExtensions = new[] { ".cs", ".sql", ".txt", ".config", ".cshtml", ".vb" };
            var files = Directory.GetFiles(projectDirectory, "*.*", SearchOption.AllDirectories)
                                 .Where(f => allowedExtensions.Contains(Path.GetExtension(f)));

            var usageList = new List<ProcedureUsageInfo>();

            foreach (var sp in dbProcedures)
            {
                var info = new ProcedureUsageInfo
                {
                    Procedure = sp,
                    Count = 0,
                    LocationPaths = new List<LocationInfo>()
                };

                string pattern = $@"\b{Regex.Escape(sp)}\b";
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);

                foreach (var file in files)
                {
                    try
                    {
                        var lines = File.ReadAllLines(file);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (regex.IsMatch(lines[i]))
                            {
                                info.Count++;
                                info.LocationPaths.Add(new LocationInfo
                                {
                                    FullPath= Path.GetFullPath(file),
                                    LineNumber = i + 1
                                });
                            }
                        }
                    }
                    catch
                    {
                        // تجاهل الملفات التي تفشل
                    }
                }

                usageList.Add(info);
            }

            return usageList;
        }


        /// <summary>
        /// تستقبل قائمة محددة مسبقا فيها اسماء البروسيجر , وتحلل الملفات لمعرفة عدد المرات ومواقع الاستخدام
        /// </summary>
        /// <param name="projectPath"></param>
        /// <param name="procedureNames"></param>
        /// <returns></returns>
        public static List<ProcedureUsageInfo> AnalyzeProjectUsage(string projectPath, List<string> procedureNames)
        {
            var resultList = new List<ProcedureUsageInfo>();
            var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

            foreach (var proc in procedureNames)
            {
                int totalCount = 0;
                var foundInFiles = new List<LocationInfo>();

                foreach (var file in files)
                {
                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (Regex.IsMatch(lines[i], $@"\b{Regex.Escape(proc)}\b", RegexOptions.IgnoreCase))
                        {
                            int count = Regex.Matches(lines[i], $@"\b{Regex.Escape(proc)}\b", RegexOptions.IgnoreCase).Count;
                            totalCount += count;

                            // 🔁 أضف كل مرة يظهر فيها حتى داخل نفس الملف
                            foundInFiles.Add(new LocationInfo
                            {
                                FullPath = file,
                                LineNumber = i + 1 // 1-based
                            });
                        }
                    }
                }


                resultList.Add(new ProcedureUsageInfo
                {
                    Procedure = proc,
                    Count = totalCount,
                    LocationPaths = foundInFiles
                    
                });
            }

            return resultList;
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
