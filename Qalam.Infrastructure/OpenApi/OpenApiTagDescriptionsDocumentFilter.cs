using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Qalam.Infrastructure.OpenApi;

public sealed class OpenApiTagDescriptionsDocumentFilter : IDocumentFilter
{
    private static readonly Dictionary<string, string> Descriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Authentication Config (Public)"] =
            """
            **For frontend apps** — no JWT. Start here: `GET /Api/V1/Authentication/Config`.
            
            Returns `teacher` / `student` UI flags (`showPhoneField`, `otpDelivery`, hints) and `otp` (`length`, `expirySeconds`).
            
            **Repo guide:** `docs/Auth-Config-Frontend.md`
            """,
        ["Admin · Auth Settings"] =
            """
            **For SuperAdmin only** — JWT with role SuperAdmin.
            
            `GET` / `PUT /Api/V1/Admin/SystemSettings/Auth` — same `Auth.Settings` row as public Config (`common.SystemSettings`).
            
            **Repo guide:** `docs/Auth-Config-Frontend.md` (section «Admin: change settings»)
            """,
        ["Teacher Authentication"] =
            """
            Teacher login/register OTP flow. Configure delivery via **Authentication Config** → `data.teacher`.
            Email OTP: `LoginOrRegister` → bilingual email → `VerifyOtp`.
            """,
        ["Student Authentication"] =
            """
            Student/parent OTP flow. Configure via **Authentication Config** → `data.student`.
            Email OTP: `SendOtp` → bilingual email → `VerifyOtp`.
            """,
    };

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags ??= new List<OpenApiTag>();

        foreach (var (name, description) in Descriptions)
        {
            var tag = swaggerDoc.Tags.FirstOrDefault(t =>
                string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

            if (tag is null)
            {
                swaggerDoc.Tags.Add(new OpenApiTag { Name = name, Description = description });
                continue;
            }

            tag.Description = description;
        }
    }
}
