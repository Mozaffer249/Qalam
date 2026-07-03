using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;

namespace Qalam.Data.AppMetaData;

/// <summary>
/// Sample domain questions seeded for school, quran, language, skills, and university domains.
/// Domain IDs are resolved at seed time by <see cref="TeacherDomainQuestionsSeeder"/>.
/// </summary>
public static class TeacherDomainQuestionsDefaults
{
    public const string DefaultExtensionsJson = "[\".pdf\",\".jpg\",\".jpeg\",\".png\"]";
    public const int DefaultMaxFileSizeBytes = 10 * 1024 * 1024;

    public static IReadOnlyList<TeacherDomainQuestion> Create(
        IReadOnlyDictionary<string, int> domainIdsByCode,
        DateTime? createdAt = null)
    {
        var now = createdAt ?? DateTime.UtcNow;
        var extensions = RegistrationRequirementExtensionsHelper.ToJson(new[] { ".pdf", ".jpg", ".jpeg", ".png" });

        var questions = new List<TeacherDomainQuestion>();

        if (domainIdsByCode.TryGetValue("school", out var schoolId))
        {
            questions.AddRange(CreateSchoolQuestions(schoolId, extensions, now));
        }

        if (domainIdsByCode.TryGetValue("quran", out var quranId))
        {
            questions.AddRange(CreateQuranQuestions(quranId, extensions, now));
        }

        if (domainIdsByCode.TryGetValue("language", out var languageId))
        {
            questions.AddRange(CreateLanguageQuestions(languageId, extensions, now));
        }

        if (domainIdsByCode.TryGetValue("skills", out var skillsId))
        {
            questions.AddRange(CreateSkillsQuestions(skillsId, extensions, now));
        }

        if (domainIdsByCode.TryGetValue("university", out var universityId))
        {
            questions.AddRange(CreateUniversityQuestions(universityId, extensions, now));
        }

        return questions;
    }

    private static IEnumerable<TeacherDomainQuestion> CreateSchoolQuestions(
        int domainId,
        string extensions,
        DateTime now) =>
    [
        new()
        {
            DomainId = domainId,
            Code = TeacherDomainQuestionCodes.SchoolExperienceYears,
            NameAr = "سنوات الخبرة في التدريس",
            NameEn = "Years of teaching experience",
            DescriptionAr = "عدد سنوات خبرتك في التعليم المدرسي",
            DescriptionEn = "How many years you have taught in school settings",
            RequirementType = RegistrationRequirementType.Text,
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = false,
            SortOrder = 10,
            MinCount = 1,
            MaxCount = 1,
            MaxLength = 100,
            IsSystem = true,
            CreatedAt = now
        },
        new()
        {
            DomainId = domainId,
            Code = TeacherDomainQuestionCodes.SchoolTeachingLicense,
            NameAr = "الرخصة المهنية للمعلمين",
            NameEn = "Professional teacher license",
            DescriptionAr = "رفع صورة أو PDF للرخصة المهنية للمعلمين (إن وجدت)",
            DescriptionEn = "Upload your professional teacher license if applicable",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = true,
            SortOrder = 20,
            MinCount = 0,
            MaxCount = 1,
            MaxFileSizeBytes = DefaultMaxFileSizeBytes,
            AllowedExtensionsJson = extensions,
            MapsToDocumentType = TeacherDocumentType.Other,
            IsSystem = true,
            CreatedAt = now
        }
    ];

    private static IEnumerable<TeacherDomainQuestion> CreateQuranQuestions(
        int domainId,
        string extensions,
        DateTime now) =>
    [
        new()
        {
            DomainId = domainId,
            Code = TeacherDomainQuestionCodes.QuranHasIjaza,
            NameAr = "هل لديك إجازة قرآنية؟",
            NameEn = "Do you hold a Quran ijaza?",
            DescriptionAr = "حدد ما إذا كنت حاصلاً على إجازة في القرآن",
            DescriptionEn = "Indicate whether you have been granted ijaza",
            RequirementType = RegistrationRequirementType.Boolean,
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = false,
            SortOrder = 10,
            MinCount = 1,
            MaxCount = 1,
            IsSystem = true,
            CreatedAt = now
        },
        new()
        {
            DomainId = domainId,
            Code = TeacherDomainQuestionCodes.QuranTeachingExperience,
            NameAr = "خبرة تدريس القرآن",
            NameEn = "Quran teaching experience",
            DescriptionAr = "صف بإيجاز خبرتك في حفظ أو تلاوة أو تجويد القرآن",
            DescriptionEn = "Briefly describe your Quran teaching background",
            RequirementType = RegistrationRequirementType.Text,
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = false,
            SortOrder = 20,
            MinCount = 1,
            MaxCount = 1,
            MaxLength = 500,
            IsSystem = true,
            CreatedAt = now
        },
        new()
        {
            DomainId = domainId,
            Code = TeacherDomainQuestionCodes.QuranIjazaCertificate,
            NameAr = "شهادة الإجازة",
            NameEn = "Ijaza certificate",
            DescriptionAr = "رفع شهادة الإجازة إن وجدت (تتطلب مراجعة الإدارة)",
            DescriptionEn = "Upload ijaza certificate if available (requires admin review)",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = false,
            RequiresAdminReview = true,
            SortOrder = 30,
            MinCount = 0,
            MaxCount = 1,
            MaxFileSizeBytes = DefaultMaxFileSizeBytes,
            AllowedExtensionsJson = extensions,
            MapsToDocumentType = TeacherDocumentType.Certificate,
            IsSystem = true,
            CreatedAt = now
        }
    ];

