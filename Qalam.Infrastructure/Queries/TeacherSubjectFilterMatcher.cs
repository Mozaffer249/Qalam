using Qalam.Data.DTOs.Student;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Infrastructure.Queries;

/// <summary>
/// Applies Discover/Catalog filters against <see cref="TeacherSubject"/> coverage junctions.
/// </summary>
public static class TeacherSubjectFilterMatcher
{
    public static IQueryable<TeacherSubject> ApplyDiscoverFilters(
        this IQueryable<TeacherSubject> query,
        TeacherSubjectDiscoverFilters filters)
    {
        if (filters.DomainId.HasValue)
        {
            var domainId = filters.DomainId.Value;
            query = query.Where(ts => ts.Subject != null && ts.Subject.DomainId == domainId);
        }

        if (filters.CurriculumId.HasValue)
        {
            var curriculumId = filters.CurriculumId.Value;
            query = query.Where(ts => ts.Subject != null && ts.Subject.CurriculumId == curriculumId);
        }

        if (filters.ParentSubjectId.HasValue)
        {
            var parentSubjectId = filters.ParentSubjectId.Value;
            query = query.Where(ts =>
                ts.Subject != null && ts.Subject.ParentSubjectId == parentSubjectId);
        }

        if (filters.SubjectIds is { Count: > 0 })
        {
            var subjectIds = filters.SubjectIds;
            query = query.Where(ts => subjectIds.Contains(ts.SubjectId));
        }
        else if (filters.SubjectId.HasValue)
        {
            var subjectId = filters.SubjectId.Value;
            query = query.Where(ts => ts.SubjectId == subjectId);
        }

        if (filters.LevelId.HasValue)
        {
            var levelId = filters.LevelId.Value;
            query = query.Where(ts =>
                ts.Subject != null
                && (ts.Subject.LevelId == levelId
                    || ts.EducationLevels.Any(el => el.EducationLevelId == levelId)));
        }

        if (filters.GradeId.HasValue)
        {
            var gradeId = filters.GradeId.Value;
            query = query.Where(ts =>
                ts.Subject != null
                && (ts.Subject.GradeId == gradeId
                    || ts.Grades.Any(g => g.GradeId == gradeId)));
        }

        var qContentIds = ResolveIdList(filters.QuranContentTypeIds, filters.QuranContentTypeId);
        if (qContentIds is { Count: > 0 })
        {
            query = query.Where(ts =>
                !ts.QuranContentTypes.Any()
                || ts.QuranContentTypes.Any(c => qContentIds.Contains(c.QuranContentTypeId)));
        }

        var qLevelIds = ResolveIdList(filters.QuranLevelIds, filters.QuranLevelId);
        if (qLevelIds is { Count: > 0 })
        {
            query = query.Where(ts =>
                (!ts.QuranLevels.Any() && !ts.EducationLevels.Any())
                || ts.QuranLevels.Any(l => qLevelIds.Contains(l.QuranLevelId))
                || ts.EducationLevels.Any(el => qLevelIds.Contains(el.EducationLevelId)));
        }

        if (filters.WritableFilterValueIds is { Count: > 0 })
        {
            var writableIds = filters.WritableFilterValueIds;
            query = query.Where(ts =>
                ts.WritableFilters.Any(w => writableIds.Contains(w.WritableFilterValueId)));
        }

        if (filters.FieldLevelPairs is { Count: > 0 } fieldLevelPairs)
        {
            IQueryable<int>? matchingTeacherSubjectIds = null;
            foreach (var pair in fieldLevelPairs)
            {
                var valueId = pair.WritableFilterValueId;
                var levelId = pair.EducationLevelId;
                var branch = query.Where(ts =>
                        ts.FieldLevels.Any(fl =>
                            fl.WritableFilterValueId == valueId
                            && fl.EducationLevelId == levelId))
                    .Select(ts => ts.Id);
                matchingTeacherSubjectIds = matchingTeacherSubjectIds == null
                    ? branch
                    : matchingTeacherSubjectIds.Union(branch);
            }

            if (matchingTeacherSubjectIds != null)
                query = query.Where(ts => matchingTeacherSubjectIds.Contains(ts.Id));
        }

        return query;
    }

    public static TeacherSubjectDiscoverFilters FromTeacherSearchFilters(
        TeacherSearchFilters filters) =>
        new()
        {
            DomainId = filters.DomainId,
            CurriculumId = filters.CurriculumId,
            ParentSubjectId = filters.ParentSubjectId,
            SubjectId = filters.SubjectId,
            SubjectIds = filters.SubjectIds,
            LevelId = filters.LevelId,
            GradeId = filters.GradeId,
            QuranContentTypeId = filters.QuranContentTypeId,
            QuranContentTypeIds = filters.QuranContentTypeIds,
            QuranLevelId = filters.QuranLevelId,
            QuranLevelIds = filters.QuranLevelIds,
            WritableFilterValueIds = filters.WritableFilterValueIds,
            FieldLevelPairs = filters.FieldLevelPairs,
        };

    public static bool HasAnyDiscoverFilters(TeacherSubjectDiscoverFilters filters) =>
        filters.DomainId.HasValue
        || filters.CurriculumId.HasValue
        || filters.ParentSubjectId.HasValue
        || filters.SubjectId.HasValue
        || filters.SubjectIds is { Count: > 0 }
        || filters.LevelId.HasValue
        || filters.GradeId.HasValue
        || filters.QuranContentTypeId.HasValue
        || filters.QuranContentTypeIds is { Count: > 0 }
        || filters.QuranLevelId.HasValue
        || filters.QuranLevelIds is { Count: > 0 }
        || filters.WritableFilterValueIds is { Count: > 0 }
        || filters.FieldLevelPairs is { Count: > 0 };

    public static bool HasAnyDiscoverFilters(TeacherSearchFilters filters) =>
        HasAnyDiscoverFilters(FromTeacherSearchFilters(filters));

    private static List<int>? ResolveIdList(List<int>? ids, int? singleId)
    {
        if (ids is { Count: > 0 })
            return ids;
        if (singleId.HasValue)
            return new List<int> { singleId.Value };
        return null;
    }
}
