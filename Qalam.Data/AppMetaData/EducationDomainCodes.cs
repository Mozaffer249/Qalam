namespace Qalam.Data.AppMetaData;

/// <summary>Stable education domain codes. Never use database ids.</summary>
public static class EducationDomainCodes
{
    public const string School = "school";
    public const string Quran = "quran";
    public const string Language = "language";
    public const string Skills = "skills";
    public const string University = "university";
    public const string SoftSkills = "soft-skills";
    public const string LifeSkills = "life-skills";
    public const string TechSkills = "tech-skills";
    public const string Hobbies = "hobbies";
    public const string Finance = "finance";
    public const string Knowledge = "knowledge";

    public static readonly string[] Wave1SplitFromSkills =
    [
        SoftSkills,
        LifeSkills,
        TechSkills,
        Hobbies,
        Finance,
        Knowledge
    ];
}
