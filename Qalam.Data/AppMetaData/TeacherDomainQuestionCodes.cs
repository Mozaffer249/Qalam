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

    // General skills (domain: skills) — legacy
    public const string SkillsExperienceYears = "skills_experience_years";
    public const string SkillsCertification = "skills_certification";

    public const string SoftSkillsExperienceYears = "soft_skills_experience_years";
    public const string SoftSkillsCertification = "soft_skills_certification";
    public const string LifeSkillsExperienceYears = "life_skills_experience_years";
    public const string LifeSkillsCertification = "life_skills_certification";
    public const string TechSkillsExperienceYears = "tech_skills_experience_years";
    public const string TechSkillsCertification = "tech_skills_certification";
    public const string HobbiesExperienceYears = "hobbies_experience_years";
    public const string HobbiesCertification = "hobbies_certification";
    public const string FinanceExperienceYears = "finance_experience_years";
    public const string FinanceCertification = "finance_certification";
    public const string KnowledgeExperienceYears = "knowledge_experience_years";
    public const string KnowledgeCertification = "knowledge_certification";

    // University (domain: university)
    public const string UniversityTeachingExperience = "university_teaching_experience";
    public const string UniversityDegreeCertificate = "university_degree_certificate";
}
