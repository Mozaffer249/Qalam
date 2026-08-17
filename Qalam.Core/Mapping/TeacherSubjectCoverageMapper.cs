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
        else if (src.WritableFilters.Count > 0)
        {
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

    private static void AppendSegment(
        IEnumerable<(string NameAr, string NameEn)> items,
        string kind,
        List<TeacherSubjectCoverageLabelDto> labels,
        List<string> segmentsAr,
        List<string> segmentsEn)
    {
        var list = items.ToList();
        if (list.Count == 0)
        {
            return;
        }

        foreach (var (nameAr, nameEn) in list)
        {
            labels.Add(new TeacherSubjectCoverageLabelDto
            {
                NameAr = nameAr,
                NameEn = nameEn,
                Kind = kind,
            });
        }

        segmentsAr.Add(string.Join(Separator, list.Select(i => i.NameAr)));
        segmentsEn.Add(string.Join(Separator, list.Select(i => i.NameEn)));
    }
}
