using SpAnalyzerTool.Models;

namespace SpAnalyzerTool.Helper
{
    /// <summary>
    /// خدمة مركزية لتحميل وحفظ إعدادات التطبيق مع حماية سلسلة الاتصال تلقائيًا عبر DPAPI.
    /// تُخفي تفاصيل التشفير والمسار عن بقية الكود (مبدأ المسؤولية الواحدة + DRY)،
    /// فيتعامل بقية التطبيق مع سلسلة الاتصال كنص عادي دائمًا.
    /// </summary>
    public static class AppSettingsService
    {
        /// <summary>المسار النسبي الموحّد لملف الإعدادات داخل مجلد التطبيق.</summary>
        public const string FileName = "SettingesFiles\\appsettings.json";

        /// <summary>
        /// يُحمّل الإعدادات من القرص ويفك تشفير سلسلة الاتصال لتعود كنص عادي جاهز للاستخدام.
        /// </summary>
        /// <returns>كائن الإعدادات مع سلسلة اتصال غير مشفّرة.</returns>
        public static AppSettings Load()
        {
            var settings = SettingsHelper.Load<AppSettings>(FileName);
            settings.DefaultConnectionString =
                ConnectionStringProtector.Unprotect(settings.DefaultConnectionString);
            return settings;
        }

        /// <summary>
        /// يحفظ الإعدادات بعد تشفير سلسلة الاتصال، دون تعديل الكائن الأصلي في الذاكرة
        /// (يبقى يحمل النص العادي ليستمر التطبيق في استخدامه بعد الحفظ).
        /// </summary>
        /// <param name="settings">الإعدادات المراد حفظها (تحوي نصًا عاديًا).</param>
        /// <exception cref="ArgumentNullException">يُرمى إذا كان الكائن null.</exception>
        public static void Save(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var toPersist = new AppSettings
            {
                DefaultConnectionString =
                    ConnectionStringProtector.Protect(settings.DefaultConnectionString),
                FileSize = settings.FileSize
            };

            SettingsHelper.Save(FileName, toPersist);
        }
    }
}
