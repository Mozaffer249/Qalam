namespace Qalam.Data.AppMetaData;

/// <summary>Stable codes for seeded / system domain questions (unique per domain).</summary>
public static class TeacherDomainQuestionCodes
{
    // School (domain: school)
    public const string SchoolExperienceYears = "school_experience_years";
    public const string SchoolTeachingLicense = "school_teaching_license";

    // Quran (domain: quran)
    public const string QuranHasIjaza = "quran_has_ijaza";
    public const string QuranTeachingExperience = "quran_teaching_experience";
    public const string QuranIjazaCertificate = "quran_ijaza_certificate";

    // Languages (domain: language)
    public const string LanguageNativeSpeaker = "language_native_speaker";
    public const string LanguageProficiencyProof = "language_proficiency_proof";
}
