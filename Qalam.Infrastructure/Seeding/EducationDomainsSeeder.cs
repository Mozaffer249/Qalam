using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class EducationDomainsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        // Safely check if data exists (returns false if table doesn't exist)
        if (!await SeederHelper.HasAnyDataAsync(context.EducationDomains))
        {
            var domains = new List<EducationDomain>
            {
                // School Domain
                new()
                {
                    NameAr = "تعليم مدرسي",
                    NameEn = "School Education",
                    Code = "school",
                    DescriptionAr = "التعليم المدرسي الأكاديمي بجميع مراحله",
                    DescriptionEn = "Academic school education at all levels",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EducationRule = new EducationRule
                    {
                        HasCurriculum = true,
                        HasEducationLevel = true,
                        HasGrade = true,
                        HasAcademicTerm = true,
                        HasContentUnits = true,
                        HasLessons = true,
                        RequiresQuranContentType = false,
                        RequiresQuranLevel = false,
                        MinSessions = 1,
                        MaxSessions = 200,
                        DefaultSessionDurationMinutes = 45,
                        MinGroupSize = 1,
                        MaxGroupSize = 30,
                        AllowExtension = true,
                        AllowFlexibleCourses = true,
                        CreatedAt = DateTime.UtcNow
                    }
                },
                // Quran Domain
                new()
                {
                    NameAr = "قرآن كريم",
                    NameEn = "Quran",
                    Code = "quran",
                    DescriptionAr = "تعليم القرآن الكريم حفظاً وتلاوة وتجويداً",
                    DescriptionEn = "Quran education: memorization, recitation, and tajweed",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EducationRule = new EducationRule
                    {
                        HasCurriculum = false,
                        HasEducationLevel = false,
                        HasGrade = false,
                        HasAcademicTerm = false,
                        HasContentUnits = true,
                        HasLessons = false,
                        RequiresQuranContentType = true,
                        RequiresQuranLevel = true,
                        MinSessions = 1,
                        MaxSessions = 300,
                        DefaultSessionDurationMinutes = 60,
                        MinGroupSize = 1,
                        MaxGroupSize = 10,
                        AllowExtension = true,
                        AllowFlexibleCourses = true,
                        CreatedAt = DateTime.UtcNow
                    }
                },
                // Language Domain
                new()
                {
                    NameAr = "لغات",
                    NameEn = "Languages",
                    Code = "language",
                    DescriptionAr = "تعليم اللغات الأجنبية والعربية",
                    DescriptionEn = "Foreign and Arabic language education",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EducationRule = new EducationRule
                    {
                        HasCurriculum = false,
                        HasEducationLevel = true,
                        HasGrade = false,
                        HasAcademicTerm = false,
                        HasContentUnits = true,
                        HasLessons = true,
                        RequiresQuranContentType = false,
                        RequiresQuranLevel = false,
                        MinSessions = 1,
                        MaxSessions = 150,
                        DefaultSessionDurationMinutes = 60,
                        MinGroupSize = 1,
                        MaxGroupSize = 15,
                        AllowExtension = true,
                        AllowFlexibleCourses = true,
                        CreatedAt = DateTime.UtcNow
                    }
                },
                // Skills Domain
                new()
                {
                    NameAr = "مهارات عامة",
                    NameEn = "General Skills",
                    Code = "skills",
                    DescriptionAr = "المهارات الحياتية والمهنية والتقنية",
                    DescriptionEn = "Life, professional, and technical skills",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EducationRule = new EducationRule
                    {
                        HasCurriculum = false,
                        HasEducationLevel = false,
                        HasGrade = false,
                        HasAcademicTerm = false,
                        HasContentUnits = true,
                        HasLessons = true,
                        RequiresQuranContentType = false,
                        RequiresQuranLevel = false,
                        MinSessions = 1,
                        MaxSessions = 100,
                        DefaultSessionDurationMinutes = 60,
                        MinGroupSize = 1,
                        MaxGroupSize = 20,
                        AllowExtension = true,
                        AllowFlexibleCourses = true,
                        CreatedAt = DateTime.UtcNow
                    }
                },
                // University Domain
                new()
                {
                    NameAr = "تعليم جامعي",
                    NameEn = "University Education",
                    Code = "university",
                    DescriptionAr = "التعليم الجامعي والدراسات العليا",
                    DescriptionEn = "University and higher education",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EducationRule = new EducationRule
                    {
                        HasCurriculum = true,
                        HasEducationLevel = true,
                        HasGrade = false,
                        HasAcademicTerm = true,
                        HasContentUnits = true,
                        HasLessons = true,
                        RequiresQuranContentType = false,
                        RequiresQuranLevel = false,
                        MinSessions = 1,
                        MaxSessions = 250,
                        DefaultSessionDurationMinutes = 90,
                        MinGroupSize = 1,
                        MaxGroupSize = 40,
                        AllowExtension = true,
                        AllowFlexibleCourses = true,
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };

            await context.EducationDomains.AddRangeAsync(domains);
            await context.SaveChangesAsync();
        }
    }
}

