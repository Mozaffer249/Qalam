namespace Qalam.Infrastructure.Seeding.Data;

public sealed record LegalSeedSection(
    string AnchorKey,
    string TitleAr,
    string TitleEn,
    string? ContentAr,
    string? ContentEn,
    int DisplayOrder,
    IReadOnlyList<LegalSeedSection>? Children = null);

public sealed record LegalSeedDocument(
    string Code,
    string TitleAr,
    string TitleEn,
    int DisplayOrder,
    bool RequiresConsent,
    IReadOnlyList<LegalSeedSection> Sections);

/// <summary>
/// Initial legal document content transcribed from privacy.txt (Arabic bodies; English titles only).
/// </summary>
public static class LegalDocumentSeedData
{
    public static IReadOnlyList<LegalSeedDocument> GetDocuments() => new[]
    {
        BuildTerms(),
        BuildPrivacy(),
        BuildRefund(),
        BuildPricing()
    };

    private static LegalSeedDocument BuildTerms() => new(
        Code: "terms-conditions",
        TitleAr: "الشروط والأحكام",
        TitleEn: "Terms & Conditions",
        DisplayOrder: 1,
        RequiresConsent: true,
        Sections: new[]
        {
            new LegalSeedSection("terms-1", "التعريف بمنصة قلم", "Introduction to the Qalam Platform", null, null, 1),
            new LegalSeedSection("terms-2", "قبول الشروط عند إنشاء الحساب أو استخدام المنصة", "Acceptance of Terms Upon Account Creation or Platform Use", null, null, 2),
            new LegalSeedSection("terms-3", "أنواع الحسابات", "Account Types", null, null, 3),
            new LegalSeedSection("terms-4", "مسؤوليات كل نوع من المستخدمين", "Responsibilities of Each User Type", null, null, 4),
            new LegalSeedSection("terms-5", "تسجيل واعتماد المعلمين", "Teacher Registration and Approval", null, null, 5),
            new LegalSeedSection("terms-6", "الدورات الثابتة والمرنة", "Fixed and Flexible Courses", null, null, 6),
            new LegalSeedSection("terms-7", "الاشتراك في الدورات", "Course Subscription", null, null, 7),
            new LegalSeedSection("terms-8", "طلب الجلسات من معلم محدد", "Requesting Sessions from a Specific Teacher", null, null, 8),
            new LegalSeedSection("terms-9", "طلب الجلسات الموجه للمعلمين", "Open Session Requests Directed to Teachers", null, null, 9),
            new LegalSeedSection("terms-10", "العروض المقدمة من المعلمين", "Offers Submitted by Teachers", null, null, 10),
            new LegalSeedSection("terms-11", "الدفع وتأكيد الحجز", "Payment and Booking Confirmation", null, null, 11),
            new LegalSeedSection("terms-12", "جدولة الجلسات", "Session Scheduling", null, null, 12),
            new LegalSeedSection("terms-13", "تنفيذ الجلسات Online / In-Person", "Conducting Sessions Online / In-Person", null, null, 13),
            new LegalSeedSection("terms-14", "استخدام LiveKit للجلسات الإلكترونية", "Use of LiveKit for Online Sessions", null, null, 14),
            new LegalSeedSection("terms-15", "المحتوى والملفات والواجبات", "Content, Files, and Assignments", null, null, 15),
            new LegalSeedSection("terms-16", "التقييمات والمراجعات", "Ratings and Reviews", null, null, 16),
            new LegalSeedSection("terms-17", "الباقات المستقبلية", "Future Packages", null, null, 17),
            new LegalSeedSection("terms-18", "حقوق الملكية الفكرية", "Intellectual Property Rights", null, null, 18),
            new LegalSeedSection("terms-19", "السلوكيات المحظورة", "Prohibited Conduct", null, null, 19),
            new LegalSeedSection("terms-20", "تعليق أو إلغاء الحساب", "Account Suspension or Termination", null, null, 20),
            new LegalSeedSection("terms-21", "مسؤوليات المعلم", "Teacher Responsibilities", null, null, 21),
            new LegalSeedSection("terms-22", "مسؤوليات الطالب وولي الأمر", "Student and Guardian Responsibilities", null, null, 22),
            new LegalSeedSection("terms-23", "حدود مسؤولية المنصة", "Platform Liability Limitations", null, null, 23),
            new LegalSeedSection("terms-24", "الخدمات المقدمة من أطراف ثالثة", "Third-Party Services", null, null, 24),
            new LegalSeedSection("terms-25", "تعديل الخدمات والشروط", "Modification of Services and Terms", null, null, 25),
            new LegalSeedSection("terms-26", "القانون والاختصاص", "Governing Law and Jurisdiction", null, null, 26),
            new LegalSeedSection("terms-27", "التواصل والشكاوى", "Communication and Complaints", null, null, 27),
            new LegalSeedSection("terms-28", "تاريخ سريان الشروط", "Effective Date of the Terms", null, null, 28),
        });