    private static IEnumerable<TeacherDomainQuestion> CreateLanguageQuestions(
        int domainId,
        string extensions,
        DateTime now) =>
    [
        new()
        {
            DomainId = domainId,
            Code = TeacherDomainQuestionCodes.LanguageNativeSpeaker,
            NameAr = "هل أنت متحدث أصلي؟",
            NameEn = "Are you a native speaker?",
            RequirementType = RegistrationRequirementType.Boolean,
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = false,
            SortOrder = 10,
            MinCount = 1,
            MaxCount = 1,
            IsSystem = true,
            CreatedAt = now
        },
        new()
        {
            DomainId = domainId,
            Code = TeacherDomainQuestionCodes.LanguageProficiencyProof,
            NameAr = "إثبات الكفاءة اللغوية",
            NameEn = "Language proficiency proof",
            DescriptionAr = "شهادة IELTS أو TOEFL أو ما يعادلها (تتطلب مراجعة الإدارة)",
            DescriptionEn = "IELTS, TOEFL, or equivalent certificate (requires admin review)",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = false,
            RequiresAdminReview = true,
            SortOrder = 20,
            MinCount = 0,
            MaxCount = 1,
            MaxFileSizeBytes = DefaultMaxFileSizeBytes,
            AllowedExtensionsJson = extensions,
            MapsToDocumentType = TeacherDocumentType.Other,
            IsSystem = true,
            CreatedAt = now
        }
    ];

    private static IEnumerable<TeacherDomainQuestion> CreateSkillsQuestions(
        int domainId,
        string extensions,
        DateTime now) =>
    [
        new()
        {
            DomainId = domainId,
            Code = TeacherDomainQuestionCodes.SkillsExperienceYears,
            NameAr = "سنوات الخبرة في تدريس المهارات",
            NameEn = "Years of skills teaching experience",
            DescriptionAr = "عدد سنوات خبرتك في تدريس المهارات العامة أو المهنية",
            DescriptionEn = "How many years you have taught life, professional, or technical skills",
            RequirementType = RegistrationRequirementType.Text,
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = false,
            SortOrder = 10,
            MinCount = 1,
            MaxCount = 1,
            MaxLength = 100,
            IsSystem = true,
            CreatedAt = now
        },
        new()
        {
            DomainId = domainId,
            Code = TeacherDomainQuestionCodes.SkillsCertification,
            NameAr = "شهادة مهنية أو تدريب",
            NameEn = "Professional or training certification",
            DescriptionAr = "رفع شهادة مهنية أو تدريب ذات صلة (إن وجدت)",
            DescriptionEn = "Upload a relevant professional or training certificate if available",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = false,
            RequiresAdminReview = true,
            SortOrder = 20,
            MinCount = 0,
            MaxCount = 1,
            MaxFileSizeBytes = DefaultMaxFileSizeBytes,
            AllowedExtensionsJson = extensions,
            MapsToDocumentType = TeacherDocumentType.Certificate,
            IsSystem = true,
            CreatedAt = now
        }
    ];

    private static IEnumerable<TeacherDomainQuestion> CreateUniversityQuestions(
        int domainId,
        string extensions,
        DateTime now) =>
    [
        new()
        {
            DomainId = domainId,
            Code = TeacherDomainQuestionCodes.UniversityTeachingExperience,
            NameAr = "خبرة التدريس الجامعي",
            NameEn = "University teaching experience",
            DescriptionAr = "صف بإيجاز خبرتك في التدريس الجامعي أو الدراسات العليا",
            DescriptionEn = "Briefly describe your university or higher-education teaching background",
            RequirementType = RegistrationRequirementType.Text,
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = false,
            SortOrder = 10,
            MinCount = 1,
            MaxCount = 1,
            MaxLength = 500,
            IsSystem = true,
            CreatedAt = now
        },
        new()
        {
            DomainId = domainId,
            Code = TeacherDomainQuestionCodes.UniversityDegreeCertificate,
            NameAr = "شهادة الدرجة العلمية",
            NameEn = "Degree certificate",
            DescriptionAr = "رفع شهادة الماجستير أو الدكتوراه أو ما يعادلهما",
            DescriptionEn = "Upload your master's, doctoral, or equivalent degree certificate",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = true,
            SortOrder = 20,
            MinCount = 1,
            MaxCount = 1,
            MaxFileSizeBytes = DefaultMaxFileSizeBytes,
            AllowedExtensionsJson = extensions,
            MapsToDocumentType = TeacherDocumentType.Certificate,
            IsSystem = true,
            CreatedAt = now
        }
    ];
}
