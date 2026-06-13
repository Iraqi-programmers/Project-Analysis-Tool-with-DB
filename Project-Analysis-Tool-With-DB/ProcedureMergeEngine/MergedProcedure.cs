using CommunityToolkit.Mvvm.ComponentModel;

namespace SpAnalyzerTool.ProcedureMergeEngine
{
    /// <summary>حالة الإجراء ضمن نتيجة دمج ملفّي نسخ احتياطي.</summary>
    public enum MergeStatus
    {
        /// <summary>موجود في الملف الأول فقط.</summary>
        OnlyInFirst,
        /// <summary>موجود في الملف الثاني فقط.</summary>
        OnlyInSecond,
        /// <summary>موجود في الملفين بتعريف متطابق.</summary>
        Identical,
        /// <summary>موجود في الملفين بتعريفين مختلفين (تعارض).</summary>
        Conflict
    }

    /// <summary>
    /// يمثّل إجراءً واحدًا في نتيجة الدمج مع تصنيف حالته، وإتاحة اختيار النسخة
    /// المعتمدة عند التعارض، والتحكم في تضمينه ضمن السكربت الناتج.
    /// </summary>
    public partial class MergedProcedure : ObservableObject
    {
        /// <summary>اسم الإجراء (مفتاح المطابقة بين الملفين، غير حسّاس لحالة الأحرف).</summary>
        public string Name { get; }

        /// <summary>تعريف الإجراء في الملف الأول (null إذا لم يوجد فيه).</summary>
        public string? FirstDefinition { get; }

        /// <summary>تعريف الإجراء في الملف الثاني (null إذا لم يوجد فيه).</summary>
        public string? SecondDefinition { get; }

        /// <summary>حالة الإجراء ضمن الدمج.</summary>
        public MergeStatus Status { get; }

        /// <summary>هل يُضمَّن هذا الإجراء في السكربت الناتج؟ (افتراضيًا نعم).</summary>
        [ObservableProperty]
        private bool _isIncluded = true;

        /// <summary>عند التعارض: true لاعتماد نسخة الملف الثاني، false لنسخة الملف الأول.</summary>
        [ObservableProperty]
        private bool _useSecond;

        /// <summary>ينشئ عنصر نتيجة دمج جديدًا ويحدّد النسخة الافتراضية عند التعارض.</summary>
        /// <param name="name">اسم الإجراء.</param>
        /// <param name="firstDefinition">تعريف الملف الأول أو null.</param>
        /// <param name="secondDefinition">تعريف الملف الثاني أو null.</param>
        /// <param name="status">حالة الإجراء.</param>
        public MergedProcedure(string name, string? firstDefinition, string? secondDefinition, MergeStatus status)
        {
            Name = name;
            FirstDefinition = firstDefinition;
            SecondDefinition = secondDefinition;
            Status = status;

            // عند التعارض نعتمد الملف الثاني افتراضيًا (يُفترض أنه الأحدث)، مع إتاحة التغيير للمستخدم.
            _useSecond = status == MergeStatus.Conflict;
        }

        /// <summary>التعريف المعتمد فعليًا للإخراج حسب الحالة واختيار المستخدم.</summary>
        public string EffectiveDefinition => Status switch
        {
            MergeStatus.OnlyInFirst => FirstDefinition ?? string.Empty,
            MergeStatus.OnlyInSecond => SecondDefinition ?? string.Empty,
            MergeStatus.Identical => FirstDefinition ?? SecondDefinition ?? string.Empty,
            MergeStatus.Conflict => UseSecond ? (SecondDefinition ?? string.Empty)
                                              : (FirstDefinition ?? string.Empty),
            _ => FirstDefinition ?? SecondDefinition ?? string.Empty
        };

        /// <summary>هل الحالة تعارض؟ (لإظهار خيار اختيار النسخة في الواجهة).</summary>
        public bool IsConflict => Status == MergeStatus.Conflict;

        /// <summary>نص توضيحي مختصر للحالة يظهر في الواجهة.</summary>
        public string StatusText => Status switch
        {
            MergeStatus.OnlyInFirst => "الملف الأول فقط",
            MergeStatus.OnlyInSecond => "الملف الثاني فقط",
            MergeStatus.Identical => "متطابق",
            MergeStatus.Conflict => "⚠ تعارض",
            _ => string.Empty
        };
    }
}
