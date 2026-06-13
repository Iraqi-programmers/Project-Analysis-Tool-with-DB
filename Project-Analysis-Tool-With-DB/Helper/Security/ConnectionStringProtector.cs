using System.Security.Cryptography;
using System.Text;

namespace SpAnalyzerTool.Helper
{
    /// <summary>
    /// يحمي القيم الحساسة (مثل سلسلة الاتصال) بتشفير DPAPI على نطاق المستخدم الحالي.
    /// التشفير مرتبط بحساب مستخدم Windows؛ لا يمكن فك تشفير القيمة على حساب أو جهاز آخر.
    /// يدعم التوافق الرجعي: القيم النصية القديمة (غير المشفّرة) تُقرأ كما هي.
    /// </summary>
    public static class ConnectionStringProtector
    {
        /// <summary>بادئة تُميّز القيم المشفّرة عن القيم النصية القديمة.</summary>
        private const string Prefix = "enc:v1:";

        /// <summary>إنتروبيا إضافية تربط البيانات المشفّرة بهذا التطبيق تحديدًا.</summary>
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SpAnalyzerTool.ConnString.v1");

        /// <summary>
        /// يشفّر نصًا عاديًا ويُعيده مُرمّزًا Base64 مع بادئة تمييز.
        /// النص الفارغ يُعاد فارغًا، والقيمة المشفّرة مسبقًا تُعاد دون تغيير.
        /// </summary>
        /// <param name="plainText">النص الحساس المراد حمايته.</param>
        /// <returns>القيمة المحمية، أو سلسلة فارغة عند الإدخال الفارغ.</returns>
        public static string Protect(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            if (IsProtected(plainText)) return plainText;

            byte[] data = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// يفك تشفير قيمة سبق حمايتها. القيم القديمة غير المشفّرة تُعاد كما هي (توافق رجعي).
        /// عند فشل فك التشفير (حساب/جهاز مختلف أو بيانات تالفة) تُعاد سلسلة فارغة.
        /// </summary>
        /// <param name="storedValue">القيمة المخزّنة (مشفّرة أو نص قديم).</param>
        /// <returns>النص العادي الأصلي، أو سلسلة فارغة عند التعذّر.</returns>
        public static string Unprotect(string? storedValue)
        {
            if (string.IsNullOrEmpty(storedValue)) return string.Empty;
            if (!IsProtected(storedValue)) return storedValue;

            try
            {
                byte[] encrypted = Convert.FromBase64String(storedValue.Substring(Prefix.Length));
                byte[] data = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                return string.Empty;
            }
        }

        /// <summary>يحدد ما إذا كانت القيمة محميّة بهذا النظام (تحمل البادئة).</summary>
        /// <param name="value">القيمة المراد فحصها.</param>
        /// <returns>true إذا كانت مشفّرة.</returns>
        public static bool IsProtected(string? value)
            => !string.IsNullOrEmpty(value) && value.StartsWith(Prefix, StringComparison.Ordinal);
    }
}
