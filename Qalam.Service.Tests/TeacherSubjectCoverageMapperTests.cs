using Qalam.Core.Mapping;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Quran;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Service.Tests;

public class TeacherSubjectCoverageMapperTests
{
    [Fact]
    public void ApplyCoverage_Quran_KeepsContentTypeWritableEducationLevelOrder()
    {
        var src = new TeacherSubject
        {
            CanTeachFullSubject = true,
            Subject = new Subject
            {
                NameAr = "قرآن",
                NameEn = "Quran",
                Domain = new EducationDomain { Code = "quran", NameAr = "قرآن", NameEn = "Quran" },
            },
            QuranContentTypes =
            {
                new TeacherSubjectQuranContentType
                {
                    QuranContentType = new QuranContentType { NameAr = "التجويد", NameEn = "Tajweed" },
                },
            },
            WritableFilters =
            {
                new TeacherSubjectWritableFilter
                {
                    WritableFilterValue = new WritableFilterValue { NameAr = "حفص", NameEn = "Hafs" },
                },
            },
            EducationLevels =
            {
                new TeacherSubjectEducationLevel
                {
                    EducationLevel = new EducationLevel { NameAr = "الصغار", NameEn = "Children" },
                },
            },
        };

        var dest = new TeacherSubjectResponseDto();
        TeacherSubjectCoverageMapper.ApplyCoverage(src, dest);

        Assert.Equal("التجويد · حفص · الصغار", dest.CoverageSummaryAr);
        Assert.Equal("Tajweed · Hafs · Children", dest.CoverageSummaryEn);
        Assert.Equal(["QuranContentType", "WritableFilter", "EducationLevel"], dest.CoverageLabels.Select(l => l.Kind).ToList());
    }

    [Fact]
    public void ApplyCoverage_StackedDomain_IncludesParentEducationLevelsAndWritables()
    {
        var src = new TeacherSubject
        {
            CanTeachFullSubject = true,
            Subject = new Subject
            {
                NameAr = "الطبخ",
                NameEn = "Cooking",
                Domain = new EducationDomain { Code = "life-skills", NameAr = "مهارات", NameEn = "Life skills" },
                ParentSubject = new Subject { NameAr = "الطبخ والضيافة", NameEn = "Cooking & hospitality" },
            },
            EducationLevels =
            {
                new TeacherSubjectEducationLevel
                {
                    EducationLevel = new EducationLevel { NameAr = "مبتدئ", NameEn = "Beginner" },
                },
                new TeacherSubjectEducationLevel
                {
                    EducationLevel = new EducationLevel { NameAr = "متوسط", NameEn = "Intermediate" },
                },
            },
            WritableFilters =
            {
                new TeacherSubjectWritableFilter
                {
                    WritableFilterValue = new WritableFilterValue { NameAr = "عملي", NameEn = "Practical" },
                },
            },
        };

        var dest = new TeacherSubjectResponseDto();
        TeacherSubjectCoverageMapper.ApplyCoverage(src, dest);

        Assert.Equal("الطبخ والضيافة · مبتدئ · متوسط · عملي", dest.CoverageSummaryAr);
        Assert.Contains(dest.CoverageLabels, l => l.Kind == "ParentSubject");
        Assert.Contains(dest.CoverageLabels, l => l.Kind == "EducationLevel" && l.NameAr == "مبتدئ");
        Assert.Contains(dest.CoverageLabels, l => l.Kind == "WritableFilter");
    }

    [Fact]
    public void ApplyCoverage_FlatDomain_IncludesEducationLevelsAndWritables()
    {
        var src = new TeacherSubject
        {
            CanTeachFullSubject = true,
            Subject = new Subject
            {
                NameAr = "برمجة",
                NameEn = "Programming",
                Domain = new EducationDomain { Code = "tech-skills", NameAr = "تقنية", NameEn = "Tech" },
            },
            EducationLevels =
            {
                new TeacherSubjectEducationLevel
                {
                    EducationLevel = new EducationLevel { NameAr = "متقدم", NameEn = "Advanced" },
                },
            },
            WritableFilters =
            {
                new TeacherSubjectWritableFilter
                {
                    WritableFilterValue = new WritableFilterValue { NameAr = "Python", NameEn = "Python" },
                },
            },
        };

        var dest = new TeacherSubjectResponseDto();
        TeacherSubjectCoverageMapper.ApplyCoverage(src, dest);

        Assert.Equal("متقدم · Python", dest.CoverageSummaryAr);
        Assert.DoesNotContain(dest.CoverageLabels, l => l.Kind == "ParentSubject");
    }

    [Fact]
    public void ApplyCoverage_School_IncludesCatalogPathAndWritables()
    {
        var src = new TeacherSubject
        {
            CanTeachFullSubject = true,
            Subject = new Subject
            {
                NameAr = "رياضيات",
                NameEn = "Mathematics",
                Domain = new EducationDomain { Code = "school", NameAr = "مدرسة", NameEn = "School" },
                Curriculum = new Curriculum { NameAr = "السعودي", NameEn = "Saudi" },
                Level = new EducationLevel { NameAr = "ابتدائي", NameEn = "Primary" },
                Grade = new Grade { NameAr = "الثالث", NameEn = "Grade 3" },
            },
            WritableFilters =
            {
                new TeacherSubjectWritableFilter
                {
                    WritableFilterValue = new WritableFilterValue { NameAr = "تأسيس", NameEn = "Foundation" },
                },
            },
        };

        var dest = new TeacherSubjectResponseDto();
        TeacherSubjectCoverageMapper.ApplyCoverage(src, dest);

        Assert.Equal("السعودي · ابتدائي · الثالث · تأسيس", dest.CoverageSummaryAr);
        Assert.Contains(dest.CoverageLabels, l => l.Kind == "Curriculum");
        Assert.Contains(dest.CoverageLabels, l => l.Kind == "Stage");
        Assert.Contains(dest.CoverageLabels, l => l.Kind == "Grade");
    }

