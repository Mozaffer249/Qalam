using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class LanguageSubjectsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var languageDomainId = 3; // Languages domain

        // Get language levels
        var beginnerLevel = await context.EducationLevels
            .FirstOrDefaultAsync(el => el.DomainId == languageDomainId && el.NameEn == "Beginner Level");
        var intermediateLevel = await context.EducationLevels
            .FirstOrDefaultAsync(el => el.DomainId == languageDomainId && el.NameEn == "Intermediate Level");
        var advancedLevel = await context.EducationLevels
            .FirstOrDefaultAsync(el => el.DomainId == languageDomainId && el.NameEn == "Advanced Level");

        if (beginnerLevel == null || intermediateLevel == null || advancedLevel == null)
        {
            throw new Exception("Language levels must be seeded before language subjects");
        }

        // Get proficiency grades
        var grades = await context.Grades
            .Where(g => g.LevelId == beginnerLevel.Id || g.LevelId == intermediateLevel.Id || g.LevelId == advancedLevel.Id)
            .OrderBy(g => g.LevelId).ThenBy(g => g.OrderIndex)
            .ToListAsync();

        if (!await SeederHelper.HasAnyDataAsync(context.Subjects, s => s.DomainId == languageDomainId))
        {
            var subjects = new List<Subject>();

            // Languages to seed
            var languages = new[]
            {
                new { Code = "en", NameAr = "الإنجليزية", NameEn = "English" },
                new { Code = "fr", NameAr = "الفرنسية", NameEn = "French" },
                new { Code = "de", NameAr = "الألمانية", NameEn = "German" },
                new { Code = "tr", NameAr = "التركية", NameEn = "Turkish" },
                new { Code = "es", NameAr = "الإسبانية", NameEn = "Spanish" },
                new { Code = "zh", NameAr = "الصينية (ماندرين)", NameEn = "Chinese (Mandarin)" },
                new { Code = "ja", NameAr = "اليابانية", NameEn = "Japanese" },
                new { Code = "ko", NameAr = "الكورية", NameEn = "Korean" }
            };

            // Subject types for each proficiency level
            var subjectTypes = new[]
            {
                new { NameAr = "القواعد", NameEn = "Grammar", DescAr = "تعلم القواعد والتراكيب اللغوية", DescEn = "Learning grammar and language structures" },
                new { NameAr = "المحادثة", NameEn = "Conversation", DescAr = "ممارسة المحادثة والتواصل الشفهي", DescEn = "Practicing conversation and oral communication" },
                new { NameAr = "القراءة والكتابة", NameEn = "Reading & Writing", DescAr = "تطوير مهارات القراءة والكتابة", DescEn = "Developing reading and writing skills" },
                new { NameAr = "الاستماع والفهم", NameEn = "Listening Comprehension", DescAr = "تحسين مهارات الاستماع والفهم", DescEn = "Improving listening and comprehension skills" },
                new { NameAr = "المفردات", NameEn = "Vocabulary", DescAr = "توسيع الحصيلة اللغوية والمفردات", DescEn = "Expanding vocabulary and lexicon" }
            };

            // Create subjects for each language, level, and grade
            foreach (var language in languages)
            {
                foreach (var grade in grades)
                {
                    var level = grade.LevelId == beginnerLevel.Id ? beginnerLevel :
                               grade.LevelId == intermediateLevel.Id ? intermediateLevel : advancedLevel;

                    foreach (var subjectType in subjectTypes)
                    {
                        subjects.Add(new Subject
                        {
                            DomainId = languageDomainId,
                            LevelId = level.Id,
                            GradeId = grade.Id,
                            NameAr = $"{language.NameAr} - {subjectType.NameAr} ({grade.NameAr})",
                            NameEn = $"{language.NameEn} - {subjectType.NameEn} ({grade.NameEn})",
                            DescriptionAr = $"{subjectType.DescAr} - {language.NameAr}",
                            DescriptionEn = $"{subjectType.DescEn} - {language.NameEn}",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    // Add specialized subjects for advanced levels
                    if (level.Id == advancedLevel.Id)
                    {
                        subjects.Add(new Subject
                        {
                            DomainId = languageDomainId,
                            LevelId = level.Id,
                            GradeId = grade.Id,
                            NameAr = $"{language.NameAr} - الأدب والنصوص ({grade.NameAr})",
                            NameEn = $"{language.NameEn} - Literature & Texts ({grade.NameEn})",
                            DescriptionAr = $"دراسة الأدب والنصوص الكلاسيكية - {language.NameAr}",
                            DescriptionEn = $"Study of literature and classic texts - {language.NameEn}",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });

                        subjects.Add(new Subject
                        {
                            DomainId = languageDomainId,
                            LevelId = level.Id,
                            GradeId = grade.Id,
                            NameAr = $"{language.NameAr} - اللغة المهنية ({grade.NameAr})",
                            NameEn = $"{language.NameEn} - Business Language ({grade.NameEn})",
                            DescriptionAr = $"تعلم اللغة المتخصصة للأعمال والمهن - {language.NameAr}",
                            DescriptionEn = $"Learning specialized business and professional language - {language.NameEn}",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            // Add Arabic for non-native speakers
            subjects.Add(new Subject
            {
                DomainId = languageDomainId,
                LevelId = beginnerLevel.Id,
                GradeId = grades.First(g => g.LevelId == beginnerLevel.Id && g.OrderIndex == 1).Id,
                NameAr = "العربية لغير الناطقين بها - مبتدئ",
                NameEn = "Arabic for Non-Native Speakers - Beginner",
                DescriptionAr = "تعليم اللغة العربية للناطقين بغيرها - المستوى المبتدئ",
                DescriptionEn = "Teaching Arabic language for non-native speakers - Beginner level",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = languageDomainId,
                LevelId = intermediateLevel.Id,
                GradeId = grades.First(g => g.LevelId == intermediateLevel.Id && g.OrderIndex == 1).Id,
                NameAr = "العربية لغير الناطقين بها - متوسط",
                NameEn = "Arabic for Non-Native Speakers - Intermediate",
                DescriptionAr = "تعليم اللغة العربية للناطقين بغيرها - المستوى المتوسط",
                DescriptionEn = "Teaching Arabic language for non-native speakers - Intermediate level",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            subjects.Add(new Subject
            {
                DomainId = languageDomainId,
                LevelId = advancedLevel.Id,
                GradeId = grades.First(g => g.LevelId == advancedLevel.Id && g.OrderIndex == 1).Id,
                NameAr = "العربية لغير الناطقين بها - متقدم",
                NameEn = "Arabic for Non-Native Speakers - Advanced",
                DescriptionAr = "تعليم اللغة العربية للناطقين بغيرها - المستوى المتقدم",
                DescriptionEn = "Teaching Arabic language for non-native speakers - Advanced level",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await context.Subjects.AddRangeAsync(subjects);
            await context.SaveChangesAsync();
        }
    }
}