    private static LegalSeedDocument BuildPrivacy() => new(
        Code: "privacy-policy",
        TitleAr: "سياسة الخصوصية",
        TitleEn: "Privacy Policy",
        DisplayOrder: 2,
        RequiresConsent: true,
        Sections: new[]
        {
            new LegalSeedSection(
                "privacy-collect",
                "البيانات التي تجمعها قلم",
                "Information We Collect",
                "<ul><li>الاسم.</li><li>رقم الجوال.</li><li>البريد الإلكتروني.</li><li>بيانات الحساب.</li><li>البيانات التعليمية.</li><li>بيانات المعلم ومؤهلاته وشهاداته.</li><li>بيانات الطلاب وأولياء الأمور.</li><li>بيانات الدورات والاشتراكات.</li><li>بيانات الجلسات.</li><li>الملفات والمرفقات.</li><li>التقييمات والمراجعات.</li><li>بيانات الدفع والمعاملات.</li><li>بيانات استخدام التطبيق والجهاز.</li><li>سجلات الدخول والأمان.</li></ul>",
                null,
                1),
            new LegalSeedSection(
                "privacy-use",
                "استخدام البيانات",
                "How We Use Information",
                "<p>تستخدم البيانات من أجل:</p><ul><li>إنشاء وإدارة الحساب.</li><li>التحقق من المستخدم.</li><li>اعتماد المعلمين.</li><li>إدارة الدورات.</li><li>مطابقة الطلبات.</li><li>جدولة الجلسات.</li><li>معالجة المدفوعات.</li><li>إرسال الإشعارات.</li><li>تقديم الدعم.</li><li>حماية المنصة.</li><li>تحسين الخدمات.</li><li>الالتزام بالمتطلبات النظامية.</li></ul>",
                null,
                2),
            new LegalSeedSection(
                "privacy-share",
                "مشاركة البيانات",
                "Data Sharing",
                "<p>قد تتم مشاركة البيانات بالقدر اللازم مع:</p><ul><li>المعلمين والطلاب عند ارتباطهم بدورة أو جلسة.</li><li>مزود الدفع.</li><li>مزودي الخدمات التقنية.</li><li>خدمات البريد والإشعارات.</li><li>خدمات البث المباشر.</li></ul><p><strong>بيانات المستخدمين الأساسية مستضافة ومعالجة داخل المملكة العربية السعودية</strong>، مع توضيح أي معالجة خارج المملكة مستقبلاً قبل تفعيلها وفق المتطلبات النظامية.</p>",
                null,
                3),
            new LegalSeedSection(
                "privacy-minors",
                "بيانات القُصّر",
                "Minors' Data",
                "<p>توضح السياسة:</p><ul><li>دور ولي الأمر.</li><li>إدارة بيانات الأبناء.</li><li>حدود ظهور بيانات الطالب.</li><li>حماية بيانات القاصرين.</li></ul>",
                null,
                4),
            new LegalSeedSection(
                "privacy-rights",
                "حقوق المستخدم",
                "User Rights",
                "<ul><li>الوصول إلى بياناته.</li><li>تصحيح البيانات.</li><li>تحديثها.</li><li>طلب حذفها حيثما يسمح النظام.</li><li>تقديم طلبات الخصوصية.</li><li>تقديم الشكاوى.</li></ul>",
                null,
                5),
            new LegalSeedSection(
                "privacy-security",
                "الأمان والاحتفاظ",
                "Security and Retention",
                "<ul><li>حماية الحسابات.</li><li>التحكم في الصلاحيات.</li><li>التشفير.</li><li>سجلات التدقيق.</li><li>النسخ الاحتياطي.</li><li>الاحتفاظ بالبيانات للمدة اللازمة نظامياً وتشغيلياً.</li></ul>",
                null,
                6),
        });