    [Fact]
    public void ApplyCoverage_University_IncludesInstitutionPathAndDoesNotDuplicateYear()
    {
        var year = new EducationLevel { Id = 10, NameAr = "السنة الأولى", NameEn = "Year 1" };
        var src = new TeacherSubject
        {
            CanTeachFullSubject = true,
            Subject = new Subject
            {
                NameAr = "قواعد البيانات",
                NameEn = "Databases",
                Domain = new EducationDomain { Code = "university", NameAr = "جامعي", NameEn = "University" },
                University = new University { NameAr = "جامعة الملك سعود", NameEn = "King Saud University" },
                AcademicProgram = new AcademicProgram
                {
                    NameAr = "علوم الحاسب",
                    NameEn = "Computer Science",
                    Department = new Department
                    {
                        NameAr = "الحاسب الآلي",
                        NameEn = "Computer",
                        College = new College
                        {
                            NameAr = "كلية علوم الحاسب",
                            NameEn = "College of Computer Science",
                        },
                    },
                },
                LevelId = 10,
                Level = year,
            },
            EducationLevels =
            {
                new TeacherSubjectEducationLevel
                {
                    EducationLevelId = 10,
                    EducationLevel = year,
                },
            },
        };

        var dest = new TeacherSubjectResponseDto();
        TeacherSubjectCoverageMapper.ApplyCoverage(src, dest);

        Assert.Equal(
            "جامعة الملك سعود · كلية علوم الحاسب · الحاسب الآلي · علوم الحاسب · السنة الأولى",
            dest.CoverageSummaryAr);
        Assert.DoesNotContain("السنة الأولى · السنة الأولى", dest.CoverageSummaryAr);
        Assert.Contains(dest.CoverageLabels, l => l.Kind == "University");
        Assert.Contains(dest.CoverageLabels, l => l.Kind == "College");
        Assert.Contains(dest.CoverageLabels, l => l.Kind == "AcademicProgram");
        Assert.Single(dest.CoverageLabels.Where(l => l.NameAr == "السنة الأولى"));
    }

    [Fact]
    public void ApplyCoverage_Language_GroupsGradesByAgeBandThenWritables()
    {
        var children = new EducationLevel { Id = 1, NameAr = "أطفال", NameEn = "Children", OrderIndex = 1 };
        var youth = new EducationLevel { Id = 2, NameAr = "شباب", NameEn = "Youth", OrderIndex = 2 };
        var src = new TeacherSubject
        {
            CanTeachFullSubject = true,
            Subject = new Subject
            {
                NameAr = "اللغة العربية لغير الناطقين بها",
                NameEn = "Arabic for non-native speakers",
                Domain = new EducationDomain { Code = "language", NameAr = "اللغات", NameEn = "Languages" },
            },
            EducationLevels =
            {
                new TeacherSubjectEducationLevel { EducationLevelId = 1, EducationLevel = children },
                new TeacherSubjectEducationLevel { EducationLevelId = 2, EducationLevel = youth },
            },
            Grades =
            {
                new TeacherSubjectGrade
                {
                    Grade = new Grade { NameAr = "A2", NameEn = "A2", OrderIndex = 2, LevelId = 1, Level = children },
                },
                new TeacherSubjectGrade
                {
                    Grade = new Grade { NameAr = "A1", NameEn = "A1", OrderIndex = 1, LevelId = 1, Level = children },
                },
                new TeacherSubjectGrade
                {
                    Grade = new Grade { NameAr = "B1", NameEn = "B1", OrderIndex = 3, LevelId = 2, Level = youth },
                },
            },
            WritableFilters =
            {
                new TeacherSubjectWritableFilter
                {
                    WritableFilterValue = new WritableFilterValue { NameAr = "المحادثة والممارسة", NameEn = "Conversation" },
                },
                new TeacherSubjectWritableFilter
                {
                    WritableFilterValue = new WritableFilterValue { NameAr = "لغة الأعمال والعمل", NameEn = "Business" },
                },
            },
        };

        var dest = new TeacherSubjectResponseDto();
        TeacherSubjectCoverageMapper.ApplyCoverage(src, dest);

        Assert.Equal(
            "أطفال (A1، A2) · شباب (B1) · المحادثة والممارسة · لغة الأعمال والعمل",
            dest.CoverageSummaryAr);
        Assert.Equal(
            "Children (A1, A2) · Youth (B1) · Conversation · Business",
            dest.CoverageSummaryEn);
        Assert.Contains(dest.CoverageLabels, l => l.Kind == "LevelGrade" && l.NameAr == "أطفال (A1، A2)");
        Assert.Contains(dest.CoverageLabels, l => l.Kind == "WritableFilter");
        Assert.DoesNotContain(dest.CoverageLabels, l => l.Kind == "EducationLevel");
    }
}
