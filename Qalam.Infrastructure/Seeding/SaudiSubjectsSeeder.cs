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

        if (!await SeederHelper.HasAnyDataAsync(context.Subjects, s => s.LevelId == elementaryLevel.Id))
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

            // Seed ContentUnits for elementary subjects
            await SeedElementaryContentUnitsAsync(context, saudiCurriculumId, elementaryLevel.Id);
        }
    }

    /// <summary>
    /// Seeds ContentUnits for Saudi elementary subjects with realistic curriculum units distributed across 3 terms
    /// </summary>
    private static async Task SeedElementaryContentUnitsAsync(ApplicationDBContext context, int curriculumId, int levelId)
    {
        // Check if content units already exist for elementary subjects
        var existingUnits = await context.ContentUnits
            .AnyAsync(cu => cu.Subject.LevelId == levelId && cu.UnitTypeCode == "SchoolUnit");

        if (existingUnits)
            return; // Already seeded

        // Get academic terms
        var terms = await context.AcademicTerms
            .Where(t => t.CurriculumId == curriculumId)
            .OrderBy(t => t.OrderIndex)
            .ToListAsync();

        if (terms.Count < 3)
            throw new Exception("Saudi academic terms must be seeded before content units");

        var term1 = terms[0];
        var term2 = terms[1];
        var term3 = terms[2];

        // Get all elementary subjects
        var elementarySubjects = await context.Subjects
            .Where(s => s.LevelId == levelId && s.CurriculumId == curriculumId)
            .ToListAsync();

        var contentUnits = new List<ContentUnit>();

        foreach (var subject in elementarySubjects)
        {
            List<(string NameAr, string NameEn, int TermId)> units;

            // Define units based on subject name
            switch (subject.NameEn)
            {
                case "Arabic Language":
                    units = new List<(string, string, int)>
                    {
                        ("الوحدة الأولى: الحروف والأصوات", "Unit 1: Letters and Sounds", term1.Id),
                        ("الوحدة الثانية: الكلمات والجمل", "Unit 2: Words and Sentences", term1.Id),
                        ("الوحدة الثالثة: القراءة الأساسية", "Unit 3: Basic Reading", term1.Id),
                        ("الوحدة الرابعة: الكتابة الأساسية", "Unit 4: Basic Writing", term2.Id),
                        ("الوحدة الخامسة: القصص القصيرة", "Unit 5: Short Stories", term2.Id),
                        ("الوحدة السادسة: الأناشيد", "Unit 6: Poems", term2.Id),
                        ("الوحدة السابعة: المحادثة", "Unit 7: Conversation", term3.Id),
                        ("الوحدة الثامنة: المراجعة العامة", "Unit 8: General Review", term3.Id)
                    };
                    break;

                case "Mathematics":
                    units = new List<(string, string, int)>
                    {
                        ("الوحدة الأولى: الأعداد من 1 إلى 10", "Unit 1: Numbers 1-10", term1.Id),
                        ("الوحدة الثانية: الجمع الأساسي", "Unit 2: Basic Addition", term1.Id),
                        ("الوحدة الثالثة: الطرح الأساسي", "Unit 3: Basic Subtraction", term1.Id),
                        ("الوحدة الرابعة: الأشكال الهندسية", "Unit 4: Geometric Shapes", term2.Id),
                        ("الوحدة الخامسة: القياس", "Unit 5: Measurement", term2.Id),
                        ("الوحدة السادسة: الأنماط", "Unit 6: Patterns", term2.Id),
                        ("الوحدة السابعة: الوقت", "Unit 7: Time", term3.Id),
                        ("الوحدة الثامنة: النقود", "Unit 8: Money", term3.Id),
                        ("الوحدة التاسعة: حل المسائل", "Unit 9: Problem Solving", term3.Id)
                    };
                    break;

                case "Islamic Education":
                    units = new List<(string, string, int)>
                    {
                        ("الوحدة الأولى: أركان الإسلام", "Unit 1: Pillars of Islam", term1.Id),
                        ("الوحدة الثانية: السور القصيرة", "Unit 2: Short Surahs", term1.Id),
                        ("الوحدة الثالثة: الوضوء والطهارة", "Unit 3: Wudu and Purity", term1.Id),
                        ("الوحدة الرابعة: الصلاة", "Unit 4: Prayer", term2.Id),
                        ("الوحدة الخامسة: الأخلاق الإسلامية", "Unit 5: Islamic Morals", term2.Id),
                        ("الوحدة السادسة: قصص الأنبياء", "Unit 6: Stories of Prophets", term2.Id),
                        ("الوحدة السابعة: الأذكار اليومية", "Unit 7: Daily Remembrances", term3.Id),
                        ("الوحدة الثامنة: السيرة النبوية", "Unit 8: Prophet's Biography", term3.Id),
                        ("الوحدة التاسعة: المراجعة", "Unit 9: Review", term3.Id)
                    };
                    break;

                case "Science":
                    units = new List<(string, string, int)>
                    {
                        ("الوحدة الأولى: الكائنات الحية", "Unit 1: Living Things", term1.Id),
                        ("الوحدة الثانية: النباتات", "Unit 2: Plants", term1.Id),
                        ("الوحدة الثالثة: الحيوانات", "Unit 3: Animals", term2.Id),
                        ("الوحدة الرابعة: الماء والهواء", "Unit 4: Water and Air", term2.Id),
                        ("الوحدة الخامسة: الأرض والفضاء", "Unit 5: Earth and Space", term3.Id),
                        ("الوحدة السادسة: الطقس", "Unit 6: Weather", term3.Id)
                    };
                    break;

                case "Art Education":
                    units = new List<(string, string, int)>
                    {
                        ("الوحدة الأولى: الألوان", "Unit 1: Colors", term1.Id),
                        ("الوحدة الثانية: الرسم الأساسي", "Unit 2: Basic Drawing", term1.Id),
                        ("الوحدة الثالثة: الأشغال اليدوية", "Unit 3: Handicrafts", term2.Id),
                        ("الوحدة الرابعة: الأشكال الفنية", "Unit 4: Artistic Shapes", term2.Id),
                        ("الوحدة الخامسة: الفنون التقليدية", "Unit 5: Traditional Arts", term3.Id),
                        ("الوحدة السادسة: المشروع الفني", "Unit 6: Art Project", term3.Id)
                    };
                    break;

                case "Physical Education":
                    units = new List<(string, string, int)>
                    {
                        ("الوحدة الأولى: اللياقة البدنية", "Unit 1: Physical Fitness", term1.Id),
                        ("الوحدة الثانية: الحركات الأساسية", "Unit 2: Basic Movements", term1.Id),
                        ("الوحدة الثالثة: الألعاب الجماعية", "Unit 3: Team Games", term2.Id),
                        ("الوحدة الرابعة: المهارات الحركية", "Unit 4: Motor Skills", term2.Id),
                        ("الوحدة الخامسة: الرياضات الفردية", "Unit 5: Individual Sports", term3.Id),
                        ("الوحدة السادسة: السلامة الرياضية", "Unit 6: Sports Safety", term3.Id)
                    };
                    break;

                case "English Language":
                    units = new List<(string, string, int)>
                    {
                        ("الوحدة الأولى: الحروف والأصوات", "Unit 1: Letters and Sounds", term1.Id),
                        ("الوحدة الثانية: الكلمات الأساسية", "Unit 2: Basic Words", term1.Id),
                        ("الوحدة الثالثة: الجمل البسيطة", "Unit 3: Simple Sentences", term2.Id),
                        ("الوحدة الرابعة: المحادثات اليومية", "Unit 4: Daily Conversations", term2.Id),
                        ("الوحدة الخامسة: القصص والأغاني", "Unit 5: Stories and Songs", term3.Id),
                        ("الوحدة السادسة: المراجعة", "Unit 6: Review", term3.Id)
                    };
                    break;

                case "Digital Skills":
                    units = new List<(string, string, int)>
                    {
                        ("الوحدة الأولى: مقدمة في الحاسب", "Unit 1: Introduction to Computer", term1.Id),
                        ("الوحدة الثانية: استخدام لوحة المفاتيح", "Unit 2: Using Keyboard", term1.Id),
                        ("الوحدة الثالثة: الرسم الرقمي", "Unit 3: Digital Drawing", term2.Id),
                        ("الوحدة الرابعة: البرمجة البسيطة", "Unit 4: Simple Programming", term2.Id),
                        ("الوحدة الخامسة: الإنترنت الآمن", "Unit 5: Safe Internet", term3.Id),
                        ("الوحدة السادسة: المشروع الرقمي", "Unit 6: Digital Project", term3.Id)
                    };
                    break;

                default:
                    continue; // Skip unknown subjects
            }

            // Create ContentUnits for this subject
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                contentUnits.Add(new ContentUnit
                {
                    SubjectId = subject.Id,
                    TermId = unit.TermId,
                    NameAr = unit.NameAr,
                    NameEn = unit.NameEn,
                    OrderIndex = i + 1,
                    UnitTypeCode = "SchoolUnit",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (contentUnits.Any())
        {
            await context.ContentUnits.AddRangeAsync(contentUnits);
            await context.SaveChangesAsync();
        }
    }
}

