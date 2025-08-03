using Newtonsoft.Json.Linq;
using SpAnalyzerTool.Models;
using System.IO;
using System.Text.Json;

namespace SpAnalyzerTool.Helper
{
    public static class SettingsHelper
    {
        /// <summary>
        /// يُرجع المسار الكامل لملف الإعدادات داخل مجلد التطبيق.
        /// </summary>
        public static string GetSettingsPath(string fileName)
            => Path.Combine(AppContext.BaseDirectory, fileName);

        /// <summary>
        /// تحميل إعدادات كاملة من ملف JSON وتحويلها إلى كائن من النوع المحدد.
        /// </summary>
        public static T Load<T>(string fileName) where T : new()
        {
            string path = GetSettingsPath(fileName);

            if (!File.Exists(path))
                return new T();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json) ?? new T();
        }

        /// <summary>
        /// حفظ الكائن بالكامل إلى ملف JSON واستبدال كل محتوياته.
        /// </summary>
        public static void Save<T>(string fileName, T settings)
        {
            string path = GetSettingsPath(fileName);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// تعديل قيم محددة فقط داخل ملف JSON دون التأثير على باقي المفاتيح.
        /// </summary>
        public static void Update(string fileName, object newValues)
        {
            string path = GetSettingsPath(fileName);
            var json = File.Exists(path) ? File.ReadAllText(path) : "{}";

            var jObj = JObject.Parse(json);
            var newProps = JObject.FromObject(newValues);

            foreach (var prop in newProps.Properties())
            {
                jObj[prop.Name] = prop.Value;
            }

            File.WriteAllText(path, jObj.ToString(Newtonsoft.Json.Formatting.Indented));
        }


    }
}
