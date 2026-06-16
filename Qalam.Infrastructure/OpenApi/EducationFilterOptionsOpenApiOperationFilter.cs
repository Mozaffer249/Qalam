using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Qalam.Infrastructure.OpenApi;

/// <summary>
/// Documents the education filter wizard step order (Subject before Term) in Scalar/Swagger.
/// </summary>
public sealed class EducationFilterOptionsOpenApiOperationFilter : IOperationFilter
{
    private const string Guide = "Qalam.Data/AppMetaData/docs/Education_Business_Logic.md";

    private static readonly string[] QueryParameterOrder =
    [
        "domainId", "DomainId",
        "curriculumId", "CurriculumId",
        "levelId", "LevelId",
        "gradeId", "GradeId",
        "subjectId", "SubjectId",
        "termIds", "TermIds",
        "contentUnitId", "ContentUnitId",
        "lessonIds", "LessonIds",
        "quranContentTypeId", "QuranContentTypeId",
        "quranLevelId", "QuranLevelId",
        "unitTypeCode", "UnitTypeCode",
        "skipLessons", "SkipLessons",
        "pageNumber", "PageNumber",
        "pageSize", "PageSize"
    ];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.TrimEnd('/') ?? string.Empty;
        if (!path.EndsWith("Education/filter-options", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(context.ApiDescription.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        AppendDescription(operation,
            $"""

            **Education filter wizard (stateless)**
            
            Call repeatedly, passing **all** selections accumulated so far. Read `data.nextStep` and render `data.options[]`, `data.unit[]` (when `nextStep` is `Unit`), or finish at `Done`.

            ### Standard domains (`school`, `university`, `language`, …)

            | Step | Send when `nextStep` was | Query param to add |
            |:----:|--------------------------|-------------------|
            | 1 | — | `domainId` (required) |
            | 2 | `Curriculum` | `curriculumId` |
            | 3 | `Level` | `levelId` |
            | 4 | `Grade` | `gradeId` |
            | 5 | **`Subject`** | **`subjectId`** ← **before term** |
            | 6 | **`Term`** | **`termIds`** (repeat: `termIds=1&termIds=2`) |
            | 7 | `Unit` | pick from `data.unit[]`, then send **`contentUnitId`** |
            | 8 | `Lesson` (optional) | **`lessonIds`** (repeat) or **`skipLessons=true`** |
            | 9 | `Done` | selection complete |

            > **Order:** Subject before Term. After unit, domains with `rule.hasLessons` show lessons (optional — use `skipLessons=true` to skip).

            ### Quran domain (`code = quran`)

            Single response: auto-selected subject + `contentTypes` + `levels` + paginated `unit[]`. Use `unitTypeCode` (`QuranPart` default, or `QuranSurah`).

            **Full guide:** `{Guide}`
            """);

        ReorderQueryParameters(operation);
        SetParameterDescription(operation, "subjectId",
            "Step 5 (standard flow): subject ID — send after gradeId, before termIds.");
        SetParameterDescription(operation, "SubjectId",
            "Step 5 (standard flow): subject ID — send after gradeId, before termIds.");
        SetParameterDescription(operation, "termIds",
            "Step 6 (standard flow): one or more academic term IDs — send after subjectId. Repeat param for multi-select.");
        SetParameterDescription(operation, "TermIds",
            "Step 6 (standard flow): one or more academic term IDs — send after subjectId. Repeat param for multi-select.");
        SetParameterDescription(operation, "gradeId",
            "Step 4: grade ID — next step is Subject (not Term).");
        SetParameterDescription(operation, "GradeId",
            "Step 4: grade ID — next step is Subject (not Term).");
        SetParameterDescription(operation, "contentUnitId",
            "Step 7: content unit ID — send after picking from data.unit[] when nextStep was Unit.");
        SetParameterDescription(operation, "ContentUnitId",
            "Step 7: content unit ID — send after picking from data.unit[] when nextStep was Unit.");
        SetParameterDescription(operation, "lessonIds",
            "Step 8 (optional): lesson IDs after contentUnitId. Repeat param for multi-select.");
        SetParameterDescription(operation, "LessonIds",
            "Step 8 (optional): lesson IDs after contentUnitId. Repeat param for multi-select.");
        SetParameterDescription(operation, "skipLessons",
            "When true with contentUnitId, skip Lesson step and return Done.");
        SetParameterDescription(operation, "SkipLessons",
            "When true with contentUnitId, skip Lesson step and return Done.");
    }

    private static void ReorderQueryParameters(OpenApiOperation operation)
    {
        if (operation.Parameters is null || operation.Parameters.Count == 0)
            return;

        operation.Parameters = operation.Parameters
            .OrderBy(p =>
            {
                var index = Array.FindIndex(
                    QueryParameterOrder,
                    name => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
                return index >= 0 ? index : QueryParameterOrder.Length;
            })
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void SetParameterDescription(OpenApiOperation operation, string name, string description)
    {
        var param = operation.Parameters?.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (param is not null)
            param.Description = description;
    }

    private static void AppendDescription(OpenApiOperation operation, string extra)
    {
        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? extra.Trim()
            : operation.Description.TrimEnd() + extra;
    }
}
