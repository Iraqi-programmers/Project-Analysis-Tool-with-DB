using Newtonsoft.Json;
using SpAnalyzerTool.Models;
using System.IO;
using System.Text.RegularExpressions;

namespace SpAnalyzerTool.Helper
{
    public static class clsSqlErrorFixSuggester
    {
        private static Dictionary<string, string>? _knownWords;

        public static void LoadSuggestionsFromJson(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            var data = JsonConvert.DeserializeObject<SuggestionModel>(json);

            _knownWords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var keyword in data?.keywords ?? Enumerable.Empty<string>())
                _knownWords[keyword] = "Keyword";

            foreach (var table in data?.tables ?? Enumerable.Empty<string>())
                _knownWords[table] = "Table";
        }

        public static string SuggestFix(string errorMessage)
        {
            var match = Regex.Match(errorMessage, @"near\s+'(?<word>\w+)'", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string wrongWord = match.Groups["word"].Value;
                return FindClosestKnownWord(wrongWord);
            }

            return string.Empty;
        }

        /// <summary>
        /// استخراج أسماء الجداول من كود SQL ومقارنتها بالكلمات المعروفة.
        /// </summary>
        public static List<string> GetUnknownTables(string sql)
        {
            var matches = Regex.Matches(sql, @"\b(?:FROM|JOIN|UPDATE|INTO|DELETE\s+FROM)\s+(\[?\w+\]?)", RegexOptions.IgnoreCase);

            var foundTables = matches
                .Cast<Match>()
                .Select(m => m.Groups[1].Value.Trim('[', ']'))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            var unknown = foundTables
                .Where(name => !_knownWords.TryGetValue(name, out string? type) || type != "Table")
                .ToList();

            return unknown;
        }


        /// <summary>
        /// ايجاد رقم السطر الذي يحتوي على اسم الجدول في كود SQL.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static int FindLineNumber(string sql, string tableName)
        {
            var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], $@"\b{Regex.Escape(tableName)}\b", RegexOptions.IgnoreCase))
                    return i + 1; // السطر يبدأ من 1 وليس 0
            }
            return 0; // إذا لم يتم العثور
        }

        /// <summary>
        /// ايجاد أقرب كلمة معروفة من نوع معين (مثل "Table" أو "Keyword") بناءً على مسافة ليفنشتاين.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="expectedType"></param>
        /// <returns></returns>
        public static string FindClosestKeywordOfType(string input, string expectedType)
        {
            if (_knownWords == null || _knownWords.Count == 0)
                return string.Empty;

            string bestMatch = string.Empty;
            int minDistance = int.MaxValue;

            foreach (var pair in _knownWords)
            {
                if (!string.Equals(pair.Value, expectedType, StringComparison.OrdinalIgnoreCase))
                    continue;

                int distance = LevenshteinDistance(input.ToUpper(), pair.Key.ToUpper());
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestMatch = pair.Key;
                }
            }

            return minDistance <= 3 ? "أو يوجد خطأ آخر في السطر نفسه " + bestMatch + " قد تكون غير صحيحة" : string.Empty;
        }

        private static int LevenshteinDistance(string s, string t)
        {
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
       
        private static string FindClosestKnownWord(string input)
        {
            string bestMatch = string.Empty;
            int minDistance = int.MaxValue;

            foreach (var entry in _knownWords.Keys)
            {
                int distance = LevenshteinDistance(input.ToUpper(), entry.ToUpper());
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestMatch = entry;
                }
            }

            return minDistance <= 3 ? "أو يوجد خطأ آخر في السطر نفسه " + bestMatch +" قد تكون غير صحيحة"  : string.Empty;
        }

    }


}
