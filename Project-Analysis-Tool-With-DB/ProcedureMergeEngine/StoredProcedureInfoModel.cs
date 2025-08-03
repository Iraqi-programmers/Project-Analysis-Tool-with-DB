using CommunityToolkit.Mvvm.ComponentModel;

namespace SpAnalyzerTool.ProcedureMergeEngine
{
    /// <summary>
    /// يمثل إجراء مخزن (Stored Procedure) مع معلوماته الأساسية،
    /// مثل الاسم، تعريف الإنشاء (CREATE PROCEDURE)، وأسماء الأعمدة الناتجة منه.
    /// يُستخدم في عمليات التحليل والمقارنة والدمج لاحقًا.
    /// </summary>
    public partial class StoredProcedureInfo :ObservableObject
    {
        /// <summary>
        /// اسم الإجراء المخزن (مثل: sp_GetUsers).
        /// </summary>
        [ObservableProperty]
        public string name;

        /// <summary>
        /// نص تعريف البروسيجر الكامل (CREATE PROCEDURE ...).
        /// </summary>
        [ObservableProperty]
        public string definition;

        /// <summary>
        /// أسماء الأعمدة التي يرجعها هذا البروسيجر عند التنفيذ، بدون ترتيب.
        /// يتم استخدامها للمقارنة مع بروسيجرات أخرى.
        /// </summary>
        [ObservableProperty]
        public HashSet<string> outputColumnNames;

        /// <summary>
        /// اسم قاعدة البيانات التي أتى منها هذا البروسيجر (للتتبع فقط).
        /// </summary>
        [ObservableProperty]
        public string sourceDatabase;

        /// <summary>
        /// يُستخدم لتحديد ما إذا كان هذا الإجراء قد تم استبعاده لسبب ما (مثل التكرار).
        /// </summary>
        [ObservableProperty]
        public bool isExcluded;

        /// <summary>
        /// ينشئ كائن StoredProcedureInfo جديد.
        /// </summary>
        /// <param name="name">اسم الإجراء</param>
        /// <param name="definition">نص التعريف الكامل</param>
        /// <param name="outputColumns">أسماء الأعمدة الناتجة</param>
        /// <param name="sourceDatabase">اسم قاعدة المصدر</param>
        public StoredProcedureInfo(string name, string definition, IEnumerable<string> outputColumns, string sourceDatabase)
        {
            Name = name;
            Definition = definition;
            OutputColumnNames = new HashSet<string>(outputColumns, StringComparer.OrdinalIgnoreCase);
            SourceDatabase = sourceDatabase;
            IsExcluded = false;
        }

        public StoredProcedureInfo()
        {
            
        }
    }
}