    private static LegalSeedDocument BuildRefund() => new(
        Code: "refund-policy",
        TitleAr: "سياسة الاسترداد والإلغاء",
        TitleEn: "Refund & Cancellation Policy",
        DisplayOrder: 3,
        RequiresConsent: false,
        Sections: new[]
        {
            new LegalSeedSection(
                "refund-scope",
                "أولاً: نطاق السياسة",
                "First: Policy Scope",
                "<p>تنطبق هذه السياسة على المبالغ المدفوعة مقابل:</p><ul><li>الاشتراك في الدورات.</li><li>الجلسات الخاصة.</li><li>الباقات عند إطلاقها.</li><li>أي خدمات تعليمية مدفوعة توفرها المنصة.</li></ul>",
                null,
                1),
            new LegalSeedSection(
                "refund-before-payment",
                "ثانياً: قبل تأكيد الدفع",
                "Second: Before Payment Confirmation",
                "<p>يمكن للطالب مراجعة:</p><ul><li>الخدمة.</li><li>عدد الجلسات.</li><li>مدة الجلسات.</li><li>السعر الإجمالي.</li><li>بيانات المعلم.</li><li>الجدول المطلوب.</li></ul><p>ولا يتم تنفيذ الدفع إلا بعد تأكيد الطالب.</p>",
                null,
                2),
            new LegalSeedSection(
                "refund-payment-failure",
                "ثالثاً: فشل عملية الدفع",
                "Third: Payment Failure",
                "<p>إذا لم تكتمل عملية الدفع:</p><ul><li>لا يعتبر الاشتراك أو الطلب مدفوعاً.</li><li>لا يتم إنشاء الحجز النهائي بناءً على عملية دفع فاشلة.</li><li>يمكن للمستخدم إعادة محاولة الدفع.</li></ul>",
                null,
                3),
            new LegalSeedSection(
                "refund-student-cancel",
                "رابعاً: إلغاء الطالب",
                "Fourth: Student Cancellation",
                "<p><strong>مبدئياً يمكن اعتماد القاعدة التالية:</strong></p><ul><li>قبل بدء أول جلسة: يحق للطالب طلب الإلغاء والاسترداد وفق شروط الخدمة.</li><li>بعد بدء الدورة أو الجلسات: يتم احتساب الجلسات التي تم تنفيذها، ويُنظر في استرداد الرصيد المتبقي وفق شروط الإلغاء.</li><li>الجلسة التي بدأت أو اكتملت لا تكون قابلة للاسترداد، إلا في الحالات التي تحددها المنصة.</li></ul>",
                null,
                4),
            new LegalSeedSection(
                "refund-teacher-cancel",
                "خامساً: إلغاء المعلم",
                "Fifth: Teacher Cancellation",
                "<p>إذا قام المعلم بإلغاء الطلب أو تعذر عليه تنفيذ الجلسات بعد الدفع:</p><ul><li>يحق للطالب استرداد المبلغ المتعلق بالخدمة غير المقدمة.</li><li>يمكن للمنصة اقتراح معلم بديل أو موعد بديل بموافقة الطالب.</li></ul>",
                null,
                5),
            new LegalSeedSection(
                "refund-student-no-show",
                "سادساً: عدم حضور الطالب",
                "Sixth: Student No-Show",
                "<p>إذا لم يحضر الطالب في الموعد المحدد دون إلغاء مسبق:</p><ul><li>يمكن اعتبار الجلسة منفذة وفق سياسة الحضور المعتمدة.</li><li>لا يكون الاسترداد تلقائياً.</li></ul>",
                null,
                6),
            new LegalSeedSection(
                "refund-teacher-no-show",
                "سابعاً: عدم حضور المعلم",
                "Seventh: Teacher No-Show",
                "<p>إذا لم يحضر المعلم:</p><ul><li>لا تحتسب الجلسة كجلسة منفذة.</li><li>يمكن إعادة جدولة الجلسة.</li><li>أو استرداد قيمتها وفق الحالة.</li></ul>",
                null,
                7),
            new LegalSeedSection(
                "refund-technical",
                "ثامناً: المشاكل التقنية",
                "Eighth: Technical Issues",
                "<p>إذا حدثت مشكلة تقنية أثناء جلسة Online:</p><ul><li>يتم التحقق من حالة الجلسة.</li><li>إذا تعذر تنفيذ الجلسة بسبب خلل تقني مؤثر، يمكن إعادة جدولتها.</li><li>وفي حال تعذر إعادة الجدولة، يمكن استرداد قيمة الجلسة وفق الحالة.</li></ul>",
                null,
                8),
            new LegalSeedSection(
                "refund-method",
                "تاسعاً: طريقة الاسترداد",
                "Ninth: Refund Method",
                "<p>يتم رد المبلغ إلى <strong>وسيلة الدفع الأصلية</strong> متى كان ذلك ممكناً، وقد تستغرق عملية ظهور المبلغ في حساب العميل مدة تعتمد على البنك أو مزود الدفع.</p>",
                null,
                9),
            new LegalSeedSection(
                "refund-fees",
                "عاشراً: رسوم الدفع",
                "Tenth: Payment Fees",
                "<p>إذا كانت هناك رسوم غير قابلة للاسترداد من مزود الدفع، يتم توضيحها للمستخدم قبل إتمام العملية، وتحدد معالجتها وفق شروط الخدمة والقوانين المعمول بها.</p>",
                null,
                10),
            new LegalSeedSection(
                "refund-request",
                "الحادي عشر: طلب الاسترداد",
                "Eleventh: Refund Request",
                "<p>يمكن للمستخدم تقديم طلب من خلال:</p><p><strong>الدعم → المدفوعات → طلب استرداد</strong></p><p>ويشمل الطلب:</p><ul><li>رقم العملية.</li><li>الخدمة.</li><li>سبب الاسترداد.</li><li>المرفقات عند الحاجة.</li></ul><p>وتقوم المنصة بمراجعة الطلب وإبلاغ المستخدم بالقرار.</p>",
                null,
                11),
        });

