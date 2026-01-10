using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public class SaudiSubjectsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        var saudiCurriculumId = 1;
        var schoolDomainId = 1;
        
        // Get education levels
        var elementaryLevel = await context.EducationLevels
            .FirstOrDefaultAsync(el => el.CurriculumId == saudiCurriculumId && el.NameEn == "Elementary");
        var intermediateLevel = await context.EducationLevels
            .FirstOrDefaultAsync(el => el.CurriculumId == saudiCurriculumId && el.NameEn == "Intermediate");
        var secondaryLevel = await context.EducationLevels
            .FirstOrDefaultAsync(el => el.CurriculumId == saudiCurriculumId && el.NameEn == "Secondary");

        if (elementaryLevel == null || intermediateLevel == null || secondaryLevel == null)
        {
            throw new Exception("Saudi education levels must be seeded before subjects");
        }

        // Get grades
        var elementaryGrades = await context.Grades
            .Where(g => g.LevelId == elementaryLevel.Id)
            .OrderBy(g => g.OrderIndex)
            .ToListAsync();
        
        var intermediateGrades = await context.Grades
            .Where(g => g.LevelId == intermediateLevel.Id)
            .OrderBy(g => g.OrderIndex)
            .ToListAsync();
        
        var secondaryGrades = await context.Grades
            .Where(g => g.LevelId == secondaryLevel.Id)
            .OrderBy(g => g.OrderIndex)
            .ToListAsync();

        if (!await context.Subjects.AnyAsync(s => s.LevelId == elementaryLevel.Id))
        {
            var subjects = new List<Subject>();

            // ====== ELEMENTARY SUBJECTS ======
            // Common subjects for all elementary grades
            var elementaryCommonSubjects = new[]
            {
                new { NameAr = "اللغة العربية", NameEn = "Arabic Language", DescAr = "تعليم القراءة والكتابة والقواعد اللغوية", DescEn = "Teaching reading, writing, and grammar" },
                new { NameAr = "التربية الإسلامية", NameEn = "Islamic Education", DescAr = "تعليم القرآن الكريم والحديث والفقه", DescEn = "Teaching Quran, Hadith, and Islamic jurisprudence" },
                new { NameAr = "الرياضيات", NameEn = "Mathematics", DescAr = "تعليم الأعداد والعمليات الحسابية", DescEn = "Teaching numbers and arithmetic operations" },
                new { NameAr = "العلوم", NameEn = "Science", DescAr = "تعليم العلوم الطبيعية والبيئية", DescEn = "Teaching natural and environmental sciences" },
                new { NameAr = "التربية الفنية", NameEn = "Art Education", DescAr = "تنمية المهارات الفنية والإبداعية", DescEn = "Developing artistic and creative skills" },
                new { NameAr = "التربية البدنية", NameEn = "Physical Education", DescAr = "تنمية اللياقة البدنية والمهارات الحركية", DescEn = "Developing physical fitness and motor skills" }
            };

            foreach (var grade in elementaryGrades)
            {
                // Add common subjects for all grades
                foreach (var subject in elementaryCommonSubjects)
                {
                    subjects.Add(new Subject
                    {
                        DomainId = schoolDomainId,
                        CurriculumId = saudiCurriculumId,
                        LevelId = elementaryLevel.Id,
                        GradeId = grade.Id,
                        NameAr = subject.NameAr,
                        NameEn = subject.NameEn,
                        DescriptionAr = subject.DescAr,
                        DescriptionEn = subject.DescEn,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Add English Language starting from 4th grade
                if (grade.OrderIndex >= 4)
                {
                    subjects.Add(new Subject
                    {
                        DomainId = schoolDomainId,
                        CurriculumId = saudiCurriculumId,
                        LevelId = elementaryLevel.Id,
                        GradeId = grade.Id,
                        NameAr = "اللغة الإنجليزية",
                        NameEn = "English Language",
                        DescriptionAr = "تعليم اللغة الإنجليزية الأساسية",
                        DescriptionEn = "Teaching basic English language",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });

                    subjects.Add(new Subject
                    {
                        DomainId = schoolDomainId,
                        CurriculumId = saudiCurriculumId,
                        LevelId = elementaryLevel.Id,
                        GradeId = grade.Id,
                        NameAr = "المهارات الرقمية",
                        NameEn = "Digital Skills",
                        DescriptionAr = "تعليم المهارات الرقمية والحاسوبية الأساسية",
                        DescriptionEn = "Teaching basic digital and computer skills",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // ====== INTERMEDIATE SUBJECTS ======
            var intermediateSubjects = new[]
            {
                new { NameAr = "اللغة العربية", NameEn = "Arabic Language", DescAr = "تطوير مهارات اللغة العربية المتقدمة", DescEn = "Developing advanced Arabic language skills" },
                new { NameAr = "التربية الإسلامية", NameEn = "Islamic Education", DescAr = "دراسة القرآن والحديث والفقه", DescEn = "Study of Quran, Hadith, and Islamic jurisprudence" },
                new { NameAr = "الرياضيات", NameEn = "Mathematics", DescAr = "الجبر والهندسة والإحصاء", DescEn = "Algebra, geometry, and statistics" },
                new { NameAr = "العلوم", NameEn = "Science", DescAr = "الفيزياء والكيمياء والأحياء الأساسية", DescEn = "Basic physics, chemistry, and biology" },
                new { NameAr = "اللغة الإنجليزية", NameEn = "English Language", DescAr = "تطوير مهارات اللغة الإنجليزية", DescEn = "Developing English language skills" },
                new { NameAr = "الدراسات الاجتماعية", NameEn = "Social Studies", DescAr = "دراسة التاريخ والجغرافيا والوطنية", DescEn = "Study of history, geography, and citizenship" },
                new { NameAr = "التربية الفنية", NameEn = "Art Education", DescAr = "تنمية المهارات الفنية والتشكيلية", DescEn = "Developing artistic and creative skills" },
                new { NameAr = "التربية البدنية", NameEn = "Physical Education", DescAr = "تطوير اللياقة البدنية والمهارات الرياضية", DescEn = "Developing fitness and sports skills" },
                new { NameAr = "المهارات الرقمية", NameEn = "Digital Skills", DescAr = "مهارات الحاسب والبرمجة الأساسية", DescEn = "Computer and basic programming skills" },
                new { NameAr = "التفكير الناقد", NameEn = "Critical Thinking", DescAr = "تنمية مهارات التفكير والتحليل", DescEn = "Developing thinking and analytical skills" }
            };

            foreach (var grade in intermediateGrades)
            {
                foreach (var subject in intermediateSubjects)
                {
                    subjects.Add(new Subject
                    {
                        DomainId = schoolDomainId,
                        CurriculumId = saudiCurriculumId,
                        LevelId = intermediateLevel.Id,
                        GradeId = grade.Id,
                        NameAr = subject.NameAr,
                        NameEn = subject.NameEn,
                        DescriptionAr = subject.DescAr,
                        DescriptionEn = subject.DescEn,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // ====== SECONDARY SUBJECTS ======
            var secondarySubjects = new[]
            {
                new { NameAr = "اللغة العربية", NameEn = "Arabic Language", DescAr = "الأدب والنقد والبلاغة", DescEn = "Literature, criticism, and rhetoric" },
                new { NameAr = "التربية الإسلامية", NameEn = "Islamic Education", DescAr = "دراسات إسلامية متقدمة", DescEn = "Advanced Islamic studies" },
                new { NameAr = "الرياضيات", NameEn = "Mathematics", DescAr = "التفاضل والتكامل والرياضيات المتقدمة", DescEn = "Calculus and advanced mathematics" },
                new { NameAr = "اللغة الإنجليزية", NameEn = "English Language", DescAr = "اللغة الإنجليزية المتقدمة", DescEn = "Advanced English language" },
                new { NameAr = "الفيزياء", NameEn = "Physics", DescAr = "دراسة القوانين الفيزيائية والميكانيكا", DescEn = "Study of physical laws and mechanics" },
                new { NameAr = "الكيمياء", NameEn = "Chemistry", DescAr = "الكيمياء العامة والعضوية", DescEn = "General and organic chemistry" },
                new { NameAr = "الأحياء", NameEn = "Biology", DescAr = "علم الأحياء والأنظمة الحيوية", DescEn = "Biology and biological systems" },
                new { NameAr = "التاريخ", NameEn = "History", DescAr = "التاريخ الإسلامي والعالمي", DescEn = "Islamic and world history" },
                new { NameAr = "الجغرافيا", NameEn = "Geography", DescAr = "الجغرافيا الطبيعية والبشرية", DescEn = "Physical and human geography" },
                new { NameAr = "الحاسب الآلي", NameEn = "Computer Science", DescAr = "علوم الحاسب والبرمجة", DescEn = "Computer science and programming" },
                new { NameAr = "التربية البدنية", NameEn = "Physical Education", DescAr = "الرياضة واللياقة البدنية", DescEn = "Sports and physical fitness" }
            };

            foreach (var grade in secondaryGrades)
            {
                foreach (var subject in secondarySubjects)
                {
                    subjects.Add(new Subject
                    {
                        DomainId = schoolDomainId,
                        CurriculumId = saudiCurriculumId,
                        LevelId = secondaryLevel.Id,
                        GradeId = grade.Id,
                        NameAr = subject.NameAr,
                        NameEn = subject.NameEn,
                        DescriptionAr = subject.DescAr,
                        DescriptionEn = subject.DescEn,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await context.Subjects.AddRangeAsync(subjects);
            await context.SaveChangesAsync();
        }
    }
}

