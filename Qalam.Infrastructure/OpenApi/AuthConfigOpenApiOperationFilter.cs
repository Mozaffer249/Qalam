using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Qalam.Infrastructure.OpenApi;

/// <summary>
/// Adds Scalar/Swagger hints and doc references for auth settings endpoints.
/// </summary>
public sealed class AuthConfigOpenApiOperationFilter : IOperationFilter
{
    private const string FrontendGuide = "docs/Auth-Config-Frontend.md";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.TrimEnd('/') ?? string.Empty;

        switch (path)
        {
            case "Api/V1/Authentication/Config" when context.ApiDescription.HttpMethod == "GET":
                AppendDescription(operation,
                    """
                    
                    **Scalar hint — frontend flow**
                    1. Call this endpoint on app load (no `Authorization` header).
                    2. Read `data.teacher` or `data.student` for which fields to show and whether OTP goes to Email or Sms.
                    3. Use `data.otp.length` for the OTP input and `otpHintEn` / `otpHintAr` on the verify screen.
                    4. Then call Teacher `LoginOrRegister` or Student `SendOtp` (see repo guide below).

                    **Repository guide:** `docs/Auth-Config-Frontend.md`  
                    **Also see:** `STUDENT_AUTH_FRONTEND_GUIDE.md`, `TEACHER_AUTH_FRONTEND_GUIDE copy.md` (repo root)
                    """);
                break;

            case "Api/V1/Admin/SystemSettings/Auth" when context.ApiDescription.HttpMethod == "GET":
                AppendDescription(operation,
                    $"""
                    
                    **Scalar hint — admin read**
                    - Requires **SuperAdmin** JWT (`Authorization: Bearer …`).
                    - Returns the stored JSON (`teacher`, `student`, `otp`) from `common.SystemSettings` key `Auth.Settings`.
                    - Same settings as public Config, but admin shape (`registerRequiresEmail`, not `showEmailField`).

                    **Repository guide:** `{FrontendGuide}` (Admin section)
                    """);
                break;

            case "Api/V1/Admin/SystemSettings/Auth" when context.ApiDescription.HttpMethod == "PUT":
                AppendDescription(operation,
                    $"""
                    
                    **Scalar hint — admin update**
                    - Requires **SuperAdmin** JWT.
                    - Body: full `AuthSettingsDto` (`teacher`, `student`, `otp`).
                    - `otpDelivery: "Sms"` only works if `SmsSettings:Enabled` is true on the server.
                    - Changes apply immediately; public `GET …/Authentication/Config` reads from DB (no cache).

                    **Repository guide:** `{FrontendGuide}` (Admin section)
                    """);
                break;

            case "Api/V1/Authentication/Teacher/LoginOrRegister" when context.ApiDescription.HttpMethod == "POST":
                AppendDescription(operation,
                    """
                    
                    **Teacher auth — send OTP**
                    1. Load `GET …/Authentication/Config` → `data.teacher`.
                    2. If `otpDelivery` is `Email`, provide `email` (+ phone). OTP is queued → Messaging API → SMTP.
                    3. Email subject/body: bilingual HTML (teacher copy). Check `otpSentTo` / `maskedDestination`.
                    4. Next: `POST …/Authentication/Teacher/VerifyOtp` with phone + OTP code.
                    """);
                break;

            case "Api/V1/Authentication/Teacher/VerifyOtp" when context.ApiDescription.HttpMethod == "POST":
                AppendDescription(operation,
                    """
                    
                    **Teacher auth — verify OTP** (code from email or SMS per `data.teacher.otpDelivery`).
                    """);
                break;

            case "Api/V1/Authentication/Student/SendOtp" when context.ApiDescription.HttpMethod == "POST":
                AppendDescription(operation,
                    """
                    
                    **Student/parent auth — send OTP**
                    1. Load `GET …/Authentication/Config` → `data.student`.
                    2. If `otpDelivery` is `Email`, include `email` when required. Same SMTP pipeline as teacher.
                    3. Next: `POST …/Authentication/Student/VerifyOtp`.
                    """);
                break;

            case "Api/V1/Authentication/Student/VerifyOtp" when context.ApiDescription.HttpMethod == "POST":
                AppendDescription(operation,
                    """
                    
                    **Student/parent auth — verify OTP** (code from email or SMS per `data.student.otpDelivery`).
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