    private static LegalSeedDocument BuildPricing() => new(
        Code: "pricing-services",
        TitleAr: "عرض الخدمات والأسعار",
        TitleEn: "Services & Pricing",
        DisplayOrder: 4,
        RequiresConsent: false,
        Sections: new[]
        {
            new LegalSeedSection(
                "pricing-courses",
                "للدورات",
                "For Courses",
                "<ul><li>اسم الدورة.</li><li>المعلم.</li><li>المادة.</li><li>عدد الجلسات.</li><li>مدة الجلسة.</li><li>نوع التعليم.</li><li>السعر.</li><li>أي رسوم إضافية.</li><li>الإجمالي النهائي.</li></ul>",
                null,
                1),
            new LegalSeedSection(
                "pricing-private",
                "للجلسات الخاصة",
                "For Private Sessions",
                "<ul><li>المعلم.</li><li>المادة.</li><li>عدد الجلسات.</li><li>مدة الجلسة.</li><li>المواعيد المطلوبة.</li><li>السعر الذي قدمه المعلم.</li><li>الإجمالي.</li><li>حالة الطلب.</li></ul>",
                null,
                2),
            new LegalSeedSection(
                "pricing-checkout",
                "قبل الدفع",
                "Before Payment",
                "<p>يظهر للمستخدم بشكل واضح:</p><blockquote><p><strong>ملخص الطلب</strong></p><p>الخدمة: …</p><p>عدد الجلسات: …</p><p>السعر: …</p><p>الرسوم: …</p><p><strong>الإجمالي المستحق: … ريال</strong></p><p>[الدفع وتأكيد الطلب]</p></blockquote>",
                null,
                3),
        });
}
