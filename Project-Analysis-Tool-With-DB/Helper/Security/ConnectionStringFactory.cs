using Microsoft.Data.SqlClient;

namespace SpAnalyzerTool.Helper
{
    /// <summary>
    /// مصنع مركزي لبناء سلاسل اتصال SQL Server بأمان عبر <see cref="SqlConnectionStringBuilder"/>.
    /// الهدف منع حقن سلسلة الاتصال (Connection String Injection) عند دمج إدخالات المستخدم
    /// مثل اسم الخادم أو قاعدة البيانات، وتوحيد إعدادات الاتصال في مكان واحد (DRY).
    /// </summary>
    public static class ConnectionStringFactory
    {
        /// <summary>
        /// يبني سلسلة اتصال موثوقة (Integrated Security) لخادم وقاعدة محددين.
        /// كل القيم تُسند عبر خصائص الباني فيتولّى الاقتباس والهروب الصحيح تلقائيًا.
        /// </summary>
        /// <param name="server">اسم الخادم أو النسخة (instance) — إدخال مستخدم محتمل.</param>
        /// <param name="database">اسم قاعدة البيانات؛ يُستخدم master عند الإغفال.</param>
        /// <param name="connectTimeoutSeconds">مهلة الاتصال بالثواني (افتراضي 30).</param>
        /// <returns>سلسلة اتصال مُهيّأة بأمان.</returns>
        /// <exception cref="ArgumentException">يُرمى إذا كان اسم الخادم فارغًا.</exception>
        public static string Build(string server, string? database = null, int connectTimeoutSeconds = 30)
        {
            if (string.IsNullOrWhiteSpace(server))
                throw new ArgumentException("اسم الخادم مطلوب.", nameof(server));

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = string.IsNullOrWhiteSpace(database) ? "master" : database,
                IntegratedSecurity = true,
                Encrypt = false,
                TrustServerCertificate = true,
                ConnectTimeout = connectTimeoutSeconds
            };

            return builder.ConnectionString;
        }

        /// <summary>
        /// يُنشئ سلسلة اتصال جديدة من سلسلة قائمة مع تبديل قاعدة البيانات الهدف فقط،
        /// بأمان ودون إعادة بناء السلسلة نصيًا.
        /// </summary>
        /// <param name="baseConnectionString">سلسلة الاتصال الأساسية.</param>
        /// <param name="database">اسم قاعدة البيانات الجديدة.</param>
        /// <returns>سلسلة اتصال بنفس الإعدادات مع القاعدة المحددة.</returns>
        /// <exception cref="ArgumentException">يُرمى إذا كانت السلسلة الأساسية فارغة.</exception>
        public static string WithDatabase(string baseConnectionString, string database)
        {
            if (string.IsNullOrWhiteSpace(baseConnectionString))
                throw new ArgumentException("سلسلة الاتصال الأساسية مطلوبة.", nameof(baseConnectionString));

            var builder = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = database
            };

            return builder.ConnectionString;
        }
    }
}
