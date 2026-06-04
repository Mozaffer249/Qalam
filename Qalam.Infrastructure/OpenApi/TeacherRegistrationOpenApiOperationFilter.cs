using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Qalam.Infrastructure.OpenApi;

/// <summary>
/// Scalar/Swagger hints for admin-controlled teacher registration requirements.
/// </summary>
public sealed class TeacherRegistrationOpenApiOperationFilter : IOperationFilter
{
    private const string Guide = "docs/Teacher-Registration-Requirements.md";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.TrimEnd('/') ?? string.Empty;
        var method = context.ApiDescription.HttpMethod;

        switch (path)
        {
            case "Api/V1/Authentication/Teacher/RegistrationRequirements" when method == "GET":
                AppendDescription(operation,
                    $"""
                    
                    **Teacher registration — load wizard fields**
                    - No auth required (`AllowAnonymous`).
                    - Returns active requirements only (`code`, `type`, `required`, labels, file limits).
                    - Call before step 4 (documents / bio / location).

                    **Repository guide:** `{Guide}`
                    """);
                break;

            case "Api/V1/Authentication/Teacher/SubmitRegistrationRequirements" when method == "POST":
                AppendDescription(operation,
                    $"""
                    
                    **Teacher registration — submit requirements (multipart)**
                    - Requires Teacher JWT.
                    - Legacy fields: `identityDocumentFile`, `certificates`, `isInSaudiArabia`, `bio`, identity metadata.
                    - Custom file requirements: form field name `file_` + requirement `code` (e.g. `file_custom_cv`).
                    - Prefer this endpoint over obsolete `UploadDocuments`.

                    **Repository guide:** `{Guide}`
                    """);
                break;

            case "Api/V1/Authentication/Teacher/UploadDocuments" when method == "POST":
                AppendDescription(operation,
                    """
                    
                    **Obsolete wrapper** — same handler as `SubmitRegistrationRequirements` for backward-compatible mobile builds.
                    """);
                break;

            case "Api/V1/Admin/TeacherRegistrationRequirements" when method == "GET":
            case var p when p.StartsWith("Api/V1/Admin/TeacherRegistrationRequirements/", StringComparison.Ordinal) && method == "GET":
                AppendDescription(operation,
                    $"""
                    
                    **SuperAdmin** — manage registration requirement catalog (includes inactive).
                    **Repository guide:** `{Guide}`
                    """);
                break;

            case "Api/V1/Teacher/TeacherDocuments/Status" when method == "GET":
                AppendDescription(operation,
                    $"""
                    
                    **Teacher** — per-requirement submission checklist plus legacy document list.
                    **Repository guide:** `{Guide}`
                    """);
                break;
        }
    }

    private static void AppendDescription(OpenApiOperation operation, string extra)
    {
        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? extra.Trim()
            : operation.Description.TrimEnd() + extra;
    }
}
