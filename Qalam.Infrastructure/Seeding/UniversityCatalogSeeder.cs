using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// Seeds top 3 Saudi universities with a representative college → department → program → level → term → subject tree.
/// Idempotent: skips when Universities table already has rows.
/// </summary>
public static class UniversityCatalogSeeder
{
    private sealed record UniSeed(
        string Code,
        string NameAr,
        string NameEn,
        string City,
        List<CollegeSeed> Colleges);

    private sealed record CollegeSeed(string Code, string NameAr, string NameEn, List<DeptSeed> Departments);
    private sealed record DeptSeed(string Code, string NameAr, string NameEn, List<ProgSeed> Programs);
    private sealed record ProgSeed(
        string Code,
        string NameAr,
        string NameEn,
        string DegreeType,
        List<(string Code, string NameAr, string NameEn)> Subjects);

    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (await SeederHelper.HasAnyDataAsync(context.Universities))
            return;

        var universityDomain = await context.EducationDomains
            .FirstOrDefaultAsync(d => d.Code == "university")
            ?? throw new InvalidOperationException("University domain must be seeded before university catalog");

        var now = DateTime.UtcNow;
        var catalog = BuildCatalog();

        foreach (var uni in catalog)
        {
            var university = new University
            {
                Code = uni.Code,
                NameAr = uni.NameAr,
                NameEn = uni.NameEn,
                Country = "Saudi Arabia",
                City = uni.City,
                IsActive = true,
                CreatedAt = now,
            };
            context.Universities.Add(university);
            await context.SaveChangesAsync();

            foreach (var col in uni.Colleges)
            {
                var college = new College
                {
                    UniversityId = university.Id,
                    Code = col.Code,
                    NameAr = col.NameAr,
                    NameEn = col.NameEn,
                    IsActive = true,
                    CreatedAt = now,
                };
                context.Colleges.Add(college);
                await context.SaveChangesAsync();

                foreach (var dep in col.Departments)
                {
                    var department = new Department
                    {
                        CollegeId = college.Id,
                        Code = dep.Code,
                        NameAr = dep.NameAr,
                        NameEn = dep.NameEn,
                        IsActive = true,
                        CreatedAt = now,
                    };
                    context.Departments.Add(department);
                    await context.SaveChangesAsync();

                    foreach (var prog in dep.Programs)
                    {
                        var program = new AcademicProgram
                        {
                            DepartmentId = department.Id,
                            Code = prog.Code,
                            NameAr = prog.NameAr,
                            NameEn = prog.NameEn,
                            DegreeType = prog.DegreeType,
                            IsActive = true,
                            CreatedAt = now,
                        };
                        context.AcademicPrograms.Add(program);
                        await context.SaveChangesAsync();

                        // Levels 1–4
                        var levelIds = new List<int>();
                        for (var i = 1; i <= 4; i++)
                        {
                            var level = new EducationLevel
                            {
                                DomainId = universityDomain.Id,
                                AcademicProgramId = program.Id,
                                NameAr = $"السنة {ToArabicOrdinal(i)}",
                                NameEn = $"Year {i}",
                                OrderIndex = i,
                                IsActive = true,
                                CreatedAt = now,
                            };
                            context.EducationLevels.Add(level);
                            await context.SaveChangesAsync();
                            levelIds.Add(level.Id);
                        }

                        // Optional semesters
                        context.AcademicTerms.AddRange(
                            new AcademicTerm
                            {
                                AcademicProgramId = program.Id,
                                NameAr = "الفصل الأول",
                                NameEn = "Semester 1",
                                OrderIndex = 1,
                                IsMandatory = false,
                                IsActive = true,
                                CreatedAt = now,
                            },
                            new AcademicTerm
                            {
                                AcademicProgramId = program.Id,
                                NameAr = "الفصل الثاني",
                                NameEn = "Semester 2",
                                OrderIndex = 2,
                                IsMandatory = false,
                                IsActive = true,
                                CreatedAt = now,
                            });
                        await context.SaveChangesAsync();

                        // Subjects attached to program + first level by default
                        var subjectIndex = 0;
                        foreach (var sub in prog.Subjects)
                        {
                            var levelId = levelIds[Math.Min(subjectIndex, levelIds.Count - 1)];
                            context.Subjects.Add(new Subject
                            {
                                DomainId = universityDomain.Id,
                                UniversityId = university.Id,
                                AcademicProgramId = program.Id,
                                LevelId = levelId,
                                Code = sub.Code,
                                NameAr = sub.NameAr,
                                NameEn = sub.NameEn,
                                IsActive = true,
                                CreatedAt = now,
                            });
                            subjectIndex++;
                        }
                        await context.SaveChangesAsync();
                    }
                }
            }
        }
    }

    private static string ToArabicOrdinal(int n) => n switch
    {
        1 => "الأولى",
        2 => "الثانية",
        3 => "الثالثة",
        4 => "الرابعة",
        _ => n.ToString(),
    };

    private static List<UniSeed> BuildCatalog() =>
    [
        new(
            "ksu",
            "جامعة الملك سعود",
            "King Saud University",
            "Riyadh",
            [
                new("ksu-eng", "كلية الهندسة", "College of Engineering",
                [
                    new("ksu-eng-cpe", "قسم هندسة الحاسب", "Department of Computer Engineering",
                    [
                        new("ksu-eng-cpe-bsc", "بكالوريوس هندسة الحاسب", "Bachelor of Computer Engineering", "Bachelor",
                        [
                            ("ksu-cpe-calc1", "حساب التفاضل والتكامل 1", "Calculus I"),
                            ("ksu-cpe-prog", "أساسيات البرمجة", "Programming Fundamentals"),
                            ("ksu-cpe-ds", "هياكل البيانات", "Data Structures"),
                        ])
                    ])
                ]),
                new("ksu-sci", "كلية العلوم", "College of Science",
                [
                    new("ksu-sci-math", "قسم الرياضيات", "Department of Mathematics",
                    [
                        new("ksu-sci-math-bsc", "بكالوريوس الرياضيات", "Bachelor of Mathematics", "Bachelor",
                        [
                            ("ksu-math-calc1", "حساب التفاضل والتكامل 1", "Calculus I"),
                            ("ksu-math-lin", "الجبر الخطي", "Linear Algebra"),
                            ("ksu-math-prob", "الاحتمالات والإحصاء", "Probability and Statistics"),
                        ])
                    ])
                ]),
            ]),
        new(
            "kau",
            "جامعة الملك عبدالعزيز",
            "King Abdulaziz University",
            "Jeddah",
            [
                new("kau-cit", "كلية الحاسبات وتقنية المعلومات", "College of Computing & IT",
                [
                    new("kau-cit-cs", "قسم علوم الحاسب", "Department of Computer Science",
                    [
                        new("kau-cit-cs-bsc", "بكالوريوس علوم الحاسب", "Bachelor of Computer Science", "Bachelor",
                        [
                            ("kau-cs-intro", "مقدمة في علوم الحاسب", "Introduction to Computer Science"),
                            ("kau-cs-oop", "البرمجة كائنية التوجه", "Object-Oriented Programming"),
                            ("kau-cs-db", "قواعد البيانات", "Databases"),
                        ])
                    ])
                ]),
                new("kau-med", "كلية الطب", "College of Medicine",
                [
                    new("kau-med-med", "قسم الطب", "Department of Medicine",
                    [
                        new("kau-med-mbbs", "بكالوريوس الطب والجراحة", "MBBS", "MBBS",
                        [
                            ("kau-med-anat", "التشريح", "Anatomy"),
                            ("kau-med-phys", "علم وظائف الأعضاء", "Physiology"),
                            ("kau-med-bioc", "الكيمياء الحيوية", "Biochemistry"),
                        ])
                    ])
                ]),
            ]),
        new(
            "kfupm",
            "جامعة الملك فهد للبترول والمعادن",
            "King Fahd University of Petroleum & Minerals",
            "Dhahran",
            [
                new("kfupm-eng", "كلية الهندسة", "College of Engineering",
                [
                    new("kfupm-eng-ee", "قسم الهندسة الكهربائية", "Department of Electrical Engineering",
                    [
                        new("kfupm-eng-ee-bsc", "بكالوريوس الهندسة الكهربائية", "Bachelor of Electrical Engineering", "Bachelor",
                        [
                            ("kfupm-ee-circ", "الدوائر الكهربائية", "Electric Circuits"),
                            ("kfupm-ee-sig", "الإشارات والأنظمة", "Signals and Systems"),
                            ("kfupm-ee-em", "الكهرومغناطيسية", "Electromagnetics"),
                        ])
                    ])
                ]),
                new("kfupm-comp", "كلية الحوسبة", "College of Computing",
                [
                    new("kfupm-comp-se", "قسم هندسة البرمجيات", "Department of Software Engineering",
                    [
                        new("kfupm-comp-se-bsc", "بكالوريوس هندسة البرمجيات", "Bachelor of Software Engineering", "Bachelor",
                        [
                            ("kfupm-se-req", "هندسة المتطلبات", "Requirements Engineering"),
                            ("kfupm-se-design", "تصميم البرمجيات", "Software Design"),
                            ("kfupm-se-test", "اختبار البرمجيات", "Software Testing"),
                        ])
                    ])
                ]),
            ]),
    ];
}
