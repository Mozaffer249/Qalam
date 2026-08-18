using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Core.Mapping;

/// <summary>
/// Builds profile-facing coverage summaries from persisted TeacherSubject selections.
/// </summary>
public static class TeacherSubjectCoverageMapper
{
    private const string Separator = " · ";

    public static void ApplyCoverage(TeacherSubject src, TeacherSubjectResponseDto dest)
    {
        var labels = new List<TeacherSubjectCoverageLabelDto>();
        var segmentsAr = new List<string>();
        var segmentsEn = new List<string>();

        var domainCode = src.Subject?.Domain?.Code?.Trim().ToLowerInvariant() ?? "";
        var isQuran = domainCode == "quran";

        if (isQuran)
        {
            AppendSegment(
                src.QuranContentTypes
                    .Where(c => c.QuranContentType != null)
                    .Select(c => (c.QuranContentType!.NameAr, c.QuranContentType.NameEn)),
                "QuranContentType",
                labels,
                segmentsAr,
                segmentsEn);

            AppendSegment(
                src.WritableFilters
                    .Where(w => w.WritableFilterValue != null)
                    .Select(w => (w.WritableFilterValue!.NameAr, w.WritableFilterValue.NameEn)),
                "WritableFilter",
                labels,
                segmentsAr,
                segmentsEn);

            AppendSegment(
                src.EducationLevels
                    .Where(l => l.EducationLevel != null)
                    .Select(l => (l.EducationLevel!.NameAr, l.EducationLevel.NameEn)),
                "EducationLevel",
                labels,
                segmentsAr,
                segmentsEn);
        }
        else
        {
            AppendCatalogPath(src, labels, segmentsAr, segmentsEn);

            if (src.Subject?.ParentSubject != null)
            {
                AppendSegment(
                    [(src.Subject.ParentSubject.NameAr, src.Subject.ParentSubject.NameEn)],
                    "ParentSubject",
                    labels,
                    segmentsAr,
                    segmentsEn);
            }

            var catalogLevelId = src.Subject?.LevelId;
            AppendSegment(
                src.EducationLevels
                    .Where(l => l.EducationLevel != null && l.EducationLevelId != catalogLevelId)
                    .Select(l => (l.EducationLevel!.NameAr, l.EducationLevel.NameEn)),
                "EducationLevel",
                labels,
                segmentsAr,
                segmentsEn);

            AppendSegment(
                src.WritableFilters
                    .Where(w => w.WritableFilterValue != null)
                    .Select(w => (w.WritableFilterValue!.NameAr, w.WritableFilterValue.NameEn)),
                "WritableFilter",
                labels,
                segmentsAr,
                segmentsEn);
        }

        if (!src.CanTeachFullSubject && src.TeacherSubjectUnits.Count > 0)
        {
            AppendSegment(
                src.TeacherSubjectUnits
                    .Where(u => u.Unit != null)
                    .Select(u => (u.Unit!.NameAr, u.Unit.NameEn)),
                "Unit",
                labels,
                segmentsAr,
                segmentsEn);
        }

        dest.CoverageLabels = labels;
        dest.CoverageSummaryAr = string.Join(Separator, segmentsAr);
        dest.CoverageSummaryEn = string.Join(Separator, segmentsEn);
    }

    private static void AppendCatalogPath(
        TeacherSubject src,
        List<TeacherSubjectCoverageLabelDto> labels,
        List<string> segmentsAr,
        List<string> segmentsEn)
    {
        var subject = src.Subject;
        if (subject == null)
        {
            return;
        }

        var college = subject.AcademicProgram?.Department?.College;
        var university = subject.University ?? college?.University;

        if (university != null)
        {
            AppendSegment([(university.NameAr, university.NameEn)], "University", labels, segmentsAr, segmentsEn);
        }

        if (college != null)
        {
            AppendSegment([(college.NameAr, college.NameEn)], "College", labels, segmentsAr, segmentsEn);
        }

        if (subject.AcademicProgram?.Department != null)
        {
            AppendSegment(
                [(subject.AcademicProgram.Department.NameAr, subject.AcademicProgram.Department.NameEn)],
                "Department",
                labels,
                segmentsAr,
                segmentsEn);
        }

        if (subject.AcademicProgram != null)
        {
            AppendSegment(
                [(subject.AcademicProgram.NameAr, subject.AcademicProgram.NameEn)],
                "AcademicProgram",
                labels,
                segmentsAr,
                segmentsEn);
        }

        if (subject.Curriculum != null)
        {
            AppendSegment(
                [(subject.Curriculum.NameAr, subject.Curriculum.NameEn)],
                "Curriculum",
                labels,
                segmentsAr,
                segmentsEn);
        }

        if (subject.Level != null)
        {
            AppendSegment(
                [(subject.Level.NameAr, subject.Level.NameEn)],
                "Stage",
                labels,
                segmentsAr,
                segmentsEn);
        }

        if (subject.Grade != null)
        {
            AppendSegment(
                [(subject.Grade.NameAr, subject.Grade.NameEn)],
                "Grade",
                labels,
                segmentsAr,
                segmentsEn);
        }
    }

    private static void AppendSegment(
        IEnumerable<(string NameAr, string NameEn)> items,
        string kind,
        List<TeacherSubjectCoverageLabelDto> labels,
        List<string> segmentsAr,
        List<string> segmentsEn)
    {
        var list = items
            .Where(i => !string.IsNullOrWhiteSpace(i.NameAr) || !string.IsNullOrWhiteSpace(i.NameEn))
            .ToList();
        if (list.Count == 0)
        {
            return;
        }

        var lastAr = segmentsAr.Count > 0 ? segmentsAr[^1] : null;
        var lastEn = segmentsEn.Count > 0 ? segmentsEn[^1] : null;

        foreach (var (nameAr, nameEn) in list)
        {
            if (string.Equals(nameAr, lastAr, StringComparison.Ordinal)
                && string.Equals(nameEn, lastEn, StringComparison.Ordinal))
            {
                continue;
            }

            labels.Add(new TeacherSubjectCoverageLabelDto
            {
                NameAr = nameAr,
                NameEn = nameEn,
                Kind = kind,
            });
        }

        var joinedAr = string.Join(Separator, list.Select(i => i.NameAr));
        var joinedEn = string.Join(Separator, list.Select(i => i.NameEn));
        if (string.Equals(joinedAr, lastAr, StringComparison.Ordinal)
            && string.Equals(joinedEn, lastEn, StringComparison.Ordinal))
        {
            return;
        }

        segmentsAr.Add(joinedAr);
        segmentsEn.Add(joinedEn);
    }
}
